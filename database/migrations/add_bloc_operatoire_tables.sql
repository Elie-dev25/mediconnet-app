-- Migration: Ajout des tables bloc_operatoire et reservation_bloc
-- Date: 2026-03-25

-- Table bloc_operatoire
CREATE TABLE IF NOT EXISTS `bloc_operatoire` (
  `id_bloc` INT NOT NULL AUTO_INCREMENT,
  `nom` VARCHAR(100) NOT NULL COMMENT 'Nom du bloc (ex: Bloc A, Bloc B)',
  `description` VARCHAR(500) DEFAULT NULL,
  `statut` VARCHAR(20) DEFAULT 'libre' COMMENT 'libre, occupe, maintenance',
  `actif` TINYINT(1) DEFAULT 1,
  `localisation` VARCHAR(100) DEFAULT NULL COMMENT 'Emplacement dans l''hôpital',
  `capacite` INT DEFAULT NULL COMMENT 'Capacité du bloc',
  `equipements` VARCHAR(500) DEFAULT NULL COMMENT 'Liste des équipements disponibles',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_bloc`),
  KEY `IX_bloc_operatoire_statut` (`statut`),
  KEY `IX_bloc_operatoire_actif` (`actif`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Table reservation_bloc
CREATE TABLE IF NOT EXISTS `reservation_bloc` (
  `id_reservation` INT NOT NULL AUTO_INCREMENT,
  `id_bloc` INT NOT NULL,
  `id_programmation` INT NOT NULL,
  `id_medecin` INT NOT NULL,
  `date_reservation` DATE NOT NULL,
  `heure_debut` VARCHAR(5) NOT NULL COMMENT 'Format HH:mm',
  `heure_fin` VARCHAR(5) NOT NULL COMMENT 'Format HH:mm',
  `duree_minutes` INT NOT NULL,
  `statut` VARCHAR(20) DEFAULT 'confirmee' COMMENT 'confirmee, en_cours, terminee, annulee',
  `notes` VARCHAR(500) DEFAULT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_reservation`),
  KEY `IX_reservation_bloc_bloc` (`id_bloc`),
  KEY `IX_reservation_bloc_programmation` (`id_programmation`),
  KEY `IX_reservation_bloc_medecin` (`id_medecin`),
  KEY `IX_reservation_bloc_date` (`date_reservation`),
  UNIQUE KEY `IX_reservation_bloc_unique` (`id_bloc`, `date_reservation`, `heure_debut`),
  CONSTRAINT `fk_reservation_bloc` FOREIGN KEY (`id_bloc`) REFERENCES `bloc_operatoire` (`id_bloc`) ON DELETE CASCADE,
  CONSTRAINT `fk_reservation_programmation` FOREIGN KEY (`id_programmation`) REFERENCES `programmation_intervention` (`id_programmation`) ON DELETE CASCADE,
  CONSTRAINT `fk_reservation_medecin` FOREIGN KEY (`id_medecin`) REFERENCES `medecin` (`id_user`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Données de test: quelques blocs opératoires
INSERT INTO `bloc_operatoire` (`nom`, `description`, `localisation`, `capacite`, `equipements`) VALUES
('Bloc A', 'Bloc opératoire principal - Chirurgie générale', 'Bâtiment Principal, 2ème étage', 1, 'Table opératoire, Scialytique, Monitoring complet, Respirateur'),
('Bloc B', 'Bloc opératoire secondaire - Chirurgie orthopédique', 'Bâtiment Principal, 2ème étage', 1, 'Table opératoire orthopédique, Amplificateur de brillance, Arthroscope'),
('Bloc C', 'Bloc opératoire - Chirurgie cardiaque', 'Bâtiment Cardiologie, 1er étage', 1, 'CEC, Table opératoire, Monitoring avancé, Échographie transœsophagienne'),
('Bloc D', 'Bloc opératoire ambulatoire', 'Bâtiment Ambulatoire, RDC', 1, 'Table opératoire, Monitoring standard');
