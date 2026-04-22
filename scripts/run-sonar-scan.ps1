# Lance l'analyse SonarQube complete du backend :
# 1. Begin scanner
# 2. Build + tests avec couverture OpenCover
# 3. End scanner (upload)
#
# Utilisation :
#   .\scripts\run-sonar-scan.ps1 -SonarToken "squ_xxxxxx"
#   .\scripts\run-sonar-scan.ps1                         # lit SONAR_TOKEN depuis l'env

param(
    [string]$SonarHostUrl = "http://localhost:9000",
    [string]$ProjectKey = "mediconnect",
    [string]$SonarToken = $env:SONAR_TOKEN
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($SonarToken)) {
    Write-Error "SonarToken manquant. Fournissez -SonarToken ou definissez `$env:SONAR_TOKEN."
    exit 1
}

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    # Nettoyer la precedente couverture
    $coverageDir = Join-Path $repoRoot "Mediconnet-Backend.Tests/TestResults"
    if (Test-Path $coverageDir) {
        Remove-Item -Recurse -Force $coverageDir
    }

    # Installer dotnet-sonarscanner globalement si absent
    $scanner = dotnet tool list -g 2>$null | Select-String "dotnet-sonarscanner"
    if (-not $scanner) {
        Write-Host "Installation de dotnet-sonarscanner..."
        dotnet tool install --global dotnet-sonarscanner
    }

    $coveragePath = "Mediconnet-Backend.Tests/TestResults/**/coverage.opencover.xml"

    # Exclusions : code non metier (migrations, DTOs, entites, DbContext...)
    $coverageExclusions = @(
        "Mediconnet-Backend/Migrations/**/*",
        "Mediconnet-Backend/Program.cs",
        "Mediconnet-Backend/Data/**/*",
        "Mediconnet-Backend/Core/Entities/**/*",
        "Mediconnet-Backend/Core/Enums/**/*",
        "Mediconnet-Backend/DTOs/**/*",
        "Mediconnet-Backend/Hubs/**/*",
        "Mediconnet-Backend/Properties/**/*",
        "Mediconnet-Backend/Configuration/**/*"
    ) -join ","

    Write-Host "=== SonarScanner BEGIN ==="
    dotnet sonarscanner begin `
        /k:"$ProjectKey" `
        /d:sonar.host.url="$SonarHostUrl" `
        /d:sonar.token="$SonarToken" `
        /d:sonar.cs.opencover.reportsPaths="$coveragePath" `
        /d:sonar.coverage.exclusions="$coverageExclusions" `
        /d:sonar.exclusions="**/Migrations/**,**/bin/**,**/obj/**,**/TestResults/**"

    Write-Host "=== BUILD ==="
    dotnet build Mediconnet-Backend/Mediconnet-Backend.csproj -c Debug --nologo

    Write-Host "=== TESTS + COVERAGE ==="
    dotnet test Mediconnet-Backend.Tests/Mediconnet-Backend.Tests.csproj `
        --no-build `
        --nologo `
        --settings Mediconnet-Backend.Tests/coverlet.runsettings `
        --results-directory Mediconnet-Backend.Tests/TestResults

    Write-Host "=== SonarScanner END ==="
    dotnet sonarscanner end /d:sonar.token="$SonarToken"

    Write-Host ""
    Write-Host "Analyse terminee. Consultez $SonarHostUrl/dashboard?id=$ProjectKey"
}
finally {
    Pop-Location
}
