-- ============================================
-- SIMULATION CONSULTATION COMPLETE
-- Patient: Anicet Pantone (id=14)
-- Médecin: Dr. Samuel DIKI - Généraliste (id=2)
-- Date: Aujourd'hui
-- ============================================

SET @patient_id = 14;
SET @medecin_id = 2;
SET @date_consultation = NOW();

-- 1. CRÉER LA CONSULTATION COMPLÈTE
INSERT INTO consultation (
    id_patient,
    id_medecin,
    date_heure,
    date_debut_effective,
    date_fin,
    duree_minutes,
    motif,
    statut,
    type_consultation,
    anamnese,
    antecedents,
    poids,
    temperature,
    tension,
    notes_cliniques,
    examen_inspection,
    examen_palpation,
    examen_auscultation,
    examen_percussion,
    examen_autres,
    diagnostic,
    diagnostics_secondaires,
    hypotheses_diagnostiques,
    explication_diagnostic,
    options_traitement,
    resume_consultation,
    questions_patient,
    consignes_patient,
    recommandations,
    etape_actuelle,
    updated_at
) VALUES (
    @patient_id,
    @medecin_id,
    @date_consultation,
    @date_consultation,
    DATE_ADD(@date_consultation, INTERVAL 35 MINUTE),
    35,
    'Douleurs abdominales persistantes depuis 3 jours avec nausées et fatigue générale',
    'terminee',
    'consultation',
    'Le patient se présente pour des douleurs abdominales épigastriques évoluant depuis 3 jours. Les douleurs sont décrites comme des crampes, d''intensité modérée à sévère (6/10), aggravées par les repas et partiellement soulagées par le jeûne. Associées à des nausées sans vomissements, une perte d''appétit et une fatigue inhabituelle. Pas de fièvre, pas de diarrhée ni constipation. Le patient signale un stress professionnel important ces dernières semaines. A pris de l''ibuprofène pendant 2 jours pour des maux de tête.',
    'Gastrite diagnostiquée il y a 2 ans, traitée par IPP. Appendicectomie en 2015. Pas de diabète ni HTA. Père: ulcère gastrique. Mère: diabète type 2. Aucune allergie médicamenteuse connue. Intolérance au lactose suspectée.',
    72.5,
    37.2,
    '125/80',
    'Patient conscient, bien orienté. Faciès légèrement pâle. Pas d''ictère. FC: 78 bpm, FR: 16/min, SpO2: 98%. Glycémie: 0.95 g/L.',
    'Patient conscient, bien orienté. Faciès légèrement pâle. Pas d''ictère. Pas de signes de déshydratation. Abdomen plat, pas de distension visible. Cicatrice d''appendicectomie en FID.',
    'Abdomen souple, dépressible. Sensibilité épigastrique nette à la palpation profonde. Pas de défense ni contracture. Pas de masse palpable. Foie et rate non palpables. Points urétéraux indolores.',
    'Bruits hydro-aériques présents et normaux dans les 4 quadrants. Pas de souffle abdominal. Examen cardio-pulmonaire sans particularité.',
    'Tympanisme normal. Pas de matité anormale.',
    'Toucher rectal non réalisé (non indiqué). Pas d''adénopathie inguinale.',
    'Gastrite aiguë probablement médicamenteuse (AINS)',
    'Stress professionnel avec composante fonctionnelle. Dyspepsie.',
    'Gastrite aiguë médicamenteuse, Ulcère gastro-duodénal à éliminer, Dyspepsie fonctionnelle',
    'Vous souffrez d''une inflammation de l''estomac (gastrite) probablement déclenchée par les anti-inflammatoires que vous avez pris. Votre estomac est irrité et c''est ce qui cause vos douleurs et nausées. Ce n''est pas grave mais il faut traiter pour éviter les complications.',
    'Traitement médicamenteux par inhibiteur de la pompe à protons pendant 4 semaines. Mesures hygiéno-diététiques. Éviction des AINS. Si pas d''amélioration sous 2 semaines, une fibroscopie gastrique sera envisagée.',
    'Consultation pour douleurs épigastriques évoluant depuis 3 jours chez un patient avec antécédent de gastrite. Examen clinique retrouvant une sensibilité épigastrique isolée. Diagnostic retenu: gastrite aiguë probablement médicamenteuse (prise récente d''AINS). Traitement par IPP et antiacides instauré. Bilan biologique prescrit.',
    'Le patient a demandé si c''était grave et s''il pouvait continuer à travailler. Rassuré sur le caractère bénin avec le traitement adapté. Peut continuer son activité professionnelle.',
    'Prendre le traitement comme prescrit. Éviter absolument les anti-inflammatoires. Manger léger et fractionné. Éviter alcool, café, épices pendant le traitement. Revenir si aggravation ou apparition de nouveaux symptômes.',
    'Consultation de contrôle dans 2-3 semaines pour évaluer l''efficacité du traitement. Si persistance des symptômes, prévoir une fibroscopie gastrique. Gestion du stress recommandée.',
    'suivi',
    NOW()
);

