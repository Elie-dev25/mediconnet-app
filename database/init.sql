-- MediConnect Database Schema
-- Toutes les clés primaires sont INT AUTO_INCREMENT

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
  `role` ENUM('patient','medecin','infirmier','administrateur','caissier') NOT NULL,
  `password_hash` VARCHAR(500) DEFAULT NULL,
  `photo` VARCHAR(500) DEFAULT NULL,
  `email_confirmed` BOOLEAN NOT NULL DEFAULT FALSE,
  `email_confirmed_at` TIMESTAMP NULL DEFAULT NULL,
  `profile_completed` BOOLEAN NOT NULL DEFAULT FALSE,
  `profile_completed_at` TIMESTAMP NULL DEFAULT NULL,
  `nationalite` VARCHAR(100) DEFAULT 'Cameroun',
  `region_origine` VARCHAR(100) DEFAULT NULL,
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
-- Table: assurance
-- --------------------------------------------------------

CREATE TABLE `assurance` (
  `id_assurance` INT NOT NULL AUTO_INCREMENT,
  `nom_assurance` VARCHAR(150) NOT NULL,
  `couverture` FLOAT DEFAULT NULL,
  `numero_assurance` VARCHAR(30) DEFAULT NULL,
  `date_delivrance` DATE DEFAULT NULL,
  `date_expiration` DATE DEFAULT NULL,
  PRIMARY KEY (`id_assurance`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: service
-- --------------------------------------------------------

CREATE TABLE `service` (
  `id_service` INT NOT NULL AUTO_INCREMENT,
  `nom_service` VARCHAR(150) NOT NULL,
  `responsable_service` INT DEFAULT NULL,
  `description` TEXT DEFAULT NULL,
  PRIMARY KEY (`id_service`),
  KEY `responsable_service` (`responsable_service`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

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
  PRIMARY KEY (`id_user`),
  UNIQUE KEY `matricule` (`matricule`)
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
-- Table: chambre
-- --------------------------------------------------------

CREATE TABLE `chambre` (
  `id_chambre` INT NOT NULL AUTO_INCREMENT,
  `numero` VARCHAR(20) DEFAULT NULL,
  `capacite` INT DEFAULT NULL,
  `etat` VARCHAR(50) DEFAULT NULL,
  `statut` VARCHAR(50) DEFAULT NULL,
  PRIMARY KEY (`id_chambre`),
  UNIQUE KEY `numero` (`numero`)
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
  `motif` TEXT DEFAULT NULL,
  `diagnostic` TEXT DEFAULT NULL,
  `statut` VARCHAR(20) DEFAULT NULL,
  `id_medecin` INT NOT NULL,
  `id_patient` INT NOT NULL,
  `poids` DECIMAL(5,2) DEFAULT NULL,
  `temperature` DECIMAL(4,2) DEFAULT NULL,
  `type_consultation` VARCHAR(100) DEFAULT NULL,
  `antecedents` TEXT DEFAULT NULL,
  `chemin_questionnaire` VARCHAR(255) DEFAULT NULL,
  `tension` VARCHAR(10) DEFAULT NULL,
  PRIMARY KEY (`id_consultation`),
  KEY `id_medecin` (`id_medecin`),
  KEY `id_patient` (`id_patient`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: hospitalisation
-- --------------------------------------------------------

CREATE TABLE `hospitalisation` (
  `id_admission` INT NOT NULL AUTO_INCREMENT,
  `date_entree` DATE NOT NULL,
  `date_sortie` DATE DEFAULT NULL,
  `motif` TEXT DEFAULT NULL,
  `statut` VARCHAR(20) DEFAULT NULL,
  `id_patient` INT NOT NULL,
  `id_lit` INT NOT NULL,
  PRIMARY KEY (`id_admission`),
  KEY `id_patient` (`id_patient`),
  KEY `id_lit` (`id_lit`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: prescription (ordonnance)
-- --------------------------------------------------------

CREATE TABLE `prescription` (
  `id_ord` INT NOT NULL AUTO_INCREMENT,
  `date` DATE NOT NULL,
  `id_consultation` INT NOT NULL,
  `commentaire` TEXT DEFAULT NULL,
  PRIMARY KEY (`id_ord`),
  KEY `id_consultation` (`id_consultation`)
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
  `forme_galenique` ENUM('comprimé','sirop','injectable') DEFAULT NULL,
  `laboratoire` VARCHAR(150) DEFAULT NULL,
  `conditionnement` VARCHAR(100) DEFAULT NULL,
  `date_peremption` DATE DEFAULT NULL,
  `actif` TINYINT(1) DEFAULT 1,
  `emplacement_rayon` VARCHAR(50) DEFAULT NULL,
  `temperature_conservation` VARCHAR(50) DEFAULT NULL,
  PRIMARY KEY (`id_medicament`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: prescription_medicament
-- --------------------------------------------------------

CREATE TABLE `prescription_medicament` (
  `id_ord` INT NOT NULL,
  `id_medicament` INT NOT NULL,
  `quantite` INT DEFAULT 1,
  `duree_traitement` VARCHAR(100) DEFAULT NULL,
  `posologie` TEXT DEFAULT NULL,
  PRIMARY KEY (`id_ord`, `id_medicament`),
  KEY `id_medicament` (`id_medicament`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: laboratoire
-- --------------------------------------------------------

CREATE TABLE `laboratoire` (
  `id_labo` INT NOT NULL AUTO_INCREMENT,
  `nom_labo` VARCHAR(150) NOT NULL,
  `contact` VARCHAR(150) DEFAULT NULL,
  PRIMARY KEY (`id_labo`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: examens
-- --------------------------------------------------------

CREATE TABLE `examens` (
  `id_exam` INT NOT NULL AUTO_INCREMENT,
  `nom_exam` VARCHAR(150) NOT NULL,
  `description` TEXT DEFAULT NULL,
  `prix_unitaire` DECIMAL(10,2) NOT NULL,
  `duree_estimee_minutes` INT DEFAULT 30,
  `preparation_requise` TEXT DEFAULT NULL,
  `type_examen` ENUM('biologie','radiologie','scanner','irm','echographie','autre') NOT NULL,
  `categorie` ENUM('standard','specialise','urgence') DEFAULT 'standard',
  `actif` TINYINT(1) DEFAULT 1,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_exam`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: bulletin_examen
-- --------------------------------------------------------

CREATE TABLE `bulletin_examen` (
  `id_bull_exam` INT NOT NULL AUTO_INCREMENT,
  `date_demande` DATE NOT NULL,
  `id_labo` INT DEFAULT NULL,
  `id_consultation` INT DEFAULT NULL,
  `instructions` TEXT DEFAULT NULL,
  `id_exam` INT DEFAULT NULL,
  PRIMARY KEY (`id_bull_exam`),
  KEY `id_labo` (`id_labo`),
  KEY `id_consultation` (`id_consultation`),
  KEY `fk_bulletin_examen` (`id_exam`)
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
  PRIMARY KEY (`id_facture`),
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
-- Table: notification
-- --------------------------------------------------------

CREATE TABLE `notification` (
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
-- Contraintes (Foreign Keys)
-- --------------------------------------------------------

ALTER TABLE `service`
  ADD CONSTRAINT `service_ibfk_1` FOREIGN KEY (`responsable_service`) REFERENCES `utilisateurs` (`id_user`) ON DELETE SET NULL;

ALTER TABLE `patient`
  ADD CONSTRAINT `fk_patient_assurance` FOREIGN KEY (`id_assurance`) REFERENCES `assurance` (`id_assurance`) ON DELETE SET NULL,
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

ALTER TABLE `lit`
  ADD CONSTRAINT `lit_ibfk_1` FOREIGN KEY (`id_chambre`) REFERENCES `chambre` (`id_chambre`);

ALTER TABLE `consultation`
  ADD CONSTRAINT `consultation_ibfk_1` FOREIGN KEY (`id_medecin`) REFERENCES `medecin` (`id_user`),
  ADD CONSTRAINT `consultation_ibfk_2` FOREIGN KEY (`id_patient`) REFERENCES `patient` (`id_user`) ON DELETE CASCADE;

ALTER TABLE `hospitalisation`
  ADD CONSTRAINT `hospitalisation_ibfk_1` FOREIGN KEY (`id_patient`) REFERENCES `patient` (`id_user`) ON DELETE CASCADE,
  ADD CONSTRAINT `hospitalisation_ibfk_2` FOREIGN KEY (`id_lit`) REFERENCES `lit` (`id_lit`);

ALTER TABLE `prescription`
  ADD CONSTRAINT `prescription_ibfk_1` FOREIGN KEY (`id_consultation`) REFERENCES `consultation` (`id_consultation`) ON DELETE CASCADE;

ALTER TABLE `prescription_medicament`
  ADD CONSTRAINT `prescription_medicament_ibfk_1` FOREIGN KEY (`id_ord`) REFERENCES `prescription` (`id_ord`) ON DELETE CASCADE,
  ADD CONSTRAINT `prescription_medicament_ibfk_2` FOREIGN KEY (`id_medicament`) REFERENCES `medicament` (`id_medicament`);

ALTER TABLE `bulletin_examen`
  ADD CONSTRAINT `bulletin_examen_ibfk_1` FOREIGN KEY (`id_labo`) REFERENCES `laboratoire` (`id_labo`) ON DELETE SET NULL,
  ADD CONSTRAINT `bulletin_examen_ibfk_2` FOREIGN KEY (`id_consultation`) REFERENCES `consultation` (`id_consultation`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_bulletin_examen` FOREIGN KEY (`id_exam`) REFERENCES `examens` (`id_exam`) ON DELETE SET NULL ON UPDATE CASCADE;

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

ALTER TABLE `notification`
  ADD CONSTRAINT `notification_ibfk_1` FOREIGN KEY (`id_user`) REFERENCES `utilisateurs` (`id_user`) ON DELETE CASCADE;

ALTER TABLE `acces_verification`
  ADD CONSTRAINT `acces_verification_ibfk_1` FOREIGN KEY (`id_patient`) REFERENCES `patient` (`id_user`),
  ADD CONSTRAINT `acces_verification_ibfk_2` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateurs` (`id_user`);

ALTER TABLE `historique_acces_dossier`
  ADD CONSTRAINT `historique_acces_dossier_ibfk_1` FOREIGN KEY (`id_patient`) REFERENCES `patient` (`id_user`),
  ADD CONSTRAINT `historique_acces_dossier_ibfk_2` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateurs` (`id_user`);

SET FOREIGN_KEY_CHECKS = 1;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
