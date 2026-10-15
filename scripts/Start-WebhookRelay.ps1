param([string]$Repository = 'huybitvvt/WebBanHang-CICD')
$ErrorActionPreference = 'Stop'
if (-not (Get-Command node -ErrorAction SilentlyContinue)) { throw 'Install Node.js 22 LTS or newer.' }
$secureSecret = Read-Host 'GitHub webhook secret (32+ characters; same value in GitHub)' -AsSecureString
$pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureSecret)
$previousSecret = $env:WEBHOOK_SECRET
$previousRepository = $env:GITHUB_REPOSITORY
try {
    $env:WEBHOOK_SECRET = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    $env:GITHUB_REPOSITORY = $Repository
    & node "$PSScriptRoot\webhook-relay.mjs"
    if ($LASTEXITCODE -ne 0) { throw 'Webhook relay stopped with an error.' }
} finally {
    $env:WEBHOOK_SECRET = $previousSecret
    $env:GITHUB_REPOSITORY = $previousRepository
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
}
