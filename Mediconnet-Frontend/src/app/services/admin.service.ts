import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ServiceDto {
  idService: number;
  nomService: string;
  description?: string;
  responsableId?: number;
  responsableNom?: string;
  nombreMedecins: number;
  /** Coût de la consultation pour ce service (en FCFA) */
  coutConsultation: number;
}

export interface CreateServiceRequest {
  nomService: string;
  description?: string;
  responsableId?: number;
  /** Coût de la consultation pour ce service (en FCFA). Défaut: 5000 */
  coutConsultation?: number;
}

export interface UpdateServiceRequest {
  nomService: string;
  description?: string;
  responsableId?: number;
  /** Coût de la consultation pour ce service (en FCFA) */
  coutConsultation: number;
}

export interface Responsable {
  id: number;
  nom: string;
}

export interface SpecialiteInfirmierDto {
  idSpecialite: number;
  code?: string;
  nom: string;
  description?: string;
  actif: boolean;
  createdAt: string;
  nombreInfirmiers: number;
}

export interface CreateSpecialiteInfirmierRequest {
  code?: string;
  nom: string;
  description?: string;
}

export interface UpdateSpecialiteInfirmierRequest {
  code?: string;
  nom: string;
  description?: string;
  actif: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private apiUrl = `${environment.apiUrl}/admin`;

  constructor(private http: HttpClient) {}

  // Services CRUD
  getServices(): Observable<ServiceDto[]> {
    return this.http.get<ServiceDto[]>(`${this.apiUrl}/services`);
  }

  getService(id: number): Observable<ServiceDto> {
    return this.http.get<ServiceDto>(`${this.apiUrl}/services/${id}`);
  }

  createService(request: CreateServiceRequest): Observable<{ message: string; serviceId: number }> {
    return this.http.post<{ message: string; serviceId: number }>(`${this.apiUrl}/services`, request);
  }

  updateService(id: number, request: UpdateServiceRequest): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/services/${id}`, request);
  }

  deleteService(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/services/${id}`);
  }

  // Responsables (médecins)
  getResponsables(): Observable<Responsable[]> {
    return this.http.get<Responsable[]>(`${this.apiUrl}/responsables`);
  }

  // Spécialités (médecins)
  getSpecialites(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/specialites`);
  }

  // Spécialités Infirmiers
  getSpecialitesInfirmiers(): Observable<SpecialiteInfirmierDto[]> {
    return this.http.get<SpecialiteInfirmierDto[]>(`${environment.apiUrl}/specialites-infirmiers`);
  }

  getSpecialitesInfirmiersActives(): Observable<SpecialiteInfirmierDto[]> {
    return this.http.get<SpecialiteInfirmierDto[]>(`${environment.apiUrl}/specialites-infirmiers/actives`);
  }

  getSpecialiteInfirmier(id: number): Observable<SpecialiteInfirmierDto> {
    return this.http.get<SpecialiteInfirmierDto>(`${environment.apiUrl}/specialites-infirmiers/${id}`);
  }

  createSpecialiteInfirmier(request: CreateSpecialiteInfirmierRequest): Observable<SpecialiteInfirmierDto> {
    return this.http.post<SpecialiteInfirmierDto>(`${environment.apiUrl}/specialites-infirmiers`, request);
  }

  updateSpecialiteInfirmier(id: number, request: UpdateSpecialiteInfirmierRequest): Observable<SpecialiteInfirmierDto> {
    return this.http.put<SpecialiteInfirmierDto>(`${environment.apiUrl}/specialites-infirmiers/${id}`, request);
  }

  deleteSpecialiteInfirmier(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${environment.apiUrl}/specialites-infirmiers/${id}`);
  }
}
