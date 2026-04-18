# ============================================================================
# Complete Performance Testing Orchestrator
# ============================================================================
# Exécute tous les tests et génère un rapport

param(
    [switch]$Frontend,
    [switch]$Backend,
    [switch]$LoadTest,
    [switch]$All
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   MEDICONNET - COMPLETE PERFORMANCE TEST SUITE           ║" -ForegroundColor Cyan
Write-Host "║   Timestamp: $timestamp                        ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

# Si aucun paramètre, afficher l'aide
if (-not $Frontend -and -not $Backend -and -not $LoadTest -and -not $All) {
    Write-Host "`n📋 Usage:" -ForegroundColor Yellow
    Write-Host "   .\run-all-tests.ps1 -All              # Exécuter tous les tests"
    Write-Host "   .\run-all-tests.ps1 -Frontend         # Tests frontend uniquement"
    Write-Host "   .\run-all-tests.ps1 -Backend          # Tests backend uniquement"
    Write-Host "   .\run-all-tests.ps1 -LoadTest         # Tests de charge uniquement"
    Write-Host "`n" -ForegroundColor White
    exit 1
}

$resultsPath = "./results"
if (!(Test-Path $resultsPath)) {
    New-Item -ItemType Directory -Path $resultsPath -Force | Out-Null
}

# ============================================================================
# FRONTEND TESTS
# ============================================================================

if ($All -or $Frontend) {
    Write-Host "`n`n" -ForegroundColor White
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "▶ FRONTEND PERFORMANCE TESTS" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    
    & .\frontend\run-performance-tests.ps1 -ResultsPath $resultsPath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n❌ Frontend tests failed" -ForegroundColor Red
    } else {
        Write-Host "`n✅ Frontend tests completed" -ForegroundColor Green
    }
}

# ============================================================================
# BACKEND TESTS
# ============================================================================

if ($All -or $Backend) {
    Write-Host "`n`n" -ForegroundColor White
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "▶ BACKEND API PERFORMANCE TESTS" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    
    & .\backend\run-api-tests.ps1 -ResultsPath $resultsPath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n❌ Backend tests failed" -ForegroundColor Red
    } else {
        Write-Host "`n✅ Backend tests completed" -ForegroundColor Green
    }
}

# ============================================================================
# LOAD TESTS
# ============================================================================

if ($All -or $LoadTest) {
    Write-Host "`n`n" -ForegroundColor White
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "▶ LOAD TESTING (k6)" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    
    & .\load-testing\run-load-tests.ps1 -ResultsPath $resultsPath -TestType "standard"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n❌ Load tests failed" -ForegroundColor Red
    } else {
        Write-Host "`n✅ Load tests completed" -ForegroundColor Green
    }
}

# ============================================================================
# GENERATE SUMMARY REPORT
# ============================================================================

Write-Host "`n`n" -ForegroundColor White
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "▶ RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

$results = Get-ChildItem $resultsPath -Filter "*metrics*.json" -ErrorAction SilentlyContinue | 
    Where-Object { $_.LastWriteTime -gt (Get-Date).AddHours(-1) }

if ($results) {
    Write-Host "`n📊 Test results found:" -ForegroundColor Green
    foreach ($file in $results) {
        Write-Host "   ✓ $($file.Name)" -ForegroundColor Gray
    }
} else {
    Write-Host "`n⚠️  No recent test results found" -ForegroundColor Yellow
}

Write-Host "`n📁 All results saved to: $(Resolve-Path $resultsPath)" -ForegroundColor Cyan

Write-Host "`n" -ForegroundColor White
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   ✅ PERFORMANCE TESTING COMPLETE                        ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

Write-Host "`n📝 Next steps:" -ForegroundColor Yellow
Write-Host "   1. Review results in results/ folder" -ForegroundColor Gray
Write-Host "   2. Extract key metrics" -ForegroundColor Gray
Write-Host "   3. Update readme-result.md with real data" -ForegroundColor Gray
Write-Host "   4. Use metrics for portfolio" -ForegroundColor Gray

Write-Host "`n" -ForegroundColor White
