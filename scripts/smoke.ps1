$ErrorActionPreference = 'Stop'

docker compose up --detach --build --wait

$deadline = (Get-Date).AddMinutes(3)
do {
    try {
        $response = Invoke-WebRequest -Uri 'http://localhost:8080/health' -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-Output 'Smoke test passed: http://localhost:8080/health is healthy.'
            exit 0
        }
    }
    catch {
        Start-Sleep -Seconds 2
    }
} while ((Get-Date) -lt $deadline)

docker compose ps
docker compose logs --tail 100 api web nginx
throw 'Smoke test failed: the aggregate health endpoint did not become healthy within 3 minutes.'
