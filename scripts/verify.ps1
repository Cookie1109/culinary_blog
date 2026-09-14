$ErrorActionPreference = 'Stop'

Push-Location backend
try {
    dotnet tool restore
    dotnet restore CulinaryBlog.slnx --locked-mode
    dotnet format CulinaryBlog.slnx --verify-no-changes --no-restore
    dotnet build CulinaryBlog.slnx --configuration Release --no-restore
    dotnet test CulinaryBlog.slnx --configuration Release --no-build --no-restore
}
finally {
    Pop-Location
}

Push-Location frontend
try {
    npm ci
    npm run generate:api
    npm run format
    npm test
    npm run lint
    npm run typecheck
    npm run build
    npm audit --audit-level=high --omit=dev
}
finally {
    Pop-Location
}

docker compose config --quiet
