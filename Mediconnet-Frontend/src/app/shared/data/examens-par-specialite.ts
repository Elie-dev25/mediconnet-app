/**
 * Configuration centralisée des examens médicaux par spécialité
 * Chaque spécialité a ses propres catégories et examens associés
 * 
 * Structure:
 * - specialiteKey: clé normalisée de la spécialité (lowercase, sans accents)
 * - categories: liste des catégories d'examens avec leurs examens
 */

export interface ExamenDefinition {
  nom: string;
  description?: string;
}

export interface CategorieExamenDefinition {
  type: string;
  label: string;
  icon: string;
  examens: ExamenDefinition[];
}

export interface SpecialiteExamensConfig {
  specialiteKey: string;
  specialiteLabel: string;
  categories: CategorieExamenDefinition[];
}

/**
 * Examens pour le médecin généraliste (spécialité par défaut)
 */
const EXAMENS_MEDECINE_GENERALE: CategorieExamenDefinition[] = [
  {
    type: 'biologie',
    label: 'Biologie / Analyses',
    icon: 'test-tube',
    examens: [
      { nom: 'NFS (Numération Formule Sanguine)' },
      { nom: 'Glycémie à jeun' },
      { nom: 'HbA1c' },
      { nom: 'Bilan lipidique complet' },
      { nom: 'Bilan rénal (Urée, Créatinine)' },
      { nom: 'Bilan hépatique' },
      { nom: 'Ionogramme sanguin' },
      { nom: 'CRP (Protéine C-Réactive)' },
      { nom: 'VS (Vitesse de Sédimentation)' },
      { nom: 'TSH / T3 / T4' },
      { nom: 'Bilan martial (Fer, Ferritine)' },
      { nom: 'Groupe sanguin / Rhésus' },
      { nom: 'TP / INR' },
      { nom: 'D-Dimères' },
      { nom: 'Troponine' },
      { nom: 'BNP / NT-proBNP' },
      { nom: 'ECBU' },
      { nom: 'Hémocultures' },
      { nom: 'Sérologies' }
    ]
  },
  {
    type: 'imagerie',
    label: 'Imagerie médicale',
    icon: 'scan-line',
    examens: [
      { nom: 'Radiographie thoracique' },
      { nom: 'Radiographie osseuse' },
      { nom: 'Échographie abdominale' },
      { nom: 'Échographie pelvienne' },
      { nom: 'Échographie cardiaque' },
      { nom: 'Scanner thoracique' },
      { nom: 'Scanner abdomino-pelvien' },
      { nom: 'Scanner cérébral' },
      { nom: 'IRM cérébrale' },
      { nom: 'IRM lombaire' },
      { nom: 'IRM articulaire' },
      { nom: 'Mammographie' },
      { nom: 'Doppler veineux' },
      { nom: 'Doppler artériel' }
    ]
  },
  {
    type: 'cardiologie',
    label: 'Cardiologie',
    icon: 'heart-pulse',
    examens: [
      { nom: 'ECG (Électrocardiogramme)' },
      { nom: 'Holter ECG 24h' },
      { nom: 'Holter tensionnel (MAPA)' },
      { nom: 'Échocardiographie' },
      { nom: 'Épreuve d\'effort' },
      { nom: 'Coronarographie' }
    ]
  },
  {
    type: 'neurologie',
    label: 'Neurologie',
    icon: 'brain-circuit',
    examens: [
      { nom: 'EEG (Électroencéphalogramme)' },
      { nom: 'EMG (Électromyogramme)' },
      { nom: 'Potentiels évoqués' },
      { nom: 'Ponction lombaire' }
    ]
  }
];

/**
 * Examens pour le gynécologue (Obstétrique et Gynécologie)
 */
