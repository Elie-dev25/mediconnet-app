#!/bin/bash
# ==============================================================================
# Script de monitoring du stockage des documents médicaux
# Exécution: cron quotidien (0 2 * * *)
# ==============================================================================

set -e

# Configuration
STORAGE_PATH="/storage/mediConnect"
LOG_FILE="/var/log/mediconnect/storage_health.log"
ALERT_EMAIL="${ALERT_EMAIL:-admin@mediconnect.local}"
DISK_THRESHOLD=20  # Alerte si < 20% d'espace libre
SAMPLE_SIZE=10     # Nombre de fichiers à vérifier pour le hash
DB_HOST="${DB_HOST:-mediconnet-mysql}"
DB_USER="${DB_USER:-app}"
DB_PASS="${DB_PASS:-app}"
DB_NAME="${DB_NAME:-mediconnect}"

# Couleurs pour les logs
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Créer le répertoire de logs si nécessaire
mkdir -p "$(dirname "$LOG_FILE")"

# Fonction de logging
log() {
    local level=$1
    local message=$2
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    echo "[$timestamp] [$level] $message" >> "$LOG_FILE"
    
    case $level in
        "ERROR")   echo -e "${RED}[$level] $message${NC}" ;;
        "WARNING") echo -e "${YELLOW}[$level] $message${NC}" ;;
        "INFO")    echo -e "${GREEN}[$level] $message${NC}" ;;
        *)         echo "[$level] $message" ;;
    esac
}

# Fonction d'envoi d'alerte
send_alert() {
    local subject=$1
    local body=$2
    
    log "ALERT" "$subject: $body"
    
    # Envoyer par email si configuré
    if command -v mail &> /dev/null && [ -n "$ALERT_EMAIL" ]; then
        echo "$body" | mail -s "[MediConnect ALERT] $subject" "$ALERT_EMAIL"
    fi
    
    # Insérer l'alerte dans la base de données
    mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -e "
        INSERT INTO alertes_systeme (type_alerte, message, severite, created_at)
        VALUES ('storage_health', '$subject: $body', 'critical', NOW())
        ON DUPLICATE KEY UPDATE message = VALUES(message), created_at = NOW();
    " 2>/dev/null || true
}

# ==============================================================================
# 1. Vérification de l'espace disque
# ==============================================================================
check_disk_space() {
    log "INFO" "=== Vérification de l'espace disque ==="
    
    local disk_usage=$(df -h "$STORAGE_PATH" | awk 'NR==2 {print $5}' | tr -d '%')
    local disk_free=$((100 - disk_usage))
    local disk_available=$(df -h "$STORAGE_PATH" | awk 'NR==2 {print $4}')
    
    log "INFO" "Espace utilisé: ${disk_usage}%, Libre: ${disk_free}% (${disk_available})"
    
    if [ "$disk_free" -lt "$DISK_THRESHOLD" ]; then
        send_alert "Espace disque critique" "Seulement ${disk_free}% d'espace libre sur $STORAGE_PATH (${disk_available} disponible)"
        return 1
    fi
    
    return 0
}

# ==============================================================================
# 2. Vérification fichiers vs base de données
# ==============================================================================
check_files_vs_db() {
    log "INFO" "=== Vérification fichiers vs base de données ==="
    
    # Compter les fichiers physiques
    local file_count=$(find "$STORAGE_PATH" -type f \( -name "*.pdf" -o -name "*.png" -o -name "*.jpg" -o -name "*.jpeg" \) 2>/dev/null | wc -l)
    
    # Compter les documents en base
    local db_count=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -N -e "
        SELECT COUNT(*) FROM documents_medicaux WHERE statut = 'actif';
    " 2>/dev/null || echo "0")
    
    log "INFO" "Fichiers physiques: $file_count, Documents en base: $db_count"
    
    # Vérifier les fichiers orphelins (en base mais pas sur disque)
    local orphans=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -N -e "
        SELECT uuid, chemin_relatif FROM documents_medicaux 
        WHERE statut = 'actif' 
        LIMIT 100;
    " 2>/dev/null)
    
    local orphan_count=0
    while IFS=$'\t' read -r uuid path; do
        if [ -n "$path" ] && [ ! -f "$STORAGE_PATH/$path" ]; then
            log "WARNING" "Fichier manquant: $uuid -> $path"
            orphan_count=$((orphan_count + 1))
            
            # Mettre à jour le statut en base
            mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -e "
                UPDATE documents_medicaux SET statut = 'quarantaine' WHERE uuid = '$uuid';
                INSERT INTO verification_integrite (document_uuid, statut_verification, type_verification, timestamp)
                VALUES ('$uuid', 'fichier_absent', 'automatique', NOW());
            " 2>/dev/null || true
        fi
    done <<< "$orphans"
    
    if [ "$orphan_count" -gt 0 ]; then
        send_alert "Fichiers manquants détectés" "$orphan_count fichier(s) référencé(s) en base mais absent(s) du disque"
        return 1
    fi
    
    return 0
}

