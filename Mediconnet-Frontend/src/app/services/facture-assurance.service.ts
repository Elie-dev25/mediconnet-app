import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface FactureAssurance {
  idFacture: number;
  numeroFacture: string;
  dateFacture: string;
  typeFacture: string;
  statut: string;
  montantTotal: number;
  montantAssurance: number;
  montantPatient: number;
  tauxCouverture: number;
  dateEnvoiAssurance?: string;
  datePaiement?: string;
  notes?: string;
  patientNom?: string;
  idPatient: number;
  numeroCarteAssurance?: string;
  assuranceNom?: string;
  assuranceId?: number;
  assuranceEmail?: string;
}

export interface FactureAssuranceFilter {
  idAssurance?: number;
  statut?: string;
  typeFacture?: string;
  dateDebut?: string;
  dateFin?: string;
  recherche?: string;
  limit?: number;
}

export interface FactureAssuranceStats {
  totalFactures: number;
  facturesEnAttente: number;
  facturesEnvoyees: number;
  facturesPayees: number;
  facturesRejetees: number;
  montantTotalDu: number;
  montantTotalPaye: number;
  montantEnAttente: number;
}

export interface UpdateStatutRequest {
  statut: string;
  notes?: string;
}

export const STATUTS_FACTURE = [
  { value: 'en_attente', label: 'En attente', color: 'warning' },
  { value: 'envoyee_assurance', label: 'Envoyée', color: 'info' },
  { value: 'payee', label: 'Payée', color: 'success' },
  { value: 'partiellement_payee', label: 'Partiellement payée', color: 'warning' },
  { value: 'rejetee', label: 'Rejetée', color: 'danger' },
  { value: 'annulee', label: 'Annulée', color: 'secondary' }
];

export const TYPES_FACTURE = [
  { value: 'consultation', label: 'Consultation' },
  { value: 'hospitalisation', label: 'Hospitalisation' },
  { value: 'examen', label: 'Examens' },
  { value: 'pharmacie', label: 'Pharmacie' }
];

@Injectable({
  providedIn: 'root'
})
export class FactureAssuranceService {
  private apiUrl = `${environment.apiUrl}/factures-assurance`;

  constructor(private http: HttpClient) {}

  getFactures(filter?: FactureAssuranceFilter): Observable<{ success: boolean; data: FactureAssurance[]; total: number }> {
    let params = new HttpParams();
    
    if (filter) {
      if (filter.idAssurance) params = params.set('idAssurance', filter.idAssurance.toString());
      if (filter.statut) params = params.set('statut', filter.statut);
      if (filter.typeFacture) params = params.set('typeFacture', filter.typeFacture);
      if (filter.dateDebut) params = params.set('dateDebut', filter.dateDebut);
      if (filter.dateFin) params = params.set('dateFin', filter.dateFin);
      if (filter.recherche) params = params.set('recherche', filter.recherche);
      if (filter.limit) params = params.set('limit', filter.limit.toString());
    }

    return this.http.get<{ success: boolean; data: FactureAssurance[]; total: number }>(this.apiUrl, { params });
  }

  getFacture(id: number): Observable<{ success: boolean; data: FactureAssurance }> {
    return this.http.get<{ success: boolean; data: FactureAssurance }>(`${this.apiUrl}/${id}`);
  }

  getStatistiques(): Observable<{ success: boolean; data: FactureAssuranceStats }> {
    return this.http.get<{ success: boolean; data: FactureAssuranceStats }>(`${this.apiUrl}/stats`);
  }

  envoyerFacture(id: number): Observable<{ success: boolean; message: string; numeroFacture?: string }> {
    return this.http.post<{ success: boolean; message: string; numeroFacture?: string }>(`${this.apiUrl}/${id}/envoyer`, {});
  }

  envoyerLot(factureIds: number[]): Observable<{ success: boolean; message: string; details: any[] }> {
    return this.http.post<{ success: boolean; message: string; details: any[] }>(`${this.apiUrl}/envoyer-lot`, { factureIds });
  }

  updateStatut(id: number, request: UpdateStatutRequest): Observable<{ success: boolean; message: string }> {
    return this.http.put<{ success: boolean; message: string }>(`${this.apiUrl}/${id}/statut`, request);
  }

  telechargerPdf(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/pdf`, { responseType: 'blob' });
  }

  getStatutLabel(statut: string): string {
    const found = STATUTS_FACTURE.find(s => s.value === statut);
    return found ? found.label : statut;
  }

  getStatutColor(statut: string): string {
    const found = STATUTS_FACTURE.find(s => s.value === statut);
    return found ? found.color : 'secondary';
  }

  getTypeLabel(type: string): string {
    const found = TYPES_FACTURE.find(t => t.value === type);
    return found ? found.label : type;
  }
}
