[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$EnvFile,
    [string]$ManifestDirectory,
    [string]$EvidenceDirectory,
    [switch]$Template
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot 'infra/production/compose.production.yaml'
$resolvedEnvFile = (Resolve-Path $EnvFile).Path
$failures = [System.Collections.Generic.List[string]]::new()

$configOutput = & docker compose --env-file $resolvedEnvFile -f $composeFile config --format json
if ($LASTEXITCODE -ne 0) {
    throw 'Production Compose configuration could not be rendered.'
}
$config = $configOutput | ConvertFrom-Json

foreach ($property in $config.services.PSObject.Properties) {
    $serviceName = $property.Name
    $service = $property.Value
    if ($service.PSObject.Properties.Name -contains 'build') {
        $failures.Add("Service '$serviceName' contains a build instruction; release deployments must pull immutable artifacts.")
    }
    if ($service.image -notmatch '@sha256:[0-9a-f]{64}$') {
        $failures.Add("Service '$serviceName' is not pinned by sha256 digest.")
    }
    if ($serviceName -ne 'nginx' -and $service.ports) {
        $failures.Add("Service '$serviceName' publishes a host port; only Nginx may be public.")
    }
}

$api = $config.services.api
$web = $config.services.web
$releaseCommit = $api.environment.Release__CommitSha
$apiDigest = $api.environment.Release__ApiImageDigest
$webDigest = $api.environment.Release__WebImageDigest

if ($releaseCommit -notmatch '^[0-9a-f]{40}$') {
    $failures.Add('Release commit must be a full lowercase 40-character Git SHA.')
}
if ($apiDigest -notmatch '^sha256:[0-9a-f]{64}$' -or -not $api.image.EndsWith("@$apiDigest")) {
    $failures.Add('API image and Release__ApiImageDigest do not identify the same artifact.')
}
if ($webDigest -notmatch '^sha256:[0-9a-f]{64}$' -or -not $web.image.EndsWith("@$webDigest")) {
    $failures.Add('Web image and Release__WebImageDigest do not identify the same artifact.')
}
if ($api.environment.Sitemap__PublicBaseUrl -notmatch '^https://[^/]+/?$') {
    $failures.Add('PUBLIC_URL must be a single HTTPS origin.')
}
if ($api.environment.Database__ApplyMigrationsOnStartup -ne 'false') {
    $failures.Add('Production must not apply database migrations implicitly at application startup.')
}
if ($api.environment.AdminSeed__Enabled -ne 'false') {
    $failures.Add('Admin seed must be disabled in release environments.')
}

if (-not $Template) {
    $sensitiveValues = @(
        $api.environment.ConnectionStrings__Database,
        $api.environment.Jwt__SigningKey,
        $api.environment.ObjectStorage__AccessKey,
        $api.environment.ObjectStorage__SecretKey,
        $api.environment.Sitemap__RevalidationSecret,
        $web.environment.AUTH_SECRET,
        $config.services.postgres.environment.POSTGRES_PASSWORD,
        $config.services.grafana.environment.GF_SECURITY_ADMIN_PASSWORD
    )
    foreach ($value in $sensitiveValues) {
        if ([string]::IsNullOrWhiteSpace($value) -or $value -match '(?i)local-development|example-|replace-with|set-in-') {
            $failures.Add('A required secret is empty or still contains a development/example placeholder.')
            break
        }
    }

    foreach ($mount in @($config.services.nginx.volumes + $config.services.alertmanager.volumes)) {
        $requiredTargets = @('/etc/nginx/tls/tls.crt', '/etc/nginx/tls/tls.key', '/run/secrets/operations_webhook_url')
        if ($mount.type -eq 'bind' -and $mount.target -in $requiredTargets -and -not (Test-Path -LiteralPath $mount.source -PathType Leaf)) {
            $failures.Add("Required deployment file does not exist: $($mount.source)")
        }
    }

    if ([string]::IsNullOrWhiteSpace($ManifestDirectory)) {
        $failures.Add('ManifestDirectory is required for a real release gate.')
    }
}

$manifestEvidence = @{}
if (-not [string]::IsNullOrWhiteSpace($ManifestDirectory)) {
    $resolvedManifestDirectory = (Resolve-Path $ManifestDirectory).Path
    foreach ($component in @('api', 'web')) {
        $manifestPath = Join-Path $resolvedManifestDirectory "$component.json"
        if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
            $failures.Add("Missing release manifest: $manifestPath")
            continue
        }
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        $expectedImage = if ($component -eq 'api') { $api.image } else { $web.image }
        $expectedDigest = if ($component -eq 'api') { $apiDigest } else { $webDigest }
        if ($manifest.component -ne $component -or $manifest.commitSha -ne $releaseCommit -or $manifest.digest -ne $expectedDigest -or "$($manifest.image)@$($manifest.digest)" -ne $expectedImage) {
            $failures.Add("$component manifest does not match the rendered deployment identity.")
        }
        $manifestEvidence[$component] = @{
            path = $manifestPath
            sha256 = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ -ErrorAction Continue }
    throw "Release gate failed with $($failures.Count) issue(s)."
}

$openApiFile = Join-Path $repositoryRoot 'docs/specs/phase-0/openapi.v1.yaml'
$evidence = [ordered]@{
    status = 'pass'
    validatedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    commitSha = $releaseCommit
    apiImage = $api.image
    webImage = $web.image
    composeSha256 = (Get-FileHash -LiteralPath $composeFile -Algorithm SHA256).Hash.ToLowerInvariant()
    openApiSha256 = (Get-FileHash -LiteralPath $openApiFile -Algorithm SHA256).Hash.ToLowerInvariant()
    manifests = $manifestEvidence
    templateValidation = [bool]$Template
}

if (-not [string]::IsNullOrWhiteSpace($EvidenceDirectory)) {
    New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null
    $evidencePath = Join-Path $EvidenceDirectory 'release-gate.json'
    $evidence | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $evidencePath -Encoding utf8NoBOM
    Write-Output "Release gate passed. Evidence: $evidencePath"
}
else {
    Write-Output 'Release gate passed.'
}
