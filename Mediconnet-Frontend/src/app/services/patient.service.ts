import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

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
  photo?: string;
  numeroDossier?: string;
  ethnie?: string;
  groupeSanguin?: string;
  nbEnfants?: number;
  personneContact?: string;
  numeroContact?: string;
  profession?: string;
  createdAt?: string;
  isProfileComplete: boolean;
  declarationHonneurAcceptee?: boolean;
}

export interface VisiteDto {
  idRendezVous: number;
  dateHeure: string;
  duree: number;
  statut: string;
  typeRdv: string;
  motif?: string;
  nomMedecin: string;
  service?: string;
}

export interface TraitementDto {
  idPrescription: number;
  medicament: string;
  posologie: string;
  dateDebut: string;
  dateFin?: string;
  nomMedecin: string;
}

export interface PatientStatsDto {
  totalRendezVous: number;
  rendezVousAVenir: number;
  rendezVousPasses: number;
  ordonnances: number;
  examens: number;
  factures: number;
}

export interface PatientDashboardDto {
  visitesAVenir: VisiteDto[];
  visitesPassees: VisiteDto[];
  traitementsPrevus: TraitementDto[];
  stats: PatientStatsDto;
}

export interface PatientBasicInfo {
  idUser: number;
  numeroDossier: string;
  nom: string;
  prenom: string;
  email: string;
  telephone: string;
  dateNaissance?: string;
  sexe?: string;
  groupeSanguin?: string;
  createdAt: string;
}

export interface PatientSearchRequest {
  searchTerm?: string;
  limit?: number;
}

export interface PatientSearchResponse {
  success: boolean;
  message: string;
  patients: PatientBasicInfo[];
  totalCount: number;
}

// DTOs pour le dossier médical
export interface ConsultationHistoryDto {
  idConsultation: number;
  dateConsultation: string;
  motif: string;
  diagnosticPrincipal?: string;
  nomMedecin: string;
  specialite?: string;
  statut: string;
}

export interface OrdonnanceDto {
  idOrdonnance: number;
  dateOrdonnance: string;
  nomMedecin: string;
  medicaments: MedicamentPrescritDto[];
  statut: string;
}

export interface MedicamentPrescritDto {
  nom: string;
  dosage: string;
  frequence: string;
  duree: string;
  instructions?: string;
}

export interface ExamenDto {
  idExamen: number;
  dateExamen: string;
  typeExamen: string;
  nomExamen: string;
  resultat?: string;
  nomMedecin: string;
  statut: string;
  urgent: boolean;
}

export interface AntecedentDto {
  type: string;
  description: string;
  dateDebut?: string;
  actif: boolean;
}

export interface AllergieDto {
  type: string;
  allergene: string;
  severite: string;
  reaction?: string;
}

export interface DossierMedicalDto {
  patient: PatientProfile;
  antecedents: AntecedentDto[];
  allergies: AllergieDto[];
  consultations: ConsultationHistoryDto[];
  ordonnances: OrdonnanceDto[];
  examens: ExamenDto[];
  stats: {
    totalConsultations: number;
    totalOrdonnances: number;
    totalExamens: number;
    derniereVisite?: string;
  };
}

export interface RecentPatientsResponse {
  success: boolean;
  message: string;
  patients: PatientBasicInfo[];
}

export interface PatientByIdResponse {
  success: boolean;
  message?: string;
  patient?: PatientBasicInfo;
}

@Injectable({
  providedIn: 'root'
})
export class PatientService {
  private apiUrl = `${environment.apiUrl}/patient`;

  constructor(private http: HttpClient) {}

  /**
   * Récupérer le profil complet du patient connecté
   */
  getProfile(): Observable<PatientProfile> {
    return this.http.get<PatientProfile>(`${this.apiUrl}/profile`);
  }

  /**
   * Mettre à jour le profil du patient
   */
  updateProfile(data: Partial<PatientProfile>): Observable<{ message: string; isComplete: boolean }> {
    return this.http.put<{ message: string; isComplete: boolean }>(`${this.apiUrl}/profile`, data);
  }

  /**
   * Récupérer les données du dashboard patient
   */
  getDashboard(): Observable<PatientDashboardDto> {
    return this.http.get<PatientDashboardDto>(`${this.apiUrl}/dashboard`);
  }

  /**
   * Récupérer les N patients les plus récents (par défaut 6)
   */
  getRecentPatients(count: number = 6): Observable<RecentPatientsResponse> {
    return this.http.get<RecentPatientsResponse>(`${this.apiUrl}/recent?count=${count}`);
  }

  /**
   * Rechercher des patients par numéro de dossier, nom ou email
   */
  searchPatients(request: PatientSearchRequest): Observable<PatientSearchResponse> {
    return this.http.post<PatientSearchResponse>(`${this.apiUrl}/search`, request);
  }

  /**
   * Récupérer le dossier médical complet du patient connecté
   */
  getDossierMedical(): Observable<DossierMedicalDto> {
    return this.http.get<DossierMedicalDto>(`${this.apiUrl}/dossier-medical`);
  }

  /**
   * Récupérer un patient par son ID
   */
  getPatientById(patientId: number): Observable<PatientByIdResponse> {
    return this.http.get<PatientByIdResponse>(`${this.apiUrl}/${patientId}`);
  }
}
