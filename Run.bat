@echo off
setlocal

echo ========================================
echo  Realm of Reality - Quick Run (No Build)
echo ========================================
echo.

REM Check if executables exist
if not exist "Server\bin\Debug\net8.0\RealmOfReality.Server.exe" (
    echo [ERROR] Server executable not found!
    echo Please build the solution first using BuildAndRun.bat or Visual Studio.
    pause
    exit /b 1
)

if not exist "Client\bin\Debug\net8.0\RealmOfReality.Client.exe" (
    echo [ERROR] Client executable not found!
    echo Please build the solution first using BuildAndRun.bat or Visual Studio.
    pause
    exit /b 1
)

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
echo Server: localhost:7775
echo Login: admin / admin
echo.
echo Close this window when done.
pause
