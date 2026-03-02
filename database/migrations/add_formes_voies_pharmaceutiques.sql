-- Migration: Ajout des tables formes_pharmaceutiques et voies_administration
-- Date: 2026-02-24
-- Description: Gestion dynamique des formes pharmaceutiques et voies d'administration par médicament

-- --------------------------------------------------------
-- Table: forme_pharmaceutique
-- --------------------------------------------------------

CREATE TABLE IF NOT EXISTS `forme_pharmaceutique` (
  `id_forme` INT NOT NULL AUTO_INCREMENT,
  `code` VARCHAR(50) NOT NULL,
  `libelle` VARCHAR(100) NOT NULL,
  `description` VARCHAR(255) DEFAULT NULL,
  `icone` VARCHAR(50) DEFAULT NULL,
  `ordre` INT NOT NULL DEFAULT 0,
  `actif` TINYINT(1) NOT NULL DEFAULT 1,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_forme`),
  UNIQUE KEY `uq_forme_code` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: voie_administration
-- --------------------------------------------------------

CREATE TABLE IF NOT EXISTS `voie_administration` (
  `id_voie` INT NOT NULL AUTO_INCREMENT,
  `code` VARCHAR(50) NOT NULL,
  `libelle` VARCHAR(100) NOT NULL,
  `description` VARCHAR(255) DEFAULT NULL,
  `icone` VARCHAR(50) DEFAULT NULL,
  `ordre` INT NOT NULL DEFAULT 0,
  `actif` TINYINT(1) NOT NULL DEFAULT 1,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_voie`),
  UNIQUE KEY `uq_voie_code` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table de liaison: medicament_forme (many-to-many)
-- --------------------------------------------------------