const EXAMENS_GYNECOLOGIE: CategorieExamenDefinition[] = [
  {
    type: 'biologie',
    label: 'Analyses biologiques',
    icon: 'test-tube',
    examens: [
      { nom: 'Dosage TSH' },
      { nom: 'Dosage FSH' },
      { nom: 'Dosage LH' },
      { nom: 'Dosage Œstradiol' },
      { nom: 'Dosage Progestérone' },
      { nom: 'Dosage Prolactine' },
      { nom: 'Dosage Androgènes (Testostérone, DHEA-S)' },
      { nom: 'Dosage AMH (Hormone Anti-Müllérienne)' },
      { nom: 'Bilan sanguin prénatal complet' },
      { nom: 'NFS (Numération Formule Sanguine)' },
      { nom: 'Groupe sanguin / Rhésus / RAI' },
      { nom: 'Sérologies prénatales (Toxoplasmose, Rubéole, CMV, VIH, Hépatite B)' },
      { nom: 'Glycémie à jeun' },
      { nom: 'HGPO (Hyperglycémie provoquée orale)' },
      { nom: 'Marqueur tumoral CA-125' },
      { nom: 'Marqueur tumoral CA 15-3' },
      { nom: 'Bilan de ménopause (Vitamine D, Bilan lipidique)' },
      { nom: 'Bêta-HCG quantitatif' },
      { nom: 'Bilan thyroïdien (TSH, T3, T4)' }
    ]
  },
  {
    type: 'imagerie',
    label: 'Imagerie médicale',
    icon: 'scan-line',
    examens: [
      { nom: 'Échographie pelvienne endovaginale' },
      { nom: 'Échographie pelvienne sus-pubienne' },
      { nom: 'Échographie de datation (1er trimestre)' },
      { nom: 'Échographie morphologique (2ème trimestre)' },
      { nom: 'Échographie de croissance (3ème trimestre)' },
      { nom: 'Échographie de suivi de grossesse' },
      { nom: 'Mammographie bilatérale' },
      { nom: 'Échographie mammaire' },
      { nom: 'IRM pelvienne' },
      { nom: 'IRM mammaire' },
      { nom: 'Hystérosalpingographie' },
      { nom: 'Ostéodensitométrie (DMO)' },
      { nom: 'Doppler utérin' },
      { nom: 'Doppler ombilical' }
    ]
  },
  {
    type: 'prelevements',
    label: 'Prélèvements / Anatomopathologie',
    icon: 'microscope',
    examens: [
      { nom: 'Frottis cervico-utérin (FCU)' },
      { nom: 'Test HPV (Papillomavirus)' },
      { nom: 'Prélèvement vaginal (IST)' },
      { nom: 'Prélèvement urétral' },
      { nom: 'Prélèvement endocervical' },
      { nom: 'Colposcopie avec biopsie' },
      { nom: 'Biopsie endométriale (Pipelle de Cornier)' },
      { nom: 'Biopsie mammaire (microbiopsie)' },
      { nom: 'Biopsie mammaire (macrobiopsie)' },
      { nom: 'Biopsie vulvaire' },
      { nom: 'Cytoponction mammaire' },
      { nom: 'Amniocentèse' },
      { nom: 'Biopsie de trophoblaste (choriocentèse)' }
    ]
  },
  {
    type: 'endoscopie',
    label: 'Examens endoscopiques / Invasifs',
    icon: 'scan-eye',
    examens: [
      { nom: 'Hystéroscopie diagnostique' },
      { nom: 'Hystéroscopie opératoire' },
      { nom: 'Cœlioscopie exploratrice' },
      { nom: 'Cœlioscopie opératoire' },
      { nom: 'Colposcopie' },
      { nom: 'Hystérosalpingographie (HSG)' },
      { nom: 'Salpingoscopie' }
    ]
  }
];

/**
 * Examens pour le chirurgien général
 */
