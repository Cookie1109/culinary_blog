$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$statePath = Join-Path $taskRoot 'tmp/minio-tunnel/session.json'
if (-not (Test-Path -LiteralPath $statePath)) { Write-Output 'No active demo tunnel session.'; return }
$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
$process = Get-Process -Id $state.cloudflaredPid -ErrorAction SilentlyContinue
if ($process) {
    if ($process.Path -ne $state.cloudflaredPath -or $process.StartTime.ToUniversalTime().Ticks.ToString() -ne $state.processStartTicks) { throw 'Process identity changed; refusing to stop it.' }
    Stop-Process -Id $process.Id
}
$gateway = & docker inspect $state.gateway | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or $gateway[0].Config.Labels.'culinary.minio-tunnel' -ne $state.session) { throw 'Gateway identity changed; refusing to remove it.' }
& docker rm -f $state.gateway | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not remove demo gateway.' }
Remove-Item -LiteralPath $statePath
Write-Output 'Tunnel and its gateway stopped. MinIO, bucket, volumes and demo IAM user retained.'
