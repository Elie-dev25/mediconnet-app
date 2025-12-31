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
}

export interface CreateServiceRequest {
  nomService: string;
  description?: string;
  responsableId?: number;
}

export interface UpdateServiceRequest {
  nomService: string;
  description?: string;
  responsableId?: number;
}

export interface Responsable {
  id: number;
  nom: string;
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

  // Spécialités
  getSpecialites(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/specialites`);
  }
}
