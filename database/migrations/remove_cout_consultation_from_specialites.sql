-- Migration: Supprimer le champ cout_consultation de la table specialites
-- Le prix de consultation est maintenant géré uniquement au niveau du service
-- Date: 2026-04-02

-- Supprimer la colonne cout_consultation de la table specialites
-- Note: MySQL ne supporte pas "DROP COLUMN IF EXISTS", vérifier d'abord si la colonne existe
ALTER TABLE `specialites` DROP COLUMN `cout_consultation`;

-- Note: Le prix de consultation est maintenant récupéré depuis la table `service`
-- via le champ `cout_consultation` qui existe déjà dans cette table.
