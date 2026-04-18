# Frontend Components Analysis Script
# Counts exact number of components, services, modules, etc.

param(
    [string]$FrontendPath = "../../Mediconnet-Frontend",
    [string]$ResultsPath = "../results"
)

$ErrorActionPreference = "Continue"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Write-Host "[*] Frontend Component Analysis - $timestamp" -ForegroundColor Cyan

$results = @{
    timestamp = $timestamp
}

# 1. Count Components
Write-Host "[*] Counting Angular Components..." -ForegroundColor Yellow
$componentFiles = Get-ChildItem -Path "$FrontendPath/src/app" -Recurse -Filter "*.component.ts" -ErrorAction SilentlyContinue
$results.components = @($componentFiles).Count
Write-Host "[OK] Components: $($results.components)" -ForegroundColor Green

# 2. Count Services
Write-Host "[*] Counting Services..." -ForegroundColor Yellow
$serviceFiles = Get-ChildItem -Path "$FrontendPath/src/app" -Recurse -Filter "*.service.ts" -ErrorAction SilentlyContinue
$results.services = @($serviceFiles).Count
Write-Host "[OK] Services: $($results.services)" -ForegroundColor Green

# 3. Count Modules
Write-Host "[*] Counting Modules..." -ForegroundColor Yellow
$moduleFiles = Get-ChildItem -Path "$FrontendPath/src/app" -Recurse -Filter "*.module.ts" -ErrorAction SilentlyContinue
$results.modules = @($moduleFiles).Count
Write-Host "[OK] Modules: $($results.modules)" -ForegroundColor Green

# 4. Count Pipes
Write-Host "[*] Counting Pipes..." -ForegroundColor Yellow
$pipeFiles = Get-ChildItem -Path "$FrontendPath/src/app" -Recurse -Filter "*.pipe.ts" -ErrorAction SilentlyContinue
$results.pipes = @($pipeFiles).Count
Write-Host "[OK] Pipes: $($results.pipes)" -ForegroundColor Green

# 5. Count Directives
Write-Host "[*] Counting Directives..." -ForegroundColor Yellow
$directiveFiles = Get-ChildItem -Path "$FrontendPath/src/app" -Recurse -Filter "*.directive.ts" -ErrorAction SilentlyContinue
$results.directives = @($directiveFiles).Count
Write-Host "[OK] Directives: $($results.directives)" -ForegroundColor Green

# 6. Count Guards
Write-Host "[*] Counting Guards..." -ForegroundColor Yellow
$guardFiles = Get-ChildItem -Path "$FrontendPath/src/app" -Recurse -Filter "*guard*" -ErrorAction SilentlyContinue
$results.guards = @($guardFiles).Count
Write-Host "[OK] Guards: $($results.guards)" -ForegroundColor Green

# 7. Count Standalone Components
Write-Host "[*] Detecting Standalone Components..." -ForegroundColor Yellow
$standaloneCount = 0
$componentFiles | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    if ($content -match "standalone\s*:\s*true") {
        $standaloneCount++
    }
}
$results.standalonePercent = if ($results.components -gt 0) { [math]::Round(($standaloneCount / $results.components) * 100) } else { 0 }
Write-Host "[OK] Standalone: $standaloneCount of $($results.components) (" + $results.standalonePercent + "%)" -ForegroundColor Green

# 8. Save Results
$resultsFile = "$ResultsPath/frontend-analysis-$timestamp.json"
$results | ConvertTo-Json | Out-File $resultsFile
Write-Host "[OK] Results saved: $resultsFile" -ForegroundColor Green

# 9. Display Summary
Write-Host "`n========== SUMMARY ==========" -ForegroundColor Cyan
Write-Host "Components:    $($results.components)" -ForegroundColor White
Write-Host "Services:      $($results.services)" -ForegroundColor White
Write-Host "Modules:       $($results.modules)" -ForegroundColor White
Write-Host "Pipes:         $($results.pipes)" -ForegroundColor White
Write-Host "Directives:    $($results.directives)" -ForegroundColor White
Write-Host "Guards:        $($results.guards)" -ForegroundColor White
Write-Host "Standalone:    $($results.standalonePercent)%" -ForegroundColor White
Write-Host "===========================" -ForegroundColor Cyan

# Copy to clipboard
$summary = "Components: $($results.components) | Services: $($results.services) | Modules: $($results.modules) | Standalone: $($results.standalonePercent)%"
$summary | Set-Clipboard
Write-Host "[OK] Summary copied to clipboard" -ForegroundColor Green