CREATE TABLE IF NOT EXISTS `medicament_forme` (
  `id_medicament` INT NOT NULL,
  `id_forme` INT NOT NULL,
  `est_defaut` TINYINT(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`id_medicament`, `id_forme`),
  KEY `fk_med_forme_forme` (`id_forme`),
  CONSTRAINT `fk_med_forme_medicament` FOREIGN KEY (`id_medicament`) 
    REFERENCES `medicament` (`id_medicament`) ON DELETE CASCADE,
  CONSTRAINT `fk_med_forme_forme` FOREIGN KEY (`id_forme`) 
    REFERENCES `forme_pharmaceutique` (`id_forme`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table de liaison: medicament_voie (many-to-many)
-- --------------------------------------------------------

CREATE TABLE IF NOT EXISTS `medicament_voie` (
  `id_medicament` INT NOT NULL,
  `id_voie` INT NOT NULL,
  `est_defaut` TINYINT(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`id_medicament`, `id_voie`),
  KEY `fk_med_voie_voie` (`id_voie`),
  CONSTRAINT `fk_med_voie_medicament` FOREIGN KEY (`id_medicament`) 
    REFERENCES `medicament` (`id_medicament`) ON DELETE CASCADE,
  CONSTRAINT `fk_med_voie_voie` FOREIGN KEY (`id_voie`) 
    REFERENCES `voie_administration` (`id_voie`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Données par défaut: Formes pharmaceutiques
-- --------------------------------------------------------

INSERT INTO `forme_pharmaceutique` (`code`, `libelle`, `description`, `ordre`) VALUES
('comprime', 'Comprimé', 'Forme solide à avaler ou à croquer', 1),
('gelule', 'Gélule', 'Capsule contenant une poudre ou un liquide', 2),
('sirop', 'Sirop', 'Solution sucrée pour administration orale', 3),
('solution_buvable', 'Solution buvable', 'Liquide à boire', 4),
('ampoule_injectable', 'Ampoule injectable', 'Solution pour injection', 5),
('pommade', 'Pommade', 'Préparation semi-solide pour application cutanée', 6),
('creme', 'Crème', 'Émulsion pour application cutanée', 7),
('gel', 'Gel', 'Préparation semi-solide transparente', 8),
('suppositoire', 'Suppositoire', 'Forme solide pour administration rectale', 9),
('collyre', 'Collyre', 'Solution pour application ophtalmique', 10),
('spray_nasal', 'Spray nasal', 'Solution pour pulvérisation nasale', 11),
('inhalateur', 'Inhalateur', 'Dispositif pour inhalation', 12),
('patch', 'Patch', 'Dispositif transdermique', 13),
('sachet', 'Sachet', 'Poudre ou granulés en sachet', 14),
('gouttes', 'Gouttes', 'Solution en gouttes', 15),
('ovule', 'Ovule', 'Forme solide pour administration vaginale', 16),
('suspension', 'Suspension', 'Particules solides dispersées dans un liquide', 17),
('poudre', 'Poudre', 'Forme solide à reconstituer', 18);

-- --------------------------------------------------------
-- Données par défaut: Voies d'administration
-- --------------------------------------------------------

INSERT INTO `voie_administration` (`code`, `libelle`, `description`, `ordre`) VALUES
('orale', 'Voie orale', 'Administration par la bouche', 1),
('intraveineuse', 'Voie intraveineuse (IV)', 'Injection directe dans une veine', 2),
('intramusculaire', 'Voie intramusculaire (IM)', 'Injection dans un muscle', 3),
('sous_cutanee', 'Voie sous-cutanée (SC)', 'Injection sous la peau', 4),
('rectale', 'Voie rectale', 'Administration par le rectum', 5),
('cutanee', 'Voie cutanée', 'Application sur la peau', 6),
('ophtalmique', 'Voie ophtalmique', 'Application dans l''œil', 7),
('nasale', 'Voie nasale', 'Administration par le nez', 8),
('inhalee', 'Voie inhalée', 'Inhalation par les voies respiratoires', 9),
('sublinguale', 'Voie sublinguale', 'Sous la langue', 10),
('vaginale', 'Voie vaginale', 'Administration par le vagin', 11),
('transdermique', 'Voie transdermique', 'À travers la peau (patch)', 12),
('auriculaire', 'Voie auriculaire', 'Application dans l''oreille', 13),
('intradermique', 'Voie intradermique', 'Injection dans le derme', 14),
('intrathécale', 'Voie intrathécale', 'Injection dans l''espace sous-arachnoïdien', 15);

-- --------------------------------------------------------
-- Association des médicaments existants avec formes et voies par défaut
-- Basé sur les médicaments de init.sql
-- --------------------------------------------------------

-- Paracetamol (comprimé, voie orale)
INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 1 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Paracetamol' AND f.code = 'comprime';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 1 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Paracetamol' AND v.code = 'orale';

-- Ibuprofene (comprimé, voie orale)
INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 1 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Ibuprofene' AND f.code = 'comprime';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 1 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Ibuprofene' AND v.code = 'orale';

-- Amoxicilline (comprimé + gélule, voie orale)
INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 1 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Amoxicilline' AND f.code = 'gelule';

INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 0 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Amoxicilline' AND f.code = 'suspension';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 1 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Amoxicilline' AND v.code = 'orale';

-- Metronidazole (comprimé, voie orale + IV)
INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 1 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Metronidazole' AND f.code = 'comprime';

INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 0 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Metronidazole' AND f.code = 'ampoule_injectable';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 1 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Metronidazole' AND v.code = 'orale';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 0 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Metronidazole' AND v.code = 'intraveineuse';

-- Omeprazole (gélule, voie orale)
INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 1 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Omeprazole' AND f.code = 'gelule';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 1 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Omeprazole' AND v.code = 'orale';

-- Tramadol (gélule + injectable, voie orale + IV + IM)
INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 1 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Tramadol' AND f.code = 'gelule';

INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 0 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Tramadol' AND f.code = 'ampoule_injectable';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 1 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Tramadol' AND v.code = 'orale';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 0 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Tramadol' AND v.code = 'intraveineuse';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 0 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Tramadol' AND v.code = 'intramusculaire';

-- Diclofenac (comprimé + gel + injectable, voie orale + cutanée + IM)
INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 1 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Diclofenac' AND f.code = 'comprime';

INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 0 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Diclofenac' AND f.code = 'gel';

INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 0 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Diclofenac' AND f.code = 'ampoule_injectable';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 1 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Diclofenac' AND v.code = 'orale';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 0 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Diclofenac' AND v.code = 'cutanee';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 0 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Diclofenac' AND v.code = 'intramusculaire';

-- Ciprofloxacine (comprimé + injectable, voie orale + IV)
INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 1 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Ciprofloxacine' AND f.code = 'comprime';

INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 0 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Ciprofloxacine' AND f.code = 'ampoule_injectable';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 1 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Ciprofloxacine' AND v.code = 'orale';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 0 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Ciprofloxacine' AND v.code = 'intraveineuse';

-- Cotrimoxazole (comprimé + suspension, voie orale)
INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 1 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Cotrimoxazole' AND f.code = 'comprime';

INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 0 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Cotrimoxazole' AND f.code = 'suspension';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 1 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Cotrimoxazole' AND v.code = 'orale';

-- Metformine (comprimé, voie orale)
INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 1 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Metformine' AND f.code = 'comprime';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 1 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Metformine' AND v.code = 'orale';

-- Amlodipine (comprimé, voie orale)
INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 1 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Amlodipine' AND f.code = 'comprime';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 1 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Amlodipine' AND v.code = 'orale';

-- Losartan (comprimé, voie orale)
INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 1 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Losartan' AND f.code = 'comprime';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 1 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Losartan' AND v.code = 'orale';

-- Salbutamol (inhalateur + sirop, voie inhalée + orale)
INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 1 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Salbutamol' AND f.code = 'inhalateur';

INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 0 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Salbutamol' AND f.code = 'sirop';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 1 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Salbutamol' AND v.code = 'inhalee';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 0 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Salbutamol' AND v.code = 'orale';

-- Prednisolone (comprimé + sirop, voie orale)
INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 1 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Prednisolone' AND f.code = 'comprime';

INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 0 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Prednisolone' AND f.code = 'sirop';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 1 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Prednisolone' AND v.code = 'orale';

-- Cefixime (comprimé + suspension, voie orale)
INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 1 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Cefixime' AND f.code = 'comprime';

INSERT INTO `medicament_forme` (`id_medicament`, `id_forme`, `est_defaut`) 
SELECT m.id_medicament, f.id_forme, 0 
FROM `medicament` m, `forme_pharmaceutique` f 
WHERE m.nom = 'Cefixime' AND f.code = 'suspension';

INSERT INTO `medicament_voie` (`id_medicament`, `id_voie`, `est_defaut`) 
SELECT m.id_medicament, v.id_voie, 1 
FROM `medicament` m, `voie_administration` v 
WHERE m.nom = 'Cefixime' AND v.code = 'orale';
