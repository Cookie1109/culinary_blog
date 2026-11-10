param(
    [Parameter(Mandatory = $true)][string]$ConfigPath,
    [string]$PythonPath = 'C:\Python313\pythonw.exe'
)
$ErrorActionPreference = 'Stop'
$taskName = 'Culinary-G06-PostgreSQLBackup'
$description = 'G06 PostgreSQL only; no media or Google Drive. Daily03:00UTC;30-day own-archive retention.'
$config = Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
$scriptPath = Join-Path $PSScriptRoot 'backup-neon.py'
if ((Get-FileHash -LiteralPath $scriptPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $config.scriptSha256) {
    throw 'Backup script differs from the reviewed private configuration.'
}
if (-not (Test-Path -LiteralPath $PythonPath)) { throw 'Configured Python runtime is unavailable.' }
$existing = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($existing -and $existing.Description -ne $description) { throw 'A different task already uses this name.' }
$userSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Minutes 15) -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 5) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
$principal = New-ScheduledTaskPrincipal -UserId $userSid -LogonType Interactive -RunLevel Limited
$action = New-ScheduledTaskAction -Execute $PythonPath -Argument ('"{0}" --config "{1}"' -f $scriptPath, (Resolve-Path -LiteralPath $ConfigPath).Path)
# A UTC-qualified StartBoundary keeps03:00UTC when Windows timezone changes.
$trigger = New-ScheduledTaskTrigger -Daily -At '03:00'
$trigger.StartBoundary = [DateTime]::UtcNow.Date.AddDays(1).AddHours(3).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")
Register-ScheduledTask -TaskName $taskName -Description $description -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Force | Out-Null
Write-Output 'Registered PostgreSQL-only backup at03:00UTC (10:00Bangkok); no passwords stored in task arguments.'
Write-Output 'Requires this user logged in and Docker running. Missed runs start when available; this is not an always-on host guarantee.'
