-- ============================================================================
-- SCRIPT DE MIGRATION POUR BASE EXISTANTE
-- Migration sécurisée des fichiers médicaux (UUID + Intégrité)
-- MediConnect - À exécuter via Adminer ou MySQL CLI
-- ============================================================================
-- 
-- 🛑 INSTRUCTIONS:
-- 1. BACKUP la base de données AVANT d'exécuter ce script
-- 2. Exécuter ce script section par section dans Adminer
-- 3. Vérifier les COUNT(*) avant/après chaque étape
-- 4. En cas d'erreur, utiliser le script de rollback
--
-- ============================================================================

-- ============================================================================
-- ÉTAPE 1: VÉRIFICATION PRÉ-MIGRATION
-- ============================================================================

-- Compter les enregistrements existants (noter ces valeurs)
SELECT 'ÉTAT AVANT MIGRATION' as info;
SELECT 'bulletin_examen avec fichiers' as table_name, COUNT(*) as count FROM bulletin_examen WHERE resultat_fichier IS NOT NULL;
SELECT 'document_dmp avec fichiers' as table_name, COUNT(*) as count FROM document_dmp WHERE chemin_fichier IS NOT NULL;
SELECT 'table fichier' as table_name, COUNT(*) as count FROM fichier;

-- ============================================================================
-- ÉTAPE 2: CRÉATION DES NOUVELLES TABLES
-- ============================================================================

