@echo off
setlocal

set "ROOT=%~dp0"
set "APP_DIR=%ROOT%src\PersonalFinance.Web"
set "APP_PROJECT=%ROOT%src\PersonalFinance.Web\PersonalFinance.Web.csproj"
set "CADDYFILE=%ROOT%deploy\Caddyfile"
set "CADDY_EXE=%LOCALAPPDATA%\Programs\Caddy\caddy.exe"
set "APP_URL=https://127.0.0.1/"

if not exist "%APP_PROJECT%" (
    echo Could not find the web project:
    echo   "%APP_PROJECT%"
    pause
    exit /b 1
)

if not exist "%CADDYFILE%" (
    echo Could not find the Caddy configuration:
    echo   "%CADDYFILE%"
    pause
    exit /b 1
)

if not exist "%CADDY_EXE%" set "CADDY_EXE="
if not defined CADDY_EXE (
    for /f "delims=" %%I in ('where caddy.exe 2^>nul') do if not defined CADDY_EXE set "CADDY_EXE=%%I"
)

if not defined CADDY_EXE (
    echo Could not find caddy.exe. Install Caddy or add it to PATH, then try again.
    pause
    exit /b 1
)

echo Starting Personal Finance Web and Caddy in separate Command Prompt windows...
start "Personal Finance Web" cmd.exe /k "cd /d ""%APP_DIR%"" && set ASPNETCORE_ENVIRONMENT=Development && dotnet run --no-launch-profile"
start "Caddy - Personal Finance" cmd.exe /k ""%CADDY_EXE%" run --config "%CADDYFILE%" --adapter caddyfile"

echo Waiting briefly for the services to start, then opening %APP_URL%
timeout /t 4 /nobreak >nul
start "" "%APP_URL%"

echo The app and Caddy windows will stay open. Close them or press Ctrl+C in each to stop the services.
endlocal
