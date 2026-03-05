-- MediConnect Database Schema
-- Toutes les clés primaires sont INT AUTO_INCREMENT
--
-- =====================================================
-- VALEURS DE STATUTS NORMALISÉES
-- =====================================================
-- Consultation: planifie, en_cours, en_pause, terminee, annulee
-- Hospitalisation: en_attente_lit, en_cours, terminee, annulee
-- RendezVous: en_attente, confirme, planifie, en_cours, termine, annule, absent
-- Soin: prescrit, en_cours, termine, annule
-- Facture: en_attente, payee, annulee, partielle
-- Lit: libre, occupe, maintenance, reserve
-- Examen: prescrit, en_cours, termine, annule
-- =====================================================

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET FOREIGN_KEY_CHECKS = 0;
START TRANSACTION;
SET time_zone = "+00:00";

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

-- --------------------------------------------------------
-- Table principale: utilisateurs
-- --------------------------------------------------------

CREATE TABLE `utilisateurs` (
  `id_user` INT NOT NULL AUTO_INCREMENT,
  `nom` VARCHAR(100) NOT NULL,
  `prenom` VARCHAR(100) NOT NULL,
  `naissance` DATE DEFAULT NULL,
  `sexe` VARCHAR(10) DEFAULT NULL,
  `telephone` VARCHAR(20) DEFAULT NULL,
  `email` VARCHAR(120) NOT NULL,
  `situation_matrimoniale` VARCHAR(50) DEFAULT NULL,
  `adresse` TEXT DEFAULT NULL,
  `role` ENUM('patient','medecin','infirmier','administrateur','caissier','accueil','pharmacien','laborantin') NOT NULL,
  `password_hash` VARCHAR(500) DEFAULT NULL,
  `photo` VARCHAR(500) DEFAULT NULL,
  `email_confirmed` BOOLEAN NOT NULL DEFAULT FALSE,
  `email_confirmed_at` TIMESTAMP NULL DEFAULT NULL,
  `profile_completed` BOOLEAN NOT NULL DEFAULT FALSE,
  `profile_completed_at` TIMESTAMP NULL DEFAULT NULL,
  `nationalite` VARCHAR(100) DEFAULT 'Cameroun',
  `region_origine` VARCHAR(100) DEFAULT NULL,
  `must_change_password` BOOLEAN NOT NULL DEFAULT FALSE,
  `created_at` TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_user`),
  UNIQUE KEY `email` (`email`),
  KEY `idx_profile_completed` (`profile_completed`),
  KEY `idx_email_profile` (`email_confirmed`, `profile_completed`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: email_confirmation_tokens
-- --------------------------------------------------------

CREATE TABLE `email_confirmation_tokens` (
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

-- --------------------------------------------------------
-- Table: specialites
-- --------------------------------------------------------

CREATE TABLE `specialites` (
  `id_specialite` INT NOT NULL AUTO_INCREMENT,
  `nom_specialite` VARCHAR(100) NOT NULL,
  `cout_consultation` DECIMAL(12,2) DEFAULT 5000,
  PRIMARY KEY (`id_specialite`),
  UNIQUE KEY `nom_specialite` (`nom_specialite`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `specialites` (`id_specialite`, `nom_specialite`) VALUES
(1, 'Médecine Général'),
(2, 'Allergologie et Immunologie'),
(3, 'Anesthésiologie'),
(4, 'Cardiologie'),
(5, 'Chirurgie Cardio-thoracique'),
(6, 'Chirurgie Colorectale'),
(7, 'Dermatologie'),
(8, 'Radiologie Diagnostique'),
(9, 'Médecine d\'Urgence'),
(10, 'Endocrinologie'),
(11, 'Gastro-entérologie'),
(12, 'Chirurgie Générale'),
(13, 'Gériatrie'),
(14, 'Hématologie'),
(15, 'Médecine Palliative'),
(16, 'Maladies Infectieuses'),
(17, 'Médecine Interne'),
(18, 'Génétique Médicale et Génomique'),
(19, 'Néphrologie'),
(20, 'Neurologie'),
(21, 'Neurochirurgie'),
(22, 'Médecine Nucléaire'),
(23, 'Obstétrique et Gynécologie'),
(24, 'Médecine du Travail'),
(25, 'Ophtalmologie'),
(26, 'Chirurgie Orthopédique'),
(27, 'Oto-rhino-laryngologie (ORL)'),
(28, 'Pathologie'),
(29, 'Pédiatrie'),
(30, 'Médecine Physique et de Réadaptation'),
(31, 'Chirurgie Plastique'),
(32, 'Médecine Préventive'),
(33, 'Psychiatrie'),
(34, 'Pneumologie (Médecine Respiratoire)'),
(35, 'Radiothérapie'),
(36, 'Rhumatologie'),
(37, 'Médecine du Sommeil'),
(38, 'Médecine du Sport'),
(39, 'Chirurgie Thoracique'),
(40, 'Urologie'),
(41, 'Chirurgie Vasculaire'),
(42, 'Santé Publique / Médecine Communautaire'),
(43, 'Services Médicaux d\'Urgence'),
(44, 'Pharmacologie Clinique'),
(45, 'Pathologie Médico-légale'),
(46, 'Médecine de la Douleur'),
(47, 'Médecine du Travail et de l\'Environnement');

-- --------------------------------------------------------
-- Tables de référence: Assurance
-- --------------------------------------------------------

CREATE TABLE `type_prestation` (
  `code` VARCHAR(50) NOT NULL,
  `libelle` VARCHAR(100) NOT NULL,
  `description` VARCHAR(255) DEFAULT NULL,
  `icone` VARCHAR(50) DEFAULT NULL,
  `ordre` INT NOT NULL DEFAULT 0,
  `actif` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `type_prestation` (`code`, `libelle`, `description`, `icone`, `ordre`) VALUES
('consultation', 'Consultation', 'Consultations médicales générales et spécialisées', 'stethoscope', 1),
('hospitalisation', 'Hospitalisation', 'Séjours hospitaliers et soins intensifs', 'bed', 2),
('examen', 'Examens', 'Examens de laboratoire et imagerie médicale', 'microscope', 3),
('pharmacie', 'Pharmacie', 'Médicaments et produits pharmaceutiques', 'pill', 4);

CREATE TABLE `categorie_beneficiaire` (
  `id_categorie` INT NOT NULL AUTO_INCREMENT,
  `code` VARCHAR(50) NOT NULL,
  `libelle` VARCHAR(100) NOT NULL,
  `description` VARCHAR(255) DEFAULT NULL,
  `actif` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id_categorie`),
  UNIQUE KEY `uq_categorie_code` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `categorie_beneficiaire` (`code`, `libelle`, `description`) VALUES
('salaries', 'Salariés', 'Employés du secteur formel'),
('familles', 'Familles', 'Membres de la famille des assurés'),
('retraites', 'Retraités', 'Personnes à la retraite'),
('artisans', 'Artisans', 'Travailleurs indépendants et artisans'),
('etudiants', 'Étudiants', 'Étudiants et élèves'),
('femmes_enceintes', 'Femmes enceintes', 'Couverture maternité'),
('enfants', 'Enfants', 'Mineurs de moins de 18 ans'),
('diaspora', 'Diaspora', 'Ressortissants vivant à l''étranger'),
('indigents', 'Indigents', 'Personnes en situation de précarité');

CREATE TABLE `mode_paiement` (
  `id_mode` INT NOT NULL AUTO_INCREMENT,
  `code` VARCHAR(50) NOT NULL,
  `libelle` VARCHAR(100) NOT NULL,
  `description` VARCHAR(255) DEFAULT NULL,
  `actif` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id_mode`),
  UNIQUE KEY `uq_mode_code` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `mode_paiement` (`code`, `libelle`, `description`) VALUES
('mobile_money', 'Mobile Money', 'Paiement via Orange Money, MTN MoMo, etc.'),
('virement', 'Virement bancaire', 'Virement depuis un compte bancaire'),
('prelevement', 'Prélèvement automatique', 'Prélèvement mensuel automatique'),
('especes', 'Espèces', 'Paiement en espèces'),
('employeur', 'Retenue employeur', 'Cotisation prélevée sur salaire'),
('cotisation_groupe', 'Cotisation de groupe', 'Cotisation collective');

CREATE TABLE `zone_couverture` (
  `id_zone` INT NOT NULL AUTO_INCREMENT,
  `code` VARCHAR(50) NOT NULL,
  `libelle` VARCHAR(100) NOT NULL,
  `description` VARCHAR(255) DEFAULT NULL,
  `actif` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id_zone`),
  UNIQUE KEY `uq_zone_code` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `zone_couverture` (`code`, `libelle`, `description`) VALUES
('national', 'National', 'Couverture sur tout le territoire national'),
('regional', 'Régional', 'Couverture limitée à certaines régions'),
('urbain', 'Zones urbaines', 'Couverture dans les grandes villes'),
('rural', 'Zones rurales', 'Couverture en milieu rural'),
('international', 'International', 'Couverture à l''étranger incluse'),
('diaspora', 'Diaspora', 'Couverture pour les ressortissants à l''étranger');

