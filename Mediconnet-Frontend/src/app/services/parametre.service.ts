import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * Interface pour les paramètres vitaux
 */
export interface ParametreDto {
  idParametre: number;
  idConsultation: number;
  poids: number | null;
  temperature: number | null;
  tensionSystolique: number | null;
  tensionDiastolique: number | null;
  taille: number | null;
  dateEnregistrement: string;
  enregistrePar: number | null;
  nomEnregistrant: string | null;
  tensionFormatee: string | null;
  imc: number | null;
}

/**
 * Interface pour créer/mettre à jour les paramètres
 */
export interface CreateParametreRequest {
  idConsultation: number;
  poids?: number | null;
  temperature?: number | null;
  tensionSystolique?: number | null;
  tensionDiastolique?: number | null;
  taille?: number | null;
}

/**
 * Interface pour créer des paramètres directement pour un patient
 */
export interface CreateParametreByPatientRequest {
  idPatient: number;
  poids?: number | null;
  temperature?: number | null;
  tensionSystolique?: number | null;
  tensionDiastolique?: number | null;
  taille?: number | null;
}

/**
 * Interface pour mettre à jour les paramètres
 */
export interface UpdateParametreRequest {
  poids?: number | null;
  temperature?: number | null;
  tensionSystolique?: number | null;
  tensionDiastolique?: number | null;
  taille?: number | null;
}

/**
 * Interface pour la réponse API
 */
export interface ParametreResponse {
  success: boolean;
  data?: ParametreDto;
  message?: string;
  errors?: string[];
}

export interface ParametreListResponse {
  success: boolean;
  data: ParametreDto[];
  count: number;
}

/**
 * Service pour la gestion des paramètres vitaux
 */
@Injectable({
  providedIn: 'root'
})
export class ParametreService {
  private readonly API_URL = `${environment.apiUrl}/parametre`;

  constructor(private http: HttpClient) {}

  /**
   * Récupère les paramètres d'une consultation
   */
  getByConsultation(consultationId: number): Observable<ParametreResponse> {
    return this.http.get<ParametreResponse>(`${this.API_URL}/consultation/${consultationId}`);
  }

  /**
   * Récupère l'historique des paramètres d'un patient
   */
  getHistoriquePatient(patientId: number): Observable<ParametreListResponse> {
    return this.http.get<ParametreListResponse>(`${this.API_URL}/patient/${patientId}/historique`);
  }

  /**
   * Crée ou met à jour les paramètres
   */
  createOrUpdate(request: CreateParametreRequest): Observable<ParametreResponse> {
    return this.http.post<ParametreResponse>(this.API_URL, request);
  }

  /**
   * Crée les paramètres directement pour un patient (sans consultation existante)
   */
  createByPatient(request: CreateParametreByPatientRequest): Observable<ParametreResponse> {
    return this.http.post<ParametreResponse>(`${this.API_URL}/patient`, request);
  }

  /**
   * Met à jour les paramètres existants
   */
  update(parametreId: number, request: UpdateParametreRequest): Observable<ParametreResponse> {
    return this.http.put<ParametreResponse>(`${this.API_URL}/${parametreId}`, request);
  }

  /**
   * Supprime les paramètres (admin uniquement)
   */
  delete(parametreId: number): Observable<{ success: boolean; message: string }> {
    return this.http.delete<{ success: boolean; message: string }>(`${this.API_URL}/${parametreId}`);
  }

  /**
   * Calcule l'IMC côté client
   */
  calculerIMC(poids: number | null, taille: number | null): number | null {
    if (!poids || !taille || taille <= 0) return null;
    const tailleM = taille / 100;
    return Math.round((poids / (tailleM * tailleM)) * 100) / 100;
  }

  /**
   * Interprète l'IMC
   */
  interpreterIMC(imc: number | null): { label: string; color: string } {
    if (!imc) return { label: '-', color: 'gray' };
    if (imc < 18.5) return { label: 'Insuffisance pondérale', color: 'warning' };
    if (imc < 25) return { label: 'Poids normal', color: 'success' };
    if (imc < 30) return { label: 'Surpoids', color: 'warning' };
    return { label: 'Obésité', color: 'danger' };
  }

  /**
   * Interprète la tension
   */
  interpreterTension(sys: number | null, dia: number | null): { label: string; color: string } {
    if (!sys || !dia) return { label: '-', color: 'gray' };
    if (sys < 90 || dia < 60) return { label: 'Hypotension', color: 'info' };
    if (sys <= 120 && dia <= 80) return { label: 'Normale', color: 'success' };
    if (sys <= 139 || dia <= 89) return { label: 'Pré-hypertension', color: 'warning' };
    return { label: 'Hypertension', color: 'danger' };
  }

  /**
   * Interprète la température
   */
  interpreterTemperature(temp: number | null): { label: string; color: string } {
    if (!temp) return { label: '-', color: 'gray' };
    if (temp < 36) return { label: 'Hypothermie', color: 'info' };
    if (temp <= 37.5) return { label: 'Normale', color: 'success' };
    if (temp <= 38) return { label: 'Fébricule', color: 'warning' };
    if (temp <= 39) return { label: 'Fièvre modérée', color: 'warning' };
    return { label: 'Fièvre élevée', color: 'danger' };
  }
}
