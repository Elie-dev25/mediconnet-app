SET NAMES utf8mb4;
SET CHARACTER SET utf8mb4;

-- Fix encoding for service table
UPDATE service SET nom_service = 'Medecine Generale', description = 'Service de medecine generale' WHERE id_service = 3;

-- Fix encoding for specialites table
UPDATE specialites SET nom_specialite = 'Medecine General' WHERE id_specialite = 1;
UPDATE specialites SET nom_specialite = 'Anesthesiologie' WHERE id_specialite = 3;
UPDATE specialites SET nom_specialite = 'Medecine d''Urgence' WHERE id_specialite = 9;
UPDATE specialites SET nom_specialite = 'Gastro-enterologie' WHERE id_specialite = 11;
UPDATE specialites SET nom_specialite = 'Chirurgie Generale' WHERE id_specialite = 12;
UPDATE specialites SET nom_specialite = 'Geriatrie' WHERE id_specialite = 13;
UPDATE specialites SET nom_specialite = 'Hematologie' WHERE id_specialite = 14;
UPDATE specialites SET nom_specialite = 'Medecine Palliative' WHERE id_specialite = 15;
UPDATE specialites SET nom_specialite = 'Medecine Interne' WHERE id_specialite = 17;
UPDATE specialites SET nom_specialite = 'Genetique Medicale et Genomique' WHERE id_specialite = 18;
UPDATE specialites SET nom_specialite = 'Nephrologie' WHERE id_specialite = 19;
UPDATE specialites SET nom_specialite = 'Medecine Nucleaire' WHERE id_specialite = 22;
UPDATE specialites SET nom_specialite = 'Obstetrique et Gynecologie' WHERE id_specialite = 23;
UPDATE specialites SET nom_specialite = 'Medecine du Travail' WHERE id_specialite = 24;
UPDATE specialites SET nom_specialite = 'Chirurgie Orthopedique' WHERE id_specialite = 26;
UPDATE specialites SET nom_specialite = 'Pediatrie' WHERE id_specialite = 29;
UPDATE specialites SET nom_specialite = 'Medecine Physique et de Readaptation' WHERE id_specialite = 30;
UPDATE specialites SET nom_specialite = 'Medecine Preventive' WHERE id_specialite = 32;
UPDATE specialites SET nom_specialite = 'Pneumologie (Medecine Respiratoire)' WHERE id_specialite = 34;
UPDATE specialites SET nom_specialite = 'Radiotherapie' WHERE id_specialite = 35;
UPDATE specialites SET nom_specialite = 'Medecine du Sommeil' WHERE id_specialite = 37;
UPDATE specialites SET nom_specialite = 'Medecine du Sport' WHERE id_specialite = 38;
UPDATE specialites SET nom_specialite = 'Sante Publique / Medecine Communautaire' WHERE id_specialite = 42;
UPDATE specialites SET nom_specialite = 'Services Medicaux d''Urgence' WHERE id_specialite = 43;
UPDATE specialites SET nom_specialite = 'Pathologie Medico-legale' WHERE id_specialite = 45;
UPDATE specialites SET nom_specialite = 'Medecine de la Douleur' WHERE id_specialite = 46;
UPDATE specialites SET nom_specialite = 'Medecine du Travail et de l''Environnement' WHERE id_specialite = 47;
