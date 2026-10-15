Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:ProjectRoot = Split-Path -Parent $PSScriptRoot
function Get-DotNet {
    $localDotnet = Join-Path $script:ProjectRoot '.tools\dotnet\dotnet.exe'
    if (Test-Path -LiteralPath $localDotnet) { return $localDotnet }
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        $sdks = & dotnet --list-sdks
        if ($sdks -match '^8\.0\.') { return (Get-Command dotnet).Source }
    }
    throw 'Missing .NET 8 SDK. Run: powershell -ExecutionPolicy Bypass -File scripts\Install-DotNet.ps1'
}
function Invoke-DotNet {
    $exe = Get-DotNet
    & $exe @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" }
}
