-- Migration: Ajout de la table consultation_chirurgicale
-- Date: 2026-03-24
-- Description: Table d'extension 1-1 pour les examens chirurgicaux

CREATE TABLE IF NOT EXISTS `consultation_chirurgicale` (
  `id_consultation` INT NOT NULL,
  `zone_examinee` VARCHAR(255) DEFAULT NULL COMMENT 'Zone anatomique examinée',
  `inspection_locale` TEXT DEFAULT NULL COMMENT 'Inspection locale de la zone',
  `palpation_locale` TEXT DEFAULT NULL COMMENT 'Palpation locale',
  `signes_inflammatoires` TEXT DEFAULT NULL COMMENT 'Signes inflammatoires locaux',
  `cicatrices_existantes` TEXT DEFAULT NULL COMMENT 'État des cicatrices existantes',
  `mobilite_fonction` TEXT DEFAULT NULL COMMENT 'Mobilité et fonction de la zone',
  `conclusion_chirurgicale` TEXT DEFAULT NULL COMMENT 'Conclusion de l examen chirurgical',
  `decision` VARCHAR(50) DEFAULT NULL COMMENT 'surveillance, traitement_medical, indication_operatoire',
  `notes_complementaires` TEXT DEFAULT NULL COMMENT 'Notes complémentaires',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT NULL,
  PRIMARY KEY (`id_consultation`),
  CONSTRAINT `fk_consultation_chirurgicale_consultation`
    FOREIGN KEY (`id_consultation`) REFERENCES `consultation` (`id_consultation`)
    ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE UNIQUE INDEX IF NOT EXISTS `IX_consultation_chirurgicale_consultation`
  ON `consultation_chirurgicale` (`id_consultation`);
