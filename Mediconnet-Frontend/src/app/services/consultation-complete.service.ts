import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ==================== INTERFACES ====================

export interface DossierPatientDto {
  idPatient: number;
  numeroDossier: string;
  nom: string;
  prenom: string;
  naissance?: Date;
  age?: number;
  sexe?: string;
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
  groupeSanguin?: string;
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
  // Dates
  dateCreation?: string;
  // Historique
  consultations: HistoriqueConsultationDto[];
  ordonnances: HistoriqueOrdonnanceDto[];
  examens: HistoriqueExamenDto[];
}

export interface HistoriqueConsultationDto {
  idConsultation: number;
  dateHeure: Date;
  motif?: string;
  diagnostic?: string;
  statut?: string;
  medecinNom: string;
  specialite?: string;
}

export interface HistoriqueOrdonnanceDto {
  idOrdonnance: number;
  dateCreation: Date;
  dureeTraitement?: string;
  medicaments: MedicamentDto[];
}

export interface HistoriqueExamenDto {
  idExamen: number;
  typeExamen: string;
  nomExamen: string;
  statut: string;
  datePrescription: Date;
  dateRealisation?: Date;
  resultats?: string;
}

export interface ConsultationEnCoursDto {
  idConsultation: number;
  idPatient: number;
  patientNom: string;
  patientPrenom: string;
  dateHeure: Date;
  motif?: string;
  statut?: string;
  etapeActuelle?: string;  // Étape sauvegardée pour reprise après pause
  isPremiereConsultation: boolean;
  specialiteId: number;
  // Workflow mis à jour
  anamnese?: AnamneseDto;
  examenClinique?: ExamenCliniqueDto;
  examenGynecologique?: ExamenGynecologiqueDto;
  diagnostic?: DiagnosticDto;
  planTraitement?: PlanTraitementDto;
  conclusion?: ConclusionDto;
  // Conservé pour compatibilité
  prescriptions?: PrescriptionsDto;
}

export interface ConsultationDetailDto {
  idConsultation: number;
  idPatient: number;
  patientNom: string;
  patientPrenom: string;
  numeroDossier?: string;
  dateConsultation: string;
  duree?: number;
  motif?: string;
  statut: string;
  anamnese?: string;
  notesCliniques?: string;
  diagnostic?: string;
  conclusion?: string;
  recommandations?: string;
  ordonnance?: OrdonnanceDto;
  examensPrescrits?: ExamenPrescritDetailDto[];
  questionnaire?: QuestionReponseDto[];
  parametresVitaux?: ParametresVitauxDto;
  examenClinique?: ExamenCliniqueDto;
  examenGynecologique?: ExamenGynecologiqueDto;
  planTraitement?: PlanTraitementDto;
  conclusionDetaillee?: ConclusionDto;
}

export interface ExamenPrescritDetailDto {
  idExamen?: number;
  nomExamen: string;
  instructions?: string;
  statut?: string;
}

export interface AnamneseDto {
  motifConsultation?: string;
  histoireMaladie?: string;
  antecedentsPersonnels?: string;
  antecedentsFamiliaux?: string;
  allergiesConnues?: string;
  traitementsEnCours?: string;
  habitudesVie?: string;
  questionsReponses: QuestionReponseDto[];
  parametresVitaux?: ParametresVitauxDto;
}

export interface QuestionReponseDto {
  questionId: string;
  question: string;
  reponse: string;
}

export interface ParametresVitauxDto {
  poids?: number;
  taille?: number;
  temperature?: number;
  tensionArterielle?: string;
  frequenceCardiaque?: number;
  frequenceRespiratoire?: number;
  saturationOxygene?: number;
  glycemie?: number;
}

// Étape 2: Examen Clinique
export interface ExamenCliniqueDto {
  parametresVitaux?: ParametresVitauxDto;
  parametresPrisParInfirmier: boolean;
  infirmierNom?: string;
  datePriseParametres?: Date;
  inspection?: string;
  palpation?: string;
  auscultation?: string;
  percussion?: string;
  autresObservations?: string;
}

export interface ExamenGynecologiqueDto {
  inspectionExterne?: string;
  examenSpeculum?: string;
  toucherVaginal?: string;
  autresObservations?: string;
}

