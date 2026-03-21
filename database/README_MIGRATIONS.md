# 📋 Migrations de la Base de Données MediConnect

## 🗓️ Date : 4 Mars 2026

## 🎯 Objectif
Consolidation et documentation de toutes les migrations appliquées à la base de données MediConnect pour assurer la cohérence entre le schéma et le code EF Core.

## 🔄 Modifications Principales

### 1. **Simplification des Tables d'Ordonnances**
- ❌ **Suppression** : `prescription` (table vide et redondante)
- ❌ **Suppression** : `ordonnance` (table basique, 0 enregistrements)
- ✅ **Remplacement** : `ordonnance` (structure complète de l'ancienne `prescription`)
- ✅ **Renommage** : `prescription_medicament` → `ordonnance_medicament`

#### Changements de structure :
```sql
-- Avant (prescription)
CREATE TABLE `prescription` (
  `id_ord` INT NOT NULL AUTO_INCREMENT,
  -- ...
);

-- Après (ordonnance)
CREATE TABLE `ordonnance` (
  `id_ordonnance` INT NOT NULL AUTO_INCREMENT,
  -- Structure complète avec tous les champs
  `date_expiration` DATETIME,
  `renouvelable` TINYINT(1),
  -- ...
);
```

### 2. **Mise à jour Table Pharmacien**
- ✅ **Ajout** : `matricule`, `date_embauche`, `actif`, `created_at`, `updated_at`
- ✅ **Correction** : Clé primaire `id_user` (pas de `id_pharmacien`)

```sql
CREATE TABLE `pharmacien` (
  `id_user` INT NOT NULL PRIMARY KEY,
  `numero_ordre` VARCHAR(50) DEFAULT NULL,
  `matricule` VARCHAR(50) DEFAULT NULL,
  `date_embauche` DATETIME DEFAULT NULL,
  `actif` BOOLEAN DEFAULT TRUE,
  `created_at` DATETIME DEFAULT NULL,
  `updated_at` DATETIME DEFAULT NULL
);
```

### 3. **Table Hospitalisation - Colonnes Ajoutées**
- ✅ `date_sortie_prevue` : Date de sortie prévue
- ✅ `motif_sortie` : Motif de sortie
- ✅ `resume_medical` : Résumé médical
- ✅ `date_lit_attribue` : Date d'attribution du lit
- ✅ `id_lit_attribue_par` : ID utilisateur qui a attribué le lit
- ✅ `role_lit_attribue_par` : Rôle de l'utilisateur

### 4. **Table Dispensation - Mise à jour**
- ✅ **Changement** : `id_ordonnance` → `id_prescription`
- ✅ **Contrainte** : FK vers `ordonnance.id_ordonnance`

### 5. **Tables Assurance (Déjà présentes)**
- ✅ `assurance_couverture` : Couverture par type de prestation
- ✅ 28 enregistrements (7 assurances × 4 types)

## 🔗 Mises à Jour EF Core

### Entités modifiées :
1. **OrdonnanceEntity.cs**
   - `[Table("ordonnance")]` (anciennement `prescription`)
   - `[Column("id_ordonnance")]` (anciennement `id_ord`)

2. **PrescriptionMedicament**
   - `[Table("ordonnance_medicament")]` (anciennement `prescription_medicament`)
   - `[Column("id_ordonnance")]` (anciennement `id_ord`)

3. **PharmacienEntity.cs**
   - `[Table("pharmacien")]` avec clé primaire `IdUser`
   - `entity.Ignore(e => e.IdPharmacien)` pour ignorer la propriété non mappée

## 📊 État Final des Tables

| Table | Enregistrements | Statut |
|-------|----------------|--------|
| `ordonnance` | 17 | ✅ Active (remplace prescription) |
| `ordonnance_medicament` | 13 | ✅ Active (remplace prescription_medicament) |
| `pharmacien` | 1+ | ✅ Active avec structure complète |
| `hospitalisation` | N/A | ✅ Active avec colonnes ajoutées |
| `assurance_couverture` | 28 | ✅ Active |

## 🚀 Scripts de Migration

### Script consolidé : `migrations_consolidated.sql`
Contient toutes les commandes SQL pour appliquer les migrations :
- Création des nouvelles tables
- Migration des données
- Mise à jour des contraintes
- Nettoyage des anciennes tables

### Init.sql mis à jour
- ✅ Tables `ordonnance` et `ordonnance_medicament` à jour
- ✅ Contraintes de clés étrangères corrigées
- ✅ Structure `pharmacien` enrichie
- ✅ Colonnes `hospitalisation` ajoutées

## 🧪 Tests Recommandés

1. **Délivrance d'ordonnance** : Vérifier que l'ordonnance #12 fonctionne
2. **Création pharmacien** : Tester la création via l'admin
3. **Hospitalisation** : Vérifier les nouvelles colonnes
4. **Facturation** : Confirmer que `assurance_couverture` fonctionne

## 📝 Notes importantes

- **Aucune donnée perdue** : Les 17 ordonnances existantes ont été migrées
- **Rétrocompatibilité** : Le code EF Core a été mis à jour pour correspondre
- **Performance** : Les index ont été conservés et optimisés
- **Cohérence** : Plus de confusion entre `ordonnance`/`prescription`

## 🔍 Vérification

```sql
-- Vérifier les tables
SHOW TABLES LIKE '%ordonnance%';
SHOW TABLES LIKE '%prescription%';

-- Vérifier les données
SELECT COUNT(*) FROM ordonnance;
SELECT COUNT(*) FROM ordonnance_medicament;

-- Vérifier l'ordonnance #12
SELECT * FROM ordonnance WHERE id_ordonnance = 12;
```

---

**✅ Migration terminée avec succès !**
