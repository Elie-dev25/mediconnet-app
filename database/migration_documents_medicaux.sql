-- ============================================================================
-- MIGRATION SÉCURISÉE DES FICHIERS MÉDICAUX (UUID + INTÉGRITÉ)
-- MediConnect - Version 1.0
-- Date: 2026-02-06
-- ============================================================================
-- 
-- 🛑 PRÉREQUIS ABSOLUS:
-- ❌ NE PAS exécuter en production sans backup
-- ✅ Travailler sur environnement de test d'abord
-- ✅ Utiliser des transactions
-- ✅ Valider chaque migration avec COUNT(*) avant / après
-- ✅ Logger toutes les erreurs
--
-- ============================================================================

-- ============================================================================
-- ÉTAPE 0: BACKUP ET DOCUMENTATION DE L'ÉTAT ACTUEL
-- ============================================================================

-- Créer une table de log pour la migration
CREATE TABLE IF NOT EXISTS `migration_log` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `etape` VARCHAR(100) NOT NULL,
  `description` TEXT,
  `count_avant` INT DEFAULT NULL,
  `count_apres` INT DEFAULT NULL,
  `statut` ENUM('en_cours', 'succes', 'erreur') DEFAULT 'en_cours',
  `message_erreur` TEXT DEFAULT NULL,
  `timestamp` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Documenter l'état initial
INSERT INTO `migration_log` (`etape`, `description`) VALUES 
('BACKUP', 'Début de la migration - État initial documenté');

-- Compter les enregistrements existants
SET @count_bulletin_examen = (SELECT COUNT(*) FROM `bulletin_examen` WHERE `resultat_fichier` IS NOT NULL);
SET @count_document_dmp = (SELECT COUNT(*) FROM `document_dmp` WHERE `chemin_fichier` IS NOT NULL);
SET @count_fichier = (SELECT COUNT(*) FROM `fichier`);

INSERT INTO `migration_log` (`etape`, `description`, `count_avant`) VALUES 
('BACKUP', CONCAT('bulletin_examen avec fichiers: ', @count_bulletin_examen), @count_bulletin_examen),
('BACKUP', CONCAT('document_dmp avec fichiers: ', @count_document_dmp), @count_document_dmp),
('BACKUP', CONCAT('table fichier: ', @count_fichier), @count_fichier);

-- ============================================================================
-- ÉTAPE 1: CRÉATION DES NOUVELLES TABLES
-- ============================================================================

START TRANSACTION;

INSERT INTO `migration_log` (`etape`, `description`) VALUES 
('ETAPE_1', 'Création des nouvelles tables');