// Étape 3: Diagnostic
export interface DiagnosticDto {
  examenClinique?: string;
  diagnosticPrincipal?: string;
  diagnosticsSecondaires?: string;
  hypothesesDiagnostiques?: string;
  notesCliniques?: string;
  recapitulatifPatient?: RecapitulatifPatientDto;
}

// Récapitulatif patient pour l'étape diagnostic
export interface RecapitulatifPatientDto {
  // Informations personnelles
  regionOrigine?: string;
  situationMatrimoniale?: string;
  profession?: string;
  nbEnfants?: number;
  ethnie?: string;
  // Informations médicales
  groupeSanguin?: string;
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
  // Diagnostics précédents
  diagnosticsPrecedents?: DiagnosticPrecedentDto[];
}

// Diagnostic précédent pour l'historique
export interface DiagnosticPrecedentDto {
  date: Date;
  diagnostic: string;
  medecinNom: string;
  medecinPrenom?: string;
  specialite?: string;
}

// Étape 4: Plan de Traitement
export interface PlanTraitementDto {
  explicationDiagnostic?: string;
  optionsTraitement?: string;
  ordonnance?: OrdonnanceDto;
  examensPrescrits: ExamenPrescritDto[];
  orientationSpecialiste?: string;
  motifOrientation?: string;
  idSpecialisteOriente?: number;
}

// Étape 5: Conclusion
export interface ConclusionDto {
  resumeConsultation?: string;
  questionsPatient?: string;
  consignesPatient?: string;
  recommandations?: string;
  typeSuivi?: string;
  dateSuiviPrevue?: Date;
  notesSuivi?: string;
}

export interface PrescriptionsDto {
  ordonnance?: OrdonnanceDto;
  examens: ExamenPrescritDto[];
  orientations: OrientationPreConsultationDto[];
}

export interface OrdonnanceDto {
  idOrdonnance?: number;
  notes?: string;
  dureeTraitement?: string;
  medicaments: MedicamentDto[];
}

export interface MedicamentDto {
  idPrescription?: number;
  idMedicament?: number;
  nomMedicament: string;
  dosage?: string;
  posologie?: string;
  frequence?: string;
  duree?: string;
  voieAdministration?: string;
  formePharmaceutique?: string;
  instructions?: string;
  quantite?: number;
}

export interface ExamenPrescritDto {
  idExamen?: number;
  categorie?: string;
  specialite?: string;
  typeExamen?: string;
  nomExamen: string;
  description?: string;
  urgence: boolean;
  notes?: string;
  disponible?: boolean;
  idLaboratoire?: number;
  nomLaboratoire?: string;
}

export interface LaboratoireDto {
  idLabo: number;
  nomLabo: string;
  contact?: string;
  adresse?: string;
  telephone?: string;
  type?: string;
}

// ==================== ORIENTATION PRE-CONSULTATION (UNIFIÉ) ====================

/** Types d'orientation disponibles */
export const TYPES_ORIENTATION = {
  MEDECIN_INTERNE: 'medecin_interne',
  MEDECIN_EXTERNE: 'medecin_externe',
  HOPITAL: 'hopital',
  SERVICE_INTERNE: 'service_interne',
  LABORATOIRE: 'laboratoire'
} as const;

export type TypeOrientation = typeof TYPES_ORIENTATION[keyof typeof TYPES_ORIENTATION];

/** Statuts d'orientation */
export const STATUTS_ORIENTATION = {
  EN_ATTENTE: 'en_attente',
  ACCEPTEE: 'acceptee',
  REFUSEE: 'refusee',
  RDV_PRIS: 'rdv_pris',
  TERMINEE: 'terminee',
  ANNULEE: 'annulee'
} as const;

export type StatutOrientation = typeof STATUTS_ORIENTATION[keyof typeof STATUTS_ORIENTATION];

