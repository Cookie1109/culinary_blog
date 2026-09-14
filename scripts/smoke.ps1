$ErrorActionPreference = 'Stop'

docker compose up --detach --build --wait

$deadline = (Get-Date).AddMinutes(3)
do {
    try {
        $response = Invoke-WebRequest -Uri 'http://localhost:8080/health' -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            $homeResponse = Invoke-WebRequest -Uri 'http://localhost:8080/' -UseBasicParsing
            $contentSecurityPolicy = $homeResponse.Headers['Content-Security-Policy']
            if ($homeResponse.StatusCode -eq 200 -and $contentSecurityPolicy) {
                Write-Output 'Smoke test passed: health and homepage are available with CSP enabled.'
                exit 0
            }
        }
    }
    catch {
        Start-Sleep -Seconds 2
    }
} while ((Get-Date) -lt $deadline)

docker compose ps
docker compose logs --tail 100 api web nginx
throw 'Smoke test failed: health, homepage, or CSP did not become ready within 3 minutes.'