-- -----------------------------------------------------------------------------
-- Table principale: documents_medicaux
-- Stockage centralisé de tous les documents médicaux avec UUID
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `documents_medicaux` (
  -- Identifiant unique UUID (clé primaire)
  `uuid` CHAR(36) NOT NULL,
  
  -- Métadonnées du fichier
  `nom_fichier_original` VARCHAR(255) NOT NULL COMMENT 'Nom original du fichier uploadé',
  `nom_fichier_stockage` VARCHAR(255) NOT NULL COMMENT 'Nom du fichier sur le disque (UUID.extension)',
  `chemin_relatif` VARCHAR(500) NOT NULL COMMENT 'Chemin relatif depuis la racine de stockage',
  `extension` VARCHAR(20) DEFAULT NULL COMMENT 'Extension du fichier (.pdf, .jpg, etc.)',
  `mime_type` VARCHAR(100) NOT NULL COMMENT 'Type MIME du fichier',
  `taille_octets` BIGINT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Taille en octets',
  
  -- Intégrité
  `hash_sha256` CHAR(64) DEFAULT NULL COMMENT 'Hash SHA-256 du contenu pour vérification intégrité',
  `hash_calcule_at` TIMESTAMP NULL DEFAULT NULL COMMENT 'Date du dernier calcul de hash',
  
  -- Classification du document
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
  ) NOT NULL DEFAULT 'autre' COMMENT 'Type de document médical',
  
  `sous_type` VARCHAR(100) DEFAULT NULL COMMENT 'Sous-type spécifique (ex: radiographie, IRM, etc.)',
  
  -- Confidentialité et accès
  `niveau_confidentialite` ENUM('normal', 'sensible', 'tres_sensible') NOT NULL DEFAULT 'normal',
  `acces_patient` BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Le patient peut-il voir ce document?',
  `acces_restreint_roles` JSON DEFAULT NULL COMMENT 'Rôles autorisés si accès restreint',
  
  -- Relations (liens avec les entités métier)
  `id_patient` INT NOT NULL COMMENT 'Patient propriétaire du document',
  `id_consultation` INT DEFAULT NULL COMMENT 'Consultation associée',
  `id_bulletin_examen` INT DEFAULT NULL COMMENT 'Bulletin d examen associé',
  `id_hospitalisation` INT DEFAULT NULL COMMENT 'Hospitalisation associée',
  `id_dmp` INT DEFAULT NULL COMMENT 'DMP associé',
  
  -- Traçabilité
  `id_createur` INT NOT NULL COMMENT 'Utilisateur ayant uploadé le document',
  `id_validateur` INT DEFAULT NULL COMMENT 'Utilisateur ayant validé le document',
  `date_validation` TIMESTAMP NULL DEFAULT NULL,
  
  -- Versioning
  `version` INT UNSIGNED NOT NULL DEFAULT 1 COMMENT 'Numéro de version',
  `uuid_version_precedente` CHAR(36) DEFAULT NULL COMMENT 'UUID de la version précédente',
  `est_version_courante` BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Est-ce la version active?',
  
  -- Métadonnées temporelles
  `date_document` DATE DEFAULT NULL COMMENT 'Date du document (peut différer de la date upload)',
  `description` TEXT DEFAULT NULL COMMENT 'Description libre du document',
  `tags` JSON DEFAULT NULL COMMENT 'Tags pour recherche',
  
  -- Statut
  `statut` ENUM('actif', 'archive', 'supprime', 'quarantaine') NOT NULL DEFAULT 'actif',
  `date_archivage` TIMESTAMP NULL DEFAULT NULL,
  `motif_archivage` VARCHAR(500) DEFAULT NULL,
  
  -- Timestamps
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  
  PRIMARY KEY (`uuid`),
  
  -- Index pour les recherches fréquentes
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
  INDEX `idx_documents_version_courante` (`est_version_courante`),
  
  -- Contraintes de clés étrangères
  CONSTRAINT `fk_documents_patient` FOREIGN KEY (`id_patient`) 
    REFERENCES `patient` (`id_user`) ON DELETE CASCADE,
  CONSTRAINT `fk_documents_consultation` FOREIGN KEY (`id_consultation`) 
    REFERENCES `consultation` (`id_consultation`) ON DELETE SET NULL,
  CONSTRAINT `fk_documents_bulletin` FOREIGN KEY (`id_bulletin_examen`) 
    REFERENCES `bulletin_examen` (`id_bull_exam`) ON DELETE SET NULL,
  CONSTRAINT `fk_documents_hospitalisation` FOREIGN KEY (`id_hospitalisation`) 
    REFERENCES `hospitalisation` (`id_admission`) ON DELETE SET NULL,
  CONSTRAINT `fk_documents_dmp` FOREIGN KEY (`id_dmp`) 
    REFERENCES `dossier_medical_partage` (`id_dmp`) ON DELETE SET NULL,
  CONSTRAINT `fk_documents_createur` FOREIGN KEY (`id_createur`) 
    REFERENCES `utilisateurs` (`id_user`) ON DELETE RESTRICT,
  CONSTRAINT `fk_documents_validateur` FOREIGN KEY (`id_validateur`) 
    REFERENCES `utilisateurs` (`id_user`) ON DELETE SET NULL
    
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Table centralisée des documents médicaux avec UUID et intégrité';

