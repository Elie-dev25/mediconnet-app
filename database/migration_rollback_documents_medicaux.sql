-- ============================================================================
-- SCRIPT DE ROLLBACK - Migration documents médicaux
-- MediConnect - À utiliser UNIQUEMENT en cas de problème
-- ============================================================================
-- 
-- ⚠️ ATTENTION: Ce script supprime les nouvelles tables et colonnes
-- Exécuter UNIQUEMENT si la migration a échoué et que vous devez revenir
-- à l'état précédent.
--
-- ============================================================================

-- Désactiver les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================================
-- ÉTAPE 1: SUPPRIMER LES VUES
-- ============================================================================

DROP VIEW IF EXISTS `v_dashboard_documents`;
DROP VIEW IF EXISTS `v_documents_problemes`;
DROP VIEW IF EXISTS `v_statistiques_documents`;

SELECT 'Vues supprimées' as status;

-- ============================================================================
-- ÉTAPE 2: SUPPRIMER LES CONTRAINTES FK
-- ============================================================================

-- Supprimer FK de bulletin_examen
ALTER TABLE `bulletin_examen` DROP FOREIGN KEY IF EXISTS `fk_bulletin_document_uuid`;

-- Supprimer FK de document_dmp
ALTER TABLE `document_dmp` DROP FOREIGN KEY IF EXISTS `fk_dmp_document_uuid`;

SELECT 'Contraintes FK supprimées' as status;

-- ============================================================================
-- ÉTAPE 3: SUPPRIMER LES COLONNES UUID
-- ============================================================================

-- Supprimer colonne de bulletin_examen
ALTER TABLE `bulletin_examen` DROP INDEX IF EXISTS `idx_bulletin_document_uuid`;
ALTER TABLE `bulletin_examen` DROP COLUMN IF EXISTS `document_resultat_uuid`;

-- Supprimer colonne de document_dmp
ALTER TABLE `document_dmp` DROP INDEX IF EXISTS `idx_dmp_document_uuid`;
ALTER TABLE `document_dmp` DROP COLUMN IF EXISTS `document_uuid`;

SELECT 'Colonnes UUID supprimées' as status;

-- ============================================================================
-- ÉTAPE 4: SUPPRIMER LES NOUVELLES TABLES
-- ============================================================================

DROP TABLE IF EXISTS `verification_integrite`;
DROP TABLE IF EXISTS `audit_acces_documents`;
DROP TABLE IF EXISTS `documents_medicaux`;

SELECT 'Tables supprimées' as status;

-- ============================================================================
-- ÉTAPE 5: RESTAURER LES PERMISSIONS biologiste (si nécessaire)
-- ============================================================================

-- Supprimer les permissions laborantin
DELETE FROM `role_permissions` WHERE `role` = 'laborantin';

-- Restaurer les permissions biologiste
INSERT IGNORE INTO `role_permissions` (`role`, `id_permission`)
SELECT 'biologiste', `id_permission` FROM `permissions` WHERE `code` IN (
    'patients.view',
    'examens.view', 'examens.result', 'examens.validate'
);

SELECT 'Permissions restaurées' as status;

-- Réactiver les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 1;

SELECT '✅ ROLLBACK TERMINÉ - Base restaurée à l état précédent' as status;

-- ============================================================================
-- FIN DU SCRIPT DE ROLLBACK
-- ============================================================================
