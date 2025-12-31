import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ==================== CONSULTATIONS ====================

export interface ConsultationDto {
  idConsultation: number;
  idRendezVous: number;
  idPatient: number;
  patientNom: string;
  patientPrenom: string;
  numeroDossier?: string;
  dateConsultation: string;
  motif: string;
  diagnostic?: string;
  notes?: string;
  statut: string; // en_cours, terminee, a_faire
  duree: number;
  hasOrdonnance: boolean;
  hasExamens: boolean;
  isPremiereConsultation?: boolean;
  specialiteId?: number;
}

export interface ConsultationStatsDto {
  totalConsultations: number;
  consultationsAujourdHui: number;
  consultationsSemaine: number;
  consultationsMois: number;
  enAttente: number;
  terminees: number;
}

// ==================== PATIENTS ====================

export interface MedecinPatientDto {
  idPatient: number;
  idUser: number;
  nom: string;
  prenom: string;
  numeroDossier?: string;
  telephone?: string;
  email?: string;
  sexe?: string;
  age?: number;
  derniereVisite?: string;
  prochaineVisite?: string;
  nombreConsultations: number;
  groupeSanguin?: string;
}

export interface MedecinPatientDetailDto {
  idPatient: number;
  idUser: number;
  nom: string;
  prenom: string;
  numeroDossier?: string;
  telephone?: string;
  email?: string;
  sexe?: string;
  naissance?: string;
  adresse?: string;
  groupeSanguin?: string;
  ethnie?: string;
  personneContact?: string;
  numeroContact?: string;
  profession?: string;
  dernieresConsultations: ConsultationHistoriqueDto[];
  prochainsRdv: RendezVousHistoriqueDto[];
}

export interface ConsultationHistoriqueDto {
  idConsultation: number;
  dateConsultation: string;
  motif: string;
  diagnostic?: string;
}

export interface RendezVousHistoriqueDto {
  idRendezVous: number;
  dateHeure: string;
  motif: string;
  statut: string;
}

export interface MedecinPatientStatsDto {
  totalPatients: number;
  nouveauxCeMois: number;
  avecRdvPlanifie: number;
}

@Injectable({
  providedIn: 'root'
})
export class MedecinDataService {
  private apiUrl = `${environment.apiUrl}/medecin`;

  constructor(private http: HttpClient) {}

  // ==================== CONSULTATIONS ====================

  /**
   * Obtenir les statistiques des consultations
   */
  getConsultationStats(): Observable<ConsultationStatsDto> {
    return this.http.get<ConsultationStatsDto>(`${this.apiUrl}/consultations/stats`);
  }

  /**
   * Obtenir la liste des consultations
   */
  getConsultations(dateDebut?: string, dateFin?: string, statut?: string): Observable<ConsultationDto[]> {
    let params: any = {};
    if (dateDebut) params.dateDebut = dateDebut;
    if (dateFin) params.dateFin = dateFin;
    if (statut) params.statut = statut;
    return this.http.get<ConsultationDto[]>(`${this.apiUrl}/consultations`, { params });
  }

  /**
   * Obtenir les consultations du jour
   */
  getConsultationsJour(date?: string): Observable<ConsultationDto[]> {
    let params: any = {};
    if (date) params.date = date;
    return this.http.get<ConsultationDto[]>(`${this.apiUrl}/consultations/jour`, { params });
  }

  // ==================== PATIENTS ====================

  /**
   * Obtenir les statistiques des patients
   */
  getPatientStats(): Observable<MedecinPatientStatsDto> {
    return this.http.get<MedecinPatientStatsDto>(`${this.apiUrl}/patients/stats`);
  }

  /**
   * Obtenir la liste des patients du médecin
   */
  getPatients(recherche?: string): Observable<MedecinPatientDto[]> {
    let params: any = {};
    if (recherche) params.recherche = recherche;
    return this.http.get<MedecinPatientDto[]>(`${this.apiUrl}/patients`, { params });
  }

  /**
   * Obtenir le détail d'un patient
   */
  getPatientDetail(idPatient: number): Observable<MedecinPatientDetailDto> {
    return this.http.get<MedecinPatientDetailDto>(`${this.apiUrl}/patients/${idPatient}`);
  }

  // ==================== HELPERS ====================

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatTime(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
  }

  formatDateTime(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { 
      day: '2-digit', 
      month: 'short',
      hour: '2-digit', 
      minute: '2-digit' 
    });
  }
}
