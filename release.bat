@echo off
setlocal enabledelayedexpansion
echo.
echo ============================================
echo   Finalmouse Polling Rate Switcher - Release
echo ============================================
echo.

:: Set gh path
set "GH=C:\Program Files\GitHub CLI\gh.exe"
if not exist "%GH%" (
    echo ERROR: GitHub CLI not found at %GH%
    pause
    exit /b 1
)

:: Get latest tag from GitHub using JSON output for reliable parsing
echo Fetching latest release...
set LATEST_TAG=
for /f "delims=" %%a in ('"%GH%" release list --repo xBambooz/FinalmousePollingRateSwitcher --limit 1 --json tagName -q ".[0].tagName" 2^>nul') do set LATEST_TAG=%%a

if "%LATEST_TAG%"=="" (
    echo No existing releases found, starting at v1.0.0
    set LATEST_TAG=v1.0.0
)

echo Latest release: %LATEST_TAG%

:: Parse version numbers (strip the 'v' prefix)
set VER=%LATEST_TAG:v=%
for /f "tokens=1,2,3 delims=." %%a in ("%VER%") do (
    set MAJOR=%%a
    set MINOR=%%b
    set PATCH=%%c
)

:: Bump patch version
set /a PATCH=%PATCH%+1
set NEW_VER=v!MAJOR!.!MINOR!.!PATCH!

echo New version: !NEW_VER!
echo.

:: Ask for release description
echo Type release notes (what changed in this version):
set /p DESC="Description: "
if "!DESC!"=="" (
    echo ERROR: Description cannot be empty.
    pause
    exit /b 1
)

:: Check publish folder exists
if not exist publish\FinalmousePollingService.exe (
    echo ERROR: publish folder missing! Run build.bat first.
    pause
    exit /b 1
)

:: Create zip
echo.
echo Zipping publish files...
if exist FinalmousePollingRateSwitcher.zip del /q FinalmousePollingRateSwitcher.zip
powershell -Command "Compress-Archive -Path 'publish\*' -DestinationPath 'FinalmousePollingRateSwitcher.zip'"

if not exist FinalmousePollingRateSwitcher.zip (
    echo ERROR: Failed to create zip!
    pause
    exit /b 1
)

:: Create GitHub release
echo Creating release !NEW_VER!...
"%GH%" release create !NEW_VER! FinalmousePollingRateSwitcher.zip --repo xBambooz/FinalmousePollingRateSwitcher --title "FinalmousePollingRateSwitcher !NEW_VER!" --notes "!DESC!"

if errorlevel 1 (
    echo ERROR: Release creation failed!
    pause
    exit /b 1
)

:: Clean up zip
del /q FinalmousePollingRateSwitcher.zip

echo.
echo ============================================
echo        RELEASE !NEW_VER! PUBLISHED
echo ============================================
echo.
echo https://github.com/xBambooz/FinalmousePollingRateSwitcher/releases/tag/!NEW_VER!
echo.
pause
