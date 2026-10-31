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
    $coverageResults = Join-Path $PSScriptRoot "../tmp/verify-unit-$([guid]::NewGuid().ToString('N'))"
    Invoke-CheckedCommand dotnet @('test', 'tests/CulinaryBlog.UnitTests', '--configuration', 'Release', '--no-build', '--no-restore', '--collect', 'XPlat Code Coverage', '--results-directory', $coverageResults)
    Invoke-CheckedCommand python @('../scripts/test-application-coverage.py')
    Invoke-CheckedCommand python @('../scripts/check-application-coverage.py', $coverageResults)
    Invoke-CheckedCommand dotnet @('test', 'tests/CulinaryBlog.ArchitectureTests', '--configuration', 'Release', '--no-build', '--no-restore')
    $integrationResults = Join-Path $coverageResults 'integration'
    Invoke-CheckedCommand dotnet @('test', 'tests/CulinaryBlog.IntegrationTests', '--configuration', 'Release', '--no-build', '--no-restore', '--logger', 'trx;LogFileName=integration.trx', '--results-directory', $integrationResults)
    Invoke-CheckedCommand python @('../scripts/verify-endpoint-matrix.py', '--trx', (Join-Path $integrationResults 'integration.trx'))
}
finally {
    Pop-Location
}

Push-Location (Join-Path $PSScriptRoot '../frontend')
try {
    Invoke-CheckedCommand $npmCommand @('ci')
    $schemaPath = Join-Path (Get-Location) 'src/lib/api/schema.d.ts'
    $schemaHash = (Get-FileHash -LiteralPath $schemaPath -Algorithm SHA256).Hash
    Invoke-CheckedCommand $npmCommand @('run', 'generate:api')
    if ((Get-FileHash -LiteralPath $schemaPath -Algorithm SHA256).Hash -ne $schemaHash) {
        throw 'Generated API types changed; review and regenerate the contract before accepting gates'
    }
    Invoke-CheckedCommand $npmCommand @('run', 'format')
    Invoke-CheckedCommand $npmCommand @('test')
    Invoke-CheckedCommand $npmCommand @('run', 'test:components')
    Invoke-CheckedCommand $npmCommand @('run', 'lint')
    Invoke-CheckedCommand $npmCommand @('run', 'typecheck')
    Invoke-CheckedCommand $npmCommand @('run', 'build')
    Invoke-CheckedCommand $npmCommand @('audit', '--audit-level=high')
    Invoke-CheckedCommand $npmCommand @('run', 'test:e2e')
}
finally {
    Pop-Location
}

Push-Location (Join-Path $PSScriptRoot '..')
try {
    Invoke-CheckedCommand docker @('compose', 'config', '--quiet')
    Invoke-CheckedCommand dotnet @('restore', 'tools/QualityAnalyzers/QualityAnalyzers.csproj', '--locked-mode')
    Invoke-CheckedCommand python @('scripts/test-srs-analyzers.py')
    $analyzersTarget = Join-Path $PSScriptRoot 'srs-analyzers.targets'
    Invoke-CheckedCommand dotnet @('build', 'backend/CulinaryBlog.slnx', '-c', 'Release', '--no-restore', '-warnaserror', '-t:Rebuild', "-p:CustomAfterMicrosoftCommonTargets=$analyzersTarget")
}
finally {
    Pop-Location
}
