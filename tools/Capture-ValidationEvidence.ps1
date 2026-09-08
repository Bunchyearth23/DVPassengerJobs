param(
    [ValidateSet('Start','Finish')][string]$Mode,
    [string]$GameDirectory = 'D:\Steam\steamapps\common\Derail Valley',
    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $root 'artifacts\validation' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$sessionFile = Join-Path $OutputDirectory 'active-session.json'
$log = Join-Path $env:USERPROFILE 'AppData\LocalLow\Altfuture\Derail Valley\Player.log'
$installed = Join-Path $GameDirectory 'Mods\PassengerJobs'

if ($Mode -eq 'Start') {
    if (Get-Process -Name DerailValley -ErrorAction SilentlyContinue) { throw 'Start capture before launching Derail Valley.' }
    $session = [ordered]@{
        schemaVersion = 1
        sessionId = [Guid]::NewGuid().ToString('N')
        startedUtc = [DateTime]::UtcNow.ToString('o')
        playerLogPath = $log
        playerLogLength = if (Test-Path -LiteralPath $log) { (Get-Item -LiteralPath $log).Length } else { 0 }
        installed = @(Get-ChildItem -LiteralPath $installed -File -ErrorAction Stop | Where-Object Name -Match '^PassengerJobs(\.API|\.MP|\.Skins)?\.dll$' | Sort-Object Name | ForEach-Object {
            [ordered]@{ name = $_.Name; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
        })
    }
    $session | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $sessionFile -Encoding UTF8
    Write-Output "Validation capture armed: $($session.sessionId)"
    exit 0
}

if (Get-Process -Name DerailValley -ErrorAction SilentlyContinue) { throw 'Quit Derail Valley before finishing capture.' }
if (-not (Test-Path -LiteralPath $sessionFile)) { throw 'No active validation capture found.' }
$session = Get-Content -LiteralPath $sessionFile -Raw | ConvertFrom-Json
$destination = Join-Path $OutputDirectory ("PassengerJobs-validation-$($session.sessionId).log")
if (Test-Path -LiteralPath $log) {
    $bytes = [IO.File]::ReadAllBytes($log)
    $offset = [Math]::Min([long]$session.playerLogLength, [long]$bytes.Length)
    $text = [Text.Encoding]::UTF8.GetString($bytes, [int]$offset, $bytes.Length - [int]$offset)
    $selected = $text -split "`r?`n" | Where-Object { $_ -match 'PassengerJobs|BDVM|Multiplayer API|SkinManager|DVLangHelper' }
    [IO.File]::WriteAllLines($destination, $selected, [Text.UTF8Encoding]::new($false))
} else { [IO.File]::WriteAllText($destination, 'Player.log not found.', [Text.UTF8Encoding]::new($false)) }

$summary = [ordered]@{
    schemaVersion = 1
    sessionId = $session.sessionId
    startedUtc = $session.startedUtc
    finishedUtc = [DateTime]::UtcNow.ToString('o')
    evidenceLog = $destination
    installed = $session.installed
    manualObservations = [ordered]@{ walletBefore = $null; walletAfter = $null; jobId = $null; eventIds = @(); consistGuids = @(); role = $null; result = 'pending-review' }
}
$summaryPath = Join-Path $OutputDirectory ("PassengerJobs-validation-$($session.sessionId).json")
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $summaryPath -Encoding UTF8
Remove-Item -LiteralPath $sessionFile
Write-Output "Evidence: $destination"
Write-Output "Summary to complete: $summaryPath"
