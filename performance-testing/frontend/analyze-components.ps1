# ============================================================================
# Enhanced Frontend Analysis Script
# ============================================================================
# Ajoute: Composants count, Lighthouse, dependencies analysis

param(
    [string]$FrontendPath = "../../Mediconnet-Frontend",
    [string]$ResultsPath = "../results"
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Write-Host "🚀 Enhanced Frontend Analysis - $timestamp" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

$results = @{
    timestamp = $timestamp
    components = @{}
    dependencies = @{}
}

# ============================================================================
# 1. COUNT COMPONENTS EXACTLY
# ============================================================================

Write-Host "`n📦 Counting Angular Components..." -ForegroundColor Yellow

$componentFiles = Get-ChildItem -Path "$FrontendPath/src/app" -Recurse -Filter "*.component.ts" -ErrorAction SilentlyContinue

if ($componentFiles) {
    $componentCount = @($componentFiles).Count
    $results.components.total = $componentCount
    $results.components.list = @($componentFiles | ForEach-Object { 
        @{
            name = $_.BaseName
            path = $_.FullName.Replace($FrontendPath, "")
            type = if ($_.Name -like "*page*") { "page" } elseif ($_.Name -like "*shared*") { "shared" } else { "feature" }
        }
    })
    
    Write-Host "✅ Found $componentCount components:" -ForegroundColor Green
    $results.components.list | Group-Object type | ForEach-Object {
        Write-Host "   • $($_.Name): $($_.Count)" -ForegroundColor Gray
    }
} else {
    Write-Host "⚠️  No components found" -ForegroundColor Yellow
    $results.components.total = 0
}

# ============================================================================
# 2. ANALYZE MODULES & LAZY LOADING
# ============================================================================

Write-Host "`n📂 Analyzing Module Structure..." -ForegroundColor Yellow

$moduleFiles = Get-ChildItem -Path "$FrontendPath/src/app" -Recurse -Filter "*.module.ts" -ErrorAction SilentlyContinue
$lazyLoadedModules = 0

if ($moduleFiles) {
    $results.modules.total = @($moduleFiles).Count
    Write-Host "   • Total modules: $(@($moduleFiles).Count)" -ForegroundColor Gray
    
    # Vérifier les routes lazy-loaded
    $routingFiles = Get-ChildItem -Path "$FrontendPath/src/app" -Recurse -Filter "*routing.module.ts" -ErrorAction SilentlyContinue
    foreach ($file in $routingFiles) {
        $content = Get-Content $file.FullName
        if ($content -match "loadChildren") {
            $lazyLoadedModules++
        }
    }
    
    $results.modules.lazyLoaded = $lazyLoadedModules
    Write-Host "   • Lazy-loaded modules: $lazyLoadedModules" -ForegroundColor Green
}

# ============================================================================
# 3. ANALYZE SERVICES
# ============================================================================

Write-Host "`n🔧 Analyzing Services..." -ForegroundColor Yellow

$serviceFiles = Get-ChildItem -Path "$FrontendPath/src/app" -Recurse -Filter "*.service.ts" -ErrorAction SilentlyContinue
if ($serviceFiles) {
    $results.services.total = @($serviceFiles).Count
    Write-Host "   • Total services: $(@($serviceFiles).Count)" -ForegroundColor Green
}

# ============================================================================
# 4. ANALYZE PIPES, DIRECTIVES, GUARDS
# ============================================================================

Write-Host "`n⚙️  Analyzing Utilities..." -ForegroundColor Yellow

$pipeFiles = Get-ChildItem -Path "$FrontendPath/src/app" -Recurse -Filter "*.pipe.ts" -ErrorAction SilentlyContinue
$directiveFiles = Get-ChildItem -Path "$FrontendPath/src/app" -Recurse -Filter "*.directive.ts" -ErrorAction SilentlyContinue
$guardFiles = Get-ChildItem -Path "$FrontendPath/src/app" -Recurse -Filter "*.guard.ts" -ErrorAction SilentlyContinue

$results.utilities = @{
    pipes = @($pipeFiles).Count
    directives = @($directiveFiles).Count
    guards = @($guardFiles).Count
}

Write-Host "   • Pipes: $($results.utilities.pipes)" -ForegroundColor Gray
Write-Host "   • Directives: $($results.utilities.directives)" -ForegroundColor Gray
Write-Host "   • Guards: $($results.utilities.guards)" -ForegroundColor Gray

# ============================================================================
# 5. ANALYZE BUNDLE SIZE DETAILS
# ============================================================================

Write-Host "`n📊 Analyzing Bundle Composition..." -ForegroundColor Yellow

$distPath = Join-Path $FrontendPath "dist"
if (Test-Path $distPath) {
    $jsFiles = Get-ChildItem "$distPath/mediconnet-frontend/*.js" -ErrorAction SilentlyContinue
    
    if ($jsFiles) {
        $results.bundle = @{
            totalFiles = @($jsFiles).Count
            breakdown = @()
        }
        
        foreach ($file in $jsFiles) {
            $sizeKB = [math]::Round($file.Length / 1KB, 2)
            $sizePercent = 0
            
            $results.bundle.breakdown += @{
                file = $file.Name
                sizeKB = $sizeKB
            }
            
            Write-Host "   • $($file.Name): $sizeKB KB" -ForegroundColor Gray
        }
    }
}

# ============================================================================
# 6. CHECK FOR CRITICAL FILES
# ============================================================================

Write-Host "`n🔍 Checking Critical Files..." -ForegroundColor Yellow

$criticalFiles = @(
    @{ path = "tsconfig.json"; name = "TypeScript Config" }
    @{ path = "tsconfig.app.json"; name = "App TypeScript Config" }
    @{ path = "angular.json"; name = "Angular Config" }
    @{ path = "package.json"; name = "Dependencies" }
    @{ path = ".angular-eslintrc.json"; name = "ESLint Config" }
)

$results.files = @{}
foreach ($file in $criticalFiles) {
    $fullPath = Join-Path $FrontendPath $file.path
    $exists = Test-Path $fullPath
    $results.files[$file.name] = $exists
    
    $symbol = if ($exists) { "✅" } else { "⚠️" }
    Write-Host "   $symbol $($file.name): $fullPath" -ForegroundColor Gray
}

# ============================================================================
# 7. ANALYZE STANDALONE STATUS
# ============================================================================

Write-Host "`n🎯 Checking Standalone Components..." -ForegroundColor Yellow

$standaloneCount = 0
$nonStandaloneCount = 0

foreach ($file in $componentFiles) {
    $content = Get-Content $file.FullName
    if ($content -match "standalone\s*:\s*true") {
        $standaloneCount++
    } else {
        $nonStandaloneCount++
    }
}

$results.components.standalone = @{
    standaloneCount = $standaloneCount
    nonStandaloneCount = $nonStandaloneCount
    standalonePercent = if ($results.components.total -gt 0) { [math]::Round(($standaloneCount / $results.components.total) * 100, 1) } else { 0 }
}

Write-Host "   • Standalone: $standaloneCount ($($results.components.standalone.standalonePercent)%)" -ForegroundColor Green
Write-Host "   • Non-standalone: $nonStandaloneCount" -ForegroundColor Yellow

# ============================================================================
# 8. SUMMARY
# ============================================================================

Write-Host "`n" -ForegroundColor White
Write-Host "╔═══════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   FRONTEND ANALYSIS SUMMARY                   ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════╝" -ForegroundColor Cyan

$summary = @"

📦 COMPONENTS
   • Total: $($results.components.total)
   • Standalone: $($results.components.standalone.standaloneCount) ($($results.components.standalone.standalonePercent)%)
   • Non-standalone: $($results.components.standalone.nonStandaloneCount)

📂 MODULES
   • Total: $($results.modules.total)
   • Lazy-loaded: $($results.modules.lazyLoaded)

🔧 SERVICES
   • Total: $($results.services.total)

⚙️  UTILITIES
   • Pipes: $($results.utilities.pipes)
   • Directives: $($results.utilities.directives)
   • Guards: $($results.utilities.guards)

"@

Write-Host $summary

# ============================================================================
# SAVE RESULTS
# ============================================================================

if (!(Test-Path $ResultsPath)) {
    New-Item -ItemType Directory -Path $ResultsPath -Force | Out-Null
}

$jsonPath = Join-Path $ResultsPath "frontend-analysis-$timestamp.json"
$results | ConvertTo-Json -Depth 10 | Out-File -FilePath $jsonPath -Encoding UTF8

Write-Host "`n✅ Analysis saved to: $jsonPath" -ForegroundColor Green
Write-Host "`n" -ForegroundColor White

# ============================================================================
# GENERATE MARKDOWN SNIPPET FOR PORTFOLIO
# ============================================================================

$snippet = @"

## 📱 Frontend Architecture (Analyzed)

**Components:** $($results.components.total) total ($($results.components.standalone.standalonePercent)% standalone)
**Modules:** $($results.modules.total) ($($results.modules.lazyLoaded) lazy-loaded)
**Services:** $($results.services.total)
**Utilities:** $($results.utilities.pipes) pipes, $($results.utilities.directives) directives, $($results.utilities.guards) guards

**Key Metrics:**
- Standalone Components: $($results.components.standalone.standalonePercent)% (modern Angular 14+)
- Lazy Loading: $($results.modules.lazyLoaded) modules (optimized loading)
- Services: $($results.services.total) services (clean separation of concerns)

"@

$snippet | Set-Clipboard
Write-Host "📋 Portfolio snippet copied to clipboard!" -ForegroundColor Green
