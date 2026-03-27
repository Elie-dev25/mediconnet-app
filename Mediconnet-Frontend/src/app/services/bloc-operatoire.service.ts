import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ==================== INTERFACES ====================

export interface BlocOperatoireDto {
  idBloc: number;
  nom: string;
  description?: string;
  statut: string;
  actif: boolean;
  localisation?: string;
  capacite?: number;
  equipements?: string;
  createdAt: string;
  updatedAt?: string;
  nombreReservationsAujourdhui: number;
  reservationEnCours?: ReservationBlocDto;
}

export interface BlocOperatoireListDto {
  idBloc: number;
  nom: string;
  description?: string;
  statut: string;
  actif: boolean;
  localisation?: string;
  nombreReservationsAujourdhui: number;
}

export interface CreateBlocOperatoireRequest {
  nom: string;
  description?: string;
  localisation?: string;
  capacite?: number;
  equipements?: string;
}

export interface UpdateBlocOperatoireRequest {
  nom?: string;
  description?: string;
  statut?: string;
  actif?: boolean;
  localisation?: string;
  capacite?: number;
  equipements?: string;
}

export interface ReservationBlocDto {
  idReservation: number;
  idBloc: number;
  nomBloc: string;
  idProgrammation: number;
  idMedecin: number;
  medecinNom: string;
  medecinPrenom: string;
  idPatient: number;
  patientNom: string;
  patientPrenom: string;
  typeIntervention: string;
  indicationOperatoire?: string;
  dateReservation: string;
  heureDebut: string;
  heureFin: string;
  dureeMinutes: number;
  statut: string;
  notes?: string;
  createdAt: string;
}

export interface ReservationBlocListDto {
  idReservation: number;
  idBloc: number;
  nomBloc: string;
  medecinNom: string;
  patientNom: string;
  typeIntervention: string;
  dateReservation: string;
  heureDebut: string;
  heureFin: string;
  dureeMinutes: number;
  statut: string;
}

export interface CreateReservationBlocRequest {
  idBloc: number;
  idProgrammation: number;
  dateReservation: string;
  heureDebut: string;
  dureeMinutes: number;
  notes?: string;
}

export interface UpdateReservationBlocRequest {
  idBloc?: number;
  dateReservation?: string;
  heureDebut?: string;
  dureeMinutes?: number;
  statut?: string;
  notes?: string;
}

export interface AgendaBlocDto {
  idBloc: number;
  nomBloc: string;
  date: string;
  creneaux: CreneauBlocDto[];
}

export interface CreneauBlocDto {
  heureDebut: string;
  heureFin: string;
  estReserve: boolean;
  reservation?: ReservationBlocDto;
}

export interface DisponibiliteBlocDto {
  idBloc: number;
  nomBloc: string;
  estDisponible: boolean;
  creneauxDisponibles: string[];
  creneauxOccupes: string[];
}

export interface VerifierDisponibiliteBlocRequest {
  date: string;
  heureDebut: string;
  dureeMinutes: number;
}

// ==================== CONSTANTES ====================

export const STATUTS_BLOC = [
  { value: 'libre', label: 'Libre', color: 'success' },
  { value: 'occupe', label: 'Occupé', color: 'danger' },
  { value: 'maintenance', label: 'Maintenance', color: 'warning' }
];

export const STATUTS_RESERVATION = [
  { value: 'confirmee', label: 'Confirmée', color: 'primary' },
  { value: 'en_cours', label: 'En cours', color: 'warning' },
  { value: 'terminee', label: 'Terminée', color: 'success' },
  { value: 'annulee', label: 'Annulée', color: 'danger' }
];

// ==================== SERVICE ====================

@Injectable({
  providedIn: 'root'
})
export class BlocOperatoireService {
  private apiUrl = `${environment.apiUrl}/blocs-operatoires`;

  constructor(private http: HttpClient) {}

  // ==================== GESTION DES BLOCS ====================

  getAllBlocs(): Observable<BlocOperatoireListDto[]> {
    return this.http.get<BlocOperatoireListDto[]>(this.apiUrl);
  }

