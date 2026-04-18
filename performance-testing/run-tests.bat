@echo off
REM Master Test Orchestrator - Simple Batch Version

setlocal enabledelayedexpansion

REM Timestamps and paths
for /f "tokens=2-4 delims=/ " %%a in ('date /t') do (set dateStr=%%c%%a%%b)
for /f "tokens=1-2 delims=/:" %%a in ('time /t') do (set timeStr=%%a%%b)
set timestamp=!dateStr!_!timeStr!
set scriptsPath=%~dp0

echo.
echo ============================================================
echo.  MEDICONNET - Test Suite Orchestrator
echo.  Timestamp: !timestamp!
echo.
echo ============================================================
echo.

REM Create results directory
if not exist "!scriptsPath!results" mkdir "!scriptsPath!results"

REM Frontend Component Analysis
echo [*] Running: Frontend Component Analysis
powershell -ExecutionPolicy Bypass -File "!scriptsPath!frontend\1-analyze-components.ps1" -ResultsPath "!scriptsPath!results"

REM Frontend Build Performance
echo.
echo [*] Running: Frontend Build Performance
powershell -ExecutionPolicy Bypass -File "!scriptsPath!frontend\2-build-performance.ps1" -ResultsPath "!scriptsPath!results"

REM Backend Endpoint Analysis
echo.
echo [*] Running: Backend Endpoints Analysis
powershell -ExecutionPolicy Bypass -File "!scriptsPath!backend\1-analyze-endpoints.ps1" -ResultsPath "!scriptsPath!results"

REM Backend API Performance
echo.
echo [*] Running: Backend API Performance
powershell -ExecutionPolicy Bypass -File "!scriptsPath!backend\2-api-performance.ps1" -ResultsPath "!scriptsPath!results"

echo.
echo ============================================================
echo [OK] All tests completed!
echo Results saved to: !scriptsPath!results
echo.
echo Generated results files:
dir "!scriptsPath!results\*.json"
echo.
echo ============================================================
echo.

endlocal
pause
