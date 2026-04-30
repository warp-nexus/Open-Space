@echo off
REM open-space edit start
dotnet build-server shutdown >nul 2>nul
dotnet run --project Content.Client -p:UseSharedCompilation=false
REM open-space edit end
pause
