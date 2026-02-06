import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AnamneseQuestion {
  id: number;
  texte: string;
  type: 'texte' | 'text' | 'oui_non' | 'boolean' | 'choix' | 'select' | 'echelle' | 'scale';
  obligatoire: boolean;
  placeholder?: string;
  options?: string[];
  scaleMin?: number;
  scaleMax?: number;
  ordre: number;
}

export interface AnamneseReponse {
  questionId: number;
  reponse: string;
}

export interface SaveAnamneseRequest {
  consultationId: number;
  reponses: AnamneseReponse[];
}

export interface AnamneseQuestionsResponse {
  questions: AnamneseQuestion[];
  existingReponses?: { [key: number]: string };
  consultationId?: number;
  isPremiereConsultation?: boolean;
  specialiteId?: number;
}

@Injectable({
  providedIn: 'root'
})
export class PatientAnamneseService {
  private apiUrl = `${environment.apiUrl}/patient/anamnese`;

  constructor(private http: HttpClient) {}

  /**
   * Récupérer les questions d'anamnèse pour une consultation
   */
  getQuestions(consultationId: number): Observable<AnamneseQuestionsResponse> {
    return this.http.get<AnamneseQuestionsResponse>(`${this.apiUrl}/questions/${consultationId}`);
  }

  /**
   * Récupérer les questions d'anamnèse par ID de RDV (crée la consultation si nécessaire)
   */
  getQuestionsByRdv(rdvId: number): Observable<AnamneseQuestionsResponse> {
    return this.http.get<AnamneseQuestionsResponse>(`${this.apiUrl}/questions-rdv/${rdvId}`);
  }

  /**
   * Enregistrer les réponses d'anamnèse du patient
   */
  saveReponses(request: SaveAnamneseRequest): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/reponses`, request);
  }
}
