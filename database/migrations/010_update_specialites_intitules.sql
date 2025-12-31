-- Migration: Mise à jour des intitulés des spécialités
-- Date: 2024-12-23
-- Objectif: Renommer les spécialités en format "professionnel" (ex: Médecin généraliste au lieu de Médecine générale)

-- Mise à jour des spécialités avec le nouveau format
UPDATE specialites SET nom_specialite = 'Médecin généraliste' WHERE id_specialite = 1;
UPDATE specialites SET nom_specialite = 'Allergologue-Immunologiste' WHERE id_specialite = 2;
UPDATE specialites SET nom_specialite = 'Anesthésiste-Réanimateur' WHERE id_specialite = 3;
UPDATE specialites SET nom_specialite = 'Cardiologue' WHERE id_specialite = 4;
UPDATE specialites SET nom_specialite = 'Chirurgien cardio-thoracique' WHERE id_specialite = 5;
UPDATE specialites SET nom_specialite = 'Chirurgien colorectal' WHERE id_specialite = 6;
UPDATE specialites SET nom_specialite = 'Dermatologue' WHERE id_specialite = 7;
UPDATE specialites SET nom_specialite = 'Radiologue' WHERE id_specialite = 8;
UPDATE specialites SET nom_specialite = 'Urgentiste' WHERE id_specialite = 9;
UPDATE specialites SET nom_specialite = 'Endocrinologue' WHERE id_specialite = 10;
UPDATE specialites SET nom_specialite = 'Gastro-entérologue' WHERE id_specialite = 11;
UPDATE specialites SET nom_specialite = 'Chirurgien général' WHERE id_specialite = 12;
UPDATE specialites SET nom_specialite = 'Gériatre' WHERE id_specialite = 13;
UPDATE specialites SET nom_specialite = 'Hématologue' WHERE id_specialite = 14;
UPDATE specialites SET nom_specialite = 'Médecin palliatif' WHERE id_specialite = 15;
UPDATE specialites SET nom_specialite = 'Infectiologue' WHERE id_specialite = 16;
UPDATE specialites SET nom_specialite = 'Interniste' WHERE id_specialite = 17;
UPDATE specialites SET nom_specialite = 'Généticien médical' WHERE id_specialite = 18;
UPDATE specialites SET nom_specialite = 'Néphrologue' WHERE id_specialite = 19;
UPDATE specialites SET nom_specialite = 'Neurologue' WHERE id_specialite = 20;
UPDATE specialites SET nom_specialite = 'Neurochirurgien' WHERE id_specialite = 21;
UPDATE specialites SET nom_specialite = 'Médecin nucléaire' WHERE id_specialite = 22;
UPDATE specialites SET nom_specialite = 'Gynécologue-Obstétricien' WHERE id_specialite = 23;
UPDATE specialites SET nom_specialite = 'Médecin du travail' WHERE id_specialite = 24;
UPDATE specialites SET nom_specialite = 'Ophtalmologue' WHERE id_specialite = 25;
UPDATE specialites SET nom_specialite = 'Chirurgien orthopédiste' WHERE id_specialite = 26;
UPDATE specialites SET nom_specialite = 'ORL (Oto-rhino-laryngologiste)' WHERE id_specialite = 27;
UPDATE specialites SET nom_specialite = 'Pathologiste' WHERE id_specialite = 28;
UPDATE specialites SET nom_specialite = 'Pédiatre' WHERE id_specialite = 29;
UPDATE specialites SET nom_specialite = 'Médecin rééducateur' WHERE id_specialite = 30;
UPDATE specialites SET nom_specialite = 'Chirurgien plasticien' WHERE id_specialite = 31;
UPDATE specialites SET nom_specialite = 'Médecin préventif' WHERE id_specialite = 32;
UPDATE specialites SET nom_specialite = 'Psychiatre' WHERE id_specialite = 33;
UPDATE specialites SET nom_specialite = 'Pneumologue' WHERE id_specialite = 34;
UPDATE specialites SET nom_specialite = 'Radiothérapeute' WHERE id_specialite = 35;
UPDATE specialites SET nom_specialite = 'Rhumatologue' WHERE id_specialite = 36;
UPDATE specialites SET nom_specialite = 'Somnologue' WHERE id_specialite = 37;
UPDATE specialites SET nom_specialite = 'Médecin du sport' WHERE id_specialite = 38;
UPDATE specialites SET nom_specialite = 'Chirurgien thoracique' WHERE id_specialite = 39;
UPDATE specialites SET nom_specialite = 'Urologue' WHERE id_specialite = 40;
UPDATE specialites SET nom_specialite = 'Chirurgien vasculaire' WHERE id_specialite = 41;
UPDATE specialites SET nom_specialite = 'Médecin de santé publique' WHERE id_specialite = 42;
UPDATE specialites SET nom_specialite = 'Médecin urgentiste' WHERE id_specialite = 43;
UPDATE specialites SET nom_specialite = 'Pharmacologue clinicien' WHERE id_specialite = 44;
UPDATE specialites SET nom_specialite = 'Médecin légiste' WHERE id_specialite = 45;
UPDATE specialites SET nom_specialite = 'Algologue (spécialiste douleur)' WHERE id_specialite = 46;
UPDATE specialites SET nom_specialite = 'Médecin environnementaliste' WHERE id_specialite = 47;

-- Vérification
SELECT id_specialite, nom_specialite FROM specialites ORDER BY id_specialite;
