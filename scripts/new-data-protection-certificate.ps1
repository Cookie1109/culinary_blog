#requires -Version 7.0
param([string]$OutputDirectory = 'tmp/data-protection')
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$target = [IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
if (-not $target.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Certificate output must stay inside the workspace.'
}
$privatePath = Join-Path $target 'certificate.base64'
$metadataPath = Join-Path $target 'certificate-metadata.json'
if ((Test-Path -LiteralPath $privatePath) -or (Test-Path -LiteralPath $metadataPath)) {
    throw 'Certificate output already exists. Preserve the existing secret; this tool does not rotate it.'
}
& git -C $repoRoot check-ignore -q -- $privatePath
if ($LASTEXITCODE -ne 0) { throw 'Private certificate output must be Git-ignored.' }
[IO.Directory]::CreateDirectory($target) | Out-Null
$rsa = [Security.Cryptography.RSA]::Create(3072)
$certificate = $null
try {
    $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new(
        'CN=CulinaryBlog Data Protection', $rsa,
        [Security.Cryptography.HashAlgorithmName]::SHA256,
        [Security.Cryptography.RSASignaturePadding]::Pkcs1)
    $certificate = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddMinutes(-5), [DateTimeOffset]::UtcNow.AddYears(2))
    New-Item -ItemType File -Path $privatePath -ErrorAction Stop | Out-Null
    $acl = [Security.AccessControl.FileSecurity]::new()
    $acl.SetAccessRuleProtection($true, $false)
    $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
        [Security.Principal.WindowsIdentity]::GetCurrent().User,
        [Security.AccessControl.FileSystemRights]::FullControl,
        [Security.AccessControl.AccessControlType]::Allow))
    Set-Acl -LiteralPath $privatePath -AclObject $acl
    [IO.File]::WriteAllText($privatePath,
        [Convert]::ToBase64String($certificate.Export([Security.Cryptography.X509Certificates.X509ContentType]::Pfx)),
        [Text.UTF8Encoding]::new($false))
    $metadata = [ordered]@{
        certificateSha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($certificate.RawData))
        notBeforeUtc = $certificate.NotBefore.ToUniversalTime().ToString('o')
        notAfterUtc = $certificate.NotAfter.ToUniversalTime().ToString('o')
        rsaBits = 3072
        privateFile = $privatePath
        purpose = 'Data Protection key encryption; not an HTTPS certificate'
    }
    [IO.File]::WriteAllText($metadataPath, ($metadata | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
    Write-Output 'Certificate generated locally; private value is not printed.'
    Write-Output "Private file for secret configuration: $privatePath"
    Write-Output "Metadata: $metadataPath"
}
finally {
    if ($certificate) { $certificate.Dispose() }
    $rsa.Dispose()
}
