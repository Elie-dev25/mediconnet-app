import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import {
  CoordinationInterventionService,
  CoordinationIntervention,
  CoordinationHistorique,
  ValiderCoordinationRequest,
  ModifierCoordinationRequest,
  RefuserCoordinationRequest
} from '../../../services/coordination-intervention.service';

@Component({
  selector: 'app-coordination-anesthesiste-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './coordination-anesthesiste-panel.component.html',
  styleUrl: './coordination-anesthesiste-panel.component.scss'
})
export class CoordinationAnesthesistePanelComponent implements OnInit {
  @Input() coordination!: CoordinationIntervention;
  @Output() closed = new EventEmitter<void>();
  @Output() actionCompleted = new EventEmitter<string>();

  activeTab: 'details' | 'historique' | 'reponse' = 'details';
  responseMode: 'valider' | 'modifier' | 'refuser' | null = null;

  historique: CoordinationHistorique[] = [];
  isLoadingHistorique = false;

  // Formulaire de réponse
  commentaire = '';
  motifRefus = '';
  dateContreProposee = '';
  heureContreProposee = '';

  // Champs pour le RDV de consultation pré-opératoire
  dateRdvConsultation = '';
  heureRdvConsultation = '';

  isSubmitting = false;
  error: string | null = null;
  success = false;

  minDate: string = '';
  maxDateRdv: string = '';

  constructor(private coordinationService: CoordinationInterventionService) {}

  ngOnInit(): void {
    this.setMinDate();
    this.loadHistorique();
  }

  private setMinDate(): void {
    const today = new Date();
    this.minDate = today.toISOString().split('T')[0];
    
    // La date max du RDV est la veille de l'intervention
    if (this.coordination?.dateProposee) {
      const dateIntervention = new Date(this.coordination.dateProposee);
      dateIntervention.setDate(dateIntervention.getDate() - 1);
      this.maxDateRdv = dateIntervention.toISOString().split('T')[0];
    }
  }

  private loadHistorique(): void {
    this.isLoadingHistorique = true;
    this.coordinationService.getHistorique(this.coordination.idCoordination).subscribe({
      next: (historique) => {
        this.historique = historique;
        this.isLoadingHistorique = false;
      },
      error: (err) => {
        console.error('Erreur chargement historique:', err);
        this.isLoadingHistorique = false;
      }
    });
  }

  setResponseMode(mode: 'valider' | 'modifier' | 'refuser'): void {
    this.responseMode = mode;
    this.activeTab = 'reponse';
    this.error = null;

    // Pré-remplir les champs pour modification
    if (mode === 'modifier') {
      this.dateContreProposee = this.coordination.dateProposee.split('T')[0];
      this.heureContreProposee = this.coordination.heureProposee;
    }
  }

  cancelResponse(): void {
    this.responseMode = null;
    this.activeTab = 'details';
    this.commentaire = '';
    this.motifRefus = '';
    this.dateContreProposee = '';
    this.heureContreProposee = '';
    this.dateRdvConsultation = '';
    this.heureRdvConsultation = '';
    this.error = null;
  }

  submitResponse(): void {
    this.error = null;

    if (this.responseMode === 'valider') {
      this.validerCoordination();
    } else if (this.responseMode === 'modifier') {
      this.modifierCoordination();
    } else if (this.responseMode === 'refuser') {
      this.refuserCoordination();
    }
  }

  private validerCoordination(): void {
    // Validation : si une date est fournie, l'heure doit l'être aussi
    if (this.dateRdvConsultation && !this.heureRdvConsultation) {
      this.error = 'Veuillez indiquer l\'heure du RDV de consultation';
      return;
    }
    if (!this.dateRdvConsultation && this.heureRdvConsultation) {
      this.error = 'Veuillez indiquer la date du RDV de consultation';
      return;
    }

    this.isSubmitting = true;

    const request: ValiderCoordinationRequest = {
      idCoordination: this.coordination.idCoordination,
      commentaireAnesthesiste: this.commentaire || undefined,
      dateRdvConsultation: this.dateRdvConsultation ? new Date(this.dateRdvConsultation).toISOString() : undefined,
      heureRdvConsultation: this.heureRdvConsultation || undefined
    };

    this.coordinationService.validerCoordination(request).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        if (response.success) {
          this.success = true;
          this.actionCompleted.emit('validee');
        } else {
          this.error = response.message;
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        this.error = err.error?.message || 'Erreur lors de la validation';
      }
    });
  }

  private modifierCoordination(): void {
    if (!this.dateContreProposee || !this.heureContreProposee) {
      this.error = 'Veuillez renseigner la date et l\'heure';
      return;
    }

    if (!this.commentaire || this.commentaire.length < 10) {
      this.error = 'Veuillez ajouter un commentaire explicatif (min 10 caractères)';
      return;
    }

    this.isSubmitting = true;

    const request: ModifierCoordinationRequest = {
      idCoordination: this.coordination.idCoordination,
      dateContreProposee: new Date(this.dateContreProposee).toISOString(),
      heureContreProposee: this.heureContreProposee,
      commentaireAnesthesiste: this.commentaire
    };

    this.coordinationService.modifierCoordination(request).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        if (response.success) {
          this.success = true;
          this.actionCompleted.emit('modifiee');
        } else {
          this.error = response.message;
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        this.error = err.error?.message || 'Erreur lors de la modification';
      }
    });
  }

  private refuserCoordination(): void {
    if (!this.motifRefus || this.motifRefus.length < 10) {
      this.error = 'Veuillez indiquer le motif de refus (min 10 caractères)';
      return;
    }

    this.isSubmitting = true;

    const request: RefuserCoordinationRequest = {
      idCoordination: this.coordination.idCoordination,
      motifRefus: this.motifRefus
    };

    this.coordinationService.refuserCoordination(request).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        if (response.success) {
          this.success = true;
          this.actionCompleted.emit('refusee');
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

  onClose(): void {
    this.closed.emit();
  }

  getStatutLabel(statut: string): string {
    return this.coordinationService.getStatutLabel(statut);
  }

  getStatutClass(statut: string): string {
    return this.coordinationService.getStatutClass(statut);
  }

  getTypeActionLabel(typeAction: string): string {
    return this.coordinationService.getTypeActionLabel(typeAction);
  }

  formatDuree(minutes: number): string {
    return this.coordinationService.formatDuree(minutes);
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('fr-FR', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    });
  }

  formatDateTime(dateStr: string): string {
    return new Date(dateStr).toLocaleString('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  canRespond(): boolean {
    return this.coordination.statut === 'proposee';
  }
}
