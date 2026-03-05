-- Migration pour ajouter la colonne date_lit_attribue à la table hospitalisation
-- Date: 2026-03-04

-- Vérifier si la colonne existe déjà
SELECT COUNT(*) as column_exists 
FROM information_schema.columns 
WHERE table_name = 'hospitalisation' 
AND column_name = 'date_lit_attribue' 
AND table_schema = DATABASE();

-- Ajouter la colonne si elle n'existe pas
ALTER TABLE hospitalisation 
ADD COLUMN IF NOT EXISTS date_lit_attribue DATETIME NULL 
COMMENT 'Date/heure d''attribution effective du lit';

-- Afficher la structure mise à jour
DESCRIBE hospitalisation;
