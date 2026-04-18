-- ============================================
-- SIMULATION CONSULTATION COMPLETE
-- Patient: Anicet Pantone (id=14)
-- Médecin: Dr. Samuel DIKI - Généraliste (id=2)
-- Date: Aujourd'hui
-- ============================================

SET @patient_id = 14;
SET @medecin_id = 2;
SET @date_consultation = NOW();

-- 1. CRÉER LA CONSULTATION
INSERT INTO consultation (
    id_patient,
    id_medecin,
    date_heure,
    motif,
    statut,
    type_consultation,
    created_at,
    updated_at
) VALUES (
    @patient_id,
    @medecin_id,
    @date_consultation,
    'Douleurs abdominales persistantes depuis 3 jours avec nausées et fatigue générale',
    'terminee',
    'consultation',
    NOW(),
    NOW()
);

SET @consultation_id = LAST_INSERT_ID();

-- 2. ANAMNÈSE
INSERT INTO anamnese (
    id_consultation,
    histoire_maladie,
    antecedents_personnels,
    antecedents_familiaux,
    allergies,
    traitements_en_cours,
    mode_vie,
    created_at
) VALUES (
    @consultation_id,
    'Le patient se présente pour des douleurs abdominales épigastriques évoluant depuis 3 jours. Les douleurs sont décrites comme des crampes, d''intensité modérée à sévère (6/10), aggravées par les repas et partiellement soulagées par le jeûne. Associées à des nausées sans vomissements, une perte d''appétit et une fatigue inhabituelle. Pas de fièvre, pas de diarrhée ni constipation. Le patient signale un stress professionnel important ces dernières semaines.',
    'Gastrite diagnostiquée il y a 2 ans, traitée par IPP. Appendicectomie en 2015. Pas de diabète ni HTA.',
    'Père: ulcère gastrique. Mère: diabète type 2. Pas de cancer digestif connu dans la famille.',
    'Aucune allergie médicamenteuse connue. Intolérance au lactose suspectée.',
    'Oméprazole 20mg 1x/jour (arrêté depuis 6 mois)',
    'Cadre dans une entreprise, stress professionnel élevé. Alimentation irrégulière, tendance à sauter le petit-déjeuner. Consommation modérée d''alcool (2-3 verres/semaine). Non fumeur. Activité physique limitée.',
    NOW()
);

-- 3. QUESTIONNAIRE MÉDICAL (Questions-Réponses)
INSERT INTO questionnaire_reponse (id_consultation, question, reponse, ordre, created_at) VALUES
(@consultation_id, 'Depuis quand avez-vous ces douleurs ?', 'Depuis 3 jours, elles ont commencé progressivement', 1, NOW()),
(@consultation_id, 'Comment décrivez-vous la douleur ?', 'Comme des crampes, des brûlures dans le haut du ventre', 2, NOW()),
(@consultation_id, 'La douleur irradie-t-elle ?', 'Parfois vers le dos', 3, NOW()),
(@consultation_id, 'Avez-vous des nausées ou vomissements ?', 'Nausées oui, mais pas de vomissements', 4, NOW()),
(@consultation_id, 'Comment sont vos selles ?', 'Normales, pas de sang visible', 5, NOW()),
(@consultation_id, 'Avez-vous de la fièvre ?', 'Non, pas de fièvre', 6, NOW()),
(@consultation_id, 'Prenez-vous des anti-inflammatoires ?', 'Oui, j''ai pris de l''ibuprofène pendant 2 jours pour des maux de tête', 7, NOW()),
(@consultation_id, 'Avez-vous perdu du poids récemment ?', 'Peut-être 1-2 kg ces dernières semaines, je mange moins', 8, NOW());

-- 4. EXAMEN CLINIQUE
INSERT INTO examen_clinique (
    id_consultation,
    inspection,
    palpation,
    auscultation,
    percussion,
    autres_observations,
    parametres_pris_par_infirmier,
    date_prise_parametres,
    created_at
) VALUES (
    @consultation_id,
    'Patient conscient, bien orienté. Faciès légèrement pâle. Pas d''ictère. Pas de signes de déshydratation. Abdomen plat, pas de distension visible. Pas de cicatrice autre que celle de l''appendicectomie en FID.',
    'Abdomen souple, dépressible. Sensibilité épigastrique nette à la palpation profonde. Pas de défense ni contracture. Pas de masse palpable. Foie et rate non palpables. Points urétéraux indolores. Pas d''adénopathie inguinale.',
    'Bruits hydro-aériques présents et normaux dans les 4 quadrants. Pas de souffle abdominal.',
    'Tympanisme normal. Pas de matité anormale.',
    'Toucher rectal non réalisé (non indiqué). Examen cardio-pulmonaire sans particularité. TA normale, pouls régulier.',
    0,
    NOW(),
    NOW()
);

