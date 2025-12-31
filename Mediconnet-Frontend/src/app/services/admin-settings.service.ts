import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ==================== CHAMBRES ====================

export interface ChambreAdminDto {
  idChambre: number;
  numero: string;
  capacite: number;
  etat: string;
  statut: string;
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
  lits?: CreateLitRequest[];
}

export interface UpdateChambreRequest {
  numero?: string;
  capacite?: number;
  etat?: string;
  statut?: string;
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
}
