import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface MedecinProfileDto {
  idUser: number;
  nom: string;
  prenom: string;
  email: string;
  telephone?: string;
  adresse?: string;
  photo?: string;
  sexe?: string;
  naissance?: string;
  numeroOrdre?: string;
  specialite?: string;
  idSpecialite?: number;
  service?: string;
  idService: number;
  createdAt?: string;
}

export interface MedecinDashboardDto {
  totalPatients: number;
  consultationsMois: number;
  rdvAujourdHui: number;
  rdvAVenir: number;
  ordonnancesMois: number;
  examensMois: number;
}

export type AgendaSlotStatus = 'disponible' | 'occupe' | 'indisponible' | 'passe';

export interface AgendaSlotDto {
  dateHeure: string;
  duree: number;
  statut: AgendaSlotStatus;
  disponible: boolean;
  raison?: string;
  idRendezVous?: number;
  patientNom?: string;
  patientPrenom?: string;
  motif?: string;
}

export interface AgendaJourDto {
  date: string;
  jourSemaine: string;
  slots: AgendaSlotDto[];
  totalDisponibles: number;
  totalOccupes: number;
  totalIndisponibles: number;
}

export interface AgendaResponse {
  dateDebut: string;
  dateFin: string;
  jours: AgendaJourDto[];
  prochainRdv?: AgendaSlotDto;
}

export interface RendezVousMedecinDto {
  idConsultation: number;
  idRendezVous: number;
  dateHeure: string;
  duree: number;
  statut: string;
  motif?: string;
  typeRdv: string;
  dateCreation?: string;
  dateModification?: string | null;
  patientNom: string;
  patientPrenom: string;
  patientId: number;
  isPremiereConsultation?: boolean;
  specialiteId?: number;
  origine?: 'accueil' | 'rdv_confirme';
  heureArrivee?: string | null;
}

export interface UpdateMedecinProfileRequest {
  telephone?: string;
  adresse?: string;
}

@Injectable({
  providedIn: 'root'
})
export class MedecinService {
  private apiUrl = `${environment.apiUrl}/medecin`;

  constructor(private http: HttpClient) {}

  /**
   * Récupérer le profil complet du médecin connecté
   */
  getProfile(): Observable<MedecinProfileDto> {
    return this.http.get<MedecinProfileDto>(`${this.apiUrl}/profile`);
  }

  /**
   * Récupérer les statistiques du dashboard médecin
   */
  getDashboard(): Observable<MedecinDashboardDto> {
    return this.http.get<MedecinDashboardDto>(`${this.apiUrl}/dashboard`);
  }

  /**
   * Mettre à jour le profil du médecin
   */
  updateProfile(data: UpdateMedecinProfileRequest): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/profile`, data);
  }

  /**
   * Récupérer l'agenda du médecin (créneaux et RDV)
   */
  getAgenda(dateDebut: string, dateFin: string): Observable<AgendaResponse> {
    return this.http.get<AgendaResponse>(`${this.apiUrl}/agenda`, {
      params: { dateDebut, dateFin }
    });
  }

  /**
   * Récupérer les rendez-vous du jour
   */
  getRdvAujourdHui(): Observable<RendezVousMedecinDto[]> {
    return this.http.get<RendezVousMedecinDto[]>(`${this.apiUrl}/rdv/aujourdhui`);
  }

  /**
   * Récupérer les prochains rendez-vous
   */
  getProchainRdv(limite: number = 5): Observable<RendezVousMedecinDto[]> {
    return this.http.get<RendezVousMedecinDto[]>(`${this.apiUrl}/rdv/prochains`, {
      params: { limite: limite.toString() }
    });
  }
}
