import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

test('P8-16/P8-17: daily PostgreSQL and versioned object backups retain 30 days', async () => {
  const [compose, postgres, minio, baseCompose] = await Promise.all([
    readFile(new URL('../../infra/operations/compose.reliability.yaml', import.meta.url), 'utf8'),
    readFile(new URL('../../infra/operations/postgres-backup.sh', import.meta.url), 'utf8'),
    readFile(new URL('../../infra/operations/minio-backup.sh', import.meta.url), 'utf8'),
    readFile(new URL('../../compose.yaml', import.meta.url), 'utf8'),
  ])

  assert.match(compose, /BACKUP_INTERVAL_SECONDS: .*86400/)
  assert.match(compose, /BACKUP_RETENTION_DAYS: "30"/)
  assert.match(postgres, /pg_dump/)
  assert.match(postgres, /pg_restore --list/)
  assert.match(postgres, /sha256sum/)
  assert.match(minio, /mc version info/)
  assert.match(minio, /mc mirror --overwrite --preserve/)
  assert.match(baseCompose, /mc version enable/)
})

test('P8-18/P8-19: restore and failure drills verify recovery and write RTO/RPO evidence', async () => {
  const [restore, failure, postgresRestore, minioRestore] = await Promise.all([
    readFile(new URL('../../scripts/restore-drill.ps1', import.meta.url), 'utf8'),
    readFile(new URL('../../scripts/failure-drill.ps1', import.meta.url), 'utf8'),
    readFile(new URL('../../infra/operations/restore-postgres-drill.sh', import.meta.url), 'utf8'),
    readFile(new URL('../../infra/operations/restore-minio-drill.sh', import.meta.url), 'utf8'),
  ])

  assert.match(restore, /rtoSeconds/)
  assert.match(restore, /rpoSeconds/)
  assert.match(restore, /postgres-restore:tmpfs/)
  assert.match(postgresRestore, /source_migrations.*restored_migrations/s)
  assert.match(postgresRestore, /source_recipes.*restored_recipes/s)
  assert.match(minioRestore, /source_count.*restored_count/s)
  for (const service of ['api', 'redis', 'minio', 'mailhog', 'postgres']) {
    assert.match(failure, new RegExp(`(?:restart|stop) ${service}`))
  }
  assert.match(failure, /CulinaryBlogReadinessDown/)
})

test('P8-20/P8-21: runtime limits, bounded logs, readiness probing, and routed alerts are configured', async () => {
  const [compose, prometheus, alerts, routing] = await Promise.all([
    readFile(new URL('../../infra/operations/compose.reliability.yaml', import.meta.url), 'utf8'),
    readFile(new URL('../../infra/observability/prometheus.yml', import.meta.url), 'utf8'),
    readFile(new URL('../../infra/observability/alerts.yml', import.meta.url), 'utf8'),
    readFile(new URL('../../infra/observability/alertmanager.production.yml', import.meta.url), 'utf8'),
  ])

  assert.match(compose, /restart: unless-stopped/)
  assert.match(compose, /limits: \{ cpus:/)
  assert.match(compose, /max-size: 10m/)
  assert.match(compose, /max-file: "5"/)
  assert.match(compose, /postgres-backups:/)
  assert.match(compose, /minio-backups:/)
  assert.match(prometheus, /scrape_interval: 10s/)
  assert.match(prometheus, /http:\/\/nginx\/health\/ready/)
  assert.match(alerts, /probe_success\{job="culinary-blog-readiness"\}/)
  assert.match(alerts, /for: 1m/)
  assert.match(alerts, /CulinaryBlogWatchdog/)
  assert.match(routing, /url_file: \/run\/secrets\/operations_webhook_url/)
  assert.match(routing, /send_resolved: true/)
})

test('P8-21a: maintenance announcement is runtime-configured and shown across layouts', async () => {
  const [banner, layout, program, guard] = await Promise.all([
    readFile(new URL('../src/features/culinary/components/MaintenanceBanner.tsx', import.meta.url), 'utf8'),
    readFile(new URL('../src/features/culinary/components/layout/AppLayout.tsx', import.meta.url), 'utf8'),
    readFile(new URL('../../backend/src/CulinaryBlog.Api/Program.cs', import.meta.url), 'utf8'),
    readFile(
      new URL('../../backend/src/CulinaryBlog.Api/Security/ProductionConfigurationGuard.cs', import.meta.url),
      'utf8',
    ),
  ])

  assert.match(program, /\/api\/v1\/operations\/maintenance/)
  assert.match(banner, /operations\/maintenance/)
  assert.match(banner, /role="status"/)
  assert.match(layout, /<MaintenanceBanner \/>/)
  assert.match(guard, /TimeSpan\.FromHours\(48\)/)
})
