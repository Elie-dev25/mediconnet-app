-- Migration: Ajout des tables de caisse (transactions et sessions)
-- Date: 2024-12-11

-- Table des sessions de caisse
CREATE TABLE IF NOT EXISTS `session_caisse` (
    `id_session` INT AUTO_INCREMENT PRIMARY KEY,
    `id_caissier` INT NOT NULL,
    `montant_ouverture` DECIMAL(12,2) NOT NULL DEFAULT 0,
    `montant_fermeture` DECIMAL(12,2) NULL,
    `montant_systeme` DECIMAL(12,2) NULL,
    `ecart` DECIMAL(12,2) NULL,
    `date_ouverture` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `date_fermeture` DATETIME NULL,
    `statut` VARCHAR(20) NOT NULL DEFAULT 'ouverte',
    `notes_ouverture` VARCHAR(500) NULL,
    `notes_fermeture` VARCHAR(500) NULL,
    `notes_rapprochement` VARCHAR(500) NULL,
    `valide_par` INT NULL,
    
    INDEX `IX_session_caisse_caissier` (`id_caissier`),
    INDEX `IX_session_caisse_statut` (`statut`),
    INDEX `IX_session_caisse_date` (`date_ouverture`),
    
    CONSTRAINT `FK_session_caisse_caissier` FOREIGN KEY (`id_caissier`) 
        REFERENCES `caissier` (`id_user`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table des transactions de paiement
CREATE TABLE IF NOT EXISTS `transaction_paiement` (
    `id_transaction` INT AUTO_INCREMENT PRIMARY KEY,
    `numero_transaction` VARCHAR(50) NOT NULL,
    `transaction_uuid` VARCHAR(36) NOT NULL,
    `id_facture` INT NOT NULL,
    `id_patient` INT NULL,
    `id_caissier` INT NOT NULL,
    `id_session_caisse` INT NULL,
    `montant` DECIMAL(12,2) NOT NULL,
    `mode_paiement` VARCHAR(30) NOT NULL DEFAULT 'especes',
    `statut` VARCHAR(30) NOT NULL DEFAULT 'complete',
    `reference` VARCHAR(100) NULL,
    `notes` VARCHAR(500) NULL,
    `date_transaction` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `date_annulation` DATETIME NULL,
    `motif_annulation` VARCHAR(500) NULL,
    `annule_par` INT NULL,
    `est_paiement_partiel` TINYINT(1) NOT NULL DEFAULT 0,
    `montant_recu` DECIMAL(12,2) NULL,
    `rendu_monnaie` DECIMAL(12,2) NULL,
    
    UNIQUE INDEX `IX_transaction_uuid` (`transaction_uuid`),
    INDEX `IX_transaction_numero` (`numero_transaction`),
    INDEX `IX_transaction_facture` (`id_facture`),
    INDEX `IX_transaction_patient` (`id_patient`),
    INDEX `IX_transaction_caissier` (`id_caissier`),
    INDEX `IX_transaction_session` (`id_session_caisse`),
    INDEX `IX_transaction_date` (`date_transaction`),
    INDEX `IX_transaction_statut` (`statut`),
    INDEX `IX_transaction_mode` (`mode_paiement`),
    
    CONSTRAINT `FK_transaction_facture` FOREIGN KEY (`id_facture`) 
        REFERENCES `facture` (`id_facture`) ON DELETE CASCADE,
    CONSTRAINT `FK_transaction_patient` FOREIGN KEY (`id_patient`) 
        REFERENCES `patient` (`id_user`) ON DELETE SET NULL,
    CONSTRAINT `FK_transaction_caissier` FOREIGN KEY (`id_caissier`) 
        REFERENCES `caissier` (`id_user`) ON DELETE CASCADE,
    CONSTRAINT `FK_transaction_session` FOREIGN KEY (`id_session_caisse`) 
        REFERENCES `session_caisse` (`id_session`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