-- -----------------------------------------------------------------------------
-- Table: audit_acces_documents
-- Traçabilité complète de tous les accès aux documents
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `audit_acces_documents` (
  `id_audit` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
  
  -- Document concerné
  `document_uuid` CHAR(36) NOT NULL COMMENT 'UUID du document accédé',
  
  -- Utilisateur
  `id_utilisateur` INT NOT NULL COMMENT 'Utilisateur ayant effectué l action',
  `role_utilisateur` VARCHAR(50) NOT NULL COMMENT 'Rôle au moment de l accès',
  
  -- Action
  `type_action` ENUM(
    'consultation',      -- Visualisation
    'telechargement',    -- Téléchargement
    'impression',        -- Impression
    'creation',          -- Upload initial
    'modification',      -- Modification métadonnées
    'suppression',       -- Suppression (soft delete)
    'restauration',      -- Restauration après suppression
    'archivage',         -- Mise en archive
    'partage',           -- Partage avec autre professionnel
    'verification',      -- Vérification d intégrité
    'tentative_non_autorisee' -- Tentative d accès refusée
  ) NOT NULL,
  
  -- Résultat
  `autorise` BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'L accès a-t-il été autorisé?',
  `motif_refus` VARCHAR(255) DEFAULT NULL COMMENT 'Raison du refus si non autorisé',
  
  -- Contexte technique
  `ip_address` VARCHAR(45) DEFAULT NULL COMMENT 'Adresse IP (IPv4 ou IPv6)',
  `user_agent` VARCHAR(500) DEFAULT NULL COMMENT 'User-Agent du navigateur',
  `session_id` VARCHAR(100) DEFAULT NULL COMMENT 'ID de session',
  `endpoint_api` VARCHAR(255) DEFAULT NULL COMMENT 'Endpoint API appelé',
  
  -- Contexte métier
  `contexte` JSON DEFAULT NULL COMMENT 'Contexte additionnel (consultation en cours, etc.)',
  
  -- Timestamp
  `timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  
  -- Index pour les recherches et rapports
  INDEX `idx_audit_document` (`document_uuid`),
  INDEX `idx_audit_utilisateur` (`id_utilisateur`),
  INDEX `idx_audit_action` (`type_action`),
  INDEX `idx_audit_timestamp` (`timestamp`),
  INDEX `idx_audit_autorise` (`autorise`),
  INDEX `idx_audit_ip` (`ip_address`),
  
  -- Pas de FK sur document_uuid car on veut garder l'audit même si document supprimé
  CONSTRAINT `fk_audit_utilisateur` FOREIGN KEY (`id_utilisateur`) 
    REFERENCES `utilisateurs` (`id_user`) ON DELETE CASCADE
    
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Journal d audit des accès aux documents médicaux';

-- -----------------------------------------------------------------------------
-- Table: verification_integrite
-- Historique des contrôles d'intégrité des fichiers
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `verification_integrite` (
  `id_verification` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
  
  -- Document vérifié
  `document_uuid` CHAR(36) NOT NULL COMMENT 'UUID du document vérifié',
  
  -- Résultat de la vérification
  `statut_verification` ENUM(
    'ok',               -- Fichier présent et hash valide
    'hash_invalide',    -- Fichier présent mais hash différent (corruption)
    'fichier_absent',   -- Fichier introuvable sur le disque
    'erreur_lecture',   -- Erreur lors de la lecture du fichier
    'hash_non_calcule'  -- Hash de référence non disponible
  ) NOT NULL,
  
  -- Détails
  `hash_attendu` CHAR(64) DEFAULT NULL COMMENT 'Hash SHA-256 attendu (stocké)',
  `hash_calcule` CHAR(64) DEFAULT NULL COMMENT 'Hash SHA-256 calculé lors de la vérification',
  `taille_attendue` BIGINT UNSIGNED DEFAULT NULL,
  `taille_reelle` BIGINT UNSIGNED DEFAULT NULL,
  
  -- Contexte
  `type_verification` ENUM('automatique', 'manuelle', 'restauration') NOT NULL DEFAULT 'automatique',
  `id_declencheur` INT DEFAULT NULL COMMENT 'Utilisateur ayant déclenché (si manuelle)',
  
  -- Action corrective
  `action_corrective` VARCHAR(255) DEFAULT NULL COMMENT 'Action prise en cas de problème',
  `alerte_envoyee` BOOLEAN NOT NULL DEFAULT FALSE,
  `date_alerte` TIMESTAMP NULL DEFAULT NULL,
  
  -- Timestamp
  `timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  
  -- Index
  INDEX `idx_verif_document` (`document_uuid`),
  INDEX `idx_verif_statut` (`statut_verification`),
  INDEX `idx_verif_timestamp` (`timestamp`),
  INDEX `idx_verif_type` (`type_verification`)
  
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Historique des vérifications d intégrité des documents';

-- Log de succès étape 1
UPDATE `migration_log` SET `statut` = 'succes' 
WHERE `etape` = 'ETAPE_1' AND `statut` = 'en_cours';

INSERT INTO `migration_log` (`etape`, `description`, `statut`) VALUES 
('ETAPE_1', 'Tables documents_medicaux, audit_acces_documents, verification_integrite créées', 'succes');

COMMIT;

-- ============================================================================
-- ÉTAPE 2: AJOUT DES COLONNES UUID AUX TABLES EXISTANTES
-- ============================================================================

START TRANSACTION;

INSERT INTO `migration_log` (`etape`, `description`) VALUES 
('ETAPE_2', 'Ajout des colonnes UUID aux tables existantes');

-- Ajouter colonne UUID à bulletin_examen
ALTER TABLE `bulletin_examen` 
ADD COLUMN IF NOT EXISTS `document_resultat_uuid` CHAR(36) DEFAULT NULL 
COMMENT 'UUID du document résultat dans documents_medicaux'
AFTER `resultat_fichier`;

-- Ajouter colonne UUID à document_dmp
ALTER TABLE `document_dmp` 
ADD COLUMN IF NOT EXISTS `document_uuid` CHAR(36) DEFAULT NULL 
COMMENT 'UUID du document dans documents_medicaux'
AFTER `chemin_fichier`;

-- Ajouter index sur les nouvelles colonnes
ALTER TABLE `bulletin_examen` 
ADD INDEX IF NOT EXISTS `idx_bulletin_document_uuid` (`document_resultat_uuid`);

ALTER TABLE `document_dmp` 
ADD INDEX IF NOT EXISTS `idx_dmp_document_uuid` (`document_uuid`);

UPDATE `migration_log` SET `statut` = 'succes' 
WHERE `etape` = 'ETAPE_2' AND `statut` = 'en_cours';

INSERT INTO `migration_log` (`etape`, `description`, `statut`) VALUES 
('ETAPE_2', 'Colonnes document_resultat_uuid et document_uuid ajoutées', 'succes');

COMMIT;

-- ============================================================================
-- ÉTAPE 2b: MIGRATION DES DONNÉES EXISTANTES
-- ============================================================================
-- NOTE: Cette partie génère les UUIDs et crée les entrées dans documents_medicaux
-- Le calcul réel du hash SHA-256 sera fait côté application

START TRANSACTION;

INSERT INTO `migration_log` (`etape`, `description`) VALUES 
('ETAPE_2b', 'Migration des données existantes vers documents_medicaux');

-- Migrer les fichiers de bulletin_examen
INSERT INTO `documents_medicaux` (
  `uuid`,
  `nom_fichier_original`,
  `nom_fichier_stockage`,
  `chemin_relatif`,
  `extension`,
  `mime_type`,
  `taille_octets`,
  `hash_sha256`,
  `type_document`,
  `sous_type`,
  `niveau_confidentialite`,
  `id_patient`,
  `id_consultation`,
  `id_bulletin_examen`,
  `id_createur`,
  `date_document`,
  `description`,
  `statut`,
  `created_at`
)
SELECT 
  UUID() as `uuid`,
  COALESCE(SUBSTRING_INDEX(be.resultat_fichier, '/', -1), 'document_inconnu') as `nom_fichier_original`,
  CONCAT(UUID(), '.', COALESCE(SUBSTRING_INDEX(be.resultat_fichier, '.', -1), 'pdf')) as `nom_fichier_stockage`,
  be.resultat_fichier as `chemin_relatif`,
  LOWER(SUBSTRING_INDEX(be.resultat_fichier, '.', -1)) as `extension`,
  CASE 
    WHEN LOWER(SUBSTRING_INDEX(be.resultat_fichier, '.', -1)) = 'pdf' THEN 'application/pdf'
    WHEN LOWER(SUBSTRING_INDEX(be.resultat_fichier, '.', -1)) IN ('jpg', 'jpeg') THEN 'image/jpeg'
    WHEN LOWER(SUBSTRING_INDEX(be.resultat_fichier, '.', -1)) = 'png' THEN 'image/png'
    WHEN LOWER(SUBSTRING_INDEX(be.resultat_fichier, '.', -1)) = 'dicom' THEN 'application/dicom'
    ELSE 'application/octet-stream'
  END as `mime_type`,
  0 as `taille_octets`, -- Sera calculé par l'application
  NULL as `hash_sha256`, -- Sera calculé par l'application
  'resultat_examen' as `type_document`,
  COALESCE(e.nom_exam, 'Examen') as `sous_type`,
  'sensible' as `niveau_confidentialite`,
  c.id_patient,
  be.id_consultation,
  be.id_bull_exam,
  COALESCE(be.id_biologiste, c.id_medecin, 1) as `id_createur`,
  COALESCE(be.date_resultat, be.date_demande) as `date_document`,
  CONCAT('Résultat examen: ', COALESCE(e.nom_exam, 'Non spécifié')) as `description`,
  'actif' as `statut`,
  COALESCE(be.date_resultat, NOW()) as `created_at`
FROM `bulletin_examen` be
LEFT JOIN `consultation` c ON be.id_consultation = c.id_consultation
LEFT JOIN `examens` e ON be.id_exam = e.id_exam
WHERE be.resultat_fichier IS NOT NULL 
  AND be.resultat_fichier != ''
  AND be.document_resultat_uuid IS NULL;

-- Mettre à jour bulletin_examen avec les UUIDs générés
UPDATE `bulletin_examen` be
INNER JOIN `documents_medicaux` dm ON dm.id_bulletin_examen = be.id_bull_exam
SET be.document_resultat_uuid = dm.uuid
WHERE be.document_resultat_uuid IS NULL;

-- Compter les migrations bulletin_examen
SET @count_migrated_bulletin = (SELECT COUNT(*) FROM `bulletin_examen` WHERE `document_resultat_uuid` IS NOT NULL);

INSERT INTO `migration_log` (`etape`, `description`, `count_apres`, `statut`) VALUES 
('ETAPE_2b', CONCAT('bulletin_examen migrés: ', @count_migrated_bulletin), @count_migrated_bulletin, 'succes');

-- Migrer les fichiers de document_dmp
INSERT INTO `documents_medicaux` (
  `uuid`,
  `nom_fichier_original`,
  `nom_fichier_stockage`,
  `chemin_relatif`,
  `extension`,
  `mime_type`,
  `taille_octets`,
  `hash_sha256`,
  `type_document`,
  `niveau_confidentialite`,
  `id_patient`,
  `id_dmp`,
  `id_createur`,
  `date_document`,
  `description`,
  `statut`,
  `created_at`
)
SELECT 
  UUID() as `uuid`,
  COALESCE(dd.titre, SUBSTRING_INDEX(dd.chemin_fichier, '/', -1), 'document_dmp') as `nom_fichier_original`,
  CONCAT(UUID(), '.', COALESCE(SUBSTRING_INDEX(dd.chemin_fichier, '.', -1), 'pdf')) as `nom_fichier_stockage`,
  dd.chemin_fichier as `chemin_relatif`,
  LOWER(SUBSTRING_INDEX(dd.chemin_fichier, '.', -1)) as `extension`,
  COALESCE(dd.mime_type, 
    CASE 
      WHEN LOWER(SUBSTRING_INDEX(dd.chemin_fichier, '.', -1)) = 'pdf' THEN 'application/pdf'
      WHEN LOWER(SUBSTRING_INDEX(dd.chemin_fichier, '.', -1)) IN ('jpg', 'jpeg') THEN 'image/jpeg'
      WHEN LOWER(SUBSTRING_INDEX(dd.chemin_fichier, '.', -1)) = 'png' THEN 'image/png'
      ELSE 'application/octet-stream'
    END
  ) as `mime_type`,
  COALESCE(dd.taille_fichier, 0) as `taille_octets`,
  dd.hash_fichier as `hash_sha256`,
  CASE dd.type_document
    WHEN 'resultat_examen' THEN 'resultat_examen'
    WHEN 'ordonnance' THEN 'ordonnance'
    WHEN 'certificat' THEN 'certificat_medical'
    WHEN 'compte_rendu' THEN 'compte_rendu_hospitalisation'
    ELSE 'document_externe'
  END as `type_document`,
  'sensible' as `niveau_confidentialite`,
  dmp.id_patient,
  dd.id_dmp,
  COALESCE(dd.id_createur, 1) as `id_createur`,
  COALESCE(dd.date_document, dd.date_creation) as `date_document`,
  dd.description as `description`,
  'actif' as `statut`,
  COALESCE(dd.date_creation, NOW()) as `created_at`
FROM `document_dmp` dd
INNER JOIN `dossier_medical_partage` dmp ON dd.id_dmp = dmp.id_dmp
WHERE dd.chemin_fichier IS NOT NULL 
  AND dd.chemin_fichier != ''
  AND dd.document_uuid IS NULL;

-- Mettre à jour document_dmp avec les UUIDs générés
UPDATE `document_dmp` dd
INNER JOIN `documents_medicaux` dm ON dm.id_dmp = dd.id_dmp 
  AND dm.chemin_relatif = dd.chemin_fichier
SET dd.document_uuid = dm.uuid
WHERE dd.document_uuid IS NULL;

-- Compter les migrations document_dmp
SET @count_migrated_dmp = (SELECT COUNT(*) FROM `document_dmp` WHERE `document_uuid` IS NOT NULL);

INSERT INTO `migration_log` (`etape`, `description`, `count_apres`, `statut`) VALUES 
('ETAPE_2b', CONCAT('document_dmp migrés: ', @count_migrated_dmp), @count_migrated_dmp, 'succes');

COMMIT;

-- ============================================================================
-- ÉTAPE 3: AJOUT DES CONTRAINTES DE CLÉS ÉTRANGÈRES
-- ============================================================================

START TRANSACTION;

INSERT INTO `migration_log` (`etape`, `description`) VALUES 
('ETAPE_3', 'Ajout des contraintes de clés étrangères');

-- FK bulletin_examen -> documents_medicaux
-- Note: On utilise SET NULL car le document peut être supprimé indépendamment
ALTER TABLE `bulletin_examen`
ADD CONSTRAINT `fk_bulletin_document_uuid` 
FOREIGN KEY (`document_resultat_uuid`) REFERENCES `documents_medicaux` (`uuid`) 
ON DELETE SET NULL ON UPDATE CASCADE;

-- FK document_dmp -> documents_medicaux
ALTER TABLE `document_dmp`
ADD CONSTRAINT `fk_dmp_document_uuid` 
FOREIGN KEY (`document_uuid`) REFERENCES `documents_medicaux` (`uuid`) 
ON DELETE SET NULL ON UPDATE CASCADE;

UPDATE `migration_log` SET `statut` = 'succes' 
WHERE `etape` = 'ETAPE_3' AND `statut` = 'en_cours';

INSERT INTO `migration_log` (`etape`, `description`, `statut`) VALUES 
('ETAPE_3', 'Contraintes FK ajoutées sur bulletin_examen et document_dmp', 'succes');

COMMIT;

-- ============================================================================
-- ÉTAPE 4: CRÉATION DES VUES DE MONITORING
-- ============================================================================

INSERT INTO `migration_log` (`etape`, `description`) VALUES 
('ETAPE_4', 'Création des vues de monitoring');

-- -----------------------------------------------------------------------------
-- Vue: dashboard_documents
-- Tableau de bord des documents médicaux
-- -----------------------------------------------------------------------------
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
  
  -- Patient
  dm.id_patient,
  CONCAT(u_patient.prenom, ' ', u_patient.nom) as patient_nom,
  p.numero_dossier as patient_dossier,
  
  -- Créateur
  dm.id_createur,
  CONCAT(u_createur.prenom, ' ', u_createur.nom) as createur_nom,
  u_createur.role as createur_role,
  
  -- Statistiques d'accès
  (SELECT COUNT(*) FROM audit_acces_documents aad WHERE aad.document_uuid = dm.uuid) as nb_acces_total,
  (SELECT COUNT(*) FROM audit_acces_documents aad WHERE aad.document_uuid = dm.uuid AND aad.type_action = 'telechargement') as nb_telechargements,
  (SELECT MAX(timestamp) FROM audit_acces_documents aad WHERE aad.document_uuid = dm.uuid) as dernier_acces,
  
  -- Dernière vérification d'intégrité
  (SELECT vi.statut_verification FROM verification_integrite vi WHERE vi.document_uuid = dm.uuid ORDER BY vi.timestamp DESC LIMIT 1) as derniere_verif_statut,
  (SELECT vi.timestamp FROM verification_integrite vi WHERE vi.document_uuid = dm.uuid ORDER BY vi.timestamp DESC LIMIT 1) as derniere_verif_date

FROM `documents_medicaux` dm
INNER JOIN `patient` p ON dm.id_patient = p.id_user
INNER JOIN `utilisateurs` u_patient ON p.id_user = u_patient.id_user
INNER JOIN `utilisateurs` u_createur ON dm.id_createur = u_createur.id_user
WHERE dm.est_version_courante = TRUE;

-- -----------------------------------------------------------------------------
-- Vue: documents_problemes
-- Documents avec problèmes d'intégrité ou manquants
-- -----------------------------------------------------------------------------
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
    WHEN 'hash_non_calcule' THEN 'INFO - Hash non calculé'
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

-- -----------------------------------------------------------------------------
-- Vue: statistiques_documents
-- Statistiques globales sur les documents
-- -----------------------------------------------------------------------------
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

INSERT INTO `migration_log` (`etape`, `description`, `statut`) VALUES 
('ETAPE_4', 'Vues v_dashboard_documents, v_documents_problemes, v_statistiques_documents créées', 'succes');

-- ============================================================================
-- ÉTAPE 5: PROCÉDURE DE VÉRIFICATION D'INTÉGRITÉ
-- ============================================================================

INSERT INTO `migration_log` (`etape`, `description`) VALUES 
('ETAPE_5', 'Création de la procédure de vérification d intégrité');

DELIMITER //

-- -----------------------------------------------------------------------------
-- Procédure: sp_enregistrer_verification_integrite
-- Enregistre le résultat d'une vérification d'intégrité (appelée par l'application)
-- -----------------------------------------------------------------------------
CREATE PROCEDURE IF NOT EXISTS `sp_enregistrer_verification_integrite`(
  IN p_document_uuid CHAR(36),
  IN p_statut_verification VARCHAR(20),
  IN p_hash_calcule CHAR(64),
  IN p_taille_reelle BIGINT UNSIGNED,
  IN p_type_verification VARCHAR(20),
  IN p_id_declencheur INT
)
BEGIN
  DECLARE v_hash_attendu CHAR(64);
  DECLARE v_taille_attendue BIGINT UNSIGNED;
  
  -- Récupérer les valeurs attendues
  SELECT hash_sha256, taille_octets 
  INTO v_hash_attendu, v_taille_attendue
  FROM documents_medicaux 
  WHERE uuid = p_document_uuid;
  
  -- Enregistrer la vérification
  INSERT INTO verification_integrite (
    document_uuid,
    statut_verification,
    hash_attendu,
    hash_calcule,
    taille_attendue,
    taille_reelle,
    type_verification,
    id_declencheur
  ) VALUES (
    p_document_uuid,
    p_statut_verification,
    v_hash_attendu,
    p_hash_calcule,
    v_taille_attendue,
    p_taille_reelle,
    p_type_verification,
    p_id_declencheur
  );
  
  -- Si le hash est valide et différent de celui stocké, mettre à jour
  IF p_statut_verification = 'ok' AND p_hash_calcule IS NOT NULL THEN
    UPDATE documents_medicaux 
    SET hash_sha256 = p_hash_calcule,
        hash_calcule_at = NOW(),
        taille_octets = COALESCE(p_taille_reelle, taille_octets)
    WHERE uuid = p_document_uuid 
      AND (hash_sha256 IS NULL OR hash_sha256 != p_hash_calcule);
  END IF;
  
  -- Si problème détecté, mettre le document en quarantaine
  IF p_statut_verification IN ('hash_invalide', 'fichier_absent') THEN
    UPDATE documents_medicaux 
    SET statut = 'quarantaine'
    WHERE uuid = p_document_uuid AND statut = 'actif';
  END IF;
  
END //

-- -----------------------------------------------------------------------------
-- Procédure: sp_obtenir_documents_a_verifier
-- Retourne la liste des documents à vérifier (pour job quotidien)
-- -----------------------------------------------------------------------------
CREATE PROCEDURE IF NOT EXISTS `sp_obtenir_documents_a_verifier`(
  IN p_limite INT,
  IN p_jours_depuis_derniere_verif INT
)
BEGIN
  SELECT 
    dm.uuid,
    dm.chemin_relatif,
    dm.hash_sha256 as hash_attendu,
    dm.taille_octets as taille_attendue,
    (SELECT MAX(vi.timestamp) FROM verification_integrite vi WHERE vi.document_uuid = dm.uuid) as derniere_verification
  FROM documents_medicaux dm
  WHERE dm.statut = 'actif'
    AND dm.est_version_courante = TRUE
    AND (
      -- Jamais vérifié
      NOT EXISTS (SELECT 1 FROM verification_integrite vi WHERE vi.document_uuid = dm.uuid)
      OR
      -- Dernière vérification trop ancienne
      (SELECT MAX(vi.timestamp) FROM verification_integrite vi WHERE vi.document_uuid = dm.uuid) 
        < DATE_SUB(NOW(), INTERVAL p_jours_depuis_derniere_verif DAY)
    )
  ORDER BY 
    -- Priorité aux documents jamais vérifiés
    CASE WHEN NOT EXISTS (SELECT 1 FROM verification_integrite vi WHERE vi.document_uuid = dm.uuid) THEN 0 ELSE 1 END,
    dm.created_at ASC
  LIMIT p_limite;
END //

-- -----------------------------------------------------------------------------
-- Procédure: sp_audit_acces_document
-- Enregistre un accès à un document
-- -----------------------------------------------------------------------------
CREATE PROCEDURE IF NOT EXISTS `sp_audit_acces_document`(
  IN p_document_uuid CHAR(36),
  IN p_id_utilisateur INT,
  IN p_role_utilisateur VARCHAR(50),
  IN p_type_action VARCHAR(30),
  IN p_autorise BOOLEAN,
  IN p_motif_refus VARCHAR(255),
  IN p_ip_address VARCHAR(45),
  IN p_user_agent VARCHAR(500),
  IN p_session_id VARCHAR(100),
  IN p_endpoint_api VARCHAR(255),
  IN p_contexte JSON
)
BEGIN
  INSERT INTO audit_acces_documents (
    document_uuid,
    id_utilisateur,
    role_utilisateur,
    type_action,
    autorise,
    motif_refus,
    ip_address,
    user_agent,
    session_id,
    endpoint_api,
    contexte
  ) VALUES (
    p_document_uuid,
    p_id_utilisateur,
    p_role_utilisateur,
    p_type_action,
    p_autorise,
    p_motif_refus,
    p_ip_address,
    p_user_agent,
    p_session_id,
    p_endpoint_api,
    p_contexte
  );
END //

-- -----------------------------------------------------------------------------
-- Fonction: fn_verifier_acces_document
-- Vérifie si un utilisateur a accès à un document
-- -----------------------------------------------------------------------------
CREATE FUNCTION IF NOT EXISTS `fn_verifier_acces_document`(
  p_document_uuid CHAR(36),
  p_id_utilisateur INT,
  p_role_utilisateur VARCHAR(50)
) RETURNS BOOLEAN
DETERMINISTIC
READS SQL DATA
BEGIN
  DECLARE v_id_patient INT;
  DECLARE v_acces_patient BOOLEAN;
  DECLARE v_niveau_confidentialite VARCHAR(20);
  DECLARE v_acces_restreint_roles JSON;
  DECLARE v_statut VARCHAR(20);
  
  -- Récupérer les infos du document
  SELECT id_patient, acces_patient, niveau_confidentialite, acces_restreint_roles, statut
  INTO v_id_patient, v_acces_patient, v_niveau_confidentialite, v_acces_restreint_roles, v_statut
  FROM documents_medicaux
  WHERE uuid = p_document_uuid;
  
  -- Document inexistant ou supprimé
  IF v_id_patient IS NULL OR v_statut IN ('supprime', 'quarantaine') THEN
    RETURN FALSE;
  END IF;
  
  -- Administrateur a toujours accès
  IF p_role_utilisateur = 'administrateur' THEN
    RETURN TRUE;
  END IF;
  
  -- Patient peut voir ses propres documents si autorisé
  IF p_role_utilisateur = 'patient' THEN
    RETURN v_acces_patient AND v_id_patient = p_id_utilisateur;
  END IF;
  
  -- Médecins, infirmiers, laborantins ont accès aux documents de leurs patients
  IF p_role_utilisateur IN ('medecin', 'infirmier', 'laborantin', 'pharmacien') THEN
    -- Vérifier si rôles restreints
    IF v_acces_restreint_roles IS NOT NULL THEN
      RETURN JSON_CONTAINS(v_acces_restreint_roles, CONCAT('"', p_role_utilisateur, '"'));
    END IF;
    RETURN TRUE;
  END IF;
  
  RETURN FALSE;
END //

DELIMITER ;

INSERT INTO `migration_log` (`etape`, `description`, `statut`) VALUES 
('ETAPE_5', 'Procédures sp_enregistrer_verification_integrite, sp_obtenir_documents_a_verifier, sp_audit_acces_document et fonction fn_verifier_acces_document créées', 'succes');

-- ============================================================================
-- ÉTAPE FINALE: RAPPORT DE MIGRATION
-- ============================================================================

INSERT INTO `migration_log` (`etape`, `description`) VALUES 
('RAPPORT', 'Génération du rapport de migration');

-- Rapport final
SELECT 
  '=== RAPPORT DE MIGRATION ===' as titre
UNION ALL
SELECT CONCAT('Timestamp: ', NOW())
UNION ALL
SELECT ''
UNION ALL
SELECT '--- TABLES CRÉÉES ---'
UNION ALL
SELECT '✅ documents_medicaux'
UNION ALL
SELECT '✅ audit_acces_documents'
UNION ALL
SELECT '✅ verification_integrite'
UNION ALL
SELECT ''
UNION ALL
SELECT '--- COLONNES AJOUTÉES ---'
UNION ALL
SELECT '✅ bulletin_examen.document_resultat_uuid'
UNION ALL
SELECT '✅ document_dmp.document_uuid'
UNION ALL
SELECT ''
UNION ALL
SELECT '--- VUES CRÉÉES ---'
UNION ALL
SELECT '✅ v_dashboard_documents'
UNION ALL
SELECT '✅ v_documents_problemes'
UNION ALL
SELECT '✅ v_statistiques_documents'
UNION ALL
SELECT ''
UNION ALL
SELECT '--- PROCÉDURES CRÉÉES ---'
UNION ALL
SELECT '✅ sp_enregistrer_verification_integrite'
UNION ALL
SELECT '✅ sp_obtenir_documents_a_verifier'
UNION ALL
SELECT '✅ sp_audit_acces_document'
UNION ALL
SELECT '✅ fn_verifier_acces_document';

-- Statistiques de migration
SELECT 
  'STATISTIQUES DE MIGRATION' as categorie,
  '' as valeur
UNION ALL
SELECT 
  'Documents dans documents_medicaux',
  CAST((SELECT COUNT(*) FROM documents_medicaux) AS CHAR)
UNION ALL
SELECT 
  'bulletin_examen avec UUID',
  CAST((SELECT COUNT(*) FROM bulletin_examen WHERE document_resultat_uuid IS NOT NULL) AS CHAR)
UNION ALL
SELECT 
  'document_dmp avec UUID',
  CAST((SELECT COUNT(*) FROM document_dmp WHERE document_uuid IS NOT NULL) AS CHAR);

INSERT INTO `migration_log` (`etape`, `description`, `statut`) VALUES 
('RAPPORT', 'Migration terminée avec succès', 'succes');

-- ============================================================================
-- FIN DE LA MIGRATION
-- ============================================================================
