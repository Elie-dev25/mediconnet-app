import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

// DTOs pour le dossier pharmaceutique patient
export interface DossierPharmaceutiqueDto {
  idPatient: number;
  nomPatient: string;
  totalOrdonnances: number;
  ordonnancesActives: number;
  ordonnancesDelivrees: number;
  ordonnancesPartielles: number;
  ordonnances: OrdonnancePatientDto[];
}

export interface OrdonnancePatientDto {
  idOrdonnance: number;
  datePrescription: string;
  idMedecin: number;
  nomMedecin: string;
  specialiteMedecin?: string;
  typeContexte: string; // consultation, hospitalisation, directe
  service?: string;
  idConsultation?: number;
  idHospitalisation?: number;
  diagnostic?: string;
  notes?: string;
  statut: string; // active, dispensee, partielle, annulee, expiree
  statutDelivrance: string; // non_delivre, en_attente, partiel, delivre
  dateExpiration?: string;
  estExpire: boolean;
  renouvelable: boolean;
  nombreRenouvellements?: number;
  renouvellementRestants?: number;
  dateDelivrance?: string;
  nomPharmacien?: string;
  medicaments: MedicamentPrescritDto[];
}

export interface MedicamentPrescritDto {
  idPrescriptionMed: number;
  idMedicament?: number;
  nomMedicament: string;
  dosage?: string;
  formePharmaceutique?: string;
  voieAdministration?: string;
  estHorsCatalogue: boolean;
  quantitePrescrite: number;
  posologie?: string;
  frequence?: string;
  dureeTraitement?: string;
  instructions?: string;
  quantiteDelivree: number;
  statutDelivrance: string; // non_delivre, partiel, delivre
  dateDelivrance?: string;
}

export interface FiltreOrdonnancesPatientRequest {
  statut?: string; // active, dispensee, partielle, annulee, expiree
  typeContexte?: string; // consultation, hospitalisation, directe
  dateDebut?: string;
  dateFin?: string;
  idMedecin?: number;
  tri?: string; // date_desc, date_asc, medecin
  page?: number;
  pageSize?: number;
}

@Injectable({
  providedIn: 'root'
})
export class OrdonnancesPatientService {
  private readonly baseUrl = '/api/patient';

  constructor(private http: HttpClient) {}

  /**
   * Récupère le dossier pharmaceutique complet du patient
   */
  getDossierPharmaceutique(filtre?: FiltreOrdonnancesPatientRequest): Observable<DossierPharmaceutiqueDto> {
    const params = this.buildParams(filtre);
    return this.http.get<DossierPharmaceutiqueDto>(`${this.baseUrl}/ordonnances`, { params });
  }

  /**
   * Récupère une ordonnance spécifique du patient
   */
  getOrdonnancePatient(ordonnanceId: number): Observable<OrdonnancePatientDto> {
    return this.http.get<OrdonnancePatientDto>(`${this.baseUrl}/ordonnances/${ordonnanceId}`);
  }

  /**
   * Construit les paramètres HTTP à partir des filtres
   */
  private buildParams(filtre?: FiltreOrdonnancesPatientRequest): HttpParams {
    if (!filtre) return new HttpParams();

    const paramMap: Record<string, string | number | undefined> = {
      statut: filtre.statut,
      typeContexte: filtre.typeContexte,
      dateDebut: filtre.dateDebut,
      dateFin: filtre.dateFin,
      idMedecin: filtre.idMedecin,
      tri: filtre.tri,
      page: filtre.page,
      pageSize: filtre.pageSize
    };

    return Object.entries(paramMap)
      .filter(([, value]) => value !== undefined && value !== null)
      .reduce((params, [key, value]) => params.set(key, String(value)), new HttpParams());
  }

  /**
   * Retourne le libellé du statut de délivrance
   */
  getStatutDelivranceLabel(statut: string): string {
    switch (statut) {
      case 'non_delivre': return 'Non délivré';
      case 'en_attente': return 'En attente';
      case 'partiel': return 'Délivré partiellement';
      case 'delivre': return 'Délivré';
      default: return statut;
    }
  }

  /**
   * Retourne la classe CSS pour le statut de délivrance
   */
  getStatutDelivranceClass(statut: string): string {
    switch (statut) {
      case 'non_delivre': return 'badge-secondary';
      case 'en_attente': return 'badge-warning';
      case 'partiel': return 'badge-info';
      case 'delivre': return 'badge-success';
      default: return 'badge-secondary';
    }
  }

  /**
   * Retourne le libellé du type de contexte
   */
  getTypeContexteLabel(type: string): string {
    switch (type) {
      case 'consultation': return 'Consultation';
      case 'hospitalisation': return 'Hospitalisation';
      case 'directe': return 'Prescription directe';
      default: return type;
    }
  }

  /**
   * Retourne la classe CSS pour le type de contexte
   */
  getTypeContexteClass(type: string): string {
    switch (type) {
      case 'consultation': return 'badge-primary';
      case 'hospitalisation': return 'badge-danger';
      case 'directe': return 'badge-info';
      default: return 'badge-secondary';
    }
  }

  /**
   * Retourne le libellé du statut de l'ordonnance
   */
  getStatutLabel(statut: string): string {
    switch (statut) {
      case 'active': return 'Active';
      case 'dispensee': return 'Délivrée';
      case 'partielle': return 'Partielle';
      case 'annulee': return 'Annulée';
      case 'expiree': return 'Expirée';
      default: return statut;
    }
  }

  /**
   * Retourne la classe CSS pour le statut de l'ordonnance
   */
  getStatutClass(statut: string): string {
    switch (statut) {
      case 'active': return 'badge-success';
      case 'dispensee': return 'badge-primary';
      case 'partielle': return 'badge-warning';
      case 'annulee': return 'badge-danger';
      case 'expiree': return 'badge-secondary';
      default: return 'badge-secondary';
    }
  }

  /**
   * Formate une date pour l'affichage
   */
  formatDate(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  /**
   * Formate une date et heure pour l'affichage
   */
  formatDateTime(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
