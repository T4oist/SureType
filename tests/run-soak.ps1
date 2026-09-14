param(
    [Parameter(Mandatory = $true)][string]$TestExecutable,
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [double]$Minutes = 480
)
$ErrorActionPreference = "Stop"
$testPath = [IO.Path]::GetFullPath($TestExecutable)
$logDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
$machine = [pscustomobject]@{
    os = (Get-CimInstance Win32_OperatingSystem | Select-Object Caption,Version,BuildNumber)
    cpu = (Get-CimInstance Win32_Processor | Select-Object Name,NumberOfCores,NumberOfLogicalProcessors)
    ramBytes = (Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory
    testExecutableSha256 = (Get-FileHash -LiteralPath $testPath -Algorithm SHA256).Hash
    startedAt = [DateTimeOffset]::Now.ToString("o")
    durationMinutes = $Minutes
}
$machine | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $logDirectory "machine.json") -Encoding utf8
$arguments = '--soak "' + $logDirectory + '" ' + $Minutes.ToString([Globalization.CultureInfo]::InvariantCulture) + ' --startup'
$runner = Start-Process -FilePath $testPath -ArgumentList $arguments -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput (Join-Path $logDirectory "stdout.log") -RedirectStandardError (Join-Path $logDirectory "stderr.log")
[pscustomobject]@{ pid=$runner.Id; startTime=$runner.StartTime.ToString("o"); executable=$testPath } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $logDirectory "process.json") -Encoding utf8
$deadline = [DateTime]::UtcNow.AddMinutes($Minutes + 3)
$samples = Join-Path $logDirectory "samples.jsonl"
while (!$runner.WaitForExit(60000)) {
    if (Test-Path -LiteralPath $samples) {
        if (([DateTime]::UtcNow - (Get-Item -LiteralPath $samples).LastWriteTimeUtc).TotalMinutes -gt 3) {
            "Application dispatcher stopped producing heartbeat samples." |
                Set-Content -LiteralPath (Join-Path $logDirectory "watchdog-failure.txt")
            $runner.Kill()
            exit 1
        }
    } elseif (([DateTime]::Now - $runner.StartTime).TotalMinutes -gt 3) {
        "Application never produced its first heartbeat." |
            Set-Content -LiteralPath (Join-Path $logDirectory "watchdog-failure.txt")
        $runner.Kill()
        exit 1
    }
    if ([DateTime]::UtcNow -gt $deadline) {
        "Validation exceeded its duration limit." | Set-Content -LiteralPath (Join-Path $logDirectory "watchdog-failure.txt")
        $runner.Kill()
        exit 1
    }
}
$runner.Refresh()
if ($runner.ExitCode -ne 0 -or !(Test-Path -LiteralPath (Join-Path $logDirectory "result.json"))) {
    "Validation exited without a successful result (exit $($runner.ExitCode))." |
        Set-Content -LiteralPath (Join-Path $logDirectory "watchdog-failure.txt")
    exit 1
}
$records = @(Get-Content -LiteralPath $samples | ForEach-Object { $_ | ConvertFrom-Json })
for ($index = 1; $index -lt $records.Count; $index++) {
    $gap = [DateTimeOffset]::Parse($records[$index].timestamp) - [DateTimeOffset]::Parse($records[$index - 1].timestamp)
    if ($gap.TotalSeconds -gt 180) {
        "Heartbeat gap exceeded three minutes; continuous runtime was not verified." |
            Set-Content -LiteralPath (Join-Path $logDirectory "watchdog-failure.txt")
        exit 1
    }
}
"Completed; review result.json and samples.jsonl before marking stable." | Write-Output
