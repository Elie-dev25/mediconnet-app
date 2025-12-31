import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ChambreDto {
  idChambre: number;
  numero: string;
  capacite: number;
  etat: string;
  statut: string;
  litsDisponibles: number;
  litsOccupes: number;
  lits?: LitDto[];
}

export interface LitDto {
  idLit: number;
  numero: string;
  statut: string;
  idChambre: number;
  numeroChambre: string;
  estDisponible: boolean;
}

export interface HospitalisationDto {
  idAdmission: number;
  dateEntree: string;
  dateSortie?: string;
  motif: string;
  statut: string;
  idPatient: number;
  patientNom?: string;
  patientPrenom?: string;
  patientNumeroDossier?: string;
  idLit: number;
  numeroLit?: string;
  numeroChambre?: string;
  dureeJours?: number;
}

export interface CreerHospitalisationRequest {
  idPatient: number;
  idLit: number;
  motif?: string;
  dateEntreePrevue?: string;
  idConsultation?: number;
}

export interface DemandeHospitalisationRequest {
  idConsultation: number;
  idPatient: number;
  motif: string;
  urgence?: string;
  notes?: string;
}

export interface HospitalisationResponse {
  success: boolean;
  message: string;
  idAdmission?: number;
  hospitalisation?: HospitalisationDto;
}

export interface LitsDisponiblesResponse {
  success: boolean;
  lits: LitDto[];
  totalDisponibles: number;
}

export interface ChambresResponse {
  success: boolean;
  chambres: ChambreDto[];
  totalChambres: number;
  totalLits: number;
  litsDisponibles: number;
}

@Injectable({
  providedIn: 'root'
})
export class HospitalisationService {
  private apiUrl = `${environment.apiUrl}/hospitalisation`;

  constructor(private http: HttpClient) {}

  /**
   * Récupérer toutes les chambres avec leurs lits
   */
  getChambres(): Observable<ChambresResponse> {
    return this.http.get<ChambresResponse>(`${this.apiUrl}/chambres`);
  }

  /**
   * Récupérer les lits disponibles
   */
  getLitsDisponibles(): Observable<LitsDisponiblesResponse> {
    return this.http.get<LitsDisponiblesResponse>(`${this.apiUrl}/lits/disponibles`);
  }

  /**
   * Récupérer toutes les hospitalisations avec filtres optionnels
   */
  getHospitalisations(
    statut?: string,
    idPatient?: number,
    dateDebut?: string,
    dateFin?: string
  ): Observable<HospitalisationDto[]> {
    let params: any = {};
    if (statut) params.statut = statut;
    if (idPatient) params.idPatient = idPatient;
    if (dateDebut) params.dateDebut = dateDebut;
    if (dateFin) params.dateFin = dateFin;
    
    return this.http.get<HospitalisationDto[]>(this.apiUrl, { params });
  }

  /**
   * Récupérer une hospitalisation par son ID
   */
  getHospitalisation(id: number): Observable<HospitalisationDto> {
    return this.http.get<HospitalisationDto>(`${this.apiUrl}/${id}`);
  }

  /**
   * Récupérer l'historique des hospitalisations d'un patient
   */
  getHospitalisationsPatient(idPatient: number): Observable<HospitalisationDto[]> {
    return this.http.get<HospitalisationDto[]>(`${this.apiUrl}/patient/${idPatient}`);
  }

  /**
   * Créer une nouvelle hospitalisation
   */
  creerHospitalisation(request: CreerHospitalisationRequest): Observable<HospitalisationResponse> {
    return this.http.post<HospitalisationResponse>(this.apiUrl, request);
  }

  /**
   * Demander une hospitalisation depuis une consultation (médecin)
   */
  demanderHospitalisation(request: DemandeHospitalisationRequest): Observable<HospitalisationResponse> {
    return this.http.post<HospitalisationResponse>(`${this.apiUrl}/demande`, request);
  }

  /**
   * Terminer une hospitalisation
   */
  terminerHospitalisation(id: number, dateSortie?: string, notesDepart?: string): Observable<HospitalisationResponse> {
    return this.http.post<HospitalisationResponse>(`${this.apiUrl}/${id}/terminer`, {
      dateSortie,
      notesDepart
    });
  }
}
