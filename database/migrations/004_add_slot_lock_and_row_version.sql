-- Migration: Ajout du verrouillage des créneaux et gestion de concurrence
-- Date: 2024-12-08
-- Description: 
--   - Ajoute la colonne row_version sur rendez_vous pour le verrouillage optimiste
--   - Crée la table slot_lock pour le verrouillage temporaire des créneaux

-- ==================== VERROUILLAGE OPTIMISTE SUR RENDEZ-VOUS ====================

-- Ajouter la colonne row_version pour le verrouillage optimiste
-- Note: Vérifie si la colonne existe avant d'ajouter
SET @column_exists = (
    SELECT COUNT(*) FROM information_schema.columns 
    WHERE table_schema = 'mediconnect' 
    AND table_name = 'rendez_vous' 
    AND column_name = 'row_version'
);

SET @sql = IF(@column_exists = 0, 
    'ALTER TABLE rendez_vous ADD COLUMN row_version TIMESTAMP(6) DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6)', 
    'SELECT "Column row_version already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ==================== TABLE SLOT_LOCK ====================

-- Table pour gérer les verrous temporaires sur les créneaux
-- Prévient les doubles réservations lors de tentatives simultanées
CREATE TABLE IF NOT EXISTS slot_lock (
    id_lock INT AUTO_INCREMENT PRIMARY KEY,
    id_medecin INT NOT NULL,
    date_heure DATETIME NOT NULL,
    duree INT DEFAULT 30,
    id_user INT NOT NULL,
    lock_token VARCHAR(64) NOT NULL,
    expires_at DATETIME NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    -- Clés étrangères
    CONSTRAINT fk_slot_lock_medecin FOREIGN KEY (id_medecin) REFERENCES medecin(id_user) ON DELETE CASCADE,
    CONSTRAINT fk_slot_lock_user FOREIGN KEY (id_user) REFERENCES utilisateurs(id_user) ON DELETE CASCADE,
    
    -- Index unique pour éviter les doublons sur le même créneau
    UNIQUE INDEX IX_slot_lock_medecin_date (id_medecin, date_heure),
    
    -- Index pour nettoyer les verrous expirés
    INDEX IX_slot_lock_expires (expires_at),
    
    -- Index sur le token pour les lookups rapides
    INDEX IX_slot_lock_token (lock_token)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==================== INDEX SUPPLÉMENTAIRES SUR RENDEZ-VOUS ====================

-- Les index sont créés par EF Core automatiquement, pas besoin de les créer manuellement

-- Afficher le statut de la migration
SELECT 'Migration 004_add_slot_lock_and_row_version.sql appliquée avec succès' AS status;
