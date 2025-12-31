import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ==================== INTERFACES ====================

export interface CreneauHoraireDto {
  idCreneau: number;
  jourSemaine: number;
  jourNom: string;
  heureDebut: string;
  heureFin: string;
  dureeParDefaut: number;
  actif: boolean;
}

export interface CreateCreneauRequest {
  jourSemaine: number;
  heureDebut: string;
  heureFin: string;
  dureeParDefaut?: number;
}

export interface JourSemaineDto {
  numero: number;
  nom: string;
  date?: string;
  creneaux: CreneauHoraireDto[];
  travaille: boolean;
  heuresTotal: string;
  estIndisponible?: boolean;
}

export interface SemaineTypeDto {
  jours: JourSemaineDto[];
  totalHeures: number;
  totalCreneaux: number;
}

export interface SemainePlanningDto {
  dateDebut: string;
  dateFin: string;
  label: string;
  jours: JourSemaineDto[];
  totalHeures: number;
  totalCreneaux: number;
  estSemaineCourante: boolean;
}

export interface IndisponibiliteDto {
  idIndisponibilite: number;
  dateDebut: string;
  dateFin: string;
  type: string;
  typeLibelle: string;
  motif?: string;
  journeeComplete: boolean;
  nombreJours: number;
}

export interface CreateIndisponibiliteRequest {
  dateDebut: string;
  dateFin: string;
  type: string;
  motif?: string;
  journeeComplete?: boolean;
}

export interface RdvPlanningDto {
  idRendezVous: number;
  dateHeure: string;
  duree: number;
  patientId?: number;
  idPatient?: number;
  patientNom: string;
  patientPrenom: string;
  numeroDossier?: string;
  motif?: string;
  typeRdv: string;
  statut: string;
}

export interface PlanningDashboardDto {
  rdvAujourdHui: number;
  rdvCetteSemaine: number;
  rdvCeMois: number;
  joursCongeRestants: number;
  prochainsRdv: RdvPlanningDto[];
  prochaineIndisponibilite?: IndisponibiliteDto;
}

export interface JourneeCalendrierDto {
  date: string;
  jourNom: string;
  estIndisponible: boolean;
  motifIndisponibilite?: string;
  rendezVous: RdvPlanningDto[];
}

// ==================== SERVICE ====================

@Injectable({
  providedIn: 'root'
})
export class MedecinPlanningService {
  private apiUrl = `${environment.apiUrl}/medecin/planning`;

  constructor(private http: HttpClient) {}

  // ==================== DASHBOARD ====================

  getDashboard(): Observable<PlanningDashboardDto> {
    return this.http.get<PlanningDashboardDto>(`${this.apiUrl}/dashboard`);
  }

  // ==================== CRÉNEAUX ====================

  getSemaineType(): Observable<SemaineTypeDto> {
    return this.http.get<SemaineTypeDto>(`${this.apiUrl}/semaine-type`);
  }

