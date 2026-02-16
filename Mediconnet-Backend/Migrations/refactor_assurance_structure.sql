-- Migration: Refactoring complet du système d'assurance
-- Date: 2026-02-16
-- Description: Création des tables de référence et normalisation de la structure

-- ==================== 1. TABLES DE RÉFÉRENCE ====================

-- Table des types de prestation (pour AssuranceCouverture)
CREATE TABLE IF NOT EXISTS type_prestation (
    code VARCHAR(50) PRIMARY KEY,
    libelle VARCHAR(100) NOT NULL,
    description VARCHAR(255) NULL,
    icone VARCHAR(50) NULL,
    ordre INT NOT NULL DEFAULT 0,
    actif TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table des catégories de bénéficiaires
CREATE TABLE IF NOT EXISTS categorie_beneficiaire (
    id_categorie INT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    libelle VARCHAR(100) NOT NULL,
    description VARCHAR(255) NULL,
    actif TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table des modes de paiement
CREATE TABLE IF NOT EXISTS mode_paiement (
    id_mode INT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    libelle VARCHAR(100) NOT NULL,
    description VARCHAR(255) NULL,
    actif TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table des zones de couverture
CREATE TABLE IF NOT EXISTS zone_couverture (
    id_zone INT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    libelle VARCHAR(100) NOT NULL,
    description VARCHAR(255) NULL,
    actif TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table des types de couverture santé (hospitalisation, maternité, etc.)
CREATE TABLE IF NOT EXISTS type_couverture_sante (
    id_type_couverture INT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    libelle VARCHAR(100) NOT NULL,
    description VARCHAR(255) NULL,
    actif TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==================== 2. DONNÉES DE RÉFÉRENCE ====================

-- Types de prestation (pour facturation)
INSERT INTO type_prestation (code, libelle, description, icone, ordre) VALUES
('consultation', 'Consultation', 'Consultations médicales générales et spécialisées', 'stethoscope', 1),
('hospitalisation', 'Hospitalisation', 'Séjours hospitaliers et soins intensifs', 'bed', 2),
('examen', 'Examens', 'Examens de laboratoire et imagerie médicale', 'microscope', 3),
('pharmacie', 'Pharmacie', 'Médicaments et produits pharmaceutiques', 'pill', 4)
ON DUPLICATE KEY UPDATE libelle = VALUES(libelle), description = VALUES(description);

-- Catégories de bénéficiaires
INSERT INTO categorie_beneficiaire (code, libelle, description) VALUES
('salaries', 'Salariés', 'Employés du secteur formel'),
('familles', 'Familles', 'Membres de la famille des assurés'),
('retraites', 'Retraités', 'Personnes à la retraite'),
('artisans', 'Artisans', 'Travailleurs indépendants et artisans'),
('etudiants', 'Étudiants', 'Étudiants et élèves'),
('femmes_enceintes', 'Femmes enceintes', 'Couverture maternité'),
('enfants', 'Enfants', 'Mineurs de moins de 18 ans'),
('diaspora', 'Diaspora', 'Ressortissants vivant à l''étranger'),
('indigents', 'Indigents', 'Personnes en situation de précarité')
ON DUPLICATE KEY UPDATE libelle = VALUES(libelle);

-- Modes de paiement
INSERT INTO mode_paiement (code, libelle, description) VALUES
('mobile_money', 'Mobile Money', 'Paiement via Orange Money, MTN MoMo, etc.'),
('virement', 'Virement bancaire', 'Virement depuis un compte bancaire'),
('prelevement', 'Prélèvement automatique', 'Prélèvement mensuel automatique'),
('especes', 'Espèces', 'Paiement en espèces'),
('employeur', 'Retenue employeur', 'Cotisation prélevée sur salaire'),
('cotisation_groupe', 'Cotisation de groupe', 'Cotisation collective')
ON DUPLICATE KEY UPDATE libelle = VALUES(libelle);

-- Zones de couverture
INSERT INTO zone_couverture (code, libelle, description) VALUES
('national', 'National', 'Couverture sur tout le territoire national'),
('regional', 'Régional', 'Couverture limitée à certaines régions'),
('urbain', 'Zones urbaines', 'Couverture dans les grandes villes'),
('rural', 'Zones rurales', 'Couverture en milieu rural'),
('international', 'International', 'Couverture à l''étranger incluse'),
('diaspora', 'Diaspora', 'Couverture pour les ressortissants à l''étranger')
ON DUPLICATE KEY UPDATE libelle = VALUES(libelle);

-- Types de couverture santé
INSERT INTO type_couverture_sante (code, libelle, description) VALUES
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
('maladies_chroniques', 'Maladies chroniques', 'Prise en charge des ALD')
ON DUPLICATE KEY UPDATE libelle = VALUES(libelle);

-- ==================== 3. TABLE DE LIAISON ASSURANCE - TYPES COUVERTURE ====================

CREATE TABLE IF NOT EXISTS assurance_type_couverture (
    id_assurance INT NOT NULL,
    id_type_couverture INT NOT NULL,
    PRIMARY KEY (id_assurance, id_type_couverture),
    CONSTRAINT fk_atc_assurance FOREIGN KEY (id_assurance) REFERENCES assurances(id_assurance) ON DELETE CASCADE,
    CONSTRAINT fk_atc_type_couverture FOREIGN KEY (id_type_couverture) REFERENCES type_couverture_sante(id_type_couverture) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==================== 4. TABLE DE LIAISON ASSURANCE - CATÉGORIES BÉNÉFICIAIRES ====================

CREATE TABLE IF NOT EXISTS assurance_categorie_beneficiaire (
    id_assurance INT NOT NULL,
    id_categorie INT NOT NULL,
    PRIMARY KEY (id_assurance, id_categorie),
    CONSTRAINT fk_acb_assurance FOREIGN KEY (id_assurance) REFERENCES assurances(id_assurance) ON DELETE CASCADE,
    CONSTRAINT fk_acb_categorie FOREIGN KEY (id_categorie) REFERENCES categorie_beneficiaire(id_categorie) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==================== 5. TABLE DE LIAISON ASSURANCE - MODES PAIEMENT ====================

CREATE TABLE IF NOT EXISTS assurance_mode_paiement (
    id_assurance INT NOT NULL,
    id_mode INT NOT NULL,
    PRIMARY KEY (id_assurance, id_mode),
    CONSTRAINT fk_amp_assurance FOREIGN KEY (id_assurance) REFERENCES assurances(id_assurance) ON DELETE CASCADE,
    CONSTRAINT fk_amp_mode FOREIGN KEY (id_mode) REFERENCES mode_paiement(id_mode) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==================== 6. MODIFICATION TABLE ASSURANCES ====================

-- Ajouter colonne zone_couverture_id (FK)
ALTER TABLE assurances 
ADD COLUMN IF NOT EXISTS id_zone_couverture INT NULL,
ADD CONSTRAINT fk_assurance_zone FOREIGN KEY (id_zone_couverture) REFERENCES zone_couverture(id_zone) ON DELETE SET NULL;

-- Migrer les données existantes de zone_couverture vers la nouvelle FK
UPDATE assurances a
SET a.id_zone_couverture = (
    SELECT z.id_zone FROM zone_couverture z WHERE z.code = a.zone_couverture LIMIT 1
)
WHERE a.zone_couverture IS NOT NULL AND a.id_zone_couverture IS NULL;

-- ==================== 7. MIGRATION DES DONNÉES EXISTANTES ====================

-- Migrer type_couverture (CSV) vers table de liaison
-- Pour chaque assurance active, ajouter les types de couverture par défaut
INSERT IGNORE INTO assurance_type_couverture (id_assurance, id_type_couverture)
SELECT a.id_assurance, tc.id_type_couverture
FROM assurances a
CROSS JOIN type_couverture_sante tc
WHERE a.is_active = 1 
AND tc.code IN ('hospitalisation', 'ambulatoire', 'medicaments');

-- Migrer categorie_beneficiaires vers table de liaison
INSERT IGNORE INTO assurance_categorie_beneficiaire (id_assurance, id_categorie)
SELECT a.id_assurance, cb.id_categorie
FROM assurances a
CROSS JOIN categorie_beneficiaire cb
WHERE a.is_active = 1 
AND cb.code IN ('salaries', 'familles');

-- Migrer mode_paiement vers table de liaison
INSERT IGNORE INTO assurance_mode_paiement (id_assurance, id_mode)
SELECT a.id_assurance, mp.id_mode
FROM assurances a
CROSS JOIN mode_paiement mp
WHERE a.is_active = 1 
AND mp.code IN ('mobile_money', 'virement');

-- ==================== 8. NETTOYAGE (optionnel - à exécuter après validation) ====================
-- Ces colonnes peuvent être supprimées une fois la migration validée
-- ALTER TABLE assurances DROP COLUMN type_couverture;
-- ALTER TABLE assurances DROP COLUMN categorie_beneficiaires;
-- ALTER TABLE assurances DROP COLUMN mode_paiement;
-- ALTER TABLE assurances DROP COLUMN zone_couverture;

-- ==================== 9. MODIFICATION TABLE PATIENT ====================

-- Renommer CouvertureAssurance en taux_couverture_override pour clarifier son rôle
-- C'est un override manuel qui prend priorité sur la config assurance
ALTER TABLE patient 
CHANGE COLUMN couverture_assurance taux_couverture_override DECIMAL(5,2) NULL 
COMMENT 'Override manuel du taux de couverture (priorité sur config assurance)';

-- ==================== 10. INDEX POUR PERFORMANCE ====================

CREATE INDEX IF NOT EXISTS idx_type_prestation_code ON type_prestation(code);
CREATE INDEX IF NOT EXISTS idx_categorie_beneficiaire_code ON categorie_beneficiaire(code);
CREATE INDEX IF NOT EXISTS idx_mode_paiement_code ON mode_paiement(code);
CREATE INDEX IF NOT EXISTS idx_zone_couverture_code ON zone_couverture(code);
CREATE INDEX IF NOT EXISTS idx_type_couverture_sante_code ON type_couverture_sante(code);
