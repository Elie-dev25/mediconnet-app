import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ==================== INTERFACES ====================

export interface AssuranceListItem {
  idAssurance: number;
  nom: string;
  typeAssurance: string;
  groupe?: string;
  zoneCouverture?: string;
  isActive: boolean;
  nombrePatientsAssures: number;
}

export interface AssuranceDetail {
  idAssurance: number;
  nom: string;
  typeAssurance: string;
  siteWeb?: string;
  telephoneServiceClient?: string;
  groupe?: string;
  paysOrigine?: string;
  statutJuridique?: string;
  description?: string;
  typeCouverture?: string;
  isComplementaire: boolean;
  categorieBeneficiaires?: string;
  conditionsAdhesion?: string;
  zoneCouverture?: string;
  modePaiement?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  nombrePatientsAssures: number;
}

export interface CreateAssuranceDto {
  nom: string;
  typeAssurance: string;
  siteWeb?: string;
  telephoneServiceClient?: string;
  groupe?: string;
  paysOrigine?: string;
  statutJuridique?: string;
  description?: string;
  typeCouverture?: string;
  isComplementaire?: boolean;
  categorieBeneficiaires?: string;
  conditionsAdhesion?: string;
  zoneCouverture?: string;
  modePaiement?: string;
  isActive?: boolean;
}

export interface PatientAssuranceInfo {
  estAssure: boolean;
  assuranceId?: number;
  nomAssurance?: string;
  typeAssurance?: string;
  couvertureAssurance?: number; // Taux de couverture du patient
  numeroCarteAssurance?: string;
  dateDebutValidite?: string;
  dateFinValidite?: string;
  estValide: boolean;
  tauxEffectif?: number; // Taux effectif calculé
}

export interface UpdatePatientAssuranceDto {
  assuranceId?: number;
  numeroCarteAssurance?: string;
  dateDebutValidite?: string;
  dateFinValidite?: string;
  couvertureAssurance?: number;
}

export interface AssuranceListResponse {
  success: boolean;
  data: AssuranceListItem[];
  total: number;
  totalActives: number;
}

export interface AssuranceResponse {
  success: boolean;
  message: string;
  data?: AssuranceDetail;
}

export interface PatientAssuranceResponse {
  success: boolean;
  message: string;
  data?: PatientAssuranceInfo;
}

export interface AssuranceFilter {
  typeAssurance?: string;
  zoneCouverture?: string;
  isActive?: boolean;
  recherche?: string;
  page?: number;
  pageSize?: number;
}

// ==================== CONSTANTES ====================

export const TYPES_ASSURANCE = [
  { value: 'privee', label: 'Privée' },
  { value: 'publique', label: 'Publique' },
  { value: 'mutuelle', label: 'Mutuelle' },
  { value: 'micro_assurance', label: 'Micro-assurance' },
  { value: 'programme_public', label: 'Programme public' }
];

export const STATUTS_JURIDIQUES = [
  { value: 'compagnie', label: 'Compagnie' },
  { value: 'mutuelle', label: 'Mutuelle' },
  { value: 'organisme_public', label: 'Organisme public' },
  { value: 'cooperative', label: 'Coopérative' }
];

export const ZONES_COUVERTURE = [
  { value: 'national', label: 'National' },
  { value: 'regional', label: 'Régional' },
  { value: 'rural', label: 'Rural' },
  { value: 'diaspora', label: 'Diaspora' },
  { value: 'international', label: 'International' }
];

export const MODES_PAIEMENT = [
  { value: 'mobile_money', label: 'Mobile Money' },
  { value: 'entreprise', label: 'Entreprise' },
  { value: 'cotisations', label: 'Cotisations' },
  { value: 'prelevement', label: 'Prélèvement bancaire' },
  { value: 'carte', label: 'Carte bancaire' }
];

// ==================== SERVICE ====================

@Injectable({
  providedIn: 'root'
})
export class AssuranceService {
  private apiUrl = `${environment.apiUrl}/assurance`;

  constructor(private http: HttpClient) {}

  // ==================== ASSURANCES ====================

  /**
   * Récupérer la liste des assurances avec filtres
   */
  getAssurances(filter?: AssuranceFilter): Observable<AssuranceListResponse> {
    let params = new HttpParams();
    if (filter) {
      if (filter.typeAssurance) params = params.set('typeAssurance', filter.typeAssurance);
      if (filter.zoneCouverture) params = params.set('zoneCouverture', filter.zoneCouverture);
      if (filter.isActive !== undefined) params = params.set('isActive', filter.isActive.toString());
      if (filter.recherche) params = params.set('recherche', filter.recherche);
      if (filter.page) params = params.set('page', filter.page.toString());
      if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    }
    return this.http.get<AssuranceListResponse>(this.apiUrl, { params });
  }

  /**
   * Récupérer les assurances actives (pour listes déroulantes)
   */
  getAssurancesActives(): Observable<AssuranceListItem[]> {
    return this.http.get<AssuranceListItem[]>(`${this.apiUrl}/actives`);
  }

  /**
   * Récupérer une assurance par son ID
   */
  getAssuranceById(id: number): Observable<AssuranceDetail> {
    return this.http.get<AssuranceDetail>(`${this.apiUrl}/${id}`);
  }

  /**
   * Créer une nouvelle assurance
   */
  createAssurance(data: CreateAssuranceDto): Observable<AssuranceResponse> {
    return this.http.post<AssuranceResponse>(this.apiUrl, data);
  }

  /**
   * Mettre à jour une assurance
   */
  updateAssurance(id: number, data: CreateAssuranceDto): Observable<AssuranceResponse> {
    return this.http.put<AssuranceResponse>(`${this.apiUrl}/${id}`, data);
  }

  /**
   * Activer/Désactiver une assurance
   */
  toggleAssuranceStatus(id: number): Observable<AssuranceResponse> {
    return this.http.patch<AssuranceResponse>(`${this.apiUrl}/${id}/toggle-status`, {});
  }

  /**
   * Supprimer une assurance
   */
  deleteAssurance(id: number): Observable<AssuranceResponse> {
    return this.http.delete<AssuranceResponse>(`${this.apiUrl}/${id}`);
  }

  // ==================== PATIENT ASSURANCE ====================

  /**
   * Récupérer l'assurance d'un patient
   */
  getPatientAssurance(idPatient: number): Observable<PatientAssuranceInfo> {
    return this.http.get<PatientAssuranceInfo>(`${this.apiUrl}/patient/${idPatient}`);
  }

  /**
   * Mettre à jour l'assurance d'un patient
   */
  updatePatientAssurance(idPatient: number, data: UpdatePatientAssuranceDto): Observable<PatientAssuranceResponse> {
    return this.http.put<PatientAssuranceResponse>(`${this.apiUrl}/patient/${idPatient}`, data);
  }

  /**
   * Retirer l'assurance d'un patient
   */
  removePatientAssurance(idPatient: number): Observable<PatientAssuranceResponse> {
    return this.http.delete<PatientAssuranceResponse>(`${this.apiUrl}/patient/${idPatient}`);
  }
}