-- Table: documents_medicaux (Stockage centralisé avec UUID)
CREATE TABLE IF NOT EXISTS `documents_medicaux` (
  `uuid` CHAR(36) NOT NULL COMMENT 'Identifiant unique UUID',
  `nom_fichier_original` VARCHAR(255) NOT NULL COMMENT 'Nom original du fichier uploade',
  `nom_fichier_stockage` VARCHAR(255) NOT NULL COMMENT 'Nom du fichier sur le disque (UUID.extension)',
  `chemin_relatif` VARCHAR(500) NOT NULL COMMENT 'Chemin relatif depuis la racine de stockage',
  `extension` VARCHAR(20) DEFAULT NULL COMMENT 'Extension du fichier (.pdf, .jpg, etc.)',
  `mime_type` VARCHAR(100) NOT NULL COMMENT 'Type MIME du fichier',
  `taille_octets` BIGINT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Taille en octets',
  `hash_sha256` CHAR(64) DEFAULT NULL COMMENT 'Hash SHA-256 du contenu pour verification integrite',
  `hash_calcule_at` TIMESTAMP NULL DEFAULT NULL COMMENT 'Date du dernier calcul de hash',
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

-- Table: audit_acces_documents (Traçabilité des accès)
CREATE TABLE IF NOT EXISTS `audit_acces_documents` (
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

-- Table: verification_integrite (Historique des contrôles)
CREATE TABLE IF NOT EXISTS `verification_integrite` (
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

SELECT 'Tables créées avec succès' as status;

-- ============================================================================
-- ÉTAPE 3: AJOUT DES COLONNES UUID AUX TABLES EXISTANTES
-- ============================================================================

-- Ajouter colonne UUID à bulletin_examen (si elle n'existe pas)
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                   WHERE TABLE_SCHEMA = DATABASE() 
                   AND TABLE_NAME = 'bulletin_examen' 
                   AND COLUMN_NAME = 'document_resultat_uuid');

SET @sql = IF(@col_exists = 0, 
  'ALTER TABLE `bulletin_examen` ADD COLUMN `document_resultat_uuid` CHAR(36) DEFAULT NULL COMMENT ''UUID du document resultat dans documents_medicaux'' AFTER `resultat_fichier`',
  'SELECT ''Colonne document_resultat_uuid existe déjà'' as info');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Ajouter index sur la nouvelle colonne
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
                   WHERE TABLE_SCHEMA = DATABASE() 
                   AND TABLE_NAME = 'bulletin_examen' 
                   AND INDEX_NAME = 'idx_bulletin_document_uuid');

SET @sql = IF(@idx_exists = 0, 
  'ALTER TABLE `bulletin_examen` ADD INDEX `idx_bulletin_document_uuid` (`document_resultat_uuid`)',
  'SELECT ''Index idx_bulletin_document_uuid existe déjà'' as info');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Ajouter colonne UUID à document_dmp (si elle n'existe pas)
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                   WHERE TABLE_SCHEMA = DATABASE() 
                   AND TABLE_NAME = 'document_dmp' 
                   AND COLUMN_NAME = 'document_uuid');

SET @sql = IF(@col_exists = 0, 
  'ALTER TABLE `document_dmp` ADD COLUMN `document_uuid` CHAR(36) DEFAULT NULL COMMENT ''UUID du document dans documents_medicaux'' AFTER `chemin_fichier`',
  'SELECT ''Colonne document_uuid existe déjà'' as info');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Ajouter index sur la nouvelle colonne
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
                   WHERE TABLE_SCHEMA = DATABASE() 
                   AND TABLE_NAME = 'document_dmp' 
                   AND INDEX_NAME = 'idx_dmp_document_uuid');

SET @sql = IF(@idx_exists = 0, 
  'ALTER TABLE `document_dmp` ADD INDEX `idx_dmp_document_uuid` (`document_uuid`)',
  'SELECT ''Index idx_dmp_document_uuid existe déjà'' as info');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT 'Colonnes UUID ajoutées avec succès' as status;

-- ============================================================================
-- ÉTAPE 4: AJOUT DES CONTRAINTES DE CLÉS ÉTRANGÈRES
-- ============================================================================

-- FK pour documents_medicaux
ALTER TABLE `documents_medicaux`
  ADD CONSTRAINT `fk_documents_patient` FOREIGN KEY (`id_patient`) REFERENCES `patient` (`id_user`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_documents_consultation` FOREIGN KEY (`id_consultation`) REFERENCES `consultation` (`id_consultation`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_documents_bulletin` FOREIGN KEY (`id_bulletin_examen`) REFERENCES `bulletin_examen` (`id_bull_exam`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_documents_hospitalisation` FOREIGN KEY (`id_hospitalisation`) REFERENCES `hospitalisation` (`id_admission`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_documents_dmp` FOREIGN KEY (`id_dmp`) REFERENCES `dossier_medical_partage` (`id_dmp`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_documents_createur` FOREIGN KEY (`id_createur`) REFERENCES `utilisateurs` (`id_user`) ON DELETE RESTRICT,
  ADD CONSTRAINT `fk_documents_validateur` FOREIGN KEY (`id_validateur`) REFERENCES `utilisateurs` (`id_user`) ON DELETE SET NULL;

-- FK pour audit_acces_documents
ALTER TABLE `audit_acces_documents`
  ADD CONSTRAINT `fk_audit_utilisateur` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateurs` (`id_user`) ON DELETE CASCADE;

-- FK pour bulletin_examen -> documents_medicaux
ALTER TABLE `bulletin_examen`
  ADD CONSTRAINT `fk_bulletin_document_uuid` FOREIGN KEY (`document_resultat_uuid`) REFERENCES `documents_medicaux` (`uuid`) ON DELETE SET NULL ON UPDATE CASCADE;

-- FK pour document_dmp -> documents_medicaux
ALTER TABLE `document_dmp`
  ADD CONSTRAINT `fk_dmp_document_uuid` FOREIGN KEY (`document_uuid`) REFERENCES `documents_medicaux` (`uuid`) ON DELETE SET NULL ON UPDATE CASCADE;

SELECT 'Contraintes FK ajoutées avec succès' as status;

-- ============================================================================
-- ÉTAPE 5: CRÉATION DES VUES DE MONITORING
-- ============================================================================

-- Vue: dashboard_documents
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

-- Vue: documents_problemes
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

-- Vue: statistiques_documents
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

SELECT 'Vues de monitoring créées avec succès' as status;

-- ============================================================================
-- ÉTAPE 6: MISE À JOUR DES PERMISSIONS (biologiste -> laborantin)
-- ============================================================================

-- Supprimer les anciennes permissions biologiste
DELETE FROM `role_permissions` WHERE `role` = 'biologiste';

-- Ajouter les permissions pour laborantin
INSERT IGNORE INTO `role_permissions` (`role`, `id_permission`)
SELECT 'laborantin', `id_permission` FROM `permissions` WHERE `code` IN (
    'patients.view',
    'examens.view', 'examens.result', 'examens.validate'
);

SELECT 'Permissions laborantin mises à jour' as status;

-- ============================================================================
-- ÉTAPE 7: VÉRIFICATION POST-MIGRATION
-- ============================================================================

SELECT 'ÉTAT APRÈS MIGRATION' as info;
SELECT 'documents_medicaux' as table_name, COUNT(*) as count FROM documents_medicaux;
SELECT 'audit_acces_documents' as table_name, COUNT(*) as count FROM audit_acces_documents;
SELECT 'verification_integrite' as table_name, COUNT(*) as count FROM verification_integrite;

-- Vérifier les colonnes ajoutées
SELECT 'bulletin_examen avec UUID' as table_name, 
       COUNT(*) as total,
       SUM(CASE WHEN document_resultat_uuid IS NOT NULL THEN 1 ELSE 0 END) as avec_uuid
FROM bulletin_examen;

SELECT 'document_dmp avec UUID' as table_name,
       COUNT(*) as total,
       SUM(CASE WHEN document_uuid IS NOT NULL THEN 1 ELSE 0 END) as avec_uuid
FROM document_dmp;

SELECT '✅ MIGRATION TERMINÉE AVEC SUCCÈS' as status;

-- ============================================================================
-- FIN DU SCRIPT DE MIGRATION
-- ============================================================================
