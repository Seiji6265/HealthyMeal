# HealthyMeal Beta Build Script
# Builds and packages the application for beta testing with security measures

param(
    [string]$Version = "1.2.0",
    [string]$VersionSuffix = "Beta",
    [string]$ProxyUrl = "https://healthymeal-api.yourdomain.com",
    [switch]$SkipTests = $false
)

$FullVersion = "$Version-$VersionSuffix"
Write-Host "Building HealthyMeal Beta v$FullVersion" -ForegroundColor Green
Write-Host "Proxy URL: $ProxyUrl" -ForegroundColor Yellow

# Set variables
$ProjectPath = "HealthyMeal.csproj"
$OutputPath = ".\publish\beta-$FullVersion"
$ZipFileName = "HealthyMeal-Beta-$FullVersion.zip"

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Blue
if (Test-Path $OutputPath) {
    Remove-Item -Recurse -Force $OutputPath
}

# Update proxy URL in GeminiAIService if provided
if ($ProxyUrl -ne "https://healthymeal-api.yourdomain.com") {
    Write-Host "Updating proxy URL..." -ForegroundColor Blue
    $ServiceFile = ".\Services\GeminiAIService.cs"
    $Content = Get-Content $ServiceFile -Raw
    $Content = $Content -replace 'private const string PROXY_BASE_URL = ".*?"', "private const string PROXY_BASE_URL = `"$ProxyUrl`""
    Set-Content -Path $ServiceFile -Value $Content
    Write-Host "Proxy URL updated to: $ProxyUrl" -ForegroundColor Green
}

# Build the application
Write-Host "Building application..." -ForegroundColor Blue
dotnet publish $ProjectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $OutputPath `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false `
    -p:AssemblyVersion=$Version `
    -p:FileVersion=$Version

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build completed successfully!" -ForegroundColor Green

# Create beta info file
Write-Host "Creating beta info file..." -ForegroundColor Blue
$BetaInfo = @"
HealthyMeal Beta v$FullVersion
Build Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Build Environment: Windows 11

SECURITY FEATURES:
- API keys are secured on remote server
- Client uses secure proxy communication
- Rate limiting protection
- Encrypted local database (SQLCipher)
- Session management with auto-expiration

BETA TESTING INSTRUCTIONS:
1. Extract all files to a folder
2. Run HealthyMeal.exe
3. If Windows SmartScreen appears, click "More info" -> "Run anyway"
4. Create an account or login
5. Test all features and report bugs

BETA LIMITATIONS:
- This is a test version, may contain bugs
- Data may be reset between beta versions
- Requires internet connection for meal planning
- Windows Defender may flag as unknown application

Report issues to: your-email@domain.com
Server Status: $ProxyUrl/health

Built with love for beta testers
"@

Set-Content -Path "$OutputPath\BetaInfo.txt" -Value $BetaInfo

# Create README for beta testers
$ReadMe = @"
# HealthyMeal Beta v$FullVersion

## Quick Start
1. Extract all files to a folder (e.g., C:\HealthyMeal-Beta)
2. Run **HealthyMeal.exe**
3. If Windows shows security warning:
   - Click "More info"
   - Click "Run anyway"
4. Create account and start testing!

## What to Test
- Account registration and login
- "Remember me" functionality
- Profile creation and editing
- Meal plan generation (1-7 days)
- Recipe browsing and editing
- Theme switching (Light/Dark)
- Data persistence between sessions

## Known Issues
- First run may take longer (Windows security scan)
- Antivirus may quarantine - add to exceptions if needed
- Internet connection required for AI features

## Feedback
Please report any bugs, crashes, or suggestions!
Email: your-email@domain.com

## System Requirements
- Windows 10/11 (64-bit)
- .NET 8.0 Runtime (included)
- Internet connection
- 50MB free disk space

Thank you for beta testing!
"@

Set-Content -Path "$OutputPath\README.md" -Value $ReadMe

# Create installation script
$InstallScript = @"
@echo off
echo HealthyMeal Beta v$FullVersion Installer
echo.

REM Create application directory
if not exist "%USERPROFILE%\AppData\Local\HealthyMeal-Beta" (
    mkdir "%USERPROFILE%\AppData\Local\HealthyMeal-Beta"
)

REM Copy files
echo Copying files...
xcopy /E /I /Y "." "%USERPROFILE%\AppData\Local\HealthyMeal-Beta\"

REM Create desktop shortcut
echo Creating desktop shortcut...
powershell -Command "& {`$WshShell = New-Object -comObject WScript.Shell; `$Shortcut = `$WshShell.CreateShortcut('%USERPROFILE%\Desktop\HealthyMeal Beta.lnk'); `$Shortcut.TargetPath = '%USERPROFILE%\AppData\Local\HealthyMeal-Beta\HealthyMeal.exe'; `$Shortcut.WorkingDirectory = '%USERPROFILE%\AppData\Local\HealthyMeal-Beta'; `$Shortcut.Description = 'HealthyMeal Beta v$FullVersion'; `$Shortcut.Save()}"

echo.
echo Installation completed!
echo You can now run HealthyMeal from your desktop or start menu.
echo.
pause
"@

Set-Content -Path "$OutputPath\install.bat" -Value $InstallScript

# Security hardening - remove debug files and symbols
Write-Host "Applying security hardening..." -ForegroundColor Blue
Get-ChildItem -Path $OutputPath -Filter "*.pdb" | Remove-Item -Force
Get-ChildItem -Path $OutputPath -Filter "*.xml" | Remove-Item -Force

# Create ZIP package
Write-Host "Creating ZIP package..." -ForegroundColor Blue
$ZipPath = ".\publish\$ZipFileName"
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($OutputPath, $ZipPath)

Write-Host "Package created: $ZipPath" -ForegroundColor Green

# Display package info
$ZipSize = [math]::Round((Get-Item $ZipPath).Length / 1MB, 2)
Write-Host "Package size: $ZipSize MB" -ForegroundColor Cyan

# Security summary
Write-Host "`nSECURITY SUMMARY:" -ForegroundColor Yellow
Write-Host "API keys secured on remote server" -ForegroundColor Green
Write-Host "Client API key embedded (rotatable)" -ForegroundColor Green
Write-Host "Database encrypted with SQLCipher" -ForegroundColor Green
Write-Host "Debug symbols removed" -ForegroundColor Green
Write-Host "Self-contained deployment" -ForegroundColor Green

Write-Host "`nBeta package ready for distribution!" -ForegroundColor Green
Write-Host "Location: $ZipPath" -ForegroundColor Cyan
Write-Host "Proxy Server: $ProxyUrl" -ForegroundColor Cyan

# Open folder
Start-Process explorer.exe -ArgumentList "/select,`"$(Resolve-Path $ZipPath)`"" 