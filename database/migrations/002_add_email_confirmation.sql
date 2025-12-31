-- Migration: Add Email Confirmation System
-- Date: 2024-12-05
-- Description: Ajoute le système de confirmation d'email

-- Ajouter les colonnes à la table utilisateurs
ALTER TABLE `utilisateurs` 
ADD COLUMN `email_confirmed` BOOLEAN NOT NULL DEFAULT FALSE AFTER `photo`,
ADD COLUMN `email_confirmed_at` TIMESTAMP NULL DEFAULT NULL AFTER `email_confirmed`;

-- Créer la table pour les tokens de confirmation
CREATE TABLE IF NOT EXISTS `email_confirmation_tokens` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `id_user` INT NOT NULL,
  `token` VARCHAR(100) NOT NULL,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `expires_at` TIMESTAMP NOT NULL,
  `is_used` BOOLEAN NOT NULL DEFAULT FALSE,
  `used_at` TIMESTAMP NULL DEFAULT NULL,
  `confirmed_from_ip` VARCHAR(50) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `IX_email_token` (`token`),
  KEY `IX_email_token_user` (`id_user`),
  CONSTRAINT `FK_email_token_user` FOREIGN KEY (`id_user`) 
    REFERENCES `utilisateurs` (`id_user`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Mettre à jour les utilisateurs existants comme ayant confirmé leur email
-- (pour ne pas bloquer les comptes existants)
UPDATE `utilisateurs` SET `email_confirmed` = TRUE, `email_confirmed_at` = NOW() 
WHERE `email_confirmed` = FALSE;
