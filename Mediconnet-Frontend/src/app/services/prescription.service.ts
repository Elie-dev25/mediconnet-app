import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ==================== Interfaces ====================

export interface MedicamentPrescriptionRequest {
  idMedicament?: number;
  nomMedicament: string;
  dosage?: string;
  quantite: number;
  posologie?: string;
  frequence?: string;
  dureeTraitement?: string;
  voieAdministration?: string;
  formePharmaceutique?: string;
  instructions?: string;
}

export interface CreateOrdonnanceRequest {
  idPatient: number;
  idConsultation?: number;
  idHospitalisation?: number;
  notes?: string;
  medicaments: MedicamentPrescriptionRequest[];
  dureeValiditeJours?: number;
  renouvelable?: boolean;
  nombreRenouvellements?: number | null;
}

export interface CreateOrdonnanceConsultationRequest {
  notes?: string;
  medicaments: MedicamentPrescriptionRequest[];
}

export interface CreateOrdonnanceHospitalisationRequest {
  notes?: string;
  medicaments: MedicamentPrescriptionRequest[];
}

export interface CreateOrdonnanceDirecteRequest {
  idPatient: number;
  notes?: string;
  medicaments: MedicamentPrescriptionRequest[];
}

export interface UpdateOrdonnanceRequest {
  notes?: string;
  medicaments: MedicamentPrescriptionRequest[];
}

export interface AnnulerOrdonnanceRequest {
  motif: string;
}

export interface ValiderPrescriptionRequest {
  idPatient: number;
  medicaments: MedicamentPrescriptionRequest[];
}

export interface AlertePrescription {
  type: string; // stock_faible, rupture, interaction, allergie, perime, peremption_proche
  severite: string; // info, warning, error
  message: string;
  idMedicament?: number;
  nomMedicament?: string;
}

export interface LignePrescriptionDto {
  idPrescriptionMed: number;
  /** ID du médicament dans le catalogue (null si hors catalogue) */
  idMedicament?: number | null;
  nomMedicament: string;
  dosage?: string;
  /** Indique si le médicament est hors catalogue (saisie libre) */
  estHorsCatalogue: boolean;
  quantite: number;
  posologie?: string;
  frequence?: string;
  dureeTraitement?: string;
  voieAdministration?: string;
  formePharmaceutique?: string;
  instructions?: string;
  quantiteDispensee: number;
  estDispense: boolean;
}

export interface OrdonnanceDto {
  idOrdonnance: number;
  date: Date;
  idPatient: number;
  nomPatient: string;
  idMedecin: number;
  nomMedecin: string;
  idConsultation?: number;
  idHospitalisation?: number;
  typeContexte: string; // consultation, hospitalisation, directe
  statut: string; // active, dispensee, partielle, annulee, expiree
  notes?: string;
  createdAt: Date;
  lignes: LignePrescriptionDto[];
}

export interface OrdonnanceResult {
  success: boolean;
  message: string;
  idOrdonnance?: number;
  ordonnance?: OrdonnanceDto;
  erreurs: string[];
  alertes: AlertePrescription[];
}

export interface ValidationPrescriptionResult {
  estValide: boolean;
  erreurs: string[];
  alertes: AlertePrescription[];
}

export interface FiltreOrdonnanceRequest {
  idPatient?: number;
  idMedecin?: number;
  idConsultation?: number;
  idHospitalisation?: number;
  statut?: string;
  typeContexte?: string;
  dateDebut?: Date;
  dateFin?: Date;
  page?: number;
  pageSize?: number;
}

