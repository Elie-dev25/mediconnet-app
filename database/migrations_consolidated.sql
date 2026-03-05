-- =====================================================
-- MIGRATIONS CONSOLIDÉES - MediConnect
-- Toutes les modifications apportées à la base de données
-- =====================================================

-- =====================================================
-- 1. TABLES ORDONNANCES (Simplification)
-- =====================================================

-- Supprimer l'ancienne table ordonnance basique
DROP TABLE IF EXISTS `ordonnance`;

-- Supprimer l'ancienne table prescription (sera remplacée)
DROP TABLE IF EXISTS `prescription`;

-- Créer la nouvelle table ordonnance avec la structure complète
CREATE TABLE `ordonnance` (
    `id_ordonnance` INT NOT NULL AUTO_INCREMENT,
    `date` DATE NOT NULL,
    `id_patient` INT DEFAULT NULL COMMENT 'Lien direct au patient',
    `id_medecin` INT DEFAULT NULL COMMENT 'Lien direct au medecin',
    `id_consultation` INT DEFAULT NULL COMMENT 'ID consultation de rattachement',
    `id_hospitalisation` INT DEFAULT NULL COMMENT 'ID hospitalisation si contexte hospitalier',
    `type_contexte` VARCHAR(50) DEFAULT NULL COMMENT 'consultation, hospitalisation, directe',
    `statut` VARCHAR(50) DEFAULT 'active' COMMENT 'active, dispensee, partielle, annulee, expiree',
    `commentaire` TEXT DEFAULT NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    `date_expiration` DATETIME DEFAULT NULL COMMENT 'Date d expiration de l ordonnance',
    `renouvelable` TINYINT(1) DEFAULT 0 COMMENT 'Ordonnance renouvelable',
    `nombre_renouvellements` INT DEFAULT NULL COMMENT 'Nombre de renouvellements autorises',
    `renouvellements_restants` INT DEFAULT NULL COMMENT 'Nombre de renouvellements restants',
    `id_ordonnance_originale` INT DEFAULT NULL COMMENT 'ID ordonnance originale si renouvellement',
    PRIMARY KEY (`id_ordonnance`),
    KEY `id_consultation` (`id_consultation`),
    KEY `idx_ordonnance_patient` (`id_patient`),
    KEY `idx_ordonnance_medecin` (`id_medecin`),
    KEY `idx_ordonnance_hospitalisation` (`id_hospitalisation`),
    KEY `idx_ordonnance_type_contexte` (`type_contexte`),
    KEY `idx_ordonnance_statut` (`statut`),
    KEY `idx_ordonnance_date` (`date` DESC),
    KEY `idx_ordonnance_expiration` (`date_expiration`),
    KEY `idx_ordonnance_renouvelable` (`renouvelable`),
    KEY `idx_ordonnance_originale` (`id_ordonnance_originale`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Renommer prescription_medicament en ordonnance_medicament
DROP TABLE IF EXISTS `ordonnance_medicament`;
RENAME TABLE `prescription_medicament` TO `ordonnance_medicament`;

-- Mettre à jour la colonne id_ord vers id_ordonnance
ALTER TABLE `ordonnance_medicament` 
CHANGE COLUMN `id_ord` `id_ordonnance` INT NOT NULL;

-- =====================================================
-- 2. TABLE PHARMACIEN (Mise à jour structure)
-- =====================================================

-- Supprimer et recréer la table pharmacien avec la structure complète
DROP TABLE IF EXISTS `pharmacien`;

CREATE TABLE `pharmacien` (
    `id_user` INT NOT NULL,
    `numero_ordre` VARCHAR(50) DEFAULT NULL,
    `matricule` VARCHAR(50) DEFAULT NULL,
    `date_embauche` DATETIME DEFAULT NULL,
    `actif` BOOLEAN DEFAULT TRUE,
    `created_at` DATETIME DEFAULT NULL,
    `updated_at` DATETIME DEFAULT NULL,
    PRIMARY KEY (`id_user`),
    CONSTRAINT `pharmacien_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `utilisateurs` (`id_user`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- =====================================================
-- 3. TABLES HOSPITALISATION (Ajout de colonnes)
-- =====================================================

ALTER TABLE `hospitalisation` 
ADD COLUMN IF NOT EXISTS `date_sortie_prevue` DATETIME NULL,
ADD COLUMN IF NOT EXISTS `motif_sortie` TEXT NULL,
ADD COLUMN IF NOT EXISTS `resume_medical` TEXT NULL,
ADD COLUMN IF NOT EXISTS `date_lit_attribue` DATETIME NULL,
ADD COLUMN IF NOT EXISTS `id_lit_attribue_par` INT NULL,
ADD COLUMN IF NOT EXISTS `role_lit_attribue_par` VARCHAR(20) NULL;

-- =====================================================
-- 4. TABLES ASSURANCE (Déjà présentes dans init.sql)
-- =====================================================

-- La table assurance_couverture existe déjà dans init.sql
-- Pas de modification nécessaire

-- =====================================================
-- 5. CONTRAINTES DE CLÉS ÉTRANGÈRES
-- =====================================================

-- Contraintes pour ordonnance
ALTER TABLE `ordonnance`
  ADD CONSTRAINT `ordonnance_ibfk_1` FOREIGN KEY (`id_consultation`) REFERENCES `consultation` (`id_consultation`) ON DELETE SET NULL,
  ADD CONSTRAINT `ordonnance_ibfk_2` FOREIGN KEY (`id_patient`) REFERENCES `utilisateurs` (`id_user`) ON DELETE SET NULL,
  ADD CONSTRAINT `ordonnance_ibfk_3` FOREIGN KEY (`id_medecin`) REFERENCES `utilisateurs` (`id_user`) ON DELETE SET NULL,
  ADD CONSTRAINT `ordonnance_ibfk_4` FOREIGN KEY (`id_hospitalisation`) REFERENCES `hospitalisation` (`id_admission`) ON DELETE SET NULL,
  ADD CONSTRAINT `ordonnance_ibfk_5` FOREIGN KEY (`id_ordonnance_originale`) REFERENCES `ordonnance` (`id_ordonnance`) ON DELETE SET NULL;

-- Contraintes pour ordonnance_medicament
ALTER TABLE `ordonnance_medicament`
  ADD CONSTRAINT `ordonnance_medicament_ibfk_1` FOREIGN KEY (`id_ordonnance`) REFERENCES `ordonnance` (`id_ordonnance`) ON DELETE CASCADE,
  ADD CONSTRAINT `ordonnance_medicament_ibfk_2` FOREIGN KEY (`id_medicament`) REFERENCES `medicament` (`id_medicament`) ON DELETE SET NULL;

-- =====================================================
-- 6. DISPENSATION (Mise à jour colonne id_ordonnance)
-- =====================================================

ALTER TABLE `dispensation`
DROP FOREIGN KEY IF EXISTS `dispensation_ibfk_1`;

ALTER TABLE `dispensation`
CHANGE COLUMN `id_ordonnance` `id_prescription` INT DEFAULT NULL;

ALTER TABLE `dispensation`
ADD CONSTRAINT `dispensation_ibfk_1` FOREIGN KEY (`id_prescription`) REFERENCES `ordonnance` (`id_ordonnance`) ON DELETE SET NULL;

-- =====================================================
-- 7. NETTOYAGE
-- =====================================================

-- Supprimer les anciennes tables si elles existent encore
DROP TABLE IF EXISTS `prescription`;

-- =====================================================
-- FIN DES MIGRATIONS
-- =====================================================
