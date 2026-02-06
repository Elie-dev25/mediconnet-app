# Migration Sécurisée des Fichiers Médicaux (UUID + Intégrité)

## 📋 Résumé

Cette migration transforme le système de gestion des fichiers médicaux de MediConnect d'une architecture basée sur chemins bruts vers une architecture robuste, sécurisée et traçable basée sur UUID.

## 🎯 Objectifs

- ✅ Contrôle d'intégrité (hash SHA-256)
- ✅ Audit des accès
- ✅ Monitoring
- ✅ Compatibilité backend existant
- ✅ Zéro perte de données

## 📁 Fichiers de Migration

| Fichier | Description |
|---------|-------------|
| `init.sql` | Schéma complet mis à jour (pour nouvelles installations) |
| `migration_apply_documents_medicaux.sql` | Script de migration pour base existante |
| `migration_documents_medicaux.sql` | Script détaillé avec logs et transactions |
| `migration_rollback_documents_medicaux.sql` | Script de rollback en cas de problème |

## 🗂️ Nouvelles Tables

### `documents_medicaux`
Table centrale pour tous les documents médicaux avec :
- UUID comme clé primaire
- Métadonnées complètes (mime, taille, hash SHA-256)
- Classification (type_document, sous_type)
- Niveaux de confidentialité
- Versioning
- Liens vers patient, consultation, bulletin, hospitalisation, DMP

### `audit_acces_documents`
Journal d'audit complet :
- Traçabilité de tous les accès
- Type d'action (consultation, téléchargement, modification, etc.)
- Contexte technique (IP, user-agent, session)
- Autorisations accordées/refusées

### `verification_integrite`
Historique des contrôles d'intégrité :
- Statut de vérification (ok, hash_invalide, fichier_absent, etc.)
- Hash attendu vs calculé
- Actions correctives

## 📊 Vues de Monitoring

| Vue | Description |
|-----|-------------|
| `v_dashboard_documents` | Tableau de bord des documents avec statistiques d'accès |
| `v_documents_problemes` | Documents avec problèmes d'intégrité pour alertes |
| `v_statistiques_documents` | Statistiques globales par type de document |

## 🔄 Colonnes Ajoutées aux Tables Existantes

| Table | Colonne | Description |
|-------|---------|-------------|
| `bulletin_examen` | `document_resultat_uuid` | Lien vers documents_medicaux |
| `document_dmp` | `document_uuid` | Lien vers documents_medicaux |

## 🚀 Instructions d'Exécution

### Prérequis

1. **BACKUP** complet de la base de données
2. **BACKUP** physique des fichiers stockés
3. Environnement de test validé

### Via Adminer (Docker)

1. Accéder à Adminer : `http://localhost:8081`
2. Se connecter à la base `mediconnect`
3. Aller dans "Commande SQL"
4. Copier/coller le contenu de `migration_apply_documents_medicaux.sql`
5. Exécuter section par section
6. Vérifier les COUNT(*) après chaque étape

### Via MySQL CLI

```bash
# Depuis le container Docker
docker exec -i mediconnet-db mysql -u root -proot mediconnect < migration_apply_documents_medicaux.sql
```

## ✅ Vérification Post-Migration

```sql
-- Vérifier les nouvelles tables
SHOW TABLES LIKE '%document%';
SHOW TABLES LIKE '%audit%';
SHOW TABLES LIKE '%verification%';

-- Vérifier les colonnes ajoutées
DESCRIBE bulletin_examen;
DESCRIBE document_dmp;

-- Vérifier les vues
SELECT * FROM v_statistiques_documents;
```

## ⚠️ Rollback

En cas de problème, exécuter :
```sql
SOURCE migration_rollback_documents_medicaux.sql;
```

## 🔐 Accès Backend via UUID

Après migration, le backend doit accéder aux documents **uniquement via UUID** :

```csharp
// Exemple d'accès
var document = await _context.DocumentsMedicaux
    .FirstOrDefaultAsync(d => d.Uuid == documentUuid);

// Vérification d'accès
var hasAccess = await _documentService.VerifyAccess(documentUuid, userId, userRole);
```

## 📈 Prochaines Étapes (Backend)

1. Créer l'entité `DocumentMedical` dans le backend
2. Implémenter le service de calcul de hash SHA-256
3. Créer les endpoints API pour upload/download via UUID
4. Implémenter le job de vérification d'intégrité quotidien
5. Migrer les fichiers physiques vers la nouvelle arborescence

## 📂 Structure Physique Recommandée

```
/storage/
├── documents/
│   ├── 2026/
│   │   ├── 01/
│   │   │   ├── patient_123/
│   │   │   │   ├── uuid1.pdf
│   │   │   │   └── uuid2.jpg
│   │   │   └── patient_456/
│   │   └── 02/
│   └── 2025/
├── cache/
├── backup/
├── quarantine/
└── temp/
```

Permissions : `750` (rwxr-x---), propriétaire : `www-data`

## 📝 Changelog

- **2026-02-06** : Migration initiale
  - Création des tables documents_medicaux, audit_acces_documents, verification_integrite
  - Ajout des colonnes UUID aux tables existantes
  - Création des vues de monitoring
  - Mise à jour des permissions (biologiste → laborantin)
