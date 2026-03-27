-- ============================================================
-- Migration: Support des ventes directes sans ordonnance
-- Date: 2026-03-23
-- Description: Ajoute les colonnes nécessaires pour permettre
--              les ventes directes de médicaments sans ordonnance
-- ============================================================

-- 1. Rendre id_patient nullable (pour clients non enregistrés)
ALTER TABLE `dispensation` 
MODIFY COLUMN `id_patient` INT DEFAULT NULL COMMENT 'NULL pour clients non enregistrés';

-- 2. Ajouter la colonne type_vente
ALTER TABLE `dispensation` 
ADD COLUMN `type_vente` ENUM('avec_ordonnance', 'vente_directe') NOT NULL DEFAULT 'avec_ordonnance' 
COMMENT 'Type de vente' AFTER `notes`;

-- 3. Ajouter les colonnes pour les informations client (vente directe)
ALTER TABLE `dispensation` 
ADD COLUMN `nom_client` VARCHAR(100) DEFAULT NULL COMMENT 'Nom du client pour vente directe' AFTER `type_vente`,
ADD COLUMN `telephone_client` VARCHAR(20) DEFAULT NULL COMMENT 'Téléphone du client pour vente directe' AFTER `nom_client`;

-- 4. Ajouter les colonnes pour la facturation
ALTER TABLE `dispensation` 
ADD COLUMN `montant_total` DECIMAL(12,2) DEFAULT NULL COMMENT 'Montant total de la vente' AFTER `telephone_client`,
ADD COLUMN `mode_paiement` VARCHAR(50) DEFAULT NULL COMMENT 'Mode de paiement (especes, carte, mobile_money)' AFTER `montant_total`,
ADD COLUMN `numero_ticket` VARCHAR(50) DEFAULT NULL COMMENT 'Numéro de ticket de caisse' AFTER `mode_paiement`;

-- 5. Ajouter les index pour optimiser les requêtes
ALTER TABLE `dispensation` 
ADD INDEX `idx_disp_type_vente` (`type_vente`),
ADD INDEX `idx_disp_date` (`date_dispensation`),
ADD INDEX `idx_disp_numero_ticket` (`numero_ticket`);

-- 6. Mettre à jour les dispensations existantes (toutes sont avec ordonnance)
UPDATE `dispensation` SET `type_vente` = 'avec_ordonnance' WHERE `type_vente` IS NULL OR `type_vente` = '';

-- 7. Modifier la colonne reference_type de mouvement_stock pour inclure 'vente_directe'
ALTER TABLE `mouvement_stock` 
MODIFY COLUMN `reference_type` ENUM('commande','prescription','inventaire','ajustement','dispensation','vente_directe') DEFAULT NULL;

-- ============================================================
-- Vérification post-migration
-- ============================================================
-- SELECT 
--   COUNT(*) as total,
--   SUM(CASE WHEN type_vente = 'avec_ordonnance' THEN 1 ELSE 0 END) as avec_ordonnance,
--   SUM(CASE WHEN type_vente = 'vente_directe' THEN 1 ELSE 0 END) as vente_directe
-- FROM dispensation;
