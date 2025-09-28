@echo off

echo **************************************************************************************************************
echo *                                                                                                            *
echo *                              Welcome to Cuga-Calibration-Solution-Simulator Publish Assistant              *
echo *                                                                                                            *
echo *                                          version V2.0                                                      *
echo *                                                                                                            *
echo **************************************************************************************************************

echo.
echo.

echo **************************************************************************************************************
echo *                                                                                                            *
echo *                                     The follow we're going to do                                           *
echo *                                                                                                            *
echo *  0. check runtime                                                                                          *
echo *  1. pull code from server                                                                                  *
echo *  2. compiling program                                                                                      *
echo *  3. copy program to apps directory                                                                         *
echo *                                                                                                            *
echo *                                                                                                            *
echo **************************************************************************************************************
pause

set git_user=xuankaizhi
set brance_name=develop
set repository_url=ssh://git@192.168.0.28:23334/semix/software/calibrationui/netcore_semix_cuga_calibration.git

set apps_home=C:\APPs\Cuga-Calibration
set repository_path=%apps_home%\Cuga-Calibration-Solution_Code
set apps_name=Cuga-Calibration-Solution-Simulator_Publish
set apps_path=%apps_home%\%apps_name%

set tempDir=%apps_home%\dotnet_install
set sdkUrl=http://192.168.60.201:8090/chfs/shared/04_SDKS/dotnet-sdk-8.0.406-win-x64.zip
set devPackEnUrl=http://192.168.60.201:8090/chfs/shared/04_SDKS/NDP48-DevPack-ENU.exe
set devPackChsUrl=http://192.168.60.201:8090/chfs/shared/04_SDKS/NDP48-DevPack-CHS.exe
set gitUrl=http://192.168.60.201:8090/chfs/shared/04_SDKS/PortableGit-2.48.1-64-bit.zip
if not exist "%tempDir%" mkdir "%tempDir%"

set compile_mode=Release
set csproj_name=Sources\Cuga-Calibration-Solution\Cuga-Calibration-Solution.csproj
set csproj_output_name=Sources\Cuga-Calibration-Solution\bin\%compile_mode%\publish\net480\win-x64

echo.
echo.
echo ==============================================================================================================
echo 0. check runtime
echo.
setlocal

:Net
if exist "%tempDir%\dotnet-sdk-8.0.303-win-x64\dotnet.exe" (
    echo .NET SDK 8.0 is already installed.
    goto :NetFramework
) else (
    echo .NET SDK 8.0 is not installed.
    echo .
    echo Click Continue to install .NET SDK 8.0
    pause

    echo Downloading .NET SDK 8.0.303
    if not exist "%tempDir%\dotnet-sdk-8.0.303-win-x64.zip" curl -o "%tempDir%\dotnet-sdk-8.0.303-win-x64.zip" "%sdkUrl%"
    echo Installing .NET SDK 8.0.303
    powershell -command "Expand-Archive -Path '%tempDir%\dotnet-sdk-8.0.303-win-x64.zip' -DestinationPath '%tempDir%\dotnet-sdk-8.0.303-win-x64' -Force"
    goto :Net
)

:NetFramework
for /f "tokens=2*" %%A in ('reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release 2^>nul') do set "release=%%B"

if not defined release (
    echo .NET Framework Version 4.5 or later is not installed.
    echo.
    goto :NetFrameworkDownload
)

set "version="
if %release% geq 533320 set "version=4.8.1 or later"
if %release% geq 528040 if %release% lss 533320 set "version=4.8"
if %release% geq 461808 if %release% lss 528040 set "version=4.7.2"
if %release% geq 461308 if %release% lss 461808 set "version=4.7.1"
if %release% geq 460798 if %release% lss 461308 set "version=4.7"
if %release% geq 394802 if %release% lss 460798 set "version=4.6.2"
if %release% geq 394254 if %release% lss 394802 set "version=4.6.1"
if %release% geq 393295 if %release% lss 394254 set "version=4.6"
if %release% geq 379893 if %release% lss 393295 set "version=4.5.2"
if %release% geq 378675 if %release% lss 379893 set "version=4.5.1"
if %release% geq 378389 if %release% lss 378675 set "version=4.5"

