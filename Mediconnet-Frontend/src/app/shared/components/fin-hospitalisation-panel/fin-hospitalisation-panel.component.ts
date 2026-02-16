import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { HospitalisationService, TerminerHospitalisationRequest } from '../../../services/hospitalisation.service';

@Component({
  selector: 'app-fin-hospitalisation-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './fin-hospitalisation-panel.component.html',
  styleUrls: ['./fin-hospitalisation-panel.component.scss']
})
export class FinHospitalisationPanelComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() hospitalisationId: number | null = null;
  @Input() patientNom = '';
  @Input() patientPrenom = '';

  @Output() close = new EventEmitter<void>();
  @Output() completed = new EventEmitter<void>();

  // Form fields
  motifSortie = '';
  resumeMedical = '';
  dateSortie = '';

  // State
  isLoading = false;
  isSubmitting = false;
  error: string | null = null;
  success = false;

  // Validation data
  examensEnCours = 0;
  hospitalisationStatut = '';
  hospitalisationDetails: any = null;

  constructor(private hospitalisationService: HospitalisationService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen && this.hospitalisationId) {
      this.reset();
      this.loadDetails();
    }
  }

  private reset(): void {
    this.motifSortie = '';
    this.resumeMedical = '';
    this.dateSortie = new Date().toISOString().slice(0, 16);
    this.error = null;
    this.success = false;
    this.isSubmitting = false;
    this.examensEnCours = 0;
    this.hospitalisationStatut = '';
    this.hospitalisationDetails = null;
  }

  private loadDetails(): void {
    if (!this.hospitalisationId) return;

    this.isLoading = true;
    this.hospitalisationService.getHospitalisationDetails(this.hospitalisationId, 'medecin').subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.hospitalisationDetails = response.data;
          this.hospitalisationStatut = response.data.statut;
          // Count examens not terminated/cancelled
          const examens = response.data.examens || [];
          this.examensEnCours = examens.filter(
            (e: any) => e.statut !== 'termine' && e.statut !== 'annule'
          ).length;
        }
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Impossible de charger les détails';
        this.isLoading = false;
      }
    });
  }

  get canSubmit(): boolean {
    return !this.isSubmitting
      && !!this.resumeMedical.trim()
      && this.hospitalisationStatut?.toLowerCase() === 'en_cours';
  }

  get hasBlockingExamens(): boolean {
    return this.examensEnCours > 0;
  }

  onClose(): void {
    this.close.emit();
  }

  onOverlayClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('panel-overlay')) {
      this.onClose();
    }
  }

  submit(): void {
    if (!this.canSubmit || !this.hospitalisationId) return;

    this.isSubmitting = true;
    this.error = null;

    const request: TerminerHospitalisationRequest = {
      motifSortie: this.motifSortie || undefined,
      resumeMedical: this.resumeMedical,
      dateSortie: this.dateSortie || undefined
    };

    this.hospitalisationService.terminerHospitalisation(this.hospitalisationId, request).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        if (response.success) {
          this.success = true;
          setTimeout(() => {
            this.completed.emit();
          }, 1500);
        } else {
          this.error = response.message || 'Erreur lors de la terminaison';
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        this.error = err.error?.message || 'Erreur serveur';
      }
    });
  }
}
