@echo off
setlocal

echo ========================================
echo  Realm of Reality - Build and Run
echo ========================================
echo.

REM Find MSBuild from Visual Studio 2022
set MSBUILD=
if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
)

REM Check if dotnet is available (preferred method)
where dotnet >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo [BUILD] Using dotnet CLI...
    echo.
    
    echo [1/3] Restoring packages...
    dotnet restore RealmOfReality.sln
    if %ERRORLEVEL% NEQ 0 (
        echo [ERROR] Package restore failed!
        pause
        exit /b 1
    )
    
    echo.
    echo [2/3] Building solution...
    dotnet build RealmOfReality.sln --configuration Debug --no-restore
    if %ERRORLEVEL% NEQ 0 (
        echo [ERROR] Build failed!
        pause
        exit /b 1
    )
    
    echo.
    echo [3/3] Starting Server and Client...
    goto :run
) else if defined MSBUILD (
    echo [BUILD] Using MSBuild from Visual Studio 2022...
    echo.
    
    echo [1/2] Building solution...
    "%MSBUILD%" RealmOfReality.sln /p:Configuration=Debug /m /verbosity:minimal
    if %ERRORLEVEL% NEQ 0 (
        echo [ERROR] Build failed!
        pause
        exit /b 1
    )
    
    echo.
    echo [2/2] Starting Server and Client...
    goto :run
) else (
    echo [ERROR] Neither dotnet CLI nor Visual Studio 2022 MSBuild found!
    echo Please install .NET 8 SDK or Visual Studio 2022.
    pause
    exit /b 1
)

:run
echo.
echo ========================================
echo  Starting Game
echo ========================================
echo.

REM Start server in new window
echo Starting Server...
start "Realm of Reality - Server" cmd /c "cd /d "%~dp0Server\bin\Debug\net8.0" && RealmOfReality.Server.exe"

REM Wait a moment for server to initialize
echo Waiting for server to start...
timeout /t 2 /nobreak >nul

REM Start client in new window
echo Starting Client...
start "Realm of Reality - Client" cmd /c "cd /d "%~dp0Client\bin\Debug\net8.0" && RealmOfReality.Client.exe"

echo.
echo ========================================
echo  Game Running!
echo ========================================
echo.
echo Server and Client windows should now be open.
echo Close this window when done, or press any key to exit.
echo.
pause
