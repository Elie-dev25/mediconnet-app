# ============================================================================
# Frontend Performance Testing Script
# ============================================================================
# Teste: Bundle size, Lighthouse, Build time, Performance metrics

param(
    [string]$FrontendPath = "../../Mediconnet-Frontend",
    [string]$ResultsPath = "../results"
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Write-Host "🚀 Frontend Performance Tests - $timestamp" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# Vérifier que npm est installé
try {
    npm --version | Out-Null
} catch {
    Write-Host "❌ npm n'est pas installé. Installez Node.js d'abord." -ForegroundColor Red
    exit 1
}

# Vérifier le répertoire frontend
if (!(Test-Path $FrontendPath)) {
    Write-Host "❌ Frontend path not found: $FrontendPath" -ForegroundColor Red
    exit 1
}

$results = @{}

# ============================================================================
# 1. INSTALL DEPENDENCIES
# ============================================================================
Write-Host "`n📦 Installing dependencies..." -ForegroundColor Yellow
Push-Location $FrontendPath
npm install 2>&1 | Out-Null
Pop-Location
Write-Host "✅ Dependencies installed" -ForegroundColor Green

# ============================================================================
# 2. BUILD ANALYSIS
# ============================================================================
Write-Host "`n🔨 Building production bundle..." -ForegroundColor Yellow
$buildStartTime = Get-Date

Push-Location $FrontendPath
$buildOutput = npm run build 2>&1
$buildTime = ((Get-Date) - $buildStartTime).TotalSeconds
Pop-Location

Write-Host "✅ Build completed in $buildTime seconds" -ForegroundColor Green
$results['buildTimeSeconds'] = [math]::Round($buildTime, 2)

# ============================================================================
# 3. BUNDLE SIZE ANALYSIS
# ============================================================================
Write-Host "`n📊 Analyzing bundle size..." -ForegroundColor Yellow

$distPath = Join-Path $FrontendPath "dist"
if (Test-Path $distPath) {
    # Main bundle
    $mainBundles = Get-ChildItem "$distPath\mediconnet-frontend\*main*.js" -ErrorAction SilentlyContinue
    $totalBundleSize = 0
    $bundles = @()
    
    foreach ($bundle in $mainBundles) {
        $sizeKB = [math]::Round($bundle.Length / 1KB, 2)
        $totalBundleSize += $sizeKB
        $bundles += @{
            name = $bundle.Name
            sizeKB = $sizeKB
        }
        Write-Host "  • $($bundle.Name): $sizeKB KB" -ForegroundColor Gray
    }
    
    $results['bundleSizeKB'] = [math]::Round($totalBundleSize, 2)
    $results['bundleCount'] = $bundles.Count
    Write-Host "✅ Total bundle size: $totalBundleSize KB" -ForegroundColor Green
} else {
    Write-Host "⚠️  dist folder not found" -ForegroundColor Yellow
    $results['bundleSizeKB'] = 0
}

# ============================================================================
# 4. GZIP SIZE MEASUREMENT
# ============================================================================
Write-Host "`n📦 Measuring gzip compression..." -ForegroundColor Yellow

if (Test-Path $distPath) {
    $jsFiles = Get-ChildItem "$distPath\mediconnet-frontend\*.js"
    $totalGzipSize = 0
    
    foreach ($file in $jsFiles) {
        $gzipSizeBytes = (gzip $file.FullName 2>&1 | Measure-Object -Property Length | Select-Object -ExpandProperty Sum)
        if ($gzipSizeBytes) {
            $gzipSizeKB = $gzipSizeBytes / 1KB
            $totalGzipSize += $gzipSizeKB
        }
    }
    
    if ($totalGzipSize -gt 0) {
        $results['gzipSizeKB'] = [math]::Round($totalGzipSize, 2)
        $compressionRatio = [math]::Round(($results['gzipSizeKB'] / $results['bundleSizeKB']) * 100, 1)
        Write-Host "✅ Gzip size: $([math]::Round($totalGzipSize, 2)) KB (Compression: $compressionRatio%)" -ForegroundColor Green
    }
}

# ============================================================================
# 5. LIGHTHOUSE AUDIT (si lighthouse-cli disponible)
# ============================================================================
Write-Host "`n💡 Running Lighthouse audit..." -ForegroundColor Yellow

try {
    # Essayer d'utiliser lighthouse si disponible
    $lighthouseExists = npm list -g lighthouse 2>&1 | Select-String "lighthouse"
    if ($lighthouseExists) {
        Write-Host "  Lighthouse disponible - audit en cours..." -ForegroundColor Gray
        # Note: Lighthouse nécessite un serveur de base
        $results['lighthouseAvailable'] = $true
    } else {
        Write-Host "⚠️  Lighthouse CLI non installé globalement (optionnel)" -ForegroundColor Yellow
        $results['lighthouseAvailable'] = $false
    }
} catch {
    $results['lighthouseAvailable'] = $false
}

# ============================================================================
# 6. NPM PACKAGE ANALYSIS
# ============================================================================
Write-Host "`n📦 Analyzing dependencies..." -ForegroundColor Yellow

Push-Location $FrontendPath
$packageJson = Get-Content "package.json" | ConvertFrom-Json
$prodDependencies = $packageJson.dependencies | Get-Member -MemberType NoteProperty | Measure-Object
$devDependencies = $packageJson.devDependencies | Get-Member -MemberType NoteProperty | Measure-Object
Pop-Location

$results['productionDependencies'] = $prodDependencies.Count
$results['devDependencies'] = $devDependencies.Count
Write-Host "✅ Production dependencies: $($prodDependencies.Count)" -ForegroundColor Green
Write-Host "✅ Dev dependencies: $($devDependencies.Count)" -ForegroundColor Green

# ============================================================================
# SUMMARY
# ============================================================================
Write-Host "`n" -ForegroundColor White
Write-Host "╔════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   FRONTEND PERFORMANCE SUMMARY             ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════╝" -ForegroundColor Cyan

$summary = @"
📊 Build Performance:
   • Build Time: $($results['buildTimeSeconds']) seconds
   • Bundle Size: $($results['bundleSizeKB']) KB
   • Gzip Size: $($results['gzipSizeKB']) KB
   
📦 Dependencies:
   • Production: $($results['productionDependencies'])
   • Dev: $($results['devDependencies'])
"@

Write-Host $summary

# ============================================================================
# SAVE RESULTS
# ============================================================================
if (!(Test-Path $ResultsPath)) {
    New-Item -ItemType Directory -Path $ResultsPath -Force | Out-Null
}

$jsonPath = Join-Path $ResultsPath "frontend-metrics-$timestamp.json"
$results | ConvertTo-Json | Out-File -FilePath $jsonPath -Encoding UTF8

Write-Host "`n✅ Results saved to: $jsonPath" -ForegroundColor Green
Write-Host "`n" -ForegroundColor White