# ==============================================================================
# 3. Vérification d'intégrité (échantillon de hash)
# ==============================================================================
check_hash_integrity() {
    log "INFO" "=== Vérification d'intégrité (échantillon) ==="
    
    # Sélectionner un échantillon aléatoire de documents avec hash
    local samples=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -N -e "
        SELECT uuid, chemin_relatif, hash_sha256 
        FROM documents_medicaux 
        WHERE statut = 'actif' AND hash_sha256 IS NOT NULL
        ORDER BY RAND()
        LIMIT $SAMPLE_SIZE;
    " 2>/dev/null)
    
    local corruption_count=0
    local verified_count=0
    
    while IFS=$'\t' read -r uuid path expected_hash; do
        if [ -z "$uuid" ] || [ -z "$path" ] || [ -z "$expected_hash" ]; then
            continue
        fi
        
        local file_path="$STORAGE_PATH/$path"
        
        if [ ! -f "$file_path" ]; then
            log "WARNING" "Fichier non trouvé pour vérification: $uuid"
            continue
        fi
        
        # Calculer le hash actuel
        local actual_hash=$(sha256sum "$file_path" 2>/dev/null | awk '{print $1}')
        
        if [ "$actual_hash" != "$expected_hash" ]; then
            log "ERROR" "CORRUPTION DÉTECTÉE: $uuid (attendu: $expected_hash, calculé: $actual_hash)"
            corruption_count=$((corruption_count + 1))
            
            # Enregistrer la corruption
            mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -e "
                UPDATE documents_medicaux SET statut = 'quarantaine' WHERE uuid = '$uuid';
                INSERT INTO verification_integrite 
                    (document_uuid, statut_verification, hash_attendu, hash_calcule, type_verification, timestamp)
                VALUES ('$uuid', 'hash_invalide', '$expected_hash', '$actual_hash', 'automatique', NOW());
            " 2>/dev/null || true
        else
            log "INFO" "Hash OK: $uuid"
            verified_count=$((verified_count + 1))
        fi
    done <<< "$samples"
    
    log "INFO" "Vérification terminée: $verified_count OK, $corruption_count corruptions"
    
    if [ "$corruption_count" -gt 0 ]; then
        send_alert "Corruption de fichiers détectée" "$corruption_count fichier(s) corrompu(s) détecté(s) et mis en quarantaine"
        return 1
    fi
    
    return 0
}

# ==============================================================================
# 4. Détection d'accès suspects
# ==============================================================================
check_suspicious_access() {
    log "INFO" "=== Détection d'accès suspects ==="
    
    local yesterday=$(date -d 'yesterday' '+%Y-%m-%d')
    
    # Compter les tentatives non autorisées
    local unauthorized=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -N -e "
        SELECT COUNT(*) FROM audit_acces_documents 
        WHERE autorise = 0 AND DATE(timestamp) = '$yesterday';
    " 2>/dev/null || echo "0")
    
    # Compter les accès par IP suspecte (plus de 100 accès en 24h)
    local suspicious_ips=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -N -e "
        SELECT ip_address, COUNT(*) as cnt 
        FROM audit_acces_documents 
        WHERE DATE(timestamp) = '$yesterday'
        GROUP BY ip_address 
        HAVING cnt > 100;
    " 2>/dev/null)
    
    log "INFO" "Tentatives non autorisées hier: $unauthorized"
    
    if [ "$unauthorized" -gt 10 ]; then
        send_alert "Accès non autorisés suspects" "$unauthorized tentatives d'accès non autorisées détectées hier"
    fi
    
    if [ -n "$suspicious_ips" ]; then
        log "WARNING" "IPs suspectes détectées:"
        echo "$suspicious_ips" | while read -r ip count; do
            log "WARNING" "  $ip: $count accès"
        done
        send_alert "Activité IP suspecte" "IPs avec plus de 100 accès en 24h détectées"
    fi
    
    return 0
}

