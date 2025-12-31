import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface InfirmierQueueItem {
  idConsultation: number;
  idRendezVous: number | null;
  idPatient: number;
  patientNom: string;
  patientPrenom: string;
  numeroDossier: string | null;
  idMedecin: number;
  medecinNom?: string;
  medecinPrenom?: string;
  dateHeure: string;
  statutConsultation: string | null;
}

export interface InfirmierQueueResponse {
  success: boolean;
  data: InfirmierQueueItem[];
  count: number;
  message?: string;
}

@Injectable({
  providedIn: 'root'
})
export class InfirmierService {
  private readonly API_URL = `${environment.apiUrl}/infirmier`;

  constructor(private http: HttpClient) {}

  getFileAttente(): Observable<InfirmierQueueResponse> {
    return this.http.get<InfirmierQueueResponse>(`${this.API_URL}/file-attente`);
  }
}