const EXAMENS_CHIRURGIE_GENERALE: CategorieExamenDefinition[] = [
  {
    type: 'biologie',
    label: 'Examens biologiques',
    icon: 'test-tube',
    examens: [
      { nom: 'NFS (Numération Formule Sanguine)' },
      { nom: 'CRP (Protéine C-Réactive)' },
      { nom: 'VS (Vitesse de Sédimentation)' },
      { nom: 'Ionogramme sanguin' },
      { nom: 'Créatinine / Urée' },
      { nom: 'Glycémie à jeun' },
      { nom: 'Bilan hépatique complet (ASAT, ALAT, GGT, PAL, Bilirubine)' },
      { nom: 'Bilan d\'hémostase (TP, TCA, Fibrinogène)' },
      { nom: 'Groupe sanguin / Rhésus' },
      { nom: 'RAI (Recherche d\'Agglutinines Irrégulières)' },
      { nom: 'ECBU (Examen Cytobactériologique des Urines)' },
      { nom: 'Bêta-HCG (si femme en âge de procréer)' },
      { nom: 'Sérologies pré-opératoires (VIH, Hépatite B, Hépatite C)' },
      { nom: 'Marqueur tumoral ACE' },
      { nom: 'Marqueur tumoral CA 19-9' },
      { nom: 'Marqueur tumoral AFP' },
      { nom: 'Procalcitonine' },
      { nom: 'Lactates' },
      { nom: 'Amylase / Lipase' }
    ]
  },
  {
    type: 'imagerie',
    label: 'Imagerie médicale',
    icon: 'scan-line',
    examens: [
      { nom: 'Échographie abdominale' },
      { nom: 'Échographie hépatobiliaire' },
      { nom: 'Échographie thyroïdienne' },
      { nom: 'Doppler veineux membres inférieurs' },
      { nom: 'Doppler artériel' },
      { nom: 'Radiographie thoracique' },
      { nom: 'Radiographie ASP (Abdomen Sans Préparation)' },
      { nom: 'Scanner abdomino-pelvien' },
      { nom: 'Scanner thoraco-abdomino-pelvien (TAP)' },
      { nom: 'Scanner thoracique' },
      { nom: 'IRM abdomino-pelvienne' },
      { nom: 'IRM hépatique' },
      { nom: 'Transit œso-gastro-duodénal (TOGD)' },
      { nom: 'Transit du grêle' },
      { nom: 'Lavement baryté' },
      { nom: 'Entéro-scanner' },
      { nom: 'Entéro-IRM' },
      { nom: 'Cholangio-IRM (Bili-IRM)' }
    ]
  },
  {
    type: 'endoscopie',
    label: 'Explorations endoscopiques',
    icon: 'scan-eye',
    examens: [
      { nom: 'Gastroscopie (FOGD)' },
      { nom: 'Coloscopie' },
      { nom: 'Sigmoïdoscopie' },
      { nom: 'Écho-endoscopie digestive haute' },
      { nom: 'Écho-endoscopie digestive basse' },
      { nom: 'CPRE (Cholangio-Pancréatographie Rétrograde Endoscopique)' },
      { nom: 'Entéroscopie' },
      { nom: 'Vidéocapsule endoscopique' },
      { nom: 'Rectoscopie' },
      { nom: 'Anuscopie' },
      { nom: 'Bronchoscopie' }
    ]
  },
  {
    type: 'preoperatoire',
    label: 'Examens pré/post-opératoires',
    icon: 'activity',
    examens: [
      { nom: 'ECG (Électrocardiogramme)' },
      { nom: 'Échocardiographie transthoracique' },
      { nom: 'EFR / Spirométrie' },
      { nom: 'Gaz du sang artériel' },
      { nom: 'Consultation d\'anesthésie' },
      { nom: 'Bilan pré-opératoire standard' },
      { nom: 'Radiographie thoracique pré-opératoire' },
      { nom: 'Évaluation nutritionnelle (Albumine, Préalbumine)' },
      { nom: 'Score ASA' },
      { nom: 'Surveillance post-opératoire (drain, cicatrice)' },
      { nom: 'Contrôle biologique post-opératoire' }
    ]
  }
];

/**
 * Examens pour le cardiologue
 */
