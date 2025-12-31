-- Migration: Ajout id_consultation sur facture + id_rdv sur consultation (liaison workflow infirmier)
-- Date: 2025-12-16

-- ==================== FACTURE: AJOUT ID_CONSULTATION ====================

SET @column_exists = (
    SELECT COUNT(*) FROM information_schema.columns 
    WHERE table_schema = 'mediconnect' 
    AND table_name = 'facture' 
    AND column_name = 'id_consultation'
);

SET @sql = IF(@column_exists = 0,
    'ALTER TABLE facture ADD COLUMN id_consultation INT NULL AFTER id_specialite',
    'SELECT "Column id_consultation already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Index utile (recherche facture par consultation)
SET @idx_exists = (
    SELECT COUNT(*) FROM information_schema.statistics
    WHERE table_schema = 'mediconnect'
      AND table_name = 'facture'
      AND index_name = 'IX_facture_consultation'
);

SET @sql_idx = IF(@idx_exists = 0,
    'CREATE INDEX IX_facture_consultation ON facture (id_consultation)',
    'SELECT "Index IX_facture_consultation already exists"');
PREPARE stmt_idx FROM @sql_idx;
EXECUTE stmt_idx;
DEALLOCATE PREPARE stmt_idx;

-- FK vers consultation (si elle n'existe pas déjà)
SET @fk_exists = (
    SELECT COUNT(*) FROM information_schema.table_constraints 
    WHERE table_schema = 'mediconnect'
      AND table_name = 'facture'
      AND constraint_type = 'FOREIGN KEY'
      AND constraint_name = 'FK_facture_consultation'
);

SET @sql_fk = IF(@fk_exists = 0,
    'ALTER TABLE facture ADD CONSTRAINT FK_facture_consultation FOREIGN KEY (id_consultation) REFERENCES consultation(id_consultation) ON DELETE SET NULL',
    'SELECT "FK_facture_consultation already exists"');
PREPARE stmt_fk FROM @sql_fk;
EXECUTE stmt_fk;
DEALLOCATE PREPARE stmt_fk;

-- ==================== CONSULTATION: AJOUT ID_RDV ====================

SET @column_exists2 = (
    SELECT COUNT(*) FROM information_schema.columns 
    WHERE table_schema = 'mediconnect' 
    AND table_name = 'consultation' 
    AND column_name = 'id_rdv'
);

SET @sql2 = IF(@column_exists2 = 0,
    'ALTER TABLE consultation ADD COLUMN id_rdv INT NULL AFTER id_patient',
    'SELECT "Column id_rdv already exists"');
PREPARE stmt2 FROM @sql2;
EXECUTE stmt2;
DEALLOCATE PREPARE stmt2;

-- Index utile (lookup consultation par rdv)
SET @idx2_exists = (
    SELECT COUNT(*) FROM information_schema.statistics
    WHERE table_schema = 'mediconnect'
      AND table_name = 'consultation'
      AND index_name = 'IX_consultation_rdv'
);

SET @sql_idx2 = IF(@idx2_exists = 0,
    'CREATE INDEX IX_consultation_rdv ON consultation (id_rdv)',
    'SELECT "Index IX_consultation_rdv already exists"');
PREPARE stmt_idx2 FROM @sql_idx2;
EXECUTE stmt_idx2;
DEALLOCATE PREPARE stmt_idx2;

-- FK vers rendez_vous (si elle n'existe pas déjà)
SET @fk2_exists = (
    SELECT COUNT(*) FROM information_schema.table_constraints 
    WHERE table_schema = 'mediconnect'
      AND table_name = 'consultation'
      AND constraint_type = 'FOREIGN KEY'
      AND constraint_name = 'FK_consultation_rdv'
);

SET @sql_fk2 = IF(@fk2_exists = 0,
    'ALTER TABLE consultation ADD CONSTRAINT FK_consultation_rdv FOREIGN KEY (id_rdv) REFERENCES rendez_vous(id_rdv) ON DELETE SET NULL',
    'SELECT "FK_consultation_rdv already exists"');
PREPARE stmt_fk2 FROM @sql_fk2;
EXECUTE stmt_fk2;
DEALLOCATE PREPARE stmt_fk2;

SELECT 'Migration 007_add_facture_consultation_and_consultation_rdv.sql appliquée avec succès' AS status;
