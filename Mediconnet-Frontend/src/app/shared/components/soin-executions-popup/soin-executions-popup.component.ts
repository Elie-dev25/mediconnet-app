import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { LucideAngularModule } from 'lucide-angular';
import { environment } from '../../../../environments/environment';

export interface ExecutionDetail {
  idExecution: number;
  datePrevue: string;
  moment: string;
  numeroSeance?: number;
  heurePrevue: string;
  heureExecution?: string;
  statut: string;
  dateExecution?: string;
  executant?: string;
  observations?: string;
}

export interface ExecutionParJour {
  date: string;
  executions: ExecutionDetail[];
}

export interface SoinDetailsResponse {
  idSoin: number;
  typeSoin: string;
  description: string;
  dureeJours: number;
  moments: string;
  priorite: string;
  instructions?: string;
  statut: string;
  datePrescription: string;
  dateDebut: string;
  dateFinPrevue: string;
  prescripteur?: string;
  patient?: string;
  nbExecutionsPrevues: number;
  nbExecutionsEffectuees: number;
  nbExecutionsManquees: number;
  nbExecutionsPrevuesRestantes: number;
  executionsParJour: ExecutionParJour[];
}

@Component({
  selector: 'app-soin-executions-popup',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './soin-executions-popup.component.html',
  styleUrls: ['./soin-executions-popup.component.scss']
})
export class SoinExecutionsPopupComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() soinId: number | null = null;
  @Output() close = new EventEmitter<void>();

  soinDetails: SoinDetailsResponse | null = null;
  isLoading = false;
  error: string | null = null;

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen && this.soinId) {
      this.loadSoinDetails();
    }
    if (changes['isOpen'] && !this.isOpen) {
      this.soinDetails = null;
      this.error = null;
    }
  }

  loadSoinDetails(): void {
    if (!this.soinId) return;

    this.isLoading = true;
    this.error = null;

    this.http.get<{ success: boolean; data: SoinDetailsResponse }>(
      `${this.apiUrl}/medecin/hospitalisation/soins/${this.soinId}/details`
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.soinDetails = response.data;
        } else {
          this.error = 'Impossible de charger les détails';
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement détails soin:', err);
        this.error = 'Erreur de connexion au serveur';
        this.isLoading = false;
      }
    });
  }

  onClose(): void {
    this.close.emit();
  }

  onOverlayClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('popup-overlay')) {
      this.onClose();
    }
  }

  getMomentIcon(moment: string): string {
    switch (moment?.toLowerCase()) {
      case 'matin': return 'sun';
      case 'midi': return 'sun';
      case 'soir': return 'cloud';
      case 'nuit': return 'moon';
      default: return 'clock';
    }
  }

  getMomentLabel(moment: string): string {
    switch (moment?.toLowerCase()) {
      case 'matin': return 'Matin';
      case 'midi': return 'Midi';
      case 'soir': return 'Soir';
      case 'nuit': return 'Nuit';
      default: return moment;
    }
  }

  getStatutClass(statut: string): string {
    switch (statut?.toLowerCase()) {
      case 'fait': return 'status-done';
      case 'manque': return 'status-missed';
      case 'prevu': return 'status-pending';
      default: return 'status-pending';
    }
  }

  getStatutLabel(statut: string): string {
    switch (statut?.toLowerCase()) {
      case 'fait': return 'Fait';
      case 'manque': return 'Manqué';
      case 'prevu': return 'Prévu';
      default: return statut;
    }
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { 
      weekday: 'long',
      day: '2-digit', 
      month: 'long', 
      year: 'numeric' 
    });
  }

  formatTime(timeStr: string): string {
    if (!timeStr) return '-';
    return timeStr.substring(0, 5);
  }

  formatDateTime(dateStr: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { 
      day: '2-digit', 
      month: 'short',
      hour: '2-digit', 
      minute: '2-digit' 
    });
  }

  getProgressPercent(): number {
    if (!this.soinDetails || this.soinDetails.nbExecutionsPrevues <= 0) return 0;
    return Math.round((this.soinDetails.nbExecutionsEffectuees / this.soinDetails.nbExecutionsPrevues) * 100);
  }

  getExecutionForMoment(executions: ExecutionDetail[], moment: string): ExecutionDetail | null {
    return executions.find(e => e.moment?.toLowerCase() === moment.toLowerCase()) || null;
  }

  hasMoment(moment: string): boolean {
    if (!this.soinDetails?.executionsParJour) return false;
    return this.soinDetails.executionsParJour.some(jour => 
      jour.executions.some(e => e.moment?.toLowerCase() === moment.toLowerCase())
    );
  }

  getStatutIcon(statut: string): string {
    switch (statut?.toLowerCase()) {
      case 'fait': return 'check-circle';
      case 'manque': return 'x-circle';
      case 'prevu': return 'clock';
      default: return 'clock';
    }
  }

  getDayName(dateStr: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { weekday: 'short' });
  }

  formatShortDate(dateStr: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit' });
  }

  formatTimeOnly(dateStr: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
  }

  // ==================== Méthodes pour les séances ====================

  getSeances(): number[] {
    if (!this.soinDetails?.executionsParJour?.length) return [1];
    
    // Trouver le nombre max de séances par jour
    let maxSeances = 1;
    for (const jour of this.soinDetails.executionsParJour) {
      if (jour.executions.length > maxSeances) {
        maxSeances = jour.executions.length;
      }
    }
    return Array.from({ length: maxSeances }, (_, i) => i + 1);
  }

  getExecutionForSeance(executions: ExecutionDetail[], seance: number): ExecutionDetail | null {
    // Chercher par numeroSeance d'abord
    const bySeance = executions.find(e => e.numeroSeance === seance);
    if (bySeance) return bySeance;
    
    // Fallback: utiliser l'index (pour les anciens soins)
    if (executions.length >= seance) {
      return executions[seance - 1];
    }
    return null;
  }

  formatHeurePrevue(heurePrevue: string): string {
    if (!heurePrevue) return '--:--';
    // Format TimeSpan "HH:mm:ss" -> "HH:mm"
    if (heurePrevue.includes(':')) {
      return heurePrevue.substring(0, 5);
    }
    return heurePrevue;
  }

  formatHeureExecution(exec: ExecutionDetail): string {
    // Utiliser heureExecution si disponible
    if (exec.heureExecution) {
      return exec.heureExecution.substring(0, 5);
    }
    // Sinon extraire de dateExecution
    if (exec.dateExecution) {
      const date = new Date(exec.dateExecution);
      return date.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
    }
    return '--:--';
  }

  isEnRetard(exec: ExecutionDetail): boolean {
    if (!exec.heurePrevue || exec.statut !== 'fait') return false;
    
    const heurePrevue = this.parseTimeToMinutes(exec.heurePrevue);
    let heureEffective: number;
    
    if (exec.heureExecution) {
      heureEffective = this.parseTimeToMinutes(exec.heureExecution);
    } else if (exec.dateExecution) {
      const date = new Date(exec.dateExecution);
      heureEffective = date.getHours() * 60 + date.getMinutes();
    } else {
      return false;
    }
    
    // Considérer en retard si > 30 minutes après l'heure prévue
    return heureEffective > heurePrevue + 30;
  }

  private parseTimeToMinutes(timeStr: string): number {
    if (!timeStr) return 0;
    const parts = timeStr.split(':');
    return Number.parseInt(parts[0], 10) * 60 + Number.parseInt(parts[1], 10);
  }
}
