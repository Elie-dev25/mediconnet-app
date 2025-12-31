/**
 * @deprecated Ce service n'est plus utilisé depuis que le profil est complété lors de l'inscription.
 * Les modaux de profil dans dashboard-layout ont été supprimés.
 * Pour les opérations de profil patient, utiliser PatientService (patient.service.ts).
 * Ce fichier est conservé pour référence mais ne doit plus être utilisé.
 */
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

/** @deprecated */
export interface PatientProfile {
  idUser: number;
  nom: string;
  prenom: string;
  email: string;
  naissance?: string;
  sexe?: string;
  telephone?: string;
  situationMatrimoniale?: string;
  adresse?: string;
  numeroDossier?: string;
  ethnie?: string;
  groupeSanguin?: string;
  nbEnfants?: number;
  personneContact?: string;
  numeroContact?: string;
  profession?: string;
  isProfileComplete: boolean;
}

export interface ProfileStatus {
  isComplete: boolean;
  missingFields: string[];
  message: string;
}

export interface UpdateProfileRequest {
  naissance?: string;
  sexe?: string;
  telephone?: string;
  situationMatrimoniale?: string;
  adresse?: string;
  ethnie?: string;
  groupeSanguin?: string;
  nbEnfants?: number;
  personneContact?: string;
  numeroContact?: string;
  profession?: string;
}

@Injectable({
  providedIn: 'root'
})
export class PatientProfileService {
  private apiUrl = `${environment.apiUrl}/patient`;
  
  private profileStatusSubject = new BehaviorSubject<ProfileStatus | null>(null);
  public profileStatus$ = this.profileStatusSubject.asObservable();

  private profileSubject = new BehaviorSubject<PatientProfile | null>(null);
  public profile$ = this.profileSubject.asObservable();

  constructor(private http: HttpClient) {}

  /**
   * Recuperer le profil complet du patient
   */
  getProfile(): Observable<PatientProfile> {
    return this.http.get<PatientProfile>(`${this.apiUrl}/profile`).pipe(
      tap(profile => this.profileSubject.next(profile))
    );
  }

  /**
   * Verifier si le profil est complet
   */
  checkProfileStatus(): Observable<ProfileStatus> {
    return this.http.get<ProfileStatus>(`${this.apiUrl}/profile/status`).pipe(
      tap(status => this.profileStatusSubject.next(status))
    );
  }

  /**
   * Mettre a jour le profil du patient
   */
  updateProfile(data: UpdateProfileRequest): Observable<{ message: string; isComplete: boolean }> {
    return this.http.put<{ message: string; isComplete: boolean }>(`${this.apiUrl}/profile`, data).pipe(
      tap(response => {
        if (response.isComplete) {
          this.profileStatusSubject.next({
            isComplete: true,
            missingFields: [],
            message: 'Votre profil est complet'
          });
        }
        // Rafraichir le profil apres mise a jour
        this.getProfile().subscribe();
      })
    );
  }

  /**
   * Verifier si l'alerte de profil incomplet a deja ete affichee
   */
  hasShownProfileAlert(): boolean {
    return sessionStorage.getItem('profileAlertShown') === 'true';
  }

  /**
   * Marquer l'alerte comme affichee pour cette session
   */
  markProfileAlertShown(): void {
    sessionStorage.setItem('profileAlertShown', 'true');
  }

  /**
   * Reinitialiser l'etat de l'alerte
   */
  resetProfileAlert(): void {
    sessionStorage.removeItem('profileAlertShown');
  }
}
