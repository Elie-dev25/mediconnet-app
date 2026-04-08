-- Migration: Ajout de la table affectation_service pour l'historique des affectations
-- Date: 2026-03-30

-- 1. Créer la table affectation_service
CREATE TABLE IF NOT EXISTS `affectation_service` (
    `id_affectation` INT AUTO_INCREMENT PRIMARY KEY,
    `id_user` INT NOT NULL COMMENT 'ID de l''utilisateur (médecin ou infirmier)',
    `type_user` VARCHAR(20) NOT NULL COMMENT 'Type: medecin ou infirmier',
    `id_service` INT NOT NULL COMMENT 'ID du service affecté',
    `date_debut` DATETIME NOT NULL COMMENT 'Date de début de l''affectation',
    `date_fin` DATETIME NULL COMMENT 'Date de fin (NULL si affectation en cours)',
    `motif_changement` VARCHAR(500) NULL COMMENT 'Motif du changement de service',
    `id_admin_changement` INT NULL COMMENT 'ID de l''admin ayant effectué le changement',
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX `idx_affectation_user` (`id_user`, `type_user`),
    INDEX `idx_affectation_service` (`id_service`),
    INDEX `idx_affectation_date_fin` (`date_fin`),
    CONSTRAINT `fk_affectation_user` FOREIGN KEY (`id_user`) 
        REFERENCES `utilisateurs`(`id_user`) ON DELETE CASCADE,
    CONSTRAINT `fk_affectation_service` FOREIGN KEY (`id_service`) 
        REFERENCES `service`(`id_service`) ON DELETE RESTRICT,
    CONSTRAINT `fk_affectation_admin` FOREIGN KEY (`id_admin_changement`) 
        REFERENCES `utilisateurs`(`id_user`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. Migrer les données existantes des médecins
INSERT INTO `affectation_service` (`id_user`, `type_user`, `id_service`, `date_debut`, `motif_changement`)
SELECT 
    m.id_user,
    'medecin',
    m.id_service,
    COALESCE(u.created_at, NOW()),
    'Migration initiale - affectation existante'
FROM `medecin` m
JOIN `utilisateurs` u ON m.id_user = u.id_user
WHERE m.id_service IS NOT NULL
ON DUPLICATE KEY UPDATE id_affectation = id_affectation;

-- 3. Migrer les données existantes des infirmiers
INSERT INTO `affectation_service` (`id_user`, `type_user`, `id_service`, `date_debut`, `motif_changement`)
SELECT 
    i.id_user,
    'infirmier',
    i.id_service,
    COALESCE(u.created_at, NOW()),
    'Migration initiale - affectation existante'
FROM `infirmier` i
JOIN `utilisateurs` u ON i.id_user = u.id_user
WHERE i.id_service IS NOT NULL
ON DUPLICATE KEY UPDATE id_affectation = id_affectation;
