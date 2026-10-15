param([int]$Port = 5080)
. "$PSScriptRoot\Common.ps1"
Set-Location $script:ProjectRoot
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:PathBase = '/webbanhang'
Write-Host "Open http://localhost:$Port/webbanhang (Kestrel local rehearsal; IIS is separate)."
Invoke-DotNet run --project src/WebBanHang --urls "http://localhost:$Port"
