import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ==================== Interfaces ====================

export interface CreneauDisponible {
  date: string;
  heureDebut: string;
  heureFin: string;
  dureeMinutes: number;
  estDisponible: boolean;
  motifIndisponibilite?: string;
}

export interface AnesthesisteDisponibilite {
  idMedecin: number;
  nom: string;
  prenom: string;
  nomComplet: string;
  photo?: string;
  nbInterventionsSemaine: number;
  creneauxDisponibles: CreneauDisponible[];
}

export interface CoordinationIntervention {
  idCoordination: number;
  idProgrammation: number;
  idChirurgien: number;
  nomChirurgien: string;
  specialiteChirurgien: string;
  idAnesthesiste: number;
  nomAnesthesiste: string;
  idPatient: number;
  nomPatient: string;
  indicationOperatoire: string;
  typeIntervention: string;
  dateProposee: string;
  heureProposee: string;
  dureeEstimee: number;
  statut: 'proposee' | 'validee' | 'modifiee' | 'refusee' | 'annulee' | 'contre_proposition_refusee';
  dateContreProposee?: string;
  heureContreProposee?: string;
  commentaireAnesthesiste?: string;
  motifRefus?: string;
  notesChirurgien?: string;
  notesAnesthesie?: string;
  classificationAsa?: string;
  risqueOperatoire?: string;
  idRdvConsultationAnesthesiste?: number;
  dateRdvConsultation?: string;
  dateValidation?: string;
  dateReponse?: string;
  nbModifications: number;
  createdAt: string;
}

export interface CoordinationHistorique {
  idHistorique: number;
  typeAction: string;
  nomUser: string;
  roleUser: string;
  details?: string;
  dateProposee?: string;
  heureProposee?: string;
  createdAt: string;
}

export interface CoordinationStats {
  enAttente: number;
  validees: number;
  modifiees: number;
  refusees: number;
  total: number;
}

export interface CoordinationActionResponse {
  success: boolean;
  message: string;
  idCoordination?: number;
  nouveauStatut?: string;
  idRdvConsultationAnesthesiste?: number;
  dateRdvConsultation?: string;
}

// ==================== Requêtes ====================

export interface ProposerCoordinationRequest {
  idProgrammation: number;
  idAnesthesiste: number;
  dateProposee: string;
  heureProposee: string;
  dureeEstimee: number;
  notesChirurgien?: string;
}

export interface ValiderCoordinationRequest {
  idCoordination: number;
  commentaireAnesthesiste?: string;
  dateRdvConsultation?: string;
  heureRdvConsultation?: string;
}

export interface ModifierCoordinationRequest {
  idCoordination: number;
  dateContreProposee: string;
  heureContreProposee: string;
  commentaireAnesthesiste: string;
}

export interface RefuserCoordinationRequest {
  idCoordination: number;
  motifRefus: string;
}

export interface AccepterContrePropositionRequest {
  idCoordination: number;
  notesChirurgien?: string;
}

export interface RefuserContrePropositionRequest {
  idCoordination: number;
  motifRefus: string;
  relancerAvecAutre?: boolean;
}

export interface AnnulerCoordinationRequest {
  idCoordination: number;
  motifAnnulation: string;
}

export interface CoordinationFilter {
  statut?: string;
  dateDebut?: string;
  dateFin?: string;
  idChirurgien?: number;
  idAnesthesiste?: number;
  idPatient?: number;
}

@Injectable({
  providedIn: 'root'
})
export class CoordinationInterventionService {
  private apiUrl = `${environment.apiUrl}/coordination-intervention`;

  constructor(private http: HttpClient) {}

  // ==================== Anesthésistes et disponibilités ====================

  getAnesthesistesDisponibles(
    dateDebut: string,
    dateFin: string,
    dureeMinutes: number = 60
  ): Observable<AnesthesisteDisponibilite[]> {
    const params = new HttpParams()
      .set('dateDebut', dateDebut)
      .set('dateFin', dateFin)
      .set('dureeMinutes', dureeMinutes.toString());

    return this.http.get<AnesthesisteDisponibilite[]>(
      `${this.apiUrl}/anesthesistes`,
      { params }
    );
  }

  getCreneauxAnesthesiste(
    idAnesthesiste: number,
    dateDebut: string,
    dateFin: string
  ): Observable<CreneauDisponible[]> {
    const params = new HttpParams()
      .set('dateDebut', dateDebut)
      .set('dateFin', dateFin);

    return this.http.get<CreneauDisponible[]>(
      `${this.apiUrl}/anesthesistes/${idAnesthesiste}/creneaux`,
      { params }
    );
  }

  // ==================== Actions Chirurgien ====================

  proposerCoordination(request: ProposerCoordinationRequest): Observable<CoordinationActionResponse> {
    return this.http.post<CoordinationActionResponse>(
      `${this.apiUrl}/proposer`,
      request
    );
  }

