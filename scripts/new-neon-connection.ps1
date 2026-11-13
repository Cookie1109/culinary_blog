param([string]$PrivateDirectory)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
if (-not $PrivateDirectory) {
    $session = Get-Content -LiteralPath (Join-Path $taskRoot 'tmp/neon-migration/session.json') -Raw | ConvertFrom-Json
    $PrivateDirectory = Join-Path $session.backupDirectory 'neon-private'
}
$PrivateDirectory = [IO.Path]::GetFullPath($PrivateDirectory)
if ($PrivateDirectory.StartsWith([IO.Path]::GetFullPath($taskRoot), [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Credentials must be stored outside the repository.'
}
[IO.Directory]::CreateDirectory($PrivateDirectory) | Out-Null
$sid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
& icacls $PrivateDirectory /inheritance:r /grant:r "*${sid}:(OI)(CI)F" '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' *> $null
if ($LASTEXITCODE -ne 0) { throw 'Cannot restrict credential directory ACL.' }
$branch = Read-Host 'Select production in Neon Console Connect first, then type production'
if ($branch -cne 'production') { throw 'Branch not confirmed; nothing saved.' }
& python -X utf8 (Join-Path $taskRoot 'tools/postgres-neon/save_connection.py') $PrivateDirectory
if ($LASTEXITCODE -ne 0) { throw 'Private connection setup failed; no database connection performed.' }
