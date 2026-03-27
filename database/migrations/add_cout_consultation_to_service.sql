-- Migration: Ajouter la colonne cout_consultation à la table service
-- Date: 2026-03-24
-- Description: Le coût de consultation est maintenant rattaché au service (et non à la spécialité)
--              Cela permet à l'admin de modifier le tarif par service

-- Ajouter la colonne cout_consultation à la table service
ALTER TABLE service ADD COLUMN IF NOT EXISTS cout_consultation DECIMAL(10,2) NOT NULL DEFAULT 5000;

-- Mettre à jour les services existants avec des tarifs différenciés selon le type de service
-- Ces valeurs sont des exemples et peuvent être ajustées par l'administrateur
UPDATE service SET cout_consultation = 
    CASE 
        WHEN LOWER(nom_service) LIKE '%urgence%' THEN 10000
        WHEN LOWER(nom_service) LIKE '%chirurgie%' THEN 15000
        WHEN LOWER(nom_service) LIKE '%cardiologie%' THEN 12000
        WHEN LOWER(nom_service) LIKE '%gynécologie%' OR LOWER(nom_service) LIKE '%gynecologie%' THEN 8000
        WHEN LOWER(nom_service) LIKE '%pédiatrie%' OR LOWER(nom_service) LIKE '%pediatrie%' THEN 7000
        WHEN LOWER(nom_service) LIKE '%neurologie%' THEN 13000
        WHEN LOWER(nom_service) LIKE '%urologie%' THEN 11000
        WHEN LOWER(nom_service) LIKE '%ophtalmologie%' THEN 9000
        WHEN LOWER(nom_service) LIKE '%dermatologie%' THEN 8000
        WHEN LOWER(nom_service) LIKE '%médecine générale%' OR LOWER(nom_service) LIKE '%medecine generale%' THEN 5000
        ELSE 5000
    END
WHERE cout_consultation = 5000;

-- Vérification
SELECT id_service, nom_service, cout_consultation FROM service ORDER BY nom_service;
