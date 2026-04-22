# Analyse Sonar du frontend Angular (100% Docker)
#
# Lance sonarsource/sonar-scanner-cli dans un conteneur Docker.
#
# PREREQUIS :
#   - Docker Desktop lance
#   - SonarQube accessible via host.docker.internal:9000
#
# Utilisation :
#   .\scripts\run-sonar-scan-frontend.ps1 -SonarToken "squ_xxxxxx"

param(
    [string]$SonarHostUrl = "http://host.docker.internal:9000",
    [string]$ProjectKey = "mediconnect-front",
    [string]$SonarToken = $env:SONAR_TOKEN
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($SonarToken)) {
    Write-Error "SonarToken manquant. Fournissez -SonarToken ou definissez `$env:SONAR_TOKEN."
    exit 1
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$frontendDir = Join-Path $repoRoot "Mediconnet-Frontend"

if (-not (Test-Path $frontendDir)) {
    Write-Error "Dossier frontend introuvable : $frontendDir"
    exit 1
}

Write-Host "Lancement du scan Sonar sur le frontend..."

docker run --rm `
    --add-host=host.docker.internal:host-gateway `
    -v "${frontendDir}:/usr/src" `
    -w /usr/src `
    -e SONAR_HOST_URL=$SonarHostUrl `
    -e SONAR_TOKEN=$SonarToken `
    sonarsource/sonar-scanner-cli:latest

Write-Host ""
Write-Host "Analyse terminee. Consultez http://localhost:9000/dashboard?id=$ProjectKey"
