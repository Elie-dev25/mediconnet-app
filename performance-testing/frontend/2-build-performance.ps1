# Frontend Performance Tests - Build and Bundle Size Analysis

param(
    [string]$FrontendPath = "../../Mediconnet-Frontend",
    [string]$ResultsPath = "../results"
)

$ErrorActionPreference = "Continue"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Write-Host "[*] Frontend Performance Analysis - $timestamp" -ForegroundColor Cyan

$results = @{
    timestamp = $timestamp
}

# 1. Check package.json
Write-Host "[*] Analyzing build configuration..." -ForegroundColor Yellow
if (Test-Path "$FrontendPath/package.json") {
    $packageJson = Get-Content "$FrontendPath/package.json" -Raw | ConvertFrom-Json
    $results.dependencies = ($packageJson.dependencies | Get-Member -MemberType NoteProperty).Count
    $results.devDependencies = ($packageJson.devDependencies | Get-Member -MemberType NoteProperty).Count
    Write-Host "[OK] Dependencies: $($results.dependencies)" -ForegroundColor Green
    Write-Host "[OK] Dev Dependencies: $($results.devDependencies)" -ForegroundColor Green
}

# 2. Build frontend if needed
Write-Host "[*] Building frontend..." -ForegroundColor Yellow
$buildStart = Get-Date
try {
    Push-Location $FrontendPath
    $buildOutput = npm run build 2>&1
    $buildEnd = Get-Date
    $buildTime = ($buildEnd - $buildStart).TotalSeconds
    $results.buildTimeSeconds = [math]::Round($buildTime, 2)
    Write-Host "[OK] Build completed in $buildTime seconds" -ForegroundColor Green
    Pop-Location
} catch {
    Write-Host "[ERROR] Build failed" -ForegroundColor Red
    $results.buildTimeSeconds = 0
    Pop-Location
}

# 3. Analyze bundle sizes
Write-Host "[*] Analyzing bundle sizes..." -ForegroundColor Yellow
$distPath = "$FrontendPath/dist"
if (Test-Path $distPath) {
    $jsFiles = Get-ChildItem -Path $distPath -Recurse -Filter "*.js" -ErrorAction SilentlyContinue
    $cssFiles = Get-ChildItem -Path $distPath -Recurse -Filter "*.css" -ErrorAction SilentlyContinue
    
    $totalSizeKB = 0
    $totalSizeGzip = 0
    
    $jsFiles | ForEach-Object {
        $sizeKB = [math]::Round($_.Length / 1024, 2)
        $totalSizeKB += $sizeKB
        Write-Host "  - $($_.Name): $sizeKB KB" -ForegroundColor Gray
    }
    
    $results.bundleSizeKB = [math]::Round($totalSizeKB, 2)
    $results.jsFilesCount = @($jsFiles).Count
    $results.cssFilesCount = @($cssFiles).Count
    
    Write-Host "[OK] Total JS: $($results.bundleSizeKB) KB" -ForegroundColor Green
}

# 4. Check for gzip compression
Write-Host "[*] Checking gzip files..." -ForegroundColor Yellow
$gzipFiles = Get-ChildItem -Path $distPath -Recurse -Filter "*.gz" -ErrorAction SilentlyContinue
$totalGzipKB = 0
$gzipFiles | ForEach-Object {
    $sizeKB = [math]::Round($_.Length / 1024, 2)
    $totalGzipKB += $sizeKB
}
if ($totalGzipKB -gt 0) {
    $results.gzipSizeKB = [math]::Round($totalGzipKB, 2)
    Write-Host "[OK] Gzip total: $($results.gzipSizeKB) KB" -ForegroundColor Green
} else {
    Write-Host "[INFO] No pre-compressed files found" -ForegroundColor Yellow
}

# 5. Save Results
$resultsFile = "$ResultsPath/frontend-performance-$timestamp.json"
$results | ConvertTo-Json | Out-File $resultsFile
Write-Host "[OK] Results saved: $resultsFile" -ForegroundColor Green

# 6. Display Summary
Write-Host "`n========== FRONTEND METRICS ==========" -ForegroundColor Cyan
Write-Host "Build Time:        $($results.buildTimeSeconds) seconds" -ForegroundColor White
Write-Host "Bundle Size:       $($results.bundleSizeKB) KB" -ForegroundColor White
Write-Host "JS Files:          $($results.jsFilesCount)" -ForegroundColor White
Write-Host "CSS Files:         $($results.cssFilesCount)" -ForegroundColor White
Write-Host "Dependencies:      $($results.dependencies)" -ForegroundColor White
Write-Host "Dev Dependencies:  $($results.devDependencies)" -ForegroundColor White
Write-Host "======================================" -ForegroundColor Cyan

$summary = "Build: $($results.buildTimeSeconds)s | Bundle: $($results.bundleSizeKB) KB | JS: $($results.jsFilesCount) files"
$summary | Set-Clipboard
Write-Host "[OK] Summary copied to clipboard" -ForegroundColor Green
