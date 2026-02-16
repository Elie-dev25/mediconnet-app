-- Migration: Ajouter colonnes pour facturation assurance
-- Date: 2026-02-16
-- Description: Ajoute les colonnes nécessaires pour le suivi des factures assurance

-- Ajouter la colonne date_envoi_assurance
ALTER TABLE factures 
ADD COLUMN IF NOT EXISTS date_envoi_assurance DATETIME NULL COMMENT 'Date d envoi de la facture a l assurance';

-- Ajouter la colonne montant_patient si elle n'existe pas
ALTER TABLE factures 
ADD COLUMN IF NOT EXISTS montant_patient DECIMAL(12,2) DEFAULT 0 COMMENT 'Montant a payer par le patient';

-- Mettre à jour montant_patient pour les factures existantes
UPDATE factures 
SET montant_patient = montant_total - COALESCE(montant_assurance, 0)
WHERE montant_patient = 0 OR montant_patient IS NULL;

-- Ajouter un index pour les recherches par statut
CREATE INDEX IF NOT EXISTS IX_facture_statut ON factures(statut);

-- Ajouter un index pour les recherches par assurance
CREATE INDEX IF NOT EXISTS IX_facture_assurance ON factures(id_assurance);
