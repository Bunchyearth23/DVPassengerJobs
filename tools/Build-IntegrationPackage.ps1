param(
    [string]$Configuration = 'Release',
    [string]$RuntimeSeedDirectory = 'D:\Steam\steamapps\common\Derail Valley\Mods\PassengerJobs',
    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $root 'artifacts\PassengerJobs' }
$output = [IO.Path]::GetFullPath($OutputDirectory)
$stage = Join-Path $output 'stage'
$packageRoot = Join-Path $stage 'PassengerJobs'
$zip = Join-Path $output 'PassengerJobs-5.3.0-bdvm-integration.zip'

& dotnet build (Join-Path $root 'PassengerJobs.sln') -c $Configuration
if ($LASTEXITCODE -ne 0) { throw 'PassengerJobs solution build failed.' }
& (Join-Path $root "PassengerJobs.Tests\bin\$Configuration\netframework4.8\PassengerJobs.Tests.exe")
if ($LASTEXITCODE -ne 0) { throw 'PassengerJobs policy tests failed.' }

if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null

$sources = [ordered]@{
    'PassengerJobs.dll' = Join-Path $root "PassengerJobs\bin\$Configuration\netframework4.8\PassengerJobs.dll"
    'PassengerJobs.API.dll' = Join-Path $root "PassengerJobs.API\bin\$Configuration\netframework4.8\PassengerJobs.API.dll"
    'PassengerJobs.MP.dll' = Join-Path $root "PassengerJobs.MP\bin\$Configuration\netframework4.8\PassengerJobs.MP.dll"
    'PassengerJobs.Skins.dll' = Join-Path $root "PassengerJobs.Skins\bin\$Configuration\netframework4.8\PassengerJobs.Skins.dll"
    'Info.json' = Join-Path $root 'Info.json'
    'default_routes.json' = Join-Path $root 'PassengerJobs\default_routes.json'
    'default_stations.json' = Join-Path $root 'PassengerJobs\default_stations.json'
    'platform_signs.csv' = Join-Path $root 'PassengerJobs\platform_signs.csv'
    'LICENSE' = Join-Path $root 'LICENSE'
    'README.md' = Join-Path $root 'README.md'
    'VALIDATION.md' = Join-Path $root 'VALIDATION.md'
    'passengerjobs' = Join-Path $RuntimeSeedDirectory 'passengerjobs'
    'translations.csv' = Join-Path $RuntimeSeedDirectory 'translations.csv'
}

foreach ($entry in $sources.GetEnumerator()) {
    if (-not (Test-Path -LiteralPath $entry.Value -PathType Leaf)) { throw "Missing package input: $($entry.Value)" }
    Copy-Item -LiteralPath $entry.Value -Destination (Join-Path $packageRoot $entry.Key)
}

$commit = (& git -C $root rev-parse HEAD).Trim()
$files = @(Get-ChildItem -LiteralPath $packageRoot -File | Sort-Object Name | ForEach-Object {
    [ordered]@{ path = $_.Name; length = $_.Length; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
})
$manifest = [ordered]@{
    schemaVersion = 1
    package = 'PassengerJobs'
    version = '5.3.0'
    apiVersion = '1.1'
    branch = 'bdvm-integration'
    commit = $commit
    upstream = 'https://github.com/katycat5e/DVPassengerJobs'
    fork = 'https://github.com/Bunchyearth23/DVPassengerJobs'
    upstreamBaseCommit = '9bb668cbc2f3d270d282b2b3297667f01bec3e18'
    license = 'MIT'
    runtimeSeed = [ordered]@{ source = $RuntimeSeedDirectory; purpose = 'upstream Unity asset bundle and translation catalog only' }
    files = $files
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $packageRoot 'package-manifest.json') -Encoding UTF8

Add-Type -AssemblyName System.IO.Compression
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
$stream = [IO.File]::Open($zip, [IO.FileMode]::CreateNew)
$archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem -LiteralPath $packageRoot -File | Sort-Object Name) {
        $entry = $archive.CreateEntry("PassengerJobs/$($file.Name)", [IO.Compression.CompressionLevel]::Optimal)
        $entry.LastWriteTime = [DateTimeOffset]::new(2026, 9, 8, 0, 0, 0, [TimeSpan]::Zero)
        $input = [IO.File]::OpenRead($file.FullName)
        $destination = $entry.Open()
        try { $input.CopyTo($destination) } finally { $destination.Dispose(); $input.Dispose() }
    }
} finally { $archive.Dispose(); $stream.Dispose() }

$zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Output "Package: $zip"
Write-Output "SHA-256: $zipHash"
Write-Output "Commit: $commit"
