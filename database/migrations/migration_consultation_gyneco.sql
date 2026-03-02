START TRANSACTION;

SET @has_old_table := (
    SELECT COUNT(*)
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'consultation_gyneco'
);

SET @fk_exists := (
    SELECT COUNT(*)
    FROM information_schema.table_constraints
    WHERE table_schema = DATABASE()
      AND table_name = 'consultation_gyneco'
      AND constraint_name = 'fk_consultation_gyneco_consultation'
      AND constraint_type = 'FOREIGN KEY'
);

SET @sql_drop_fk := IF(@fk_exists > 0,
    'ALTER TABLE consultation_gyneco DROP FOREIGN KEY fk_consultation_gyneco_consultation',
    'SELECT 1');
PREPARE stmt FROM @sql_drop_fk;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql_drop_fk_backup := IF(@has_old_table > 0,
    'ALTER TABLE consultation_gyneco_backup DROP FOREIGN KEY fk_consultation_gyneco_consultation',
    'SELECT 1');
PREPARE stmt FROM @sql_drop_fk_backup;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql_backup := IF(@has_old_table > 0,
    'RENAME TABLE consultation_gyneco TO consultation_gyneco_backup',
    'SELECT 1');
PREPARE stmt FROM @sql_backup;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

DROP TABLE IF EXISTS consultation_gyneco;

CREATE TABLE consultation_gyneco (
    id_consultation INT NOT NULL,
    inspection_externe TEXT NULL,
    examen_speculum TEXT NULL,
    toucher_vaginal TEXT NULL,
    autres_observations TEXT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NULL,
    PRIMARY KEY (id_consultation),
    CONSTRAINT fk_consultation_gyneco_consultation
        FOREIGN KEY (id_consultation) REFERENCES consultation(id_consultation)
        ON DELETE CASCADE
);

SET @sql_insert := IF(@has_old_table > 0,
    'INSERT INTO consultation_gyneco (id_consultation, inspection_externe, examen_speculum, toucher_vaginal, autres_observations, created_at, updated_at)
     SELECT IFNULL(id_consultation, 0), inspection_externe, examen_speculum, toucher_vaginal, autres_observations, IFNULL(created_at, NOW()), updated_at
     FROM consultation_gyneco_backup',
    'SELECT 1');
PREPARE stmt FROM @sql_insert;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql_drop_backup := IF(@has_old_table > 0,
    'DROP TABLE consultation_gyneco_backup',
    'SELECT 1');
PREPARE stmt FROM @sql_drop_backup;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

COMMIT;
