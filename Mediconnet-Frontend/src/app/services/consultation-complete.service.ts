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
  groupeSanguin?: string;
  maladiesChroniques?: string;
  allergiesDetails?: string;
  antecedentsFamiliauxDetails?: string;
  operationsDetails?: string;
  consommationAlcool?: boolean;
  tabagisme?: boolean;
  activitePhysique?: boolean;
  nomAssurance?: string;
  numeroCarteAssurance?: string;
  couvertureAssurance?: number;
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
  isPremiereConsultation: boolean;
  specialiteId: number;
  anamnese?: AnamneseDto;
  diagnostic?: DiagnosticDto;
  prescriptions?: PrescriptionsDto;
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

export interface DiagnosticDto {
  examenClinique?: string;
  diagnosticPrincipal?: string;
  diagnosticsSecondaires?: string;
  notesCliniques?: string;
}

export interface PrescriptionsDto {
  ordonnance?: OrdonnanceDto;
  examens: ExamenPrescritDto[];
  recommandations: RecommandationDto[];
}

export interface OrdonnanceDto {
  idOrdonnance?: number;
  notes?: string;
  dureeTraitement?: string;
  medicaments: MedicamentDto[];
}

export interface MedicamentDto {
  idPrescription?: number;
  nomMedicament: string;
  dosage?: string;
  frequence?: string;
  duree?: string;
  instructions?: string;
  quantite?: number;
}

export interface ExamenPrescritDto {
  idExamen?: number;
  typeExamen: string;
  nomExamen: string;
  description?: string;
  urgence: boolean;
  notes?: string;
}

export interface RecommandationDto {
  idRecommandation?: number;
  type: string;
  specialiteOrientee?: string;
  idMedecinOriente?: number;
  motif?: string;
  description?: string;
  urgence: boolean;
}

export interface ValiderConsultationRequest {
  conclusion?: string;
  imprimer: boolean;
}

export interface ConsultationRecapitulatifDto {
  consultation: ConsultationEnCoursDto;
  patient: DossierPatientDto;
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
   * Sauvegarder l'anamnèse (étape 1)
   */
  saveAnamnese(idConsultation: number, anamnese: AnamneseDto): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/${idConsultation}/anamnese`, anamnese
    );
  }

  /**
   * Sauvegarder le diagnostic (étape 2)
   */
  saveDiagnostic(idConsultation: number, diagnostic: DiagnosticDto): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/${idConsultation}/diagnostic`, diagnostic
    );
  }

  /**
   * Sauvegarder les prescriptions (étape 3)
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
}