export interface PagedOrdonnances {
  items: OrdonnanceDto[];
  totalItems: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ==================== Constantes ====================

export const TYPE_CONTEXTE = {
  CONSULTATION: 'consultation',
  HOSPITALISATION: 'hospitalisation',
  DIRECTE: 'directe'
} as const;

export const STATUT_ORDONNANCE = {
  ACTIVE: 'active',
  DISPENSEE: 'dispensee',
  PARTIELLE: 'partielle',
  ANNULEE: 'annulee',
  EXPIREE: 'expiree'
} as const;

export const SEVERITE_ALERTE = {
  INFO: 'info',
  WARNING: 'warning',
  ERROR: 'error'
} as const;

/**
 * Service centralisé pour la gestion des prescriptions médicamenteuses
 * Unifie tous les points d'entrée de prescription :
 * - Consultation classique
 * - Hospitalisation
 * - Prescription directe (fiche patient)
 */
@Injectable({
  providedIn: 'root'
})
export class PrescriptionService {
  private readonly apiUrl = `${environment.apiUrl}/prescription`;

  constructor(private http: HttpClient) {}

  // ==================== Création ====================

  /**
   * Crée une ordonnance générique
   */
  creerOrdonnance(request: CreateOrdonnanceRequest): Observable<OrdonnanceResult> {
    return this.http.post<OrdonnanceResult>(this.apiUrl, request);
  }

  /**
   * Crée une ordonnance dans le contexte d'une consultation
   */
  creerOrdonnanceConsultation(
    idConsultation: number, 
    request: CreateOrdonnanceConsultationRequest
  ): Observable<OrdonnanceResult> {
    return this.http.post<OrdonnanceResult>(
      `${this.apiUrl}/consultation/${idConsultation}`, 
      request
    );
  }

  /**
   * Crée une ordonnance dans le contexte d'une hospitalisation
   */
  creerOrdonnanceHospitalisation(
    idHospitalisation: number, 
    request: CreateOrdonnanceHospitalisationRequest
  ): Observable<OrdonnanceResult> {
    return this.http.post<OrdonnanceResult>(
      `${this.apiUrl}/hospitalisation/${idHospitalisation}`, 
      request
    );
  }

  /**
   * Crée une ordonnance directe (hors consultation/hospitalisation)
   * Utile pour les renouvellements ou prescriptions depuis la fiche patient
   */
  creerOrdonnanceDirecte(request: CreateOrdonnanceDirecteRequest): Observable<OrdonnanceResult> {
    return this.http.post<OrdonnanceResult>(`${this.apiUrl}/directe`, request);
  }

  // ==================== Lecture ====================

  /**
   * Récupère une ordonnance par son ID
   */
  getOrdonnance(idOrdonnance: number): Observable<OrdonnanceDto> {
    return this.http.get<OrdonnanceDto>(`${this.apiUrl}/${idOrdonnance}`);
  }

  /**
   * Récupère l'ordonnance d'une consultation
   */
  getOrdonnanceByConsultation(idConsultation: number): Observable<OrdonnanceDto> {
    return this.http.get<OrdonnanceDto>(`${this.apiUrl}/consultation/${idConsultation}`);
  }

  /**
   * Récupère les ordonnances d'un patient
   */
  getOrdonnancesPatient(idPatient: number): Observable<OrdonnanceDto[]> {
    return this.http.get<OrdonnanceDto[]>(`${this.apiUrl}/patient/${idPatient}`);
  }

  /**
   * Récupère les ordonnances d'une hospitalisation
   */
  getOrdonnancesHospitalisation(idHospitalisation: number): Observable<OrdonnanceDto[]> {
    return this.http.get<OrdonnanceDto[]>(`${this.apiUrl}/hospitalisation/${idHospitalisation}`);
  }

