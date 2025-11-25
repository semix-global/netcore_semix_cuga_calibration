# 1. Generate SSH Key And Clone Repository
# 2. Checkout To Specify Tag
# 3. Publish TEST And SIMULATOR Release 
# 4. Create Installer
$RepositoryUrl = "ssh://git@192.168.0.28:23334/semix/software/calibrationui/netcore_semix_cuga_calibration.git"  # Modify to your repository URL
$TargetPath = "C:\APPs\Cuga-Calibration-Code"  # Modify to your desired target path
$TargetProjectName = "Cuga-Calibration-Solution"
$CsprojName = "Sources\$TargetProjectName\$TargetProjectName.csproj"
$RepositoryPath = $TargetPath
$PublishConfiguration = "Release"
$SdkPath = "dotnet"
$InnoSetupName = "Inno Setup 6"
$InstallerExeOutPutPath = "$TargetPath\Installer"
# Display configuration information
Write-Host "Configuration Information:" -ForegroundColor Cyan
Write-Host "Repository URL: $RepositoryUrl" -ForegroundColor Yellow
Write-Host "Target Path: $TargetPath" -ForegroundColor Yellow
Write-Host ""

# Clone repository using the new function
Write-Host "Step 1: Clone repository and submodules..." -ForegroundColor Green

