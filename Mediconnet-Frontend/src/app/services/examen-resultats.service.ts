import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface DocumentResultat {
  uuid: string;
  nomFichier: string;
  mimeType: string;
  tailleOctets: number;
  dateUpload: string;
  description?: string;
}

export interface LaboratoireInfo {
  idLabo: number;
  nomLabo: string;
  telephone?: string;
}

export interface PersonneInfo {
  idUser: number;
  nom: string;
  prenom: string;
  dateNaissance?: string;
}

export interface ResultatExamenDetail {
  idBulletinExamen: number;
  dateDemande: string;
  dateResultat?: string;
  statut: string;
  urgence: boolean;
  nomExamen: string;
  categorie?: string;
  specialite?: string;
  description?: string;
  instructions?: string;
  resultatTexte?: string;
  commentaireLabo?: string;
  laboratoire?: LaboratoireInfo;
  patient?: PersonneInfo;
  medecin?: PersonneInfo;
  documents: DocumentResultat[];
}

export interface ResultatExamenList {
  idBulletinExamen: number;
  dateDemande: string;
  dateResultat?: string;
  nomExamen: string;
  specialite?: string;
  nomLabo?: string;
  hasDocuments: boolean;
}

export interface ResultatsListResponse {
  examens: ResultatExamenList[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root'
})
export class ExamenResultatsService {
  private apiUrl = `${environment.apiUrl}/examens/resultats`;

  constructor(private http: HttpClient) {}

  /**
   * Récupérer les détails d'un résultat d'examen
   */
  getResultatExamen(idBulletin: number): Observable<{ success: boolean; data: ResultatExamenDetail }> {
    return this.http.get<{ success: boolean; data: ResultatExamenDetail }>(`${this.apiUrl}/${idBulletin}`);
  }

  /**
   * Télécharger un document de résultat
   */
  downloadDocument(idBulletin: number, uuid: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${idBulletin}/documents/${uuid}/download`, {
      responseType: 'blob'
    });
  }

  /**
   * Récupérer les résultats du patient connecté
   */
  getMesResultats(page: number = 1, pageSize: number = 20): Observable<{ success: boolean; data: ResultatsListResponse }> {
    return this.http.get<{ success: boolean; data: ResultatsListResponse }>(
      `${this.apiUrl}/patient/mes-resultats`,
      { params: { page: page.toString(), pageSize: pageSize.toString() } }
    );
  }

  /**
   * Récupérer les résultats d'un patient (pour médecin)
   */
  getResultatsPatient(idPatient: number, page: number = 1, pageSize: number = 20): Observable<{ success: boolean; data: ResultatsListResponse }> {
    return this.http.get<{ success: boolean; data: ResultatsListResponse }>(
      `${this.apiUrl}/medecin/patient/${idPatient}`,
      { params: { page: page.toString(), pageSize: pageSize.toString() } }
    );
  }

  /**
   * Formater la taille du fichier
   */
  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Number.parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  /**
   * Obtenir l'icône selon le type MIME
   */
  getFileIcon(mimeType: string): string {
    if (mimeType.startsWith('image/')) return 'image';
    if (mimeType === 'application/pdf') return 'file-text';
    if (mimeType.includes('word')) return 'file-text';
    if (mimeType.includes('excel') || mimeType.includes('spreadsheet')) return 'file-spreadsheet';
    return 'file';
  }
}
