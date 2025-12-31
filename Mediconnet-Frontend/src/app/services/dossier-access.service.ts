import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SendCodeRequest {
  idPatient: number;
}

export interface SendCodeResponse {
  success: boolean;
  message: string;
  expiresAt?: Date;
}

export interface VerifyCodeRequest {
  idPatient: number;
  code: string;
}

export interface VerifyCodeResponse {
  success: boolean;
  message: string;
  accessToken?: string;
}

@Injectable({
  providedIn: 'root'
})
export class DossierAccessService {
  private apiUrl = `${environment.apiUrl}/medecin/dossier-access`;

  constructor(private http: HttpClient) {}

  /**
   * Envoie un code de validation par email au patient
   */
  sendValidationCode(idPatient: number): Observable<SendCodeResponse> {
    return this.http.post<SendCodeResponse>(`${this.apiUrl}/send-code`, { idPatient });
  }

  /**
   * Vérifie le code saisi par le médecin
   */
  verifyCode(idPatient: number, code: string): Observable<VerifyCodeResponse> {
    return this.http.post<VerifyCodeResponse>(`${this.apiUrl}/verify-code`, { idPatient, code });
  }
}
