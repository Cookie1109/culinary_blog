$ErrorActionPreference = 'Stop'

Push-Location backend
try {
    dotnet build CulinaryBlog.slnx --configuration Release --no-restore
    dotnet test tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj `
        --configuration Release --no-build --no-restore
    dotnet test tests/CulinaryBlog.ArchitectureTests/CulinaryBlog.ArchitectureTests.csproj `
        --configuration Release --no-build --no-restore
    dotnet test tests/CulinaryBlog.IntegrationTests/CulinaryBlog.IntegrationTests.csproj `
        --configuration Release --no-build --no-restore `
        --filter 'FullyQualifiedName!~RecipeDiscoveryPerformanceTests'
}
finally {
    Pop-Location
}

npm --prefix frontend test
