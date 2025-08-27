# 获取当前脚本所在目录
$currentDir = $PSScriptRoot
if (-not $currentDir) {
    $currentDir = Get-Location
}

# 获取当前目录下所有的.nupkg文件
$nupkgFiles = Get-ChildItem -Path $currentDir -Filter "*.nupkg" -File

if ($nupkgFiles.Count -eq 0) {
    Write-Host "当前目录中没有找到.nupkg文件"
    exit
}

# NuGet推送参数
$apiKey = "22a40da9-901c-310f-bb3c-7325bcd23d07"
$source = "http://192.168.60.201:8081/repository/semix-nuget/index.json"

foreach ($file in $nupkgFiles) {
    try {
        Write-Host "正在推送: $($file.Name) ..."
        
        # 直接使用当前文件名执行推送命令
        dotnet nuget push $file.FullName --api-key $apiKey --source $source --force-english-output --skip-duplicate
        
        Write-Host "推送完成: $($file.Name)"
    }
    catch {
        Write-Host "处理文件 $($file.Name) 时出错: $_" -ForegroundColor Red
    }
}

Write-Host "所有.nupkg文件推送完成"