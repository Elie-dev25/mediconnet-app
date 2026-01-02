-- =====================================================
-- Migration: Création de la table audit_logs
-- Description: Journal d'audit pour la traçabilité des actions
-- Conformité: RGPD et HDS (Hébergeur de Données de Santé)
-- =====================================================

-- Table des logs d'audit
CREATE TABLE IF NOT EXISTS audit_logs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL DEFAULT 0,
    action VARCHAR(100) NOT NULL,
    resource_type VARCHAR(100) NOT NULL,
    resource_id INT NULL,
    details TEXT NULL,
    ip_address VARCHAR(45) NULL,
    user_agent VARCHAR(500) NULL,
    success BOOLEAN NOT NULL DEFAULT TRUE,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Index pour les recherches fréquentes
    INDEX idx_audit_user_id (user_id),
    INDEX idx_audit_action (action),
    INDEX idx_audit_resource (resource_type, resource_id),
    INDEX idx_audit_created_at (created_at),
    INDEX idx_audit_success (success),
    
    -- Clé étrangère optionnelle vers utilisateurs
    -- Commentée car user_id peut être 0 pour les actions non authentifiées
    -- FOREIGN KEY (user_id) REFERENCES utilisateurs(id_user) ON DELETE SET NULL
    
    COMMENT = 'Journal d''audit pour la traçabilité des actions - Conformité RGPD/HDS'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Vue pour les statistiques d'audit
CREATE OR REPLACE VIEW v_audit_stats AS
SELECT 
    DATE(created_at) as date_audit,
    action,
    resource_type,
    COUNT(*) as nombre_actions,
    SUM(CASE WHEN success = TRUE THEN 1 ELSE 0 END) as succes,
    SUM(CASE WHEN success = FALSE THEN 1 ELSE 0 END) as echecs
FROM audit_logs
GROUP BY DATE(created_at), action, resource_type
ORDER BY date_audit DESC, nombre_actions DESC;

-- Vue pour les tentatives de connexion échouées (détection d'attaques)
CREATE OR REPLACE VIEW v_failed_logins AS
SELECT 
    ip_address,
    COUNT(*) as tentatives,
    MIN(created_at) as premiere_tentative,
    MAX(created_at) as derniere_tentative
FROM audit_logs
WHERE action IN ('LOGIN_FAILED', 'AUTH_FAILURE')
    AND created_at >= DATE_SUB(NOW(), INTERVAL 24 HOUR)
GROUP BY ip_address
HAVING COUNT(*) >= 5
ORDER BY tentatives DESC;

-- Procédure pour nettoyer les anciens logs (conservation 2 ans pour conformité)
DELIMITER //
CREATE PROCEDURE IF NOT EXISTS sp_cleanup_old_audit_logs()
BEGIN
    DECLARE retention_days INT DEFAULT 730; -- 2 ans
    
    DELETE FROM audit_logs 
    WHERE created_at < DATE_SUB(NOW(), INTERVAL retention_days DAY);
    
    SELECT ROW_COUNT() as deleted_rows;
END //
DELIMITER ;

-- Event pour nettoyage automatique mensuel (si EVENT_SCHEDULER est activé)
-- CREATE EVENT IF NOT EXISTS evt_cleanup_audit_logs
-- ON SCHEDULE EVERY 1 MONTH
-- DO CALL sp_cleanup_old_audit_logs();
