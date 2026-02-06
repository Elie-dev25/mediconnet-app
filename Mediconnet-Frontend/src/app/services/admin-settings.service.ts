import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ==================== STANDARDS DE CHAMBRE ====================

export interface StandardChambreDto {
  idStandard: number;
  nom: string;
  description?: string;
  prixJournalier: number;
  privileges: string[];
  localisation?: string;
  actif: boolean;
  nombreChambres: number;
  chambresDisponibles: number;
}

export interface StandardChambreSelectDto {
  idStandard: number;
  nom: string;
  prixJournalier: number;
  privileges: string[];
  localisation?: string;
  displayText: string;
}

export interface CreateStandardChambreRequest {
  nom: string;
  description?: string;
  prixJournalier: number;
  privileges: string[];
  localisation?: string;
}

export interface UpdateStandardChambreRequest {
  nom?: string;
  description?: string;
  prixJournalier?: number;
  privileges?: string[];
  localisation?: string;
  actif?: boolean;
}

export interface StandardChambreResponse {
  success: boolean;
  message?: string;
  data?: StandardChambreDto;
}

export interface StandardsListResponse {
  success: boolean;
  data: StandardChambreDto[];
  count: number;
}

// ==================== CHAMBRES ====================

export interface ChambreAdminDto {
  idChambre: number;
  numero: string;
  capacite: number;
  etat: string;
  statut: string;
  idStandard?: number;
  standardNom?: string;
  standardPrix?: number;
  nombreLits: number;
  litsLibres: number;
  litsOccupes: number;
  litsHorsService: number;
  lits: LitAdminDto[];
}

export interface CreateChambreRequest {
  numero: string;
  capacite: number;
  etat?: string;
  statut?: string;
  idStandard?: number;
  lits?: CreateLitRequest[];
}

export interface UpdateChambreRequest {
  numero?: string;
  capacite?: number;
  etat?: string;
  statut?: string;
  idStandard?: number;
}

// ==================== LITS ====================

export interface LitAdminDto {
  idLit: number;
  numero: string;
  statut: string;
  idChambre: number;
  numeroChambre?: string;
  estOccupe: boolean;
  patientActuel?: string;
}

export interface CreateLitRequest {
  numero: string;
  statut?: string;
}

export interface UpdateLitRequest {
  numero?: string;
  statut?: string;
}

// ==================== RESPONSES ====================

export interface ChambreResponse {
  success: boolean;
  message: string;
  chambre?: ChambreAdminDto;
}

export interface ChambresListResponse {
  success: boolean;
  chambres: ChambreAdminDto[];
  total: number;
  stats: ChambresStats;
}

export interface ChambresStats {
  totalChambres: number;
  chambresActives: number;
  totalLits: number;
  litsLibres: number;
  litsOccupes: number;
  litsHorsService: number;
}

export interface LitResponse {
  success: boolean;
  message: string;
  lit?: LitAdminDto;
}

// ==================== LABORATOIRES ====================

export interface LaboratoireDto {
  idLabo: number;
  nom: string;
  adresse?: string;
  telephone?: string;
  email?: string;
  actif: boolean;
}

export interface LaboratoiresListResponse {
  success: boolean;
  laboratoires: LaboratoireDto[];
  total: number;
}

@Injectable({
  providedIn: 'root'
})
export class AdminSettingsService {
  private apiUrl = `${environment.apiUrl}/admin/settings`;

  constructor(private http: HttpClient) {}

  // ==================== CHAMBRES ====================

  getChambres(): Observable<ChambresListResponse> {
    return this.http.get<ChambresListResponse>(`${this.apiUrl}/chambres`);
  }

  getChambre(id: number): Observable<ChambreAdminDto> {
    return this.http.get<ChambreAdminDto>(`${this.apiUrl}/chambres/${id}`);
  }

  createChambre(request: CreateChambreRequest): Observable<ChambreResponse> {
    return this.http.post<ChambreResponse>(`${this.apiUrl}/chambres`, request);
  }

  updateChambre(id: number, request: UpdateChambreRequest): Observable<ChambreResponse> {
    return this.http.put<ChambreResponse>(`${this.apiUrl}/chambres/${id}`, request);
  }

  deleteChambre(id: number): Observable<ChambreResponse> {
    return this.http.delete<ChambreResponse>(`${this.apiUrl}/chambres/${id}`);
  }

  getChambresStats(): Observable<ChambresStats> {
    return this.http.get<ChambresStats>(`${this.apiUrl}/chambres/stats`);
  }

  // ==================== LITS ====================

  addLit(chambreId: number, request: CreateLitRequest): Observable<LitResponse> {
    return this.http.post<LitResponse>(`${this.apiUrl}/chambres/${chambreId}/lits`, request);
  }

  updateLit(litId: number, request: UpdateLitRequest): Observable<LitResponse> {
    return this.http.put<LitResponse>(`${this.apiUrl}/lits/${litId}`, request);
  }

  deleteLit(litId: number): Observable<LitResponse> {
    return this.http.delete<LitResponse>(`${this.apiUrl}/lits/${litId}`);
  }

  // ==================== LABORATOIRES ====================

  getLaboratoires(): Observable<LaboratoiresListResponse> {
    return this.http.get<LaboratoiresListResponse>(`${this.apiUrl}/laboratoires`);
  }

  // ==================== STANDARDS DE CHAMBRE ====================

  private standardsUrl = `${environment.apiUrl}/standard-chambre`;

  getStandards(): Observable<StandardsListResponse> {
    return this.http.get<StandardsListResponse>(this.standardsUrl);
  }

  getStandardsForSelect(): Observable<{ success: boolean; data: StandardChambreSelectDto[] }> {
    return this.http.get<{ success: boolean; data: StandardChambreSelectDto[] }>(`${this.standardsUrl}/select`);
  }

  getStandard(id: number): Observable<StandardChambreResponse> {
    return this.http.get<StandardChambreResponse>(`${this.standardsUrl}/${id}`);
  }

  createStandard(request: CreateStandardChambreRequest): Observable<StandardChambreResponse> {
    return this.http.post<StandardChambreResponse>(this.standardsUrl, request);
  }

  updateStandard(id: number, request: UpdateStandardChambreRequest): Observable<StandardChambreResponse> {
    return this.http.put<StandardChambreResponse>(`${this.standardsUrl}/${id}`, request);
  }

  deleteStandard(id: number): Observable<{ success: boolean; message: string }> {
    return this.http.delete<{ success: boolean; message: string }>(`${this.standardsUrl}/${id}`);
  }
}
