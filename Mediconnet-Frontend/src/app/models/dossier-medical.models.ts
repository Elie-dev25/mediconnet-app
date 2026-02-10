/**
 * Interfaces centralisées pour le dossier médical
 * Utilisées par les composants et services liés au dossier patient
 */

export type ViewMode = 'patient' | 'medecin';
export type TabType = 'resume' | 'infos' | 'consultations' | 'ordonnances' | 'examens' | 'antecedents';

/**
 * Informations du patient dans le dossier médical
 */
export interface DossierPatientInfo {
  idUser?: number;
  nom: string;
  prenom: string;
  numeroDossier?: string;
  groupeSanguin?: string;
  naissance?: string;
  sexe?: string;
  age?: number;
  // Informations personnelles
  telephone?: string;
  email?: string;
  adresse?: string;
  nationalite?: string;
  regionOrigine?: string;
  situationMatrimoniale?: string;
  profession?: string;
  ethnie?: string;
  nbEnfants?: number;
  // Informations médicales
  maladiesChroniques?: string;
  allergiesConnues?: boolean;
  allergiesDetails?: string;
  antecedentsFamiliaux?: boolean;
  antecedentsFamiliauxDetails?: string;
  operationsChirurgicales?: boolean;
  operationsDetails?: string;
  // Habitudes de vie
  consommationAlcool?: boolean;
  frequenceAlcool?: string;
  tabagisme?: boolean;
  activitePhysique?: boolean;
  // Contact d'urgence
  personneContact?: string;
  numeroContact?: string;
  // Assurance
  nomAssurance?: string;
  numeroCarteAssurance?: string;
  couvertureAssurance?: number;
  dateDebutValidite?: string;
  dateFinValidite?: string;
}

/**
 * Statistiques du dossier médical
 */
export interface DossierStats {
  totalConsultations: number;
  totalOrdonnances: number;
  totalExamens: number;
  derniereVisite?: string;
}

/**
 * Item de consultation dans l'historique
 */
export interface ConsultationItem {
  idConsultation?: number;
  dateConsultation?: string;
  dateHeure?: string;
  motif: string;
  diagnosticPrincipal?: string;
  diagnostic?: string;
  nomMedecin?: string;
  medecinNom?: string;
  specialite?: string;
  statut: string;
}

/**
 * Item d'ordonnance dans l'historique
 */
export interface OrdonnanceItem {
  idOrdonnance: number;
  dateOrdonnance?: string;
  dateCreation?: string;
  nomMedecin?: string;
  statut?: string;
  medicaments: MedicamentItem[];
}

/**
 * Item de médicament dans une ordonnance
 */
export interface MedicamentItem {
  nom?: string;
  nomMedicament?: string;
  dosage?: string;
  frequence?: string;
  duree?: string;
  instructions?: string;
}

/**
 * Item d'examen dans l'historique
 */
export interface ExamenItem {
  idExamen: number;
  dateExamen?: string;
  datePrescription?: string;
  typeExamen: string;
  nomExamen: string;
  resultat?: string;
  resultats?: string;
  nomMedecin?: string;
  statut: string;
  urgent?: boolean;
}

/**
 * Item d'antécédent médical
 */
export interface AntecedentItem {
  type: string;
  description: string;
  dateDebut?: string;
  actif: boolean;
}

/**
 * Item d'allergie
 */
export interface AllergieItem {
  type: string;
  allergene: string;
  severite: string;
  reaction?: string;
}

/**
 * Structure complète du dossier médical
 */
export interface DossierMedicalData {
  patient: DossierPatientInfo;
  stats?: DossierStats;
  consultations: ConsultationItem[];
  ordonnances: OrdonnanceItem[];
  examens: ExamenItem[];
  antecedents: AntecedentItem[];
  allergies: AllergieItem[];
}

/**
 * Statuts possibles pour les différentes entités
 */
export const STATUTS = {
  CONSULTATION: {
    PLANIFIE: 'planifie',
    EN_COURS: 'en_cours',
    TERMINEE: 'terminee',
    ANNULEE: 'annulee'
  },
  EXAMEN: {
    PRESCRIT: 'prescrit',
    EN_COURS: 'en_cours',
    TERMINE: 'termine',
    ANNULE: 'annule'
  },
  HOSPITALISATION: {
    EN_ATTENTE: 'en_attente',
    EN_COURS: 'en_cours',
    TERMINE: 'termine'
  },
  ORDONNANCE: {
    EN_ATTENTE: 'en_attente',
    DELIVREE: 'delivree',
    PARTIELLEMENT_DELIVREE: 'partiellement_delivree'
  }
} as const;

/**
 * Helper pour vérifier si un statut est "terminé"
 */
export function isStatutTermine(statut: string | undefined): boolean {
  if (!statut) return false;
  const s = statut.toLowerCase();
  return s === 'termine' || s === 'terminee' || s === 'realise';
}

/**
 * Helper pour vérifier si un statut est "en cours"
 */
export function isStatutEnCours(statut: string | undefined): boolean {
  if (!statut) return false;
  const s = statut.toLowerCase();
  return s === 'en_cours' || s === 'actif';
}

/**
 * Helper pour vérifier si un statut est "annulé"
 */
export function isStatutAnnule(statut: string | undefined): boolean {
  if (!statut) return false;
  const s = statut.toLowerCase();
  return s === 'annule' || s === 'annulee';
}

/**
 * Obtenir le label d'affichage pour un statut
 */
export function getStatutLabel(statut: string | undefined): string {
  if (!statut) return '-';
  const s = statut.toLowerCase();
  switch (s) {
    case 'terminee':
    case 'termine':
      return 'Terminé';
    case 'en_cours':
      return 'En cours';
    case 'en_attente':
      return 'En attente';
    case 'planifie':
      return 'Planifié';
    case 'prescrit':
      return 'Prescrit';
    case 'realise':
      return 'Réalisé';
    case 'annule':
    case 'annulee':
      return 'Annulé';
    case 'delivree':
      return 'Délivrée';
    case 'partiellement_delivree':
      return 'Partiellement délivrée';
    default:
      return statut;
  }
}

/**
 * Obtenir la classe CSS pour un statut
 */
export function getStatutClass(statut: string | undefined): string {
  if (!statut) return '';
  const s = statut.toLowerCase();
  switch (s) {
    case 'terminee':
    case 'termine':
    case 'realise':
    case 'delivree':
      return 'success';
    case 'en_cours':
    case 'actif':
      return 'info';
    case 'en_attente':
    case 'prescrit':
    case 'planifie':
    case 'partiellement_delivree':
      return 'warning';
    case 'annulee':
    case 'annule':
      return 'danger';
    default:
      return '';
  }
}
