-- Migration: Mise à jour des statuts de dispensation pour le workflow vente pharmacie
-- Date: 2026-03-23
-- Description: Nouveau workflow: Pharmacie → Facturation → Caisse (paiement) → Pharmacie → Délivrance

-- 1. Convertir les anciens statuts
UPDATE `dispensation` SET `statut` = 'terminee' WHERE `statut` = 'complete';

-- 2. Modifier la colonne statut pour utiliser les nouveaux statuts
ALTER TABLE `dispensation` 
MODIFY COLUMN `statut` ENUM('en_attente', 'paye', 'delivre', 'annule', 'en_cours', 'terminee') 
DEFAULT 'en_attente' 
COMMENT 'Statut: en_attente (facturé), paye (payé à la caisse), delivre (médicaments remis)';

-- 3. Ajouter un index sur le statut pour les requêtes de filtrage
ALTER TABLE `dispensation` ADD INDEX `idx_disp_statut` (`statut`);

-- Workflow des statuts pour ventes directes:
-- en_attente : Vente créée/facturée, en attente de paiement à la caisse
-- paye       : Paiement effectué à la caisse, en attente de délivrance
-- delivre    : Médicaments remis au patient, stock décrémenté
-- annule     : Vente annulée

-- Pour les ventes avec ordonnance (workflow existant):
-- en_cours   : Dispensation en cours
-- terminee   : Dispensation terminée
