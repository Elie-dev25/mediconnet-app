/**
 * @deprecated Ce service est obsolète depuis que le profil est complété lors de l'inscription.
 * La page /complete-profile n'est plus utilisée et redirige vers /register.
 * Ce fichier est conservé pour référence mais ne doit plus être utilisé.
 * Utiliser PatientProfileService pour les opérations liées au profil patient.
 */
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

/** @deprecated */
export interface PersonalInfoDto {
  dateNaissance: string;
  nationalite: string;
  regionOrigine: string;
  ethnie?: string;
  sexe: string;
  situationMatrimoniale?: string;
  nbEnfants?: number;
  adresse?: string;
}

export interface MedicalInfoDto {
  groupeSanguin?: string;
  profession?: string;
  maladiesChroniques: string[];
  autreMaladieChronique?: string;
  operationsChirurgicales: boolean;
  operationsDetails?: string;
  allergiesConnues: boolean;
  allergiesDetails?: string;
  antecedentsFamiliaux: boolean;
  antecedentsFamiliauxDetails?: string;
}

export interface LifestyleInfoDto {
  consommationAlcool: boolean;
  frequenceAlcool?: string;
  tabagisme: boolean;
  activitePhysique: boolean;
}

export interface EmergencyContactDto {
  personneContact: string;
  numeroContact: string;
}

export interface DeclarationHonneurDto {
  acceptee: boolean;
}

export interface CompleteProfileRequest {
  personalInfo: PersonalInfoDto;
  medicalInfo: MedicalInfoDto;
  lifestyleInfo: LifestyleInfoDto;
  emergencyContact: EmergencyContactDto;
  declarationHonneur: DeclarationHonneurDto;
}

export interface ProfileStatusResponse {
  profileCompleted: boolean;
  profileCompletedAt?: string;
  emailConfirmed: boolean;
  redirectTo?: string;
}

export interface ProfileFormOptions {
  regions: string[];
  groupesSanguins: string[];
  situationsMatrimoniales: string[];
  maladiesChroniquesOptions: string[];
  frequencesAlcool: string[];
}

export interface CompleteProfileResponse {
  success: boolean;
  message: string;
  errorCode?: string;
  profile?: any;
}

@Injectable({
  providedIn: 'root'
})
export class ProfileService {
  private readonly API_URL = '/api/profile';

  constructor(private http: HttpClient) {}

  /**
   * Récupère le statut de complétion du profil
   */
  getProfileStatus(): Observable<ProfileStatusResponse> {
    return this.http.get<ProfileStatusResponse>(`${this.API_URL}/status`);
  }

  /**
   * Récupère les options pour les formulaires
   */
  getFormOptions(): Observable<ProfileFormOptions> {
    return this.http.get<ProfileFormOptions>(`${this.API_URL}/form-options`);
  }

  /**
   * Récupère le profil actuel
   */
  getCurrentProfile(): Observable<any> {
    return this.http.get<any>(`${this.API_URL}/current`);
  }

  /**
   * Complète le profil patient
   */
  completeProfile(data: CompleteProfileRequest): Observable<CompleteProfileResponse> {
    return this.http.post<CompleteProfileResponse>(`${this.API_URL}/complete`, data);
  }
}
