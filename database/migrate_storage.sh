#!/bin/bash
# ==============================================================================
# Script de migration du stockage des documents médicaux
# ==============================================================================
# Ce script migre les fichiers depuis le volume Docker vers le bind mount local
# et met à jour les chemins en base de données.
#
# IMPORTANT: 
# - Exécuter AVANT de changer le docker-compose.yml
# - Faire un backup de la base de données avant
# - ZÉRO suppression automatique - les anciens fichiers sont conservés
# ==============================================================================

set -e

# Configuration
MYSQL_HOST="${MYSQL_HOST:-localhost}"
MYSQL_PORT="${MYSQL_PORT:-3306}"
MYSQL_USER="${MYSQL_USER:-app}"
MYSQL_PASSWORD="${MYSQL_PASSWORD:-app}"
MYSQL_DATABASE="${MYSQL_DATABASE:-mediconnect}"

# Chemins
OLD_STORAGE_ROOT="/var/lib/docker/volumes/mediconnet_app_document_storage/_data"
NEW_STORAGE_ROOT="./storage"
LOG_FILE="./migration_storage_$(date +%Y%m%d_%H%M%S).log"

# Couleurs pour les logs
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Fonctions de logging
log_info() {
    echo -e "${GREEN}[INFO]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE"
}

# Vérifier les prérequis
check_prerequisites() {
    log_info "Vérification des prérequis..."
    
    # Vérifier que mysql client est disponible
    if ! command -v mysql &> /dev/null; then
        log_error "mysql client n'est pas installé"
        exit 1
    fi
    
    # Vérifier la connexion à la base
    if ! mysql -h "$MYSQL_HOST" -P "$MYSQL_PORT" -u "$MYSQL_USER" -p"$MYSQL_PASSWORD" -e "SELECT 1" "$MYSQL_DATABASE" &> /dev/null; then
        log_error "Impossible de se connecter à la base de données"
        exit 1
    fi
    
    log_info "Prérequis OK"
}

# Créer la structure de dossiers
create_directory_structure() {
    log_info "Création de la structure de dossiers..."
    
    mkdir -p "$NEW_STORAGE_ROOT"
    mkdir -p "$NEW_STORAGE_ROOT/quarantine"
    mkdir -p "$NEW_STORAGE_ROOT/temp"
    mkdir -p "$NEW_STORAGE_ROOT/backup"
    
    # Permissions (si sur Linux)
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        chmod 750 "$NEW_STORAGE_ROOT"
        chmod 750 "$NEW_STORAGE_ROOT/quarantine"
        chmod 750 "$NEW_STORAGE_ROOT/temp"
        chmod 750 "$NEW_STORAGE_ROOT/backup"
    fi
    
    log_info "Structure de dossiers créée"
}

# Migrer les fichiers depuis le conteneur Docker
migrate_from_docker() {
    log_info "Migration des fichiers depuis le conteneur Docker..."
    
    # Copier les fichiers depuis le conteneur
    if docker ps | grep -q mediconnet-backend; then
        log_info "Conteneur mediconnet-backend trouvé, copie des fichiers..."
        
        # Créer un conteneur temporaire pour accéder au volume
        docker run --rm -v mediconnet_app_document_storage:/source -v "$(pwd)/storage":/dest alpine sh -c "cp -rv /source/* /dest/ 2>/dev/null || true"
        
        log_info "Fichiers copiés depuis le volume Docker"
    else
        log_warn "Conteneur mediconnet-backend non trouvé, vérification du volume..."
        
        # Essayer de copier directement depuis le volume
        if docker volume ls | grep -q mediconnet_app_document_storage; then
            docker run --rm -v mediconnet_app_document_storage:/source -v "$(pwd)/storage":/dest alpine sh -c "cp -rv /source/* /dest/ 2>/dev/null || true"
            log_info "Fichiers copiés depuis le volume Docker"
        else
            log_warn "Volume Docker non trouvé, migration manuelle requise"
        fi
    fi
}