CREATE TABLE `type_couverture_sante` (
  `id_type_couverture` INT NOT NULL AUTO_INCREMENT,
  `code` VARCHAR(50) NOT NULL,
  `libelle` VARCHAR(100) NOT NULL,
  `description` VARCHAR(255) DEFAULT NULL,
  `actif` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id_type_couverture`),
  UNIQUE KEY `uq_type_couverture_code` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `type_couverture_sante` (`code`, `libelle`, `description`) VALUES
('hospitalisation', 'Hospitalisation', 'Frais d''hospitalisation et séjours'),
('ambulatoire', 'Soins ambulatoires', 'Consultations et soins sans hospitalisation'),
('maternite', 'Maternité', 'Suivi grossesse, accouchement, post-partum'),
('dentaire', 'Soins dentaires', 'Soins et prothèses dentaires'),
('optique', 'Optique', 'Lunettes et lentilles de contact'),
('medicaments', 'Médicaments', 'Remboursement des médicaments'),
('prevention', 'Prévention', 'Vaccinations et bilans de santé'),
('urgences', 'Urgences', 'Soins d''urgence et évacuations'),
('chirurgie', 'Chirurgie', 'Interventions chirurgicales'),
('reeducation', 'Rééducation', 'Kinésithérapie et rééducation'),
('psychiatrie', 'Psychiatrie', 'Soins de santé mentale'),
('maladies_chroniques', 'Maladies chroniques', 'Prise en charge des ALD');

-- --------------------------------------------------------
-- Table: assurances (catalogue des assurances)
-- --------------------------------------------------------

CREATE TABLE `assurances` (
  `id_assurance` INT NOT NULL AUTO_INCREMENT,
  `nom` VARCHAR(150) NOT NULL,
  `type_assurance` VARCHAR(50) DEFAULT 'privee',
  `site_web` VARCHAR(255) DEFAULT NULL,
  `telephone_service_client` VARCHAR(30) DEFAULT NULL,
  `groupe` VARCHAR(100) DEFAULT NULL,
  `pays_origine` VARCHAR(100) DEFAULT NULL,
  `statut_juridique` VARCHAR(50) DEFAULT NULL,
  `description` VARCHAR(1000) DEFAULT NULL,
  `is_complementaire` TINYINT(1) DEFAULT 0,
  `conditions_adhesion` VARCHAR(1000) DEFAULT NULL,
  `is_active` TINYINT(1) DEFAULT 1,
  `id_zone_couverture` INT DEFAULT NULL COMMENT 'FK vers zone_couverture',
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  -- Champs legacy (pour compatibilité pendant migration)
  `type_couverture` VARCHAR(500) DEFAULT NULL COMMENT 'LEGACY: utiliser assurance_type_couverture',
  `categorie_beneficiaires` VARCHAR(255) DEFAULT NULL COMMENT 'LEGACY: utiliser assurance_categorie_beneficiaire',
  `zone_couverture` VARCHAR(100) DEFAULT NULL COMMENT 'LEGACY: utiliser id_zone_couverture',
  `mode_paiement` VARCHAR(255) DEFAULT NULL COMMENT 'LEGACY: utiliser assurance_mode_paiement',
  `email_facturation` VARCHAR(255) DEFAULT NULL COMMENT 'Email pour envoi des factures',
  PRIMARY KEY (`id_assurance`),
  KEY `IX_assurance_nom` (`nom`),
  KEY `IX_assurance_type` (`type_assurance`),
  KEY `IX_assurance_zone` (`id_zone_couverture`),
  KEY `IX_assurance_email` (`email_facturation`),
  CONSTRAINT `fk_assurance_zone` FOREIGN KEY (`id_zone_couverture`) REFERENCES `zone_couverture` (`id_zone`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Donnees par defaut: Assurances
INSERT INTO `assurances` (`nom`, `type_assurance`, `type_couverture`, `zone_couverture`, `is_active`) VALUES
('CNAM', 'publique', 'maladies,hospitalisation', 'national', 1),
('NSIA Assurances', 'privee', 'accidents,maladies,hospitalisation,maternite', 'national', 1),
('Allianz', 'privee', 'accidents,maladies,hospitalisation', 'national', 1),
('SUNU Assurances', 'privee', 'maladies,hospitalisation,maternite', 'national', 1),
('AXA Assurances', 'privee', 'accidents,maladies,hospitalisation,maternite', 'international', 1),
('Saham Assurance', 'privee', 'maladies,hospitalisation', 'national', 1),
('CMU', 'publique', 'forfait_soins_base', 'national', 1);

-- --------------------------------------------------------
-- Table: assurance_couverture (couverture par type de prestation)
-- --------------------------------------------------------

CREATE TABLE `assurance_couverture` (
  `id_couverture` INT NOT NULL AUTO_INCREMENT,
  `id_assurance` INT NOT NULL,
  `type_prestation` VARCHAR(50) NOT NULL COMMENT 'consultation, hospitalisation, examen, pharmacie',
  `taux_couverture` DECIMAL(5,2) NOT NULL DEFAULT 0 COMMENT 'Pourcentage de couverture (0-100)',
  `plafond_annuel` DECIMAL(12,2) DEFAULT NULL COMMENT 'Plafond annuel de remboursement (null = illimite)',
  `plafond_par_acte` DECIMAL(12,2) DEFAULT NULL COMMENT 'Plafond par acte/facture (null = illimite)',
  `franchise` DECIMAL(12,2) DEFAULT NULL COMMENT 'Montant minimum non couvert (null = pas de franchise)',
  `actif` TINYINT(1) NOT NULL DEFAULT 1,
  `notes` VARCHAR(500) DEFAULT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT NULL,
  PRIMARY KEY (`id_couverture`),
  UNIQUE KEY `uq_assurance_type_prestation` (`id_assurance`, `type_prestation`),
  CONSTRAINT `fk_assurance_couverture_assurance` FOREIGN KEY (`id_assurance`) REFERENCES `assurances` (`id_assurance`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Donnees par defaut: Couvertures assurance (taux par type de prestation)
INSERT INTO `assurance_couverture` (`id_assurance`, `type_prestation`, `taux_couverture`, `actif`) VALUES
(1, 'consultation', 80.00, 1), (1, 'hospitalisation', 70.00, 1), (1, 'examen', 60.00, 1), (1, 'pharmacie', 50.00, 1),
(2, 'consultation', 80.00, 1), (2, 'hospitalisation', 75.00, 1), (2, 'examen', 70.00, 1), (2, 'pharmacie', 60.00, 1),
(3, 'consultation', 85.00, 1), (3, 'hospitalisation', 80.00, 1), (3, 'examen', 75.00, 1), (3, 'pharmacie', 65.00, 1),
(4, 'consultation', 75.00, 1), (4, 'hospitalisation', 70.00, 1), (4, 'examen', 65.00, 1), (4, 'pharmacie', 55.00, 1),
(5, 'consultation', 90.00, 1), (5, 'hospitalisation', 85.00, 1), (5, 'examen', 80.00, 1), (5, 'pharmacie', 70.00, 1),
(6, 'consultation', 80.00, 1), (6, 'hospitalisation', 75.00, 1), (6, 'examen', 65.00, 1), (6, 'pharmacie', 55.00, 1),
(7, 'consultation', 100.00, 1), (7, 'hospitalisation', 80.00, 1), (7, 'examen', 70.00, 1), (7, 'pharmacie', 60.00, 1);

-- --------------------------------------------------------
-- Tables de liaison many-to-many: Assurance
-- --------------------------------------------------------

CREATE TABLE `assurance_type_couverture` (
  `id_assurance` INT NOT NULL,
  `id_type_couverture` INT NOT NULL,
  PRIMARY KEY (`id_assurance`, `id_type_couverture`),
  CONSTRAINT `fk_atc_assurance` FOREIGN KEY (`id_assurance`) REFERENCES `assurances` (`id_assurance`) ON DELETE CASCADE,
  CONSTRAINT `fk_atc_type_couverture` FOREIGN KEY (`id_type_couverture`) REFERENCES `type_couverture_sante` (`id_type_couverture`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE `assurance_categorie_beneficiaire` (
  `id_assurance` INT NOT NULL,
  `id_categorie` INT NOT NULL,
  PRIMARY KEY (`id_assurance`, `id_categorie`),
  CONSTRAINT `fk_acb_assurance` FOREIGN KEY (`id_assurance`) REFERENCES `assurances` (`id_assurance`) ON DELETE CASCADE,
  CONSTRAINT `fk_acb_categorie` FOREIGN KEY (`id_categorie`) REFERENCES `categorie_beneficiaire` (`id_categorie`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE `assurance_mode_paiement` (
  `id_assurance` INT NOT NULL,
  `id_mode` INT NOT NULL,
  PRIMARY KEY (`id_assurance`, `id_mode`),
  CONSTRAINT `fk_amp_assurance` FOREIGN KEY (`id_assurance`) REFERENCES `assurances` (`id_assurance`) ON DELETE CASCADE,
  CONSTRAINT `fk_amp_mode` FOREIGN KEY (`id_mode`) REFERENCES `mode_paiement` (`id_mode`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Donnees par defaut: Liaisons assurance (exemples)
-- Toutes les assurances: hospitalisation + ambulatoire + medicaments
INSERT INTO `assurance_type_couverture` (`id_assurance`, `id_type_couverture`)
SELECT a.id_assurance, tc.id_type_couverture
FROM assurances a
CROSS JOIN type_couverture_sante tc
WHERE tc.code IN ('hospitalisation', 'ambulatoire', 'medicaments');

-- Toutes les assurances: salaries + familles
INSERT INTO `assurance_categorie_beneficiaire` (`id_assurance`, `id_categorie`)
SELECT a.id_assurance, cb.id_categorie
FROM assurances a
CROSS JOIN categorie_beneficiaire cb
WHERE cb.code IN ('salaries', 'familles');

-- Toutes les assurances: mobile_money + virement
INSERT INTO `assurance_mode_paiement` (`id_assurance`, `id_mode`)
SELECT a.id_assurance, mp.id_mode
FROM assurances a
CROSS JOIN mode_paiement mp
WHERE mp.code IN ('mobile_money', 'virement');

-- --------------------------------------------------------
-- Table: service
-- --------------------------------------------------------

CREATE TABLE `service` (
  `id_service` INT NOT NULL AUTO_INCREMENT,
  `nom_service` VARCHAR(150) NOT NULL,
  `responsable_service` INT DEFAULT NULL,
  `id_major` INT DEFAULT NULL COMMENT 'ID de l infirmier Major du service',
  `description` TEXT DEFAULT NULL,
  PRIMARY KEY (`id_service`),
  KEY `responsable_service` (`responsable_service`),
  KEY `fk_service_major` (`id_major`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Donnees par defaut: Services
INSERT INTO `service` (`id_service`, `nom_service`, `responsable_service`, `description`) VALUES
(1, 'Administration', NULL, 'Service administratif'),
(2, 'Urgences', NULL, 'Service des urgences'),
(3, 'Médecine Générale', NULL, 'Service de médecine générale'),
(4, 'Chirurgie', NULL, 'Service de chirurgie'),
(5, 'Pédiatrie', NULL, 'Service de pédiatrie'),
(6, 'Maternité', NULL, 'Service de maternité'),
(7, 'Cardiologie', NULL, 'Service de cardiologie'),
(8, 'Radiologie', NULL, 'Service de radiologie');

-- --------------------------------------------------------
-- Table: patient
-- --------------------------------------------------------

CREATE TABLE `patient` (
  `id_user` INT NOT NULL,
  `numero_dossier` VARCHAR(30) DEFAULT NULL,
  -- Informations personnelles
  `ethnie` VARCHAR(100) DEFAULT NULL,
  -- Informations médicales
  `groupe_sanguin` VARCHAR(10) DEFAULT NULL,
  `profession` VARCHAR(255) DEFAULT NULL,
  `maladies_chroniques` TEXT DEFAULT NULL COMMENT 'Liste des maladies chroniques séparées par virgule',
  `operations_chirurgicales` BOOLEAN DEFAULT NULL COMMENT 'A eu des opérations chirurgicales',
  `operations_details` TEXT DEFAULT NULL COMMENT 'Détails des opérations chirurgicales',
  `allergies_connues` BOOLEAN DEFAULT NULL COMMENT 'A des allergies connues',
  `allergies_details` TEXT DEFAULT NULL COMMENT 'Détails des allergies',
  `antecedents_familiaux` BOOLEAN DEFAULT NULL COMMENT 'A des antécédents familiaux',
  `antecedents_familiaux_details` TEXT DEFAULT NULL COMMENT 'Détails des antécédents familiaux',
  -- Habitudes de vie
  `consommation_alcool` BOOLEAN DEFAULT NULL COMMENT 'Consomme de l alcool',
  `frequence_alcool` VARCHAR(50) DEFAULT NULL COMMENT 'Fréquence: occasionnel, regulier, quotidien',
  `tabagisme` BOOLEAN DEFAULT NULL COMMENT 'Fumeur',
  `activite_physique` BOOLEAN DEFAULT NULL COMMENT 'Pratique une activité physique régulière',
  -- Contacts d'urgence
  `nb_enfants` INT DEFAULT 0,
  `personne_contact` VARCHAR(150) DEFAULT NULL,
  `numero_contact` VARCHAR(50) DEFAULT NULL,
  `date_creation` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `id_assurance` INT DEFAULT NULL,
  -- Clôture de dossier médical
  `dossier_cloture` BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Dossier clôturé - prochaine consultation = première consultation',
  `date_cloture_dossier` TIMESTAMP NULL DEFAULT NULL COMMENT 'Date de clôture du dossier',
  `id_medecin_cloture` INT DEFAULT NULL COMMENT 'Médecin ayant clôturé le dossier',
  `taux_couverture_override` DECIMAL(5,2) DEFAULT NULL COMMENT 'Override manuel du taux de couverture assurance',
  PRIMARY KEY (`id_user`),
  UNIQUE KEY `numero_dossier` (`numero_dossier`),
  KEY `fk_patient_assurance` (`id_assurance`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: medecin
-- --------------------------------------------------------

CREATE TABLE `medecin` (
  `id_user` INT NOT NULL,
  `numero_ordre` VARCHAR(50) DEFAULT NULL,
  `id_service` INT NOT NULL,
  `id_specialite` INT DEFAULT NULL,
  PRIMARY KEY (`id_user`),
  UNIQUE KEY `numero_ordre` (`numero_ordre`),
  KEY `fk_medecin_specialite` (`id_specialite`),
  KEY `fk_medecin_service` (`id_service`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: infirmier
-- --------------------------------------------------------

CREATE TABLE `infirmier` (
  `id_user` INT NOT NULL,
  `matricule` VARCHAR(50) DEFAULT NULL,
  `statut` VARCHAR(20) NOT NULL DEFAULT 'actif',
  `is_major` BOOLEAN NOT NULL DEFAULT FALSE,
  `id_service_major` INT NULL,
  `id_service` INT NOT NULL COMMENT 'Service de rattachement obligatoire',
  `date_nomination_major` TIMESTAMP NULL,
  `accreditations` VARCHAR(500) NULL,
  PRIMARY KEY (`id_user`),
  UNIQUE KEY `matricule` (`matricule`),
  INDEX `IX_infirmier_statut` (`statut`),
  INDEX `IX_infirmier_is_major` (`is_major`),
  INDEX `idx_infirmier_service` (`id_service`),
  CONSTRAINT `FK_infirmier_service_major` FOREIGN KEY (`id_service_major`) 
    REFERENCES `service`(`id_service`) ON DELETE SET NULL,
  CONSTRAINT `fk_infirmier_service` FOREIGN KEY (`id_service`) 
    REFERENCES `service`(`id_service`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: administrateur
-- --------------------------------------------------------

CREATE TABLE `administrateur` (
  `id_user` INT NOT NULL,
  PRIMARY KEY (`id_user`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: caissier
-- --------------------------------------------------------

CREATE TABLE `caissier` (
  `id_user` INT NOT NULL,
  PRIMARY KEY (`id_user`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: accueil
-- --------------------------------------------------------

CREATE TABLE `accueil` (
  `id_user` INT NOT NULL,
  `poste` VARCHAR(100) DEFAULT NULL,
  `date_embauche` DATETIME DEFAULT NULL,
  PRIMARY KEY (`id_user`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: pharmacien
-- --------------------------------------------------------

CREATE TABLE `pharmacien` (
  `id_user` INT NOT NULL,
  `numero_ordre` VARCHAR(50) DEFAULT NULL,
  `matricule` VARCHAR(50) DEFAULT NULL,
  `date_embauche` DATETIME DEFAULT NULL,
  `actif` BOOLEAN DEFAULT TRUE,
  `created_at` DATETIME DEFAULT NULL,
  `updated_at` DATETIME DEFAULT NULL,
  PRIMARY KEY (`id_user`),
  CONSTRAINT `pharmacien_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `utilisateurs` (`id_user`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: laborantin (remplace biologiste)
-- --------------------------------------------------------

CREATE TABLE `laborantin` (
  `id_user` INT NOT NULL,
  `matricule` VARCHAR(50) DEFAULT NULL,
  `specialisation` VARCHAR(100) DEFAULT NULL COMMENT 'Spécialisation: microbiologie, biochimie, hématologie, etc.',
  `id_labo` INT NOT NULL COMMENT 'Laboratoire d affectation (obligatoire)',
  `date_embauche` DATETIME DEFAULT NULL,
  `actif` TINYINT(1) DEFAULT 1,
  `created_at` TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_user`),
  KEY `idx_laborantin_labo` (`id_labo`),
  CONSTRAINT `fk_laborantin_user` FOREIGN KEY (`id_user`) REFERENCES `utilisateurs` (`id_user`) ON DELETE CASCADE,
  CONSTRAINT `fk_laborantin_labo` FOREIGN KEY (`id_labo`) REFERENCES `laboratoire` (`id_labo`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: standard_chambre
-- --------------------------------------------------------

CREATE TABLE `standard_chambre` (
  `id_standard` INT NOT NULL AUTO_INCREMENT,
  `nom` VARCHAR(100) NOT NULL,
  `description` TEXT DEFAULT NULL,
  `prix_journalier` DECIMAL(12,2) NOT NULL DEFAULT 0,
  `privileges` JSON DEFAULT NULL,
  `localisation` VARCHAR(255) DEFAULT NULL,
  `actif` TINYINT(1) NOT NULL DEFAULT 1,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_standard`),
  UNIQUE KEY `nom` (`nom`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Données par défaut: Standards de chambre
INSERT INTO `standard_chambre` (`nom`, `description`, `prix_journalier`, `privileges`, `localisation`) VALUES
('Standard', 'Chambre standard avec équipements de base', 15000.00, '["Lit simple", "Salle de bain partagée", "Télévision"]', 'Bâtiment A - Étage 1'),
('Confort', 'Chambre confort avec salle de bain privée', 25000.00, '["Lit double", "Salle de bain privée", "Télévision", "Climatisation", "Réfrigérateur"]', 'Bâtiment A - Étage 2'),
('VIP', 'Suite VIP avec services premium', 50000.00, '["Lit king size", "Salle de bain privée luxe", "Télévision écran plat", "Climatisation", "Réfrigérateur", "Canapé visiteur", "Service repas en chambre", "WiFi haut débit"]', 'Bâtiment B - Étage 3'),
('Soins Intensifs', 'Chambre équipée pour soins intensifs', 75000.00, '["Équipement médical avancé", "Monitoring 24h/24", "Personnel dédié"]', 'Bâtiment C - Unité SI');

-- --------------------------------------------------------
-- Table: chambre
-- --------------------------------------------------------

CREATE TABLE `chambre` (
  `id_chambre` INT NOT NULL AUTO_INCREMENT,
  `numero` VARCHAR(20) DEFAULT NULL,
  `capacite` INT DEFAULT NULL,
  `etat` VARCHAR(50) DEFAULT NULL,
  `statut` VARCHAR(50) DEFAULT NULL,
  `id_standard` INT DEFAULT NULL,
  PRIMARY KEY (`id_chambre`),
  UNIQUE KEY `numero` (`numero`),
  KEY `fk_chambre_standard` (`id_standard`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: lit
-- --------------------------------------------------------

CREATE TABLE `lit` (
  `id_lit` INT NOT NULL AUTO_INCREMENT,
  `numero` VARCHAR(20) DEFAULT NULL,
  `statut` VARCHAR(50) DEFAULT NULL,
  `id_chambre` INT NOT NULL,
  PRIMARY KEY (`id_lit`),
  KEY `id_chambre` (`id_chambre`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: consultation
-- --------------------------------------------------------

CREATE TABLE `consultation` (
  `id_consultation` INT NOT NULL AUTO_INCREMENT,
  `date_heure` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  `date_debut_effective` TIMESTAMP NULL DEFAULT NULL COMMENT 'Date et heure de début effectif de la consultation',
  `date_fin` TIMESTAMP NULL DEFAULT NULL COMMENT 'Date et heure de fin de la consultation',
  `duree_minutes` INT DEFAULT NULL COMMENT 'Durée réelle de la consultation en minutes',
  `motif_annulation` TEXT DEFAULT NULL COMMENT 'Motif d annulation si la consultation est annulée',
  `date_annulation` TIMESTAMP NULL DEFAULT NULL COMMENT 'Date et heure d annulation',
  `motif` TEXT DEFAULT NULL,
  `diagnostic` TEXT DEFAULT NULL,
  `statut` VARCHAR(20) DEFAULT NULL COMMENT 'planifie, en_cours, en_pause, terminee, annulee',
  `id_medecin` INT NOT NULL,
  `id_patient` INT NOT NULL,
  `id_rdv` INT DEFAULT NULL,
  `poids` DECIMAL(5,2) DEFAULT NULL,
  `temperature` DECIMAL(4,2) DEFAULT NULL,
  `type_consultation` VARCHAR(100) DEFAULT NULL,
  `antecedents` TEXT DEFAULT NULL,
  `chemin_questionnaire` VARCHAR(255) DEFAULT NULL,
  `tension` VARCHAR(10) DEFAULT NULL,
  `anamnese` TEXT DEFAULT NULL,
  `notes_cliniques` TEXT DEFAULT NULL,
  `conclusion` TEXT DEFAULT NULL,
  -- Examen clinique (Étape 2)
  `examen_inspection` TEXT DEFAULT NULL COMMENT 'Observations visuelles: aspect général, peau, muqueuses',
  `examen_palpation` TEXT DEFAULT NULL COMMENT 'Résultats de la palpation: abdomen, ganglions, etc.',
  `examen_auscultation` TEXT DEFAULT NULL COMMENT 'Auscultation: coeur, poumons, abdomen',
  `examen_percussion` TEXT DEFAULT NULL COMMENT 'Percussion: thorax, abdomen',
  `examen_autres` TEXT DEFAULT NULL COMMENT 'Autres observations cliniques',
  -- Diagnostic et orientation (Étape 3)
  `diagnostics_secondaires` TEXT DEFAULT NULL COMMENT 'Diagnostics différentiels ou associés',
  `hypotheses_diagnostiques` TEXT DEFAULT NULL COMMENT 'Hypothèses à confirmer par examens',
  -- Plan de traitement (Étape 4)
  `explication_diagnostic` TEXT DEFAULT NULL COMMENT 'Explication du diagnostic au patient',
  `options_traitement` TEXT DEFAULT NULL COMMENT 'Options de traitement proposées',
  `orientation_specialiste` TEXT DEFAULT NULL COMMENT 'Spécialiste vers lequel orienter le patient',
  `motif_orientation` TEXT DEFAULT NULL COMMENT 'Motif de l orientation vers un spécialiste',
  -- Conclusion (Étape 5)
  `resume_consultation` TEXT DEFAULT NULL COMMENT 'Résumé des points importants',
  `questions_patient` TEXT DEFAULT NULL COMMENT 'Questions du patient et réponses',
  `consignes_patient` TEXT DEFAULT NULL COMMENT 'Consignes données au patient',
  `recommandations` TEXT DEFAULT NULL COMMENT 'Recommandations générales',
  PRIMARY KEY (`id_consultation`),
  KEY `id_medecin` (`id_medecin`),
  KEY `id_patient` (`id_patient`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: consultation_gyneco (extension 1-1 pour Gynécologie)
-- --------------------------------------------------------

CREATE TABLE `consultation_gyneco` (
  `id_consultation` INT NOT NULL,
  `inspection_externe` TEXT DEFAULT NULL,
  `examen_speculum` TEXT DEFAULT NULL,
  `toucher_vaginal` TEXT DEFAULT NULL,
  `autres_observations` TEXT DEFAULT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT NULL,
  PRIMARY KEY (`id_consultation`),
  CONSTRAINT `fk_consultation_gyneco_consultation`
    FOREIGN KEY (`id_consultation`) REFERENCES `consultation` (`id_consultation`)
    ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE UNIQUE INDEX `IX_consultation_gyneco_consultation`
  ON `consultation_gyneco` (`id_consultation`);

-- --------------------------------------------------------
-- Table: hospitalisation
-- --------------------------------------------------------

CREATE TABLE `hospitalisation` (
  `id_admission` INT NOT NULL AUTO_INCREMENT,
  `date_entree` DATE NOT NULL,
  `date_sortie` DATE DEFAULT NULL,
  `date_sortie_prevue` DATETIME DEFAULT NULL COMMENT 'Date de sortie prévue',
  `motif` TEXT DEFAULT NULL,
  `motif_sortie` VARCHAR(500) DEFAULT NULL COMMENT 'Motif de sortie: guerison, amelioration, transfert, demande_patient, autre',
  `resume_medical` TEXT DEFAULT NULL COMMENT 'Résumé médical de sortie',
  `statut` VARCHAR(20) DEFAULT 'en_attente_lit',
  `id_patient` INT NOT NULL,
  `id_lit` INT DEFAULT NULL COMMENT 'Nullable: hospitalisation en attente de lit',
  `id_medecin` INT DEFAULT NULL,
  `urgence` VARCHAR(20) DEFAULT 'normale' COMMENT 'Niveau urgence: normale, urgente, critique',
  `diagnostic_principal` TEXT DEFAULT NULL COMMENT 'Diagnostic principal justifiant hospitalisation',
  `id_consultation` INT DEFAULT NULL COMMENT 'Consultation ayant généré hospitalisation',
  `id_service` INT DEFAULT NULL COMMENT 'Service concerné',
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_admission`),
  KEY `id_patient` (`id_patient`),
  KEY `id_lit` (`id_lit`),
  KEY `fk_hospitalisation_medecin` (`id_medecin`),
  KEY `idx_hospitalisation_statut` (`statut`),
  KEY `idx_hospitalisation_service` (`id_service`),
  KEY `idx_hospitalisation_urgence` (`urgence`),
  KEY `fk_hospitalisation_consultation` (`id_consultation`),
  KEY `fk_hospitalisation_service` (`id_service`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: orientation_pre_consultation (orientations unifiées - remplace recommandation et orientation_specialiste)
-- --------------------------------------------------------

CREATE TABLE `orientation_pre_consultation` (
  `id_orientation` INT NOT NULL AUTO_INCREMENT,
  `id_consultation` INT NOT NULL,
  `id_patient` INT NOT NULL,
  `id_medecin_prescripteur` INT NOT NULL COMMENT 'Médecin prescripteur',
  `type_orientation` VARCHAR(30) NOT NULL DEFAULT 'medecin_interne' COMMENT 'Type: medecin_interne, medecin_externe, hopital, service_interne, laboratoire',
  `id_specialite` INT DEFAULT NULL COMMENT 'FK vers spécialité (pour médecin interne)',
  `id_medecin_oriente` INT DEFAULT NULL COMMENT 'FK vers médecin interne orienté',
  `nom_destinataire` VARCHAR(255) DEFAULT NULL COMMENT 'Nom du destinataire (médecin externe ou hôpital)',
  `specialite_texte` VARCHAR(100) DEFAULT NULL COMMENT 'Spécialité en texte libre',
  `adresse_destinataire` TEXT DEFAULT NULL COMMENT 'Adresse du destinataire externe',
  `telephone_destinataire` VARCHAR(20) DEFAULT NULL COMMENT 'Téléphone du destinataire externe',
  `motif` TEXT NOT NULL COMMENT 'Motif de l orientation (obligatoire)',
  `notes` TEXT DEFAULT NULL COMMENT 'Notes complémentaires',
  `urgence` TINYINT(1) DEFAULT 0 COMMENT 'Orientation urgente',
  `prioritaire` TINYINT(1) DEFAULT 0 COMMENT 'Orientation prioritaire',
  `statut` VARCHAR(30) DEFAULT 'en_attente' COMMENT 'Statut: en_attente, acceptee, refusee, rdv_pris, terminee, annulee',
  `date_orientation` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT 'Date de création de l orientation',
  `date_rdv_propose` DATETIME DEFAULT NULL COMMENT 'Date de RDV proposée',
  `id_rdv_cree` INT DEFAULT NULL COMMENT 'FK vers le RDV créé suite à l orientation',
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_orientation`),
  KEY `idx_orientation_consultation` (`id_consultation`),
  KEY `idx_orientation_patient` (`id_patient`),
  KEY `idx_orientation_medecin_prescripteur` (`id_medecin_prescripteur`),
  KEY `idx_orientation_statut` (`statut`),
  KEY `idx_orientation_type` (`type_orientation`),
  KEY `fk_orientation_specialite` (`id_specialite`),
  KEY `fk_orientation_medecin_oriente` (`id_medecin_oriente`),
  KEY `fk_orientation_rdv` (`id_rdv_cree`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: soin_hospitalisation (soins liés à une hospitalisation)
-- --------------------------------------------------------

CREATE TABLE `soin_hospitalisation` (
  `id_soin` INT NOT NULL AUTO_INCREMENT,
  `id_hospitalisation` INT NOT NULL,
  `type_soin` VARCHAR(100) NOT NULL COMMENT 'Type de soin: soins_infirmiers, surveillance, reeducation, nutrition, autre',
  `description` VARCHAR(255) NOT NULL COMMENT 'Description du soin',
  `frequence` VARCHAR(100) DEFAULT NULL COMMENT 'Fréquence: 1x/jour, 2x/jour, 3x/jour, etc.',
  `duree_jours` INT DEFAULT NULL COMMENT 'Durée en jours',
  `moments` VARCHAR(100) DEFAULT NULL COMMENT 'Legacy: Moments matin,midi,soir (obsolète, utiliser nb_fois_par_jour)',
  `nb_fois_par_jour` INT DEFAULT 1 COMMENT 'Nombre de séances par jour (1 à 12)',
  `horaires_personnalises` TEXT DEFAULT NULL COMMENT 'Horaires personnalisés en JSON: ["08:00","12:00","18:00"]',
  `priorite` VARCHAR(20) DEFAULT 'normale' COMMENT 'Priorité: basse, normale, haute, urgente',
  `instructions` TEXT DEFAULT NULL COMMENT 'Instructions spécifiques',
  `statut` VARCHAR(20) DEFAULT 'prescrit' COMMENT 'Statut: prescrit, en_cours, termine, annule',
  `date_prescription` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `date_debut` DATE DEFAULT NULL COMMENT 'Date de debut du traitement',
  `date_fin_prevue` DATE DEFAULT NULL COMMENT 'Date de fin prevue du traitement',
  `id_prescripteur` INT DEFAULT NULL COMMENT 'Médecin ayant prescrit le soin',
  `nb_executions_prevues` INT DEFAULT 0 COMMENT 'Nombre total d executions prevues (calculé automatiquement)',
  `nb_executions_effectuees` INT DEFAULT 0 COMMENT 'Nombre d executions effectuees',
  PRIMARY KEY (`id_soin`),
  KEY `fk_soin_hospitalisation` (`id_hospitalisation`),
  KEY `fk_soin_prescripteur` (`id_prescripteur`),
  KEY `idx_soin_type` (`type_soin`),
  KEY `idx_soin_statut` (`statut`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: execution_soin (planification et suivi des exécutions de soins)
-- --------------------------------------------------------

CREATE TABLE `execution_soin` (
  `id_execution` INT NOT NULL AUTO_INCREMENT,
  `id_soin` INT NOT NULL COMMENT 'Reference au soin prescrit',
  `date_prevue` DATE NOT NULL COMMENT 'Date prevue pour l execution',
  `moment` VARCHAR(50) DEFAULT NULL COMMENT 'Legacy: Moment de la journee (obsolète, utiliser numero_seance)',
  `numero_seance` INT DEFAULT 1 COMMENT 'Numéro de la séance dans la journée (1, 2, 3...)',
  `heure_prevue` TIME DEFAULT NULL COMMENT 'Heure prevue pour cette séance',
  `heure_execution` TIME DEFAULT NULL COMMENT 'Heure réelle de l execution',
  `statut` ENUM('prevu', 'fait', 'manque', 'reporte', 'annule') NOT NULL DEFAULT 'prevu' COMMENT 'Statut de l execution',
  `date_execution` DATETIME DEFAULT NULL COMMENT 'Date et heure reelle de l execution',
  `id_executant` INT DEFAULT NULL COMMENT 'ID de l infirmier ayant effectue le soin',
  `observations` TEXT DEFAULT NULL COMMENT 'Notes et observations lors de l execution',
  `numero_execution` INT DEFAULT 1 COMMENT 'Numero sequentiel global de l execution',
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_execution`),
  KEY `idx_execution_soin` (`id_soin`),
  KEY `idx_execution_date_prevue` (`date_prevue`),
  KEY `idx_execution_seance` (`numero_seance`),
  KEY `idx_execution_statut` (`statut`),
  KEY `idx_execution_executant` (`id_executant`),
  KEY `idx_execution_jour_seance` (`date_prevue`, `numero_seance`),
  CONSTRAINT `fk_execution_soin` FOREIGN KEY (`id_soin`) 
    REFERENCES `soin_hospitalisation` (`id_soin`) ON DELETE CASCADE,
  CONSTRAINT `fk_execution_executant` FOREIGN KEY (`id_executant`) 
    REFERENCES `utilisateurs` (`id_user`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: ordonnance (remplace prescription)
-- Supporte tous les contextes de prescription :
-- - Consultation classique
-- - Hospitalisation  
-- - Prescription directe (hors consultation)
-- --------------------------------------------------------

CREATE TABLE `ordonnance` (
  `id_ordonnance` INT NOT NULL AUTO_INCREMENT,
  `date` DATE NOT NULL,
  `id_patient` INT DEFAULT NULL COMMENT 'Lien direct au patient',
  `id_medecin` INT DEFAULT NULL COMMENT 'Médecin prescripteur',
  `id_consultation` INT DEFAULT NULL COMMENT 'Consultation de rattachement (optionnel)',
  `id_hospitalisation` INT DEFAULT NULL COMMENT 'Hospitalisation si contexte hospitalier',
  `type_contexte` VARCHAR(50) DEFAULT 'consultation' COMMENT 'consultation, hospitalisation, directe',
  `statut` VARCHAR(50) DEFAULT 'active' COMMENT 'active, dispensee, partielle, annulee, expiree',
  `commentaire` TEXT DEFAULT NULL,
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  `date_expiration` DATETIME DEFAULT NULL COMMENT 'Date d expiration de l ordonnance',
  `renouvelable` TINYINT(1) DEFAULT 0 COMMENT 'Ordonnance renouvelable',
  `nombre_renouvellements` INT DEFAULT NULL COMMENT 'Nombre de renouvellements autorises',
  `renouvellements_restants` INT DEFAULT NULL COMMENT 'Nombre de renouvellements restants',
  `id_ordonnance_originale` INT DEFAULT NULL COMMENT 'ID ordonnance originale si renouvellement',
  PRIMARY KEY (`id_ordonnance`),
  KEY `id_consultation` (`id_consultation`),
  KEY `idx_ordonnance_patient` (`id_patient`),
  KEY `idx_ordonnance_medecin` (`id_medecin`),
  KEY `idx_ordonnance_hospitalisation` (`id_hospitalisation`),
  KEY `idx_ordonnance_type_contexte` (`type_contexte`),
  KEY `idx_ordonnance_statut` (`statut`),
  KEY `idx_ordonnance_date` (`date` DESC),
  KEY `idx_ordonnance_expiration` (`date_expiration`),
  KEY `idx_ordonnance_renouvelable` (`renouvelable`),
  KEY `idx_ordonnance_originale` (`id_ordonnance_originale`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: medicament
-- --------------------------------------------------------

CREATE TABLE `medicament` (
  `id_medicament` INT NOT NULL AUTO_INCREMENT,
  `nom` VARCHAR(150) NOT NULL,
  `dosage` VARCHAR(100) DEFAULT NULL,
  `date_heure_creation` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `stock` INT DEFAULT NULL,
  `prix` FLOAT DEFAULT NULL,
  `seuil_stock` INT DEFAULT NULL,
  `code_ATC` VARCHAR(20) DEFAULT NULL,
  `forme_galenique` ENUM('comprime','sirop','injectable') DEFAULT NULL,
  `laboratoire` VARCHAR(150) DEFAULT NULL,
  `conditionnement` VARCHAR(100) DEFAULT NULL,
  `date_peremption` DATE DEFAULT NULL,
  `actif` TINYINT(1) DEFAULT 1,
  `emplacement_rayon` VARCHAR(50) DEFAULT NULL,
  `temperature_conservation` VARCHAR(50) DEFAULT NULL,
  PRIMARY KEY (`id_medicament`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Donnees par defaut: Medicaments
INSERT INTO `medicament` (`nom`, `dosage`, `stock`, `prix`, `seuil_stock`, `laboratoire`, `actif`) VALUES
('Paracetamol', '500mg', 100, 500, 20, 'Sanofi', 1),
('Ibuprofene', '400mg', 80, 750, 15, 'Pfizer', 1),
('Amoxicilline', '500mg', 60, 1200, 10, 'GSK', 1),
('Metronidazole', '250mg', 50, 800, 10, 'Cipla', 1),
('Omeprazole', '20mg', 70, 1500, 15, 'AstraZeneca', 1),
('Tramadol', '50mg', 40, 2000, 10, 'Grunenthal', 1),
('Diclofenac', '50mg', 90, 600, 20, 'Novartis', 1),
('Ciprofloxacine', '500mg', 45, 1800, 10, 'Bayer', 1),
('Cotrimoxazole', '480mg', 55, 900, 15, 'Roche', 1),
('Metformine', '500mg', 65, 1100, 15, 'Merck', 1),
('Amlodipine', '5mg', 50, 1300, 10, 'Pfizer', 1),
('Losartan', '50mg', 45, 1400, 10, 'Merck', 1),
('Salbutamol', '100mcg', 30, 2500, 10, 'GSK', 1),
('Prednisolone', '5mg', 40, 1600, 10, 'Sanofi', 1),
('Cefixime', '200mg', 35, 2200, 10, 'Cipla', 1);

-- --------------------------------------------------------
-- Table: ordonnance_medicament (remplace prescription_medicament)
-- Supporte les médicaments de la BD ET les médicaments en saisie libre
-- --------------------------------------------------------

CREATE TABLE `ordonnance_medicament` (
  `id_prescription_med` INT NOT NULL AUTO_INCREMENT,
  `id_ordonnance` INT NOT NULL,
  `id_medicament` INT DEFAULT NULL COMMENT 'Référence au catalogue (NULL si saisie libre)',
  `nom_medicament_libre` VARCHAR(255) DEFAULT NULL COMMENT 'Nom du médicament en saisie libre',
  `dosage_libre` VARCHAR(100) DEFAULT NULL COMMENT 'Dosage en saisie libre',
  `est_hors_catalogue` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 si médicament non référencé',
  `quantite` INT DEFAULT 1,
  `duree_traitement` VARCHAR(100) DEFAULT NULL,
  `posologie` VARCHAR(200) DEFAULT NULL,
  `frequence` VARCHAR(100) DEFAULT NULL,
  `voie_administration` VARCHAR(100) DEFAULT NULL,
  `forme_pharmaceutique` VARCHAR(100) DEFAULT NULL,
  `instructions` TEXT DEFAULT NULL,
  PRIMARY KEY (`id_prescription_med`),
  KEY `id_ordonnance` (`id_ordonnance`),
  KEY `id_medicament` (`id_medicament`),
  KEY `idx_ordonnance_hors_catalogue` (`est_hors_catalogue`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: laboratoire
-- --------------------------------------------------------

CREATE TABLE `laboratoire` (
  `id_labo` INT NOT NULL AUTO_INCREMENT,
  `nom_labo` VARCHAR(150) NOT NULL,
  `contact` VARCHAR(150) DEFAULT NULL,
  `adresse` VARCHAR(255) DEFAULT NULL,
  `telephone` VARCHAR(20) DEFAULT NULL,
  `email` VARCHAR(150) DEFAULT NULL,
  `type` ENUM('interne', 'externe') DEFAULT 'interne',
  `actif` TINYINT(1) DEFAULT 1,
  PRIMARY KEY (`id_labo`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Donnees par defaut: Laboratoires
INSERT INTO `laboratoire` (`nom_labo`, `contact`, `adresse`, `telephone`, `type`, `actif`) VALUES
('Laboratoire Central Hopital', 'Dr. Kouame', 'Batiment A - RDC', '+225 27 22 44 55 66', 'interne', 1),
('Service Imagerie Hopital', 'Dr. Traore', 'Batiment B - 1er etage', '+225 27 22 44 55 67', 'interne', 1),
('Laboratoire BioMedical Plus', 'M. Diallo', 'Cocody, Rue des Jardins', '+225 07 08 09 10 11', 'externe', 1),
('Centre Imagerie Medicale Abidjan', 'Mme Kone', 'Plateau, Avenue Franchet', '+225 05 06 07 08 09', 'externe', 1),
('Laboratoire Pasteur Cote Ivoire', 'Dr. Bamba', 'Treichville, Bd VGE', '+225 27 21 35 46 57', 'externe', 1);

-- --------------------------------------------------------
-- --------------------------------------------------------
-- Table: categories_examens (Niveau 1)
-- --------------------------------------------------------

CREATE TABLE `categories_examens` (
  `id_categorie` INT NOT NULL AUTO_INCREMENT,
  `nom` VARCHAR(100) NOT NULL,
  `code` VARCHAR(50) NOT NULL,
  `description` TEXT DEFAULT NULL,
  `icone` VARCHAR(50) DEFAULT NULL,
  `ordre_affichage` INT DEFAULT 0,
  `actif` TINYINT(1) DEFAULT 1,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_categorie`),
  UNIQUE KEY `uk_code` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Donnees par defaut: Categories
INSERT INTO `categories_examens` (`nom`, `code`, `description`, `icone`, `ordre_affichage`) VALUES
('Biologie Medicale', 'biologie_medicale', 'Analyses de laboratoire et examens biologiques', 'flask', 1),
('Imagerie Medicale', 'imagerie_medicale', 'Examens d imagerie diagnostique', 'scan', 2);

-- --------------------------------------------------------
-- Table: specialites_examens (Niveau 2)
-- --------------------------------------------------------

CREATE TABLE `specialites_examens` (
  `id_specialite` INT NOT NULL AUTO_INCREMENT,
  `id_categorie` INT NOT NULL,
  `nom` VARCHAR(100) NOT NULL,
  `code` VARCHAR(50) NOT NULL,
  `description` TEXT DEFAULT NULL,
  `icone` VARCHAR(50) DEFAULT NULL,
  `ordre_affichage` INT DEFAULT 0,
  `actif` TINYINT(1) DEFAULT 1,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_specialite`),
  UNIQUE KEY `uk_code` (`code`),
  KEY `fk_specialite_categorie` (`id_categorie`),
  CONSTRAINT `fk_specialite_categorie` FOREIGN KEY (`id_categorie`) REFERENCES `categories_examens` (`id_categorie`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Donnees par defaut: Specialites - Biologie Medicale (id_categorie = 1)
INSERT INTO `specialites_examens` (`id_categorie`, `nom`, `code`, `description`, `ordre_affichage`) VALUES
(1, 'Hematologie', 'hematologie', 'Etude du sang et de ses composants', 1),
(1, 'Biochimie', 'biochimie', 'Analyses biochimiques et metaboliques', 2),
(1, 'Fonction Renale', 'fonction_renale', 'Evaluation de la fonction renale', 3),
(1, 'Fonction Hepatique', 'fonction_hepatique', 'Evaluation de la fonction hepatique', 4),
(1, 'Marqueurs Inflammatoires', 'marqueurs_inflammatoires', 'Marqueurs d inflammation', 5),
(1, 'Microbiologie', 'microbiologie', 'Analyses microbiologiques et serologiques', 6),
(1, 'Endocrinologie', 'endocrinologie', 'Dosages hormonaux', 7),
(1, 'Marqueurs Tumoraux', 'marqueurs_tumoraux', 'Marqueurs de cancers', 8),
(1, 'Immunologie', 'immunologie', 'Analyses immunologiques', 9);

-- Donnees par defaut: Specialites - Imagerie Medicale (id_categorie = 2)
INSERT INTO `specialites_examens` (`id_categorie`, `nom`, `code`, `description`, `ordre_affichage`) VALUES
(2, 'Radiologie Standard', 'radiologie_standard', 'Radiographies conventionnelles', 1),
(2, 'Echographie', 'echographie', 'Examens echographiques et doppler', 2),
(2, 'IRM', 'irm', 'Imagerie par resonance magnetique', 3),
(2, 'Imagerie Specialisee', 'imagerie_specialisee', 'Mammographie, osteodensitometrie', 4),
(2, 'Cardiologie', 'cardiologie', 'ECG et examens cardiaques', 5);

-- --------------------------------------------------------
-- Table: examens (Niveau 3)
-- --------------------------------------------------------

CREATE TABLE `examens` (
  `id_exam` INT NOT NULL AUTO_INCREMENT,
  `id_specialite` INT NOT NULL,
  `nom_exam` VARCHAR(150) NOT NULL,
  `description` TEXT DEFAULT NULL,
  `prix_unitaire` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `duree_estimee_minutes` INT DEFAULT 30,
  `preparation_requise` TEXT DEFAULT NULL,
  `disponible` TINYINT(1) DEFAULT 1,
  `actif` TINYINT(1) DEFAULT 1,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_exam`),
  KEY `fk_examen_specialite` (`id_specialite`),
  INDEX `idx_disponible` (`disponible`),
  CONSTRAINT `fk_examen_specialite` FOREIGN KEY (`id_specialite`) REFERENCES `specialites_examens` (`id_specialite`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Donnees par defaut: Examens
-- --------------------------------------------------------

-- Hematologie (id_specialite = 1)
INSERT INTO `examens` (`id_specialite`, `nom_exam`, `description`, `prix_unitaire`, `disponible`) VALUES
(1, 'NFS (Numeration Formule Sanguine)', 'Hemogramme complet avec numeration des globules', 5000, 1),
(1, 'Groupe sanguin ABO-Rh', 'Determination du groupe sanguin et facteur Rhesus', 4000, 1),
(1, 'Bilan de coagulation', 'TP, TCA, INR - evaluation de la coagulation', 8000, 1),
(1, 'Vitesse de sedimentation (VS)', 'Marqueur non specifique d inflammation', 2500, 1);

-- Biochimie (id_specialite = 2)
INSERT INTO `examens` (`id_specialite`, `nom_exam`, `description`, `prix_unitaire`, `disponible`) VALUES
(2, 'Glycemie a jeun', 'Dosage du glucose sanguin a jeun', 3000, 1),
(2, 'HbA1c (Hemoglobine glyquee)', 'Controle glycemique sur 3 mois', 6000, 1),
(2, 'Bilan lipidique complet', 'Cholesterol total, HDL, LDL, triglycerides', 8000, 1),
(2, 'Ionogramme sanguin', 'Na, K, Cl, bicarbonates', 8000, 1),
(2, 'Acide urique', 'Dosage de l uricemie', 3500, 1);

-- Fonction Renale (id_specialite = 3)
INSERT INTO `examens` (`id_specialite`, `nom_exam`, `description`, `prix_unitaire`, `disponible`) VALUES
(3, 'Creatininemie', 'Dosage de la creatinine sanguine', 4000, 1),
(3, 'Uree sanguine', 'Evaluation de la fonction renale', 3500, 1),
(3, 'Clairance de la creatinine', 'Estimation du debit de filtration glomerulaire', 5000, 1);

-- Fonction Hepatique (id_specialite = 4)
INSERT INTO `examens` (`id_specialite`, `nom_exam`, `description`, `prix_unitaire`, `disponible`) VALUES
(4, 'ASAT (Transaminase)', 'Enzyme hepatique - cytolyse', 3500, 1),
(4, 'ALAT (Transaminase)', 'Enzyme hepatique - cytolyse', 3500, 1),
(4, 'Gamma-GT', 'Marqueur de cholestase et alcoolisme', 4000, 1),
(4, 'Phosphatases alcalines', 'Marqueur de cholestase', 4000, 1),
(4, 'Bilirubine totale et conjuguee', 'Evaluation de l ictere', 5000, 1);

-- Marqueurs Inflammatoires (id_specialite = 5)
INSERT INTO `examens` (`id_specialite`, `nom_exam`, `description`, `prix_unitaire`, `disponible`) VALUES
(5, 'CRP (Proteine C reactive)', 'Marqueur d inflammation aigue', 5000, 1),
(5, 'Procalcitonine', 'Marqueur d infection bacterienne severe', 12000, 1);

-- Microbiologie (id_specialite = 6)
INSERT INTO `examens` (`id_specialite`, `nom_exam`, `description`, `prix_unitaire`, `disponible`) VALUES
(6, 'ECBU', 'Examen cytobacteriologique des urines', 6000, 1),
(6, 'Hemocultures', 'Recherche de bacteries dans le sang', 10000, 1),
(6, 'Coproculture', 'Examen bacteriologique des selles', 8000, 1),
(6, 'Serologie VIH', 'Depistage du virus VIH', 8000, 1),
(6, 'Serologie Hepatite B (AgHBs)', 'Depistage de l hepatite B', 7000, 1),
(6, 'Serologie Hepatite C', 'Depistage de l hepatite C', 7000, 1),
(6, 'Serologie Lyme', 'Recherche de la maladie de Lyme', 9000, 0),
(6, 'Prelevement de gorge', 'Recherche de streptocoque', 5000, 1),
(6, 'PCR virales', 'Detection moleculaire de virus', 15000, 0);

-- Endocrinologie (id_specialite = 7)
INSERT INTO `examens` (`id_specialite`, `nom_exam`, `description`, `prix_unitaire`, `disponible`) VALUES
(7, 'TSH', 'Hormone thyreostimulante', 6000, 1),
(7, 'Cortisol', 'Dosage du cortisol sanguin', 8000, 1),
(7, 'FSH', 'Hormone folliculostimulante', 7000, 1),
(7, 'LH', 'Hormone luteinisante', 7000, 1),
(7, 'Testosterone', 'Dosage de la testosterone', 8000, 1),
(7, 'Estradiol', 'Dosage de l estradiol', 7000, 1),
(7, 'Beta-HCG', 'Test de grossesse sanguin', 5000, 1);

-- Marqueurs Tumoraux (id_specialite = 8)
INSERT INTO `examens` (`id_specialite`, `nom_exam`, `description`, `prix_unitaire`, `disponible`) VALUES
(8, 'PSA (Antigene prostatique)', 'Marqueur du cancer de la prostate', 8000, 1),
(8, 'CA-125', 'Marqueur du cancer de l ovaire', 10000, 1),
(8, 'ACE (Antigene carcino-embryonnaire)', 'Marqueur tumoral digestif', 9000, 1);

-- Immunologie (id_specialite = 9)
INSERT INTO `examens` (`id_specialite`, `nom_exam`, `description`, `prix_unitaire`, `disponible`) VALUES
(9, 'Facteur rhumatoide', 'Marqueur de polyarthrite rhumatoide', 7000, 1),
(9, 'Anticorps antinucleaires (AAN)', 'Recherche de maladies auto-immunes', 12000, 0);

-- Radiologie Standard (id_specialite = 10)
INSERT INTO `examens` (`id_specialite`, `nom_exam`, `description`, `prix_unitaire`, `disponible`, `duree_estimee_minutes`) VALUES
(10, 'Radiographie thoracique', 'Radio des poumons face et profil', 15000, 1, 15),
(10, 'Radiographie osseuse', 'Radio des os (membre, articulation)', 12000, 1, 15),
(10, 'Radiographie du rachis', 'Radio de la colonne vertebrale', 15000, 1, 20),
(10, 'Radiographie abdominale (ASP)', 'Abdomen sans preparation', 12000, 1, 15);

-- Echographie (id_specialite = 11)
INSERT INTO `examens` (`id_specialite`, `nom_exam`, `description`, `prix_unitaire`, `disponible`, `duree_estimee_minutes`) VALUES
(11, 'Echographie abdominale', 'Echo de l abdomen complet', 25000, 1, 30),
(11, 'Echographie pelvienne', 'Echo du petit bassin', 20000, 1, 25),
(11, 'Echographie thyroidienne', 'Echo de la thyroide', 18000, 1, 20),
(11, 'Echo-doppler veineux', 'Doppler des membres inferieurs', 30000, 1, 30),
(11, 'Echo-doppler arteriel', 'Doppler arteriel des membres', 30000, 1, 30);

-- IRM (id_specialite = 12)
INSERT INTO `examens` (`id_specialite`, `nom_exam`, `description`, `prix_unitaire`, `disponible`, `duree_estimee_minutes`) VALUES
(12, 'IRM cerebrale', 'Imagerie par resonance magnetique du cerveau', 80000, 0, 45),
(12, 'IRM rachidienne lombaire', 'IRM de la colonne lombaire', 75000, 0, 40),
(12, 'IRM rachidienne cervicale', 'IRM de la colonne cervicale', 75000, 0, 40),
(12, 'IRM articulaire', 'IRM d une articulation (genou, epaule, etc.)', 70000, 0, 35);

-- Imagerie Specialisee (id_specialite = 13)
INSERT INTO `examens` (`id_specialite`, `nom_exam`, `description`, `prix_unitaire`, `disponible`, `duree_estimee_minutes`) VALUES
(13, 'Mammographie', 'Radiographie des seins', 25000, 1, 20),
(13, 'Osteodensitometrie', 'Mesure de la densite osseuse', 35000, 0, 25);

-- Cardiologie (id_specialite = 14)
INSERT INTO `examens` (`id_specialite`, `nom_exam`, `description`, `prix_unitaire`, `disponible`, `duree_estimee_minutes`) VALUES
(14, 'ECG', 'Electrocardiogramme', 10000, 1, 15);

-- NOTE: orientation_specialiste a été supprimée et remplacée par orientation_pre_consultation (voir plus haut)

-- --------------------------------------------------------
-- Table: bulletin_examen
-- --------------------------------------------------------

CREATE TABLE `bulletin_examen` (
  `id_bull_exam` INT NOT NULL AUTO_INCREMENT,
  `date_demande` DATE NOT NULL,
  `id_labo` INT DEFAULT NULL,
  `id_consultation` INT DEFAULT NULL,
  `id_hospitalisation` INT DEFAULT NULL COMMENT 'Hospitalisation ayant genere cet examen',
  `instructions` TEXT DEFAULT NULL,
  `id_exam` INT DEFAULT NULL,
  `urgence` TINYINT(1) DEFAULT 0 COMMENT 'Examen urgent',
  `statut` VARCHAR(20) DEFAULT 'prescrit' COMMENT 'prescrit, en_cours, termine, annule',
  `date_realisation` DATETIME DEFAULT NULL COMMENT 'Date de realisation',
  `resultat_texte` TEXT DEFAULT NULL COMMENT 'Resultat textuel',
  `resultat_fichier` VARCHAR(500) DEFAULT NULL COMMENT 'Chemin fichier resultat (legacy)',
  `document_resultat_uuid` CHAR(36) DEFAULT NULL COMMENT 'UUID du document resultat dans documents_medicaux',
  `id_biologiste` INT DEFAULT NULL COMMENT 'Laborantin validateur',
  `date_resultat` DATETIME DEFAULT NULL COMMENT 'Date saisie resultat',
  `commentaire_labo` TEXT DEFAULT NULL COMMENT 'Commentaire laboratoire',
  PRIMARY KEY (`id_bull_exam`),
  KEY `id_labo` (`id_labo`),
  KEY `id_consultation` (`id_consultation`),
  KEY `fk_bulletin_hospitalisation` (`id_hospitalisation`),
  KEY `fk_bulletin_examen` (`id_exam`),
  KEY `idx_bulletin_statut` (`statut`),
  KEY `idx_bulletin_document_uuid` (`document_resultat_uuid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: fichier
-- --------------------------------------------------------

CREATE TABLE `fichier` (
  `id_fichier` INT NOT NULL AUTO_INCREMENT,
  `nom_fichier` VARCHAR(200) NOT NULL,
  `auteur` VARCHAR(150) DEFAULT NULL,
  `date_heure` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_fichier`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: result_exam
-- --------------------------------------------------------

CREATE TABLE `result_exam` (
  `id_result` INT NOT NULL AUTO_INCREMENT,
  `date_heure` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `resultat_ecrit` TEXT DEFAULT NULL,
  `id_exam` INT NOT NULL,
  `id_fichier` INT DEFAULT NULL,
  PRIMARY KEY (`id_result`),
  KEY `id_exam` (`id_exam`),
  KEY `id_fichier` (`id_fichier`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: facture
-- --------------------------------------------------------

CREATE TABLE `facture` (
  `id_facture` INT NOT NULL AUTO_INCREMENT,
  `date_heure` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `Total` DECIMAL(10,2) DEFAULT NULL,
  `assurance` DECIMAL(10,2) DEFAULT NULL,
  `net_a_payer` DECIMAL(10,2) DEFAULT NULL,
  `statut` VARCHAR(50) DEFAULT NULL,
  `id_patient` INT NOT NULL,
  `id_caissier` INT DEFAULT NULL,
  `id_hospit` INT DEFAULT NULL,
  `contenue` VARCHAR(250) DEFAULT NULL,
  `numero_facture` VARCHAR(30) DEFAULT NULL,
  `id_medecin` INT DEFAULT NULL,
  `id_service` INT DEFAULT NULL,
  `id_specialite` INT DEFAULT NULL,
  `id_consultation` INT DEFAULT NULL,
  `montant_total` DECIMAL(12,2) DEFAULT 0,
  `montant_paye` DECIMAL(12,2) DEFAULT 0,
  `montant_restant` DECIMAL(12,2) DEFAULT 0,
  `type_facture` VARCHAR(50) DEFAULT NULL,
  `date_creation` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `date_echeance` DATETIME DEFAULT NULL,
  `date_paiement` DATETIME DEFAULT NULL,
  `notes` VARCHAR(500) DEFAULT NULL,
  `couverture_assurance` TINYINT(1) DEFAULT 0,
  `id_assurance` INT DEFAULT NULL,
  `taux_couverture` DECIMAL(5,2) DEFAULT NULL,
  `montant_assurance` DECIMAL(12,2) DEFAULT NULL,
  `montant_patient` DECIMAL(12,2) DEFAULT 0 COMMENT 'Montant a payer par le patient',
  `date_envoi_assurance` DATETIME DEFAULT NULL COMMENT 'Date envoi facture a assurance',
  PRIMARY KEY (`id_facture`),
  KEY `IX_facture_statut` (`statut`),
  KEY `IX_facture_assurance` (`id_assurance`),
  KEY `id_patient` (`id_patient`),
  KEY `id_caissier` (`id_caissier`),
  KEY `id_hospit` (`id_hospit`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: facture_item
-- --------------------------------------------------------

CREATE TABLE `facture_item` (
  `id_item` INT NOT NULL AUTO_INCREMENT,
  `id_facture` INT NOT NULL,
  `description` VARCHAR(255) NOT NULL,
  `prix_unitaire` DECIMAL(10,2) NOT NULL,
  `quantite` INT NOT NULL DEFAULT 1,
  `total_ligne` DECIMAL(10,2) GENERATED ALWAYS AS (`prix_unitaire` * `quantite`) STORED,
  `id_consultation` INT DEFAULT NULL,
  `id_medicament` INT DEFAULT NULL,
  `id_hospitalisation` INT DEFAULT NULL,
  `id_examen` INT DEFAULT NULL,
  `type_service` ENUM('consultation','medicament','hospitalisation','examen') DEFAULT NULL,
  PRIMARY KEY (`id_item`),
  KEY `fk_facture_item_facture` (`id_facture`),
  KEY `fk_item_consultation` (`id_consultation`),
  KEY `fk_item_medicament` (`id_medicament`),
  KEY `fk_item_hospitalisation` (`id_hospitalisation`),
  KEY `fk_item_examen` (`id_examen`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: fournisseur
-- --------------------------------------------------------

CREATE TABLE `fournisseur` (
  `id_fournisseur` INT NOT NULL AUTO_INCREMENT,
  `nom_fournisseur` VARCHAR(150) NOT NULL,
  `contact_nom` VARCHAR(100) DEFAULT NULL,
  `contact_email` VARCHAR(120) DEFAULT NULL,
  `contact_telephone` VARCHAR(20) DEFAULT NULL,
  `adresse` TEXT DEFAULT NULL,
  `conditions_paiement` VARCHAR(100) DEFAULT NULL,
  `delai_livraison_jours` INT DEFAULT 7,
  `actif` TINYINT(1) DEFAULT 1,
  `date_creation` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_fournisseur`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: commande_pharmacie
-- --------------------------------------------------------

CREATE TABLE `commande_pharmacie` (
  `id_commande` INT NOT NULL AUTO_INCREMENT,
  `id_fournisseur` INT NOT NULL,
  `date_commande` DATE NOT NULL,
  `date_reception_prevue` DATE DEFAULT NULL,
  `date_reception_reelle` DATE DEFAULT NULL,
  `statut` ENUM('brouillon','envoyée','partiellement_reçue','reçue','annulée') DEFAULT 'brouillon',
  `montant_total` DECIMAL(10,2) DEFAULT 0.00,
  `id_user` INT NOT NULL,
  `notes` TEXT DEFAULT NULL,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_commande`),
  KEY `id_fournisseur` (`id_fournisseur`),
  KEY `id_user` (`id_user`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: commande_ligne
-- --------------------------------------------------------

CREATE TABLE `commande_ligne` (
  `id_ligne_commande` INT NOT NULL AUTO_INCREMENT,
  `id_commande` INT NOT NULL,
  `id_medicament` INT NOT NULL,
  `quantite_commandee` INT NOT NULL,
  `quantite_recue` INT DEFAULT 0,
  `prix_achat` DECIMAL(8,2) NOT NULL,
  `date_peremption` DATE DEFAULT NULL,
  `numero_lot` VARCHAR(100) DEFAULT NULL,
  PRIMARY KEY (`id_ligne_commande`),
  KEY `id_commande` (`id_commande`),
  KEY `id_medicament` (`id_medicament`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: mouvement_stock
-- --------------------------------------------------------

CREATE TABLE `mouvement_stock` (
  `id_mouvement` INT NOT NULL AUTO_INCREMENT,
  `id_medicament` INT NOT NULL,
  `type_mouvement` ENUM('entree','sortie','ajustement','perte','retour') NOT NULL,
  `quantite` INT NOT NULL,
  `date_mouvement` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `motif` VARCHAR(255) DEFAULT NULL,
  `reference_id` INT DEFAULT NULL,
  `reference_type` ENUM('commande','prescription','inventaire','ajustement') DEFAULT NULL,
  `id_user` INT NOT NULL,
  `stock_apres_mouvement` INT NOT NULL,
  PRIMARY KEY (`id_mouvement`),
  KEY `id_medicament` (`id_medicament`),
  KEY `id_user` (`id_user`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: inventaire
-- --------------------------------------------------------

CREATE TABLE `inventaire` (
  `id_inventaire` INT NOT NULL AUTO_INCREMENT,
  `date_inventaire` DATE NOT NULL,
  `statut` ENUM('planifié','en_cours','terminé','annulé') DEFAULT 'planifié',
  `id_user_responsable` INT NOT NULL,
  `notes` TEXT DEFAULT NULL,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_inventaire`),
  KEY `id_user_responsable` (`id_user_responsable`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: inventaire_ligne
-- --------------------------------------------------------

CREATE TABLE `inventaire_ligne` (
  `id_ligne_inventaire` INT NOT NULL AUTO_INCREMENT,
  `id_inventaire` INT NOT NULL,
  `id_medicament` INT NOT NULL,
  `quantite_theorique` INT NOT NULL,
  `quantite_reelle` INT NOT NULL,
  `ecart` INT GENERATED ALWAYS AS (`quantite_reelle` - `quantite_theorique`) STORED,
  `commentaire` VARCHAR(255) DEFAULT NULL,
  PRIMARY KEY (`id_ligne_inventaire`),
  UNIQUE KEY `unique_medicament_inventaire` (`id_inventaire`, `id_medicament`),
  KEY `id_medicament` (`id_medicament`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: notifications
-- --------------------------------------------------------

CREATE TABLE `notifications` (
  `id_notification` INT NOT NULL AUTO_INCREMENT,
  `nom_notification` VARCHAR(100) DEFAULT NULL,
  `contenu` TEXT NOT NULL,
  `id_user` INT NOT NULL,
  `date_heure` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_notification`),
  KEY `id_user` (`id_user`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: pharmacie
-- --------------------------------------------------------

CREATE TABLE `pharmacie` (
  `id_pharm` INT NOT NULL AUTO_INCREMENT,
  `nom_pharmacie` VARCHAR(150) NOT NULL,
  PRIMARY KEY (`id_pharm`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: pharmacie_externe
-- --------------------------------------------------------

CREATE TABLE `pharmacie_externe` (
  `id_pharmacie` INT NOT NULL AUTO_INCREMENT,
  `nom` VARCHAR(200) NOT NULL,
  `adresse` TEXT DEFAULT NULL,
  `telephone` VARCHAR(20) DEFAULT NULL,
  `email` VARCHAR(120) DEFAULT NULL,
  `horaires` VARCHAR(200) DEFAULT NULL,
  `actif` TINYINT(1) DEFAULT 1,
  PRIMARY KEY (`id_pharmacie`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: ordonnance_electronique
-- --------------------------------------------------------

CREATE TABLE `ordonnance_electronique` (
  `id_ordonnance` INT NOT NULL AUTO_INCREMENT,
  `id_consultation` INT DEFAULT NULL,
  `id_medecin` INT NOT NULL,
  `id_patient` INT NOT NULL,
  `numero_ordonnance` VARCHAR(50) DEFAULT NULL,
  `date_creation` TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
  `date_validite` DATE DEFAULT NULL,
  `statut` VARCHAR(50) DEFAULT 'active',
  `signature_electronique` TEXT DEFAULT NULL,
  `notes` TEXT DEFAULT NULL,
  PRIMARY KEY (`id_ordonnance`),
  KEY `fk_oe_medecin` (`id_medecin`),
  KEY `fk_oe_patient` (`id_patient`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: dispensation
-- --------------------------------------------------------

CREATE TABLE `dispensation` (
  `id_dispensation` INT NOT NULL AUTO_INCREMENT,
  `id_prescription` INT DEFAULT NULL,
  `id_patient` INT NOT NULL,
  `id_pharmacien` INT DEFAULT NULL,
  `date_dispensation` TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
  `statut` VARCHAR(50) DEFAULT 'en_cours',
  `notes` TEXT DEFAULT NULL,
  PRIMARY KEY (`id_dispensation`),
  KEY `fk_disp_prescription` (`id_prescription`),
  KEY `fk_disp_patient` (`id_patient`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: dispensation_ligne
-- --------------------------------------------------------

CREATE TABLE `dispensation_ligne` (
  `id_ligne` INT NOT NULL AUTO_INCREMENT,
  `id_dispensation` INT NOT NULL,
  `id_medicament` INT NOT NULL,
  `quantite_prescrite` INT DEFAULT NULL,
  `quantite_dispensee` INT NOT NULL,
  `prix_unitaire` DECIMAL(12,2) DEFAULT NULL,
  PRIMARY KEY (`id_ligne`),
  KEY `fk_dl_dispensation` (`id_dispensation`),
  KEY `fk_dl_medicament` (`id_medicament`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: contre_indication
-- --------------------------------------------------------

CREATE TABLE `contre_indication` (
  `id_contre_indication` INT NOT NULL AUTO_INCREMENT,
  `id_medicament` INT NOT NULL,
  `condition_medicale` VARCHAR(200) NOT NULL,
  `severite` VARCHAR(50) DEFAULT 'moderee',
  `description` TEXT DEFAULT NULL,
  PRIMARY KEY (`id_contre_indication`),
  KEY `fk_ci_medicament` (`id_medicament`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: interaction_medicamenteuse
-- --------------------------------------------------------

CREATE TABLE `interaction_medicamenteuse` (
  `id_interaction` INT NOT NULL AUTO_INCREMENT,
  `id_medicament_1` INT NOT NULL,
  `id_medicament_2` INT NOT NULL,
  `severite` VARCHAR(50) DEFAULT 'moderee',
  `description` TEXT DEFAULT NULL,
  `recommandation` TEXT DEFAULT NULL,
  PRIMARY KEY (`id_interaction`),
  KEY `fk_inter_med1` (`id_medicament_1`),
  KEY `fk_inter_med2` (`id_medicament_2`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: allergie_patient
-- --------------------------------------------------------

CREATE TABLE `allergie_patient` (
  `id_allergie` INT NOT NULL AUTO_INCREMENT,
  `id_patient` INT NOT NULL,
  `allergene` VARCHAR(200) NOT NULL,
  `severite` VARCHAR(50) DEFAULT 'moderee',
  `reactions` TEXT DEFAULT NULL,
  `date_diagnostic` DATE DEFAULT NULL,
  `notes` TEXT DEFAULT NULL,
  `actif` TINYINT(1) DEFAULT 1,
  PRIMARY KEY (`id_allergie`),
  KEY `fk_allergie_patient` (`id_patient`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: alerte_medicale
-- --------------------------------------------------------

CREATE TABLE `alerte_medicale` (
  `id_alerte` INT NOT NULL AUTO_INCREMENT,
  `id_patient` INT NOT NULL,
  `type` VARCHAR(50) NOT NULL,
  `message` TEXT NOT NULL,
  `severite` VARCHAR(50) DEFAULT 'info',
  `id_source` INT DEFAULT NULL,
  `type_source` VARCHAR(50) DEFAULT NULL,
  `lu` TINYINT(1) DEFAULT 0,
  `date_creation` TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_alerte`),
  KEY `fk_alerte_patient` (`id_patient`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: dossier_medical_partage (DMP)
-- --------------------------------------------------------

CREATE TABLE `dossier_medical_partage` (
  `id_dmp` INT NOT NULL AUTO_INCREMENT,
  `id_patient` INT NOT NULL,
  `numero_dmp` VARCHAR(50) DEFAULT NULL,
  `date_creation` TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
  `statut` VARCHAR(50) DEFAULT 'actif',
  PRIMARY KEY (`id_dmp`),
  UNIQUE KEY `UK_dmp_patient` (`id_patient`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: document_dmp
-- --------------------------------------------------------

CREATE TABLE `document_dmp` (
  `id_document` INT NOT NULL AUTO_INCREMENT,
  `id_dmp` INT NOT NULL,
  `type_document` VARCHAR(100) DEFAULT NULL,
  `titre` VARCHAR(200) NOT NULL,
  `description` TEXT DEFAULT NULL,
  `chemin_fichier` VARCHAR(500) DEFAULT NULL COMMENT 'Chemin fichier (legacy)',
  `document_uuid` CHAR(36) DEFAULT NULL COMMENT 'UUID du document dans documents_medicaux',
  `taille_fichier` INT DEFAULT NULL,
  `mime_type` VARCHAR(100) DEFAULT NULL,
  `date_document` DATE DEFAULT NULL,
  `date_creation` TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
  `id_createur` INT DEFAULT NULL,
  `hash_fichier` VARCHAR(255) DEFAULT NULL,
  PRIMARY KEY (`id_document`),
  KEY `fk_doc_dmp` (`id_dmp`),
  KEY `fk_doc_createur` (`id_createur`),
  KEY `idx_dmp_document_uuid` (`document_uuid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: acces_dmp
-- --------------------------------------------------------

CREATE TABLE `acces_dmp` (
  `id_acces` INT NOT NULL AUTO_INCREMENT,
  `id_dmp` INT NOT NULL,
  `id_professionnel` INT NOT NULL,
  `type_acces` VARCHAR(50) DEFAULT NULL,
  `date_acces` TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
  `motif` TEXT DEFAULT NULL,
  PRIMARY KEY (`id_acces`),
  KEY `fk_acces_dmp` (`id_dmp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: autorisation_dmp
-- --------------------------------------------------------

CREATE TABLE `autorisation_dmp` (
  `id_autorisation` INT NOT NULL AUTO_INCREMENT,
  `id_dmp` INT NOT NULL,
  `id_professionnel` INT NOT NULL,
  `type_acces` VARCHAR(50) DEFAULT 'lecture',
  `date_debut` TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
  `date_fin` TIMESTAMP NULL DEFAULT NULL,
  `actif` TINYINT(1) DEFAULT 1,
  PRIMARY KEY (`id_autorisation`),
  KEY `fk_auth_dmp` (`id_dmp`),
  KEY `fk_auth_pro` (`id_professionnel`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: reservation_lit
-- --------------------------------------------------------

CREATE TABLE `reservation_lit` (
  `id_reservation` INT NOT NULL AUTO_INCREMENT,
  `id_lit` INT NOT NULL,
  `id_patient` INT NOT NULL,
  `date_debut` DATETIME NOT NULL,
  `date_fin` DATETIME DEFAULT NULL,
  `statut` VARCHAR(50) DEFAULT 'confirmee',
  `notes` TEXT DEFAULT NULL,
  PRIMARY KEY (`id_reservation`),
  KEY `fk_res_lit` (`id_lit`),
  KEY `fk_res_patient` (`id_patient`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: transfert_lit
-- --------------------------------------------------------

CREATE TABLE `transfert_lit` (
  `id_transfert` INT NOT NULL AUTO_INCREMENT,
  `id_patient` INT NOT NULL,
  `id_lit_source` INT NOT NULL,
  `id_lit_destination` INT NOT NULL,
  `date_transfert` TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
  `motif` TEXT DEFAULT NULL,
  `id_demandeur` INT DEFAULT NULL,
  PRIMARY KEY (`id_transfert`),
  KEY `fk_tr_patient` (`id_patient`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: maintenance_lit
-- --------------------------------------------------------

CREATE TABLE `maintenance_lit` (
  `id_maintenance` INT NOT NULL AUTO_INCREMENT,
  `id_lit` INT NOT NULL,
  `type_maintenance` VARCHAR(100) DEFAULT NULL,
  `description` TEXT DEFAULT NULL,
  `date_debut` DATETIME NOT NULL,
  `date_fin` DATETIME DEFAULT NULL,
  `statut` VARCHAR(50) DEFAULT 'en_cours',
  PRIMARY KEY (`id_maintenance`),
  KEY `fk_maint_lit` (`id_lit`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: echeancier
-- --------------------------------------------------------

CREATE TABLE `echeancier` (
  `id_echeancier` INT NOT NULL AUTO_INCREMENT,
  `id_facture` INT NOT NULL,
  `montant_total` DECIMAL(12,2) NOT NULL,
  `nombre_echeances` INT NOT NULL,
  `montant_par_echeance` DECIMAL(12,2) NOT NULL DEFAULT 0,
  `date_debut` DATE NOT NULL,
  `frequence` VARCHAR(20) NOT NULL DEFAULT 'mensuel',
  `date_creation` TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
  `statut` VARCHAR(50) DEFAULT 'actif',
  `cree_par` INT DEFAULT NULL,
  PRIMARY KEY (`id_echeancier`),
  KEY `fk_ech_facture` (`id_facture`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: echeance
-- --------------------------------------------------------

CREATE TABLE `echeance` (
  `id_echeance` INT NOT NULL AUTO_INCREMENT,
  `id_echeancier` INT NOT NULL,
  `numero` INT NOT NULL,
  `montant` DECIMAL(12,2) NOT NULL,
  `date_echeance` DATE NOT NULL,
  `date_paiement` TIMESTAMP NULL DEFAULT NULL,
  `statut` VARCHAR(50) DEFAULT 'en_attente',
  `id_transaction` INT DEFAULT NULL,
  `notes` VARCHAR(500) DEFAULT NULL,
  PRIMARY KEY (`id_echeance`),
  KEY `fk_echeance_echeancier` (`id_echeancier`),
  KEY `fk_echeance_transaction` (`id_transaction`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: demande_remboursement
-- --------------------------------------------------------

CREATE TABLE `demande_remboursement` (
  `id_demande` INT NOT NULL AUTO_INCREMENT,
  `numero_demande` VARCHAR(50) NOT NULL DEFAULT '',
  `id_facture` INT NOT NULL,
  `id_assurance` INT NOT NULL,
  `id_patient` INT NOT NULL,
  `montant_demande` DECIMAL(12,2) NOT NULL,
  `montant_approuve` DECIMAL(12,2) DEFAULT NULL,
  `montant_rembourse` DECIMAL(12,2) DEFAULT NULL,
  `date_demande` TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
  `date_reponse` TIMESTAMP NULL DEFAULT NULL,
  `date_traitement` DATETIME DEFAULT NULL,
  `statut` VARCHAR(50) DEFAULT 'en_attente',
  `motif_rejet` VARCHAR(500) DEFAULT NULL,
  `justificatif` VARCHAR(500) DEFAULT NULL,
  `traite_par` INT DEFAULT NULL,
  `reference` VARCHAR(100) DEFAULT NULL,
  PRIMARY KEY (`id_demande`),
  KEY `fk_dr_facture` (`id_facture`),
  KEY `fk_dr_assurance` (`id_assurance`),
  KEY `fk_dr_patient` (`id_patient`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: acces_verification
-- --------------------------------------------------------

CREATE TABLE `acces_verification` (
  `id_verification` INT NOT NULL AUTO_INCREMENT,
  `id_patient` INT NOT NULL,
  `id_utilisateur` INT NOT NULL,
  `code` VARCHAR(10) NOT NULL,
  `methode_envoi` ENUM('sms','email','both') NOT NULL,
  `date_creation` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `date_expiration` DATETIME NOT NULL,
  `statut` ENUM('en_attente','valide','expire','annule') DEFAULT 'en_attente',
  `token_session` VARCHAR(100) DEFAULT NULL,
  PRIMARY KEY (`id_verification`),
  KEY `id_patient` (`id_patient`),
  KEY `id_utilisateur` (`id_utilisateur`),
  KEY `idx_code_verification` (`code`, `statut`),
  KEY `idx_expiration` (`date_expiration`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: historique_acces_dossier
-- --------------------------------------------------------

CREATE TABLE `historique_acces_dossier` (
  `id_acces` INT NOT NULL AUTO_INCREMENT,
  `id_patient` INT NOT NULL,
  `id_utilisateur` INT NOT NULL,
  `date_acces` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `type_acces` ENUM('premier_acces','acces_verifie','acces_refuse') DEFAULT NULL,
  `ip_adresse` VARCHAR(45) DEFAULT NULL,
  `user_agent` TEXT DEFAULT NULL,
  PRIMARY KEY (`id_acces`),
  KEY `id_patient` (`id_patient`),
  KEY `id_utilisateur` (`id_utilisateur`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: documents_medicaux (Stockage centralisé avec UUID)
-- --------------------------------------------------------

CREATE TABLE `documents_medicaux` (
  `uuid` CHAR(36) NOT NULL COMMENT 'Identifiant unique UUID',
  `nom_fichier_original` VARCHAR(255) NOT NULL COMMENT 'Nom original du fichier uploade',
  `nom_fichier_stockage` VARCHAR(255) NOT NULL COMMENT 'Nom du fichier sur le disque (UUID.extension)',
  `chemin_relatif` VARCHAR(500) NOT NULL COMMENT 'Chemin relatif depuis la racine de stockage',
  `extension` VARCHAR(20) DEFAULT NULL COMMENT 'Extension du fichier (.pdf, .jpg, etc.)',
  `mime_type` VARCHAR(100) NOT NULL COMMENT 'Type MIME du fichier',
  `taille_octets` BIGINT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Taille en octets',
  `hash_sha256` CHAR(64) DEFAULT NULL COMMENT 'Hash SHA-256 du contenu pour verification integrite',
  `hash_calcule_at` TIMESTAMP NULL DEFAULT NULL COMMENT 'Date du dernier calcul de hash',
  `est_chiffre` BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Indique si le fichier est chiffre',
  `encryption_iv` VARBINARY(16) DEFAULT NULL COMMENT 'Vecteur d initialisation pour le chiffrement AES-GCM',
  `encryption_tag` VARBINARY(16) DEFAULT NULL COMMENT 'Tag d authentification GCM',
  `statut_integrite` ENUM('ok', 'hash_invalide', 'fichier_absent', 'erreur_lecture', 'non_verifie') NOT NULL DEFAULT 'non_verifie' COMMENT 'Statut de la derniere verification d integrite',
  `date_derniere_verification` TIMESTAMP NULL DEFAULT NULL COMMENT 'Date de la derniere verification d integrite',
  `type_document` ENUM(
    'resultat_examen',
    'imagerie_medicale',
    'compte_rendu_operatoire',
    'compte_rendu_hospitalisation',
    'ordonnance',
    'certificat_medical',
    'lettre_sortie',
    'consentement',
    'document_administratif',
    'document_externe',
    'autre'
  ) NOT NULL DEFAULT 'autre' COMMENT 'Type de document medical',
  `sous_type` VARCHAR(100) DEFAULT NULL COMMENT 'Sous-type specifique (ex: radiographie, IRM, etc.)',
  `niveau_confidentialite` ENUM('normal', 'sensible', 'tres_sensible') NOT NULL DEFAULT 'normal',
  `acces_patient` BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Le patient peut-il voir ce document?',
  `acces_restreint_roles` JSON DEFAULT NULL COMMENT 'Roles autorises si acces restreint',
  `id_patient` INT NOT NULL COMMENT 'Patient proprietaire du document',
  `id_consultation` INT DEFAULT NULL COMMENT 'Consultation associee',
  `id_bulletin_examen` INT DEFAULT NULL COMMENT 'Bulletin d examen associe',
  `id_hospitalisation` INT DEFAULT NULL COMMENT 'Hospitalisation associee',
  `id_dmp` INT DEFAULT NULL COMMENT 'DMP associe',
  `id_createur` INT NOT NULL COMMENT 'Utilisateur ayant uploade le document',
  `id_validateur` INT DEFAULT NULL COMMENT 'Utilisateur ayant valide le document',
  `date_validation` TIMESTAMP NULL DEFAULT NULL,
  `version` INT UNSIGNED NOT NULL DEFAULT 1 COMMENT 'Numero de version',
  `uuid_version_precedente` CHAR(36) DEFAULT NULL COMMENT 'UUID de la version precedente',
  `est_version_courante` BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Est-ce la version active?',
  `date_document` DATE DEFAULT NULL COMMENT 'Date du document (peut differer de la date upload)',
  `description` TEXT DEFAULT NULL COMMENT 'Description libre du document',
  `tags` JSON DEFAULT NULL COMMENT 'Tags pour recherche',
  `statut` ENUM('actif', 'archive', 'supprime', 'quarantaine') NOT NULL DEFAULT 'actif',
  `date_archivage` TIMESTAMP NULL DEFAULT NULL,
  `motif_archivage` VARCHAR(500) DEFAULT NULL,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`uuid`),
  INDEX `idx_documents_patient` (`id_patient`),
  INDEX `idx_documents_type` (`type_document`),
  INDEX `idx_documents_consultation` (`id_consultation`),
  INDEX `idx_documents_bulletin` (`id_bulletin_examen`),
  INDEX `idx_documents_hospitalisation` (`id_hospitalisation`),
  INDEX `idx_documents_dmp` (`id_dmp`),
  INDEX `idx_documents_statut` (`statut`),
  INDEX `idx_documents_createur` (`id_createur`),
  INDEX `idx_documents_date` (`date_document`),
  INDEX `idx_documents_hash` (`hash_sha256`),
  INDEX `idx_documents_version_courante` (`est_version_courante`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Table centralisee des documents medicaux avec UUID et integrite';

-- --------------------------------------------------------
-- Table: audit_acces_documents (Tracabilite des acces)
-- --------------------------------------------------------

CREATE TABLE `audit_acces_documents` (
  `id_audit` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
  `document_uuid` CHAR(36) NOT NULL COMMENT 'UUID du document accede',
  `id_utilisateur` INT NOT NULL COMMENT 'Utilisateur ayant effectue l action',
  `role_utilisateur` VARCHAR(50) NOT NULL COMMENT 'Role au moment de l acces',
  `type_action` ENUM(
    'consultation',
    'telechargement',
    'impression',
    'creation',
    'modification',
    'suppression',
    'restauration',
    'archivage',
    'partage',
    'verification',
    'tentative_non_autorisee'
  ) NOT NULL,
  `autorise` BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'L acces a-t-il ete autorise?',
  `motif_refus` VARCHAR(255) DEFAULT NULL COMMENT 'Raison du refus si non autorise',
  `ip_address` VARCHAR(45) DEFAULT NULL COMMENT 'Adresse IP (IPv4 ou IPv6)',
  `user_agent` VARCHAR(500) DEFAULT NULL COMMENT 'User-Agent du navigateur',
  `session_id` VARCHAR(100) DEFAULT NULL COMMENT 'ID de session',
  `endpoint_api` VARCHAR(255) DEFAULT NULL COMMENT 'Endpoint API appele',
  `contexte` JSON DEFAULT NULL COMMENT 'Contexte additionnel',
  `timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX `idx_audit_document` (`document_uuid`),
  INDEX `idx_audit_utilisateur` (`id_utilisateur`),
  INDEX `idx_audit_action` (`type_action`),
  INDEX `idx_audit_timestamp` (`timestamp`),
  INDEX `idx_audit_autorise` (`autorise`),
  INDEX `idx_audit_ip` (`ip_address`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Journal d audit des acces aux documents medicaux';

-- --------------------------------------------------------
-- Table: verification_integrite (Historique des controles)
-- --------------------------------------------------------

CREATE TABLE `verification_integrite` (
  `id_verification` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
  `document_uuid` CHAR(36) NOT NULL COMMENT 'UUID du document verifie',
  `statut_verification` ENUM(
    'ok',
    'hash_invalide',
    'fichier_absent',
    'erreur_lecture',
    'hash_non_calcule'
  ) NOT NULL,
  `hash_attendu` CHAR(64) DEFAULT NULL COMMENT 'Hash SHA-256 attendu (stocke)',
  `hash_calcule` CHAR(64) DEFAULT NULL COMMENT 'Hash SHA-256 calcule lors de la verification',
  `taille_attendue` BIGINT UNSIGNED DEFAULT NULL,
  `taille_reelle` BIGINT UNSIGNED DEFAULT NULL,
  `type_verification` ENUM('automatique', 'manuelle', 'restauration') NOT NULL DEFAULT 'automatique',
  `id_declencheur` INT DEFAULT NULL COMMENT 'Utilisateur ayant declenche (si manuelle)',
  `action_corrective` VARCHAR(255) DEFAULT NULL COMMENT 'Action prise en cas de probleme',
  `alerte_envoyee` BOOLEAN NOT NULL DEFAULT FALSE,
  `date_alerte` TIMESTAMP NULL DEFAULT NULL,
  `timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX `idx_verif_document` (`document_uuid`),
  INDEX `idx_verif_statut` (`statut_verification`),
  INDEX `idx_verif_timestamp` (`timestamp`),
  INDEX `idx_verif_type` (`type_verification`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Historique des verifications d integrite des documents';

-- --------------------------------------------------------
-- Table: alertes_systeme (Monitoring et alertes)
-- --------------------------------------------------------

CREATE TABLE `alertes_systeme` (
  `id_alerte` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
  `type_alerte` ENUM('storage_health', 'disk_space', 'corruption', 'access_denied', 'suspicious_activity', 'backup_failed', 'system_error') NOT NULL,
  `message` TEXT NOT NULL,
  `severite` ENUM('info', 'warning', 'critical', 'emergency') NOT NULL DEFAULT 'warning',
  `source` VARCHAR(100) DEFAULT 'system' COMMENT 'Source de l alerte (script, service, etc.)',
  `details` JSON COMMENT 'Details supplementaires en JSON',
  `acquittee` BOOLEAN NOT NULL DEFAULT FALSE,
  `acquittee_par` INT UNSIGNED NULL,
  `date_acquittement` TIMESTAMP NULL,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX `idx_alertes_type` (`type_alerte`),
  INDEX `idx_alertes_severite` (`severite`),
  INDEX `idx_alertes_acquittee` (`acquittee`),
  INDEX `idx_alertes_created` (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Alertes systeme pour le monitoring du stockage et de la securite';

-- --------------------------------------------------------
-- Contraintes (Foreign Keys)
-- --------------------------------------------------------

ALTER TABLE `service`
  ADD CONSTRAINT `service_ibfk_1` FOREIGN KEY (`responsable_service`) REFERENCES `utilisateurs` (`id_user`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_service_major` FOREIGN KEY (`id_major`) REFERENCES `infirmier` (`id_user`) ON DELETE SET NULL;

ALTER TABLE `patient`
  ADD CONSTRAINT `fk_patient_assurance` FOREIGN KEY (`id_assurance`) REFERENCES `assurances` (`id_assurance`) ON DELETE SET NULL,
  ADD CONSTRAINT `patient_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `utilisateurs` (`id_user`) ON DELETE CASCADE;

ALTER TABLE `medecin`
  ADD CONSTRAINT `fk_medecin_service` FOREIGN KEY (`id_service`) REFERENCES `service` (`id_service`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_medecin_specialite` FOREIGN KEY (`id_specialite`) REFERENCES `specialites` (`id_specialite`),
  ADD CONSTRAINT `medecin_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `utilisateurs` (`id_user`) ON DELETE CASCADE;

ALTER TABLE `infirmier`
  ADD CONSTRAINT `infirmier_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `utilisateurs` (`id_user`) ON DELETE CASCADE;

ALTER TABLE `administrateur`
  ADD CONSTRAINT `administrateur_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `utilisateurs` (`id_user`) ON DELETE CASCADE;

ALTER TABLE `caissier`
  ADD CONSTRAINT `caissier_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `utilisateurs` (`id_user`) ON DELETE CASCADE;

ALTER TABLE `accueil`
  ADD CONSTRAINT `accueil_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `utilisateurs` (`id_user`) ON DELETE CASCADE;

ALTER TABLE `pharmacien`
  ADD CONSTRAINT `pharmacien_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `utilisateurs` (`id_user`) ON DELETE CASCADE;

-- Note: La table laborantin a ses FK définies inline dans sa création

ALTER TABLE `lit`
  ADD CONSTRAINT `lit_ibfk_1` FOREIGN KEY (`id_chambre`) REFERENCES `chambre` (`id_chambre`);

ALTER TABLE `chambre`
  ADD CONSTRAINT `fk_chambre_standard` FOREIGN KEY (`id_standard`) REFERENCES `standard_chambre` (`id_standard`) ON DELETE SET NULL ON UPDATE CASCADE;

ALTER TABLE `consultation`
  ADD CONSTRAINT `consultation_ibfk_1` FOREIGN KEY (`id_medecin`) REFERENCES `medecin` (`id_user`),
  ADD CONSTRAINT `consultation_ibfk_2` FOREIGN KEY (`id_patient`) REFERENCES `patient` (`id_user`) ON DELETE CASCADE;

ALTER TABLE `hospitalisation`
  ADD CONSTRAINT `hospitalisation_ibfk_1` FOREIGN KEY (`id_patient`) REFERENCES `patient` (`id_user`) ON DELETE CASCADE,
  ADD CONSTRAINT `hospitalisation_ibfk_2` FOREIGN KEY (`id_lit`) REFERENCES `lit` (`id_lit`),
  ADD CONSTRAINT `fk_hospitalisation_medecin` FOREIGN KEY (`id_medecin`) REFERENCES `medecin` (`id_user`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_hospitalisation_service` FOREIGN KEY (`id_service`) REFERENCES `service` (`id_service`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_hospitalisation_consultation` FOREIGN KEY (`id_consultation`) REFERENCES `consultation` (`id_consultation`) ON DELETE SET NULL;

ALTER TABLE `soin_hospitalisation`
  ADD CONSTRAINT `fk_soin_hospitalisation` FOREIGN KEY (`id_hospitalisation`) REFERENCES `hospitalisation` (`id_admission`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_soin_prescripteur` FOREIGN KEY (`id_prescripteur`) REFERENCES `medecin` (`id_user`) ON DELETE SET NULL;

ALTER TABLE `ordonnance`
  ADD CONSTRAINT `ordonnance_ibfk_1` FOREIGN KEY (`id_consultation`) REFERENCES `consultation` (`id_consultation`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_ordonnance_patient` FOREIGN KEY (`id_patient`) REFERENCES `utilisateurs` (`id_user`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_ordonnance_medecin` FOREIGN KEY (`id_medecin`) REFERENCES `utilisateurs` (`id_user`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_ordonnance_hospitalisation` FOREIGN KEY (`id_hospitalisation`) REFERENCES `hospitalisation` (`id_admission`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_ordonnance_originale` FOREIGN KEY (`id_ordonnance_originale`) REFERENCES `ordonnance` (`id_ordonnance`) ON DELETE SET NULL;

ALTER TABLE `ordonnance_medicament`
  ADD CONSTRAINT `ordonnance_medicament_ibfk_1` FOREIGN KEY (`id_ordonnance`) REFERENCES `ordonnance` (`id_ordonnance`) ON DELETE CASCADE,
  ADD CONSTRAINT `ordonnance_medicament_ibfk_2` FOREIGN KEY (`id_medicament`) REFERENCES `medicament` (`id_medicament`) ON DELETE SET NULL;

ALTER TABLE `bulletin_examen`
  ADD CONSTRAINT `bulletin_examen_ibfk_1` FOREIGN KEY (`id_labo`) REFERENCES `laboratoire` (`id_labo`) ON DELETE SET NULL,
  ADD CONSTRAINT `bulletin_examen_ibfk_2` FOREIGN KEY (`id_consultation`) REFERENCES `consultation` (`id_consultation`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_bulletin_hospitalisation` FOREIGN KEY (`id_hospitalisation`) REFERENCES `hospitalisation` (`id_admission`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_bulletin_examen` FOREIGN KEY (`id_exam`) REFERENCES `examens` (`id_exam`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_bulletin_document_uuid` FOREIGN KEY (`document_resultat_uuid`) REFERENCES `documents_medicaux` (`uuid`) ON DELETE SET NULL ON UPDATE CASCADE;

ALTER TABLE `orientation_specialiste`
  ADD CONSTRAINT `fk_orientation_consultation` FOREIGN KEY (`id_consultation`) REFERENCES `consultation` (`id_consultation`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_orientation_specialite` FOREIGN KEY (`id_specialite`) REFERENCES `specialites` (`id_specialite`),
  ADD CONSTRAINT `fk_orientation_medecin` FOREIGN KEY (`id_medecin_oriente`) REFERENCES `medecin` (`id_user`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_orientation_rdv` FOREIGN KEY (`id_rdv_cree`) REFERENCES `rendez_vous` (`id_rdv`) ON DELETE SET NULL;

ALTER TABLE `result_exam`
  ADD CONSTRAINT `result_exam_ibfk_1` FOREIGN KEY (`id_exam`) REFERENCES `bulletin_examen` (`id_bull_exam`) ON DELETE CASCADE,
  ADD CONSTRAINT `result_exam_ibfk_2` FOREIGN KEY (`id_fichier`) REFERENCES `fichier` (`id_fichier`) ON DELETE SET NULL;

ALTER TABLE `facture`
  ADD CONSTRAINT `facture_ibfk_1` FOREIGN KEY (`id_patient`) REFERENCES `patient` (`id_user`) ON DELETE CASCADE,
  ADD CONSTRAINT `facture_ibfk_2` FOREIGN KEY (`id_caissier`) REFERENCES `caissier` (`id_user`) ON DELETE SET NULL,
  ADD CONSTRAINT `facture_ibfk_3` FOREIGN KEY (`id_hospit`) REFERENCES `hospitalisation` (`id_admission`) ON DELETE SET NULL;

ALTER TABLE `facture_item`
  ADD CONSTRAINT `fk_facture_item_facture` FOREIGN KEY (`id_facture`) REFERENCES `facture` (`id_facture`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_item_consultation` FOREIGN KEY (`id_consultation`) REFERENCES `consultation` (`id_consultation`),
  ADD CONSTRAINT `fk_item_examen` FOREIGN KEY (`id_examen`) REFERENCES `examens` (`id_exam`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_item_hospitalisation` FOREIGN KEY (`id_hospitalisation`) REFERENCES `hospitalisation` (`id_admission`),
  ADD CONSTRAINT `fk_item_medicament` FOREIGN KEY (`id_medicament`) REFERENCES `medicament` (`id_medicament`);

ALTER TABLE `commande_pharmacie`
  ADD CONSTRAINT `commande_pharmacie_ibfk_1` FOREIGN KEY (`id_fournisseur`) REFERENCES `fournisseur` (`id_fournisseur`),
  ADD CONSTRAINT `commande_pharmacie_ibfk_2` FOREIGN KEY (`id_user`) REFERENCES `utilisateurs` (`id_user`);

ALTER TABLE `commande_ligne`
  ADD CONSTRAINT `commande_ligne_ibfk_1` FOREIGN KEY (`id_commande`) REFERENCES `commande_pharmacie` (`id_commande`) ON DELETE CASCADE,
  ADD CONSTRAINT `commande_ligne_ibfk_2` FOREIGN KEY (`id_medicament`) REFERENCES `medicament` (`id_medicament`);

ALTER TABLE `mouvement_stock`
  ADD CONSTRAINT `mouvement_stock_ibfk_1` FOREIGN KEY (`id_medicament`) REFERENCES `medicament` (`id_medicament`),
  ADD CONSTRAINT `mouvement_stock_ibfk_2` FOREIGN KEY (`id_user`) REFERENCES `utilisateurs` (`id_user`);

ALTER TABLE `inventaire`
  ADD CONSTRAINT `inventaire_ibfk_1` FOREIGN KEY (`id_user_responsable`) REFERENCES `utilisateurs` (`id_user`);

ALTER TABLE `inventaire_ligne`
  ADD CONSTRAINT `inventaire_ligne_ibfk_1` FOREIGN KEY (`id_inventaire`) REFERENCES `inventaire` (`id_inventaire`) ON DELETE CASCADE,
  ADD CONSTRAINT `inventaire_ligne_ibfk_2` FOREIGN KEY (`id_medicament`) REFERENCES `medicament` (`id_medicament`);

ALTER TABLE `echeancier`
  ADD CONSTRAINT `fk_echeancier_facture` FOREIGN KEY (`id_facture`) REFERENCES `facture` (`id_facture`) ON DELETE CASCADE;

ALTER TABLE `echeance`
  ADD CONSTRAINT `fk_echeance_echeancier` FOREIGN KEY (`id_echeancier`) REFERENCES `echeancier` (`id_echeancier`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_echeance_transaction` FOREIGN KEY (`id_transaction`) REFERENCES `transaction_paiement` (`id_transaction`) ON DELETE SET NULL;

ALTER TABLE `demande_remboursement`
  ADD CONSTRAINT `fk_demande_facture` FOREIGN KEY (`id_facture`) REFERENCES `facture` (`id_facture`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_demande_assurance` FOREIGN KEY (`id_assurance`) REFERENCES `assurances` (`id_assurance`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_demande_patient` FOREIGN KEY (`id_patient`) REFERENCES `patient` (`id_user`) ON DELETE CASCADE;

ALTER TABLE `notifications`
  ADD CONSTRAINT `notifications_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `utilisateurs` (`id_user`) ON DELETE CASCADE;

ALTER TABLE `dispensation`
  ADD CONSTRAINT `dispensation_ibfk_1` FOREIGN KEY (`id_prescription`) REFERENCES `ordonnance` (`id_ordonnance`) ON DELETE SET NULL,
  ADD CONSTRAINT `dispensation_ibfk_2` FOREIGN KEY (`id_patient`) REFERENCES `patient` (`id_user`) ON DELETE CASCADE;

ALTER TABLE `acces_verification`
  ADD CONSTRAINT `acces_verification_ibfk_1` FOREIGN KEY (`id_patient`) REFERENCES `patient` (`id_user`),
  ADD CONSTRAINT `acces_verification_ibfk_2` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateurs` (`id_user`);

ALTER TABLE `historique_acces_dossier`
  ADD CONSTRAINT `historique_acces_dossier_ibfk_1` FOREIGN KEY (`id_patient`) REFERENCES `patient` (`id_user`),
  ADD CONSTRAINT `historique_acces_dossier_ibfk_2` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateurs` (`id_user`);

-- Contraintes pour documents_medicaux
ALTER TABLE `documents_medicaux`
  ADD CONSTRAINT `fk_documents_patient` FOREIGN KEY (`id_patient`) REFERENCES `patient` (`id_user`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_documents_consultation` FOREIGN KEY (`id_consultation`) REFERENCES `consultation` (`id_consultation`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_documents_bulletin` FOREIGN KEY (`id_bulletin_examen`) REFERENCES `bulletin_examen` (`id_bull_exam`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_documents_hospitalisation` FOREIGN KEY (`id_hospitalisation`) REFERENCES `hospitalisation` (`id_admission`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_documents_dmp` FOREIGN KEY (`id_dmp`) REFERENCES `dossier_medical_partage` (`id_dmp`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_documents_createur` FOREIGN KEY (`id_createur`) REFERENCES `utilisateurs` (`id_user`) ON DELETE RESTRICT,
  ADD CONSTRAINT `fk_documents_validateur` FOREIGN KEY (`id_validateur`) REFERENCES `utilisateurs` (`id_user`) ON DELETE SET NULL;

-- Contraintes pour audit_acces_documents
ALTER TABLE `audit_acces_documents`
  ADD CONSTRAINT `fk_audit_utilisateur` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateurs` (`id_user`) ON DELETE CASCADE;

-- Contraintes pour document_dmp (lien vers documents_medicaux)
ALTER TABLE `document_dmp`
  ADD CONSTRAINT `fk_dmp_document_uuid` FOREIGN KEY (`document_uuid`) REFERENCES `documents_medicaux` (`uuid`) ON DELETE SET NULL ON UPDATE CASCADE;

-- --------------------------------------------------------
-- Tables manquantes pour le système de rendez-vous
-- --------------------------------------------------------

CREATE TABLE IF NOT EXISTS `creneau_disponible` (
  `id_creneau` INT NOT NULL AUTO_INCREMENT,
  `id_medecin` INT NOT NULL,
  `jour_semaine` INT NOT NULL COMMENT '1=Lundi, 7=Dimanche',
  `heure_debut` TIME NOT NULL,
  `heure_fin` TIME NOT NULL,
  `duree_par_defaut` INT DEFAULT 30,
  `actif` BOOLEAN DEFAULT TRUE,
  `date_debut_validite` DATE DEFAULT NULL,
  `date_fin_validite` DATE DEFAULT NULL,
  `est_semaine_type` BOOLEAN DEFAULT TRUE,
  PRIMARY KEY (`id_creneau`),
  KEY `fk_creneau_medecin` (`id_medecin`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `indisponibilite_medecin` (
  `id_indisponibilite` INT NOT NULL AUTO_INCREMENT,
  `id_medecin` INT NOT NULL,
  `date_debut` DATETIME NOT NULL,
  `date_fin` DATETIME NOT NULL,
  `type` VARCHAR(50) DEFAULT NULL,
  `motif` VARCHAR(200) DEFAULT NULL,
  `journee_complete` BOOLEAN DEFAULT FALSE,
  PRIMARY KEY (`id_indisponibilite`),
  KEY `IX_indispo_medecin` (`id_medecin`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `slot_lock` (
  `id_lock` INT NOT NULL AUTO_INCREMENT,
  `id_medecin` INT NOT NULL,
  `date_heure` DATETIME NOT NULL,
  `duree` INT DEFAULT 30,
  `id_user` INT NOT NULL,
  `lock_token` VARCHAR(64) NOT NULL,
  `expires_at` DATETIME NOT NULL,
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_lock`),
  UNIQUE KEY `IX_slot_lock_medecin_date` (`id_medecin`, `date_heure`),
  KEY `IX_slot_lock_expires` (`expires_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------
-- Table: audit_logs (journal d'audit)
-- --------------------------------------------------------

CREATE TABLE IF NOT EXISTS `audit_logs` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `user_id` INT NOT NULL DEFAULT 0,
  `action` VARCHAR(100) NOT NULL,
  `resource_type` VARCHAR(100) NOT NULL,
  `resource_id` INT NULL,
  `details` TEXT NULL,
  `ip_address` VARCHAR(45) NULL,
  `user_agent` VARCHAR(500) NULL,
  `success` BOOLEAN NOT NULL DEFAULT TRUE,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX `idx_audit_user_id` (`user_id`),
  INDEX `idx_audit_action` (`action`),
  INDEX `idx_audit_resource` (`resource_type`, `resource_id`),
  INDEX `idx_audit_created_at` (`created_at`),
  INDEX `idx_audit_success` (`success`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: session_caisse (sessions de caisse)
-- --------------------------------------------------------

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

-- --------------------------------------------------------
-- Table: transaction_paiement (transactions de paiement)
-- --------------------------------------------------------

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

CREATE TABLE IF NOT EXISTS `rendez_vous` (
  `id_rdv` INT NOT NULL AUTO_INCREMENT,
  `id_patient` INT NOT NULL,
  `id_medecin` INT NOT NULL,
  `id_service` INT DEFAULT NULL,
  `date_heure` DATETIME NOT NULL,
  `duree` INT DEFAULT 30,
  `motif` VARCHAR(100) DEFAULT NULL,
  `statut` VARCHAR(30) DEFAULT 'planifie',
  `notes` VARCHAR(500) DEFAULT NULL,
  `motif_annulation` VARCHAR(500) DEFAULT NULL,
  `date_annulation` TIMESTAMP NULL DEFAULT NULL,
  `annule_par` INT DEFAULT NULL,
  `date_creation` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `date_modification` TIMESTAMP NULL DEFAULT NULL,
  `type_rdv` VARCHAR(50) DEFAULT NULL,
  `notifie` BOOLEAN DEFAULT FALSE,
  `rappel_envoye` BOOLEAN DEFAULT FALSE,
  `row_version` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_rdv`),
  KEY `IX_rdv_patient` (`id_patient`),
  KEY `IX_rdv_medecin` (`id_medecin`),
  KEY `IX_rdv_date` (`date_heure`),
  KEY `IX_rdv_statut` (`statut`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `parametre` (
  `id_parametre` INT NOT NULL AUTO_INCREMENT,
  `id_consultation` INT NOT NULL,
  `poids` DECIMAL(5,2) DEFAULT NULL,
  `temperature` DECIMAL(4,1) DEFAULT NULL,
  `tension_systolique` INT DEFAULT NULL,
  `tension_diastolique` INT DEFAULT NULL,
  `taille` DECIMAL(5,2) DEFAULT NULL,
  `date_enregistrement` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `enregistre_par` INT DEFAULT NULL,
  PRIMARY KEY (`id_parametre`),
  UNIQUE KEY `IX_parametre_consultation` (`id_consultation`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Correction: Ajouter must_change_password à utilisateurs si manquant
-- ALTER TABLE utilisateurs ADD COLUMN IF NOT EXISTS must_change_password BOOLEAN NOT NULL DEFAULT FALSE;

-- Correction: Ajouter cout_consultation à specialites si manquant  
-- ALTER TABLE specialites ADD COLUMN IF NOT EXISTS cout_consultation DECIMAL(12,2) DEFAULT 5000;

-- Correction: Ajouter id_rdv à consultation si manquant
-- ALTER TABLE consultation ADD COLUMN IF NOT EXISTS id_rdv INT DEFAULT NULL;

-- --------------------------------------------------------
-- Table: question (questionnaire consultation)
-- --------------------------------------------------------

CREATE TABLE `question` (
  `id_question` INT AUTO_INCREMENT PRIMARY KEY,
  `texte` TEXT NOT NULL,
  `type` VARCHAR(50) DEFAULT 'text',
  `categorie` VARCHAR(100) DEFAULT NULL,
  `ordre` INT DEFAULT 0,
  `obligatoire` TINYINT(1) DEFAULT 0,
  `actif` TINYINT(1) DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: consultation_question (liaison consultation-question)
-- --------------------------------------------------------

CREATE TABLE `consultation_question` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `id_consultation` INT NOT NULL,
  `id_question` INT NOT NULL,
  UNIQUE INDEX `UX_consultation_question` (`id_consultation`, `id_question`),
  INDEX `IX_consultation_question_question` (`id_question`),
  CONSTRAINT `FK_consultation_question_consultation` FOREIGN KEY (`id_consultation`)
    REFERENCES `consultation` (`id_consultation`) ON DELETE CASCADE,
  CONSTRAINT `FK_consultation_question_question` FOREIGN KEY (`id_question`)
    REFERENCES `question` (`id_question`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: reponse (réponses aux questions)
-- --------------------------------------------------------

CREATE TABLE `reponse` (
  `id_reponse` INT AUTO_INCREMENT PRIMARY KEY,
  `id_consultation_question` INT NOT NULL,
  `valeur` TEXT NULL,
  `date_reponse` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  INDEX `IX_reponse_consultation_question` (`id_consultation_question`),
  CONSTRAINT `FK_reponse_consultation_question` FOREIGN KEY (`id_consultation_question`)
    REFERENCES `consultation_question` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Seed: Questions prédéfinies
-- --------------------------------------------------------

INSERT INTO `question` (`texte`, `type`, `categorie`, `ordre`, `obligatoire`, `actif`) VALUES
('Motif principal de consultation', 'text', 'general', 1, 1, 1),
('Symptomes actuels (debut, duree, intensite)', 'text', 'general', 2, 1, 1),
('Antecedents medicaux pertinents', 'text', 'general', 3, 0, 1),
('Traitements en cours', 'text', 'general', 4, 0, 1),
('Allergies connues', 'text', 'general', 5, 0, 1);

-- --------------------------------------------------------
-- Table: permissions (RBAC)
-- --------------------------------------------------------

CREATE TABLE `permissions` (
  `id_permission` INT AUTO_INCREMENT PRIMARY KEY,
  `code` VARCHAR(100) NOT NULL UNIQUE,
  `nom` VARCHAR(150) NOT NULL,
  `description` VARCHAR(500) DEFAULT NULL,
  `module` VARCHAR(50) NOT NULL,
  `actif` BOOLEAN DEFAULT TRUE,
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  INDEX `IX_permission_module` (`module`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: role_permissions (liaison rôles-permissions)
-- --------------------------------------------------------

CREATE TABLE `role_permissions` (
  `id_role_permission` INT AUTO_INCREMENT PRIMARY KEY,
  `role` VARCHAR(50) NOT NULL,
  `id_permission` INT NOT NULL,
  `actif` BOOLEAN DEFAULT TRUE,
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY `UK_role_permission` (`role`, `id_permission`),
  INDEX `IX_role_permission_role` (`role`),
  CONSTRAINT `FK_role_permission_permission` FOREIGN KEY (`id_permission`) 
    REFERENCES `permissions`(`id_permission`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: user_permissions (permissions spécifiques utilisateur)
-- --------------------------------------------------------

CREATE TABLE `user_permissions` (
  `id_user_permission` INT AUTO_INCREMENT PRIMARY KEY,
  `id_user` INT NOT NULL,
  `id_permission` INT NOT NULL,
  `granted` BOOLEAN DEFAULT TRUE,
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY `UK_user_permission` (`id_user`, `id_permission`),
  CONSTRAINT `FK_user_permission_user` FOREIGN KEY (`id_user`) 
    REFERENCES `utilisateurs`(`id_user`) ON DELETE CASCADE,
  CONSTRAINT `FK_user_permission_permission` FOREIGN KEY (`id_permission`) 
    REFERENCES `permissions`(`id_permission`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Seed: Permissions par module
-- --------------------------------------------------------

INSERT INTO `permissions` (`code`, `nom`, `description`, `module`) VALUES
-- Module: Patients
('patients.view', 'Voir les patients', 'Permet de consulter la liste et les détails des patients', 'patients'),
('patients.create', 'Créer un patient', 'Permet de créer un nouveau dossier patient', 'patients'),
('patients.edit', 'Modifier un patient', 'Permet de modifier les informations d''un patient', 'patients'),
('patients.delete', 'Supprimer un patient', 'Permet de supprimer un dossier patient', 'patients'),
('patients.view_dossier', 'Voir le dossier médical', 'Permet de consulter le dossier médical complet', 'patients'),
-- Module: Consultations
('consultations.view', 'Voir les consultations', 'Permet de consulter les consultations', 'consultations'),
('consultations.create', 'Créer une consultation', 'Permet de créer une nouvelle consultation', 'consultations'),
('consultations.edit', 'Modifier une consultation', 'Permet de modifier une consultation', 'consultations'),
('consultations.close', 'Clôturer une consultation', 'Permet de clôturer une consultation', 'consultations'),
('consultations.cancel', 'Annuler une consultation', 'Permet d''annuler une consultation', 'consultations'),
-- Module: Rendez-vous
('rdv.view', 'Voir les rendez-vous', 'Permet de consulter les rendez-vous', 'rdv'),
('rdv.create', 'Créer un rendez-vous', 'Permet de créer un rendez-vous', 'rdv'),
('rdv.edit', 'Modifier un rendez-vous', 'Permet de modifier un rendez-vous', 'rdv'),
('rdv.cancel', 'Annuler un rendez-vous', 'Permet d''annuler un rendez-vous', 'rdv'),
('rdv.validate', 'Valider un rendez-vous', 'Permet de valider/confirmer un rendez-vous', 'rdv'),
-- Module: Paramètres vitaux
('parametres.view', 'Voir les paramètres', 'Permet de consulter les paramètres vitaux', 'parametres'),
('parametres.create', 'Saisir les paramètres', 'Permet de saisir les paramètres vitaux', 'parametres'),
('parametres.edit', 'Modifier les paramètres', 'Permet de modifier les paramètres vitaux', 'parametres'),
-- Module: Prescriptions
('prescriptions.view', 'Voir les prescriptions', 'Permet de consulter les prescriptions', 'prescriptions'),
('prescriptions.create', 'Créer une prescription', 'Permet de créer une prescription', 'prescriptions'),
('prescriptions.edit', 'Modifier une prescription', 'Permet de modifier une prescription', 'prescriptions'),
('prescriptions.dispense', 'Dispenser une prescription', 'Permet de dispenser les médicaments', 'prescriptions'),
-- Module: Examens
('examens.view', 'Voir les examens', 'Permet de consulter les examens', 'examens'),
('examens.request', 'Demander un examen', 'Permet de demander un examen', 'examens'),
('examens.result', 'Saisir les résultats', 'Permet de saisir les résultats d''examen', 'examens'),
('examens.validate', 'Valider les résultats', 'Permet de valider les résultats d''examen', 'examens'),
-- Module: Facturation
('facturation.view', 'Voir les factures', 'Permet de consulter les factures', 'facturation'),
('facturation.create', 'Créer une facture', 'Permet de créer une facture', 'facturation'),
('facturation.edit', 'Modifier une facture', 'Permet de modifier une facture', 'facturation'),
('facturation.payment', 'Encaisser un paiement', 'Permet d''encaisser un paiement', 'facturation'),
('facturation.refund', 'Effectuer un remboursement', 'Permet d''effectuer un remboursement', 'facturation'),
-- Module: Pharmacie
('pharmacie.view_stock', 'Voir le stock', 'Permet de consulter le stock de médicaments', 'pharmacie'),
('pharmacie.manage_stock', 'Gérer le stock', 'Permet de gérer le stock (entrées/sorties)', 'pharmacie'),
('pharmacie.order', 'Passer des commandes', 'Permet de passer des commandes fournisseurs', 'pharmacie'),
-- Module: Hospitalisation
('hospitalisation.view', 'Voir les hospitalisations', 'Permet de consulter les hospitalisations', 'hospitalisation'),
('hospitalisation.admit', 'Admettre un patient', 'Permet d''admettre un patient', 'hospitalisation'),
('hospitalisation.discharge', 'Sortir un patient', 'Permet de faire sortir un patient', 'hospitalisation'),
('hospitalisation.manage_lits', 'Gérer les lits', 'Permet de gérer les lits et chambres', 'hospitalisation'),
-- Module: Administration
('admin.users', 'Gérer les utilisateurs', 'Permet de gérer les utilisateurs du système', 'admin'),
('admin.roles', 'Gérer les rôles', 'Permet de gérer les rôles et permissions', 'admin'),
('admin.settings', 'Gérer les paramètres', 'Permet de gérer les paramètres système', 'admin'),
('admin.services', 'Gérer les services', 'Permet de gérer les services médicaux', 'admin'),
('admin.audit', 'Voir les logs d''audit', 'Permet de consulter les logs d''audit', 'admin');

-- --------------------------------------------------------
-- Seed: Attribution des permissions aux rôles
-- --------------------------------------------------------

-- Rôle: patient
INSERT INTO `role_permissions` (`role`, `id_permission`)
SELECT 'patient', `id_permission` FROM `permissions` WHERE `code` IN (
    'rdv.view', 'rdv.create', 'rdv.cancel',
    'consultations.view',
    'prescriptions.view',
    'examens.view',
    'facturation.view'
);

-- Rôle: medecin
INSERT INTO `role_permissions` (`role`, `id_permission`)
SELECT 'medecin', `id_permission` FROM `permissions` WHERE `code` IN (
    'patients.view', 'patients.view_dossier',
    'consultations.view', 'consultations.create', 'consultations.edit', 'consultations.close',
    'rdv.view', 'rdv.validate', 'rdv.cancel',
    'parametres.view',
    'prescriptions.view', 'prescriptions.create', 'prescriptions.edit',
    'examens.view', 'examens.request',
    'hospitalisation.view', 'hospitalisation.admit', 'hospitalisation.discharge'
);

-- Rôle: infirmier
INSERT INTO `role_permissions` (`role`, `id_permission`)
SELECT 'infirmier', `id_permission` FROM `permissions` WHERE `code` IN (
    'patients.view',
    'consultations.view',
    'parametres.view', 'parametres.create', 'parametres.edit',
    'prescriptions.view',
    'examens.view'
);

-- Rôle: accueil
INSERT INTO `role_permissions` (`role`, `id_permission`)
SELECT 'accueil', `id_permission` FROM `permissions` WHERE `code` IN (
    'patients.view', 'patients.create', 'patients.edit',
    'rdv.view', 'rdv.create', 'rdv.edit', 'rdv.cancel',
    'consultations.view', 'consultations.create',
    'parametres.view', 'parametres.create'
);

-- Rôle: caissier
INSERT INTO `role_permissions` (`role`, `id_permission`)
SELECT 'caissier', `id_permission` FROM `permissions` WHERE `code` IN (
    'patients.view',
    'facturation.view', 'facturation.create', 'facturation.edit', 'facturation.payment',
    'consultations.view'
);

-- Rôle: pharmacien
INSERT INTO `role_permissions` (`role`, `id_permission`)
SELECT 'pharmacien', `id_permission` FROM `permissions` WHERE `code` IN (
    'patients.view',
    'prescriptions.view', 'prescriptions.dispense',
    'pharmacie.view_stock', 'pharmacie.manage_stock', 'pharmacie.order'
);

-- Rôle: laborantin
INSERT INTO `role_permissions` (`role`, `id_permission`)
SELECT 'laborantin', `id_permission` FROM `permissions` WHERE `code` IN (
    'patients.view',
    'examens.view', 'examens.result', 'examens.validate'
);

-- --------------------------------------------------------
-- Vues de monitoring pour documents médicaux
-- --------------------------------------------------------

-- Vue: dashboard_documents (tableau de bord des documents)
CREATE OR REPLACE VIEW `v_dashboard_documents` AS
SELECT 
  dm.uuid,
  dm.nom_fichier_original,
  dm.type_document,
  dm.sous_type,
  dm.mime_type,
  dm.taille_octets,
  ROUND(dm.taille_octets / 1024 / 1024, 2) as taille_mo,
  dm.niveau_confidentialite,
  dm.statut,
  dm.hash_sha256 IS NOT NULL as hash_present,
  dm.created_at,
  dm.date_document,
  dm.id_patient,
  CONCAT(u_patient.prenom, ' ', u_patient.nom) as patient_nom,
  p.numero_dossier as patient_dossier,
  dm.id_createur,
  CONCAT(u_createur.prenom, ' ', u_createur.nom) as createur_nom,
  u_createur.role as createur_role,
  (SELECT COUNT(*) FROM audit_acces_documents aad WHERE aad.document_uuid = dm.uuid) as nb_acces_total,
  (SELECT COUNT(*) FROM audit_acces_documents aad WHERE aad.document_uuid = dm.uuid AND aad.type_action = 'telechargement') as nb_telechargements,
  (SELECT MAX(timestamp) FROM audit_acces_documents aad WHERE aad.document_uuid = dm.uuid) as dernier_acces,
  (SELECT vi.statut_verification FROM verification_integrite vi WHERE vi.document_uuid = dm.uuid ORDER BY vi.timestamp DESC LIMIT 1) as derniere_verif_statut,
  (SELECT vi.timestamp FROM verification_integrite vi WHERE vi.document_uuid = dm.uuid ORDER BY vi.timestamp DESC LIMIT 1) as derniere_verif_date
FROM `documents_medicaux` dm
INNER JOIN `patient` p ON dm.id_patient = p.id_user
INNER JOIN `utilisateurs` u_patient ON p.id_user = u_patient.id_user
INNER JOIN `utilisateurs` u_createur ON dm.id_createur = u_createur.id_user
WHERE dm.est_version_courante = TRUE;

-- Vue: documents_problemes (documents avec problèmes d'intégrité)
CREATE OR REPLACE VIEW `v_documents_problemes` AS
SELECT 
  dm.uuid,
  dm.nom_fichier_original,
  dm.chemin_relatif,
  dm.type_document,
  dm.id_patient,
  CONCAT(u.prenom, ' ', u.nom) as patient_nom,
  p.numero_dossier,
  vi.statut_verification as probleme_type,
  vi.hash_attendu,
  vi.hash_calcule,
  vi.taille_attendue,
  vi.taille_reelle,
  vi.timestamp as date_detection,
  vi.action_corrective,
  vi.alerte_envoyee,
  CASE vi.statut_verification
    WHEN 'hash_invalide' THEN 'CRITIQUE - Fichier potentiellement corrompu'
    WHEN 'fichier_absent' THEN 'URGENT - Fichier introuvable'
    WHEN 'erreur_lecture' THEN 'ATTENTION - Erreur de lecture'
    WHEN 'hash_non_calcule' THEN 'INFO - Hash non calcule'
    ELSE 'INCONNU'
  END as description_probleme,
  CASE vi.statut_verification
    WHEN 'hash_invalide' THEN 1
    WHEN 'fichier_absent' THEN 2
    WHEN 'erreur_lecture' THEN 3
    ELSE 4
  END as priorite
FROM `documents_medicaux` dm
INNER JOIN `verification_integrite` vi ON dm.uuid = vi.document_uuid
INNER JOIN `patient` p ON dm.id_patient = p.id_user
INNER JOIN `utilisateurs` u ON p.id_user = u.id_user
WHERE vi.statut_verification NOT IN ('ok')
  AND vi.id_verification = (
    SELECT MAX(vi2.id_verification) 
    FROM verification_integrite vi2 
    WHERE vi2.document_uuid = dm.uuid
  )
ORDER BY priorite ASC, vi.timestamp DESC;

-- Vue: statistiques_documents (statistiques globales)
CREATE OR REPLACE VIEW `v_statistiques_documents` AS
SELECT 
  type_document,
  COUNT(*) as nombre_documents,
  SUM(taille_octets) as taille_totale_octets,
  ROUND(SUM(taille_octets) / 1024 / 1024 / 1024, 2) as taille_totale_go,
  SUM(CASE WHEN hash_sha256 IS NOT NULL THEN 1 ELSE 0 END) as avec_hash,
  SUM(CASE WHEN hash_sha256 IS NULL THEN 1 ELSE 0 END) as sans_hash,
  SUM(CASE WHEN statut = 'actif' THEN 1 ELSE 0 END) as actifs,
  SUM(CASE WHEN statut = 'archive' THEN 1 ELSE 0 END) as archives,
  SUM(CASE WHEN statut = 'quarantaine' THEN 1 ELSE 0 END) as en_quarantaine,
  MIN(created_at) as premier_document,
  MAX(created_at) as dernier_document
FROM `documents_medicaux`
WHERE est_version_courante = TRUE
GROUP BY type_document
WITH ROLLUP;

-- --------------------------------------------------------
-- Table: consultation_audit (historique des modifications de consultations)
-- --------------------------------------------------------

CREATE TABLE `consultation_audit` (
  `id_audit` INT NOT NULL AUTO_INCREMENT,
  `id_consultation` INT NOT NULL,
  `id_utilisateur` INT NOT NULL,
  `type_action` VARCHAR(50) NOT NULL COMMENT 'creation, modification, statut_change, annulation, validation, pause, reprise',
  `champ_modifie` VARCHAR(100) DEFAULT NULL,
  `ancienne_valeur` TEXT DEFAULT NULL,
  `nouvelle_valeur` TEXT DEFAULT NULL,
  `description` TEXT DEFAULT NULL,
  `adresse_ip` VARCHAR(45) DEFAULT NULL,
  `user_agent` TEXT DEFAULT NULL,
  `date_modification` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_audit`),
  KEY `idx_consultation_audit_consultation` (`id_consultation`),
  KEY `idx_consultation_audit_utilisateur` (`id_utilisateur`),
  KEY `idx_consultation_audit_date` (`date_modification`),
  KEY `idx_consultation_audit_type` (`type_action`),
  CONSTRAINT `fk_consultation_audit_consultation` FOREIGN KEY (`id_consultation`) 
    REFERENCES `consultation` (`id_consultation`) ON DELETE CASCADE,
  CONSTRAINT `fk_consultation_audit_utilisateur` FOREIGN KEY (`id_utilisateur`) 
    REFERENCES `utilisateurs` (`id_user`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: question_libre (questions libres posées par le médecin)
-- --------------------------------------------------------

CREATE TABLE `question_libre` (
  `id_question_libre` INT NOT NULL AUTO_INCREMENT,
  `id_consultation` INT NOT NULL,
  `texte_question` TEXT NOT NULL,
  `reponse` TEXT DEFAULT NULL,
  `ordre` INT DEFAULT 0,
  `date_creation` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `id_medecin` INT NOT NULL,
  PRIMARY KEY (`id_question_libre`),
  KEY `idx_question_libre_consultation` (`id_consultation`),
  KEY `idx_question_libre_medecin` (`id_medecin`),
  CONSTRAINT `fk_question_libre_consultation` FOREIGN KEY (`id_consultation`) 
    REFERENCES `consultation` (`id_consultation`) ON DELETE CASCADE,
  CONSTRAINT `fk_question_libre_medecin` FOREIGN KEY (`id_medecin`) 
    REFERENCES `medecin` (`id_user`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

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
('intrathecale', 'Voie intrathécale', 'Injection dans l''espace sous-arachnoïdien', 15);

-- --------------------------------------------------------
-- Association des médicaments existants avec formes et voies par défaut
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

-- Amoxicilline (gélule + suspension, voie orale)
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

-- Metronidazole (comprimé + injectable, voie orale + IV)
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

SET FOREIGN_KEY_CHECKS = 1;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
