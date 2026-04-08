-- Migration: Création de la table consultation_anesthesique
-- Date: 2026-04-01
-- Description: Table pour stocker les données de consultation pré-anesthésique

CREATE TABLE IF NOT EXISTS consultation_anesthesique (
    id_consultation INT PRIMARY KEY,
    id_coordination INT NULL,
    
    -- Anamnèse spécifique anesthésie
    antecedents_medicaux TEXT NULL,
    problemes_cardiaques TEXT NULL,
    problemes_respiratoires TEXT NULL,
    allergies_anesthesie TEXT NULL,
    antecedents_chirurgicaux TEXT NULL,
    problemes_anesthesie_precedente TEXT NULL,
    medicaments_en_cours TEXT NULL,
    symptomes TEXT NULL,
    apnee_sommeil BOOLEAN NULL,
    troubles_coagulation BOOLEAN NULL,
    troubles_coagulation_details TEXT NULL,
    
    -- Examen clinique
    poids DECIMAL(5,2) NULL,
    taille DECIMAL(5,2) NULL,
    imc DECIMAL(5,2) NULL,
    tension_systolique INT NULL,
    tension_diastolique INT NULL,
    frequence_cardiaque INT NULL,
    saturation_oxygene DECIMAL(5,2) NULL,
    auscultation_cardiaque TEXT NULL,
    auscultation_pulmonaire TEXT NULL,
    
    -- Voies aériennes (critique pour intubation)
    ouverture_bouche VARCHAR(50) NULL,
    mallampati INT NULL,
    etat_dents VARCHAR(100) NULL,
    mobilite_cou VARCHAR(50) NULL,
    distance_thyro_mentonniere DECIMAL(4,2) NULL,
    intubation_difficile_prevue BOOLEAN NULL,
    notes_voies_aeriennes TEXT NULL,
    
    -- Évaluation du risque
    classification_asa INT NULL,
    niveau_risque VARCHAR(20) NULL,
    risque_cardiaque VARCHAR(20) NULL,
    risque_respiratoire VARCHAR(20) NULL,
    risque_allergique VARCHAR(20) NULL,
    risque_hemorragique VARCHAR(20) NULL,
    notes_risques TEXT NULL,
    
    -- Choix du type d'anesthésie
    type_anesthesie VARCHAR(30) NULL,
    sous_type_anesthesie VARCHAR(50) NULL,
    justification_anesthesie TEXT NULL,
    explication_patient TEXT NULL,
    consentement_obtenu BOOLEAN NULL,
    date_consentement DATETIME NULL,
    
    -- Consignes préopératoires
    duree_jeune INT NULL,
    instructions_jeune TEXT NULL,
    medicaments_a_arreter TEXT NULL,
    medicaments_a_adapter TEXT NULL,
    medicaments_a_continuer TEXT NULL,
    arret_tabac BOOLEAN NULL,
    delai_arret_tabac INT NULL,
    instructions_hygiene TEXT NULL,
    autres_consignes TEXT NULL,
    
    -- Conclusion
    resume_consultation TEXT NULL,
    aptitude VARCHAR(30) NULL,
    reserves TEXT NULL,
    motif_non_aptitude TEXT NULL,
    recommandations TEXT NULL,
    date_intervention_prevue DATETIME NULL,
    
    -- Métadonnées
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NULL,
    
    -- Contraintes
    CONSTRAINT fk_consultation_anesthesique_consultation 
        FOREIGN KEY (id_consultation) REFERENCES consultation(id_consultation) ON DELETE CASCADE,
    CONSTRAINT fk_consultation_anesthesique_coordination 
        FOREIGN KEY (id_coordination) REFERENCES coordination_intervention(id_coordination) ON DELETE SET NULL
);

-- Index
CREATE UNIQUE INDEX IX_consultation_anesthesique_consultation ON consultation_anesthesique(id_consultation);
CREATE INDEX IX_consultation_anesthesique_coordination ON consultation_anesthesique(id_coordination);
CREATE INDEX IX_consultation_anesthesique_aptitude ON consultation_anesthesique(aptitude);
CREATE INDEX IX_consultation_anesthesique_type_anesthesie ON consultation_anesthesique(type_anesthesie);
