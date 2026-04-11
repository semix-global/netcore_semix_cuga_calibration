$currentDir = $PSScriptRoot
if (-not $currentDir) {
    $currentDir = Get-Location
}

$nupkgFiles = Get-ChildItem -Path $currentDir -Filter "*.nupkg" -File

if ($nupkgFiles.Count -eq 0) {
    Write-Host "The .nupkg file was not found in the current directory" -ForegroundColor Red
    exit
}

$nugetPath = Join-Path $env:USERPROFILE ".Net.Utilities"
New-Item -ItemType Directory -Path $nugetPath -Force | Out-Null

foreach ($file in $nupkgFiles) {
    try {
        Write-Host "Push Local: $($file.Name) ..."
        
		dotnet nuget push $file.FullName --source $nugetPath
        
        Write-Host "Push Local OK: $($file.Name)"
    }
    catch {
        Write-Host "Push $($file.Name) Error: $_" -ForegroundColor Red
    }
}

Write-Host "All .nupkg files have been pushed successfully"