if defined version if "%version%" geq "4.8" (
    echo .NET Framework Version is sufficient: %version%
    goto :Git
) else (
    :NetFrameworkDownload
    echo .NET Framework 4.8 is not installed.
    echo .
    echo Click Continue to install .NET Framework 4.8
    pause

    echo Downloading .NET Framework 4.8 Developer Pack (ENU)
    if not exist "%tempDir%\ndp48-devpack-enu.exe" curl -o "%tempDir%\ndp48-devpack-enu.exe" "%devPackEnUrl%"
    echo Downloading .NET Framework 4.8 Developer Pack (CHS)
    if not exist "%tempDir%\ndp48-devpack-chs.exe" curl -o "%tempDir%\ndp48-devpack-chs.exe" "%devPackChsUrl%"

    echo Installing .NET Framework 4.8 Developer Pack (ENU)
    "%tempDir%\ndp48-devpack-enu.exe" /quiet /norestart
    echo Installing .NET Framework 4.8 Developer Pack (CHS)
    "%tempDir%\ndp48-devpack-chs.exe" /quiet /norestart
    goto :NetFramework
)

:Git
if exist "%tempDir%\PortableGit-2.48.1-64-bit\PortableGit-2.48.1-64-bit\bin\git.exe" (
    echo Git is already installed.
    goto :End
) else (
    echo Git is not installed.
    echo .
    echo Click Continue to install Git
    pause

    echo Downloading Git
    if not exist "%tempDir%\PortableGit-2.48.1-64-bit.zip" curl -o "%tempDir%\PortableGit-2.48.1-64-bit.zip" "%gitUrl%"
    echo Installing Git
    powershell -command "Expand-Archive -Path '%tempDir%\PortableGit-2.48.1-64-bit.zip' -DestinationPath '%tempDir%\PortableGit-2.48.1-64-bit' -Force"

   if exist "%tempDir%\PortableGit-2.48.1-64-bit\PortableGit-2.48.1-64-bit\etc\gitconfig" (
        del "%tempDir%\PortableGit-2.48.1-64-bit\PortableGit-2.48.1-64-bit\etc\gitconfig"
    )
    (
        echo [core]
        echo     symlinks = false
        echo     autocrlf = true
        echo     fscache = true
        echo [color]
        echo     interactive = true
        echo     ui = auto
        echo [help]
        echo     format = html
        echo [diff "astextplain"]
        echo     textconv = astextplain
        echo [rebase]
        echo     autosquash = true
        echo [filter "lfs"]
        echo     clean = git-lfs clean -- %%f
        echo     smudge = git-lfs smudge -- %%f
        echo     process = git-lfs filter-process
        echo     required = true
        echo [credential]
        echo     helper = !\"%tempDir:\=/%/PortableGit-2.48.1-64-bit/PortableGit/mingw64/bin/git-credential-manager.exe\"
    ) > "%tempDir%\PortableGit-2.48.1-64-bit\PortableGit-2.48.1-64-bit\etc\gitconfig"

    echo gitconfig has been updated.
    goto :Git
)

:Error
echo Check runtime error
pause
goto :EOF

:End
echo Check runtime ok
endlocal

echo.
echo.
echo ==============================================================================================================
echo 1. pull code from server
echo.

set is_already_clone=1
if not exist %repository_path% (
	set is_already_clone=0
)
if not exist %repository_path%\%csproj_name% (
	set is_already_clone=0
)

set "TEMP_KEY=%TEMP%\git_temp_key"

powershell -NoProfile -Command ^
"$key = [System.Text.StringBuilder]::new(); ^
$null = $key.Append('-----BEGIN OPENSSH PRIVATE KEY-----'); ^
$null = $key.Append([char]10); ^
$null = $key.Append('b3BlbnNzaC1rZXktdjEAAAAABG5vbmUAAAAEbm9uZQAAAAAAAAABAAAAMwAAAAtzc2gtZW'); ^
$null = $key.Append([char]10); ^
$null = $key.Append('QyNTUxOQAAACC2FJ37gWFHTjVv6KQX4fLibS1OPby9d104BlmfW/JRFAAAAJBYpMyuWKTM'); ^
$null = $key.Append([char]10); ^
$null = $key.Append('rgAAAAtzc2gtZWQyNTUxOQAAACC2FJ37gWFHTjVv6KQX4fLibS1OPby9d104BlmfW/JRFA'); ^
$null = $key.Append([char]10); ^
$null = $key.Append('AAAEDMNbGHQObGtNmSfpoAKJD308veOOvcF37Knf8pBGaSjLYUnfuBYUdONW/opBfh8uJt'); ^
$null = $key.Append([char]10); ^
$null = $key.Append('LU49vL13XTgGWZ9b8lEUAAAADERFTExAREVMTC1QQwE='); ^
$null = $key.Append([char]10); ^
$null = $key.Append('-----END OPENSSH PRIVATE KEY-----'); ^
$null = $key.Append([char]10); ^
[IO.File]::WriteAllText('%TEMP_KEY%', $key.ToString())"

