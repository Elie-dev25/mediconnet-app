-- Migration: Ajout des tables Question / ConsultationQuestion / Reponse (questionnaire consultation)
-- Date: 2025-12-17

-- ==================== TABLE QUESTION ====================
CREATE TABLE IF NOT EXISTS `question` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `texte_question` TEXT NOT NULL,
    `type_question` VARCHAR(50) NOT NULL DEFAULT 'texte',
    `est_predefinie` TINYINT(1) NOT NULL DEFAULT 0,
    `created_by` INT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    INDEX `IX_question_predefinie` (`est_predefinie`),
    INDEX `IX_question_created_at` (`created_at`),

    CONSTRAINT `FK_question_created_by` FOREIGN KEY (`created_by`)
        REFERENCES `utilisateurs` (`id_user`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==================== SEED QUESTIONS PRÉDÉFINIES (IDEMPOTENT) ====================
INSERT INTO `question` (`texte_question`, `type_question`, `est_predefinie`, `created_by`)
SELECT 'Motif principal de consultation', 'texte', 1, NULL
WHERE NOT EXISTS (SELECT 1 FROM `question` WHERE `texte_question` = 'Motif principal de consultation');

INSERT INTO `question` (`texte_question`, `type_question`, `est_predefinie`, `created_by`)
SELECT 'Symptômes actuels (début, durée, intensité)', 'texte', 1, NULL
WHERE NOT EXISTS (SELECT 1 FROM `question` WHERE `texte_question` = 'Symptômes actuels (début, durée, intensité)');

INSERT INTO `question` (`texte_question`, `type_question`, `est_predefinie`, `created_by`)
SELECT 'Antécédents médicaux pertinents', 'texte', 1, NULL
WHERE NOT EXISTS (SELECT 1 FROM `question` WHERE `texte_question` = 'Antécédents médicaux pertinents');

INSERT INTO `question` (`texte_question`, `type_question`, `est_predefinie`, `created_by`)
SELECT 'Traitements en cours', 'texte', 1, NULL
WHERE NOT EXISTS (SELECT 1 FROM `question` WHERE `texte_question` = 'Traitements en cours');

INSERT INTO `question` (`texte_question`, `type_question`, `est_predefinie`, `created_by`)
SELECT 'Allergies connues', 'texte', 1, NULL
WHERE NOT EXISTS (SELECT 1 FROM `question` WHERE `texte_question` = 'Allergies connues');

-- ==================== TABLE CONSULTATION_QUESTION ====================
CREATE TABLE IF NOT EXISTS `consultation_question` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `consultation_id` INT NOT NULL,
    `question_id` INT NOT NULL,
    `ordre_affichage` INT NOT NULL,

    UNIQUE INDEX `UX_consultation_question` (`consultation_id`, `question_id`),
    INDEX `IX_consultation_question_ordre` (`consultation_id`, `ordre_affichage`),
    INDEX `IX_consultation_question_question` (`question_id`),

    CONSTRAINT `FK_consultation_question_consultation` FOREIGN KEY (`consultation_id`)
        REFERENCES `consultation` (`id_consultation`) ON DELETE CASCADE,
    CONSTRAINT `FK_consultation_question_question` FOREIGN KEY (`question_id`)
        REFERENCES `question` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==================== TABLE REPONSE ====================
CREATE TABLE IF NOT EXISTS `reponse` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `consultation_id` INT NOT NULL,
    `question_id` INT NOT NULL,
    `valeur_reponse` TEXT NULL,
    `rempli_par` VARCHAR(20) NOT NULL,
    `date_saisie` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    UNIQUE INDEX `UX_reponse_consultation_question` (`consultation_id`, `question_id`),
    INDEX `IX_reponse_date_saisie` (`date_saisie`),

    CONSTRAINT `FK_reponse_consultation` FOREIGN KEY (`consultation_id`)
        REFERENCES `consultation` (`id_consultation`) ON DELETE CASCADE,
    CONSTRAINT `FK_reponse_question` FOREIGN KEY (`question_id`)
        REFERENCES `question` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SELECT 'Migration 008_add_consultation_questions_reponses.sql appliquée avec succès' AS status;
