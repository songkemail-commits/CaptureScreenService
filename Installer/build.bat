@echo off
echo ========================================
echo CaptureScreenService Installer Builder
echo ========================================
echo.

cd /d "%~dp0"

echo [1/5] Building main service...
cd ..\CaptureScreenService
dotnet publish -c Release -r win-x64 --self-contained true
if %errorlevel% neq 0 (
    echo Failed to build main service!
    pause
    exit /b 1
)

echo.
echo [2/5] Building watchdog...
cd ..\Watchdog
dotnet publish -c Release -r win-x64 --self-contained true
if %errorlevel% neq 0 (
    echo Failed to build watchdog!
    pause
    exit /b 1
)

echo.
echo [3/5] Building uninstaller...
cd ..\Uninstaller
dotnet publish -c Release -r win-x64 --self-contained true
if %errorlevel% neq 0 (
    echo Failed to build uninstaller!
    pause
    exit /b 1
)

echo.
echo [4/5] Preparing service files for embedding...
cd ..\Installer
if exist "ServiceFiles" rmdir /s /q "ServiceFiles"
mkdir "ServiceFiles"

echo Copying CaptureScreenService files...
xcopy /y /q "..\CaptureScreenService\bin\Release\net9.0\win-x64\publish\CaptureScreenService.exe" "ServiceFiles\"
xcopy /y /q "..\CaptureScreenService\bin\Release\net9.0\win-x64\publish\*.dll" "ServiceFiles\"
xcopy /y /q "..\CaptureScreenService\bin\Release\net9.0\win-x64\publish\*.json" "ServiceFiles\"

echo Copying Watchdog files...
xcopy /y /q "..\Watchdog\bin\Release\net9.0-windows\win-x64\publish\SystemHealthSvc.exe" "ServiceFiles\"
xcopy /y /q "..\Watchdog\bin\Release\net9.0-windows\win-x64\publish\*.dll" "ServiceFiles\"

echo Copying Uninstaller files...
xcopy /y /q "..\Uninstaller\bin\Release\net9.0-windows\win-x64\publish\uninstall.exe" "ServiceFiles\"

dir ServiceFiles

echo.
echo [5/5] Building installer...
dotnet publish -c Release -r win-x64 --self-contained true
if %errorlevel% neq 0 (
    echo Failed to build installer!
    pause
    exit /b 1
)

echo.
echo Cleaning up embedded files...
rmdir /s /q "ServiceFiles"

echo.
echo ========================================
echo Build completed successfully!
echo Installer location: bin\Release\net9.0-windows\win-x64\publish\install.exe
echo ========================================
pause
