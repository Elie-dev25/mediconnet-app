-- Migration: Ajouter les colonnes pour la planification des interventions chirurgicales
-- Date: 2026-03-25
-- Description: Ajoute heure_debut et id_indisponibilite à la table programmation_intervention

-- Ajouter la colonne heure_debut pour l'heure de début de l'intervention
ALTER TABLE programmation_intervention 
ADD COLUMN IF NOT EXISTS heure_debut VARCHAR(5) NULL COMMENT 'Heure de début prévue (format HH:mm)';

-- Ajouter la colonne id_indisponibilite pour lier au blocage de créneau
ALTER TABLE programmation_intervention 
ADD COLUMN IF NOT EXISTS id_indisponibilite INT NULL COMMENT 'ID de l''indisponibilité créée pour bloquer le créneau';

-- Ajouter la clé étrangère vers indisponibilite_medecin
-- Note: On vérifie d'abord si la contrainte existe déjà
SET @constraint_exists = (
    SELECT COUNT(*) 
    FROM information_schema.TABLE_CONSTRAINTS 
    WHERE CONSTRAINT_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'programmation_intervention' 
    AND CONSTRAINT_NAME = 'FK_programmation_intervention_indisponibilite'
);

SET @sql = IF(@constraint_exists = 0, 
    'ALTER TABLE programmation_intervention ADD CONSTRAINT FK_programmation_intervention_indisponibilite FOREIGN KEY (id_indisponibilite) REFERENCES indisponibilite_medecin(id_indisponibilite) ON DELETE SET NULL',
    'SELECT "Constraint already exists"'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Ajouter le type 'intervention' aux types d'indisponibilité si pas déjà présent
-- (Le type est stocké comme VARCHAR, donc pas besoin de modifier l'ENUM)