const EXAMENS_CARDIOLOGIE: CategorieExamenDefinition[] = [
  {
    type: 'biologie',
    label: 'Analyses biologiques',
    icon: 'test-tube',
    examens: [
      { nom: 'Troponine I / T' },
      { nom: 'BNP / NT-proBNP' },
      { nom: 'D-Dimères' },
      { nom: 'Bilan lipidique complet' },
      { nom: 'HbA1c' },
      { nom: 'Glycémie à jeun' },
      { nom: 'Créatinine / DFG' },
      { nom: 'Ionogramme sanguin' },
      { nom: 'NFS' },
      { nom: 'CRP ultrasensible' },
      { nom: 'Bilan thyroïdien (TSH)' },
      { nom: 'Homocystéine' },
      { nom: 'Lipoprotéine (a)' }
    ]
  },
  {
    type: 'imagerie',
    label: 'Imagerie cardiaque',
    icon: 'heart-pulse',
    examens: [
      { nom: 'Échocardiographie transthoracique (ETT)' },
      { nom: 'Échocardiographie transœsophagienne (ETO)' },
      { nom: 'Échocardiographie de stress' },
      { nom: 'Scanner coronaire (Coroscanner)' },
      { nom: 'IRM cardiaque' },
      { nom: 'Scintigraphie myocardique' },
      { nom: 'TEP cardiaque' },
      { nom: 'Radiographie thoracique' },
      { nom: 'Doppler des troncs supra-aortiques' },
      { nom: 'Doppler artériel des membres inférieurs' }
    ]
  },
  {
    type: 'electrophysiologie',
    label: 'Électrophysiologie',
    icon: 'activity',
    examens: [
      { nom: 'ECG 12 dérivations' },
      { nom: 'Holter ECG 24h' },
      { nom: 'Holter ECG 48h-72h' },
      { nom: 'Holter tensionnel (MAPA)' },
      { nom: 'Épreuve d\'effort sur tapis roulant' },
      { nom: 'Épreuve d\'effort sur vélo' },
      { nom: 'Test d\'inclinaison (Tilt-test)' },
      { nom: 'Exploration électrophysiologique endocavitaire' }
    ]
  },
  {
    type: 'invasif',
    label: 'Examens invasifs',
    icon: 'scan-eye',
    examens: [
      { nom: 'Coronarographie' },
      { nom: 'Cathétérisme cardiaque droit' },
      { nom: 'Cathétérisme cardiaque gauche' },
      { nom: 'Angioplastie coronaire' },
      { nom: 'Biopsie endomyocardique' }
    ]
  }
];

/**
 * Examens pour le pédiatre
 */
const EXAMENS_PEDIATRIE: CategorieExamenDefinition[] = [
  {
    type: 'biologie',
    label: 'Analyses biologiques',
    icon: 'test-tube',
    examens: [
      { nom: 'NFS (Numération Formule Sanguine)' },
      { nom: 'CRP' },
      { nom: 'Glycémie' },
      { nom: 'Ionogramme sanguin' },
      { nom: 'Bilan rénal' },
      { nom: 'Bilan hépatique' },
      { nom: 'Bilan martial (Fer, Ferritine)' },
      { nom: 'TSH néonatale' },
      { nom: 'Bilan phosphocalcique' },
      { nom: 'Vitamine D' },
      { nom: 'ECBU' },
      { nom: 'Coproculture' },
      { nom: 'Test de la sueur' },
      { nom: 'Sérologies vaccinales' },
      { nom: 'Dépistage néonatal (Guthrie)' }
    ]
  },
  {
    type: 'imagerie',
    label: 'Imagerie médicale',
    icon: 'scan-line',
    examens: [
      { nom: 'Radiographie thoracique' },
      { nom: 'Radiographie osseuse (âge osseux)' },
      { nom: 'Échographie abdominale' },
      { nom: 'Échographie rénale' },
      { nom: 'Échographie transfontanellaire' },
      { nom: 'Échographie des hanches' },
      { nom: 'Scanner cérébral' },
      { nom: 'IRM cérébrale' }
    ]
  },
  {
    type: 'fonctionnel',
    label: 'Examens fonctionnels',
    icon: 'activity',
    examens: [
      { nom: 'EFR pédiatrique' },
      { nom: 'Test de provocation bronchique' },
      { nom: 'Prick-tests allergologiques' },
      { nom: 'Tests cutanés' },
      { nom: 'Audiométrie' },
      { nom: 'Potentiels évoqués auditifs (PEA)' },
      { nom: 'ECG pédiatrique' },
      { nom: 'Échocardiographie pédiatrique' }
    ]
  },
  {
    type: 'neurologie',
    label: 'Neurologie pédiatrique',
    icon: 'brain-circuit',
    examens: [
      { nom: 'EEG pédiatrique' },
      { nom: 'EEG de sommeil' },
      { nom: 'EMG pédiatrique' },
      { nom: 'Ponction lombaire' },
      { nom: 'Bilan neuropsychologique' }
    ]
  }
];

/**
 * Examens pour le neurologue
 */
