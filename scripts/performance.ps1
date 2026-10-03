param(
    [ValidateSet('smoke', 'load', 'stress', 'all')]
    [string]$Profile = 'smoke',
    [string]$BaseUrl = 'http://localhost:8080',
    [string]$PrometheusUrl = 'http://127.0.0.1:9090',
    [switch]$SkipLighthouse
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$artifactDirectory = Join-Path $repositoryRoot "artifacts/performance/$timestamp"
New-Item -ItemType Directory -Force -Path $artifactDirectory | Out-Null

foreach ($command in @('k6', 'node', 'npm')) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        throw "Required command '$command' was not found."
    }
}

$readyUrl = "$($BaseUrl.TrimEnd('/'))/health/ready"
$ready = Invoke-WebRequest -Uri $readyUrl -UseBasicParsing
if ($ready.StatusCode -ne 200) {
    throw "Performance target is not ready at $readyUrl."
}

$recipes = Invoke-RestMethod -Uri "$($BaseUrl.TrimEnd('/'))/api/v1/recipes?page=1&pageSize=12"
$categories = Invoke-RestMethod -Uri "$($BaseUrl.TrimEnd('/'))/api/v1/categories"
$publishedCount = if ($null -ne $recipes.meta.total) { $recipes.meta.total } else { $recipes.data.Count }

$environment = [ordered]@{
    capturedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    target = $BaseUrl
    prometheus = $PrometheusUrl
    operatingSystem = [System.Environment]::OSVersion.VersionString
    logicalProcessors = [System.Environment]::ProcessorCount
    configuredApiLimit = '2 CPU / 4 GiB (performance/compose.performance.yaml)'
    configuredPostgresLimit = '2 CPU / 4 GiB (performance/compose.performance.yaml)'
    dataset = [ordered]@{
        publishedRecipes = $publishedCount
        categories = $categories.data.Count
        source = 'Synthetic Phase 8 seed; no production PII'
    }
    network = 'Load generator to localhost Docker/Nginx; no WAN latency shaping'
    selectedProfile = $Profile
}
$environment | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $artifactDirectory 'environment.json')

$profiles = if ($Profile -eq 'all') { @('smoke', 'load', 'stress') } else { @($Profile) }
$previousProfile = $env:PROFILE
$previousBaseUrl = $env:BASE_URL
$previousPrometheusUrl = $env:PROMETHEUS_URL
$previousRequireCacheMetrics = $env:REQUIRE_CACHE_METRICS

try {
    $env:BASE_URL = $BaseUrl.TrimEnd('/')
    $env:PROMETHEUS_URL = $PrometheusUrl.TrimEnd('/')
    $env:REQUIRE_CACHE_METRICS = 'true'

    foreach ($currentProfile in $profiles) {
        $env:PROFILE = $currentProfile
        $summaryPath = Join-Path $artifactDirectory "k6-$currentProfile.json"
        & k6 run "--summary-export=$summaryPath" (Join-Path $repositoryRoot 'performance/k6/public-api.js')
        if ($LASTEXITCODE -ne 0) {
            throw "k6 $currentProfile profile failed. See $summaryPath."
        }
    }
}
finally {
    $env:PROFILE = $previousProfile
    $env:BASE_URL = $previousBaseUrl
    $env:PROMETHEUS_URL = $previousPrometheusUrl
    $env:REQUIRE_CACHE_METRICS = $previousRequireCacheMetrics
}

if (-not $SkipLighthouse) {
    Push-Location (Join-Path $repositoryRoot 'frontend')
    $previousLhciBaseUrl = $env:LHCI_BASE_URL
    $previousLhciPaths = $env:LHCI_PATHS_JSON
    $previousLhciOutput = $env:LHCI_OUTPUT_DIR
    try {
        & npm.cmd run build
        if ($LASTEXITCODE -ne 0) { throw 'Frontend release build failed.' }

        & npm.cmd run budget:check
        if ($LASTEXITCODE -ne 0) { throw 'Frontend bundle budget failed.' }

        $paths = @('/', '/recipes', '/categories', '/search')
        if ($recipes.data.Count -gt 0) {
            $paths += "/recipes/$([Uri]::EscapeDataString($recipes.data[0].slug))"
        }
        if ($categories.data.Count -gt 0) {
            $paths += "/categories/$([Uri]::EscapeDataString($categories.data[0].slug))"
        }

        $env:LHCI_BASE_URL = $BaseUrl.TrimEnd('/')
        $env:LHCI_PATHS_JSON = ConvertTo-Json -Compress -InputObject $paths
        $env:LHCI_OUTPUT_DIR = Join-Path $artifactDirectory 'lighthouse'
        & npm.cmd run lighthouse:release
        if ($LASTEXITCODE -ne 0) { throw 'Lighthouse CI budgets failed.' }
    }
    finally {
        $env:LHCI_BASE_URL = $previousLhciBaseUrl
        $env:LHCI_PATHS_JSON = $previousLhciPaths
        $env:LHCI_OUTPUT_DIR = $previousLhciOutput
        Pop-Location
    }
}

Write-Output "Performance artifacts: $artifactDirectory"
