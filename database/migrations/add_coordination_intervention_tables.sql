-- Migration: Coordination Chirurgien-Anesthésiste pour les interventions
-- Date: 2026-03-30
-- Description: Ajoute les tables pour gérer la coordination entre chirurgien et anesthésiste

-- 1. Modifier la table programmation_intervention pour ajouter la colonne id_anesthesiste
ALTER TABLE `programmation_intervention` 
ADD COLUMN `id_anesthesiste` INT NULL AFTER `statut`,
ADD COLUMN `heure_debut` VARCHAR(5) NULL AFTER `date_prevue`,
MODIFY COLUMN `statut` VARCHAR(30) NOT NULL DEFAULT 'en_attente_coordination';

-- Ajouter l'index et la clé étrangère pour l'anesthésiste
ALTER TABLE `programmation_intervention`
ADD INDEX `IX_programmation_intervention_anesthesiste` (`id_anesthesiste`),
ADD CONSTRAINT `fk_programmation_anesthesiste` 
    FOREIGN KEY (`id_anesthesiste`) REFERENCES `medecin`(`id_user`) ON DELETE SET NULL;

-- 2. Créer la table coordination_intervention
CREATE TABLE IF NOT EXISTS `coordination_intervention` (
    `id_coordination` INT AUTO_INCREMENT PRIMARY KEY,
    `id_programmation` INT NOT NULL,
    `id_chirurgien` INT NOT NULL,
    `id_anesthesiste` INT NOT NULL,
    `date_proposee` DATETIME NOT NULL,
    `heure_proposee` VARCHAR(5) NOT NULL,
    `duree_estimee` INT NOT NULL,
    `statut` VARCHAR(20) NOT NULL DEFAULT 'proposee',
    `date_contre_proposee` DATETIME NULL,
    `heure_contre_proposee` VARCHAR(5) NULL,
    `commentaire_anesthesiste` TEXT NULL,
    `motif_refus` TEXT NULL,
    `notes_chirurgien` TEXT NULL,
    `id_rdv_consultation_anesthesiste` INT NULL,
    `id_indisponibilite_chirurgien` INT NULL,
    `id_indisponibilite_anesthesiste` INT NULL,
    `id_reservation_bloc` INT NULL,
    `date_validation` DATETIME NULL,
    `date_reponse` DATETIME NULL,
    `nb_modifications` INT NOT NULL DEFAULT 0,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NULL,
    
    INDEX `IX_coordination_programmation` (`id_programmation`),
    INDEX `IX_coordination_chirurgien` (`id_chirurgien`),
    INDEX `IX_coordination_anesthesiste` (`id_anesthesiste`),
    INDEX `IX_coordination_statut` (`statut`),
    INDEX `IX_coordination_date` (`date_proposee`),
    
    CONSTRAINT `fk_coordination_programmation` 
        FOREIGN KEY (`id_programmation`) REFERENCES `programmation_intervention`(`id_programmation`) ON DELETE CASCADE,
    CONSTRAINT `fk_coordination_chirurgien` 
        FOREIGN KEY (`id_chirurgien`) REFERENCES `medecin`(`id_user`) ON DELETE RESTRICT,
    CONSTRAINT `fk_coordination_anesthesiste` 
        FOREIGN KEY (`id_anesthesiste`) REFERENCES `medecin`(`id_user`) ON DELETE RESTRICT,
    CONSTRAINT `fk_coordination_rdv_consultation` 
        FOREIGN KEY (`id_rdv_consultation_anesthesiste`) REFERENCES `rendez_vous`(`id_rendez_vous`) ON DELETE SET NULL,
    CONSTRAINT `fk_coordination_indispo_chirurgien` 
        FOREIGN KEY (`id_indisponibilite_chirurgien`) REFERENCES `indisponibilite_medecin`(`id_indisponibilite`) ON DELETE SET NULL,
    CONSTRAINT `fk_coordination_indispo_anesthesiste` 
        FOREIGN KEY (`id_indisponibilite_anesthesiste`) REFERENCES `indisponibilite_medecin`(`id_indisponibilite`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Créer la table coordination_intervention_historique
CREATE TABLE IF NOT EXISTS `coordination_intervention_historique` (
    `id_historique` INT AUTO_INCREMENT PRIMARY KEY,
    `id_coordination` INT NOT NULL,
    `type_action` VARCHAR(30) NOT NULL,
    `id_user_action` INT NOT NULL,
    `role_user` VARCHAR(20) NOT NULL,
    `details` TEXT NULL,
    `date_proposee` DATETIME NULL,
    `heure_proposee` VARCHAR(5) NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    INDEX `IX_coordination_historique_coordination` (`id_coordination`),
    
    CONSTRAINT `fk_historique_coordination` 
        FOREIGN KEY (`id_coordination`) REFERENCES `coordination_intervention`(`id_coordination`) ON DELETE CASCADE,
    CONSTRAINT `fk_historique_user` 
        FOREIGN KEY (`id_user_action`) REFERENCES `utilisateurs`(`id_user`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. Mettre à jour les programmations existantes avec le nouveau statut
UPDATE `programmation_intervention` 
SET `statut` = 'en_attente_coordination' 
WHERE `statut` = 'en_attente';
