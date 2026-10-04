[CmdletBinding()]
param(
    [string]$ComposeProject = "culinary-blog",
    [uri]$BaseUrl = "http://localhost:8080",
    [uri]$AlertmanagerUrl = "http://127.0.0.1:9093",
    [int]$AlertHoldSeconds = 70,
    [string]$ReportDirectory = "artifacts/reliability"
)

$ErrorActionPreference = "Stop"
$affectedServices = @("api", "redis", "minio", "mailhog", "postgres")
$results = [ordered]@{}

function Invoke-Compose {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)

    & docker compose -p $ComposeProject @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose failed: $($Arguments -join ' ')"
    }
}

function Get-StatusCode {
    param([string]$Path)

    try {
        return (Invoke-WebRequest -Uri ([uri]::new($BaseUrl, $Path)) -SkipHttpErrorCheck -TimeoutSec 5).StatusCode
    }
    catch {
        return 0
    }
}

function Wait-Status {
    param(
        [string]$Path,
        [int[]]$Expected,
        [int]$TimeoutSeconds = 90
    )

    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    do {
        $status = Get-StatusCode $Path
        if ($Expected -contains $status) {
            return [Math]::Round($timer.Elapsed.TotalSeconds, 2)
        }
        Start-Sleep -Seconds 2
    } while ($timer.Elapsed.TotalSeconds -lt $TimeoutSeconds)

    throw "Timed out waiting for $Path to return one of: $($Expected -join ', '). Last status: $status"
}

function Assert-ReadinessAlertFired {
    $alerts = Invoke-RestMethod -Uri ([uri]::new($AlertmanagerUrl, "/api/v2/alerts")) -TimeoutSec 10
    $matching = @($alerts | Where-Object { $_.labels.alertname -eq "CulinaryBlogReadinessDown" })
    if ($matching.Count -eq 0) {
        throw "CulinaryBlogReadinessDown did not reach Alertmanager after ${AlertHoldSeconds}s."
    }
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker is required for the failure drill."
}

$startedAt = [DateTimeOffset]::UtcNow
Wait-Status "/health/ready" @(200) | Out-Null

try {
    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    Invoke-Compose restart api
    Wait-Status "/health/live" @(200) | Out-Null
    Wait-Status "/health/ready" @(200) | Out-Null
    $results.apiAndInProcessWorkerRestartSeconds = [Math]::Round($timer.Elapsed.TotalSeconds, 2)

    Invoke-Compose stop redis
    Wait-Status "/health/ready" @(503, 0) | Out-Null
    Start-Sleep -Seconds $AlertHoldSeconds
    Assert-ReadinessAlertFired
    $timer.Restart()
    Invoke-Compose start redis
    Wait-Status "/health/ready" @(200) | Out-Null
    $results.redisRecoverySeconds = [Math]::Round($timer.Elapsed.TotalSeconds, 2)
    $results.readinessAlertRouted = $true

    Invoke-Compose stop minio
    Wait-Status "/health" @(503, 0) | Out-Null
    if ((Get-StatusCode "/health/ready") -ne 200) {
        throw "Readiness must remain healthy while only MinIO is unavailable."
    }
    $timer.Restart()
    Invoke-Compose start minio
    Wait-Status "/health" @(200) | Out-Null
    $results.minioRecoverySeconds = [Math]::Round($timer.Elapsed.TotalSeconds, 2)

    Invoke-Compose stop mailhog
    if ((Get-StatusCode "/health/ready") -ne 200) {
        throw "SMTP outage must not make the request-serving path unready."
    }
    $timer.Restart()
    Invoke-Compose start mailhog
    Invoke-Compose ps --status running mailhog | Out-Null
    $results.smtpRecoverySeconds = [Math]::Round($timer.Elapsed.TotalSeconds, 2)

    Invoke-Compose stop postgres
    Wait-Status "/health/ready" @(503, 0) | Out-Null
    $timer.Restart()
    Invoke-Compose start postgres
    Wait-Status "/health/ready" @(200) 120 | Out-Null
    $results.postgresRecoverySeconds = [Math]::Round($timer.Elapsed.TotalSeconds, 2)

    New-Item -ItemType Directory -Force -Path $ReportDirectory | Out-Null
    $reportPath = Join-Path $ReportDirectory "failure-drill-$($startedAt.ToString('yyyyMMddTHHmmssZ')).json"
    [ordered]@{
        startedAtUtc = $startedAt.ToString("O")
        completedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        result = "pass"
        scenarios = $results
        note = "Hangfire and the email outbox worker are hosted in the API process; the API restart covers the worker restart scenario."
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $reportPath -Encoding utf8

    Write-Host "Failure drill passed. Evidence: $reportPath"
}
finally {
    foreach ($service in $affectedServices) {
        & docker compose -p $ComposeProject start $service | Out-Null
    }
    Wait-Status "/health/ready" @(200) 120 | Out-Null
}