/** DTO pour afficher une orientation */
export interface OrientationPreConsultationDto {
  idOrientation: number;
  idConsultation: number;
  idPatient: number;
  typeOrientation: TypeOrientation;
  // Destination
  idSpecialite?: number;
  nomSpecialite?: string;
  idMedecinOriente?: number;
  nomMedecinOriente?: string;
  nomDestinataire?: string;
  specialiteTexte?: string;
  adresseDestinataire?: string;
  telephoneDestinataire?: string;
  // Détails
  motif: string;
  notes?: string;
  urgence: boolean;
  prioritaire: boolean;
  // Suivi
  statut: StatutOrientation;
  dateOrientation: string;
  dateRdvPropose?: string;
  idRdvCree?: number;
  // Métadonnées
  medecinPrescripteur?: string;
  createdAt?: string;
}

/** Requête pour créer une orientation */
export interface CreateOrientationRequest {
  typeOrientation: TypeOrientation;
  // Pour médecin interne
  idSpecialite?: number;
  idMedecinOriente?: number;
  // Pour médecin externe / hôpital
  nomDestinataire?: string;
  specialiteTexte?: string;
  adresseDestinataire?: string;
  telephoneDestinataire?: string;
  // Détails
  motif: string;
  notes?: string;
  urgence: boolean;
  prioritaire: boolean;
  dateRdvPropose?: string;
}

/** Requête pour mettre à jour le statut */
export interface UpdateOrientationStatutRequest {
  statut: StatutOrientation;
  dateRdvPropose?: string;
  idRdvCree?: number;
  notes?: string;
}

export interface SpecialiteDto {
  idSpecialite: number;
  nomSpecialite: string;
  coutConsultation: number;
}

export interface MedecinSpecialisteDto {
  idUser: number;
  nom: string;
  prenom: string;
  nomComplet: string;
  idSpecialite: number;
  nomSpecialite?: string;
}

export interface ValiderConsultationRequest {
  conclusion?: string;
  imprimer: boolean;
}

export interface ConsultationRecapitulatifDto {
  consultation: ConsultationEnCoursDto;
  patient: DossierPatientDto;
}

export interface CreneauDisponible {
  heureDebut: string;
  heureFin: string;
  dateHeure: string;
  duree: number;
}

export interface CreneauxDisponiblesResponse {
  date: string;
  jourSemaine: number;
  creneaux: CreneauDisponible[];
}

export interface CreerRdvSuiviRequest {
  dateHeure: string;
  duree?: number;
  motif?: string;
  notes?: string;
}

export interface RdvSuiviResponse {
  success: boolean;
  message: string;
  idRendezVous?: number;
  dateHeure?: string;
  patientNom?: string;
  patientPrenom?: string;
}

export interface CreneauAvecStatut {
  heureDebut: string;
  heureFin: string;
  dateHeure: string;
  duree: number;
  statut: 'disponible' | 'occupe' | 'passe';
}

export interface CreneauxAvecStatutResponse {
  date: string;
  creneaux: CreneauAvecStatut[];
}

export interface CreneauxMedecinResponse {
  date: string;
  medecinDisponible: boolean;
  messageIndisponibilite?: string;
  creneaux: CreneauMedecinDto[];
}

export interface CreneauMedecinDto {
  dateHeure: string;
  heureDebut: string;
  heureFin: string;
  duree: number;
  statut: 'disponible' | 'occupe' | 'passe' | 'indisponible';
  selectionnable: boolean;
}

export interface CreerRdvOrientationRequest {
  dateHeure: string;
  duree?: number;
  motif?: string;
  notes?: string;
}

export interface CreerRdvOrientationResponse {
  success: boolean;
  message: string;
  idRendezVous?: number;
  dateHeure?: string;
}

// ==================== SERVICE ====================

@Injectable({
  providedIn: 'root'
})
export class ConsultationCompleteService {
  private readonly apiUrl = `${environment.apiUrl}/consultation`;

  constructor(private http: HttpClient) {}

  /**
   * Récupérer le dossier patient complet
   */
  getDossierPatient(idPatient: number): Observable<DossierPatientDto> {
    return this.http.get<DossierPatientDto>(`${this.apiUrl}/dossier-patient/${idPatient}`);
  }

  /**
   * Démarrer une consultation
   */
  demarrerConsultation(idConsultation: number): Observable<{ message: string; idConsultation: number }> {
    return this.http.post<{ message: string; idConsultation: number }>(
      `${this.apiUrl}/${idConsultation}/demarrer`, {}
    );
  }