const EXAMENS_NEUROLOGIE: CategorieExamenDefinition[] = [
  {
    type: 'biologie',
    label: 'Analyses biologiques',
    icon: 'test-tube',
    examens: [
      { nom: 'NFS' },
      { nom: 'Ionogramme sanguin' },
      { nom: 'Glycémie' },
      { nom: 'Bilan thyroïdien (TSH, T3, T4)' },
      { nom: 'Vitamine B12' },
      { nom: 'Folates' },
      { nom: 'Bilan hépatique' },
      { nom: 'Bilan rénal' },
      { nom: 'CRP' },
      { nom: 'VS' },
      { nom: 'Sérologies (Lyme, VIH, Syphilis)' },
      { nom: 'Anticorps anti-neuronaux' },
      { nom: 'Électrophorèse des protéines' },
      { nom: 'Dosage cuivre / céruloplasmine' }
    ]
  },
  {
    type: 'imagerie',
    label: 'Imagerie cérébrale',
    icon: 'brain-circuit',
    examens: [
      { nom: 'IRM cérébrale' },
      { nom: 'IRM médullaire' },
      { nom: 'Angio-IRM cérébrale' },
      { nom: 'Scanner cérébral' },
      { nom: 'Angioscanner cérébral' },
      { nom: 'TEP cérébrale' },
      { nom: 'SPECT cérébral' },
      { nom: 'Doppler transcrânien' },
      { nom: 'Doppler des troncs supra-aortiques' }
    ]
  },
  {
    type: 'electrophysiologie',
    label: 'Électrophysiologie',
    icon: 'activity',
    examens: [
      { nom: 'EEG standard' },
      { nom: 'EEG de sommeil' },
      { nom: 'EEG prolongé (vidéo-EEG)' },
      { nom: 'EMG (Électromyogramme)' },
      { nom: 'Étude des vitesses de conduction nerveuse' },
      { nom: 'Potentiels évoqués visuels (PEV)' },
      { nom: 'Potentiels évoqués auditifs (PEA)' },
      { nom: 'Potentiels évoqués somesthésiques (PES)' },
      { nom: 'Potentiels évoqués moteurs (PEM)' }
    ]
  },
  {
    type: 'invasif',
    label: 'Examens invasifs',
    icon: 'scan-eye',
    examens: [
      { nom: 'Ponction lombaire' },
      { nom: 'Analyse du LCR' },
      { nom: 'Biopsie nerveuse' },
      { nom: 'Biopsie musculaire' },
      { nom: 'Artériographie cérébrale' }
    ]
  }
];

/**
 * Examens pour l'urologue
 */
const EXAMENS_UROLOGIE: CategorieExamenDefinition[] = [
  {
    type: 'biologie',
    label: 'Analyses biologiques',
    icon: 'test-tube',
    examens: [
      { nom: 'PSA total' },
      { nom: 'PSA libre / PSA total' },
      { nom: 'Créatinine / DFG' },
      { nom: 'ECBU' },
      { nom: 'Cytologie urinaire' },
      { nom: 'Bilan phosphocalcique' },
      { nom: 'Uricémie' },
      { nom: 'Bêta-HCG' },
      { nom: 'Alpha-fœtoprotéine (AFP)' },
      { nom: 'LDH' },
      { nom: 'Testostérone totale' },
      { nom: 'Spermogramme' },
      { nom: 'Spermoculture' }
    ]
  },
  {
    type: 'imagerie',
    label: 'Imagerie médicale',
    icon: 'scan-line',
    examens: [
      { nom: 'Échographie rénale' },
      { nom: 'Échographie vésicale' },
      { nom: 'Échographie prostatique sus-pubienne' },
      { nom: 'Échographie prostatique endorectale' },
      { nom: 'Échographie scrotale' },
      { nom: 'Scanner abdomino-pelvien' },
      { nom: 'Uro-scanner' },
      { nom: 'IRM prostatique multiparamétrique' },
      { nom: 'IRM rénale' },
      { nom: 'Urographie intraveineuse (UIV)' },
      { nom: 'Cystographie rétrograde' },
      { nom: 'Urétrographie' }
    ]
  },
  {
    type: 'fonctionnel',
    label: 'Examens fonctionnels',
    icon: 'activity',
    examens: [
      { nom: 'Débitmétrie urinaire' },
      { nom: 'Bilan urodynamique complet' },
      { nom: 'Cystomanométrie' },
      { nom: 'Profilométrie urétrale' },
      { nom: 'Mesure du résidu post-mictionnel' }
    ]
  },
  {
    type: 'endoscopie',
    label: 'Examens endoscopiques',
    icon: 'scan-eye',
    examens: [
      { nom: 'Cystoscopie' },
      { nom: 'Urétéroscopie' },
      { nom: 'Néphroscopie' },
      { nom: 'Biopsie prostatique' },
      { nom: 'Biopsie rénale' },
      { nom: 'Biopsie vésicale' }
    ]
  }
];

