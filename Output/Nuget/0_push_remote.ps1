$currentDir = $PSScriptRoot
if (-not $currentDir) {
    $currentDir = Get-Location
}

$nupkgFiles = Get-ChildItem -Path $currentDir -Filter "*.nupkg" -File

if ($nupkgFiles.Count -eq 0) {
    Write-Host "The .nupkg file was not found in the current directory" -ForegroundColor Red
    exit
}

$apiKey = "22a40da9-901c-310f-bb3c-7325bcd23d07"
$source = "http://192.168.60.201:8081/repository/semix-nuget/index.json"

foreach ($file in $nupkgFiles) {
    try {
        Write-Host "Push Remote: $($file.Name) ..."
        
        dotnet nuget push $file.FullName --api-key $apiKey --source $source --force-english-output --skip-duplicate
        
        Write-Host "Push Remote OK: $($file.Name)"
    }
    catch {
        Write-Host "Push $($file.Name) Error: $_" -ForegroundColor Red
    }
}

Write-Host "All .nupkg files have been pushed successfully"