  /**
   * Recherche des ordonnances avec filtres
   */
  rechercherOrdonnances(filtre: FiltreOrdonnanceRequest): Observable<PagedOrdonnances> {
    let params = new HttpParams();
    
    if (filtre.idPatient) params = params.set('idPatient', filtre.idPatient.toString());
    if (filtre.idMedecin) params = params.set('idMedecin', filtre.idMedecin.toString());
    if (filtre.idConsultation) params = params.set('idConsultation', filtre.idConsultation.toString());
    if (filtre.idHospitalisation) params = params.set('idHospitalisation', filtre.idHospitalisation.toString());
    if (filtre.statut) params = params.set('statut', filtre.statut);
    if (filtre.typeContexte) params = params.set('typeContexte', filtre.typeContexte);
    if (filtre.dateDebut) params = params.set('dateDebut', filtre.dateDebut.toISOString());
    if (filtre.dateFin) params = params.set('dateFin', filtre.dateFin.toISOString());
    if (filtre.page) params = params.set('page', filtre.page.toString());
    if (filtre.pageSize) params = params.set('pageSize', filtre.pageSize.toString());

    return this.http.get<PagedOrdonnances>(`${this.apiUrl}/recherche`, { params });
  }

  // ==================== Modification ====================

  /**
   * Met à jour une ordonnance existante
   */
  mettreAJourOrdonnance(
    idOrdonnance: number, 
    request: UpdateOrdonnanceRequest
  ): Observable<OrdonnanceResult> {
    return this.http.put<OrdonnanceResult>(`${this.apiUrl}/${idOrdonnance}`, request);
  }

  /**
   * Annule une ordonnance
   */
  annulerOrdonnance(idOrdonnance: number, motif: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/${idOrdonnance}/annuler`, 
      { motif }
    );
  }

  // ==================== Validation ====================

  /**
   * Valide une prescription avant création (vérifie stock, interactions, etc.)
   */
  validerPrescription(request: ValiderPrescriptionRequest): Observable<ValidationPrescriptionResult> {
    return this.http.post<ValidationPrescriptionResult>(`${this.apiUrl}/valider`, request);
  }

  // ==================== Utilitaires ====================

  /**
   * Retourne le libellé du type de contexte
   */
  getTypeContexteLabel(typeContexte: string): string {
    switch (typeContexte) {
      case TYPE_CONTEXTE.CONSULTATION: return 'Consultation';
      case TYPE_CONTEXTE.HOSPITALISATION: return 'Hospitalisation';
      case TYPE_CONTEXTE.DIRECTE: return 'Prescription directe';
      default: return typeContexte;
    }
  }

  /**
   * Retourne le libellé du statut
   */
  getStatutLabel(statut: string): string {
    switch (statut) {
      case STATUT_ORDONNANCE.ACTIVE: return 'Active';
      case STATUT_ORDONNANCE.DISPENSEE: return 'Dispensée';
      case STATUT_ORDONNANCE.PARTIELLE: return 'Partiellement dispensée';
      case STATUT_ORDONNANCE.ANNULEE: return 'Annulée';
      case STATUT_ORDONNANCE.EXPIREE: return 'Expirée';
      default: return statut;
    }
  }

  /**
   * Retourne la classe CSS du statut
   */
  getStatutClass(statut: string): string {
    switch (statut) {
      case STATUT_ORDONNANCE.ACTIVE: return 'status-active';
      case STATUT_ORDONNANCE.DISPENSEE: return 'status-success';
      case STATUT_ORDONNANCE.PARTIELLE: return 'status-warning';
      case STATUT_ORDONNANCE.ANNULEE: return 'status-danger';
      case STATUT_ORDONNANCE.EXPIREE: return 'status-muted';
      default: return '';
    }
  }

  /**
   * Retourne la classe CSS de la sévérité d'alerte
   */
  getAlerteSeveriteClass(severite: string): string {
    switch (severite) {
      case SEVERITE_ALERTE.INFO: return 'alert-info';
      case SEVERITE_ALERTE.WARNING: return 'alert-warning';
      case SEVERITE_ALERTE.ERROR: return 'alert-danger';
      default: return '';
    }
  }

  /**
   * Retourne l'icône de la sévérité d'alerte
   */
  getAlerteSeveriteIcon(severite: string): string {
    switch (severite) {
      case SEVERITE_ALERTE.INFO: return 'info';
      case SEVERITE_ALERTE.WARNING: return 'alert-triangle';
      case SEVERITE_ALERTE.ERROR: return 'alert-circle';
      default: return 'info';
    }
  }
}