set "TEMP_KEY_FOR_GIT=%TEMP_KEY:\=/%"
echo "%TEMP_KEY_FOR_GIT%"

set "GIT_SSH_COMMAND=ssh -i '%TEMP_KEY_FOR_GIT%' -o IdentitiesOnly=yes -o StrictHostKeyChecking=no"

if %is_already_clone%==0 (
	mkdir %repository_path%
	echo clone repository from server
	%tempDir%\PortableGit-2.48.1-64-bit\PortableGit-2.48.1-64-bit\bin\git.exe clone --recursive -b %brance_name% %repository_url% %repository_path%
)

cd /d %repository_path%
%tempDir%\PortableGit-2.48.1-64-bit\PortableGit-2.48.1-64-bit\bin\git.exe reset --hard
%tempDir%\PortableGit-2.48.1-64-bit\PortableGit-2.48.1-64-bit\bin\git.exe clean -xdf
%tempDir%\PortableGit-2.48.1-64-bit\PortableGit-2.48.1-64-bit\bin\git.exe checkout %brance_name%
%tempDir%\PortableGit-2.48.1-64-bit\PortableGit-2.48.1-64-bit\bin\git.exe pull --recurse-submodules
%tempDir%\PortableGit-2.48.1-64-bit\PortableGit-2.48.1-64-bit\bin\git.exe config --local user.name "%git_user%"
%tempDir%\PortableGit-2.48.1-64-bit\PortableGit-2.48.1-64-bit\bin\git.exe config --local user.email %git_user%@semixchina.com

echo.
echo.
echo ==============================================================================================================
echo 2. compiling program 

echo.
echo csproj_path: %repository_path%\%csproj_name%
echo csproj_output_path: %repository_path%\%csproj_output_name%
echo.

if /I %compile_mode%=="Debug" (
	echo.
	echo.
	echo it will take a while to compile, take a coffee break ...
	%tempDir%\dotnet-sdk-8.0.303-win-x64\dotnet.exe publish %repository_path%\%csproj_name% -p:DefineConstants="TRACE%%3BDEBUG%%3BNETFRAMEWORK%%3BNET48%%3BSIMULATOR" --configuration Debug --framework net480 --runtime win-x64 --no-self-contained --output %repository_path%\%csproj_output_name%
) else (
	echo.
	echo.
	echo it will take a while to compile, take a coffee break ...
	%tempDir%\dotnet-sdk-8.0.303-win-x64\dotnet.exe publish %repository_path%\%csproj_name% -p:DefineConstants="TRACE%%3BRELEASE%%3BNETFRAMEWORK%%3BNET48%%3BSIMULATOR" --configuration Release --framework net480 --runtime win-x64 --no-self-contained --output %repository_path%\%csproj_output_name%
)

echo.
echo.
echo.
echo.
echo ***** Please verify the compile is successed !!! if not close the window to abort update !!! *****
echo ***** Please verify the compile is successed !!! if not close the window to abort update !!! *****
echo ***** Please verify the compile is successed !!! if not close the window to abort update !!! *****
echo.
echo.
echo.
echo.
pause

echo ==============================================================================================================
echo 3. copy program to apps directory

set out_path=%repository_path%\%csproj_output_name%
set nowtime=%DATE:~0,4%%DATE:~5,2%%DATE:~8,2%%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%
set nowtime=%nowtime: =0%

if not exist %apps_path% (
	mkdir %apps_path%
) else (
	cd /d %apps_home%
	ren %apps_name% %apps_name%_%nowtime%
	echo The backup is complete: %apps_home%\%apps_name%_%nowtime%
	mkdir %apps_path%
)

xcopy /e/q/y %out_path% %apps_path%

echo.
echo.
echo **************************************************************************************************************
echo *                                                                                                            *
echo *                                         We are finished !!!                                                *
echo *                                                                                                            *
echo *                                *_* Please start your performance *_*                                       *
echo *                                                                                                            *
echo **************************************************************************************************************
echo.
echo.
echo Publish OK, Software Path: %apps_home%\%apps_name%\Cuga-Calibration-Solution.exe
pause