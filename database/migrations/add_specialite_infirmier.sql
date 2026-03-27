-- Migration: Ajout de la table specialite_infirmier et liaison avec infirmier
-- Date: 2026-03-26

-- 1. Créer la table specialite_infirmier
CREATE TABLE IF NOT EXISTS `specialite_infirmier` (
    `id_specialite` INT AUTO_INCREMENT PRIMARY KEY,
    `code` VARCHAR(20) NULL,
    `nom` VARCHAR(100) NOT NULL,
    `description` VARCHAR(500) NULL,
    `actif` BOOLEAN DEFAULT TRUE,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX `idx_specialite_infirmier_code` (`code`),
    INDEX `idx_specialite_infirmier_actif` (`actif`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. Insérer les spécialités initiales (ignorer si déjà existantes)
INSERT IGNORE INTO `specialite_infirmier` (`code`, `nom`, `description`) VALUES
('IDE', 'Infirmier Diplômé d''État', 'Infirmier généraliste avec diplôme d''État'),
('IADE', 'Infirmier Anesthésiste', 'Infirmier spécialisé en anesthésie et réanimation'),
('IBODE', 'Infirmier de Bloc Opératoire', 'Infirmier spécialisé dans les interventions chirurgicales'),
('IPDE', 'Puéricultrice', 'Infirmier spécialisé dans les soins aux enfants'),
('ISP', 'Infirmier de Santé Publique', 'Infirmier spécialisé en santé publique et prévention');

-- 3. Ajouter la colonne id_specialite à la table infirmier (si elle n'existe pas)
SET @column_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'infirmier' AND COLUMN_NAME = 'id_specialite');

SET @sql = IF(@column_exists = 0, 
    'ALTER TABLE `infirmier` ADD COLUMN `id_specialite` INT NULL', 
    'SELECT "Column already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 4. Ajouter la contrainte de clé étrangère (si elle n'existe pas)
SET @fk_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'infirmier' AND CONSTRAINT_NAME = 'fk_infirmier_specialite');

SET @sql = IF(@fk_exists = 0, 
    'ALTER TABLE `infirmier` ADD CONSTRAINT `fk_infirmier_specialite` FOREIGN KEY (`id_specialite`) REFERENCES `specialite_infirmier`(`id_specialite`) ON DELETE SET NULL ON UPDATE CASCADE', 
    'SELECT "FK already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 5. Mettre à jour les infirmiers existants avec la spécialité par défaut (IDE)
UPDATE `infirmier` SET `id_specialite` = 1 WHERE `id_specialite` IS NULL;
