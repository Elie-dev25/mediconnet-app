import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AccueilProfileDto {
  idUser: number;
  nom: string;
  prenom: string;
  email: string;
  telephone?: string;
  poste?: string;
  dateEmbauche?: string;
  createdAt?: string;
}

export interface AccueilDashboardDto {
  patientsEnregistresAujourdHui: number;
  patientsEnAttente: number;
  rdvPrevusAujourdHui: number;
  rdvEnCours: number;
}

export interface PatientRechercheDto {
  idPatient: number;
  numeroDossier: string;
  nom: string;
  prenom: string;
  telephone?: string;
  email?: string;
  dateNaissance?: string;
  sexe?: string;
}

export interface EnregistrerArriveeRequest {
  nom: string;
  prenom: string;
  telephone?: string;
  email?: string;
  dateNaissance?: string;
  sexe?: string;
  motif?: string;
  idMedecinCible?: number;
  idRendezVous?: number;
}

export interface EnregistrerArriveeResponse {
  success: boolean;
  message: string;
  idPatient?: number;
  numeroDossier?: string;
  nouveauPatient: boolean;
  idConsultation?: number;
  patientNom?: string;
  patientPrenom?: string;
}

export interface RdvAccueilDto {
  idRendezVous: number;
  dateHeure: string;
  patientNom: string;
  patientPrenom: string;
  patientTelephone?: string;
  medecinNom: string;
  medecinPrenom: string;
  specialite?: string;
  statut: string;
  motif?: string;
  patientArrive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AccueilService {
  private apiUrl = `${environment.apiUrl}/accueil`;

  constructor(private http: HttpClient) {}

  /**
   * Recuperer le profil de l'agent d'accueil connecte
   */
  getProfile(): Observable<AccueilProfileDto> {
    return this.http.get<AccueilProfileDto>(`${this.apiUrl}/profile`);
  }

  /**
   * Recuperer les statistiques du dashboard
   */
  getDashboard(): Observable<AccueilDashboardDto> {
    return this.http.get<AccueilDashboardDto>(`${this.apiUrl}/dashboard`);
  }

  /**
   * Rechercher un patient
   */
  rechercherPatient(terme?: string, numeroDossier?: string, telephone?: string): Observable<PatientRechercheDto[]> {
    const params: any = {};
    if (terme) params.terme = terme;
    if (numeroDossier) params.numeroDossier = numeroDossier;
    if (telephone) params.telephone = telephone;
    
    return this.http.get<PatientRechercheDto[]>(`${this.apiUrl}/patients/recherche`, { params });
  }

  /**
   * Enregistrer l'arrivee d'un patient
   */
  enregistrerArrivee(data: EnregistrerArriveeRequest): Observable<EnregistrerArriveeResponse> {
    return this.http.post<EnregistrerArriveeResponse>(`${this.apiUrl}/patients/arrivee`, data);
  }

  /**
   * Obtenir les RDV du jour
   */
  getRdvAujourdHui(): Observable<RdvAccueilDto[]> {
    return this.http.get<RdvAccueilDto[]>(`${this.apiUrl}/rdv/aujourdhui`);
  }

  /**
   * Marquer l'arrivee d'un patient pour un RDV
   */
  marquerArriveeRdv(idRendezVous: number): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/rdv/marquer-arrivee`, { idRendezVous });
  }
}
