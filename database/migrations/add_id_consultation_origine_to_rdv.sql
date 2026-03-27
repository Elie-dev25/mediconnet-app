-- Migration: Ajout de la colonne id_consultation_origine à la table rendez_vous
-- Date: 2026-03-24
-- Description: Permet de lier un RDV de suivi à sa consultation d'origine

-- Ajouter la colonne id_consultation_origine
ALTER TABLE rendez_vous 
ADD COLUMN IF NOT EXISTS id_consultation_origine INT NULL 
COMMENT 'ID de la consultation d''origine (pour les RDV de suivi)';

-- Ajouter l'index pour les recherches
CREATE INDEX IF NOT EXISTS idx_rdv_consultation_origine 
ON rendez_vous(id_consultation_origine);
