import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ==================== INTERFACES ====================

export interface RendezVousDto {
  idRendezVous: number;
  idPatient: number;
  patientNom: string;
  patientPrenom: string;
  numeroDossier?: string;
  idMedecin: number;
  medecinNom: string;
  medecinPrenom: string;
  medecinSpecialite?: string;
  idService?: number;
  serviceNom?: string;
  dateHeure: string;
  duree: number;
  statut: string;
  motif?: string;
  notes?: string;
  typeRdv: string;
  dateCreation: string;
}

export interface RendezVousListDto {
  idRendezVous: number;
  dateHeure: string;
  duree: number;
  statut: string;
  typeRdv: string;
  motif?: string;
  medecinNom: string;
  serviceNom?: string;
}

export interface RendezVousStatsDto {
  totalRendezVous: number;
  rendezVousAVenir: number;
  rendezVousPasses: number;
  rendezVousAnnules: number;
  prochainRendezVous?: RendezVousDto;
}

export interface CreateRendezVousRequest {
  idMedecin: number;
  idService?: number;
  dateHeure: string;
  duree?: number;
  motif?: string;
  notes?: string;
  typeRdv?: string;
}

export interface UpdateRendezVousRequest {
  dateHeure?: string;
  duree?: number;
  motif?: string;
  notes?: string;
  typeRdv?: string;
}

export interface AnnulerRendezVousRequest {
  idRendezVous: number;
  motif: string;
}

export interface ActionRdvResponse {
  success: boolean;
  message: string;
  rendezVous?: RendezVousDto;
  conflitDetecte?: boolean;
}

export interface MedecinDisponibleDto {
  idMedecin: number;
  nom: string;
  prenom: string;
  specialite?: string;
  serviceNom?: string;
  idService?: number;
  prochainCreneauDansJours: number;
}

export interface ServiceDto {
  idService: number;
  nomService: string;
  description?: string;
}

/**
 * Statut possible d'un créneau horaire
 */
export type SlotStatus = 'disponible' | 'occupe' | 'indisponible' | 'verrouille' | 'passe';

export interface CreneauDisponibleDto {
  dateHeure: string;
  duree: number;
  disponible: boolean;
  /** Statut détaillé du créneau */
  statut: SlotStatus;
  /** Raison si indisponible */
  raison?: string;
  /** ID du rendez-vous si occupé */
  idRendezVous?: number;
}

export interface CreneauxDisponiblesResponse {
  medecinDisponible: boolean;
  messageIndisponibilite?: string;
  creneaux: CreneauDisponibleDto[];
  /** Nombre de créneaux disponibles */
  totalDisponibles: number;
  /** Nombre de créneaux occupés */
  totalOccupes: number;
  /** Horodatage de la réponse */
  timestamp: string;
}

// ==================== SERVICE ====================

@Injectable({
  providedIn: 'root'
})
export class RendezVousService {
  private apiUrl = `${environment.apiUrl}/rendezvous`;

  constructor(private http: HttpClient) {}

  // ==================== STATISTIQUES ====================

  getStats(): Observable<RendezVousStatsDto> {
    return this.http.get<RendezVousStatsDto>(`${this.apiUrl}/stats`);
  }

  // ==================== RENDEZ-VOUS ====================

  getUpcoming(): Observable<RendezVousListDto[]> {
    return this.http.get<RendezVousListDto[]>(`${this.apiUrl}/a-venir`);
  }

  getHistory(limite: number = 20): Observable<RendezVousListDto[]> {
    return this.http.get<RendezVousListDto[]>(`${this.apiUrl}/historique`, {
      params: { limite: limite.toString() }
    });
  }

  getById(id: number): Observable<RendezVousDto> {
    return this.http.get<RendezVousDto>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateRendezVousRequest): Observable<{ message: string; rendezVous: RendezVousDto }> {
    return this.http.post<{ message: string; rendezVous: RendezVousDto }>(this.apiUrl, request);
  }