/**
 * Examens pour l'anesthésiste (Consultation pré-anesthésique)
 */
const EXAMENS_ANESTHESIE: CategorieExamenDefinition[] = [
  {
    type: 'cardiaque',
    label: 'Examens cardiaques',
    icon: 'heart-pulse',
    examens: [
      { nom: 'ECG (Électrocardiogramme)', description: 'Évaluation du rythme cardiaque' },
      { nom: 'Échocardiographie transthoracique', description: 'Évaluation de la fonction cardiaque' },
      { nom: 'Échocardiographie transœsophagienne', description: 'Évaluation cardiaque approfondie' },
      { nom: 'Test d\'effort', description: 'Évaluation de la tolérance à l\'effort' },
      { nom: 'Holter ECG 24h', description: 'Surveillance du rythme cardiaque sur 24h' },
      { nom: 'Holter tensionnel (MAPA)', description: 'Surveillance de la tension artérielle sur 24h' },
      { nom: 'Coronarographie', description: 'Visualisation des artères coronaires' },
      { nom: 'Scanner cardiaque', description: 'Imagerie cardiaque par scanner' },
      { nom: 'IRM cardiaque', description: 'Imagerie cardiaque par résonance magnétique' }
    ]
  },
  {
    type: 'respiratoire',
    label: 'Examens respiratoires',
    icon: 'wind',
    examens: [
      { nom: 'EFR (Exploration Fonctionnelle Respiratoire)', description: 'Évaluation complète de la fonction pulmonaire' },
      { nom: 'Spirométrie', description: 'Mesure des volumes et débits respiratoires' },
      { nom: 'Radiographie thoracique', description: 'Imagerie pulmonaire standard' },
      { nom: 'Scanner thoracique', description: 'Imagerie pulmonaire détaillée' },
      { nom: 'Gaz du sang artériel', description: 'Mesure de l\'oxygénation et du pH sanguin' },
      { nom: 'Oxymétrie nocturne', description: 'Surveillance de la saturation pendant le sommeil' },
      { nom: 'Polysomnographie', description: 'Diagnostic de l\'apnée du sommeil' },
      { nom: 'Test de marche de 6 minutes', description: 'Évaluation de la capacité fonctionnelle' }
    ]
  },
  {
    type: 'neurologique',
    label: 'Examens neurologiques',
    icon: 'brain',
    examens: [
      { nom: 'Scanner cérébral', description: 'Imagerie cérébrale par scanner' },
      { nom: 'IRM cérébrale', description: 'Imagerie cérébrale par résonance magnétique' },
      { nom: 'EEG (Électroencéphalogramme)', description: 'Évaluation de l\'activité électrique cérébrale' },
      { nom: 'Doppler des troncs supra-aortiques', description: 'Évaluation des artères cérébrales' },
      { nom: 'Potentiels évoqués', description: 'Évaluation des voies nerveuses' }
    ]
  },
  {
    type: 'biologie',
    label: 'Bilan biologique pré-opératoire',
    icon: 'test-tube',
    examens: [
      { nom: 'NFS (Numération Formule Sanguine)', description: 'Hémoglobine, plaquettes, globules blancs' },
      { nom: 'Groupe sanguin / Rhésus', description: 'Typage sanguin' },
      { nom: 'RAI (Recherche d\'Agglutinines Irrégulières)', description: 'Compatibilité transfusionnelle' },
      { nom: 'Bilan d\'hémostase (TP, TCA, INR)', description: 'Évaluation de la coagulation' },
      { nom: 'Fibrinogène', description: 'Facteur de coagulation' },
      { nom: 'Ionogramme sanguin', description: 'Électrolytes (Na, K, Cl)' },
      { nom: 'Créatinine / Urée', description: 'Fonction rénale' },
      { nom: 'Clairance de la créatinine', description: 'Évaluation précise de la fonction rénale' },
      { nom: 'Glycémie à jeun', description: 'Dépistage du diabète' },
      { nom: 'HbA1c', description: 'Équilibre glycémique sur 3 mois' },
      { nom: 'Bilan hépatique (ASAT, ALAT, GGT, PAL)', description: 'Fonction hépatique' },
      { nom: 'Albumine', description: 'État nutritionnel' },
      { nom: 'Préalbumine', description: 'État nutritionnel récent' },
      { nom: 'Troponine', description: 'Marqueur cardiaque' },
      { nom: 'BNP / NT-proBNP', description: 'Marqueur d\'insuffisance cardiaque' }
    ]
  },
  {
    type: 'coagulation',
    label: 'Bilan de coagulation approfondi',
    icon: 'droplets',
    examens: [
      { nom: 'TP / INR', description: 'Temps de prothrombine' },
      { nom: 'TCA', description: 'Temps de céphaline activée' },
      { nom: 'Fibrinogène', description: 'Facteur de coagulation' },
      { nom: 'D-Dimères', description: 'Marqueur de thrombose' },
      { nom: 'Facteurs de coagulation (II, V, VII, X)', description: 'Dosage des facteurs' },
      { nom: 'Facteur VIII', description: 'Hémophilie A' },
      { nom: 'Facteur IX', description: 'Hémophilie B' },
      { nom: 'Facteur von Willebrand', description: 'Maladie de von Willebrand' },
      { nom: 'Temps de saignement', description: 'Fonction plaquettaire' },
      { nom: 'Agrégation plaquettaire', description: 'Fonction plaquettaire détaillée' },
      { nom: 'Antithrombine III', description: 'Inhibiteur de la coagulation' },
      { nom: 'Protéine C / Protéine S', description: 'Anticoagulants naturels' },
      { nom: 'Thromboélastographie (TEG)', description: 'Évaluation globale de la coagulation' }
    ]
  },
  {
    type: 'allergologie',
    label: 'Tests allergologiques',
    icon: 'shield-alert',
    examens: [
      { nom: 'Tests cutanés aux anesthésiques locaux', description: 'Allergie aux anesthésiques' },
      { nom: 'Tests cutanés aux curares', description: 'Allergie aux curares' },
      { nom: 'Tests cutanés au latex', description: 'Allergie au latex' },
      { nom: 'IgE spécifiques latex', description: 'Dosage sanguin allergie latex' },
      { nom: 'IgE spécifiques médicaments', description: 'Dosage sanguin allergie médicamenteuse' },
      { nom: 'Tryptase sérique', description: 'Marqueur de réaction anaphylactique' },
      { nom: 'Histamine plasmatique', description: 'Marqueur de réaction allergique' },
      { nom: 'Test de provocation médicamenteuse', description: 'Confirmation d\'allergie médicamenteuse' }
    ]
  },
  {
    type: 'imagerie',
    label: 'Imagerie générale',
    icon: 'scan-line',
    examens: [
      { nom: 'Radiographie thoracique', description: 'Évaluation pulmonaire et cardiaque' },
      { nom: 'Radiographie du rachis cervical', description: 'Évaluation pour intubation' },
      { nom: 'Scanner (TDM)', description: 'Imagerie par scanner' },
      { nom: 'IRM', description: 'Imagerie par résonance magnétique' },
      { nom: 'Échographie cardiaque', description: 'Évaluation cardiaque' },
      { nom: 'Échographie des voies aériennes', description: 'Évaluation pour intubation difficile' },
      { nom: 'Échographie abdominale', description: 'Évaluation abdominale' }
    ]
  }
];

