-- Migration: Add validity fields to creneau_disponible table
-- Date: 2024-12-22
-- Description: Adds date_debut_validite, date_fin_validite, and est_semaine_type fields
--              to support multi-week planning for doctors

-- Add new columns to creneau_disponible table
ALTER TABLE `creneau_disponible` 
ADD COLUMN IF NOT EXISTS `date_debut_validite` DATETIME NULL,
ADD COLUMN IF NOT EXISTS `date_fin_validite` DATETIME NULL,
ADD COLUMN IF NOT EXISTS `est_semaine_type` TINYINT(1) NOT NULL DEFAULT 1;

-- Create index for efficient querying by date range
CREATE INDEX `IX_creneau_disponible_validite` 
ON `creneau_disponible` (`id_medecin`, `date_debut_validite`, `date_fin_validite`);

-- Create index for filtering by est_semaine_type
CREATE INDEX `IX_creneau_disponible_semaine_type` 
ON `creneau_disponible` (`id_medecin`, `est_semaine_type`);

-- Update existing records to mark them as "semaine type" (recurring)
UPDATE `creneau_disponible` 
SET `est_semaine_type` = 1 
WHERE `date_debut_validite` IS NULL;
