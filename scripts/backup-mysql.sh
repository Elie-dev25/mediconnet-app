#!/bin/bash
# Backup script for Mediconnet MySQL database
# Usage: ./backup-mysql.sh [backup_name]
# If no name provided, uses timestamp

set -e

# Configuration
CONTAINER_NAME="mediconnet-mysql"
DB_NAME="${DB_NAME:-mediconnect}"
DB_USER="${DB_USER:-app}"
DB_PASS="${DB_PASSWORD:-app}"
BACKUP_DIR="${BACKUP_DIR:-./backups}"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_NAME="${1:-mediconnet_backup_${TIMESTAMP}}.sql"

# Create backup directory if it doesn't exist
mkdir -p "$BACKUP_DIR"

echo "🔄 Starting MySQL backup..."
echo "📦 Container: $CONTAINER_NAME"
echo "🗄️  Database: $DB_NAME"
echo "💾 Output: $BACKUP_DIR/$BACKUP_NAME"

# Execute backup
docker exec "$CONTAINER_NAME" mysqldump \
  --user="$DB_USER" \
  --password="$DB_PASS" \
  --single-transaction \
  --routines \
  --triggers \
  --databases "$DB_NAME" > "$BACKUP_DIR/$BACKUP_NAME"

# Compress backup
echo "🗜️  Compressing backup..."
gzip "$BACKUP_DIR/$BACKUP_NAME"
BACKUP_FILE="${BACKUP_DIR}/${BACKUP_NAME}.gz"

# Show backup info
BACKUP_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
echo "✅ Backup completed successfully!"
echo "📊 Size: $BACKUP_SIZE"
echo "📍 Location: $BACKUP_FILE"

# Clean old backups (keep last 7 days)
echo "🧹 Cleaning old backups..."
find "$BACKUP_DIR" -name "mediconnet_backup_*.sql.gz" -mtime +7 -delete
echo "✅ Old backups cleaned"

echo "🎉 Backup process finished!"
