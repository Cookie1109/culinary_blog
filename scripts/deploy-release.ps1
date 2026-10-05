[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('staging', 'production')]
    [string]$Target,
    [Parameter(Mandatory = $true)]
    [string]$EnvFile,
    [Parameter(Mandatory = $true)]
    [string]$ManifestDirectory,
    [Parameter(Mandatory = $true)]
    [string]$EvidenceDirectory,
    [string]$ApprovedChangeId,
    [switch]$ValidateOnly
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot 'infra/production/compose.production.yaml'
$gateScript = Join-Path $PSScriptRoot 'release-gate.ps1'
$resolvedEnvFile = (Resolve-Path $EnvFile).Path

if ($Target -eq 'production' -and -not $ValidateOnly -and [string]::IsNullOrWhiteSpace($ApprovedChangeId)) {
    throw 'Production deployment requires ApprovedChangeId from the signed go/no-go record.'
}

& $gateScript -EnvFile $resolvedEnvFile -ManifestDirectory $ManifestDirectory -EvidenceDirectory $EvidenceDirectory
if ($ValidateOnly) {
    Write-Output 'Validation completed; no deployment was performed.'
    exit 0
}

$configOutput = & docker compose --env-file $resolvedEnvFile -f $composeFile config --format json
if ($LASTEXITCODE -ne 0) { throw 'Unable to render the release Compose configuration.' }
$config = $configOutput | ConvertFrom-Json
$publicUrl = $config.services.api.environment.Sitemap__PublicBaseUrl.TrimEnd('/')
$expectedCommit = $config.services.api.environment.Release__CommitSha
$expectedApiDigest = $config.services.api.environment.Release__ApiImageDigest
$expectedWebDigest = $config.services.api.environment.Release__WebImageDigest

if (-not $PSCmdlet.ShouldProcess("$Target ($publicUrl)", 'Pull immutable images and update the release stack')) {
    return
}

$startedAt = (Get-Date).ToUniversalTime()
& docker compose --env-file $resolvedEnvFile -f $composeFile pull
if ($LASTEXITCODE -ne 0) { throw 'Release image pull failed.' }
& docker compose --env-file $resolvedEnvFile -f $composeFile up --detach --wait --no-build --remove-orphans
if ($LASTEXITCODE -ne 0) { throw 'Release stack did not become healthy.' }

$ready = Invoke-WebRequest -Uri "$publicUrl/health/ready" -UseBasicParsing
$home = Invoke-WebRequest -Uri "$publicUrl/" -UseBasicParsing
$release = Invoke-RestMethod -Uri "$publicUrl/api/v1/release"
if ($ready.StatusCode -ne 200 -or $home.StatusCode -ne 200) {
    throw 'Post-deploy health or homepage smoke test failed.'
}
if (-not $home.Headers['Strict-Transport-Security'] -or -not $home.Headers['Content-Security-Policy']) {
    throw 'Post-deploy security-header smoke test failed.'
}
if ($release.data.commitSha -ne $expectedCommit -or $release.data.apiImageDigest -ne $expectedApiDigest -or $release.data.webImageDigest -ne $expectedWebDigest) {
    throw 'The deployed release endpoint does not match the promoted artifacts.'
}

New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null
$evidencePath = Join-Path $EvidenceDirectory "$Target-deployment.json"
[ordered]@{
    status = 'pass'
    target = $Target
    publicUrl = $publicUrl
    approvedChangeId = $ApprovedChangeId
    startedAtUtc = $startedAt.ToString('o')
    completedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    commitSha = $expectedCommit
    apiImageDigest = $expectedApiDigest
    webImageDigest = $expectedWebDigest
    smoke = @{
        readiness = $ready.StatusCode
        homepage = $home.StatusCode
        hsts = [bool]$home.Headers['Strict-Transport-Security']
        csp = [bool]$home.Headers['Content-Security-Policy']
    }
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $evidencePath -Encoding utf8NoBOM

Write-Output "Deployment and smoke test passed. Evidence: $evidencePath"
