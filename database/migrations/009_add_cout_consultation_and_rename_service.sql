-- Migration: Ajout du coût de consultation par spécialité et renommage du service
-- Date: 2024-12-23
-- Base: MySQL

-- 1. Ajouter le champ cout_consultation à la table specialites (si n'existe pas)
SET @columnExists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'specialites' AND COLUMN_NAME = 'cout_consultation');
SET @sql = IF(@columnExists = 0, 'ALTER TABLE specialites ADD COLUMN cout_consultation DECIMAL(10, 2) DEFAULT 5000', 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 2. Initialiser les coûts de consultation pour les spécialités existantes (1000 à 10000 FCFA)
UPDATE specialites SET cout_consultation = 3000 WHERE nom_specialite LIKE '%généraliste%' OR nom_specialite LIKE '%general%';
UPDATE specialites SET cout_consultation = 5000 WHERE nom_specialite LIKE '%pédiatr%';
UPDATE specialites SET cout_consultation = 6000 WHERE nom_specialite LIKE '%gynéco%' OR nom_specialite LIKE '%gyneco%';
UPDATE specialites SET cout_consultation = 7000 WHERE nom_specialite LIKE '%cardio%';
UPDATE specialites SET cout_consultation = 7500 WHERE nom_specialite LIKE '%dermato%';
UPDATE specialites SET cout_consultation = 8000 WHERE nom_specialite LIKE '%ophtalmo%';
UPDATE specialites SET cout_consultation = 8500 WHERE nom_specialite LIKE '%neurolo%';
UPDATE specialites SET cout_consultation = 9000 WHERE nom_specialite LIKE '%chirurg%';
UPDATE specialites SET cout_consultation = 10000 WHERE nom_specialite LIKE '%radiolo%' OR nom_specialite LIKE '%imagerie%';

-- Mettre à jour les spécialités restantes avec un coût par défaut de 5000
UPDATE specialites SET cout_consultation = 5000 WHERE cout_consultation IS NULL OR cout_consultation = 0;

-- 3. Renommer le service "Consultation" en "Médecine générale"
UPDATE service SET nom_service = 'Médecine générale' WHERE nom_service = 'Consultation';

-- Vérification
SELECT id_specialite, nom_specialite, cout_consultation FROM specialites ORDER BY cout_consultation;
SELECT id_service, nom_service FROM service WHERE nom_service = 'Médecine générale';
