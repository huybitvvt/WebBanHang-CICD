$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$tools = Join-Path $root '.tools'
New-Item -ItemType Directory -Force -Path $tools | Out-Null
$installer = Join-Path $tools 'dotnet-install.ps1'
Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer -UseBasicParsing
& $installer -Channel 8.0 -InstallDir (Join-Path $tools 'dotnet') -NoPath
if (-not (Test-Path (Join-Path $tools 'dotnet\dotnet.exe'))) { throw 'SDK installation failed.' }
