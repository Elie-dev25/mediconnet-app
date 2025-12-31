import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { ConsultationCompleteService, DossierPatientDto, HistoriqueConsultationDto, ConsultationEnCoursDto } from '../../../services/consultation-complete.service';

@Component({
  selector: 'app-dossier-patient',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './dossier-patient.component.html',
  styleUrl: './dossier-patient.component.scss'
})
export class DossierPatientComponent implements OnInit {
  @Input() patientId!: number;
  @Input() consultationId?: number;
  @Output() startConsultation = new EventEmitter<{ patientId: number; consultationId: number }>();
  @Output() viewConsultation = new EventEmitter<{ consultationId: number }>();
  @Output() close = new EventEmitter<void>();

  dossier: DossierPatientDto | null = null;
  isLoading = true;
  error: string | null = null;
  activeTab: 'infos' | 'consultations' | 'ordonnances' | 'examens' = 'infos';

  // Modale détails consultation
  showConsultationModal = false;
  selectedConsultation: ConsultationEnCoursDto | null = null;
  isLoadingConsultation = false;

  constructor(private consultationService: ConsultationCompleteService) {}

  ngOnInit(): void {
    this.loadDossier();
  }

  loadDossier(): void {
    this.isLoading = true;
    this.error = null;

    this.consultationService.getDossierPatient(this.patientId).subscribe({
      next: (dossier) => {
        this.dossier = dossier;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement dossier:', err);
        this.error = 'Impossible de charger le dossier patient';
        this.isLoading = false;
      }
    });
  }

  setTab(tab: 'infos' | 'consultations' | 'ordonnances' | 'examens'): void {
    this.activeTab = tab;
  }

  onStartConsultation(): void {
    if (this.consultationId) {
      this.startConsultation.emit({ patientId: this.patientId, consultationId: this.consultationId });
    }
  }

  onClose(): void {
    this.close.emit();
  }

  formatDate(date: Date | string | undefined): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  formatDateTime(date: Date | string | undefined): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getStatutClass(statut: string | undefined): string {
    switch (statut) {
      case 'terminee': return 'statut-terminee';
      case 'en_cours': return 'statut-en-cours';
      case 'prescrit': return 'statut-prescrit';
      case 'realise': return 'statut-realise';
      default: return '';
    }
  }

  getStatutLabel(statut: string | undefined): string {
    switch (statut) {
      case 'terminee': return 'Terminée';
      case 'en_cours': return 'En cours';
      case 'prescrit': return 'Prescrit';
      case 'realise': return 'Réalisé';
      case 'annule': return 'Annulé';
      default: return statut || '-';
    }
  }

  onViewConsultation(consultationId: number, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    this.loadConsultationDetails(consultationId);
  }

  loadConsultationDetails(consultationId: number): void {
    this.isLoadingConsultation = true;
    this.showConsultationModal = true;
    
    this.consultationService.getConsultation(consultationId).subscribe({
      next: (consultation) => {
        this.selectedConsultation = consultation;
        this.isLoadingConsultation = false;
      },
      error: (err) => {
        console.error('Erreur chargement consultation:', err);
        this.isLoadingConsultation = false;
        this.showConsultationModal = false;
      }
    });
  }

  closeConsultationModal(): void {
    this.showConsultationModal = false;
    this.selectedConsultation = null;
  }
}
