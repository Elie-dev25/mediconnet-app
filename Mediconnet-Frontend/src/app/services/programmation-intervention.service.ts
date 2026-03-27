import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ProgrammationInterventionDto {
  idProgrammation: number;
  idConsultation: number;
  idPatient: number;
  idChirurgien: number;
  patientNom?: string;
  patientPrenom?: string;
  chirurgienNom?: string;
  chirurgienPrenom?: string;
  specialite?: string;
  typeIntervention: string;
  classificationAsa?: string;
  risqueOperatoire?: string;
  consentementEclaire: boolean;
  dateConsentement?: string;
  indicationOperatoire?: string;
  techniquePrevue?: string;
  datePrevue?: string;
  heureDebut?: string;
  dureeEstimee?: number;
  notesAnesthesie?: string;
  bilanPreoperatoire?: string;
  instructionsPatient?: string;
  statut: string;
  motifAnnulation?: string;
  notes?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface ProgrammationInterventionListDto {
  idProgrammation: number;
  patientNom?: string;
  patientPrenom?: string;
  indicationOperatoire?: string;
  techniquePrevue?: string;
  datePrevue?: string;
  heureDebut?: string;
  dureeEstimee?: number;
  statut: string;
  typeIntervention: string;
  classificationAsa?: string;
  consentementEclaire: boolean;
}

export interface CreateProgrammationRequest {
  idConsultation: number;
  typeIntervention: string;
  classificationAsa?: string;
  risqueOperatoire?: string;
  consentementEclaire: boolean;
  dateConsentement?: string;
  indicationOperatoire?: string;
  techniquePrevue?: string;
  datePrevue?: string;
  heureDebut?: string;
  dureeEstimee?: number;
  notesAnesthesie?: string;
  bilanPreoperatoire?: string;
  instructionsPatient?: string;
  notes?: string;
}

export interface UpdateProgrammationRequest {
  typeIntervention?: string;
  classificationAsa?: string;
  risqueOperatoire?: string;
  consentementEclaire?: boolean;
  dateConsentement?: string;
  indicationOperatoire?: string;
  techniquePrevue?: string;
  datePrevue?: string;
  heureDebut?: string;
  dureeEstimee?: number;
  notesAnesthesie?: string;
  bilanPreoperatoire?: string;
  instructionsPatient?: string;
  statut?: string;
  motifAnnulation?: string;
  notes?: string;
}

export const TYPES_INTERVENTION = [
  { value: 'programmee', label: 'Programmée' },
  { value: 'urgence', label: 'Urgence' },
  { value: 'ambulatoire', label: 'Ambulatoire' }
];

export const CLASSIFICATIONS_ASA = [
  { value: 'ASA1', label: 'ASA 1 - Patient en bonne santé' },
  { value: 'ASA2', label: 'ASA 2 - Maladie systémique légère' },
  { value: 'ASA3', label: 'ASA 3 - Maladie systémique sévère' },
  { value: 'ASA4', label: 'ASA 4 - Maladie systémique menaçant le pronostic vital' },
  { value: 'ASA5', label: 'ASA 5 - Patient moribond' }
];

export const RISQUES_OPERATOIRES = [
  { value: 'faible', label: 'Faible' },
  { value: 'modere', label: 'Modéré' },
  { value: 'eleve', label: 'Élevé' }
];

export const STATUTS_PROGRAMMATION = [
  { value: 'en_attente', label: 'En attente', color: '#f59e0b' },
  { value: 'validee', label: 'Validée', color: '#3b82f6' },
  { value: 'planifiee', label: 'Planifiée', color: '#8b5cf6' },
  { value: 'realisee', label: 'Réalisée', color: '#10b981' },
  { value: 'annulee', label: 'Annulée', color: '#ef4444' }
];

@Injectable({
  providedIn: 'root'
})
export class ProgrammationInterventionService {
  private apiUrl = `${environment.apiUrl}/programmation-intervention`;

  constructor(private http: HttpClient) {}

  /**
   * Récupérer toutes les programmations du chirurgien
   */
  getMyProgrammations(statut?: string): Observable<ProgrammationInterventionListDto[]> {
    if (statut) {
      return this.http.get<ProgrammationInterventionListDto[]>(this.apiUrl, { params: { statut } });
    }
    return this.http.get<ProgrammationInterventionListDto[]>(this.apiUrl);
  }

  /**
   * Récupérer une programmation par ID
   */
  getProgrammation(id: number): Observable<ProgrammationInterventionDto> {
    return this.http.get<ProgrammationInterventionDto>(`${this.apiUrl}/${id}`);
  }

  /**
   * Récupérer la programmation liée à une consultation
   */
  getByConsultation(idConsultation: number): Observable<{ exists: boolean; programmation?: ProgrammationInterventionDto }> {
    return this.http.get<{ exists: boolean; programmation?: ProgrammationInterventionDto }>(
      `${this.apiUrl}/consultation/${idConsultation}`
    );
  }

  /**
   * Créer une nouvelle programmation
   */
  createProgrammation(request: CreateProgrammationRequest): Observable<{ message: string; idProgrammation: number }> {
    return this.http.post<{ message: string; idProgrammation: number }>(this.apiUrl, request);
  }

  /**
   * Mettre à jour une programmation
   */
  updateProgrammation(id: number, request: UpdateProgrammationRequest): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Annuler une programmation
   */
  annulerProgrammation(id: number, motif?: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${id}/annuler`, { motif });
  }

  /**
   * Valider le consentement éclairé
   */
  validerConsentement(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${id}/consentement`, {});
  }

  /**
   * Obtenir le label d'un statut
   */
  getStatutLabel(statut: string): string {
    return STATUTS_PROGRAMMATION.find(s => s.value === statut)?.label || statut;
  }

  /**
   * Obtenir la couleur d'un statut
   */
  getStatutColor(statut: string): string {
    return STATUTS_PROGRAMMATION.find(s => s.value === statut)?.color || '#6b7280';
  }
}
