-- Migration: Ajout de la table assurance_couverture
-- Permet de définir des taux de couverture par type de prestation pour chaque assurance

CREATE TABLE IF NOT EXISTS assurance_couverture (
    id_couverture INT AUTO_INCREMENT PRIMARY KEY,
    id_assurance INT NOT NULL,
    type_prestation VARCHAR(50) NOT NULL COMMENT 'consultation, hospitalisation, examen, pharmacie',
    taux_couverture DECIMAL(5,2) NOT NULL DEFAULT 0 COMMENT 'Pourcentage de couverture (0-100)',
    plafond_annuel DECIMAL(12,2) NULL COMMENT 'Plafond annuel de remboursement (null = illimité)',
    plafond_par_acte DECIMAL(12,2) NULL COMMENT 'Plafond par acte/facture (null = illimité)',
    franchise DECIMAL(12,2) NULL COMMENT 'Montant minimum non couvert (null = pas de franchise)',
    actif TINYINT(1) NOT NULL DEFAULT 1,
    notes VARCHAR(500) NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NULL,
    CONSTRAINT fk_assurance_couverture_assurance FOREIGN KEY (id_assurance) REFERENCES assurances(id_assurance),
    CONSTRAINT uq_assurance_type_prestation UNIQUE (id_assurance, type_prestation)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Insérer des couvertures par défaut pour les assurances existantes
-- Par défaut: consultation 80%, hospitalisation 70%, examen 60%, pharmacie 50%
INSERT INTO assurance_couverture (id_assurance, type_prestation, taux_couverture, actif)
SELECT a.id_assurance, 'consultation', 80.00, 1 FROM assurances a WHERE a.is_active = 1
ON DUPLICATE KEY UPDATE taux_couverture = VALUES(taux_couverture);

INSERT INTO assurance_couverture (id_assurance, type_prestation, taux_couverture, actif)
SELECT a.id_assurance, 'hospitalisation', 70.00, 1 FROM assurances a WHERE a.is_active = 1
ON DUPLICATE KEY UPDATE taux_couverture = VALUES(taux_couverture);

INSERT INTO assurance_couverture (id_assurance, type_prestation, taux_couverture, actif)
SELECT a.id_assurance, 'examen', 60.00, 1 FROM assurances a WHERE a.is_active = 1
ON DUPLICATE KEY UPDATE taux_couverture = VALUES(taux_couverture);

INSERT INTO assurance_couverture (id_assurance, type_prestation, taux_couverture, actif)
SELECT a.id_assurance, 'pharmacie', 50.00, 1 FROM assurances a WHERE a.is_active = 1
ON DUPLICATE KEY UPDATE taux_couverture = VALUES(taux_couverture);
