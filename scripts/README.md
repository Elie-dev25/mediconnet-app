# Scripts de sauvegarde et restauration

Ce dossier contient les scripts pour gérer les sauvegardes de la base de données Mediconnet.

## Scripts disponibles

### backup-mysql.sh
Crée une sauvegarde de la base de données MySQL avec compression automatique.

```bash
# Usage
./scripts/backup-mysql.sh [nom_du_fichier]

# Exemples
./scripts/backup-mysql.sh                                    # Nom automatique avec timestamp
./scripts/backup-mysql.sh backup_avant_mise_a_jour.sql     # Nom personnalisé
```

**Fonctionnalités :**
- Sauvegarde complète avec routines et triggers
- Compression automatique en gzip
- Nettoyage des sauvegardes de plus de 7 jours
- Taille de la sauvegarde affichée

### restore-mysql.sh
Restaure la base de données à partir d'un fichier de sauvegarde.

```bash
# Usage
./scripts/restore-mysql.sh backup_file.sql.gz

# Exemple
./scripts/restore-mysql.sh ./backups/mediconnet_backup_20240309_143022.sql.gz
```

**Fonctionnalités :**
- Support des fichiers .sql et .sql.gz
- Confirmation avant restauration
- Arrêt/redémarrage automatique des conteneurs
- Nettoyage des fichiers temporaires

## Variables d'environnement

Les scripts utilisent les variables suivantes (définies dans `.env`) :
- `DB_NAME` : Nom de la base de données (défaut: mediconnect)
- `DB_USER` : Utilisateur MySQL (défaut: app)
- `DB_PASSWORD` : Mot de passe MySQL (défaut: app)
- `BACKUP_DIR` : Répertoire des sauvegardes (défaut: ./backups)

## Configuration des permissions

```bash
# Rendre les scripts exécutables
chmod +x scripts/backup-mysql.sh
chmod +x scripts/restore-mysql.sh
```

## Automatisation (cron)

Pour automatiser les sauvegardes quotidiennes :

```bash
# Ouvrir crontab
crontab -e

# Ajouter une sauvegarde quotidienne à 2h du matin
0 2 * * * /chemin/vers/mediconnet-app/scripts/backup-mysql.sh

# Sauvegarde hebdomadaire le dimanche à 3h du matin
0 3 * * 0 /chemin/vers/mediconnet-app/scripts/backup-mysql.sh weekly_backup.sql
```

## Bonnes pratiques

1. **Testez régulièrement vos sauvegardes** en effectuant des restaurations sur un environnement de test
2. **Stockez les sauvegardes** sur un emplacement différent du serveur (cloud, NAS, etc.)
3. **Vérifiez l'espace disque** régulièrement pour éviter les problèmes de stockage
4. **Documentez les procédures** de restauration pour votre équipe

## Dépannage

### Erreur "Container not found"
- Vérifiez que les conteneurs sont bien démarrés : `docker-compose ps`
- Redémarrez si nécessaire : `docker-compose up -d`

### Erreur "Permission denied"
- Assurez-vous que les scripts sont exécutables : `chmod +x scripts/*.sh`
- Vérifiez les permissions sur le répertoire de sauvegarde

### Erreur "Access denied for user"
- Vérifiez les variables d'environnement dans `.env`
- Confirmez que l'utilisateur MySQL a les droits nécessaires
