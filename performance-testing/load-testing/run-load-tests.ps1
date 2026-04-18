# ============================================================================
# Load Testing Script with k6
# ============================================================================
# Teste: Charge, spike, soak testing

param(
    [string]$ApiUrl = "http://localhost:5000",
    [string]$ResultsPath = "../results",
    [string]$TestType = "standard"  # standard, spike, soak, endurance
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Write-Host "🚀 Load Testing with k6 - $timestamp" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "API URL: $ApiUrl" -ForegroundColor Yellow
Write-Host "Test Type: $TestType" -ForegroundColor Yellow

# ============================================================================
# Check k6 installation
# ============================================================================
Write-Host "`n🔍 Checking k6 installation..." -ForegroundColor Yellow

try {
    $k6Version = k6 version 2>&1
    Write-Host "✅ k6 found: $k6Version" -ForegroundColor Green
} catch {
    Write-Host "❌ k6 is not installed or not in PATH" -ForegroundColor Red
    Write-Host "`nInstall k6 from: https://k6.io/docs/getting-started/installation/" -ForegroundColor Yellow
    Write-Host "Or via Chocolatey: choco install k6" -ForegroundColor Yellow
    exit 1
}

# ============================================================================
# Prepare test scenarios
# ============================================================================

$scenarios = @{
    "standard" = @{
        description = "Standard load test (0-100-200 VUS)"
        vus = 100
        duration = "9m"  # 2m + 5m + 2m
    }
    "spike" = @{
        description = "Spike test (instant jump to high load)"
        vus = 500
        duration = "1m"
    }
    "soak" = @{
        description = "Soak test (sustained load, 30 min)"
        vus = 100
        duration = "30m"
    }
    "endurance" = @{
        description = "Endurance test (8 hours)"
        vus = 50
        duration = "8h"
    }
}

if ($scenarios.ContainsKey($TestType)) {
    $scenario = $scenarios[$TestType]
    Write-Host "`n📊 $($scenario.description)" -ForegroundColor Yellow
    Write-Host "VUs: $($scenario.vus) | Duration: $($scenario.duration)" -ForegroundColor Yellow
} else {
    Write-Host "❌ Unknown test type: $TestType" -ForegroundColor Red
    Write-Host "Available: standard, spike, soak, endurance" -ForegroundColor Yellow
    exit 1
}

# ============================================================================
# Create k6 script with environment variable
# ============================================================================
Write-Host "`n🔧 Preparing k6 script..." -ForegroundColor Yellow

if (!(Test-Path "results")) {
    New-Item -ItemType Directory -Path $ResultsPath -Force | Out-Null
}

# ============================================================================
# Run k6 test
# ============================================================================
Write-Host "`n🏃 Running load test..." -ForegroundColor Yellow

$k6OutputPath = Join-Path $ResultsPath "k6-results-$timestamp.json"

# Construire la commande k6 selon le test type
$k6Args = @(
    "run"
    "load-test.js"
    "--out"
    "json=$k6OutputPath"
    "-e"
    "BASE_URL=$ApiUrl"
)

# Ajouter des paramètres spécifiques selon le test
switch ($TestType) {
    "spike" {
        $k6Args += "--stage", "10s:0", "--stage", "10s:500", "--stage", "30s:500", "--stage", "10s:0"
    }
    "soak" {
        $k6Args += "--stage", "5m:100", "--stage", "30m:100", "--stage", "5m:0"
    }
    "endurance" {
        $k6Args += "--stage", "5m:50", "--stage", "8h:50", "--stage", "5m:0"
    }
    default {
        # Standard scenario utilise le default du script
    }
}

Write-Host "`nExecution: k6 $($k6Args -join ' ')" -ForegroundColor Gray

try {
    & k6 @k6Args
    $testPassed = $LASTEXITCODE -eq 0
} catch {
    Write-Host "❌ k6 execution failed: $_" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Parse and display results
# ============================================================================
Write-Host "`n" -ForegroundColor White
Write-Host "╔════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   LOAD TEST RESULTS                        ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════╝" -ForegroundColor Cyan

if (Test-Path $k6OutputPath) {
    try {
        $jsonData = Get-Content $k6OutputPath | ConvertFrom-Json -AsHashtable
        
        # Extraire les métriques importantes
        $metrics = @{}
        
        if ($jsonData.metrics) {
            foreach ($metric in $jsonData.metrics.Keys) {
                if ($metric -like "http_req_*" -or $metric -like "checks" -or $metric -like "*_http_*") {
                    $metrics[$metric] = $jsonData.metrics[$metric]
                }
            }
        }
        
        Write-Host "`n✅ Test completed. Results saved to:" -ForegroundColor Green
        Write-Host "   $k6OutputPath" -ForegroundColor Gray
        
        if ($testPassed) {
            Write-Host "`n✅ All thresholds passed!" -ForegroundColor Green
        } else {
            Write-Host "`n⚠️  Some thresholds were not met" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "⚠️  Could not parse k6 results" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ k6 output file not found" -ForegroundColor Red
}

Write-Host "`n📝 To view detailed HTML report, convert the JSON results with:" -ForegroundColor Gray
Write-Host "   k6 run --out cloud load-test.js" -ForegroundColor Gray

Write-Host "`n" -ForegroundColor White
