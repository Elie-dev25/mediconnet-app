-- Migration: Ajouter la colonne date_consentement à la table consultation_anesthesique
-- Date: 2026-04-02

ALTER TABLE `consultation_anesthesique` 
ADD COLUMN `date_consentement` DATETIME NULL AFTER `consentement_obtenu`;