/**
 * Map des examens par clé de spécialité normalisée
 */
export const EXAMENS_PAR_SPECIALITE: Map<string, CategorieExamenDefinition[]> = new Map([
  ['medecine_generale', EXAMENS_MEDECINE_GENERALE],
  ['medecine-generale', EXAMENS_MEDECINE_GENERALE],
  ['generaliste', EXAMENS_MEDECINE_GENERALE],
  ['default', EXAMENS_MEDECINE_GENERALE],
  
  ['gynecologie', EXAMENS_GYNECOLOGIE],
  ['obstetrique_gynecologie', EXAMENS_GYNECOLOGIE],
  ['obstetrique-et-gynecologie', EXAMENS_GYNECOLOGIE],
  ['obstetrique et gynecologie', EXAMENS_GYNECOLOGIE],
  
  ['chirurgie_generale', EXAMENS_CHIRURGIE_GENERALE],
  ['chirurgie-generale', EXAMENS_CHIRURGIE_GENERALE],
  ['chirurgie generale', EXAMENS_CHIRURGIE_GENERALE],
  ['chirurgien', EXAMENS_CHIRURGIE_GENERALE],
  
  ['cardiologie', EXAMENS_CARDIOLOGIE],
  ['cardiologue', EXAMENS_CARDIOLOGIE],
  
  ['pediatrie', EXAMENS_PEDIATRIE],
  ['pédiatrie', EXAMENS_PEDIATRIE],
  ['pediatre', EXAMENS_PEDIATRIE],
  
  ['neurologie', EXAMENS_NEUROLOGIE],
  ['neurologue', EXAMENS_NEUROLOGIE],
  
  ['urologie', EXAMENS_UROLOGIE],
  ['urologue', EXAMENS_UROLOGIE],
  
  ['anesthesiologie', EXAMENS_ANESTHESIE],
  ['anesthesie', EXAMENS_ANESTHESIE],
  ['anesthesiste', EXAMENS_ANESTHESIE],
  ['anesthesiologie', EXAMENS_ANESTHESIE]
]);

