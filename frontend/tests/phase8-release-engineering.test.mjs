import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

test('P8-22: production Compose only deploys digest-pinned images behind TLS Nginx', async () => {
  const [compose, env, nginx] = await Promise.all([
    readFile(new URL('../../infra/production/compose.production.yaml', import.meta.url), 'utf8'),
    readFile(new URL('../../infra/production/release.env.example', import.meta.url), 'utf8'),
    readFile(new URL('../../infra/nginx/nginx.production.conf', import.meta.url), 'utf8'),
  ])

  assert.doesNotMatch(compose, /^\s+build:/m)
  assert.match(compose, /API_IMAGE:\?Set API_IMAGE to the release manifest image@sha256 digest/)
  assert.match(compose, /WEB_IMAGE:\?Set WEB_IMAGE to the release manifest image@sha256 digest/)
  assert.match(compose, /pull_policy: always/g)
  assert.match(compose, /backend:\s*\n\s+internal: true/)
  assert.match(compose, /Database__ApplyMigrationsOnStartup: "false"/)
  assert.match(compose, /AdminSeed__Enabled: "false"/)
  assert.match(env, /API_IMAGE=.*@sha256:[0-9a-f]{64}/)
  assert.match(env, /WEB_IMAGE=.*@sha256:[0-9a-f]{64}/)
  assert.match(nginx, /ssl_protocols TLSv1\.2 TLSv1\.3/)
  assert.match(nginx, /Strict-Transport-Security/)
})

test('P8-23: release environment checklist assigns rotation ownership and rejects placeholders', async () => {
  const [checklist, gate] = await Promise.all([
    readFile(new URL('../../docs/release/environment-and-secrets-checklist.md', import.meta.url), 'utf8'),
    readFile(new URL('../../scripts/release-gate.ps1', import.meta.url), 'utf8'),
  ])

  for (const owner of ['DBA/Platform', 'Security Lead', 'On-call Lead', 'Platform/Security']) {
    assert.match(checklist, new RegExp(owner.replace('/', '\\/')))
  }
  assert.match(checklist, /Chu kỳ\/trigger rotation/)
  assert.match(gate, /local-development\|example-\|replace-with\|set-in-/)
  assert.match(gate, /ManifestDirectory is required for a real release gate/)
})

test('P8-24/P8-28: immutable bundle is promoted without build and deployment identity is verified', async () => {
  const [workflow, deploy, gate] = await Promise.all([
    readFile(new URL('../../.github/workflows/release-images.yml', import.meta.url), 'utf8'),
    readFile(new URL('../../scripts/deploy-release.ps1', import.meta.url), 'utf8'),
    readFile(new URL('../../scripts/release-gate.ps1', import.meta.url), 'utf8'),
  ])

  assert.match(workflow, /release-bundle-\$\{\{ github\.sha \}\}/)
  assert.match(workflow, /SHA256SUMS/)
  assert.match(workflow, /attest-build-provenance@v3/)
  assert.match(gate, /manifest\.commitSha -ne \$releaseCommit/)
  assert.match(deploy, /pull/)
  assert.match(deploy, /up --detach --wait --no-build --remove-orphans/)
  assert.match(deploy, /\/api\/v1\/release/)
  assert.match(deploy, /Production deployment requires ApprovedChangeId/)
  assert.match(deploy, /Strict-Transport-Security/)
})

test('P8-25/P8-27: unsigned UAT and go/no-go records remain explicitly blocking', async () => {
  const [uat, decision, notes] = await Promise.all([
    readFile(new URL('../../docs/release/uat-signoff.md', import.meta.url), 'utf8'),
    readFile(new URL('../../docs/release/go-no-go-and-rollback.md', import.meta.url), 'utf8'),
    readFile(new URL('../../docs/release/v1.0.0-release-notes.md', import.meta.url), 'utf8'),
  ])

  for (const journey of ['CJ-01', 'CJ-02', 'CJ-03', 'CJ-04', 'CJ-05']) {
    assert.match(uat, new RegExp(journey))
  }
  assert.match(uat, /Critical\/High mở = No-Go/)
  assert.match(uat, /Decision: \*\*Pending \/ Accept \/ Reject\*\*/)
  assert.match(decision, /No-Go mặc định/)
  assert.match(decision, /Rollback triggers/)
  assert.match(decision, /previous API\/Web digests/)
  assert.match(notes, /Release Candidate \/ No-Go production/)
})
