param(
    [Parameter(Mandatory = $true)][string]$PublishPath,
    [string]$DeployPath = 'C:\wwwroot\webbanhang',
    [string]$VerifyUrl = 'http://localhost/webbanhang/health',
    [string]$ExpectedVersion = '',
    [ValidateRange(1, 60)][int]$VerifyAttempts = 20
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$source = (Resolve-Path -LiteralPath $PublishPath).Path
$target = [IO.Path]::GetFullPath($DeployPath).TrimEnd('\')
if ((Split-Path -Leaf $target) -ne 'webbanhang') { throw 'DeployPath must end in a dedicated webbanhang directory.' }
if ($source -eq $target -or $source.StartsWith($target + '\', [StringComparison]::OrdinalIgnoreCase) -or $target.StartsWith($source + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Source and deployment directories must be separate.' }
if (-not (Test-Path -LiteralPath (Join-Path $source 'WebBanHang.dll')) -or -not (Test-Path -LiteralPath (Join-Path $source 'web.config'))) { throw 'Invalid ASP.NET Core publish output.' }
New-Item -ItemType Directory -Force -Path $target | Out-Null
if ((Get-Item -LiteralPath $target).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Deployment directory cannot be a junction or symlink.' }
$backup = Join-Path (Split-Path -Parent $target) ('webbanhang-backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Force -Path $backup | Out-Null
$offline = Join-Path $target 'app_offline.htm'
if (Test-Path -LiteralPath $offline) { throw 'An existing app_offline.htm indicates maintenance or an unfinished deployment. Inspect it before retrying.' }
$hadRelease = Test-Path -LiteralPath (Join-Path $target 'WebBanHang.dll')
$reservedDirectories = @('App_Data', 'logs')
$reservedFiles = @('appsettings.Production.json', 'app_offline.htm')
function Copy-Release([string]$From, [string]$To, [switch]$Mirror) {
    $copyArguments = @($From, $To, '/E', '/IS', '/IT', '/R:10', '/W:2', '/NP', '/NFL', '/NDL', '/XD') + $reservedDirectories + @('/XF') + $reservedFiles
    if ($Mirror) { $copyArguments += '/PURGE' }
    & robocopy.exe @copyArguments | Out-Host
    if ($LASTEXITCODE -ge 8) { throw "Robocopy failed: $LASTEXITCODE" }
    $global:LASTEXITCODE = 0
}
try {
    Copy-Release $target $backup
    Set-Content -LiteralPath $offline -Value '<html><meta charset="utf-8"><p>Đang cập nhật ứng dụng. Vui lòng quay lại sau.</p></html>' -Encoding UTF8
    Start-Sleep -Seconds 3
    Copy-Release $source $target -Mirror
    Remove-Item -LiteralPath $offline -Force
    & "$PSScriptRoot\Verify-Deployment.ps1" -Url $VerifyUrl -ExpectedVersion $ExpectedVersion -Attempts $VerifyAttempts
    Write-Host "Deployment succeeded. Previous release backup: $backup"
} catch {
    $failure = $_
    Write-Warning "Deployment failed: $failure"
    if ($hadRelease) {
        try {
            Set-Content -LiteralPath $offline -Value 'Restoring previous release.'
            Start-Sleep -Seconds 3
            Copy-Release $backup $target -Mirror
            Remove-Item -LiteralPath $offline -Force
            & "$PSScriptRoot\Verify-Deployment.ps1" -Url $VerifyUrl -Attempts 10
            Write-Warning 'Previous release restored and health checked.'
        } catch { throw "Deployment failed: $failure. Rollback also failed: $_. Backup: $backup. Check app_offline.htm manually." }
    } else { Write-Warning 'First deployment failed. app_offline.htm is retained when copy fails; there is no previous release.' }
    throw $failure
}
