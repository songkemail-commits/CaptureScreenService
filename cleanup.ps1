param(
    [string]$InstallPath = "C:\Program Files\CaptureScreenService",
    [switch]$Force
)

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CaptureScreenService Cleanup Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not $Force) {
    $confirm = Read-Host "Are you sure to cleanup CaptureScreenService? (Y/N)"
    if ($confirm -ne "Y" -and $confirm -ne "y") {
        Write-Host "Cancelled by user" -ForegroundColor Yellow
        exit 0
    }
}

Write-Host "Stopping processes..." -ForegroundColor White
Get-Process -Name "CaptureScreenService" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Get-Process -Name "ScreenCapWatchdog" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "Processes stopped" -ForegroundColor Green

Write-Host "Removing startup entries..." -ForegroundColor White
Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "ScreenCap" -Force -ErrorAction SilentlyContinue
Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "ScreenCapWatchdog" -Force -ErrorAction SilentlyContinue
Write-Host "Startup entries removed" -ForegroundColor Green

Write-Host "Removing registry entries..." -ForegroundColor White
Remove-Item -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\CaptureScreenService" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Registry entries removed" -ForegroundColor Green

Write-Host "Removing program files: $InstallPath" -ForegroundColor White
if (Test-Path $InstallPath) {
    Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Program files removed" -ForegroundColor Green
}
else {
    Write-Host "Install directory not found: $InstallPath" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Cleanup completed!" -ForegroundColor Green
Write-Host ""
