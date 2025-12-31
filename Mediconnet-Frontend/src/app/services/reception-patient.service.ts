import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * DTO pour la création d'un patient complet par l'accueil
 */
export interface CreatePatientByReceptionRequest {
  // Informations personnelles (obligatoires)
  nom: string;
  prenom: string;
  dateNaissance: string; // Format ISO
  sexe: string; // 'M' ou 'F'
  telephone: string;
  
  // Informations personnelles (optionnelles)
  email?: string;
  situationMatrimoniale?: string;
  adresse: string;
  nationalite?: string;
  regionOrigine?: string;
  ethnie?: string;
  profession?: string;
  
  // Informations médicales
  groupeSanguin?: string;
  maladiesChroniques?: string;
  operationsChirurgicales?: boolean;
  operationsDetails?: string;
  allergiesConnues?: boolean;
  allergiesDetails?: string;
  antecedentsFamiliaux?: boolean;
  antecedentsFamiliauxDetails?: string;
  
  // Habitudes de vie
  consommationAlcool?: boolean;
  frequenceAlcool?: string;
  tabagisme?: boolean;
  activitePhysique?: boolean;
  
  // Contacts d'urgence
  nbEnfants?: number;
  personneContact?: string;
  numeroContact?: string;
  
  // Assurance
  assuranceId?: number;
  numeroCarteAssurance?: string;
  dateDebutValidite?: string;
  dateFinValidite?: string;
  couvertureAssurance?: number;
}

/**
 * Réponse après création d'un patient par l'accueil
 */
export interface CreatePatientByReceptionResponse {
  success: boolean;
  message: string;
  idUser?: number;
  numeroDossier?: string;
  temporaryPassword?: string;
  loginInstructions?: string;
  loginIdentifier?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ReceptionPatientService {
  private apiUrl = `${environment.apiUrl}/reception`;

  constructor(private http: HttpClient) {}

  /**
   * Crée un nouveau patient avec toutes ses informations
   */
  createPatient(request: CreatePatientByReceptionRequest): Observable<CreatePatientByReceptionResponse> {
    return this.http.post<CreatePatientByReceptionResponse>(`${this.apiUrl}/patients`, request);
  }
}