  /**
   * Récupérer les données d'une consultation en cours
   */
  getConsultation(idConsultation: number): Observable<ConsultationEnCoursDto> {
    return this.http.get<ConsultationEnCoursDto>(`${this.apiUrl}/${idConsultation}`);
  }

  /**
   * Récupérer les détails complets d'une consultation
   */
  getConsultationDetails(idConsultation: number): Observable<ConsultationDetailDto> {
    return this.http.get<ConsultationDetailDto>(`${this.apiUrl}/${idConsultation}/details`);
  }

  /**
   * Sauvegarder l'anamnèse (étape 1)
   */
  saveAnamnese(idConsultation: number, anamnese: AnamneseDto): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/${idConsultation}/anamnese`, anamnese
    );
  }

  /**
   * Sauvegarder l'examen clinique (étape 2)
   */
  saveExamenClinique(idConsultation: number, examenClinique: ExamenCliniqueDto): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/${idConsultation}/examen-clinique`, examenClinique
    );
  }

  /**
   * Sauvegarder l'examen gynécologique (étape 2 bis)
   */
  saveExamenGynecologique(idConsultation: number, examenGynecologique: ExamenGynecologiqueDto): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/${idConsultation}/examen-gynecologique`, examenGynecologique
    );
  }

  /**
   * Sauvegarder le diagnostic (étape 3)
   */
  saveDiagnostic(idConsultation: number, diagnostic: DiagnosticDto): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/${idConsultation}/diagnostic`, diagnostic
    );
  }

  /**
   * Sauvegarder le plan de traitement (étape 4)
   */
  savePlanTraitement(idConsultation: number, planTraitement: PlanTraitementDto): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/${idConsultation}/plan-traitement`, planTraitement
    );
  }

  /**
   * Sauvegarder la conclusion (étape 5)
   */
  saveConclusion(idConsultation: number, conclusion: ConclusionDto): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/${idConsultation}/conclusion`, conclusion
    );
  }

  /**
   * Sauvegarder les prescriptions (conservé pour compatibilité)
   */
  savePrescriptions(idConsultation: number, prescriptions: PrescriptionsDto): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/${idConsultation}/prescriptions`, prescriptions
    );
  }

  /**
   * Valider et terminer la consultation (étape 4)
   */
  validerConsultation(idConsultation: number, request: ValiderConsultationRequest): Observable<{ message: string; idConsultation: number; imprimer: boolean }> {
    return this.http.post<{ message: string; idConsultation: number; imprimer: boolean }>(
      `${this.apiUrl}/${idConsultation}/valider`, request
    );
  }

  /**
   * Récupérer le récapitulatif complet pour impression
   */
  getRecapitulatif(idConsultation: number): Observable<ConsultationRecapitulatifDto> {
    return this.http.get<ConsultationRecapitulatifDto>(`${this.apiUrl}/${idConsultation}/recapitulatif`);
  }

  /**
   * Récupérer les créneaux disponibles du médecin pour une date
   */
  getCreneauxDisponibles(date: string): Observable<CreneauxDisponiblesResponse> {
    return this.http.get<CreneauxDisponiblesResponse>(`${this.apiUrl}/creneaux-disponibles?date=${date}`);
  }

  /**
   * Créer un rendez-vous de suivi depuis la consultation
   */
  creerRdvSuivi(idConsultation: number, request: CreerRdvSuiviRequest): Observable<RdvSuiviResponse> {
    return this.http.post<RdvSuiviResponse>(`${this.apiUrl}/${idConsultation}/rdv-suivi`, request);
  }

  /**
   * Récupérer tous les créneaux avec leur statut (disponible/occupé/passé)
   */
  getCreneauxAvecStatut(date: string): Observable<CreneauxAvecStatutResponse> {
    return this.http.get<CreneauxAvecStatutResponse>(`${this.apiUrl}/creneaux-avec-statut?date=${date}`);
  }

  /**
   * Clôturer le dossier d'un patient
   */
  cloturerDossier(idConsultation: number, motif?: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/${idConsultation}/cloturer-dossier`, 
      { motif }
    );
  }

  /**
   * Récupérer la liste des laboratoires disponibles
   */
  getLaboratoires(): Observable<LaboratoireDto[]> {
    return this.http.get<LaboratoireDto[]>(`${this.apiUrl}/laboratoires`);
  }

  // ==================== ORIENTATION SPECIALISTE ====================

  /**
   * Récupérer la liste des spécialités disponibles
   */
  getSpecialites(): Observable<SpecialiteDto[]> {
    return this.http.get<SpecialiteDto[]>(`${this.apiUrl}/specialites`);
  }

  /**
   * Récupérer les médecins d'une spécialité
   */
  getMedecinsParSpecialite(idSpecialite: number): Observable<MedecinSpecialisteDto[]> {
    return this.http.get<MedecinSpecialisteDto[]>(`${this.apiUrl}/specialites/${idSpecialite}/medecins`);
  }

  // ==================== ORIENTATIONS PRE-CONSULTATION (UNIFIÉ) ====================

  /**
   * Créer une orientation (unifiée: médecin interne/externe, hôpital, etc.)
   */
  createOrientation(idConsultation: number, request: CreateOrientationRequest): Observable<OrientationPreConsultationDto> {
    return this.http.post<OrientationPreConsultationDto>(`${this.apiUrl}/${idConsultation}/orientations`, request);
  }

  /**
   * Récupérer les orientations d'une consultation
   */
  getOrientations(idConsultation: number): Observable<OrientationPreConsultationDto[]> {
    return this.http.get<OrientationPreConsultationDto[]>(`${this.apiUrl}/${idConsultation}/orientations`);
  }

  /**
   * Mettre à jour le statut d'une orientation
   */
  updateOrientationStatut(idOrientation: number, request: UpdateOrientationStatutRequest): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/orientations/${idOrientation}/statut`, request);
  }

  /**
   * Supprimer une orientation
   */
  deleteOrientation(idOrientation: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/orientations/${idOrientation}`);
  }

  /**
   * Récupérer les orientations d'un patient (pour le dossier médical)
   */
  getOrientationsPatient(idPatient: number): Observable<OrientationPreConsultationDto[]> {
    return this.http.get<OrientationPreConsultationDto[]>(`${this.apiUrl}/patients/${idPatient}/orientations`);
  }

  /**
   * Récupérer les créneaux disponibles d'un médecin pour une date
   */
  getCreneauxMedecinDisponibles(idMedecin: number, date: string): Observable<CreneauxMedecinResponse> {
    return this.http.get<CreneauxMedecinResponse>(`${this.apiUrl}/medecins/${idMedecin}/creneaux-disponibles?date=${date}`);
  }

  /**
   * Créer un RDV lié à une orientation
   */
  creerRdvOrientation(idOrientation: number, request: CreerRdvOrientationRequest): Observable<CreerRdvOrientationResponse> {
    return this.http.post<CreerRdvOrientationResponse>(`${this.apiUrl}/orientations/${idOrientation}/creer-rdv`, request);
  }

  // ==================== GESTION DU STATUT ====================

  /**
   * Annuler une consultation
   */
  annulerConsultation(idConsultation: number, motif: string): Observable<{ message: string; idConsultation: number; motif: string }> {
    return this.http.post<{ message: string; idConsultation: number; motif: string }>(
      `${this.apiUrl}/${idConsultation}/annuler`,
      { motif }
    );
  }

  /**
   * Mettre une consultation en pause avec sauvegarde de l'étape actuelle
   */
  pauseConsultation(idConsultation: number, etapeActuelle?: string): Observable<{ message: string; idConsultation: number }> {
    return this.http.post<{ message: string; idConsultation: number }>(
      `${this.apiUrl}/${idConsultation}/pause`,
      { etapeActuelle }
    );
  }

  /**
   * Reprendre une consultation en pause
   */
  reprendreConsultation(idConsultation: number): Observable<{ message: string; idConsultation: number }> {
    return this.http.post<{ message: string; idConsultation: number }>(
      `${this.apiUrl}/${idConsultation}/reprendre`,
      {}
    );
  }
}
