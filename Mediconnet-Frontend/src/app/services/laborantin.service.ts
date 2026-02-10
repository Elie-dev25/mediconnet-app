import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface LaborantinStats {
  examensEnAttente: number;
  examensEnCours: number;
  examensTerminesAujourdhui: number;
  urgences: number;
  totalExamensJour: number;
}

export interface ExamenLaborantin {
  idBulletinExamen: number;
  dateDemande: string;
  typeExamen?: string;
  nomExamen?: string;
  categorie?: string;
  specialite?: string;
  instructions?: string;
  urgence: boolean;
  statut: string;
  idPatient?: number;
  patientNom?: string;
  patientPrenom?: string;
  patientNumeroDossier?: string;
  patientDateNaissance?: string;
  patientSexe?: string;
  idMedecin?: number;
  medecinNom?: string;
  medecinPrenom?: string;
  medecinSpecialite?: string;
  idLabo?: number;
  nomLabo?: string;
  dateResultat?: string;
  resultatTexte?: string;
  hasResultat: boolean;
  documentResultatUuid?: string;
}

export interface PatientExamen {
  idPatient: number;
  nom: string;
  prenom: string;
  numeroDossier?: string;
  dateNaissance?: string;
  sexe?: string;
  telephone?: string;
  groupeSanguin?: string;
  allergies?: string;
}

export interface MedecinExamen {
  idMedecin: number;
  nom: string;
  prenom: string;
  specialite?: string;
  telephone?: string;
}

export interface Laboratoire {
  idLabo: number;
  nomLabo: string;
  contact?: string;
  adresse?: string;
  telephone?: string;
  type?: string;
}

export interface DocumentResultat {
  uuid: string;
  nomFichier: string;
  mimeType?: string;
  tailleOctets: number;
  dateUpload: string;
}

export interface ResultatExamen {
  dateResultat?: string;
  resultatTexte?: string;
  commentaireLabo?: string;
  idLaborantin?: number;
  laborantinNom?: string;
  documents: DocumentResultat[];
}

export interface HistoriqueExamen {
  idBulletinExamen: number;
  dateDemande: string;
  nomExamen?: string;
  statut: string;
  dateResultat?: string;
  hasResultat: boolean;
}

export interface ExamenDetails {
  idBulletinExamen: number;
  dateDemande: string;
  typeExamen?: string;
  nomExamen?: string;
  description?: string;
  categorie?: string;
  specialite?: string;
  instructions?: string;
  urgence: boolean;
  statut: string;
  prix?: number;
  patient?: PatientExamen;
  medecin?: MedecinExamen;
  laboratoire?: Laboratoire;
  idConsultation?: number;
  idHospitalisation?: number;
  dateConsultation?: string;
  resultat?: ResultatExamen;
  historique: HistoriqueExamen[];
}

export interface ExamensListResponse {
  examens: ExamenLaborantin[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ExamensFilters {
  statut?: string;
  urgence?: boolean;
  recherche?: string;
  dateDebut?: string;
  dateFin?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({
  providedIn: 'root'
})
export class LaborantinService {
  private apiUrl = `${environment.apiUrl}/laborantin`;

  constructor(private http: HttpClient) {}

  getStats(): Observable<{ success: boolean; data: LaborantinStats }> {
    return this.http.get<{ success: boolean; data: LaborantinStats }>(`${this.apiUrl}/stats`);
  }

  getExamens(filters: ExamensFilters = {}): Observable<{ success: boolean; data: ExamensListResponse }> {
    let params = new HttpParams();
    
    if (filters.statut) params = params.set('statut', filters.statut);
    if (filters.urgence !== undefined) params = params.set('urgence', filters.urgence.toString());
    if (filters.recherche) params = params.set('recherche', filters.recherche);
    if (filters.dateDebut) params = params.set('dateDebut', filters.dateDebut);
    if (filters.dateFin) params = params.set('dateFin', filters.dateFin);
    if (filters.page) params = params.set('page', filters.page.toString());
    if (filters.pageSize) params = params.set('pageSize', filters.pageSize.toString());

    return this.http.get<{ success: boolean; data: ExamensListResponse }>(`${this.apiUrl}/examens`, { params });
  }

  getExamensEnAttente(limit: number = 10): Observable<{ success: boolean; data: ExamenLaborantin[] }> {
    return this.http.get<{ success: boolean; data: ExamenLaborantin[] }>(
      `${this.apiUrl}/examens/en-attente`,
      { params: { limit: limit.toString() } }
    );
  }

  getExamenDetails(idBulletin: number): Observable<{ success: boolean; data: ExamenDetails }> {
    return this.http.get<{ success: boolean; data: ExamenDetails }>(`${this.apiUrl}/examens/${idBulletin}`);
  }

  demarrerExamen(idBulletin: number): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/examens/${idBulletin}/demarrer`, {});
  }

  enregistrerResultat(idBulletin: number, resultatTexte: string, commentaire?: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/examens/${idBulletin}/resultat`,
      { resultatTexte, commentaire }
    );
  }

  enregistrerResultatComplet(
    idBulletin: number,
    resultatTexte: string,
    commentaire: string | null,
    fichiers: File[]
  ): Observable<{ success: boolean; message: string; documentsUuids?: string[] }> {
    const formData = new FormData();
    formData.append('resultatTexte', resultatTexte);
    if (commentaire) {
      formData.append('commentaire', commentaire);
    }
    fichiers.forEach(file => {
      formData.append('fichiers', file);
    });

    return this.http.post<{ success: boolean; message: string; documentsUuids?: string[] }>(
      `${this.apiUrl}/examens/${idBulletin}/resultat-complet`,
      formData
    );
  }

  getLaboratoires(): Observable<{ success: boolean; data: Laboratoire[] }> {
    return this.http.get<{ success: boolean; data: Laboratoire[] }>(`${this.apiUrl}/laboratoires`);
  }

  downloadDocument(uuid: string): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/api/documents/${uuid}/download`, {
      responseType: 'blob'
    });
  }
}
