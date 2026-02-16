import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AnamneseQuestionDto {
  id: number;
  texte: string;
  type: string;
  obligatoire: boolean;
  placeholder?: string;
  options?: string[];
  scaleMin?: number;
  scaleMax?: number;
  ordre: number;
}

export interface AnamneseQuestionsResponse {
  questions: AnamneseQuestionDto[];
  existingReponses?: { [questionId: number]: string };
  consultationId?: number;
  isPremiereConsultation: boolean;
  specialiteId: number;
}

export interface AnamneseReponseDto {
  questionId: number;
  reponse: string;
}

export interface SaveAnamneseRequest {
  consultationId: number;
  reponses: AnamneseReponseDto[];
}

export interface SaveReponseAvecQuestionItem {
  texteQuestion: string;
  typeQuestion?: string;
  valeurReponse: string;
}

export interface SaveReponsesAvecQuestionsRequest {
  consultationId: number;
  reponses: SaveReponseAvecQuestionItem[];
}

@Injectable({
  providedIn: 'root'
})
export class PatientAnamneseService {
  private apiUrl = `${environment.apiUrl}/patient/anamnese`;

  constructor(private http: HttpClient) {}

  /**
   * Récupérer les questions d'anamnèse par ID de RDV
   * Crée la consultation si nécessaire
   */
  getQuestionsByRdv(rdvId: number): Observable<AnamneseQuestionsResponse> {
    return this.http.get<AnamneseQuestionsResponse>(`${this.apiUrl}/questions-rdv/${rdvId}`);
  }

  /**
   * Récupérer les questions d'anamnèse par ID de consultation
   */
  getQuestions(consultationId: number): Observable<AnamneseQuestionsResponse> {
    return this.http.get<AnamneseQuestionsResponse>(`${this.apiUrl}/questions/${consultationId}`);
  }

  /**
   * Enregistrer les réponses d'anamnèse
   */
  saveReponses(request: SaveAnamneseRequest): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/reponses`, request);
  }

  /**
   * Enregistrer les réponses avec les textes des questions (crée les questions si nécessaire)
   */
  saveReponsesAvecQuestions(request: SaveReponsesAvecQuestionsRequest): Observable<{ success: boolean; message: string }> {
    const apiUrl = `${environment.apiUrl}/consultations/${request.consultationId}/questionnaire`;
    return this.http.post<{ success: boolean; message: string }>(apiUrl, {
      reponses: request.reponses
    });
  }
}
