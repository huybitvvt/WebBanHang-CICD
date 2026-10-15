param(
    [string]$Url = 'http://localhost/webbanhang/health',
    [string]$ExpectedVersion = '',
    [int]$Attempts = 20,
    [int]$DelaySeconds = 2
)
$ErrorActionPreference = 'Stop'
for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
    try {
        $health = Invoke-RestMethod -Uri $Url -TimeoutSec 10
        if ($health.status -ne 'Healthy' -or $health.application -ne 'WebBanHang') { throw 'Wrong application or unhealthy database.' }
        if ($ExpectedVersion -and $health.version -ne $ExpectedVersion) { throw "Wrong version: $($health.version) (expected $ExpectedVersion)." }
        $baseUrl = $Url -replace '/health/?$', ''
        $home = Invoke-WebRequest "$baseUrl/" -UseBasicParsing -TimeoutSec 10
        $css = Invoke-WebRequest "$baseUrl/css/site.css" -UseBasicParsing -TimeoutSec 10
        if ($home.StatusCode -ne 200 -or $home.Content -notmatch 'WebBanHang' -or $css.StatusCode -ne 200) { throw 'Homepage or CSS check failed.' }
        Write-Host "Verified $Url : $($health.status), version=$($health.version), homepage=200, CSS=200"
        return
    } catch {
        if ($attempt -eq $Attempts) { throw "Verification failed after $Attempts attempts: $_" }
        Start-Sleep -Seconds $DelaySeconds
    }
}
