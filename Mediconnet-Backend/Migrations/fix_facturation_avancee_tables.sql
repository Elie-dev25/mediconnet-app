-- Migration: Aligner les tables echeancier, echeance, demande_remboursement
-- avec les entités EF Core (FactureAvanceeService)
-- Date: 2026-02-16

-- ============================================================
-- Table: echeancier - Ajouter colonnes manquantes
-- ============================================================
ALTER TABLE `echeancier`
  ADD COLUMN IF NOT EXISTS `montant_par_echeance` DECIMAL(12,2) NOT NULL DEFAULT 0 AFTER `nombre_echeances`,
  ADD COLUMN IF NOT EXISTS `date_debut` DATE NOT NULL DEFAULT (CURRENT_DATE) AFTER `montant_par_echeance`,
  ADD COLUMN IF NOT EXISTS `frequence` VARCHAR(20) NOT NULL DEFAULT 'mensuel' AFTER `date_debut`,
  ADD COLUMN IF NOT EXISTS `cree_par` INT NULL AFTER `statut`;

-- ============================================================
-- Table: echeance - Ajouter colonnes manquantes
-- ============================================================
ALTER TABLE `echeance`
  ADD COLUMN IF NOT EXISTS `id_transaction` INT NULL AFTER `statut`,
  ADD COLUMN IF NOT EXISTS `notes` VARCHAR(500) NULL AFTER `id_transaction`;

ALTER TABLE `echeance`
  ADD CONSTRAINT `fk_echeance_transaction` FOREIGN KEY (`id_transaction`) 
    REFERENCES `transaction_paiement` (`id_transaction`) ON DELETE SET NULL;

-- ============================================================
-- Table: demande_remboursement - Ajouter colonnes manquantes
-- ============================================================
ALTER TABLE `demande_remboursement`
  ADD COLUMN IF NOT EXISTS `numero_demande` VARCHAR(50) NOT NULL DEFAULT '' AFTER `id_demande`,
  ADD COLUMN IF NOT EXISTS `id_patient` INT NOT NULL DEFAULT 0 AFTER `id_assurance`,
  ADD COLUMN IF NOT EXISTS `montant_approuve` DECIMAL(12,2) NULL AFTER `montant_demande`,
  ADD COLUMN IF NOT EXISTS `date_traitement` DATETIME NULL AFTER `date_reponse`,
  ADD COLUMN IF NOT EXISTS `motif_rejet` VARCHAR(500) NULL AFTER `statut`,
  ADD COLUMN IF NOT EXISTS `justificatif` VARCHAR(500) NULL AFTER `motif_rejet`,
  ADD COLUMN IF NOT EXISTS `traite_par` INT NULL AFTER `justificatif`;

ALTER TABLE `demande_remboursement`
  ADD CONSTRAINT `fk_dr_patient` FOREIGN KEY (`id_patient`) 
    REFERENCES `patient` (`id_user`) ON DELETE CASCADE;
