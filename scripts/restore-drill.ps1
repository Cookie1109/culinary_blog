[CmdletBinding()]
param(
    [string]$ComposeProject = "culinary-blog",
    [string]$ReportDirectory = "artifacts/reliability"
)

$ErrorActionPreference = "Stop"
$baseFiles = @("-f", "compose.yaml", "-f", "infra/operations/compose.reliability.yaml")

function Invoke-Compose {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)

    & docker compose -p $ComposeProject @baseFiles @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose failed: $($Arguments -join ' ')"
    }
}

function Get-LatestBackupEpoch {
    param([ValidateSet("postgres", "minio")][string]$Kind)

    $command = if ($Kind -eq "postgres") {
        "latest=`$(find /backups/postgres -type f -name '*.completed-at' | sort | tail -n 1); test -n `"`$latest`"; cat `"`$latest`""
    }
    else {
        "set -- /backups/minio/????????T??????Z.completed-at; test -f `"`$1`"; for file; do latest=`$file; done; cat `"`$latest`""
    }

    $service = "$Kind-backup"
    $result = & docker compose -p $ComposeProject @baseFiles run --rm --no-deps --entrypoint /bin/sh $service -ec $command
    if ($LASTEXITCODE -ne 0) {
        throw "Cannot read latest $Kind backup timestamp."
    }

    return [long]($result | Select-Object -Last 1)
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker is required for the restore drill."
}

$sourceServices = Invoke-Compose ps --status running --services
foreach ($required in @("postgres", "minio")) {
    if ($sourceServices -notcontains $required) {
        throw "Service '$required' must be running before the restore drill."
    }
}

$startedAt = [DateTimeOffset]::UtcNow
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    Invoke-Compose run --rm --no-deps --entrypoint /bin/sh postgres-backup /opt/operations/postgres-backup.sh
    Invoke-Compose run --rm --no-deps --entrypoint /bin/sh minio-backup /opt/operations/minio-backup.sh

    Invoke-Compose --profile restore-drill rm --stop --force postgres-restore minio-restore
    Invoke-Compose --profile restore-drill up --detach --wait postgres-restore minio-restore
    Invoke-Compose --profile restore-drill run --rm postgres-restore-verifier
    Invoke-Compose --profile restore-drill run --rm minio-restore-verifier

    $stopwatch.Stop()
    $postgresEpoch = Get-LatestBackupEpoch postgres
    $minioEpoch = Get-LatestBackupEpoch minio
    $oldestEpoch = [Math]::Min($postgresEpoch, $minioEpoch)
    $rpoSeconds = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() - $oldestEpoch

    New-Item -ItemType Directory -Force -Path $ReportDirectory | Out-Null
    $reportPath = Join-Path $ReportDirectory "restore-drill-$($startedAt.ToString('yyyyMMddTHHmmssZ')).json"
    [ordered]@{
        startedAtUtc = $startedAt.ToString("O")
        completedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        result = "pass"
        rtoSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 2)
        rpoSeconds = $rpoSeconds
        postgresBackupEpoch = $postgresEpoch
        minioBackupEpoch = $minioEpoch
        cleanTargets = @("postgres-restore:tmpfs", "minio-restore:tmpfs")
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $reportPath -Encoding utf8

    Write-Host "Restore drill passed. RTO=$([Math]::Round($stopwatch.Elapsed.TotalSeconds, 2))s; RPO=${rpoSeconds}s"
    Write-Host "Evidence: $reportPath"
}
finally {
    Invoke-Compose --profile restore-drill rm --stop --force postgres-restore minio-restore
}