# Function to clone repository and perform initial setup
function Clone-Repository {
    param(
        [Parameter(Mandatory=$true)]
        [string]$RepositoryUrl,
        
        [Parameter(Mandatory=$true)]
        [string]$TargetPath,
        
        [switch]$ForceClone = $false
    )

  # 创建新密钥
    Write-Host "Generate SSH key..." -ForegroundColor Cyan

    # 删除旧密钥（如果存在）
    $oldKey = "$env:TEMP\git_temp_key"
    if (Test-Path $oldKey) {
        Remove-Item $oldKey -Force
        Write-Host "Old SSH key deleted: $oldKey" -ForegroundColor Yellow
    }
    $newKey = "$env:TEMP\git_temp_key"

    $key = [System.Text.StringBuilder]::new()
    $null = $key.Append("-----BEGIN OPENSSH PRIVATE KEY-----`n")
    $null = $key.Append("b3BlbnNzaC1rZXktdjEAAAAABG5vbmUAAAAEbm9uZQAAAAAAAAABAAAAMwAAAAtzc2gtZW`n")
    $null = $key.Append("QyNTUxOQAAACC2FJ37gWFHTjVv6KQX4fLibS1OPby9d104BlmfW/JRFAAAAJBYpMyuWKTM`n")
    $null = $key.Append("rgAAAAtzc2gtZWQyNTUxOQAAACC2FJ37gWFHTjVv6KQX4fLibS1OPby9d104BlmfW/JRFA`n")
    $null = $key.Append("AAAEDMNbGHQObGtNmSfpoAKJD308veOOvcF37Knf8pBGaSjLYUnfuBYUdONW/opBfh8uJt`n")
    $null = $key.Append("LU49vL13XTgGWZ9b8lEUAAAADERFTExAREVMTC1QQwE=`n")
    $null = $key.Append("-----END OPENSSH PRIVATE KEY-----`n")
    
    $keyPath = "$env:TEMP\git_temp_key"
    
    try {
        # 确保密钥目录存在
        $keyDir = Split-Path $newKey -Parent
        if (!(Test-Path $keyDir)) {
            New-Item -ItemType Directory -Path $keyDir -Force | Out-Null
        }

        # 使用UTF-8无BOM编码写入密钥
        [System.IO.File]::WriteAllText($newKey, $key.ToString())

        # 验证密钥有效性
        ssh-keygen -y -f $newKey
        
        # 设置正确的文件权限（仅限当前用户）
        # icacls $newKey /inheritance:r /grant:r "$env:USERNAME:(F)" 2>$null | Out-Null
        
        Write-Host "SSH key generated: $newKey" -ForegroundColor Green        
    }
    catch {
        Write-Error "Error generating SSH key: $_"
        return $null
    }

    if ($newKey) {
        # 设置Git环境变量
        $gitKeyPath = $newKey -replace '\\', '/'
        $env:GIT_SSH_COMMAND = "ssh -i '$gitKeyPath' -o IdentitiesOnly=yes -o StrictHostKeyChecking=no"
        
        Write-Host ""
        Write-Host "SSH environment configured successfully!" -ForegroundColor Green
        Write-Host "Key path: $newKey" -ForegroundColor Cyan
    } else {
        Write-Error "SSH key generation failed"
    }
  # Clone repository
    # Check if target repository exists and clone if necessary
    Write-Host "Checking repository status..." -ForegroundColor Cyan
    $repoExists = $false
    $shouldClone = $false

    # Check if TargetPath exists
    if (Test-Path -Path $TargetPath -PathType Container) {
        Write-Host "Target directory exists: $TargetPath" -ForegroundColor Green
        
        # Check if it's a valid git repository with correct remote
        $gitDir = Join-Path -Path $TargetPath -ChildPath ".git"
        if (Test-Path -Path $gitDir -PathType Container) {
            try {
                Set-Location -Path $TargetPath
                $currentRemote = git config --get remote.origin.url 2>$null
                
                if ($currentRemote -eq $RepositoryUrl) {
                    Write-Host "Repository already exists with correct remote URL" -ForegroundColor Green
                    $repoExists = $true
                }
                else {
                    Write-Host "Directory exists but remote URL mismatch" -ForegroundColor Yellow
                    Write-Host "Expected: $RepositoryUrl" -ForegroundColor Yellow
                    Write-Host "Found: $currentRemote" -ForegroundColor Yellow
                    $shouldClone = $true
                }
            }
            catch {
                Write-Host "Error checking repository status: $($_.Exception.Message)" -ForegroundColor Yellow
                $shouldClone = $true
            }
        }
        else {
            Write-Host "Directory exists but is not a git repository" -ForegroundColor Yellow
            $shouldClone = $true
        }
    }
    else {
        Write-Host "Target directory does not exist: $TargetPath" -ForegroundColor Yellow
        $shouldClone = $true
    }

    # Force clone if requested
    if ($ForceClone) {
        Write-Host "Force clone requested, will remove existing directory..." -ForegroundColor Yellow
        $shouldClone = $true
    }

    # Clone repository if needed
    if ($shouldClone) {
        Write-Host "Preparing to clone repository..." -ForegroundColor Green
        
        # Remove existing directory if it exists
        if (Test-Path -Path $TargetPath -PathType Container) {
            Write-Host "Removing existing directory..." -ForegroundColor Yellow
            Remove-Item -Path $TargetPath -Recurse -Force
        }
        
        # Create parent directory if needed
        $parentDir = Split-Path -Path $TargetPath -Parent
        if (-not (Test-Path -Path $parentDir -PathType Container)) {
            New-Item -ItemType Directory -Path $parentDir -Force | Out-Null
        }
        
        try {
            Write-Host "Cloning repository $RepositoryUrl to $TargetPath ..." -ForegroundColor Green
            
            # Check if target path exists, create if not
            if (-not (Test-Path -Path $TargetPath -PathType Container)) {
                Write-Host "Creating target directory $TargetPath ..." -ForegroundColor Yellow
                New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null
            }
            
            git clone --recursive $RepositoryUrl $TargetPath *>&1 | Out-Host
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Successfully cloned repository to $TargetPath" -ForegroundColor Green
                $repoExists = $true
            }
            else {
                Write-Host "Error: Clone operation failed, Git returned code: $LASTEXITCODE" -ForegroundColor Red
                return $false
            }
        }
        catch {
            Write-Host "Error: Exception occurred during cloning - $($_.Exception.Message)" -ForegroundColor Red
            return $false
        }
    }
    else {
        Write-Host "Using existing repository" -ForegroundColor Green
    }

    # Change to target directory for subsequent operations
    Set-Location -Path $TargetPath

    # Execute Git cleanup commands (reset and clean) - only if we have a valid repo
    if ($repoExists -or !$skipClone) {
        try {
            Write-Host "Executing Git cleanup commands..." -ForegroundColor Green
            
            # git reset --hard 
            Write-Host "Executing: git reset --hard" -ForegroundColor Yellow
            $resetOutput = git reset --hard 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host $resetOutput -ForegroundColor Green
            } else {
                Write-Host "Warning: git reset --hard failed with code: $LASTEXITCODE" -ForegroundColor Yellow
                Write-Host $resetOutput -ForegroundColor Yellow
            }
            
            # git clean -xdf 
            Write-Host "Executing: git clean -xdf" -ForegroundColor Yellow
            $cleanOutput = git clean -xdf 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host $cleanOutput -ForegroundColor Green
            } else {
                Write-Host "Warning: git clean -xdf failed with code: $LASTEXITCODE" -ForegroundColor Yellow
                Write-Host $cleanOutput -ForegroundColor Yellow
            }
            
            Write-Host "Git cleanup commands completed successfully" -ForegroundColor Green
            return $true
        }
        catch {
            Write-Host "Warning: Error occurred during Git cleanup - $($_.Exception.Message)" -ForegroundColor Yellow
            return $true  # Continue even if cleanup fails
        }
    }
    
    return $true
}

