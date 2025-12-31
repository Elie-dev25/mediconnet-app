-- Migration: Ajout id_specialite sur facture + index utiles pour file d'attente médecin
-- Date: 2025-12-15

-- ==================== FACTURE: AJOUT ID_SPECIALITE ====================

SET @column_exists = (
    SELECT COUNT(*) FROM information_schema.columns 
    WHERE table_schema = 'mediconnect' 
    AND table_name = 'facture' 
    AND column_name = 'id_specialite'
);

SET @sql = IF(@column_exists = 0,
    'ALTER TABLE facture ADD COLUMN id_specialite INT NULL AFTER id_service',
    'SELECT "Column id_specialite already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Index pour filtrage rapide facture consultation payée par contexte
SET @idx1_exists = (
    SELECT COUNT(*) FROM information_schema.statistics
    WHERE table_schema = 'mediconnect'
      AND table_name = 'facture'
      AND index_name = 'IX_facture_type_statut_paiement'
);

SET @sql_idx1 = IF(@idx1_exists = 0,
    'CREATE INDEX IX_facture_type_statut_paiement ON facture (type_facture, statut, date_paiement)',
    'SELECT "Index IX_facture_type_statut_paiement already exists"');
PREPARE stmt_idx1 FROM @sql_idx1;
EXECUTE stmt_idx1;
DEALLOCATE PREPARE stmt_idx1;

SET @idx2_exists = (
    SELECT COUNT(*) FROM information_schema.statistics
    WHERE table_schema = 'mediconnect'
      AND table_name = 'facture'
      AND index_name = 'IX_facture_patient_service_specialite_paiement'
);

SET @sql_idx2 = IF(@idx2_exists = 0,
    'CREATE INDEX IX_facture_patient_service_specialite_paiement ON facture (id_patient, id_service, id_specialite, date_paiement)',
    'SELECT "Index IX_facture_patient_service_specialite_paiement already exists"');
PREPARE stmt_idx2 FROM @sql_idx2;
EXECUTE stmt_idx2;
DEALLOCATE PREPARE stmt_idx2;

-- FK vers specialites (si elle n'existe pas déjà)
SET @fk_exists = (
    SELECT COUNT(*) FROM information_schema.table_constraints 
    WHERE table_schema = 'mediconnect'
      AND table_name = 'facture'
      AND constraint_type = 'FOREIGN KEY'
      AND constraint_name = 'FK_facture_specialite'
);

SET @sql_fk = IF(@fk_exists = 0,
    'ALTER TABLE facture ADD CONSTRAINT FK_facture_specialite FOREIGN KEY (id_specialite) REFERENCES specialites(id_specialite) ON DELETE SET NULL',
    'SELECT "FK_facture_specialite already exists"');
PREPARE stmt_fk FROM @sql_fk;
EXECUTE stmt_fk;
DEALLOCATE PREPARE stmt_fk;

SELECT 'Migration 006_add_facture_specialite_and_queue_indexes.sql appliquée avec succès' AS status;
