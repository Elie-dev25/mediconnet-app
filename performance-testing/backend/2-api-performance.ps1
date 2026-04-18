# Backend API Response Time Tests

param(
    [string]$ApiUrl = "http://localhost:5000",
    [string]$ResultsPath = "../results",
    [int]$NumRequests = 50
)

$ErrorActionPreference = "Continue"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Write-Host "[*] Backend API Performance Test - $timestamp" -ForegroundColor Cyan
Write-Host "[*] Target: $ApiUrl" -ForegroundColor Yellow
Write-Host "[*] Requests per endpoint: $NumRequests" -ForegroundColor Yellow

$results = @{
    timestamp = $timestamp
    apiUrl = $ApiUrl
    endpoints = @()
}

# Define test endpoints (basic health checks)
$testEndpoints = @(
    "/api/health",
    "/swagger",
    "/swagger/v1/swagger.json"
)

$allResponseTimes = @()
$successCount = 0
$failureCount = 0

# Test each endpoint
foreach ($endpoint in $testEndpoints) {
    Write-Host "`n[*] Testing: $endpoint" -ForegroundColor Yellow
    $endpointTimes = @()
    
    for ($i = 0; $i -lt $NumRequests; $i++) {
        try {
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            $response = Invoke-WebRequest -Uri "$ApiUrl$endpoint" -TimeoutSec 5 -ErrorAction SilentlyContinue
            $sw.Stop()
            
            if ($response.StatusCode -eq 200) {
                $endpointTimes += $sw.ElapsedMilliseconds
                $allResponseTimes += $sw.ElapsedMilliseconds
                $successCount++
                Write-Host "  [$i] $($sw.ElapsedMilliseconds)ms" -ForegroundColor Green
            } else {
                $failureCount++
                Write-Host "  [$i] Status: $($response.StatusCode)" -ForegroundColor Yellow
            }
        } catch {
            $failureCount++
            Write-Host "  [$i] Error: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        Start-Sleep -Milliseconds 100
    }
    
    # Calculate statistics
    if ($endpointTimes.Count -gt 0) {
        $avg = [math]::Round(($endpointTimes | Measure-Object -Average).Average, 2)
        $min = [math]::Round(($endpointTimes | Measure-Object -Minimum).Minimum, 2)
        $max = [math]::Round(($endpointTimes | Measure-Object -Maximum).Maximum, 2)
        $p95 = [math]::Round(($endpointTimes | Sort-Object)[[math]::Floor($endpointTimes.Count * 0.95)], 2)
        
        $results.endpoints += @{
            endpoint = $endpoint
            avgResponseTimeMs = $avg
            minResponseTimeMs = $min
            maxResponseTimeMs = $max
            p95ResponseTimeMs = $p95
            requestsCompleted = $endpointTimes.Count
        }
        
        Write-Host "[OK] Avg: $avg ms | Min: $min ms | Max: $max ms | P95: $p95 ms" -ForegroundColor Green
    }
}

# Calculate overall statistics
Write-Host "`n[*] Calculating overall statistics..." -ForegroundColor Yellow
if ($allResponseTimes.Count -gt 0) {
    $overallAvg = [math]::Round(($allResponseTimes | Measure-Object -Average).Average, 2)
    $overallMin = [math]::Round(($allResponseTimes | Measure-Object -Minimum).Minimum, 2)
    $overallMax = [math]::Round(($allResponseTimes | Measure-Object -Maximum).Maximum, 2)
    $successRate = [math]::Round(($successCount / ($successCount + $failureCount)) * 100, 1)
    
    $results.overall = @{
        totalRequests = $successCount + $failureCount
        successfulRequests = $successCount
        failedRequests = $failureCount
        successRatePercent = $successRate
        avgResponseTimeMs = $overallAvg
        minResponseTimeMs = $overallMin
        maxResponseTimeMs = $overallMax
    }
    
    Write-Host "[OK] Success Rate: $successRate%" -ForegroundColor Green
    Write-Host "[OK] Avg Response: $overallAvg ms" -ForegroundColor Green
}

# Save Results
$resultsFile = "$ResultsPath/backend-api-test-$timestamp.json"
$results | ConvertTo-Json | Out-File $resultsFile
Write-Host "[OK] Results saved: $resultsFile" -ForegroundColor Green

# Display Summary
Write-Host "`n========== API TEST SUMMARY ==========" -ForegroundColor Cyan
Write-Host "Successful:     $successCount" -ForegroundColor White
Write-Host "Failed:         $failureCount" -ForegroundColor White
Write-Host "Success Rate:   $($results.overall.successRatePercent)%" -ForegroundColor White
Write-Host "Avg Response:   $($results.overall.avgResponseTimeMs) ms" -ForegroundColor White
Write-Host "Min Response:   $($results.overall.minResponseTimeMs) ms" -ForegroundColor White
Write-Host "Max Response:   $($results.overall.maxResponseTimeMs) ms" -ForegroundColor White
Write-Host "======================================" -ForegroundColor Cyan

$summary = "API Tests: Success=$($results.overall.successRatePercent)% | Avg=$($results.overall.avgResponseTimeMs)ms"
$summary | Set-Clipboard
Write-Host "[OK] Summary copied to clipboard" -ForegroundColor Green
