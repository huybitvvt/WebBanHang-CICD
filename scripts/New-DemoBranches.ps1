param([switch]$Push)
. "$PSScriptRoot\Common.ps1"
Set-Location $script:ProjectRoot
function Git-Checked {
    & git @args
    if ($LASTEXITCODE -ne 0) { throw "git failed: $args" }
}
foreach ($scenario in @('build-failure', 'test-failure')) {
    $branch = "demo/$scenario"
    & git show-ref --verify --quiet "refs/heads/$branch"
    if ($LASTEXITCODE -eq 0) { Write-Host "$branch already exists; preserving it."; continue }
    $worktree = Join-Path $script:ProjectRoot ".local\scenarios\$scenario"
    Git-Checked worktree add -b $branch $worktree main
    if ($scenario -eq 'build-failure') {
        $file = Join-Path $worktree 'src\WebBanHang\Services\OrderCalculator.cs'
        Add-Content -LiteralPath $file -Value "`nTHIS_IS_AN_INTENTIONAL_CSHARP_SYNTAX_ERROR;" -Encoding UTF8
    } else {
        $file = Join-Path $worktree 'src\WebBanHang\Services\OrderCalculator.cs'
        $original = [IO.File]::ReadAllText($file)
        $broken = $original.Replace('(1 - discountPercent / 100m)', '(1 + discountPercent / 100m)')
        if ($broken -eq $original) { throw 'Calculator changed; update the scenario fixture before continuing.' }
        [IO.File]::WriteAllText($file, $broken, (New-Object Text.UTF8Encoding($false)))
    }
    Git-Checked -C $worktree add src/WebBanHang/Services/OrderCalculator.cs
    Git-Checked -C $worktree commit -m "demo: intentional $scenario (do not merge)"
}
if ($Push) { Git-Checked push origin demo/build-failure demo/test-failure }
Write-Host 'Failure branches are isolated. Never merge them into main.'
