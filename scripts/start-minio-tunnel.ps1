param(
    [string]$MinioContainer = 'culinary-blog-minio-1',
    [string]$BucketName = 'culinary-media',
    [string]$CloudflaredPath,
    [string]$GatewayImage = 'nginx:1.28.0-alpine@sha256:30f1c0d78e0ad60901648be663a710bdadf19e4c10ac6782c235200619158284'
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskDir = Join-Path $taskRoot 'tmp/minio-tunnel'
[IO.Directory]::CreateDirectory($taskDir) | Out-Null
function Write-PrivateFile($Path, $Value) {
    [IO.File]::WriteAllText($Path, $Value, (New-Object Text.UTF8Encoding($false)))
}
function Invoke-Docker([string[]]$DockerArguments, [string]$InputText) {
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue' # Docker writes progress to stderr in Windows PowerShell 5.
    try {
        if ($InputText) {
            $native = New-Object Diagnostics.Process
            $native.StartInfo = New-Object Diagnostics.ProcessStartInfo
            $native.StartInfo.FileName = 'docker'
            $native.StartInfo.Arguments = $DockerArguments -join ' '
            $native.StartInfo.UseShellExecute = $false
            $native.StartInfo.CreateNoWindow = $true
            $native.StartInfo.RedirectStandardInput = $true
            $native.StartInfo.RedirectStandardOutput = $true
            $native.StartInfo.RedirectStandardError = $true
            $native.Start() | Out-Null
            $native.StandardInput.Write($InputText.Replace("`r`n", "`n") + "`n")
            $native.StandardInput.Close()
            $result = @($native.StandardOutput.ReadToEnd(), $native.StandardError.ReadToEnd())
            $native.WaitForExit(); $code = $native.ExitCode; $native.Dispose()
        }
        else { $result = @(& docker @DockerArguments 2>&1) }
        if (-not $InputText) { $code = $LASTEXITCODE }
    }
    finally { $ErrorActionPreference = $previousPreference }
    if ($code -ne 0) { throw "Docker $($DockerArguments[0]) failed; details withheld to keep credentials out of logs." }
    return ($result -join "`n")
}
if ($BucketName -notmatch '^[a-z0-9][a-z0-9.-]{1,61}[a-z0-9]$') { throw 'Invalid bucket name.' }
if ($MinioContainer -notmatch '^[a-zA-Z0-9][a-zA-Z0-9_.-]*$') { throw 'Invalid container name.' }
$statePath = Join-Path $taskDir 'session.json'
if (Test-Path -LiteralPath $statePath) { throw 'A session exists. Run scripts/stop-minio-tunnel.ps1 first.' }
if (-not $CloudflaredPath) {
    $installed = Get-Command cloudflared -ErrorAction SilentlyContinue
    if ($installed) { $CloudflaredPath = $installed.Source }
    else { $CloudflaredPath = Join-Path $taskRoot 'tmp/g04/preview/tools/cloudflared.exe' }
}
if (-not (Test-Path -LiteralPath $CloudflaredPath)) {
    throw 'cloudflared is missing. Install a portable official binary with user approval, then pass -CloudflaredPath.'
}
$CloudflaredPath = (Resolve-Path -LiteralPath $CloudflaredPath).Path
$minio = (Invoke-Docker @('inspect', $MinioContainer) | ConvertFrom-Json)[0]
if (-not $minio.State.Running) {
    Invoke-Docker @('start', $MinioContainer) | Out-Null
    Start-Sleep -Seconds 3
    $minio = (Invoke-Docker @('inspect', $MinioContainer) | ConvertFrom-Json)[0]
}
if (-not @($minio.Mounts | Where-Object { $_.Destination -eq '/data' -and $_.Type -eq 'volume' }).Count) {
    throw 'Expected an existing persistent named volume at /data; no container or bucket will be created.'
}
$apiPort = $minio.NetworkSettings.Ports.'9000/tcp'[0].HostPort
if (-not $apiPort) { throw 'MinIO API port 9000 is not published. Do not use Console port 9001.' }
$health = Invoke-WebRequest "http://127.0.0.1:$apiPort/minio/health/live" -UseBasicParsing -TimeoutSec 10
if ($health.StatusCode -ne 200) { throw 'MinIO API is not healthy.' }
$credentialsPath = Join-Path $taskDir 'render-minio.env'
$values = @{}
if (Test-Path -LiteralPath $credentialsPath) {
    foreach ($line in Get-Content -LiteralPath $credentialsPath) {
        if ($line -match '^([^#=]+)=(.*)$') { $values[$matches[1]] = $matches[2] }
    }
}
if ($values['ObjectStorage__BucketName'] -and $values['ObjectStorage__BucketName'] -ne $BucketName) { throw 'Existing demo credentials belong to a different bucket.' }
if (-not $values['ObjectStorage__AccessKey']) {
    $values['ObjectStorage__AccessKey'] = 'demo' + [Guid]::NewGuid().ToString('N').Substring(0, 20)
    $random = New-Object byte[] 32
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($random); $rng.Dispose()
    $values['ObjectStorage__SecretKey'] = [Convert]::ToBase64String($random)
}
$access = $values['ObjectStorage__AccessKey']; $secret = $values['ObjectStorage__SecretKey']
if ($access -notmatch '^demo[a-f0-9]{20}$' -or $secret -notmatch '^[A-Za-z0-9+/=]{43,44}$') { throw 'Invalid private demo credentials format.' }
Write-PrivateFile $credentialsPath "ObjectStorage__Endpoint=`nObjectStorage__AccessKey=$access`nObjectStorage__SecretKey=$secret`nObjectStorage__BucketName=$BucketName`n"
$policyName = 'culinary-tunnel-' + $access.Substring(4)
$policy = @{
    Version = '2012-10-17'; Statement = @(
        @{ Effect = 'Allow'; Action = @('s3:GetBucketLocation', 's3:ListBucket'); Resource = @("arn:aws:s3:::$BucketName") },
        @{ Effect = 'Allow'; Action = @('s3:GetObject', 's3:PutObject', 's3:DeleteObject'); Resource = @("arn:aws:s3:::$BucketName/*") }
    )
} | ConvertTo-Json -Depth 6 -Compress
# Only stdin carries the new secret. Root credentials are expanded inside the local container.
$setup = @'
set -eu
export MC_CONFIG_DIR=/tmp/culinary-tunnel-setup
trap 'rm -rf /tmp/culinary-tunnel-setup' EXIT
mc alias set local http://127.0.0.1:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" >/dev/null 2>&1
mc stat local/__BUCKET__ >/dev/null 2>&1
mc --json anonymous get local/__BUCKET__
cat > "$MC_CONFIG_DIR/policy.json" <<'POLICY'
__POLICY__
POLICY
mc admin policy create local __POLICY_NAME__ "$MC_CONFIG_DIR/policy.json" >/dev/null 2>&1
mc admin user info local __ACCESS__ >/dev/null 2>&1 || mc admin user add local __ACCESS__ '__SECRET__' >/dev/null 2>&1
mc admin policy attach local __POLICY_NAME__ --user __ACCESS__ >/dev/null 2>&1
mc alias set client http://127.0.0.1:9000 __ACCESS__ '__SECRET__' >/dev/null 2>&1
mc stat client/__BUCKET__ >/dev/null 2>&1
'@
$setup = $setup.Replace('__BUCKET__', $BucketName).Replace('__POLICY__', $policy).Replace('__POLICY_NAME__', $policyName).Replace('__ACCESS__', $access).Replace('__SECRET__', $secret)
$permission = Invoke-Docker @('exec', '-i', $MinioContainer, 'sh', '-s') ($setup.Replace("`r`n", "`n")) | ConvertFrom-Json
if ($permission.access -ne 'private' -and $permission.permission -ne 'private') { throw 'Bucket must already be private; public tunnel not started.' }
$listener = New-Object Net.Sockets.TcpListener([Net.IPAddress]::Loopback, 0)
$listener.Start(); $gatewayPort = $listener.LocalEndpoint.Port; $listener.Stop()
$session = [Guid]::NewGuid().ToString('N').Substring(0, 12)
$gateway = "culinary-minio-tunnel-$session"
$configPath = Join-Path $taskDir 'gateway.conf'
$template = [IO.File]::ReadAllText((Join-Path $taskRoot 'infra/demo/minio-tunnel.nginx.conf.template'))
Write-PrivateFile $configPath ($template.Replace('__ACCESS_KEY__', $access).Replace('__BUCKET__', [Regex]::Escape($BucketName)).Replace('__API_PORT__', $apiPort))
$process = $null
try {
    Invoke-Docker @('run', '-d', '--name', $gateway, '--label', "culinary.minio-tunnel=$session", '--read-only', '--user', '101:101', '--cap-drop', 'ALL', '--security-opt', 'no-new-privileges', '--tmpfs', '/tmp:rw,noexec,nosuid,size=16m', '-p', "127.0.0.1:${gatewayPort}:8080", '--mount', "type=bind,source=$configPath,target=/etc/nginx/nginx.conf,readonly", '--entrypoint', 'nginx', $GatewayImage, '-g', 'daemon off;') | Out-Null
    $gatewayReady = $false
    for ($attempt = 0; $attempt -lt 10; $attempt++) {
        try {
            $check = Invoke-WebRequest "http://127.0.0.1:$gatewayPort/minio/health/live" -UseBasicParsing -TimeoutSec 2
            if ($check.StatusCode -eq 200) { $gatewayReady = $true; break }
        }
        catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $gatewayReady) { throw 'Gateway health failed; no public tunnel started.' }
    $stdout = Join-Path $taskDir 'cloudflared.out.log'; $stderr = Join-Path $taskDir 'cloudflared.err.log'
    $process = Start-Process -FilePath $CloudflaredPath -ArgumentList @('tunnel', '--no-autoupdate', '--protocol', 'http2', '--loglevel', 'info', '--metrics', '127.0.0.1:0', '--url', "http://127.0.0.1:$gatewayPort") -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru
    $url = $null
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        Start-Sleep -Seconds 1
        if ($process.HasExited) { throw 'cloudflared stopped; see its info-level log.' }
        $log = Get-Content -LiteralPath $stderr -Raw
        if ($log -match 'https://[a-z0-9-]+\.trycloudflare\.com') { $url = $matches[0]; break }
    }
    if (-not $url) { throw 'Timed out waiting for Quick Tunnel URL.' }
    $state = @{ session = $session; gateway = $gateway; cloudflaredPid = $process.Id; cloudflaredPath = $CloudflaredPath; processStartTicks = $process.StartTime.ToUniversalTime().Ticks.ToString(); endpoint = $url; apiPort = $apiPort; gatewayPort = $gatewayPort; container = $MinioContainer; bucket = $BucketName }
    Write-PrivateFile $statePath ($state | ConvertTo-Json)
    Write-PrivateFile $credentialsPath "ObjectStorage__Endpoint=$url`nObjectStorage__AccessKey=$access`nObjectStorage__SecretKey=$secret`nObjectStorage__BucketName=$BucketName`n"
    Write-Output "Quick Tunnel: $url"
    Write-Output 'Private Render variables: tmp/minio-tunnel/render-minio.env (do not commit or paste into chat).'
    Write-Output 'Stop with: powershell -NoProfile -File scripts/stop-minio-tunnel.ps1'
}
catch {
    if ($process -and -not $process.HasExited) { Stop-Process -Id $process.Id }
    Invoke-Docker @('rm', '-f', $gateway) | Out-Null
    throw
}
