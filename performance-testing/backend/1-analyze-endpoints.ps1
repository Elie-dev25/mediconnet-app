# Backend Endpoints Analysis Script

param(
    [string]$BackendPath = "../../Mediconnet-Backend",
    [string]$ResultsPath = "../results"
)

$ErrorActionPreference = "Continue"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Write-Host "[*] Backend Endpoints Analysis - $timestamp" -ForegroundColor Cyan

$results = @{
    timestamp = $timestamp
}

# 1. Count Controllers
Write-Host "[*] Counting Controllers..." -ForegroundColor Yellow
$controllerFiles = Get-ChildItem -Path "$BackendPath/Controllers" -Filter "*Controller.cs" -ErrorAction SilentlyContinue
$results.controllers = @($controllerFiles).Count
Write-Host "[OK] Controllers: $($results.controllers)" -ForegroundColor Green

# 2. Count Endpoints from Controllers
Write-Host "[*] Analyzing endpoints..." -ForegroundColor Yellow
$endpointCount = 0
$controllerFiles | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    # Count [HttpGet], [HttpPost], [HttpPut], [HttpDelete], [HttpPatch]
    $getCount = ([regex]::Matches($content, "\[HttpGet")).Count
    $postCount = ([regex]::Matches($content, "\[HttpPost")).Count
    $putCount = ([regex]::Matches($content, "\[HttpPut")).Count
    $deleteCount = ([regex]::Matches($content, "\[HttpDelete")).Count
    $patchCount = ([regex]::Matches($content, "\[HttpPatch")).Count
    $endpointCount += $getCount + $postCount + $putCount + $deleteCount + $patchCount
}
$results.endpoints = $endpointCount
Write-Host "[OK] Total Endpoints: $endpointCount" -ForegroundColor Green

# 3. Count Data Models/Entities
Write-Host "[*] Counting Entities..." -ForegroundColor Yellow
$dataPath = "$BackendPath/Data"
if (Test-Path $dataPath) {
    $entityFiles = Get-ChildItem -Path $dataPath -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.Name -notlike "*Context*" -and $_.Name -notlike "*Configuration*" }
    $results.entities = @($entityFiles).Count
    Write-Host "[OK] Entities: $($results.entities)" -ForegroundColor Green
}

# 4. Count Migrations
Write-Host "[*] Counting Migrations..." -ForegroundColor Yellow
$migrationsPath = "$BackendPath/Migrations"
if (Test-Path $migrationsPath) {
    $migrationFiles = Get-ChildItem -Path $migrationsPath -Filter "*.cs" -ErrorAction SilentlyContinue
    $results.migrations = @($migrationFiles).Count
    Write-Host "[OK] Migrations: $($results.migrations)" -ForegroundColor Green
}

# 5. Count Services
Write-Host "[*] Counting Services..." -ForegroundColor Yellow
$servicesPath = "$BackendPath/Services"
if (Test-Path $servicesPath) {
    $serviceFiles = Get-ChildItem -Path $servicesPath -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.Name -notlike "*Interface*" }
    $results.services = @($serviceFiles).Count
    Write-Host "[OK] Services: $($results.services)" -ForegroundColor Green
}

# 6. Count Repositories
Write-Host "[*] Counting Repositories..." -ForegroundColor Yellow
$reposPath = "$BackendPath/Data"
if (Test-Path $reposPath) {
    $repoFiles = Get-ChildItem -Path $reposPath -Recurse -Filter "*Repository.cs" -ErrorAction SilentlyContinue
    $results.repositories = @($repoFiles).Count
    Write-Host "[OK] Repositories: $($results.repositories)" -ForegroundColor Green
}

# 7. Save Results
$resultsFile = "$ResultsPath/backend-analysis-$timestamp.json"
$results | ConvertTo-Json | Out-File $resultsFile
Write-Host "[OK] Results saved: $resultsFile" -ForegroundColor Green

# 8. Display Summary
Write-Host "`n========== BACKEND METRICS ==========" -ForegroundColor Cyan
Write-Host "Controllers:  $($results.controllers)" -ForegroundColor White
Write-Host "Endpoints:    $($results.endpoints)" -ForegroundColor White
Write-Host "Entities:     $($results.entities)" -ForegroundColor White
Write-Host "Migrations:   $($results.migrations)" -ForegroundColor White
Write-Host "Services:     $($results.services)" -ForegroundColor White
Write-Host "Repositories: $($results.repositories)" -ForegroundColor White
Write-Host "=====================================" -ForegroundColor Cyan

$summary = "Controllers: $($results.controllers) | Endpoints: $($results.endpoints) | Entities: $($results.entities) | Migrations: $($results.migrations)"
$summary | Set-Clipboard
Write-Host "[OK] Summary copied to clipboard" -ForegroundColor Green