SET @examen_clinique_id = LAST_INSERT_ID();

-- 5. PARAMÈTRES VITAUX
INSERT INTO parametres_vitaux (
    id_examen_clinique,
    poids,
    taille,
    temperature,
    tension_arterielle,
    frequence_cardiaque,
    frequence_respiratoire,
    saturation_oxygene,
    glycemie,
    created_at
) VALUES (
    @examen_clinique_id,
    72.5,
    175,
    37.2,
    '125/80',
    78,
    16,
    98,
    0.95,
    NOW()
);

-- 6. DIAGNOSTIC
INSERT INTO diagnostic (
    id_consultation,
    diagnostic_principal,
    diagnostics_secondaires,
    notes_cliniques,
    code_cim10,
    created_at
) VALUES (
    @consultation_id,
    'Gastrite aiguë probablement médicamenteuse (AINS)',
    'Stress professionnel avec composante fonctionnelle. Dyspepsie.',
    'Tableau clinique évocateur d''une gastrite aiguë, probablement favorisée par la prise récente d''AINS (ibuprofène) sur un terrain prédisposé (antécédent de gastrite). L''absence de signes d''alarme (pas de méléna, pas d''hématémèse, pas de perte de poids importante, pas de dysphagie) est rassurante. Une prise en charge médicamenteuse et des mesures hygiéno-diététiques sont indiquées en première intention.',
    'K29.1',
    NOW()
);

-- 7. PLAN DE TRAITEMENT
INSERT INTO plan_traitement (
    id_consultation,
    explication_diagnostic,
    options_traitement,
    orientation_specialiste,
    motif_orientation,
    created_at
) VALUES (
    @consultation_id,
    'Vous souffrez d''une inflammation de l''estomac (gastrite) probablement déclenchée par les anti-inflammatoires que vous avez pris. Votre estomac est irrité et c''est ce qui cause vos douleurs et nausées. Ce n''est pas grave mais il faut traiter pour éviter les complications.',
    'Traitement médicamenteux par inhibiteur de la pompe à protons pendant 4 semaines. Mesures hygiéno-diététiques. Éviction des AINS. Si pas d''amélioration sous 2 semaines, une fibroscopie gastrique sera envisagée.',
    NULL,
    NULL,
    NOW()
);

-- 8. ORDONNANCE
INSERT INTO ordonnance (
    id_consultation,
    id_medecin,
    id_patient,
    date_prescription,
    instructions_generales,
    statut,
    created_at
) VALUES (
    @consultation_id,
    @medecin_id,
    @patient_id,
    NOW(),
    'Prendre les médicaments comme indiqué. Éviter les aliments acides, épicés et gras. Ne pas prendre d''anti-inflammatoires (Ibuprofène, Aspirine, Diclofénac...) sans avis médical. Consulter en urgence si vomissements de sang ou selles noires.',
    'active',
    NOW()
);

SET @ordonnance_id = LAST_INSERT_ID();

-- 9. MÉDICAMENTS PRESCRITS
INSERT INTO prescription_medicament (id_ordonnance, id_medicament, nom_medicament, dosage, forme, frequence, duree, quantite, instructions, created_at) VALUES
(@ordonnance_id, NULL, 'Esoméprazole 40mg', '40mg', 'Comprimé gastro-résistant', '1 comprimé le matin à jeun', '4 semaines', 28, 'Prendre 30 minutes avant le petit-déjeuner avec un verre d''eau. Ne pas croquer le comprimé.', NOW()),
(@ordonnance_id, NULL, 'Gaviscon suspension buvable', '10ml', 'Suspension buvable', '1 sachet après chaque repas et au coucher', '2 semaines', 30, 'Prendre après les repas et au coucher. Bien agiter avant emploi.', NOW()),
(@ordonnance_id, NULL, 'Métoclopramide 10mg', '10mg', 'Comprimé', '1 comprimé 3 fois par jour avant les repas', '5 jours', 15, 'Prendre 15-30 minutes avant les repas. Arrêter si somnolence importante.', NOW()),
(@ordonnance_id, NULL, 'Spasfon Lyoc 80mg', '80mg', 'Lyophilisat oral', '1 à 2 comprimés en cas de crampes', 'Si besoin', 10, 'Laisser fondre sous la langue en cas de douleurs spasmodiques. Maximum 6 par jour.', NOW());

