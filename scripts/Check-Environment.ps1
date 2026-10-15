$ErrorActionPreference = 'Continue'
Write-Host '=== OS ==='
Get-CimInstance Win32_OperatingSystem | Select-Object Caption,Version
Write-Host '=== .NET SDK ==='
if (Test-Path "$PSScriptRoot\..\.tools\dotnet\dotnet.exe") { & "$PSScriptRoot\..\.tools\dotnet\dotnet.exe" --list-sdks } else { dotnet --list-sdks }
Write-Host '=== Git / Java ==='
git --version
if (Get-Command java -ErrorAction SilentlyContinue) { java -version } else { Write-Warning 'Java 21+ missing from PATH.' }
Write-Host '=== IIS / Jenkins services ==='
Get-Service W3SVC,WAS,Jenkins -ErrorAction SilentlyContinue | Format-Table Name,Status
Write-Host '=== Hosting Bundle ==='
Write-Host (Test-Path "$env:ProgramFiles\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll")
Write-Host '=== Administrator ==='
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
Write-Host $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
