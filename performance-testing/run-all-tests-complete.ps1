# ============================================================================
# COMPLETE PERFORMANCE TEST SUITE - ENHANCED VERSION
# ============================================================================
# Exécute TOUS les tests: Frontend, Backend, Load, Lighthouse, Seed, Analysis

param(
    [switch]$Quick,          # Quick run (frontend + backend only)
    [switch]$Full,           # Full suite (tous les tests)
    [switch]$FrontendAnalysis,
    [switch]$BackendAnalysis,
    [switch]$LoadTest,
    [switch]$Lighthouse,
    [switch]$GenerateSeed,
    [switch]$All             # Alias pour Full
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

# Si aucun paramètre, afficher l'aide
if (-not $Quick -and -not $Full -and -not $FrontendAnalysis -and -not $BackendAnalysis `
    -and -not $LoadTest -and -not $Lighthouse -and -not $GenerateSeed -and -not $All) {
    
    Write-Host "`n╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║   MEDICONNET - COMPLETE PERFORMANCE TEST SUITE                     ║" -ForegroundColor Cyan
    Write-Host "║   Enhanced Version with Analysis & Metrics                         ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    
    Write-Host "`n📋 USAGE:`n" -ForegroundColor Yellow
    
    Write-Host "   Quick Run (15 min):" -ForegroundColor Green
    Write-Host "   .\run-all-tests-complete.ps1 -Quick" -ForegroundColor Gray
    Write-Host "      → Frontend + Backend performance tests only`n" -ForegroundColor Gray
    
    Write-Host "   Full Suite (45-60 min):" -ForegroundColor Green
    Write-Host "   .\run-all-tests-complete.ps1 -Full" -ForegroundColor Gray
    Write-Host "      → All tests + Lighthouse + Analysis`n" -ForegroundColor Gray
    
    Write-Host "   Individual Tests:" -ForegroundColor Green
    Write-Host "   .\run-all-tests-complete.ps1 -FrontendAnalysis    # Component & bundle analysis" -ForegroundColor Gray
    Write-Host "   .\run-all-tests-complete.ps1 -BackendAnalysis     # Endpoint & architecture analysis" -ForegroundColor Gray
    Write-Host "   .\run-all-tests-complete.ps1 -Lighthouse          # Lighthouse audit (performance score)" -ForegroundColor Gray
    Write-Host "   .\run-all-tests-complete.ps1 -LoadTest            # Load testing with k6" -ForegroundColor Gray
    Write-Host "   .\run-all-tests-complete.ps1 -GenerateSeed        # Generate test data (5000 patients)" -ForegroundColor Gray
    Write-Host "`n"
    
    exit 1
}

if ($All) { $Full = $true }

# ============================================================================
# TITLE
# ============================================================================

Write-Host "`n" -ForegroundColor White
Write-Host "╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   MEDICONNET PERFORMANCE TESTING SUITE - $timestamp              ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

$resultsPath = "./results"
if (!(Test-Path $resultsPath)) {
    New-Item -ItemType Directory -Path $resultsPath -Force | Out-Null
}

# ============================================================================
# QUICK RUN
# ============================================================================

if ($Quick) {
    Write-Host "`n🏃 QUICK RUN - Essential Tests Only (15-20 min)" -ForegroundColor Yellow
    
    # Frontend Performance
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "▶ FRONTEND PERFORMANCE" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    & .\frontend\run-performance-tests.ps1 -ResultsPath $resultsPath
    
    # Backend API Performance
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "▶ BACKEND API PERFORMANCE" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    & .\backend\run-api-tests.ps1 -ResultsPath $resultsPath
    
    $testCount = 2
    $totalTime = "15-20 min"
}

# ============================================================================
# FULL SUITE
# ============================================================================

if ($Full) {
    Write-Host "`n🚀 FULL TEST SUITE - Comprehensive Analysis (45-60 min)" -ForegroundColor Yellow
    
    $testCount = 0
    
    # 1. Frontend Performance
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "▶ 1. FRONTEND PERFORMANCE" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    & .\frontend\run-performance-tests.ps1 -ResultsPath $resultsPath
    $testCount++
    
    # 2. Backend API Performance
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "▶ 2. BACKEND API PERFORMANCE" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    & .\backend\run-api-tests.ps1 -ResultsPath $resultsPath
    $testCount++
    
    # 3. Frontend Analysis
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "▶ 3. FRONTEND ANALYSIS (Components, Services, etc.)" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    & .\frontend\analyze-components.ps1 -ResultsPath $resultsPath
    $testCount++
    
    # 4. Backend Analysis
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "▶ 4. BACKEND ANALYSIS (Endpoints, Architecture, etc.)" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    & .\backend\analyze-endpoints.ps1 -ResultsPath $resultsPath
    $testCount++
    
    # 5. Lighthouse
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "▶ 5. LIGHTHOUSE AUDIT" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    & .\frontend\lighthouse-audit.ps1 -ResultsPath $resultsPath
    $testCount++
    
    # 6. Load Testing
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "▶ 6. LOAD TESTING (k6)" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    & .\load-testing\run-load-tests.ps1 -ResultsPath $resultsPath -TestType "standard"
    $testCount++
    
    $totalTime = "45-60 min"
}

# ============================================================================
# INDIVIDUAL TESTS
# ============================================================================

if ($FrontendAnalysis -and -not $Full -and -not $Quick) {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "▶ FRONTEND ANALYSIS" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    & .\frontend\analyze-components.ps1 -ResultsPath $resultsPath
}

if ($BackendAnalysis -and -not $Full -and -not $Quick) {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "▶ BACKEND ANALYSIS" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    & .\backend\analyze-endpoints.ps1 -ResultsPath $resultsPath
}

if ($Lighthouse -and -not $Full -and -not $Quick) {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "▶ LIGHTHOUSE AUDIT" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    & .\frontend\lighthouse-audit.ps1 -ResultsPath $resultsPath
}

if ($LoadTest -and -not $Full -and -not $Quick) {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "▶ LOAD TESTING" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    & .\load-testing\run-load-tests.ps1 -ResultsPath $resultsPath -TestType "standard"
}

if ($GenerateSeed -and -not $Full -and -not $Quick) {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "▶ GENERATE SEED DATA" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    & ..\generate-seed-data.ps1
}

# ============================================================================
# RESULTS SUMMARY
# ============================================================================

Write-Host "`n" -ForegroundColor White
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "▶ RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

$resultFiles = Get-ChildItem $resultsPath -Filter "*-metrics-*.json" -ErrorAction SilentlyContinue | 
    Where-Object { $_.LastWriteTime -gt (Get-Date).AddHours(-2) }

if ($resultFiles) {
    Write-Host "`n📊 Test Results Generated:" -ForegroundColor Green
    foreach ($file in $resultFiles | Sort-Object LastWriteTime -Descending) {
        $size = [math]::Round($file.Length / 1KB, 2)
        Write-Host "   ✓ $($file.Name) ($size KB)" -ForegroundColor Gray
    }
} else {
    Write-Host "`n⚠️  No recent results found" -ForegroundColor Yellow
}

# Parse and display metrics
Write-Host "`n🔧 Generating report..." -ForegroundColor Yellow
& .\parse-results.ps1 | Out-Null

Write-Host "`n📁 All results in: $(Resolve-Path $resultsPath)" -ForegroundColor Cyan

# ============================================================================
# FINAL SUMMARY
# ============================================================================

Write-Host "`n" -ForegroundColor White
Write-Host "╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   ✅ TESTING COMPLETE                                            ║" -ForegroundColor Cyan
if ($Quick) {
    Write-Host "║   Mode: QUICK RUN (Frontend + Backend)                          ║" -ForegroundColor Cyan
} elseif ($Full) {
    Write-Host "║   Mode: FULL SUITE (All tests + Analysis)                       ║" -ForegroundColor Cyan
}
Write-Host "║   Timestamp: $timestamp                         ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

Write-Host "`n📝 Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Review results in ./results/ folder" -ForegroundColor Gray
Write-Host "   2. Run: .\parse-results.ps1 (to extract key metrics)" -ForegroundColor Gray
Write-Host "   3. Update readme-result.md with real metrics" -ForegroundColor Gray
Write-Host "   4. Use metrics for portfolio/presentations" -ForegroundColor Gray

Write-Host "`n🔗 Documentation:" -ForegroundColor Yellow
Write-Host "   • COVERAGE_ANALYSIS.md - What can be measured" -ForegroundColor Gray
Write-Host "   • UPDATE_INSTRUCTIONS.md - How to update portfolio" -ForegroundColor Gray
Write-Host "   • QUICKSTART.md - Quick reference" -ForegroundColor Gray

Write-Host "`n" -ForegroundColor White
