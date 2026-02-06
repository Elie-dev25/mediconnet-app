-- Migration: Ajouter le rattachement des infirmiers aux services
-- Date: 2026-01-26
-- Description: Ajoute la colonne id_service à la table infirmier pour le rattachement obligatoire à un service
-- Base de données: MySQL 8.0

-- 1. Vérifier si la colonne existe déjà, sinon l'ajouter
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'infirmier' AND COLUMN_NAME = 'id_service');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE infirmier ADD COLUMN id_service INT NULL',
    'SELECT "Column id_service already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 2. Mettre à jour les infirmiers existants: 
--    - Si l'infirmier est Major d'un service, le rattacher à ce service
UPDATE infirmier 
SET id_service = id_service_major 
WHERE id_service_major IS NOT NULL AND id_service IS NULL;

-- 3. Pour les infirmiers sans service Major, les rattacher au premier service (service par défaut)
UPDATE infirmier i
SET i.id_service = (SELECT s.id_service FROM service s ORDER BY s.id_service LIMIT 1)
WHERE i.id_service IS NULL;

-- 4. Rendre la colonne NOT NULL après la migration des données
ALTER TABLE infirmier MODIFY COLUMN id_service INT NOT NULL;

-- 5. Ajouter la contrainte de clé étrangère (si elle n'existe pas déjà)
SET @fk_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'infirmier' AND CONSTRAINT_NAME = 'fk_infirmier_service');

SET @sql = IF(@fk_exists = 0, 
    'ALTER TABLE infirmier ADD CONSTRAINT fk_infirmier_service FOREIGN KEY (id_service) REFERENCES service(id_service) ON DELETE RESTRICT',
    'SELECT "FK fk_infirmier_service already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 6. Créer un index pour optimiser les requêtes par service (si il n'existe pas déjà)
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'infirmier' AND INDEX_NAME = 'idx_infirmier_service');

SET @sql = IF(@idx_exists = 0, 
    'CREATE INDEX idx_infirmier_service ON infirmier(id_service)',
    'SELECT "Index idx_infirmier_service already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Vérification: Afficher les infirmiers avec leur service
SELECT i.id_user, u.nom, u.prenom, i.id_service, s.nom_service, i.is_major, i.id_service_major
FROM infirmier i
JOIN utilisateurs u ON i.id_user = u.id_user
LEFT JOIN service s ON i.id_service = s.id_service;
