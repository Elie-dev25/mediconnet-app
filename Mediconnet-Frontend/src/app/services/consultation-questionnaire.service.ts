import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type RempliPar = 'patient' | 'medecin';

export interface ConsultationQuestionItemDto {
  questionId: number;
  ordreAffichage: number;
  texteQuestion: string;
  typeQuestion: string;
  estPredefinie: boolean;
  valeurReponse?: string | null;
  rempliPar?: string | null;
  dateSaisie?: string | null;
}

export interface UpsertReponseItem {
  questionId: number;
  valeurReponse?: string | null;
}

export interface UpsertReponsesRequest {
  reponses: UpsertReponseItem[];
}

export interface AddQuestionLibreRequest {
  texteQuestion: string;
  typeQuestion: string;
}

export interface SaveReponseAvecQuestionItem {
  texteQuestion: string;
  typeQuestion: string;
  valeurReponse: string;
  questionIdDb?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ConsultationQuestionnaireService {
  private apiUrl = `${environment.apiUrl}/consultations`;

  constructor(private http: HttpClient) {}

  getQuestions(consultationId: number): Observable<{ success: boolean; data: ConsultationQuestionItemDto[] }> {
    return this.http.get<{ success: boolean; data: ConsultationQuestionItemDto[] }>(
      `${this.apiUrl}/${consultationId}/questions`
    );
  }

  upsertReponses(
    consultationId: number,
    request: UpsertReponsesRequest
  ): Observable<{ success: boolean; message?: string }> {
    return this.http.post<{ success: boolean; message?: string }>(
      `${this.apiUrl}/${consultationId}/reponses`,
      request
    );
  }

  addQuestionLibre(
    consultationId: number,
    request: AddQuestionLibreRequest
  ): Observable<{ success: boolean; data: ConsultationQuestionItemDto }>{
    return this.http.post<{ success: boolean; data: ConsultationQuestionItemDto }>(
      `${this.apiUrl}/${consultationId}/questions`,
      request
    );
  }

  saveReponsesAvecQuestions(
    consultationId: number,
    reponses: SaveReponseAvecQuestionItem[]
  ): Observable<{ success: boolean; message?: string }> {
    return this.http.post<{ success: boolean; message?: string }>(
      `${this.apiUrl}/${consultationId}/questionnaire`,
      { reponses }
    );
  }
}
