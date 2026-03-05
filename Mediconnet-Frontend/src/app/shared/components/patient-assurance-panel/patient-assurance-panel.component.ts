import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AssuranceService, AssuranceListItem, PatientAssuranceInfo, UpdatePatientAssuranceDto } from '../../../services/assurance.service';
import { ALL_ICONS_PROVIDER } from '../../icons';

export interface PatientBasicInfo {
  idPatient: number;
  nomComplet: string;
  telephone?: string;
  email?: string;
}

@Component({
  selector: 'app-patient-assurance-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './patient-assurance-panel.component.html',
  styleUrls: ['./patient-assurance-panel.component.scss']
})
export class PatientAssurancePanelComponent implements OnInit, OnChanges {
  @Input() isOpen = false;
  @Input() patient: PatientBasicInfo | null = null;
  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  // Données
  assurances: AssuranceListItem[] = [];
  currentAssurance: PatientAssuranceInfo | null = null;
  isLoading = false;
  isSaving = false;
  error: string | null = null;
  successMessage: string | null = null;

  // Formulaire
  formData: UpdatePatientAssuranceDto = {
    assuranceId: undefined,
    numeroCarteAssurance: '',
    dateDebutValidite: '',
    dateFinValidite: '',
    tauxCouvertureOverride: undefined
  };

  constructor(private assuranceService: AssuranceService) {}

  ngOnInit(): void {
    this.loadAssurances();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['patient'] && this.patient) {
      this.loadPatientAssurance();
    }
    if (changes['isOpen'] && this.isOpen) {
      this.error = null;
      this.successMessage = null;
    }
  }

  loadAssurances(): void {
    this.assuranceService.getAssurancesActives().subscribe({
      next: (assurances) => {
        this.assurances = assurances;
      },
      error: (err) => {
        console.error('Erreur chargement assurances:', err);
      }
    });
  }

  loadPatientAssurance(): void {
    if (!this.patient) return;

    this.isLoading = true;
    this.error = null;

    this.assuranceService.getPatientAssurance(this.patient.idPatient).subscribe({
      next: (info) => {
        this.currentAssurance = info;
        this.initFormFromAssurance(info);
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement assurance patient:', err);
        this.currentAssurance = null;
        this.resetForm();
        this.isLoading = false;
      }
    });
  }

  initFormFromAssurance(info: PatientAssuranceInfo): void {
    this.formData = {
      assuranceId: info.assuranceId || undefined,
      numeroCarteAssurance: info.numeroCarteAssurance || '',
      dateDebutValidite: info.dateDebutValidite ? this.formatDateForInput(info.dateDebutValidite) : '',
      dateFinValidite: info.dateFinValidite ? this.formatDateForInput(info.dateFinValidite) : '',
      tauxCouvertureOverride: info.tauxCouvertureOverride || undefined
    };
  }

  resetForm(): void {
    this.formData = {
      assuranceId: undefined,
      numeroCarteAssurance: '',
      dateDebutValidite: '',
      dateFinValidite: '',
      tauxCouvertureOverride: undefined
    };
  }

  formatDateForInput(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toISOString().split('T')[0];
  }

  onAssuranceChange(): void {
    // Si on sélectionne "Non assuré", réinitialiser les autres champs
    if (!this.formData.assuranceId) {
      this.formData.numeroCarteAssurance = '';
      this.formData.dateDebutValidite = '';
      this.formData.dateFinValidite = '';
      this.formData.tauxCouvertureOverride = undefined;
    }
  }

  save(): void {
    if (!this.patient) return;

    this.isSaving = true;
    this.error = null;
    this.successMessage = null;

    // Préparer les données
    const data: UpdatePatientAssuranceDto = {
      assuranceId: this.formData.assuranceId || undefined,
      numeroCarteAssurance: this.formData.numeroCarteAssurance || undefined,
      dateDebutValidite: this.formData.dateDebutValidite || undefined,
      dateFinValidite: this.formData.dateFinValidite || undefined,
      tauxCouvertureOverride: this.formData.tauxCouvertureOverride || undefined
    };

    this.assuranceService.updatePatientAssurance(this.patient.idPatient, data).subscribe({
      next: (response) => {
        this.isSaving = false;
        if (response.success) {
          this.successMessage = response.message || 'Assurance mise à jour avec succès';
          this.currentAssurance = response.data || null;
          this.saved.emit();
          
          // Fermer après un délai
          setTimeout(() => {
            this.closePanel();
          }, 1500);
        } else {
          this.error = response.message || 'Erreur lors de la mise à jour';
        }
      },
      error: (err) => {
        this.isSaving = false;
        this.error = err.error?.message || 'Erreur lors de la mise à jour de l\'assurance';
        console.error('Erreur mise à jour assurance:', err);
      }
    });
  }

  removeAssurance(): void {
    if (!this.patient) return;
    if (!confirm('Êtes-vous sûr de vouloir retirer l\'assurance de ce patient ?')) return;

    this.isSaving = true;
    this.error = null;

    this.assuranceService.removePatientAssurance(this.patient.idPatient).subscribe({
      next: (response) => {
        this.isSaving = false;
        if (response.success) {
          this.successMessage = 'Assurance retirée avec succès';
          this.currentAssurance = null;
          this.resetForm();
          this.saved.emit();
        } else {
          this.error = response.message || 'Erreur lors du retrait';
        }
      },
      error: (err) => {
        this.isSaving = false;
        this.error = err.error?.message || 'Erreur lors du retrait de l\'assurance';
      }
    });
  }

  closePanel(): void {
    this.close.emit();
  }

  getStatutLabel(): string {
    if (!this.currentAssurance) return 'Non assuré';
    if (!this.currentAssurance.estAssure) return 'Non assuré';
    if (!this.currentAssurance.estValide) return 'Expirée';
    return 'Valide';
  }

  getStatutClass(): string {
    if (!this.currentAssurance || !this.currentAssurance.estAssure) return 'status-none';
    if (!this.currentAssurance.estValide) return 'status-expired';
    return 'status-valid';
  }

  isExpiringSoon(): boolean {
    if (!this.formData.dateFinValidite) return false;
    const expDate = new Date(this.formData.dateFinValidite);
    const today = new Date();
    const diffDays = Math.ceil((expDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
    return diffDays > 0 && diffDays <= 30;
  }

  getDaysUntilExpiry(): number {
    if (!this.formData.dateFinValidite) return 0;
    const expDate = new Date(this.formData.dateFinValidite);
    const today = new Date();
    return Math.ceil((expDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
  }
}