$cloneSuccess = Clone-Repository -RepositoryUrl $RepositoryUrl -TargetPath $TargetPath
if (-not $cloneSuccess) {
    Write-Host "Error: Failed to clone repository, stopping script" -ForegroundColor Red
    exit 1
}


# Call the tag checkout function
Write-Host "Step 2: Check out to specify tag..." -ForegroundColor Green

# Function to checkout specific tag and update submodules
function Checkout-Tag {
    param(
        [Parameter(Mandatory=$true)]
        [string]$RepositoryPath,
        
        [Parameter(Mandatory=$false)]
        [string]$TagName,
        
        [switch]$Interactive = $true
    )
        
    try {
        # If interactive mode, prompt for tag name
        $targetTag = $TagName
        if ($Interactive -and [string]::IsNullOrWhiteSpace($targetTag)) {
            Write-Host ""
            Write-Host "Tag Checkout Configuration:" -ForegroundColor Cyan
            
            # List available tags for reference
            Set-Location -Path $RepositoryPath
            $availableTags = git tag -l 2>$null
            if ($availableTags) {
                Write-Host "Available tags:" -ForegroundColor Yellow
                Write-Host $availableTags -ForegroundColor Cyan
            }
            
            # Prompt user to enter Tag name
            $targetTag = Read-Host "Enter Tag name to checkout (leave empty to skip)"
        }
        
        if (![string]::IsNullOrWhiteSpace($targetTag)) {
            Write-Host "Checking out Tag: $targetTag" -ForegroundColor Green
            Set-Location -Path $RepositoryPath
            
            # Fetch all tags first
            Write-Host "Fetching tags..." -ForegroundColor Yellow
            git fetch --all --tags *>&1 | Out-Host
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Warning: Failed to fetch tags" -ForegroundColor Yellow
            }
            
            # Checkout the specified tag
            Write-Host "Executing: git checkout $targetTag" -ForegroundColor Yellow
            $checkoutOutput = git checkout $targetTag 2>&1 | Out-String
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Successfully checked out Tag: $targetTag" -ForegroundColor Green
               
                $filteredOutput = $checkoutOutput -replace "HEAD is now at.*", "" -replace "Previous HEAD position was.*", "" -replace "Note:.*", "" -replace "^\s*$", ""
                if ($filteredOutput.Trim()) {
                    Write-Host $filteredOutput.Trim() -ForegroundColor Green
                }
                
                # Display current commit information
                $currentCommit = git rev-parse --short HEAD 2>$null
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "Current commit: $currentCommit" -ForegroundColor Cyan
                }
                
                # After checkout tag, pull with submodules
                Write-Host ""
                Write-Host "Updating submodules after tag checkout..." -ForegroundColor Cyan
                Write-Host "Executing: git pull --recurse-submodules" -ForegroundColor Yellow
                $pullOutput = git pull --recurse-submodules 2>&1 | Out-String
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "Submodules updated successfully after tag checkout" -ForegroundColor Green
                    $filteredOutput = $pullOutput -replace "From.*", "" -replace "\*.*", "" -replace "^\s*$", ""
                    if ($filteredOutput.Trim()) {
                        Write-Host $filteredOutput.Trim() -ForegroundColor Green
                    }
                } else {
                    Write-Host "Warning: Failed to update submodules, attempting submodule update..." -ForegroundColor Yellow
                    $submoduleOutput = git submodule update --init --recursive 2>&1 | Out-String
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "Submodules initialized and updated successfully" -ForegroundColor Green
                    } else {
                        Write-Host "Warning: Submodule update failed" -ForegroundColor Yellow
                        Write-Host $submoduleOutput -ForegroundColor Yellow
                    }
                }
                
                return $true
            }
            else {
                Write-Host "Error: Failed to checkout Tag '$targetTag', Git returned code: $LASTEXITCODE" -ForegroundColor Red
                
                # List available tags for reference
                Write-Host "Available tags:" -ForegroundColor Yellow
                git tag -l 2>&1 | Out-Host
                return $false
            }
        }
        else {
            Write-Host "Skipping tag checkout (no tag specified)" -ForegroundColor Yellow
            return $true
        }
    }
    catch {
        Write-Host "Error: Exception occurred during tag checkout - $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

$checkoutSuccess = Checkout-Tag -RepositoryPath $TargetPath -Interactive
if (-not $checkoutSuccess) {
    Write-Host "Error: Failed to checkout tag, stopping script" -ForegroundColor Red
    exit 1
}

Write-Host "Step 3: Publishing project..." -ForegroundColor Green
$testEnvironmentStr="TEST"
$testCsprojOutputName = "Sources\$TargetProjectName\bin\publish\$testEnvironmentStr\$PublishConfiguration\net480\win-x64"

$simulatorEnvironmentStr="SIMULATOR"
$simulatorCsprojOutputName = "Sources\$TargetProjectName\bin\publish\$simulatorEnvironmentStr\$PublishConfiguration\net480\win-x64"

# Function to publish the project
function Publish-Project {
    param(
        [string]$ProjectName,
        [string]$ProjectPath,
        [string]$CsprojOutputName,
        [string]$RepositoryPath,
        [string]$PublishConfiguration,
        [string]$SdkPath = "dotnet",
        [bool]$isDefineConstants = $false,
        [string]$DefineConstants = ""
    )
    
    # Build full paths
    $fullCsprojPath = Join-Path -Path $RepositoryPath -ChildPath $ProjectPath
    $fullOutputPath = Join-Path -Path $RepositoryPath -ChildPath $CsprojOutputName
    
    Write-Host "Repository path: $RepositoryPath" -ForegroundColor Cyan
    Write-Host "Project file: $fullCsprojPath" -ForegroundColor Cyan
    Write-Host "Output path: $fullOutputPath" -ForegroundColor Cyan
    Write-Host "Using dotnet SDK: $SdkPath" -ForegroundColor Green
    
    # Check if specific csproj file exists
    if (-not (Test-Path -Path $fullCsprojPath -PathType Leaf)) {
        Write-Host "Error: Project file not found: $fullCsprojPath" -ForegroundColor Red
        Write-Host "Please check the repository path and project file location" -ForegroundColor Yellow
        return $false
    }
    
    Write-Host "Found project file: $fullCsprojPath" -ForegroundColor Green
    
    try {
        # Publish Step 1: dotnet restore
        Write-Host "Restoring packages..." -ForegroundColor Green
        $restoreCommand = "$SdkPath restore `"$fullCsprojPath`""
        Write-Host "Executing: $restoreCommand" -ForegroundColor Yellow
        
        # 设置控制台输出编码为UTF-8以解决中文乱码问题
        $originalEncoding = [Console]::OutputEncoding
        try {
            [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
            Invoke-Expression $restoreCommand *>&1 | Out-Host
        } finally {
            # 恢复原始编码
            [Console]::OutputEncoding = $originalEncoding
        }
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Error: Package restore failed with exit code: $LASTEXITCODE" -ForegroundColor Red
            return $false
        }
        
        Write-Host "Package restore completed successfully!" -ForegroundColor Green
        
        # Publish Step 2: Create output directory if it doesn't exist
        $outputDir = Split-Path -Path $fullOutputPath -Parent
        if (-not (Test-Path -Path $outputDir)) {
            New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
            Write-Host "Created output directory: $outputDir" -ForegroundColor Green
        }
        
        # Publish Step 3: Build publish command with proper parameter formatting
        $publishCommand = "$sdkPath publish `"$fullCsprojPath`" --configuration Release --framework net480 --runtime win-x64 --no-self-contained --output `"$fullOutputPath`""
        
        # Add DefineConstants parameter if enabled
        if ($isDefineConstants -and $DefineConstants) {
            $publishCommand = "$sdkPath publish `"$fullCsprojPath`" -p:DefineConstants=`"$DefineConstants`" --configuration Release --framework net480 --runtime win-x64 --no-self-contained --output `"$fullOutputPath`""
            Write-Host "DefineConstants enabled: $DefineConstants" -ForegroundColor Cyan
        } else {
            $publishCommand = "$sdkPath publish `"$fullCsprojPath`" --configuration Release --framework net480 --runtime win-x64 --no-self-contained --output `"$fullOutputPath`""
        }
        
        Write-Host "Executing: $publishCommand" -ForegroundColor Yellow
        
        # Fix Chinese character display issue by setting console encoding to UTF-8
        $originalEncoding = [Console]::OutputEncoding
        try {
            [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
            Invoke-Expression $publishCommand *>&1 | Out-Host
        } finally {
            # Restore original encoding
            [Console]::OutputEncoding = $originalEncoding
        }
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Error: Publish failed with exit code: $LASTEXITCODE" -ForegroundColor Red
            return $false
        }
        
        Write-Host "Publish completed successfully for $ProjectName!" -ForegroundColor Green
        Write-Host "Output location: $fullOutputPath" -ForegroundColor Cyan
        Write-Host "Used dotnet SDK: $SdkPath" -ForegroundColor Green
        
        # Generate DLL version information file (JSON format)
        $versionInfoFile = Join-Path -Path $fullOutputPath -ChildPath "AssemblyVersions.json"
        $versionInfo = @()
        
        if (Test-Path -Path $fullOutputPath) {
            $publishedFiles = Get-ChildItem -Path $fullOutputPath -File -Recurse
            if ($publishedFiles) {
                Write-Host "Published files: $($publishedFiles.Count)" -ForegroundColor Green
                
                # Collect DLL version information
                $dllFiles = $publishedFiles | Where-Object { $_.Extension -eq ".dll" }
                $exeFiles = $publishedFiles | Where-Object { $_.Extension -eq ".exe" }
                
                $allAssemblies = $dllFiles + $exeFiles
                
                foreach ($file in $allAssemblies) {
                    try {
                        $assemblyInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($file.FullName)
                        $versionInfo += [PSCustomObject]@{
                            FileName = $file.Name
                            FullPath = $file.FullName
                            AssemblyFullName=$assemblyInfo.FullName
                            AssemblyVersion = $assemblyInfo.FileVersion
                            ProductVersion = $assemblyInfo.ProductVersion
                            Company = $assemblyInfo.CompanyName
                            Description = $assemblyInfo.FileDescription
                            SizeBytes = $file.Length
                            SizeKB = [Math]::Round($file.Length / 1KB, 2)
                            SizeMB = [Math]::Round($file.Length / 1MB, 2)
                            LastModified = $file.LastWriteTime.ToString("yyyy-MM-ddTHH:mm:ss")
                            LastModifiedUTC = $file.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss")
                        }
                    }
                    catch {
                        $versionInfo += [PSCustomObject]@{
                            FileName = $file.Name
                            FullPath = $file.FullName
                            AssemblyFullName=$assemblyInfo.FullName
                            AssemblyVersion = $null
                            ProductVersion = $null
                            Company = $null
                            Description = $null
                            SizeBytes = $file.Length
                            SizeKB = [Math]::Round($file.Length / 1KB, 2)
                            SizeMB = [Math]::Round($file.Length / 1MB, 2)
                            LastModified = $file.LastWriteTime.ToString("yyyy-MM-ddTHH:mm:ss")
                            LastModifiedUTC = $file.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss")
                        }
                    }
                }
                
                # Create JSON structure
                $jsonOutput = [PSCustomObject]@{
                    Metadata = [PSCustomObject]@{
                        GeneratedAt = Get-Date -Format "yyyy-MM-ddTHH:mm:ss"
                        ProjectName = $ProjectName
                        Configuration = "Release"
                        Framework = "net480"
                        Runtime = "win-x64"
                        TotalFiles = $publishedFiles.Count
                        AssemblyCount = $versionInfo.Count
                        DLLCount = $dllFiles.Count
                        ExecutableCount = $exeFiles.Count
                    }
                    Assemblies = $versionInfo | Sort-Object FileName
                }
                
                # Write JSON to file
                $jsonContent = $jsonOutput | ConvertTo-Json -Depth 10
                $jsonContent | Out-File -FilePath $versionInfoFile -Encoding UTF8
                Write-Host "Version information saved to: $versionInfoFile" -ForegroundColor Green
                
                # Display summary
                Write-Host "Found $($dllFiles.Count) DLL files and $($exeFiles.Count) executable files" -ForegroundColor Cyan
                
                # Show main executable files
                if ($exeFiles) {
                    Write-Host "Executable files:" -ForegroundColor Green
                    $exeFiles | ForEach-Object { Write-Host "  $($_.Name)" -ForegroundColor Cyan }
                }
                
                # Show DLL files
                if ($dllFiles) {
                    Write-Host "Main DLL files:" -ForegroundColor Green
                    $dllFiles | Select-Object -First 5 | ForEach-Object { Write-Host "  $($_.Name)" -ForegroundColor Cyan }
                    if ($dllFiles.Count -gt 5) {
                        Write-Host "  ... and $($dllFiles.Count - 5) more" -ForegroundColor Gray
                    }
                }
            }
        }
        
        # Optional: Open target directory after cloning
        $openFolder = Read-Host "Open publish directory? (Y/N)"
        if ($openFolder -ne 'n' -and $openFolder -ne 'N') {
            try {
                Invoke-Item $fullOutputPath
                Write-Host "Target directory opened" -ForegroundColor Green
            }
            catch {
                Write-Host "Unable to open directory: $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }
        return $true
    }
    catch {
        Write-Host "Error: Exception occurred during publish process - $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Execute test publish
Write-Host "Publishing test release project..." -ForegroundColor Green
$publishSuccess = Publish-Project -ProjectName $TargetProjectName -ProjectPath $CsprojName -CsprojOutputName $testCsprojOutputName -RepositoryPath $RepositoryPath -SdkPath $SdkPath -isDefineConstants $false -DefineConstants ""

if (-not $publishSuccess) {
    Write-Host "Publish test release project failed, exiting..." -ForegroundColor Red
    exit 1
}

# Execute simulator publish
Write-Host "Publishing simulator release project..." -ForegroundColor Green
$publishSuccess = Publish-Project -ProjectName $TargetProjectName -ProjectPath $CsprojName -CsprojOutputName $simulatorCsprojOutputName -RepositoryPath $RepositoryPath -SdkPath $SdkPath -isDefineConstants $true -DefineConstants "TRACE%3BRELEASE%3BNETFRAMEWORK%3BNET48%3BSIMULATOR"

if (-not $publishSuccess) {
    Write-Host "Publish simulator release project failed, exiting..." -ForegroundColor Red
    exit 1
}


Write-Host "Step 4: Creating installer with Inno Setup..." -ForegroundColor Green

# Helper function to find Inno Setup compiler
function Find-InnoSetupCompiler {
    param(
        [Parameter(Mandatory=$false)]
        [string]$InnoSetupName = "Inno Setup 6"
    )
    
    $isccPath = "${env:ProgramFiles(x86)}\$InnoSetupName\ISCC.exe"
    if (-not (Test-Path -Path $isccPath -PathType Leaf)) {
        $isccPath = "${env:ProgramFiles}\$InnoSetupName\ISCC.exe"
    }
    
    if (Test-Path -Path $isccPath -PathType Leaf) {
        Write-Host "Using Inno Setup compiler: $isccPath" -ForegroundColor Green
        return $isccPath
    } else {
        Write-Host "Error: Inno Setup compiler (ISCC.exe) not found" -ForegroundColor Red
        Write-Host "Please install Inno Setup from https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
        return $null
    }
}

# Function to create installer(s) using Inno Setup - Supports multiple ISS scripts
function New-InnoSetupInstallers {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$ProjectName,
        
        [Parameter(Mandatory=$true)]
        [string]$EnvironmentMode,
        
        [Parameter(Mandatory=$true)]
        [string]$Configuration,
        
        [Parameter(Mandatory=$true)]
        [string]$RepositoryPath,
        
        [Parameter(Mandatory=$true)]
        [string]$CsprojOutputPath,
        
        [Parameter(Mandatory=$true)]
        [string]$InstallerOutputPath,
        
        [Parameter(Mandatory=$false)]
        [string[]]$IssScriptPaths = @(),
        
        [Parameter(Mandatory=$false)]
        [string]$InnoSetupName = "Inno Setup 6",
        
        [Parameter(Mandatory=$false)]
        [switch]$ProcessAllScripts = $false,
        
        [Parameter(Mandatory=$false)]
        [switch]$OpenInstallerDirectory = $false,
        
        [Parameter(Mandatory=$false)]
        [switch]$Interactive = $true
    )
    # Ask user if they want to generate the installer (if Interactive parameter is true)
    if ($Interactive) {
        $createInstaller = $true
        $userInput = Read-Host "Do you want to generate $EnvironmentMode version installer? (Y/N, default is Y)"
        if ($userInput -and $userInput.ToUpper() -eq "N") {
            $createInstaller = $false
            Write-Host "Skipping $EnvironmentMode version installer generation..." -ForegroundColor Yellow
            return $true  # Return true to indicate successful operation (even though it was skipped)
        }
    }

    # Build the .iss filename based on TargetProjectName and environmentMode
    $issScriptName = "$ProjectName-$EnvironmentMode-$Configuration-Publisher.iss"
    $issScriptPath = Join-Path -Path $PSScriptRoot -ChildPath $issScriptName

    # Alternative naming pattern (lowercase with underscores)
    $issScriptNameAlt = "$($ProjectName.ToLower().Replace('-', '_'))-$($EnvironmentMode.ToLower())_publisher.iss"
    $issScriptPathAlt = Join-Path -Path $PSScriptRoot -ChildPath $issScriptNameAlt

    Write-Host "Looking for Inno Setup script:" -ForegroundColor Cyan
    Write-Host "Inno setup script name: $issScriptName" -ForegroundColor Yellow
    Write-Host "Inno setup script alternative name: $issScriptNameAlt" -ForegroundColor Yellow
    Write-Host "issScriptPath: $issScriptPath" -ForegroundColor Yellow

    Write-Host "Step: Creating installer(s) with Inno Setup..." -ForegroundColor Green
    
    # Get current version/tag for Inno Setup macro
    $currentVersion = ""
    try {
        Set-Location -Path $TargetPath
        $currentVersion = git describe --tags --exact-match 2>$null
        if (-not $currentVersion) {
            $currentVersion = git rev-parse --short HEAD 2>$null
        }
        if (-not $currentVersion) {
            $currentVersion = "1.0.0"
        }
    }
    catch {
        $currentVersion = "1.0.0"
    }
    
    Write-Host "My App Version: $currentVersion" -ForegroundColor Yellow
    Write-Host "My App CompileMode: $Configuration" -ForegroundColor Yellow
    Write-Host "Environment Mode: $EnvironmentMode" -ForegroundColor Yellow
    Write-Host "Csproj Output Name: $CsprojOutputPath" -ForegroundColor Yellow
    Write-Host "My App Publish Path: $RepositoryPath" -ForegroundColor Yellow
    Write-Host "Installer Exe OutPut Path: $InstallerOutputPath" -ForegroundColor Yellow

    $guid = [guid]::NewGuid().ToString()
    $appId = "{{" + $guid + "}"
    # Prepare common Inno Setup parameters
    $issParams = @{
        AppId = $appId
        MyAppVersion = $currentVersion
        MyAppCompileMode = $Configuration
        EnvironmentMode = $EnvironmentMode
        CsprojOutputName = $CsprojOutputPath
        MyAppPublishPath = $RepositoryPath
        InstallerExeOutPutPath = $InstallerOutputPath
    }
    
    # Find Inno Setup compiler
    $isccPath = Find-InnoSetupCompiler -InnoSetupName $InnoSetupName
    if (-not $isccPath) {
        return $false
    }
    
    # Determine which ISS scripts to process
    $scriptsToProcess = @()
    
    if ($IssScriptPaths -and $IssScriptPaths.Count -gt 0) {
        # Use explicitly provided script paths
        foreach ($scriptPath in $IssScriptPaths) {
            if (Test-Path -Path $scriptPath -PathType Leaf) {
                $scriptsToProcess += $scriptPath
            } else {
                Write-Host "Warning: Specified ISS script not found: $scriptPath" -ForegroundColor Yellow
            }
        }
    }
    Write-Host "IssScriptPaths $IssScriptPaths" -ForegroundColor Green
    Write-Host "scriptsToProcess $scriptsToProcess" -ForegroundColor Green

    if ($scriptsToProcess.Count -eq 0) {
        # Auto-discover scripts based on naming patterns
        $namingPatterns = @(
            "$ProjectName-$EnvironmentMode-$Configuration-Publisher.iss",
            "$($ProjectName.ToLower().Replace('-', '_'))-$($EnvironmentMode.ToLower())_publisher.iss",
            "$ProjectName-$EnvironmentMode-Publisher.iss",
            "$($ProjectName.ToLower().Replace('-', '_'))_$($EnvironmentMode.ToLower())_publisher.iss"
        )
        
        foreach ($pattern in $namingPatterns) {
            $scriptPath = Join-Path -Path $PSScriptRoot -ChildPath $pattern
            if (Test-Path -Path $scriptPath -PathType Leaf) {
                $scriptsToProcess += $scriptPath
                Write-Host "Auto-discovered ISS script: $pattern" -ForegroundColor Green
                Write-Host "PSScriptRoot: $PSScriptRoot" -ForegroundColor Green
            }
        }
    }
    
    if ($scriptsToProcess.Count -eq 0) {
        Write-Host "Error: No ISS scripts found to process" -ForegroundColor Red
        Write-Host "Expected patterns:" -ForegroundColor Yellow
        foreach ($pattern in $namingPatterns) {
            Write-Host "  $pattern" -ForegroundColor Gray
        }
        return $false
    }
    
    # Display parameters
    Write-Host "Inno Setup Parameters:" -ForegroundColor Cyan
    foreach ($param in $issParams.GetEnumerator()) {
        Write-Host "$($param.Key)=$($param.Value)" -ForegroundColor Yellow
    }
    
    # Process each ISS script
    $successCount = 0
    $totalCount = $scriptsToProcess.Count
    
    Write-Host "Processing $totalCount ISS script(s)..." -ForegroundColor Green
    
    foreach ($issScript in $scriptsToProcess) {
        $scriptName = Split-Path -Path $issScript -Leaf
        Write-Host "Processing: $scriptName" -ForegroundColor Cyan
        
        try {
            # Build command arguments
            $arguments = @()
            foreach ($param in $issParams.GetEnumerator()) {
                $arguments += "/D$($param.Key)=$($param.Value)"
            }
            $arguments += $issScript
            
            Write-Host "Executing: $isccPath $($arguments -join ' ')" -ForegroundColor Green
            
            # Execute Inno Setup compiler
            & $isccPath @arguments *>&1 | Out-Host
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Successfully created installer from: $scriptName" -ForegroundColor Green
                $successCount++
            } else {
                Write-Host "Failed to create installer from: $scriptName (Exit code: $LASTEXITCODE)" -ForegroundColor Red
            }
        }
        catch {
            Write-Host "Exception occurred while processing $scriptName`: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    Write-Host "Processing complete: $successCount/$totalCount installer(s) created successfully" -ForegroundColor $(if ($successCount -eq $totalCount) { "Green" } else { "Yellow" })
    
    # Verify installers were created
    if (Test-Path -Path $InstallerOutputPath -PathType Container) {
        $installerFiles = Get-ChildItem -Path $InstallerOutputPath -Filter "*.exe" -File
        if ($installerFiles) {
            Write-Host "Generated installer(s):" -ForegroundColor Cyan
            foreach ($installer in $installerFiles) {
                Write-Host "  $($installer.FullName)" -ForegroundColor Green
            }
            
            if ($OpenInstallerDirectory) {
                try {
                    Invoke-Item $InstallerOutputPath
                    Write-Host "Installer directory opened" -ForegroundColor Green
                }
                catch {
                    Write-Host "Unable to open installer directory: $($_.Exception.Message)" -ForegroundColor Yellow
                }
            }
        }
    }
    
    return ($successCount -eq $totalCount)
}

# create test release installer(s)
Write-Host "Creating test release installer(s)..." -ForegroundColor Green
$installerSuccess = New-InnoSetupInstallers -ProjectName $TargetProjectName -EnvironmentMode $testEnvironmentStr -Configuration $PublishConfiguration -RepositoryPath $TargetPath -CsprojOutputPath $testCsprojOutputName -InstallerOutputPath $InstallerExeOutPutPath -Interactive
if (-not $installerSuccess) {
    Write-Host "Error: Installer creation failed" -ForegroundColor Red
    exit 1
}

# create simulator release installer(s)
Write-Host "Creating simulator release installer(s)..." -ForegroundColor Green
$installerSuccess = New-InnoSetupInstallers -ProjectName $TargetProjectName -EnvironmentMode $simulatorEnvironmentStr -Configuration $PublishConfiguration -RepositoryPath $TargetPath -CsprojOutputPath $simulatorCsprojOutputName -InstallerOutputPath $InstallerExeOutPutPath -Interactive
if (-not $installerSuccess) {
    Write-Host "Error: Installer creation failed" -ForegroundColor Red
    exit 1
}

Write-Host "All operations completed!" -ForegroundColor Green
Write-Host "Press any key exit..." -ForegroundColor Cyan
Read-Host
