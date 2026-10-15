. "$PSScriptRoot\Common.ps1"
Set-Location $script:ProjectRoot
Invoke-DotNet restore WebBanHang.sln --locked-mode
Invoke-DotNet build WebBanHang.sln -c Release --no-restore
Invoke-DotNet test WebBanHang.sln -c Release --no-build --logger 'trx;LogFileName=tests.trx' --results-directory artifacts/TestResults
Invoke-DotNet publish src/WebBanHang/WebBanHang.csproj -c Release --no-build -o artifacts/publish
Write-Host 'PASS: Restore, Build, Test, Publish. IIS deployment has not been run by this script.'