  accepterContreProposition(request: AccepterContrePropositionRequest): Observable<CoordinationActionResponse> {
    return this.http.post<CoordinationActionResponse>(
      `${this.apiUrl}/accepter-contre-proposition`,
      request
    );
  }

  refuserContreProposition(request: RefuserContrePropositionRequest): Observable<CoordinationActionResponse> {
    return this.http.post<CoordinationActionResponse>(
      `${this.apiUrl}/refuser-contre-proposition`,
      request
    );
  }

  getMesCoordinationsChirurgien(filter?: CoordinationFilter): Observable<CoordinationIntervention[]> {
    let params = new HttpParams();
    if (filter) {
      if (filter.statut) params = params.set('statut', filter.statut);
      if (filter.dateDebut) params = params.set('dateDebut', filter.dateDebut);
      if (filter.dateFin) params = params.set('dateFin', filter.dateFin);
    }

    return this.http.get<CoordinationIntervention[]>(
      `${this.apiUrl}/chirurgien/mes-coordinations`,
      { params }
    );
  }

  // ==================== Actions Anesthésiste ====================

  validerCoordination(request: ValiderCoordinationRequest): Observable<CoordinationActionResponse> {
    return this.http.post<CoordinationActionResponse>(
      `${this.apiUrl}/valider`,
      request
    );
  }

  modifierCoordination(request: ModifierCoordinationRequest): Observable<CoordinationActionResponse> {
    return this.http.post<CoordinationActionResponse>(
      `${this.apiUrl}/modifier`,
      request
    );
  }

  refuserCoordination(request: RefuserCoordinationRequest): Observable<CoordinationActionResponse> {
    return this.http.post<CoordinationActionResponse>(
      `${this.apiUrl}/refuser`,
      request
    );
  }

  getMesCoordinationsAnesthesiste(filter?: CoordinationFilter): Observable<CoordinationIntervention[]> {
    let params = new HttpParams();
    if (filter) {
      if (filter.statut) params = params.set('statut', filter.statut);
      if (filter.dateDebut) params = params.set('dateDebut', filter.dateDebut);
      if (filter.dateFin) params = params.set('dateFin', filter.dateFin);
    }

    return this.http.get<CoordinationIntervention[]>(
      `${this.apiUrl}/anesthesiste/mes-coordinations`,
      { params }
    );
  }

  getDemandesEnAttente(): Observable<CoordinationIntervention[]> {
    return this.http.get<CoordinationIntervention[]>(
      `${this.apiUrl}/anesthesiste/demandes-en-attente`
    );
  }

  getStatsAnesthesiste(): Observable<CoordinationStats> {
    return this.http.get<CoordinationStats>(
      `${this.apiUrl}/anesthesiste/stats`
    );
  }

  // ==================== Actions communes ====================

  annulerCoordination(request: AnnulerCoordinationRequest): Observable<CoordinationActionResponse> {
    return this.http.post<CoordinationActionResponse>(
      `${this.apiUrl}/annuler`,
      request
    );
  }

  getCoordination(idCoordination: number): Observable<CoordinationIntervention> {
    return this.http.get<CoordinationIntervention>(
      `${this.apiUrl}/${idCoordination}`
    );
  }

  getHistorique(idCoordination: number): Observable<CoordinationHistorique[]> {
    return this.http.get<CoordinationHistorique[]>(
      `${this.apiUrl}/${idCoordination}/historique`
    );
  }

  // ==================== Helpers ====================

  getStatutLabel(statut: string): string {
    const labels: { [key: string]: string } = {
      'proposee': 'Proposée',
      'validee': 'Validée',
      'modifiee': 'Contre-proposition',
      'refusee': 'Refusée',
      'annulee': 'Annulée'
    };
    return labels[statut] || statut;
  }

  getStatutClass(statut: string): string {
    const classes: { [key: string]: string } = {
      'proposee': 'warning',
      'validee': 'success',
      'modifiee': 'info',
      'refusee': 'danger',
      'annulee': 'secondary'
    };
    return classes[statut] || 'secondary';
  }

  getTypeActionLabel(typeAction: string): string {
    const labels: { [key: string]: string } = {
      'proposition': 'Proposition initiale',
      'validation': 'Validation',
      'modification': 'Contre-proposition',
      'refus': 'Refus',
      'annulation': 'Annulation',
      'acceptation_contre_proposition': 'Acceptation contre-proposition'
    };
    return labels[typeAction] || typeAction;
  }

  formatDuree(minutes: number): string {
    const heures = Math.floor(minutes / 60);
    const mins = minutes % 60;
    if (heures === 0) return `${mins} min`;
    if (mins === 0) return `${heures}h`;
    return `${heures}h${mins}`;
  }
}
