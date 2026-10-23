$ErrorActionPreference = 'Stop'

function Invoke-CheckedCommand {
    param([string]$Command, [string[]]$Arguments)

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed (exit ${LASTEXITCODE}): $Command $($Arguments -join ' ')"
    }
}

$npmCommand = if ($env:OS -eq 'Windows_NT') { 'npm.cmd' } else { 'npm' }

Push-Location (Join-Path $PSScriptRoot '../backend')
try {
    Invoke-CheckedCommand dotnet @('tool', 'restore')
    Invoke-CheckedCommand dotnet @('restore', 'CulinaryBlog.slnx', '--locked-mode')
    Invoke-CheckedCommand dotnet @('format', 'CulinaryBlog.slnx', '--verify-no-changes', '--no-restore')
    Invoke-CheckedCommand dotnet @('build', 'CulinaryBlog.slnx', '--configuration', 'Release', '--no-restore')
    Invoke-CheckedCommand dotnet @('test', 'CulinaryBlog.slnx', '--configuration', 'Release', '--no-build', '--no-restore')
}
finally {
    Pop-Location
}

Push-Location (Join-Path $PSScriptRoot '../frontend')
try {
    Invoke-CheckedCommand $npmCommand @('ci')
    Invoke-CheckedCommand $npmCommand @('run', 'generate:api')
    Invoke-CheckedCommand $npmCommand @('run', 'format')
    Invoke-CheckedCommand $npmCommand @('test')
    Invoke-CheckedCommand $npmCommand @('run', 'lint')
    Invoke-CheckedCommand $npmCommand @('run', 'typecheck')
    Invoke-CheckedCommand $npmCommand @('run', 'build')
    Invoke-CheckedCommand $npmCommand @('audit', '--audit-level=high')
}
finally {
    Pop-Location
}

Push-Location (Join-Path $PSScriptRoot '..')
try {
    Invoke-CheckedCommand docker @('compose', 'config', '--quiet')
}
finally {
    Pop-Location
}