  update(id: number, request: UpdateRendezVousRequest): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/${id}`, request);
  }

  annuler(request: AnnulerRendezVousRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/annuler`, request);
  }

  // ==================== PROPOSITIONS PATIENT ====================

  getPropositions(): Observable<RendezVousDto[]> {
    return this.http.get<RendezVousDto[]>(`${this.apiUrl}/patient/propositions`);
  }

  accepterProposition(idRendezVous: number): Observable<ActionRdvResponse> {
    return this.http.post<ActionRdvResponse>(`${this.apiUrl}/patient/accepter-proposition`, { idRendezVous });
  }

  refuserProposition(idRendezVous: number, motif?: string): Observable<ActionRdvResponse> {
    return this.http.post<ActionRdvResponse>(`${this.apiUrl}/patient/refuser-proposition`, { idRendezVous, motif });
  }

  // ==================== SERVICES ====================

  getServices(): Observable<ServiceDto[]> {
    return this.http.get<ServiceDto[]>(`${environment.apiUrl}/consultation/services`);
  }

  // ==================== MÉDECINS ET CRÉNEAUX ====================

  getMedecins(serviceId?: number): Observable<MedecinDisponibleDto[]> {
    let params = new HttpParams();
    if (serviceId) params = params.set('serviceId', serviceId.toString());
    return this.http.get<MedecinDisponibleDto[]>(`${this.apiUrl}/medecins`, { params });
  }

  getCreneaux(medecinId: number, dateDebut: string, dateFin: string): Observable<CreneauxDisponiblesResponse> {
    return this.http.get<CreneauxDisponiblesResponse>(`${this.apiUrl}/creneaux/${medecinId}`, {
      params: { dateDebut, dateFin }
    });
  }

  // ==================== HELPERS ====================

  getStatutLabel(statut: string): string {
    const labels: { [key: string]: string } = {
      'planifie': 'Planifié',
      'confirme': 'Confirmé',
      'proposition': 'Proposition en attente',
      'en_cours': 'En cours',
      'termine': 'Terminé',
      'annule': 'Annulé',
      'absent': 'Absent'
    };
    return labels[statut] || statut;
  }

  getTypeRdvLabel(type: string): string {
    const labels: { [key: string]: string } = {
      'consultation': 'Consultation',
      'suivi': 'Suivi',
      'urgence': 'Urgence',
      'examen': 'Examen',
      'vaccination': 'Vaccination'
    };
    return labels[type] || type;
  }

  formatDate(dateStr: string): string {
    const date = this.parseDate(dateStr);
    return date.toLocaleDateString('fr-FR', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    });
  }

  formatTime(dateStr: string): string {
    const date = this.parseDate(dateStr);
    return date.toLocaleTimeString('fr-FR', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatDateTime(dateStr: string): string {
    return `${this.formatDate(dateStr)} à ${this.formatTime(dateStr)}`;
  }

  formatShortDate(dateStr: string): string {
    const date = this.parseDate(dateStr);
    return date.toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: 'short'
    });
  }

  // Gestion cohérente des dates (timezone)
  private parseDate(dateStr: string): Date {
    // Si la date est au format ISO sans timezone, la traiter comme UTC
    const date = new Date(dateStr);
    return date;
  }

  toLocalISOString(date: Date): string {
    // Convertir une date locale en ISO string pour l'envoi au backend
    const offset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - offset).toISOString();
  }

  isToday(dateStr: string): boolean {
    const date = this.parseDate(dateStr);
    const today = new Date();
    return date.toDateString() === today.toDateString();
  }

  isPast(dateStr: string): boolean {
    const date = this.parseDate(dateStr);
    const now = new Date();
    // Comparer à la minute près (comme le backend)
    const dateMinute = new Date(date.getFullYear(), date.getMonth(), date.getDate(), date.getHours(), date.getMinutes(), 0);
    const nowMinute = new Date(now.getFullYear(), now.getMonth(), now.getDate(), now.getHours(), now.getMinutes(), 0);
    return dateMinute < nowMinute;
  }

  isFuture(dateStr: string): boolean {
    // Un créneau est dans le futur s'il n'est pas passé
    return !this.isPast(dateStr);
  }
}
