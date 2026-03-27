-- Migration: Ajout de la table programmation_intervention
-- Date: 2026-03-24
-- Description: Table pour la programmation des interventions chirurgicales

CREATE TABLE IF NOT EXISTS `programmation_intervention` (
    `id_programmation` INT NOT NULL AUTO_INCREMENT,
    `id_consultation` INT NOT NULL,
    `id_patient` INT NOT NULL,
    `id_medecin` INT NOT NULL,
    `type_intervention` VARCHAR(50) DEFAULT 'programmee' COMMENT 'programmee, urgence, ambulatoire',
    `classification_asa` VARCHAR(10) DEFAULT NULL COMMENT 'ASA 1 à 5',
    `risque_operatoire` VARCHAR(20) DEFAULT NULL COMMENT 'faible, modere, eleve',
    `consentement_eclaire` TINYINT(1) DEFAULT 0,
    `date_consentement` DATETIME DEFAULT NULL,
    `indication_operatoire` TEXT DEFAULT NULL,
    `technique_prevue` TEXT DEFAULT NULL,
    `date_prevue` DATETIME DEFAULT NULL,
    `duree_estimee` INT DEFAULT NULL COMMENT 'Durée en minutes',
    `notes_anesthesie` TEXT DEFAULT NULL,
    `bilan_preoperatoire` TEXT DEFAULT NULL,
    `instructions_patient` TEXT DEFAULT NULL,
    `statut` VARCHAR(20) DEFAULT 'en_attente' COMMENT 'en_attente, validee, planifiee, realisee, annulee',
    `motif_annulation` TEXT DEFAULT NULL,
    `notes` TEXT DEFAULT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id_programmation`),
    KEY `IX_programmation_intervention_consultation` (`id_consultation`),
    KEY `IX_programmation_intervention_patient` (`id_patient`),
    KEY `IX_programmation_intervention_medecin` (`id_medecin`),
    KEY `IX_programmation_intervention_statut` (`statut`),
    CONSTRAINT `fk_programmation_consultation` FOREIGN KEY (`id_consultation`) REFERENCES `consultation` (`id_consultation`) ON DELETE CASCADE,
    CONSTRAINT `fk_programmation_patient` FOREIGN KEY (`id_patient`) REFERENCES `patient` (`id_user`) ON DELETE CASCADE,
    CONSTRAINT `fk_programmation_medecin` FOREIGN KEY (`id_medecin`) REFERENCES `medecin` (`id_user`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