/**
 * Normalise une chaîne de spécialité pour la recherche
 */
export function normalizeSpecialite(specialite: string): string {
  if (!specialite) return 'default';
  
  return specialite
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '') // Supprime les accents
    .replace(/[^a-z0-9]/g, '_')      // Remplace les caractères spéciaux par _
    .replace(/_+/g, '_')             // Supprime les _ multiples
    .replace(/^_|_$/g, '');          // Supprime les _ en début/fin
}

/**
 * Extrait la spécialité du titreAffiche (ex: "Médecin - Gynécologie" -> "gynecologie")
 */
export function extractSpecialiteFromTitre(titreAffiche: string | null | undefined): string {
  if (!titreAffiche) return 'default';
  
  // Format attendu: "Médecin - Spécialité" ou juste "Spécialité"
  const parts = titreAffiche.split('-');
  const specialitePart = parts.length > 1 ? parts[1].trim() : titreAffiche.trim();
  
  return normalizeSpecialite(specialitePart);
}

/**
 * Récupère les catégories d'examens pour une spécialité donnée
 */
export function getExamensForSpecialite(specialite: string): CategorieExamenDefinition[] {
  const normalizedKey = normalizeSpecialite(specialite);
  
  // Recherche directe
  if (EXAMENS_PAR_SPECIALITE.has(normalizedKey)) {
    return EXAMENS_PAR_SPECIALITE.get(normalizedKey)!;
  }
  
  // Recherche partielle (contient)
  for (const [key, value] of EXAMENS_PAR_SPECIALITE.entries()) {
    if (normalizedKey.includes(key) || key.includes(normalizedKey)) {
      return value;
    }
  }
  
  // Fallback: médecine générale
  return EXAMENS_MEDECINE_GENERALE;
}

/**
 * Récupère les catégories d'examens à partir du titreAffiche de l'utilisateur
 */
export function getExamensFromTitreAffiche(titreAffiche: string | null | undefined): CategorieExamenDefinition[] {
  const specialiteKey = extractSpecialiteFromTitre(titreAffiche);
  return getExamensForSpecialite(specialiteKey);
}

/**
 * Liste des spécialités supportées avec leurs labels
 */
export const SPECIALITES_SUPPORTEES: { key: string; label: string }[] = [
  { key: 'medecine_generale', label: 'Médecine Générale' },
  { key: 'gynecologie', label: 'Obstétrique et Gynécologie' },
  { key: 'chirurgie_generale', label: 'Chirurgie Générale' },
  { key: 'cardiologie', label: 'Cardiologie' },
  { key: 'pediatrie', label: 'Pédiatrie' },
  { key: 'neurologie', label: 'Neurologie' },
  { key: 'urologie', label: 'Urologie' },
  { key: 'anesthesiologie', label: 'Anesthésiologie' }
];
