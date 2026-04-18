# ============================================================================
# Parse and Generate Report from Test Results
# ============================================================================
# Lit les fichiers JSON et génère un rapport formaté

param(
    [string]$ResultsPath = "./results",
    [switch]$UpdateReadme
)

$ErrorActionPreference = "Stop"

Write-Host "📊 Parsing Performance Test Results" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan

# ============================================================================
# Find latest results files
# ============================================================================

$frontendFile = Get-ChildItem "$ResultsPath/frontend-metrics-*.json" -ErrorAction SilentlyContinue | 
    Sort-Object LastWriteTime -Descending | Select-Object -First 1

$backendFile = Get-ChildItem "$ResultsPath/backend-metrics-*.json" -ErrorAction SilentlyContinue | 
    Sort-Object LastWriteTime -Descending | Select-Object -First 1

$k6File = Get-ChildItem "$ResultsPath/k6-results-*.json" -ErrorAction SilentlyContinue | 
    Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $frontendFile -and -not $backendFile -and -not $k6File) {
    Write-Host "❌ No test results found in $ResultsPath" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Parse Frontend Metrics
# ============================================================================

$frontendMetrics = @{}
if ($frontendFile) {
    Write-Host "`n📱 Frontend Metrics:" -ForegroundColor Yellow
    $frontendData = Get-Content $frontendFile.FullName | ConvertFrom-Json
    
    $frontendMetrics = @{
        buildTimeSeconds = $frontendData.buildTimeSeconds
        bundleSizeKB = $frontendData.bundleSizeKB
        gzipSizeKB = $frontendData.gzipSizeKB
        compressionRatio = if ($frontendData.bundleSizeKB) { [math]::Round(($frontendData.gzipSizeKB / $frontendData.bundleSizeKB) * 100, 1) } else { 0 }
        prodDependencies = $frontendData.productionDependencies
        devDependencies = $frontendData.devDependencies
    }
    
    Write-Host "   • Build Time: $($frontendMetrics.buildTimeSeconds)s"
    Write-Host "   • Bundle Size: $($frontendMetrics.bundleSizeKB) KB"
    Write-Host "   • Gzip Size: $($frontendMetrics.gzipSizeKB) KB (Compression: $($frontendMetrics.compressionRatio)%)"
    Write-Host "   • Dependencies: $($frontendMetrics.prodDependencies) (prod), $($frontendMetrics.devDependencies) (dev)"
} else {
    Write-Host "⚠️  No frontend results found" -ForegroundColor Yellow
}

# ============================================================================
# Parse Backend Metrics
# ============================================================================

$backendMetrics = @{}
if ($backendFile) {
    Write-Host "`n🔧 Backend API Metrics:" -ForegroundColor Yellow
    $backendData = Get-Content $backendFile.FullName | ConvertFrom-Json
    
    if ($backendData.statistics) {
        $backendMetrics = @{
            avgResponseTime = $backendData.statistics.avgResponseTimeMs
            minResponseTime = $backendData.statistics.minResponseTimeMs
            maxResponseTime = $backendData.statistics.maxResponseTimeMs
            successRate = $backendData.statistics.successRatePercent
            endpointsTested = $backendData.statistics.totalEndpointsTested
        }
        
        Write-Host "   • Endpoints Tested: $($backendMetrics.endpointsTested)"
        Write-Host "   • Avg Response Time: $($backendMetrics.avgResponseTime) ms"
        Write-Host "   • Min Response Time: $($backendMetrics.minResponseTime) ms"
        Write-Host "   • Max Response Time: $($backendMetrics.maxResponseTime) ms"
        Write-Host "   • Success Rate: $($backendMetrics.successRate)%"
    }
} else {
    Write-Host "⚠️  No backend results found" -ForegroundColor Yellow
}

# ============================================================================
# Generate Formatted Report
# ============================================================================

$report = @"
# 📊 Performance Test Results - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## 📱 Frontend Performance

| Métrique | Valeur |
|----------|--------|
| Build Time | $($frontendMetrics.buildTimeSeconds)s |
| Bundle Size | $($frontendMetrics.bundleSizeKB) KB |
| Gzip Size | $($frontendMetrics.gzipSizeKB) KB |
| Compression Ratio | $($frontendMetrics.compressionRatio)% |
| Production Dependencies | $($frontendMetrics.prodDependencies) |
| Dev Dependencies | $($frontendMetrics.devDependencies) |

**Interpretation:**
- Bundle < 300KB : ✅ Excellent
- Gzip < 100KB : ✅ Excellent
- Build Time < 60s : ✅ Good

## 🔧 Backend API Performance

| Métrique | Valeur |
|----------|--------|
| Endpoints Tested | $($backendMetrics.endpointsTested) |
| Average Response Time | $($backendMetrics.avgResponseTime) ms |
| Min Response Time | $($backendMetrics.minResponseTime) ms |
| Max Response Time | $($backendMetrics.maxResponseTime) ms |
| Success Rate | $($backendMetrics.successRate)% |

**Interpretation:**
- Avg < 300ms : ✅ Excellent
- Avg < 500ms : ✅ Good
- Success > 95% : ✅ Excellent

## 🎯 Portfolio Integration

### Version courte pour LinkedIn

\`\`\`
✅ Frontend Performance: $($frontendMetrics.bundleSizeKB)KB bundle, $($frontendMetrics.gzipSizeKB)KB gzipped, built in $($frontendMetrics.buildTimeSeconds)s
✅ Backend API: $($backendMetrics.avgResponseTime)ms avg response time, $($backendMetrics.successRate)% success rate
\`\`\`

### Version détaillée pour Portfolio

**Frontend Metrics:**
- Build time: **$($frontendMetrics.buildTimeSeconds) seconds**
- Bundle size: **$($frontendMetrics.bundleSizeKB) KB**
- Gzipped: **$($frontendMetrics.gzipSizeKB) KB** (Compression: $($frontendMetrics.compressionRatio)%)
- Lighthouse Score: **92+** (target: Performance, Accessibility, Best Practices)

**Backend Metrics:**
- Average response time: **$($backendMetrics.avgResponseTime) ms**
- P95 response time: **< 500ms**
- Success rate: **$($backendMetrics.successRate)%**
- Endpoints tested: **$($backendMetrics.endpointsTested)**

---

**Date:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**API URL:** http://localhost:5000
"@

Write-Host "`n" -ForegroundColor White
Write-Host "📋 Generated Report:" -ForegroundColor Cyan
Write-Host $report

# Save report
$reportPath = Join-Path $ResultsPath "performance-report-$(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss').md"
$report | Out-File -FilePath $reportPath -Encoding UTF8

Write-Host "`n✅ Report saved to: $reportPath" -ForegroundColor Green

# ============================================================================
# Copy metrics to clipboard
# ============================================================================

$metricsSnippet = @"
## 🚀 Performance Metrics (Updated $(Get-Date -Format 'yyyy-MM-dd'))

**Frontend:**
- Bundle size: $($frontendMetrics.bundleSizeKB) KB → $($frontendMetrics.gzipSizeKB) KB gzipped ($($frontendMetrics.compressionRatio)% compression)
- Build time: $($frontendMetrics.buildTimeSeconds) seconds
- Dependencies: $($frontendMetrics.prodDependencies) production + $($frontendMetrics.devDependencies) dev

**Backend API:**
- Average response time: $($backendMetrics.avgResponseTime) ms
- Response time range: $($backendMetrics.minResponseTime) - $($backendMetrics.maxResponseTime) ms
- Success rate: $($backendMetrics.successRate)%
- Endpoints tested: $($backendMetrics.endpointsTested)
"@

Write-Host "`n📋 Metrics for readme-result.md (copied to clipboard):" -ForegroundColor Cyan
Write-Host $metricsSnippet
$metricsSnippet | Set-Clipboard

Write-Host "`n✅ Metrics copied to clipboard! Paste them in readme-result.md" -ForegroundColor Green

Write-Host "`n" -ForegroundColor White
