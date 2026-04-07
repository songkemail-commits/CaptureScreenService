Write-Host "========================================" -ForegroundColor Green
Write-Host "CaptureScreenService Installer Builder" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host

# Set working directory
$currentDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# Step 1: Build main service
Write-Host "[1/5] Building main service..." -ForegroundColor Cyan
Set-Location "..\CaptureScreenService"
dotnet publish -c Release -r win-x64 --self-contained true
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build main service!" -ForegroundColor Red
    pause
    exit 1
}

# Step 2: Build watchdog
Write-Host "[2/5] Building watchdog..." -ForegroundColor Cyan
Set-Location "..\Watchdog"
dotnet publish -c Release -r win-x64 --self-contained true
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build watchdog!" -ForegroundColor Red
    pause
    exit 1
}

# Step 3: Build uninstaller
Write-Host "[3/5] Building uninstaller..." -ForegroundColor Cyan
Set-Location "..\Uninstaller"
dotnet publish -c Release -r win-x64 --self-contained true
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build uninstaller!" -ForegroundColor Red
    pause
    exit 1
}

# Step 4: Prepare service files for embedding
Write-Host "[4/5] Preparing service files for embedding..." -ForegroundColor Cyan
Set-Location "..\Installer"
if (Test-Path "ServiceFiles") {
    Remove-Item "ServiceFiles" -Recurse -Force
}
New-Item "ServiceFiles" -ItemType Directory -Force

Write-Host "Copying CaptureScreenService files..."
Copy-Item "..\CaptureScreenService\bin\Release\net9.0\win-x64\publish\mossvc.exe" "ServiceFiles\" -Force
Copy-Item "..\CaptureScreenService\bin\Release\net9.0\win-x64\publish\*.dll" "ServiceFiles\" -Force
Copy-Item "..\CaptureScreenService\bin\Release\net9.0\win-x64\publish\*.json" "ServiceFiles\" -Force

Write-Host "Copying Watchdog files..."
Copy-Item "..\Watchdog\bin\Release\net9.0-windows\win-x64\publish\SystemHealthSvc.exe" "ServiceFiles\" -Force
Copy-Item "..\Watchdog\bin\Release\net9.0-windows\win-x64\publish\*.dll" "ServiceFiles\" -Force

Write-Host "Copying Uninstaller files..."
Copy-Item "..\Uninstaller\bin\Release\net9.0-windows\win-x64\publish\uninstall.exe" "ServiceFiles\" -Force

Write-Host "Copying EULA..."
Copy-Item "..\CaptureScreenService\EULA.txt" "ServiceFiles\" -Force

Write-Host "ServiceFiles directory contents:"
get-childitem "ServiceFiles"

# Step 5: Build installer
Write-Host "[5/5] Building installer..." -ForegroundColor Cyan
dotnet publish -c Release -r win-x64 --self-contained true
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build installer!" -ForegroundColor Red
    pause
    exit 1
}

# Clean up embedded files
Write-Host "Cleaning up embedded files..."
if (Test-Path "ServiceFiles") {
    Remove-Item "ServiceFiles" -Recurse -Force
}

Write-Host
Write-Host "========================================" -ForegroundColor Green
Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "Installer location: bin\Release\net9.0-windows\win-x64\publish\install.exe" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
pause