# ==============================================================================
# 5. Purge de la quarantaine (fichiers > 30 jours)
# ==============================================================================
purge_quarantine() {
    log "INFO" "=== Purge de la quarantaine ==="
    
    local quarantine_path="$STORAGE_PATH/quarantine"
    
    if [ ! -d "$quarantine_path" ]; then
        log "INFO" "Répertoire quarantaine inexistant, création..."
        mkdir -p "$quarantine_path"
        return 0
    fi
    
    # Trouver les fichiers de plus de 30 jours
    local old_files=$(find "$quarantine_path" -type f -mtime +30 2>/dev/null)
    local purge_count=0
    
    while read -r file; do
        if [ -n "$file" ]; then
            local filename=$(basename "$file")
            log "INFO" "Purge: $filename (> 30 jours en quarantaine)"
            
            # Archiver avant suppression (optionnel)
            # cp "$file" "$STORAGE_PATH/archive/purged_$(date +%Y%m%d)_$filename"
            
            # Supprimer le fichier
            rm -f "$file"
            purge_count=$((purge_count + 1))
        fi
    done <<< "$old_files"
    
    if [ "$purge_count" -gt 0 ]; then
        log "INFO" "$purge_count fichier(s) purgé(s) de la quarantaine"
        
        # Logger la purge en base
        mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -e "
            INSERT INTO audit_log (action, details, created_at)
            VALUES ('purge_quarantine', 'Purge de $purge_count fichier(s) de plus de 30 jours', NOW());
        " 2>/dev/null || true
    fi
    
    return 0
}

# ==============================================================================
# 6. Rapport de synthèse
# ==============================================================================
generate_report() {
    log "INFO" "=== Génération du rapport de synthèse ==="
    
    local report_date=$(date '+%Y-%m-%d %H:%M:%S')
    
    # Statistiques générales
    local total_docs=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -N -e "
        SELECT COUNT(*) FROM documents_medicaux WHERE statut = 'actif';
    " 2>/dev/null || echo "0")
    
    local total_size=$(du -sh "$STORAGE_PATH" 2>/dev/null | awk '{print $1}')
    
    local quarantine_count=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -N -e "
        SELECT COUNT(*) FROM documents_medicaux WHERE statut = 'quarantaine';
    " 2>/dev/null || echo "0")
    
    local docs_without_hash=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -N -e "
        SELECT COUNT(*) FROM documents_medicaux WHERE statut = 'actif' AND hash_sha256 IS NULL;
    " 2>/dev/null || echo "0")
    
    log "INFO" "=========================================="
    log "INFO" "RAPPORT DE SANTÉ DU STOCKAGE"
    log "INFO" "Date: $report_date"
    log "INFO" "=========================================="
    log "INFO" "Documents actifs: $total_docs"
    log "INFO" "Taille totale: $total_size"
    log "INFO" "Documents en quarantaine: $quarantine_count"
    log "INFO" "Documents sans hash: $docs_without_hash"
    log "INFO" "=========================================="
    
    # Insérer le rapport en base
    mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -e "
        INSERT INTO audit_log (action, details, created_at)
        VALUES ('storage_health_report', 
                JSON_OBJECT(
                    'total_docs', $total_docs,
                    'total_size', '$total_size',
                    'quarantine_count', $quarantine_count,
                    'docs_without_hash', $docs_without_hash
                ), 
                NOW());
    " 2>/dev/null || true
}

# ==============================================================================
# MAIN
# ==============================================================================
main() {
    log "INFO" "=========================================="
    log "INFO" "Démarrage du check de santé du stockage"
    log "INFO" "=========================================="
    
    local errors=0
    
    check_disk_space || errors=$((errors + 1))
    check_files_vs_db || errors=$((errors + 1))
    check_hash_integrity || errors=$((errors + 1))
    check_suspicious_access || errors=$((errors + 1))
    purge_quarantine || errors=$((errors + 1))
    generate_report
    
    log "INFO" "=========================================="
    if [ "$errors" -gt 0 ]; then
        log "WARNING" "Check terminé avec $errors erreur(s)/alerte(s)"
        exit 1
    else
        log "INFO" "Check terminé avec succès"
        exit 0
    fi
}

# Exécuter le script
main "$@"
