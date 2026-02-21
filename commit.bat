@echo off
setlocal
echo.
echo ============================================
echo   Finalmouse Polling Rate Switcher - Commit
echo ============================================
echo.

:: Check for git
git --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: git not found!
    pause
    exit /b 1
)

:: Show status
echo Changes:
echo.
git status --short
echo.

:: Ask for commit message
set /p MSG="Commit message: "
if "%MSG%"=="" (
    echo ERROR: Commit message cannot be empty.
    pause
    exit /b 1
)

:: Stage, commit, push
git add -A
git commit -m "%MSG%"
if errorlevel 1 (
    echo ERROR: Commit failed!
    pause
    exit /b 1
)

git push
if errorlevel 1 (
    echo ERROR: Push failed!
    pause
    exit /b 1
)

echo.
echo ============================================
echo           PUSHED TO GITHUB
echo ============================================
echo.
pause
