# ============================================
# OpenPOS Installer Build Script
# ============================================
# Usage: powershell -ExecutionPolicy Bypass -File build-installer.ps1
#
# Prerequisites:
#   1. .NET 9 SDK installed
#   2. Inno Setup 6 installed (https://jrsoftware.org/isdownload.php)
#   3. PostgreSQL 17 installer .exe in this folder (optional, for bundling)
#
# This script:
#   1. Publishes the WPF app as self-contained for win-x64
#   2. Copies publish output to installer/publish/
#   3. Runs Inno Setup to create OpenPOS-Setup.exe

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  OpenPOS Installer Build" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$appDir = Join-Path (Split-Path -Parent $scriptDir) "openpos-app"
$publishDir = Join-Path $scriptDir "publish"
$outputDir = Join-Path $scriptDir "output"
$innoSetup = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

# Step 1: Publish the WPF app
Write-Host "[1/3] Publishing OpenPOS app (self-contained, win-x64)..." -ForegroundColor Yellow
Write-Host "       Source: $appDir" -ForegroundColor Gray

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

dotnet publish $appDir `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet publish failed!" -ForegroundColor Red
    exit 1
}

Write-Host "       Published successfully." -ForegroundColor Green
Write-Host ""

# Step 2: Check for PostgreSQL installer
$pgInstaller = Join-Path $scriptDir "postgresql-17-windows-x64.exe"
if (Test-Path $pgInstaller) {
    Write-Host "[INFO] PostgreSQL installer found. It will be bundled." -ForegroundColor Green
} else {
    Write-Host "[WARN] PostgreSQL installer NOT found at:" -ForegroundColor Yellow
    Write-Host "       $pgInstaller" -ForegroundColor Gray
    Write-Host "       Download it from: https://www.enterprisedb.com/downloads/postgres-postgresql-downloads" -ForegroundColor Gray
    Write-Host "       Place the .exe in this folder and rename to: postgresql-17-windows-x64.exe" -ForegroundColor Gray
    Write-Host "       The installer will still work but won't auto-install PostgreSQL." -ForegroundColor Yellow
    Write-Host ""
}

# Step 3: Build installer with Inno Setup
Write-Host "[2/3] Building installer with Inno Setup..." -ForegroundColor Yellow

if (-not (Test-Path $innoSetup)) {
    Write-Host ""
    Write-Host "Inno Setup 6 not found at: $innoSetup" -ForegroundColor Red
    Write-Host ""
    Write-Host "To install Inno Setup:" -ForegroundColor Yellow
    Write-Host "  1. Download from: https://jrsoftware.org/isdownload.php" -ForegroundColor Gray
    Write-Host "  2. Install with default settings" -ForegroundColor Gray
    Write-Host "  3. Run this script again" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Alternatively, open OpenPOS.iss in Inno Setup and compile manually." -ForegroundColor Gray
    Write-Host ""
    Write-Host "The published app is ready at: $publishDir" -ForegroundColor Green
    exit 0
}

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

$issFile = Join-Path $scriptDir "OpenPOS.iss"
& $innoSetup $issFile

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Inno Setup compilation failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "[3/3] Done!" -ForegroundColor Green
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Installer created successfully!" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output: $outputDir\OpenPOS-Setup-1.0.0.exe" -ForegroundColor White
Write-Host ""
