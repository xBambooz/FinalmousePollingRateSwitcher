@echo off
setlocal
echo.
echo ============================================
echo   Finalmouse Polling Rate Switcher - Build
echo ============================================
echo.

:: Check for dotnet
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET 8 SDK not found!
    echo Download from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

set OUTPUT=publish

:: Kill running exes if they exist (locks the exe and blocks build)
taskkill /F /IM FinalmousePollingService.exe >nul 2>&1
taskkill /F /IM FinalmousePollingRateConfig.exe >nul 2>&1

echo [1/3] Cleaning previous builds...
if exist %OUTPUT% rmdir /s /q %OUTPUT%
mkdir %OUTPUT%

echo [2/3] Building Service (single-file, self-contained)...
dotnet publish src\PollingService\PollingService.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o %OUTPUT% ^
    --nologo -v quiet

if errorlevel 1 (
    echo ERROR: Service build failed!
    pause
    exit /b 1
)

echo [3/3] Building Config UI (single-file, self-contained)...
dotnet publish src\ConfigUI\ConfigUI.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o %OUTPUT% ^
    --nologo -v quiet

if errorlevel 1 (
    echo ERROR: Config UI build failed!
    pause
    exit /b 1
)

:: Clean up unnecessary .pdb files
del /q %OUTPUT%\*.pdb >nul 2>&1

echo.
echo ============================================
echo             BUILD COMPLETE
echo ============================================
echo.
echo Output: %OUTPUT%\
echo.
echo Files:
echo   FinalmousePollingService.exe   - Windows Service (background)
echo   FinalmousePollingRateConfig.exe - Config UI (run as admin)
echo.
echo To distribute: zip the contents of the %OUTPUT% folder.
echo Users just need these two .exe files - no runtime install needed.
echo.
pause
