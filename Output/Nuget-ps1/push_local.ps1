$currentDir = $PSScriptRoot
if (-not $currentDir) {
    $currentDir = Get-Location
}

$parentDir = Split-Path -Parent $currentDir

$filterDir = Join-Path -Path $parentDir -ChildPath "Nuget"

$nupkgFiles = Get-ChildItem -Path $filterDir -Filter "*.nupkg" -File

if ($nupkgFiles.Count -eq 0) {
    Write-Host "The .nupkg file was not found in the current directory" -ForegroundColor Red
    exit
}

$nugetPath = Join-Path $env:USERPROFILE ".Net.Utilities"
New-Item -ItemType Directory -Path $nugetPath -Force | Out-Null

foreach ($file in $nupkgFiles) {
    try {
        Write-Host "Push Local: $($file.Name) ..."

        .\nuget.exe add $file.FullName -Source $nugetPath

        Write-Host "Push Local OK: $($file.Name)"
    }
    catch {
        Write-Host "Push $($file.Name) Error: $_" -ForegroundColor Red
    }
}

Write-Host "All .nupkg files have been pushed successfully"