  getSemainePlanning(date?: string): Observable<SemainePlanningDto> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<SemainePlanningDto>(`${this.apiUrl}/semaine`, { params });
  }

  getCreneauxJour(jourSemaine: number): Observable<CreneauHoraireDto[]> {
    return this.http.get<CreneauHoraireDto[]>(`${this.apiUrl}/creneaux/${jourSemaine}`);
  }

  createCreneau(request: CreateCreneauRequest): Observable<{ message: string; creneau: CreneauHoraireDto }> {
    return this.http.post<{ message: string; creneau: CreneauHoraireDto }>(`${this.apiUrl}/creneaux`, request);
  }

  updateCreneau(id: number, request: CreateCreneauRequest): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/creneaux/${id}`, request);
  }

  deleteCreneau(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/creneaux/${id}`);
  }

  toggleCreneau(id: number): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(`${this.apiUrl}/creneaux/${id}/toggle`, {});
  }

  // ==================== INDISPONIBILITÉS ====================

  getIndisponibilites(dateDebut?: string, dateFin?: string): Observable<IndisponibiliteDto[]> {
    let params = new HttpParams();
    if (dateDebut) params = params.set('dateDebut', dateDebut);
    if (dateFin) params = params.set('dateFin', dateFin);
    return this.http.get<IndisponibiliteDto[]>(`${this.apiUrl}/indisponibilites`, { params });
  }

  createIndisponibilite(request: CreateIndisponibiliteRequest): Observable<{ message: string; indisponibilite: IndisponibiliteDto }> {
    return this.http.post<{ message: string; indisponibilite: IndisponibiliteDto }>(`${this.apiUrl}/indisponibilites`, request);
  }

  deleteIndisponibilite(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/indisponibilites/${id}`);
  }

  // ==================== CALENDRIER ====================

  getCalendrierSemaine(date?: string): Observable<JourneeCalendrierDto[]> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<JourneeCalendrierDto[]>(`${this.apiUrl}/calendrier/semaine`, { params });
  }

  getCalendrierJour(date: string): Observable<JourneeCalendrierDto> {
    return this.http.get<JourneeCalendrierDto>(`${this.apiUrl}/calendrier/jour`, {
      params: { date }
    });
  }

  // ==================== RENDEZ-VOUS MÉDECIN ====================

  getMedecinRdvList(dateDebut?: string, dateFin?: string, statut?: string): Observable<RdvPlanningDto[]> {
    let params = new HttpParams();
    if (dateDebut) params = params.set('dateDebut', dateDebut);
    if (dateFin) params = params.set('dateFin', dateFin);
    if (statut) params = params.set('statut', statut);
    return this.http.get<RdvPlanningDto[]>(`${environment.apiUrl}/rendezvous/medecin/list`, { params });
  }

  getMedecinRdvJour(date?: string): Observable<RdvPlanningDto[]> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<RdvPlanningDto[]>(`${environment.apiUrl}/rendezvous/medecin/jour`, { params });
  }

  updateStatutRdv(rdvId: number, statut: string): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(`${environment.apiUrl}/rendezvous/medecin/${rdvId}/statut`, { statut });
  }

  // ==================== HELPERS ====================

  getJourNom(numero: number): string {
    const jours = ['', 'Lundi', 'Mardi', 'Mercredi', 'Jeudi', 'Vendredi', 'Samedi', 'Dimanche'];
    return jours[numero] || '';
  }

  getTypeIndispoOptions(): { value: string; label: string }[] {
    return [
      { value: 'conge', label: 'Congés' },
      { value: 'maladie', label: 'Arrêt maladie' },
      { value: 'formation', label: 'Formation' },
      { value: 'autre', label: 'Autre' }
    ];
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', {
      weekday: 'short',
      day: 'numeric',
      month: 'short'
    });
  }

  formatTime(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleTimeString('fr-FR', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  // ==================== RDV EN ATTENTE ====================

  getRdvEnAttente(): Observable<RdvPlanningDto[]> {
    return this.http.get<RdvPlanningDto[]>(`${environment.apiUrl}/rendezvous/medecin/en-attente`);
  }

  validerRdv(idRendezVous: number): Observable<ActionRdvResponse> {
    return this.http.post<ActionRdvResponse>(`${environment.apiUrl}/rendezvous/medecin/valider`, { idRendezVous });
  }

  annulerRdvMedecin(idRendezVous: number, motif: string): Observable<ActionRdvResponse> {
    return this.http.post<ActionRdvResponse>(`${environment.apiUrl}/rendezvous/medecin/annuler`, { idRendezVous, motif });
  }

  suggererCreneau(idRendezVous: number, nouveauCreneau: string, message?: string): Observable<ActionRdvResponse> {
    return this.http.post<ActionRdvResponse>(`${environment.apiUrl}/rendezvous/medecin/suggerer-creneau`, { 
      idRendezVous, 
      nouveauCreneau,
      message 
    });
  }
}

// Interface pour les réponses d'action
export interface ActionRdvResponse {
  success: boolean;
  message: string;
  rendezVous?: RdvPlanningDto;
  conflitDetecte?: boolean;
}