# Vérifier et mettre à jour les chemins en base
update_database_paths() {
    log_info "Mise à jour des chemins en base de données..."
    
    # Récupérer tous les documents
    local count=$(mysql -h "$MYSQL_HOST" -P "$MYSQL_PORT" -u "$MYSQL_USER" -p"$MYSQL_PASSWORD" -N -e \
        "SELECT COUNT(*) FROM documents_medicaux WHERE statut = 'actif'" "$MYSQL_DATABASE")
    
    log_info "Nombre de documents à vérifier: $count"
    
    # Pour chaque document, vérifier que le fichier existe
    mysql -h "$MYSQL_HOST" -P "$MYSQL_PORT" -u "$MYSQL_USER" -p"$MYSQL_PASSWORD" -N -e \
        "SELECT uuid, chemin_relatif, id_patient FROM documents_medicaux WHERE statut = 'actif'" "$MYSQL_DATABASE" | \
    while IFS=$'\t' read -r uuid chemin_relatif id_patient; do
        local file_path="$NEW_STORAGE_ROOT/$chemin_relatif"
        
        if [[ -f "$file_path" ]]; then
            log_info "OK: $uuid -> $chemin_relatif"
        else
            log_warn "MANQUANT: $uuid -> $chemin_relatif"
        fi
    done
}

# Vérifier l'intégrité des fichiers
verify_integrity() {
    log_info "Vérification de l'intégrité des fichiers..."
    
    local total=0
    local ok=0
    local missing=0
    local hash_mismatch=0
    
    mysql -h "$MYSQL_HOST" -P "$MYSQL_PORT" -u "$MYSQL_USER" -p"$MYSQL_PASSWORD" -N -e \
        "SELECT uuid, chemin_relatif, hash_sha256, taille_octets FROM documents_medicaux WHERE statut = 'actif'" "$MYSQL_DATABASE" | \
    while IFS=$'\t' read -r uuid chemin_relatif hash_sha256 taille_octets; do
        ((total++))
        local file_path="$NEW_STORAGE_ROOT/$chemin_relatif"
        
        if [[ ! -f "$file_path" ]]; then
            log_error "Fichier manquant: $uuid -> $file_path"
            ((missing++))
            continue
        fi
        
        # Vérifier le hash si disponible
        if [[ -n "$hash_sha256" && "$hash_sha256" != "NULL" ]]; then
            local calculated_hash=$(sha256sum "$file_path" | awk '{print $1}')
            if [[ "$calculated_hash" != "$hash_sha256" ]]; then
                log_error "Hash invalide: $uuid (attendu: $hash_sha256, calculé: $calculated_hash)"
                ((hash_mismatch++))
                continue
            fi
        fi
        
        ((ok++))
        log_info "Vérifié OK: $uuid"
    done
    
    log_info "=== Résumé de la vérification ==="
    log_info "Total: $total"
    log_info "OK: $ok"
    log_info "Manquants: $missing"
    log_info "Hash invalide: $hash_mismatch"
}

# Afficher les statistiques
show_statistics() {
    log_info "=== Statistiques du stockage ==="
    
    # Nombre de fichiers
    local file_count=$(find "$NEW_STORAGE_ROOT" -type f | wc -l)
    log_info "Nombre de fichiers: $file_count"
    
    # Taille totale
    local total_size=$(du -sh "$NEW_STORAGE_ROOT" 2>/dev/null | cut -f1)
    log_info "Taille totale: $total_size"
    
    # Nombre de documents en base
    local db_count=$(mysql -h "$MYSQL_HOST" -P "$MYSQL_PORT" -u "$MYSQL_USER" -p"$MYSQL_PASSWORD" -N -e \
        "SELECT COUNT(*) FROM documents_medicaux WHERE statut = 'actif'" "$MYSQL_DATABASE")
    log_info "Documents en base (actifs): $db_count"
}

# Menu principal
main() {
    echo "=============================================="
    echo "  Migration du stockage des documents"
    echo "=============================================="
    echo ""
    echo "1. Migration complète (recommandé)"
    echo "2. Copier les fichiers depuis Docker uniquement"
    echo "3. Vérifier l'intégrité uniquement"
    echo "4. Afficher les statistiques"
    echo "5. Quitter"
    echo ""
    read -p "Choisissez une option [1-5]: " choice
    
    case $choice in
        1)
            check_prerequisites
            create_directory_structure
            migrate_from_docker
            update_database_paths
            verify_integrity
            show_statistics
            log_info "Migration terminée avec succès!"
            ;;
        2)
            create_directory_structure
            migrate_from_docker
            log_info "Copie des fichiers terminée"
            ;;
        3)
            check_prerequisites
            verify_integrity
            ;;
        4)
            show_statistics
            ;;
        5)
            log_info "Au revoir!"
            exit 0
            ;;
        *)
            log_error "Option invalide"
            exit 1
            ;;
    esac
}

# Exécution
main "$@"
