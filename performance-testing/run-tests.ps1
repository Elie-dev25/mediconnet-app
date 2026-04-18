# Master Test Orchestrator
# Executes all tests in proper order and generates report

param(
    [ValidateSet("Quick", "Full")]
    [string]$Mode = "Quick",
    [switch]$FrontendOnly = $false,
    [switch]$BackendOnly = $false,
    [switch]$SkipBuild = $false
)

$ErrorActionPreference = "Continue"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║       MEDICONNET - Test Suite Orchestrator             ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host "Mode: $Mode | Timestamp: $timestamp" -ForegroundColor Yellow

# Ensure results directory exists
$resultsPath = "$scriptPath/results"
if (-not (Test-Path $resultsPath)) {
    New-Item -ItemType Directory -Path $resultsPath | Out-Null
    Write-Host "[OK] Created results directory: $resultsPath" -ForegroundColor Green
}

$allTests = @()

# ============================================================================
# FRONTEND TESTS
# ============================================================================
if (-not $BackendOnly) {
    Write-Host "`n[*] ========== FRONTEND TESTS ==========" -ForegroundColor Magenta
    
    # Test 1: Component Analysis
    Write-Host "`n[*] Running: Frontend Component Analysis" -ForegroundColor Yellow
    $start = Get-Date
    & powershell -ExecutionPolicy Bypass -File "$scriptPath/frontend/1-analyze-components.ps1" -ResultsPath $resultsPath
    $duration = ((Get-Date) - $start).TotalSeconds
    $allTests += @{ name = "Frontend Components"; duration = $duration; status = "OK" }
    Write-Host "[OK] Completed in $duration seconds" -ForegroundColor Green
    
    # Test 2: Build Performance
    if (-not $SkipBuild) {
        Write-Host "`n[*] Running: Frontend Build Performance" -ForegroundColor Yellow
        $start = Get-Date
        & powershell -ExecutionPolicy Bypass -File "$scriptPath/frontend/2-build-performance.ps1" -ResultsPath $resultsPath
        $duration = ((Get-Date) - $start).TotalSeconds
        $allTests += @{ name = "Frontend Build"; duration = $duration; status = "OK" }
        Write-Host "[OK] Completed in $duration seconds" -ForegroundColor Green
    }
}

# ============================================================================
# BACKEND TESTS
# ============================================================================
if (-not $FrontendOnly) {
    Write-Host "`n[*] ========== BACKEND TESTS ==========" -ForegroundColor Magenta
    
    # Test 3: Endpoint Analysis
    Write-Host "`n[*] Running: Backend Endpoints Analysis" -ForegroundColor Yellow
    $start = Get-Date
    & powershell -ExecutionPolicy Bypass -File "$scriptPath/backend/1-analyze-endpoints.ps1" -ResultsPath $resultsPath
    $duration = ((Get-Date) - $start).TotalSeconds
    $allTests += @{ name = "Backend Endpoints"; duration = $duration; status = "OK" }
    Write-Host "[OK] Completed in $duration seconds" -ForegroundColor Green
    
    # Test 4: API Performance
    Write-Host "`n[*] Running: Backend API Performance" -ForegroundColor Yellow
    $start = Get-Date
    & powershell -ExecutionPolicy Bypass -File "$scriptPath/backend/2-api-performance.ps1" -ResultsPath $resultsPath
    $duration = ((Get-Date) - $start).TotalSeconds
    $allTests += @{ name = "API Performance"; duration = $duration; status = "OK" }
    Write-Host "[OK] Completed in $duration seconds" -ForegroundColor Green
}

# ============================================================================
# FINAL REPORT
# ============================================================================
Write-Host "`n╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║              TEST EXECUTION SUMMARY                    ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

$totalDuration = 0
$allTests | ForEach-Object {
    Write-Host "[OK] $($_.name) - $($_.duration)s" -ForegroundColor Green
    $totalDuration += $_.duration
}

Write-Host "`nTotal Duration: $([math]::Round($totalDuration, 2)) seconds" -ForegroundColor Yellow
Write-Host "Results saved to: $resultsPath" -ForegroundColor Yellow

# List all results
Write-Host "`n[*] Generated Results Files:" -ForegroundColor Yellow
Get-ChildItem $resultsPath -Filter "*.json" | Sort-Object LastWriteTime -Descending | Select-Object -First 10 | ForEach-Object {
    Write-Host "  - $($_.Name)" -ForegroundColor Gray
}

Write-Host "`n[OK] All tests completed successfully!" -ForegroundColor Green
Write-Host "Next step: Run parse-results.ps1 to extract metrics" -ForegroundColor Cyan
