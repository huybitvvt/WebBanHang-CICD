param([string]$Repository = 'huybitvvt/WebBanHang-CICD')
$ErrorActionPreference = 'Stop'
foreach ($command in @('node','ngrok','gh')) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { throw "Missing $command. Install/configure it before starting the webhook demo." }
}
& node "$PSScriptRoot\start-webhook-demo.mjs" $Repository
if ($LASTEXITCODE -ne 0) { throw 'Webhook demo failed. Check ngrok authentication and GitHub permissions.' }