-- 10. EXAMENS PRESCRITS
INSERT INTO examen_prescrit (id_consultation, id_laboratoire, nom_examen, type_examen, priorite, notes, statut, created_at) VALUES
(@consultation_id, 1, 'Numération Formule Sanguine (NFS)', 'biologie', 'normale', 'Recherche d''une anémie (saignement occulte)', 'prescrit', NOW()),
(@consultation_id, 1, 'CRP (Protéine C-Réactive)', 'biologie', 'normale', 'Évaluation du syndrome inflammatoire', 'prescrit', NOW()),
(@consultation_id, 1, 'Hélicobacter pylori - Sérologie', 'biologie', 'normale', 'Recherche d''infection à H. pylori', 'prescrit', NOW()),
(@consultation_id, 1, 'Bilan hépatique (ASAT, ALAT, GGT, PAL)', 'biologie', 'normale', 'Éliminer une atteinte hépatique ou biliaire associée', 'prescrit', NOW());

-- 11. CONCLUSION
INSERT INTO conclusion_consultation (
    id_consultation,
    resume_consultation,
    questions_patient,
    consignes_patient,
    recommandations,
    type_suivi,
    date_suivi_prevue,
    notes_suivi,
    created_at
) VALUES (
    @consultation_id,
    'Consultation pour douleurs épigastriques évoluant depuis 3 jours chez un patient de 35 ans avec antécédent de gastrite. Examen clinique retrouvant une sensibilité épigastrique isolée. Diagnostic retenu: gastrite aiguë probablement médicamenteuse (prise récente d''AINS). Traitement par IPP et antiacides instauré. Bilan biologique prescrit.',
    'Le patient a demandé si c''était grave et s''il pouvait continuer à travailler. Rassuré sur le caractère bénin avec le traitement adapté. Peut continuer son activité professionnelle.',
    'Prendre le traitement comme prescrit. Éviter absolument les anti-inflammatoires. Manger léger et fractionné. Éviter alcool, café, épices pendant le traitement. Revenir si aggravation ou apparition de nouveaux symptômes.',
    'Consultation de contrôle dans 2-3 semaines pour évaluer l''efficacité du traitement. Si persistance des symptômes, prévoir une fibroscopie gastrique. Gestion du stress recommandée.',
    'Consultation de contrôle',
    DATE_ADD(NOW(), INTERVAL 21 DAY),
    'Contrôle clinique et résultats biologiques. Évaluer la réponse au traitement. Discuter de la fibroscopie si pas d''amélioration.',
    NOW()
);

-- 12. CRÉER UNE FACTURE POUR LA CONSULTATION
INSERT INTO facture (
    id_patient,
    id_consultation,
    type_facture,
    montant_total,
    montant_assurance,
    montant_patient,
    statut,
    date_facture,
    created_at
) VALUES (
    @patient_id,
    @consultation_id,
    'consultation',
    15000,
    0,
    15000,
    'payee',
    NOW(),
    NOW()
);

-- Afficher le résumé
SELECT 'CONSULTATION CRÉÉE AVEC SUCCÈS' AS Resultat;
SELECT @consultation_id AS ID_Consultation;
SELECT 
    c.id_consultation,
    CONCAT(up.prenom, ' ', up.nom) AS Patient,
    CONCAT('Dr. ', um.prenom, ' ', um.nom) AS Medecin,
    c.date_heure,
    c.motif,
    c.statut
FROM consultation c
JOIN utilisateurs up ON c.id_patient = up.id_user
JOIN utilisateurs um ON c.id_medecin = um.id_user
WHERE c.id_consultation = @consultation_id;
