import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import {
  CoordinationInterventionService,
  CoordinationIntervention,
  CoordinationHistorique
} from '../../../services/coordination-intervention.service';
import { ALL_ICONS_PROVIDER } from '../../icons';

type ResponseMode = 'accepter' | 'refuser' | null;

@Component({
  selector: 'app-coordination-chirurgien-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './coordination-chirurgien-panel.component.html',
  styleUrls: ['./coordination-chirurgien-panel.component.scss']
})
export class CoordinationChirurgienPanelComponent implements OnInit {
  @Input() coordination!: CoordinationIntervention;
  @Output() close = new EventEmitter<void>();
  @Output() actionCompleted = new EventEmitter<{ action: string; success: boolean }>();
  @Output() relancerAvecAutre = new EventEmitter<CoordinationIntervention>();

  activeTab: 'details' | 'historique' | 'reponse' = 'details';
  responseMode: ResponseMode = null;

  // Formulaire
  notesChirurgien = '';
  motifRefus = '';
  relancerAvecAutreAnesth = true;

  // États
  isSubmitting = false;
  isLoadingHistorique = false;
  success = false;
  error: string | null = null;
  historique: CoordinationHistorique[] = [];

  constructor(private coordinationService: CoordinationInterventionService) {}

  ngOnInit(): void {
    if (this.coordination.statut === 'modifiee') {
      this.activeTab = 'reponse';
    }
  }

  onClose(): void {
    this.close.emit();
  }

  canRespond(): boolean {
    return this.coordination.statut === 'modifiee';
  }

  hasContreProposition(): boolean {
    return this.coordination.statut === 'modifiee' && 
           !!this.coordination.dateContreProposee && 
           !!this.coordination.heureContreProposee;
  }

  setResponseMode(mode: ResponseMode): void {
    this.responseMode = mode;
    this.error = null;
    if (mode === 'accepter') {
      this.activeTab = 'reponse';
    } else if (mode === 'refuser') {
      this.activeTab = 'reponse';
    }
  }

  cancelResponse(): void {
    this.responseMode = null;
    this.error = null;
  }

  loadHistorique(): void {
    if (this.historique.length > 0) return;
    
    this.isLoadingHistorique = true;
    this.coordinationService.getHistorique(this.coordination.idCoordination)
      .subscribe({
        next: (data: CoordinationHistorique[]) => {
          this.historique = data;
          this.isLoadingHistorique = false;
        },
        error: () => {
          this.isLoadingHistorique = false;
        }
      });
  }

  submitResponse(): void {
    if (this.responseMode === 'accepter') {
      this.accepterContreProposition();
    } else if (this.responseMode === 'refuser') {
      this.refuserContreProposition();
    }
  }

  private accepterContreProposition(): void {
    this.isSubmitting = true;
    this.error = null;

    this.coordinationService.accepterContreProposition({
      idCoordination: this.coordination.idCoordination,
      notesChirurgien: this.notesChirurgien || undefined
    }).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        if (response.success) {
          this.success = true;
          this.actionCompleted.emit({ action: 'accepter', success: true });
        } else {
          this.error = response.message;
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        this.error = err.error?.message || 'Erreur lors de l\'acceptation';
      }
    });
  }

  private refuserContreProposition(): void {
    if (!this.motifRefus || this.motifRefus.length < 10) {
      this.error = 'Le motif de refus doit contenir au moins 10 caractères';
      return;
    }

    this.isSubmitting = true;
    this.error = null;

    this.coordinationService.refuserContreProposition({
      idCoordination: this.coordination.idCoordination,
      motifRefus: this.motifRefus,
      relancerAvecAutre: this.relancerAvecAutreAnesth
    }).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        if (response.success) {
          this.success = true;
          this.actionCompleted.emit({ action: 'refuser', success: true });
          
          if (this.relancerAvecAutreAnesth) {
            // Émettre l'événement pour relancer avec un autre anesthésiste
            setTimeout(() => {
              this.relancerAvecAutre.emit(this.coordination);
            }, 1500);
          }
        } else {
          this.error = response.message;
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        this.error = err.error?.message || 'Erreur lors du refus';
      }
    });
  }

  // Helpers
  getStatutClass(statut: string): string {
    const classes: Record<string, string> = {
      'proposee': 'warning',
      'validee': 'success',
      'modifiee': 'info',
      'refusee': 'danger',
      'annulee': 'secondary',
      'contre_proposition_refusee': 'danger'
    };
    return classes[statut] || 'secondary';
  }

  getStatutLabel(statut: string): string {
    const labels: Record<string, string> = {
      'proposee': 'En attente',
      'validee': 'Validée',
      'modifiee': 'Contre-proposition reçue',
      'refusee': 'Refusée',
      'annulee': 'Annulée',
      'contre_proposition_refusee': 'Contre-proposition refusée'
    };
    return labels[statut] || statut;
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { 
      weekday: 'long', 
      day: 'numeric', 
      month: 'long', 
      year: 'numeric' 
    });
  }

  formatDateTime(dateStr: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { 
      day: '2-digit', 
      month: '2-digit', 
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatDuree(minutes: number): string {
    if (minutes < 60) return `${minutes} min`;
    const h = Math.floor(minutes / 60);
    const m = minutes % 60;
    return m > 0 ? `${h}h${m.toString().padStart(2, '0')}` : `${h}h`;
  }

  getTypeActionLabel(type: string): string {
    const labels: Record<string, string> = {
      'proposition': 'Proposition',
      'validation': 'Validation',
      'modification': 'Contre-proposition',
      'refus': 'Refus',
      'annulation': 'Annulation',
      'acceptation_contre_proposition': 'Acceptation',
      'refus_contre_proposition': 'Refus contre-proposition'
    };
    return labels[type] || type;
  }
}
