-- Migration: Ajout des champs pour la complétion de profil
-- Date: 2024-12-05
-- Description: Ajoute les champs nécessaires pour le formulaire de complétion de profil multi-étapes
-- Note: Exécuter chaque instruction séparément en cas d'erreur "Duplicate column"

-- ============================================
-- 1. Modifications table utilisateurs
-- ============================================

ALTER TABLE utilisateurs ADD COLUMN profile_completed BOOLEAN DEFAULT FALSE;
ALTER TABLE utilisateurs ADD COLUMN profile_completed_at DATETIME NULL;
ALTER TABLE utilisateurs ADD COLUMN nationalite VARCHAR(100) DEFAULT 'Cameroun';
ALTER TABLE utilisateurs ADD COLUMN region_origine VARCHAR(100) NULL;

-- ============================================
-- 2. Modifications table patient
-- ============================================

ALTER TABLE patient ADD COLUMN maladies_chroniques TEXT NULL;
ALTER TABLE patient ADD COLUMN operations_chirurgicales BOOLEAN NULL;
ALTER TABLE patient ADD COLUMN operations_details TEXT NULL;
ALTER TABLE patient ADD COLUMN allergies_connues BOOLEAN NULL;
ALTER TABLE patient ADD COLUMN allergies_details TEXT NULL;
ALTER TABLE patient ADD COLUMN antecedents_familiaux BOOLEAN NULL;
ALTER TABLE patient ADD COLUMN antecedents_familiaux_details TEXT NULL;
ALTER TABLE patient ADD COLUMN consommation_alcool BOOLEAN NULL;
ALTER TABLE patient ADD COLUMN frequence_alcool VARCHAR(50) NULL;
ALTER TABLE patient ADD COLUMN tabagisme BOOLEAN NULL;
ALTER TABLE patient ADD COLUMN activite_physique BOOLEAN NULL;

-- Déclaration sur l'honneur
ALTER TABLE patient ADD COLUMN declaration_honneur_acceptee BOOLEAN DEFAULT FALSE;
ALTER TABLE patient ADD COLUMN declaration_honneur_at DATETIME NULL;

-- ============================================
-- 3. Mise à jour des utilisateurs existants
-- ============================================

UPDATE utilisateurs 
SET profile_completed = TRUE, 
    profile_completed_at = NOW(),
    nationalite = COALESCE(nationalite, 'Cameroun')
WHERE role IN ('administrateur', 'medecin', 'infirmier', 'caissier');
