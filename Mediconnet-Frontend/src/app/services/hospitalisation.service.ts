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
  dateSortiePrevue?: string;
  dateSortie?: string;
  motif: string;
  motifSortie?: string;
  resumeMedical?: string;
  diagnosticPrincipal?: string;
  statut: string;
  urgence?: string;
  idPatient: number;
  patientNom?: string;
  patientPrenom?: string;
  patientNumeroDossier?: string;
  idLit: number;
  numeroLit?: string;
  numeroChambre?: string;
  idLitAttribuePar?: number;
  roleLitAttribuePar?: string;
  dateLitAttribue?: string;
  idService?: number;
  serviceNom?: string;
  idMedecin?: number;
  medecinNom?: string;
  idConsultation?: number;
  dureeJours?: number;
}

export interface TerminerHospitalisationRequest {
  motifSortie?: string;
  resumeMedical: string;
  dateSortie?: string;
}

/**
 * Nouvelle requête pour ordonner une hospitalisation (médecin)
 * Le médecin ne choisit PAS de lit - le Major l'attribuera
 */
export interface OrdonnerHospitalisationRequest {
  idConsultation?: number;
  idPatient: number;
  motif: string;
  urgence?: string;
  diagnosticPrincipal?: string;
  soins?: SoinComplementaireDto[];
  dateSortiePrevue?: string;
  idServiceCible?: number;
}

export interface SoinComplementaireDto {
  typeSoin: string;
  description: string;
  frequence?: string;
  duree?: string;
  priorite: string;
  instructions?: string;
}

export interface ExamenPrescriptionDto {
  typeExamen: string;
  nomExamen: string;
  description?: string;
  urgence: boolean;
  notes?: string;
}

export interface MedicamentPrescriptionDto {
  nomMedicament: string;
  dosage?: string;
  posologie?: string;
  formePharmaceutique?: string;
  voieAdministration?: string;
  dureeTraitement?: string;
  instructions?: string;
  quantite?: number;
}

/**
 * Requête complète pour ordonner une hospitalisation avec prescriptions
 */
export interface OrdonnerHospitalisationCompleteRequest {
  idPatient: number;
  idConsultation?: number;
  motif: string;
  urgence: string;
  diagnosticPrincipal?: string;
  soinsComplementaires?: SoinComplementaireDto[];
  notes?: string;
  dateSortiePrevue?: string;
  examens?: ExamenPrescriptionDto[];
  medicaments?: MedicamentPrescriptionDto[];
  idServiceCible?: number;
}

/**
 * Requête pour attribuer un lit (Major)
 */
export interface AttribuerLitRequest {
  idAdmission: number;
  idLit: number;
  notes?: string;
}

export interface HospitalisationCreatedData extends Pick<HospitalisationDto,
  'idAdmission' |
  'idPatient' |
  'idLit' |
  'numeroChambre' |
  'numeroLit' |
  'dateEntree' |
  'dateSortiePrevue' |
  'motif' |
  'statut' |
  'idLitAttribuePar' |
  'roleLitAttribuePar' |
  'dateLitAttribue'
> {
  standardNom?: string;
  prixJournalier: number;
  idFacture?: number;
  numeroFacture?: string;
  montantEstime: number;
  dureeEstimeeJours: number;
}

export interface HospitalisationResponse {
  success: boolean;
  message: string;
  idAdmission?: number;
  hospitalisation?: HospitalisationDto;
  data?: HospitalisationCreatedData;
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
   * Ordonner une hospitalisation (nouveau workflow)
   * Le médecin ne choisit PAS de lit - le Major l'attribuera
   */
  ordonnerHospitalisation(request: OrdonnerHospitalisationRequest): Observable<HospitalisationResponse> {
    return this.http.post<HospitalisationResponse>(`${environment.apiUrl}/medecin/hospitalisation/ordonner`, request);
  }

  /**
   * Ordonner une hospitalisation complète avec prescriptions (nouveau workflow multi-étapes)
   * Inclut: hospitalisation + examens + médicaments + soins complémentaires
   */
  ordonnerHospitalisationComplete(request: OrdonnerHospitalisationCompleteRequest): Observable<HospitalisationResponse> {
    return this.http.post<HospitalisationResponse>(`${environment.apiUrl}/medecin/hospitalisation/ordonner-complete`, request);
  }

  /**
   * Attribuer un lit à une hospitalisation en attente (Major ou Médecin)
   * @param request Les données d'attribution
   * @param context Le contexte d'appel ('medecin' ou 'infirmier')
   */
  attribuerLit(request: AttribuerLitRequest, context: 'medecin' | 'infirmier' = 'infirmier'): Observable<HospitalisationResponse> {
    const endpoint = context === 'medecin' 
      ? `${environment.apiUrl}/medecin/hospitalisation/attribuer-lit`
      : `${environment.apiUrl}/infirmier/hospitalisations/attribuer-lit`;
    return this.http.post<HospitalisationResponse>(endpoint, request);
  }

  /**
   * Récupérer les hospitalisations en attente de lit (Major)
   */
  getHospitalisationsEnAttente(): Observable<any> {
    return this.http.get<any>(`${environment.apiUrl}/infirmier/hospitalisations/en-attente`);
  }

  /**
   * Récupérer les patients hospitalisés (infirmier/Major)
   * @param search Filtre optionnel par nom/prénom/numéro de dossier
   */
  getPatientsHospitalises(search?: string): Observable<any> {
    const params = search ? `?search=${encodeURIComponent(search)}` : '';
    return this.http.get<any>(`${environment.apiUrl}/infirmier/patients/hospitalises${params}`);
  }

  /**
   * Terminer une hospitalisation
   */
  terminerHospitalisation(id: number, request: TerminerHospitalisationRequest): Observable<HospitalisationResponse> {
    return this.http.post<HospitalisationResponse>(`${this.apiUrl}/${id}/terminer`, request);
  }

  /**
   * Vérifier si une hospitalisation peut être terminée (examens en cours)
   */
  getHospitalisationDetails(id: number, context: 'medecin' | 'infirmier' = 'medecin'): Observable<any> {
    return this.http.get<any>(`${environment.apiUrl}/${context}/hospitalisation/${id}/details`);
  }
}
