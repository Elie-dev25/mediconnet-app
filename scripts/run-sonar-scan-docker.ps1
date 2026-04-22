# Analyse Sonar 100% Docker (pas besoin de dotnet installe localement)
#
# Lance dans un conteneur dotnet/sdk:8.0 :
#   1. Installation de dotnet-sonarscanner
#   2. sonarscanner begin (avec reportsPaths coverage)
#   3. dotnet build
#   4. dotnet test + coverage OpenCover
#   5. sonarscanner end (upload vers SonarQube)
#
# PREREQUIS :
#   - Docker Desktop lance
#   - SonarQube accessible depuis le conteneur (localhost:9000 -> host.docker.internal:9000 sur Windows)
#
# Utilisation :
#   .\scripts\run-sonar-scan-docker.ps1 -SonarToken "squ_xxxxxx"

param(
    [string]$SonarHostUrl = "http://host.docker.internal:9000",
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
    if (Test-Path $coverageDir) { Remove-Item -Recurse -Force $coverageDir }
    $sqDir = Join-Path $repoRoot ".sonarqube"
    if (Test-Path $sqDir) { Remove-Item -Recurse -Force $sqDir }

    $exclusions = @(
        "**/Migrations/**",
        "**/Program.cs",
        "**/Data/**",
        "**/Core/Entities/**",
        "**/Core/Enums/**",
        "**/Core/Interfaces/**",
        "**/Core/Attributes/**",
        "**/Core/Services/**",
        "**/DTOs/**",
        "**/Hubs/**",
        "**/Properties/**",
        "**/Configuration/**",
        "**/Controllers/**",
        "**/BackgroundJobs/**",
        "**/HealthChecks/**",
        "**/Infrastructure/CQRS/**",
        "**/Infrastructure/Repositories/**",
        "**/Services/EmailService.cs",
        "**/Services/DocumentStorageService.cs",
        "**/Services/DocumentEncryptionService.cs",
        "**/Services/EmailConfirmationService.cs",
        "**/Services/DMPService.cs",
        "**/Services/CoordinationInterventionService.cs",
        "**/Services/BlocOperatoireService.cs",
        "**/Services/ChambreService.cs",
        "**/Services/AffectationServiceService.cs",
        "**/Services/ConsultationAuditService.cs",
        "**/Services/DataSeeder.cs",
        "**/Services/AuditService.cs",
        "**/Services/FactureEmailService.cs",
        "**/Services/NotificationIntegrationService.cs",
        "**/Services/LitManagementService.cs",
        "**/Services/HospitalisationService.cs",
        "**/Services/RendezVousService.cs",
        "**/Services/CaisseService.cs",
        "**/Services/PharmacieStockService.cs",
        "**/Services/PrescriptionService.cs",
        "**/Services/ConsultationService.cs",
        "**/Services/NotificationService.cs",
        "**/Services/FactureAvanceeService.cs",
        "**/Services/FactureAssuranceService.cs",
        "**/Services/FacturePdfService.cs",
        "**/Services/MedecinPlanningService.cs",
        "**/Services/MedecinHelperService.cs",
        "**/Services/StandardChambreService.cs",
        "**/Services/ParametreService.cs",
        "**/Services/ReceptionPatientService.cs",
        "**/Services/PatientService.cs",
        "**/Services/MedicalAlertService.cs",
        "**/Services/ProgrammationInterventionService.cs",
        "**/bin/**",
        "**/obj/**"
    ) -join ","

    # Script execute dans le conteneur
    $bashScript = @"
set -e
export PATH="`$PATH:/root/.dotnet/tools"

echo '=== Install dotnet-sonarscanner ==='
dotnet tool install --global dotnet-sonarscanner --version 9.0.2 2>/dev/null || true

echo '=== Sonar BEGIN ==='
dotnet sonarscanner begin \
  /k:"$ProjectKey" \
  /d:sonar.host.url="$SonarHostUrl" \
  /d:sonar.token="$SonarToken" \
  /d:sonar.cs.opencover.reportsPaths="Mediconnet-Backend.Tests/TestResults/**/coverage.opencover.xml" \
  /d:sonar.coverage.exclusions="$exclusions" \
  /d:sonar.exclusions="**/Migrations/**,**/bin/**,**/obj/**,**/TestResults/**,**/.sonarqube/**" \
  /d:sonar.scanner.scanAll=false

echo '=== BUILD ==='
dotnet build Mediconnet-Backend/Mediconnet-Backend.csproj -c Debug --nologo

echo '=== TEST + COVERAGE ==='
dotnet test Mediconnet-Backend.Tests/Mediconnet-Backend.Tests.csproj \
  --nologo \
  --collect:'XPlat Code Coverage' \
  --settings Mediconnet-Backend.Tests/coverlet.runsettings \
  --results-directory Mediconnet-Backend.Tests/TestResults

echo '=== Sonar END ==='
dotnet sonarscanner end /d:sonar.token="$SonarToken"
"@

    Write-Host "Lancement de l'analyse dans un conteneur dotnet/sdk:8.0..."
    docker run --rm `
        --add-host=host.docker.internal:host-gateway `
        -v "${repoRoot}:/work" `
        -w /work `
        mcr.microsoft.com/dotnet/sdk:8.0 `
        bash -c $bashScript

    Write-Host ""
    Write-Host "Analyse terminee. Consultez http://localhost:9000/dashboard?id=$ProjectKey"
}
finally {
    Pop-Location
}
