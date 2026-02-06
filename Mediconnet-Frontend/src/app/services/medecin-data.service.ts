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
  // Informations utilisateur supplémentaires
  nationalite?: string;
  regionOrigine?: string;
  situationMatrimoniale?: string;
  // Informations médicales
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
  // Famille
  nbEnfants?: number;
  // Assurance
  assuranceNom?: string;
  numeroCarteAssurance?: string;
  dateDebutValidite?: string;
  dateFinValidite?: string;
  couvertureAssurance?: number;
  // Dates
  dateCreation?: string;
  // Historique
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

// ==================== HOSPITALISATIONS ====================

export interface PatientHospitaliseDto {
  idAdmission: number;
  idPatient: number;
  patientNom: string;
  patientPrenom: string;
  numeroDossier?: string;
  sexe?: string;
  dateNaissance?: string;
  telephone?: string;
  dateEntree: string;
  dateSortiePrevue?: string;
  motif?: string;
  statut: string;
  numeroLit?: string;
  numeroChambre?: string;
  idLit: number;
  idChambre: number;
  dureeJours: number;
}

export interface HospitalisationDetailDto {
  idAdmission: number;
  idPatient: number;
  patientNom: string;
  patientPrenom: string;
  numeroDossier?: string;
  sexe?: string;
  dateNaissance?: string;
  telephone?: string;
  email?: string;
  adresse?: string;
  groupeSanguin?: string;
  personneContact?: string;
  numeroContact?: string;
  dateEntree: string;
  dateSortiePrevue?: string;
  motif?: string;
  statut: string;
  numeroLit?: string;
  numeroChambre?: string;
  idLit: number;
  idChambre: number;
  dureeJours: number;
}

export interface ConsultationHospitalisationDto {
  idConsultation: number;
  dateConsultation: string;
  motif?: string;
  diagnostic?: string;
  statut: string;
}

// ==================== STANDARDS & CHAMBRES DISPONIBLES ====================

export interface StandardHospitalisationDto {
  idStandard: number;
  nom: string;
  description?: string;
  prixJournalier: number;
  privileges?: string;
  localisation?: string;
  chambresDisponibles: number;
}

export interface ChambreDisponibleDto {
  idChambre: number;
  numero: string;
  standardNom: string;
  prixJournalier: number;
  localisation?: string;
  litsDisponibles: LitDisponibleDto[];
}

export interface LitDisponibleDto {
  idLit: number;
  numero: string;
}

export interface CreateHospitalisationRequest {
  idPatient: number;
  idLit: number;
  motif?: string;
  dateSortiePrevue?: string;
}

export interface CreateHospitalisationResponse {
  success: boolean;
  message: string;
  data?: {
    idAdmission: number;
    idPatient: number;
    idLit: number;
    numeroChambre: string;
    numeroLit: string;
    standardNom: string;
    prixJournalier: number;
    dateEntree: string;
    dateSortiePrevue?: string;
    motif?: string;
    statut: string;
    idFacture?: number;
    numeroFacture?: string;
    montantEstime: number;
    dureeEstimeeJours: number;
  };
}

export interface AjouterSoinRequest {
  typeSoin: string;
  description: string;
  frequence?: string;
  duree?: string;
  priorite: string;
  instructions?: string;
  nbExecutionsPrevues?: number;
  dateFinPrevue?: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
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
   * Obtenir la liste des patients du médecin (paginé)
   */
  getPatients(recherche?: string, page: number = 1, pageSize: number = 50): Observable<PaginatedResponse<MedecinPatientDto>> {
    let params: any = { page, pageSize };
    if (recherche) params.recherche = recherche;
    return this.http.get<PaginatedResponse<MedecinPatientDto>>(`${this.apiUrl}/patients`, { params });
  }

  /**
   * Obtenir le détail d'un patient
   */
  getPatientDetail(idPatient: number): Observable<MedecinPatientDetailDto> {
    return this.http.get<MedecinPatientDetailDto>(`${this.apiUrl}/patients/${idPatient}`);
  }

  // ==================== HOSPITALISATIONS ====================

  /**
   * Obtenir les patients hospitalisés du médecin
   */
  getPatientsHospitalises(): Observable<{ success: boolean; data: PatientHospitaliseDto[]; total: number }> {
    return this.http.get<{ success: boolean; data: PatientHospitaliseDto[]; total: number }>(`${this.apiUrl}/patients/hospitalises`);
  }

  /**
   * Obtenir les détails d'une hospitalisation
   */
  getHospitalisationDetail(idAdmission: number): Observable<{ 
    success: boolean; 
    hospitalisation: HospitalisationDetailDto; 
    consultations: ConsultationHospitalisationDto[] 
  }> {
    return this.http.get<{ 
      success: boolean; 
      hospitalisation: HospitalisationDetailDto; 
      consultations: ConsultationHospitalisationDto[] 
    }>(`${this.apiUrl}/hospitalisation/${idAdmission}`);
  }

  /**
   * Obtenir les standards de chambre disponibles pour hospitalisation
   */
  getStandardsForHospitalisation(): Observable<{ success: boolean; data: StandardHospitalisationDto[] }> {
    return this.http.get<{ success: boolean; data: StandardHospitalisationDto[] }>(`${this.apiUrl}/hospitalisation/standards`);
  }

  /**
   * Obtenir les chambres disponibles par standard
   */
  getChambresDisponiblesByStandard(idStandard: number): Observable<{ success: boolean; data: ChambreDisponibleDto[] }> {
    return this.http.get<{ success: boolean; data: ChambreDisponibleDto[] }>(`${this.apiUrl}/hospitalisation/chambres/${idStandard}`);
  }

  /**
   * Créer une nouvelle hospitalisation
   */
  createHospitalisation(request: CreateHospitalisationRequest): Observable<CreateHospitalisationResponse> {
    return this.http.post<CreateHospitalisationResponse>(`${this.apiUrl}/hospitalisation`, request);
  }

  // ==================== SOINS HOSPITALISATION ====================

  /**
   * Ajouter un soin à une hospitalisation existante
   */
  ajouterSoin(idAdmission: number, soin: AjouterSoinRequest): Observable<{ success: boolean; message: string; idSoin?: number }> {
    return this.http.post<{ success: boolean; message: string; idSoin?: number }>(
      `${this.apiUrl}/hospitalisation/${idAdmission}/soins`, 
      soin
    );
  }

  /**
   * Récupérer les soins d'une hospitalisation
   */
  getSoinsHospitalisation(idAdmission: number): Observable<{ success: boolean; soins: any[] }> {
    return this.http.get<{ success: boolean; soins: any[] }>(
      `${this.apiUrl}/hospitalisation/${idAdmission}/soins`
    );
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
