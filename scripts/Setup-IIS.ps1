param(
    [string]$DeployPath = 'C:\wwwroot\webbanhang',
    [string]$DataPath = 'C:\ProgramData\WebBanHang',
    [string]$JenkinsAccount = '',
    [switch]$EnableIIS
)
$ErrorActionPreference = 'Stop'
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Run Windows PowerShell as Administrator on the IIS machine.' }
if ($EnableIIS) {
    foreach ($feature in @('IIS-WebServerRole','IIS-WebServer','IIS-ManagementConsole','IIS-ManagementScriptingTools')) {
        Enable-WindowsOptionalFeature -Online -FeatureName $feature -All -NoRestart | Out-Null
    }
}
Import-Module WebAdministration -ErrorAction Stop
if (-not (Test-Path "$env:ProgramFiles\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll")) { throw 'Install the ASP.NET Core .NET 8 Hosting Bundle AFTER IIS (or repair it), then restart WAS/W3SVC.' }
if (-not (Test-Path 'IIS:\Sites\Default Web Site')) { throw 'Default Web Site does not exist. Create it with a localhost HTTP binding on port 80.' }
New-Item -ItemType Directory -Force -Path $DeployPath,$DataPath | Out-Null
if (-not (Test-Path 'IIS:\AppPools\WebBanHangPool')) { New-WebAppPool 'WebBanHangPool' | Out-Null }
Set-ItemProperty 'IIS:\AppPools\WebBanHangPool' -Name managedRuntimeVersion -Value ''
Set-ItemProperty 'IIS:\AppPools\WebBanHangPool' -Name enable32BitAppOnWin64 -Value $false
if (Get-WebApplication -Site 'Default Web Site' -Name 'webbanhang') {
    Set-ItemProperty 'IIS:\Sites\Default Web Site\webbanhang' -Name physicalPath -Value $DeployPath
    Set-ItemProperty 'IIS:\Sites\Default Web Site\webbanhang' -Name applicationPool -Value 'WebBanHangPool'
} else {
    New-WebApplication -Site 'Default Web Site' -Name 'webbanhang' -PhysicalPath $DeployPath -ApplicationPool 'WebBanHangPool' | Out-Null
}
function Grant-Folder([string]$Path, [string]$Account, [string]$Access) {
    & icacls.exe $Path /grant "${Account}:(OI)(CI)${Access}" | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Could not grant $Access to $Account on $Path" }
}
Grant-Folder $DeployPath 'IIS AppPool\WebBanHangPool' 'RX'
Grant-Folder $DataPath 'IIS AppPool\WebBanHangPool' 'M'
$backupPath = Join-Path (Split-Path -Parent $DeployPath) 'webbanhang-backups'
New-Item -ItemType Directory -Force -Path $backupPath | Out-Null
if ($JenkinsAccount) { Grant-Folder $DeployPath $JenkinsAccount 'M'; Grant-Folder $backupPath $JenkinsAccount 'M' }
$productionConfig = Join-Path $DeployPath 'appsettings.Production.json'
if (-not (Test-Path -LiteralPath $productionConfig)) {
    @{ DataDirectory = $DataPath; PathBase = ''; AllowedHosts = 'localhost;127.0.0.1;[::1]' } |
        ConvertTo-Json | Set-Content -LiteralPath $productionConfig -Encoding UTF8
}
Start-WebAppPool 'WebBanHangPool'
Start-Website 'Default Web Site'
Write-Host 'IIS configured. Publish/deploy with Jenkins, then open http://localhost/webbanhang'
Write-Host 'Jenkins needs Modify only on deployment and backup directories; app_offline avoids IIS administrator permissions per deployment.'
