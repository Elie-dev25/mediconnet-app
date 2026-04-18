# ============================================================================
# Lighthouse Performance Audit Script
# ============================================================================
# Lance Lighthouse pour obtenir les vrais scores de performance

param(
    [string]$AppUrl = "http://localhost",
    [int]$Port = 80,
    [string]$ResultsPath = "../results"
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Write-Host "🚀 Lighthouse Performance Audit - $timestamp" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "Target: $AppUrl" -ForegroundColor Yellow

# ============================================================================
# CHECK LIGHTHOUSE
# ============================================================================

Write-Host "`n🔍 Checking Lighthouse installation..." -ForegroundColor Yellow

try {
    $lighthouseVersion = npm list -g @lhci/cli 2>&1 | Select-String "@lhci"
    if ($lighthouseVersion) {
        Write-Host "✅ Lighthouse CLI found" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Lighthouse not installed globally. Installing..." -ForegroundColor Yellow
        npm install -g @lhci/cli 2>&1 | Out-Null
        Write-Host "✅ Lighthouse installed" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️  Could not install Lighthouse globally" -ForegroundColor Yellow
    Write-Host "Install manually: npm install -g @lhci/cli" -ForegroundColor Yellow
}

# ============================================================================
# RUN LIGHTHOUSE AUDIT
# ============================================================================

Write-Host "`n📊 Running Lighthouse audit..." -ForegroundColor Yellow
Write-Host "This may take 1-2 minutes..." -ForegroundColor Gray

$reportPath = Join-Path $ResultsPath "lighthouse-report-$timestamp.json"

try {
    # Utiliser npx pour exécuter Lighthouse
    $npxOutput = npx lighthouse `
        $AppUrl `
        --output=json `
        --output-path=$reportPath `
        --quiet 2>&1
    
    if (Test-Path $reportPath) {
        $reportData = Get-Content $reportPath | ConvertFrom-Json
        
        # Extract scores
        $categories = $reportData.categories
        $scores = @{
            performance = [math]::Round($categories.performance.score * 100, 0)
            accessibility = [math]::Round($categories.accessibility.score * 100, 0)
            bestPractices = [math]::Round($categories.'best-practices'.score * 100, 0)
            seo = [math]::Round($categories.seo.score * 100, 0)
            pwa = [math]::Round($categories.pwa.score * 100, 0)
        }
        
        # Extract metrics
        $audits = $reportData.audits
        $metrics = @{
            fcp = [math]::Round($audits.'first-contentful-paint'.numericValue / 1000, 2)  # en secondes
            lcp = [math]::Round($audits.'largest-contentful-paint'.numericValue / 1000, 2)
            cls = [math]::Round($audits.'cumulative-layout-shift'.numericValue, 3)
            tti = [math]::Round($audits.'interactive'.numericValue / 1000, 2)
            tbt = if ($audits.'total-blocking-time') { [math]::Round($audits.'total-blocking-time'.numericValue / 1000, 2) } else { 0 }
        }
        
        # Display results
        Write-Host "`n" -ForegroundColor White
        Write-Host "╔═══════════════════════════════════════════════╗" -ForegroundColor Cyan
        Write-Host "║   LIGHTHOUSE AUDIT RESULTS                    ║" -ForegroundColor Cyan
        Write-Host "╚═══════════════════════════════════════════════╝" -ForegroundColor Cyan
        
        Write-Host "`n📊 SCORES:" -ForegroundColor Green
        Write-Host "   • Performance: $($scores.performance)/100" -ForegroundColor $(if ($scores.performance -ge 90) { 'Green' } elseif ($scores.performance -ge 50) { 'Yellow' } else { 'Red' })
        Write-Host "   • Accessibility: $($scores.accessibility)/100" -ForegroundColor $(if ($scores.accessibility -ge 90) { 'Green' } elseif ($scores.accessibility -ge 50) { 'Yellow' } else { 'Red' })
        Write-Host "   • Best Practices: $($scores.bestPractices)/100" -ForegroundColor $(if ($scores.bestPractices -ge 90) { 'Green' } elseif ($scores.bestPractices -ge 50) { 'Yellow' } else { 'Red' })
        Write-Host "   • SEO: $($scores.seo)/100" -ForegroundColor $(if ($scores.seo -ge 90) { 'Green' } else { 'Yellow' })
        Write-Host "   • PWA: $($scores.pwa)/100" -ForegroundColor $(if ($scores.pwa -ge 90) { 'Green' } else { 'Yellow' })
        
        Write-Host "`n⏱️  WEB VITALS:" -ForegroundColor Green
        Write-Host "   • First Contentful Paint (FCP): $($metrics.fcp)s" -ForegroundColor Gray
        Write-Host "   • Largest Contentful Paint (LCP): $($metrics.lcp)s" -ForegroundColor Gray
        Write-Host "   • Cumulative Layout Shift (CLS): $($metrics.cls)" -ForegroundColor Gray
        Write-Host "   • Time to Interactive (TTI): $($metrics.tti)s" -ForegroundColor Gray
        Write-Host "   • Total Blocking Time (TBT): $($metrics.tbt)s" -ForegroundColor Gray
        
        # Determine overall rating
        $overallScore = [math]::Round(($scores.performance + $scores.accessibility + $scores.bestPractices + $scores.seo) / 4, 0)
        Write-Host "`n✨ OVERALL SCORE: $overallScore/100" -ForegroundColor $(if ($overallScore -ge 90) { 'Green' } elseif ($overallScore -ge 50) { 'Yellow' } else { 'Red' })
        
        # Save parsed results
        $resultsJson = @{
            timestamp = $timestamp
            url = $AppUrl
            scores = $scores
            metrics = $metrics
            overallScore = $overallScore
        }
        
        $jsonPath = Join-Path $ResultsPath "lighthouse-results-$timestamp.json"
        $resultsJson | ConvertTo-Json | Out-File -FilePath $jsonPath -Encoding UTF8
        
        Write-Host "`n✅ Report saved to:" -ForegroundColor Green
        Write-Host "   • JSON: $reportPath" -ForegroundColor Gray
        Write-Host "   • Parsed: $jsonPath" -ForegroundColor Gray
        
        # Generate snippet
        $snippet = @"

## 🚀 Lighthouse Performance Score

**Overall Score:** $overallScore/100
- Performance: $($scores.performance)
- Accessibility: $($scores.accessibility)
- Best Practices: $($scores.bestPractices)  
- SEO: $($scores.seo)

**Web Vitals:**
- First Contentful Paint: $($metrics.fcp)s
- Largest Contentful Paint: $($metrics.lcp)s
- Cumulative Layout Shift: $($metrics.cls)
- Time to Interactive: $($metrics.tti)s

"@
        
        $snippet | Set-Clipboard
        Write-Host "`n📋 Portfolio snippet copied to clipboard!" -ForegroundColor Green
        
    } else {
        Write-Host "❌ Lighthouse report not generated" -ForegroundColor Red
    }
} catch {
    Write-Host "⚠️  Lighthouse execution failed: $_" -ForegroundColor Yellow
    Write-Host "`nTroubleshooting:" -ForegroundColor Gray
    Write-Host "1. Ensure app is running: npm start (from Mediconnet-Frontend)" -ForegroundColor Gray
    Write-Host "2. Check URL is accessible: curl $AppUrl" -ForegroundColor Gray
    Write-Host "3. Install Lighthouse: npm install -g @lhci/cli" -ForegroundColor Gray
}

Write-Host "`n" -ForegroundColor White
