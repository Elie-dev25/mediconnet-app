import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface FirstLoginPatientInfo {
  success: boolean;
  message: string;
  
  // Informations personnelles
  nom: string;
  prenom: string;
  dateNaissance?: string;
  sexe?: string;
  telephone?: string;
  email?: string;
  situationMatrimoniale?: string;
  adresse?: string;
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
  
  // Numéro de dossier
  numeroDossier?: string;
  
  // Statuts
  mustChangePassword: boolean;
  declarationHonneurAcceptee: boolean;
  profileCompleted: boolean;
}

export interface FirstLoginValidationRequest {
  declarationHonneurAcceptee: boolean;
  newPassword: string;
  confirmPassword: string;
}

export interface FirstLoginValidationResponse {
  success: boolean;
  message: string;
  token?: string;
  expiresIn: number;
}

export interface FirstLoginCheckResponse {
  requiresFirstLogin: boolean;
}

export interface AcceptDeclarationRequest {
  declarationHonneurAcceptee: boolean;
}

export interface AcceptDeclarationResponse {
  success: boolean;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class FirstLoginService {
  private apiUrl = `${environment.apiUrl}/reception`;

  constructor(private http: HttpClient) {}

  /**
   * Récupère les informations du patient pour la page de première connexion
   */
  getFirstLoginInfo(): Observable<FirstLoginPatientInfo> {
    return this.http.get<FirstLoginPatientInfo>(`${this.apiUrl}/first-login/info`);
  }

  /**
   * Valide la première connexion (déclaration + changement mot de passe)
   */
  validateFirstLogin(request: FirstLoginValidationRequest): Observable<FirstLoginValidationResponse> {
    return this.http.post<FirstLoginValidationResponse>(`${this.apiUrl}/first-login/validate`, request);
  }

  /**
   * Vérifie si l'utilisateur doit compléter sa première connexion
   */
  checkFirstLoginRequired(): Observable<FirstLoginCheckResponse> {
    return this.http.get<FirstLoginCheckResponse>(`${this.apiUrl}/first-login/check`);
  }

  /**
   * Accepte uniquement la déclaration sur l'honneur (sans changement de mot de passe)
   */
  acceptDeclaration(request: AcceptDeclarationRequest): Observable<AcceptDeclarationResponse> {
    return this.http.post<AcceptDeclarationResponse>(`${this.apiUrl}/first-login/accept-declaration`, request);
  }
}
