param([int]$Port = 8080, [switch]$Download)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$tools = Join-Path $root '.tools'
New-Item -ItemType Directory -Force -Path $tools | Out-Null
$war = Join-Path $tools 'jenkins.war'
$javaCommand = Get-Command java -ErrorAction SilentlyContinue
if ($javaCommand) { $javaExe = $javaCommand.Source }
else {
    $portableJava = Get-ChildItem (Join-Path $tools 'java') -Filter java.exe -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $portableJava) { throw 'Install Java 21+ (Temurin), reopen PowerShell, then run this script again.' }
    $javaExe = $portableJava.FullName
}
if ($Download -or -not (Test-Path -LiteralPath $war)) {
    $version = (Invoke-WebRequest 'https://updates.jenkins.io/stable/latestCore.txt' -UseBasicParsing).Content.Trim()
    $url = "https://get.jenkins.io/war-stable/$version/jenkins.war"
    Invoke-WebRequest $url -OutFile $war -UseBasicParsing
    $expected = ((Invoke-WebRequest "$url.sha256" -UseBasicParsing).Content.Trim() -split '\s+')[0]
    if ((Get-FileHash -LiteralPath $war -Algorithm SHA256).Hash -ne $expected) { throw 'Jenkins SHA256 verification failed.' }
}
$env:JENKINS_HOME = Join-Path $root '.local\jenkins'
$localSdk = Join-Path $tools 'dotnet'
if (Test-Path (Join-Path $localSdk 'dotnet.exe')) { $env:DOTNET_ROOT = $localSdk; $env:PATH = $localSdk + ';' + $env:PATH }
$env:WEBBANHANG_CI_ONLY = if (Get-Service W3SVC -ErrorAction SilentlyContinue) { 'false' } else { 'true' }
Write-Host "Jenkins: http://localhost:$Port (loopback only). Keep this terminal open."
Write-Host "Unlock password file: $env:JENKINS_HOME\secrets\initialAdminPassword"
if (Test-Path (Join-Path $root '.local\jenkins-login.json')) { Write-Host 'Existing demo login: .local\jenkins-login.json (keep private).' }
& $javaExe -jar $war "--httpPort=$Port" --httpListenAddress=127.0.0.1
if ($LASTEXITCODE -ne 0) { throw 'Jenkins stopped unexpectedly.' }
