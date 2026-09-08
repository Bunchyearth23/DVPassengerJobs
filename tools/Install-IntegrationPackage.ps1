param(
    [string]$PackageStage = '',
    [string]$GameDirectory = 'D:\Steam\steamapps\common\Derail Valley'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($PackageStage)) { $PackageStage = Join-Path $root 'artifacts\PassengerJobs\stage\PassengerJobs' }
$source = [IO.Path]::GetFullPath($PackageStage)
$target = [IO.Path]::GetFullPath((Join-Path $GameDirectory 'Mods\PassengerJobs'))
$backupRoot = [IO.Path]::GetFullPath((Join-Path $root ('artifacts\runtime-backups\PassengerJobs-before-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))))

if (Get-Process -Name DerailValley -ErrorAction SilentlyContinue) { throw 'Derail Valley is running; deployment refused.' }
if (-not (Test-Path -LiteralPath (Join-Path $source 'package-manifest.json'))) { throw 'Package manifest is missing; build the integration package first.' }
$manifest = Get-Content -LiteralPath (Join-Path $source 'package-manifest.json') -Raw | ConvertFrom-Json

New-Item -ItemType Directory -Path $backupRoot, $target -Force | Out-Null
foreach ($file in $manifest.files) {
    $sourceFile = Join-Path $source $file.path
    if ((Get-FileHash -LiteralPath $sourceFile -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sha256) { throw "Package hash mismatch: $($file.path)" }
    $targetFile = Join-Path $target $file.path
    if (Test-Path -LiteralPath $targetFile) {
        Copy-Item -LiteralPath $targetFile -Destination (Join-Path $backupRoot $file.path)
    }
}
Copy-Item -LiteralPath (Join-Path $source 'package-manifest.json') -Destination (Join-Path $backupRoot 'candidate-package-manifest.json')

foreach ($file in $manifest.files) {
    $sourceFile = Join-Path $source $file.path
    $targetFile = Join-Path $target $file.path
    Copy-Item -LiteralPath $sourceFile -Destination $targetFile -Force
    if ((Get-FileHash -LiteralPath $targetFile -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sha256) { throw "Installed hash mismatch: $($file.path)" }
}

Write-Output "Backup: $backupRoot"
Write-Output "Installed PassengerJobs candidate commit $($manifest.commit). Derail Valley was not launched."