  getBlocById(id: number): Observable<BlocOperatoireDto> {
    return this.http.get<BlocOperatoireDto>(`${this.apiUrl}/${id}`);
  }

  createBloc(request: CreateBlocOperatoireRequest): Observable<BlocOperatoireDto> {
    return this.http.post<BlocOperatoireDto>(this.apiUrl, request);
  }

  updateBloc(id: number, request: UpdateBlocOperatoireRequest): Observable<BlocOperatoireDto> {
    return this.http.put<BlocOperatoireDto>(`${this.apiUrl}/${id}`, request);
  }

  deleteBloc(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }

  // ==================== GESTION DES RÉSERVATIONS ====================

  getReservationsByBloc(idBloc: number, dateDebut?: string, dateFin?: string): Observable<ReservationBlocListDto[]> {
    let params = new HttpParams();
    if (dateDebut) params = params.set('dateDebut', dateDebut);
    if (dateFin) params = params.set('dateFin', dateFin);
    return this.http.get<ReservationBlocListDto[]>(`${this.apiUrl}/${idBloc}/reservations`, { params });
  }

  getReservationsByDate(date: string): Observable<ReservationBlocListDto[]> {
    return this.http.get<ReservationBlocListDto[]>(`${this.apiUrl}/reservations/date/${date}`);
  }

  getReservationById(idReservation: number): Observable<ReservationBlocDto> {
    return this.http.get<ReservationBlocDto>(`${this.apiUrl}/reservations/${idReservation}`);
  }

  createReservation(request: CreateReservationBlocRequest): Observable<ReservationBlocDto> {
    return this.http.post<ReservationBlocDto>(`${this.apiUrl}/reservations`, request);
  }

  updateReservation(idReservation: number, request: UpdateReservationBlocRequest): Observable<ReservationBlocDto> {
    return this.http.put<ReservationBlocDto>(`${this.apiUrl}/reservations/${idReservation}`, request);
  }

  cancelReservation(idReservation: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/reservations/${idReservation}/annuler`, {});
  }

  // ==================== DISPONIBILITÉ ====================

  getDisponibilites(date: string, heureDebut: string, dureeMinutes: number): Observable<DisponibiliteBlocDto[]> {
    const params = new HttpParams()
      .set('date', date)
      .set('heureDebut', heureDebut)
      .set('dureeMinutes', dureeMinutes.toString());
    return this.http.get<DisponibiliteBlocDto[]>(`${this.apiUrl}/disponibilites`, { params });
  }

  verifierDisponibilite(idBloc: number, request: VerifierDisponibiliteBlocRequest): Observable<{ estDisponible: boolean }> {
    return this.http.post<{ estDisponible: boolean }>(`${this.apiUrl}/${idBloc}/verifier-disponibilite`, request);
  }

  // ==================== AGENDA ====================

  getAgendaBloc(idBloc: number, date?: string): Observable<AgendaBlocDto> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<AgendaBlocDto>(`${this.apiUrl}/${idBloc}/agenda`, { params });
  }

  getAgendaTousBlocs(date?: string): Observable<AgendaBlocDto[]> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<AgendaBlocDto[]>(`${this.apiUrl}/agenda`, { params });
  }

  // ==================== HELPERS ====================

  getStatutLabel(statut: string): string {
    return STATUTS_BLOC.find(s => s.value === statut)?.label || statut;
  }

  getStatutColor(statut: string): string {
    return STATUTS_BLOC.find(s => s.value === statut)?.color || 'secondary';
  }

  getReservationStatutLabel(statut: string): string {
    return STATUTS_RESERVATION.find(s => s.value === statut)?.label || statut;
  }

  getReservationStatutColor(statut: string): string {
    return STATUTS_RESERVATION.find(s => s.value === statut)?.color || 'secondary';
  }

  formatDuree(minutes: number): string {
    const heures = Math.floor(minutes / 60);
    const mins = minutes % 60;
    if (heures === 0) return `${mins}min`;
    if (mins === 0) return `${heures}h`;
    return `${heures}h${mins}`;
  }
}
