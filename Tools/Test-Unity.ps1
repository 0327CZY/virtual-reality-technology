param([string]$UnityEditor)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$editor = Find-UnityEditor $UnityEditor
$projectRoot = Split-Path -Parent $PSScriptRoot
$results = Join-Path $projectRoot 'TestResults'; New-Item -ItemType Directory -Path $results -Force | Out-Null
$xmlFile = Join-Path $results 'EditMode.xml'; $log = Join-Path $results 'EditMode.log'
$started = Get-Date
# Do not add -quit: the Unity Test Runner exits after completing its asynchronous run.
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $projectRoot + '"'), '-runTests', '-testPlatform', 'EditMode', '-testResults', ('"' + $xmlFile + '"'), '-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Unity tests failed (exit $($process.ExitCode)). Read $log" }
if (-not (Test-Path -LiteralPath $xmlFile)) { throw "Unity produced no test report. Read $log" }
if ((Get-Item -LiteralPath $xmlFile).LastWriteTime -lt $started) { throw 'Test report is stale.' }
[xml]$report = Get-Content -LiteralPath $xmlFile -Raw
$run = $report.'test-run'
if ($run.result -ne 'Passed' -or [int]$run.total -eq 0 -or [int]$run.failed -gt 0) { throw "Unity test report: $($run.result), total=$($run.total), failed=$($run.failed)" }
Write-Host "Unity EditMode tests passed: $($run.passed)/$($run.total). This does not verify Quest tracking or rendering."
