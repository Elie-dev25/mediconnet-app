#!/bin/bash
# Restore script for Mediconnet MySQL database
# Usage: ./restore-mysql.sh backup_file.sql.gz

set -e

# Configuration
CONTAINER_NAME="mediconnet-mysql"
DB_NAME="${DB_NAME:-mediconnect}"
DB_USER="${DB_USER:-app}"
DB_PASS="${DB_PASSWORD:-app}"

# Check if backup file provided
if [ -z "$1" ]; then
  echo "❌ Error: Please provide backup file"
  echo "Usage: $0 backup_file.sql.gz"
  exit 1
fi

BACKUP_FILE="$1"

# Check if backup file exists
if [ ! -f "$BACKUP_FILE" ]; then
  echo "❌ Error: Backup file not found: $BACKUP_FILE"
  exit 1
fi

echo "🔄 Starting MySQL restore..."
echo "📦 Container: $CONTAINER_NAME"
echo "🗄️  Database: $DB_NAME"
echo "📂 Backup: $BACKUP_FILE"

# Confirm restore operation
read -p "⚠️  This will replace the current database. Are you sure? (yes/no): " confirm
if [ "$confirm" != "yes" ]; then
  echo "❌ Restore cancelled"
  exit 1
fi

# Extract backup if compressed
if [[ $BACKUP_FILE == *.gz ]]; then
  echo "🗜️  Extracting backup..."
  gunzip -c "$BACKUP_FILE" > /tmp/restore_temp.sql
  RESTORE_FILE="/tmp/restore_temp.sql"
else
  RESTORE_FILE="$BACKUP_FILE"
fi

# Stop application to prevent conflicts
echo "⏸️  Stopping application containers..."
docker-compose stop mediconnet-backend mediconnet-frontend nginx

# Restore database
echo "🔄 Restoring database..."
docker exec -i "$CONTAINER_NAME" mysql \
  --user="$DB_USER" \
  --password="$DB_PASS" \
  "$DB_NAME" < "$RESTORE_FILE"

# Clean up temp file
if [ -f "/tmp/restore_temp.sql" ]; then
  rm /tmp/restore_temp.sql
fi

# Restart application
echo "▶️  Restarting application containers..."
docker-compose start mediconnet-backend mediconnet-frontend nginx

echo "✅ Database restored successfully!"
echo "🎉 Restore process finished!"
