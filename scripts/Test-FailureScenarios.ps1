param([string]$BaselineUrl = '')
. "$PSScriptRoot\Common.ps1"
Set-Location $script:ProjectRoot
$dotnet = Get-DotNet
$baseline = if ($BaselineUrl) { (Invoke-RestMethod $BaselineUrl).version } else { '' }
foreach ($scenario in @('build-failure', 'test-failure')) {
    $worktree = Join-Path $script:ProjectRoot ".local\scenarios\$scenario"
    if (-not (Test-Path "$worktree\WebBanHang.sln")) { throw 'Run New-DemoBranches.ps1 first.' }
    Push-Location $worktree
    try {
        & $dotnet restore WebBanHang.sln --locked-mode
        if ($LASTEXITCODE -ne 0) { throw 'Restore failed; this does not prove the intended scenario.' }
        & $dotnet build WebBanHang.sln -c Release --no-restore
        $buildExit = $LASTEXITCODE
        if ($scenario -eq 'build-failure') {
            if ($buildExit -eq 0) { throw 'Expected Build to fail, but it passed.' }
            Write-Host 'PASS: intentional C# syntax error stopped at Build.'
        } else {
            if ($buildExit -ne 0) { throw 'Build must pass before testing the failing calculation.' }
            & $dotnet test WebBanHang.sln -c Release --no-build --logger 'trx;LogFileName=intentional-failure.trx' --results-directory (Join-Path $script:ProjectRoot "artifacts\$scenario")
            if ($LASTEXITCODE -eq 0) { throw 'Expected calculation tests to fail, but they passed.' }
            Write-Host 'PASS: incorrect discount formula stopped at Test.'
        }
        if ($BaselineUrl) {
            $after = Invoke-RestMethod $BaselineUrl
            if ($after.status -ne 'Healthy' -or $after.version -ne $baseline) { throw 'Baseline release changed or is unhealthy.' }
            Write-Host "Baseline still healthy, version=$baseline"
        }
    } finally { Pop-Location }
}
$global:LASTEXITCODE = 0
Write-Host 'Both negative scenarios verified locally. Capture separate Jenkins/IIS evidence on the target server.'
