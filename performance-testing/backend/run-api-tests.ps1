# ============================================================================
# Backend API Performance Testing Script
# ============================================================================
# Teste: API response times, Database queries, Memory usage

param(
    [string]$ApiUrl = "http://localhost:5000",
    [string]$ResultsPath = "../results",
    [int]$TestDurationSeconds = 30
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Write-Host "🚀 Backend API Performance Tests - $timestamp" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "API URL: $ApiUrl" -ForegroundColor Yellow
Write-Host "Duration: $TestDurationSeconds seconds" -ForegroundColor Yellow

$results = @{
    timestamp = $timestamp
    apiUrl = $ApiUrl
    endpoints = @()
}

# ============================================================================
# HELPER FUNCTIONS
# ============================================================================

function Test-ApiHealth {
    param([string]$Url)
    try {
        $response = Invoke-WebRequest -Uri "$Url/health" -Method Get -TimeoutSec 5 -SkipHttpErrorCheck
        return $response.StatusCode -eq 200
    } catch {
        return $false
    }
}

function Measure-ApiEndpoint {
    param(
        [string]$Url,
        [string]$Method = "GET",
        [int]$Iterations = 20
    )
    
    $times = @()
    $errors = 0
    
    Write-Host "  Testing $Method $Url..." -ForegroundColor Gray
    
    for ($i = 0; $i -lt $Iterations; $i++) {
        try {
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            $response = Invoke-WebRequest -Uri $Url -Method $Method -TimeoutSec 10 -SkipHttpErrorCheck
            $stopwatch.Stop()
            
            $times += $stopwatch.ElapsedMilliseconds
            
            if ($response.StatusCode -ge 400) {
                $errors++
            }
        } catch {
            $errors++
        }
    }
    
    if ($times.Count -gt 0) {
        $avg = [math]::Round(($times | Measure-Object -Average).Average, 2)
        $min = [math]::Round(($times | Measure-Object -Minimum).Minimum, 2)
        $max = [math]::Round(($times | Measure-Object -Maximum).Maximum, 2)
        $p95 = [math]::Round(($times | Sort-Object)[[int]($times.Count * 0.95)], 2)
        
        return @{
            endpoint = $Url
            method = $Method
            avgMs = $avg
            minMs = $min
            maxMs = $max
            p95Ms = $p95
            successRate = [math]::Round((($Iterations - $errors) / $Iterations) * 100, 1)
        }
    } else {
        return $null
    }
}

# ============================================================================
# 1. CHECK API HEALTH
# ============================================================================
Write-Host "`n🏥 Checking API health..." -ForegroundColor Yellow

$isHealthy = Test-ApiHealth $ApiUrl
if ($isHealthy) {
    Write-Host "✅ API is healthy" -ForegroundColor Green
    $results['apiHealthy'] = $true
} else {
    Write-Host "❌ API is not responding. Make sure it's running." -ForegroundColor Red
    $results['apiHealthy'] = $false
    Write-Host "`nMake sure to start the API first:"
    Write-Host "  cd Mediconnet-Backend && dotnet run"
    exit 1
}

# ============================================================================
# 2. TEST CORE ENDPOINTS
# ============================================================================
Write-Host "`n📡 Testing API endpoints..." -ForegroundColor Yellow

# Liste des endpoints à tester (adapter selon votre API)
$endpointsToTest = @(
    @{ url = "$ApiUrl/api/patient"; method = "GET" },
    @{ url = "$ApiUrl/api/medical-alert"; method = "GET" },
    @{ url = "$ApiUrl/api/consultation"; method = "GET" },
    @{ url = "$ApiUrl/api/prescription"; method = "GET" },
    @{ url = "$ApiUrl/api/pharmacy"; method = "GET" },
    @{ url = "$ApiUrl/api/hospitalisation"; method = "GET" },
    @{ url = "$ApiUrl/api/facturation"; method = "GET" },
)

foreach ($endpoint in $endpointsToTest) {
    $metrics = Measure-ApiEndpoint -Url $endpoint.url -Method $endpoint.method -Iterations 20
    if ($metrics) {
        $results.endpoints += $metrics
        Write-Host "    ✓ Avg: $($metrics.avgMs)ms | P95: $($metrics.p95Ms)ms | Success: $($metrics.successRate)%" -ForegroundColor Green
    }
}

# ============================================================================
# 3. CALCULATE STATISTICS
# ============================================================================
Write-Host "`n📊 Calculating statistics..." -ForegroundColor Yellow

$allResponseTimes = $results.endpoints | ForEach-Object { $_.avgMs }
if ($allResponseTimes) {
    $results['statistics'] = @{
        totalEndpointsTested = $results.endpoints.Count
        avgResponseTimeMs = [math]::Round(($allResponseTimes | Measure-Object -Average).Average, 2)
        minResponseTimeMs = [math]::Round(($allResponseTimes | Measure-Object -Minimum).Minimum, 2)
        maxResponseTimeMs = [math]::Round(($allResponseTimes | Measure-Object -Maximum).Maximum, 2)
        successRatePercent = [math]::Round(($results.endpoints | ForEach-Object { $_.successRate } | Measure-Object -Average).Average, 1)
    }
}

# ============================================================================
# SUMMARY
# ============================================================================
Write-Host "`n" -ForegroundColor White
Write-Host "╔════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   BACKEND API PERFORMANCE SUMMARY          ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════╝" -ForegroundColor Cyan

if ($results['statistics']) {
    $summary = @"
📊 Response Times:
   • Average: $($results['statistics']['avgResponseTimeMs']) ms
   • Min: $($results['statistics']['minResponseTimeMs']) ms
   • Max: $($results['statistics']['maxResponseTimeMs']) ms
   
✅ Reliability:
   • Endpoints Tested: $($results['statistics']['totalEndpointsTested'])
   • Success Rate: $($results['statistics']['successRatePercent'])%

📈 Top Performers (fastest):
"@
    
    Write-Host $summary
    
    $results.endpoints | Sort-Object avgMs | Select-Object -First 3 | ForEach-Object {
        Write-Host "   • $($_.method) $($_.endpoint): $($_.avgMs)ms (P95: $($_.p95Ms)ms)" -ForegroundColor Green
    }
    
    Write-Host "`n⚠️  Slowest Endpoints:" -ForegroundColor Yellow
    $results.endpoints | Sort-Object avgMs -Descending | Select-Object -First 3 | ForEach-Object {
        Write-Host "   • $($_.method) $($_.endpoint): $($_.avgMs)ms (P95: $($_.p95Ms)ms)" -ForegroundColor Yellow
    }
}

# ============================================================================
# SAVE RESULTS
# ============================================================================
if (!(Test-Path $ResultsPath)) {
    New-Item -ItemType Directory -Path $ResultsPath -Force | Out-Null
}

$jsonPath = Join-Path $ResultsPath "backend-metrics-$timestamp.json"
$results | ConvertTo-Json -Depth 10 | Out-File -FilePath $jsonPath -Encoding UTF8

Write-Host "`n✅ Results saved to: $jsonPath" -ForegroundColor Green
Write-Host "`n" -ForegroundColor White
