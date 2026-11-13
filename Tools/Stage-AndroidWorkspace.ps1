param([string]$Destination)
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path -Parent $PSScriptRoot
if (-not $Destination) { $Destination = 'D:\VRHomework3\build-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '\QuestRayCounter' }
$destinationRoot = [IO.Path]::GetFullPath($Destination)
if ($destinationRoot -match '[^\x00-\x7F]') { throw 'The Android build workspace must use an ASCII path.' }
if (Test-Path -LiteralPath $destinationRoot) { throw "Destination already exists. Choose a new empty location: $destinationRoot" }
$sourceFull = [IO.Path]::GetFullPath($sourceRoot).TrimEnd('\') + '\'
if ($destinationRoot.StartsWith($sourceFull,[StringComparison]::OrdinalIgnoreCase)) { throw 'Destination cannot be inside the source project.' }
New-Item -ItemType Directory -Path $destinationRoot -Force | Out-Null
$excluded = @('Library','Temp','Obj','Logs','UserSettings','Builds','TestResults','.git','.vs')
Get-ChildItem -LiteralPath $sourceRoot -Force | Where-Object { $_.Name -notin $excluded } | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $destinationRoot -Recurse -Force
}
Set-Content -LiteralPath (Join-Path $destinationRoot '.source-workspace.txt') -Value $sourceRoot -Encoding UTF8
Write-Host "Created Android build workspace: $destinationRoot"
Write-Output $destinationRoot