SET @consultation_id = LAST_INSERT_ID();

-- 2. ORDONNANCE
INSERT INTO ordonnance (
    id_consultation,
    id_medecin,
    id_patient,
    date,
    type_contexte,
    statut,
    commentaire
) VALUES (
    @consultation_id,
    @medecin_id,
    @patient_id,
    CURDATE(),
    'consultation',
    'active',
    'Prendre les médicaments comme indiqué. Éviter les aliments acides, épicés et gras. Ne pas prendre d''anti-inflammatoires (Ibuprofène, Aspirine, Diclofénac...) sans avis médical. Consulter en urgence si vomissements de sang ou selles noires.'
);

SET @ordonnance_id = LAST_INSERT_ID();

-- 3. MÉDICAMENTS PRESCRITS
INSERT INTO ordonnance_medicament (id_ordonnance, id_medicament, nom_medicament_libre, dosage_libre, est_hors_catalogue, quantite, duree_traitement, posologie, frequence, voie_administration, forme_pharmaceutique, instructions) VALUES
(@ordonnance_id, NULL, 'Esoméprazole 40mg', '40mg', 1, 28, '4 semaines', '1 comprimé le matin à jeun', '1 fois par jour', 'Orale', 'Comprimé gastro-résistant', 'Prendre 30 minutes avant le petit-déjeuner avec un verre d''eau. Ne pas croquer le comprimé.'),
(@ordonnance_id, NULL, 'Gaviscon suspension buvable', '10ml', 1, 30, '2 semaines', '1 sachet après chaque repas et au coucher', '4 fois par jour', 'Orale', 'Suspension buvable', 'Prendre après les repas et au coucher. Bien agiter avant emploi.'),
(@ordonnance_id, NULL, 'Métoclopramide 10mg', '10mg', 1, 15, '5 jours', '1 comprimé 3 fois par jour avant les repas', '3 fois par jour', 'Orale', 'Comprimé', 'Prendre 15-30 minutes avant les repas. Arrêter si somnolence importante.'),
(@ordonnance_id, NULL, 'Spasfon Lyoc 80mg', '80mg', 1, 10, 'Si besoin', '1 à 2 comprimés en cas de crampes', 'Si besoin', 'Sublinguale', 'Lyophilisat oral', 'Laisser fondre sous la langue en cas de douleurs spasmodiques. Maximum 6 par jour.');

-- 4. EXAMENS PRESCRITS (bulletin_examen)
INSERT INTO bulletin_examen (date_demande, id_labo, id_consultation, instructions, id_exam, urgence, statut) VALUES
(CURDATE(), 1, @consultation_id, 'Recherche d''une anémie (saignement occulte)', 1, 0, 'prescrit'),
(CURDATE(), 1, @consultation_id, 'Évaluation du syndrome inflammatoire', 2, 0, 'prescrit'),
(CURDATE(), 1, @consultation_id, 'Recherche d''infection à H. pylori', 3, 0, 'prescrit'),
(CURDATE(), 1, @consultation_id, 'Éliminer une atteinte hépatique ou biliaire associée', 4, 0, 'prescrit');

-- 5. FACTURE
INSERT INTO facture (
    id_patient,
    id_consultation,
    type_facture,
    montant_total,
    montant_assurance,
    montant_patient,
    Total,
    net_a_payer,
    statut,
    date_creation
) VALUES (
    @patient_id,
    @consultation_id,
    'consultation',
    15000,
    0,
    15000,
    15000,
    15000,
    'payee',
    NOW()
);

-- Afficher le résumé
SELECT '✅ CONSULTATION CRÉÉE AVEC SUCCÈS' AS Resultat;
SELECT @consultation_id AS ID_Consultation;

SELECT 
    c.id_consultation,
    CONCAT(up.prenom, ' ', up.nom) AS Patient,
    CONCAT('Dr. ', um.prenom, ' ', um.nom) AS Medecin,
    c.date_heure AS Date_Consultation,
    LEFT(c.motif, 60) AS Motif,
    c.statut,
    LEFT(c.diagnostic, 50) AS Diagnostic
FROM consultation c
JOIN utilisateurs up ON c.id_patient = up.id_user
JOIN utilisateurs um ON c.id_medecin = um.id_user
WHERE c.id_consultation = @consultation_id;

SELECT '📋 ORDONNANCE:' AS Info;
SELECT om.nom_medicament_libre AS Medicament, om.dosage_libre AS Dosage, om.frequence, om.duree_traitement AS Duree, om.quantite AS Qte
FROM ordonnance_medicament om
JOIN ordonnance o ON om.id_ordonnance = o.id_ordonnance
WHERE o.id_consultation = @consultation_id;

SELECT '🔬 EXAMENS PRESCRITS:' AS Info;
SELECT e.nom_examen AS Examen, be.statut 
FROM bulletin_examen be 
JOIN examens e ON be.id_exam = e.id_exam
WHERE be.id_consultation = @consultation_id;
