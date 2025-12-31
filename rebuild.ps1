# Script de rebuild complet pour Docker Compose

Write-Host "Arrêt des conteneurs..." -ForegroundColor Green
docker-compose down

Write-Host "Nettoyage des images et volumes..." -ForegroundColor Green
docker system prune -f

Write-Host "Suppression du répertoire dist..." -ForegroundColor Green
if (Test-Path "Mediconnet-Frontend\dist") {
    Remove-Item -Recurse -Force "Mediconnet-Frontend\dist"
}

Write-Host "Rebuild du projet..." -ForegroundColor Green
docker-compose up --build

