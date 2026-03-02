import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { MedecinDataService, MedecinPatientDetailDto, ConsultationHistoriqueDto } from '../../../services/medecin-data.service';
import { ConsultationCompleteService } from '../../../services/consultation-complete.service';

export interface RdvContextInfo {
  motif?: string;
  idOrientation?: number;
  motifOrientation?: string;
  medecinOrienteur?: string;
  typeOrientation?: string;
  dateHeure?: string;
}

@Component({
  selector: 'app-fiche-patient-panel',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './fiche-patient-panel.component.html',
  styleUrl: './fiche-patient-panel.component.scss'
})
export class FichePatientPanelComponent implements OnChanges {
  @Input() patientId: number | null = null;
  @Input() isOpen = false;
  @Input() showStartConsultation = false;
  @Input() consultationId: number | null = null;
  @Input() rdvContext: RdvContextInfo | null = null;
  @Output() close = new EventEmitter<void>();
  @Output() startConsultation = new EventEmitter<void>();
  @Output() hospitaliser = new EventEmitter<number>();
  @Output() prescrireExamen = new EventEmitter<number>();
  @Output() faireOrdonnance = new EventEmitter<number>();

  selectedPatient: MedecinPatientDetailDto | null = null;
  isLoading = false;

  // Modale détails consultation
  showConsultationModal = false;
  selectedConsultation: any = null;
  isLoadingConsultation = false;

  constructor(
    private medecinDataService: MedecinDataService,
    private consultationService: ConsultationCompleteService
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['patientId'] && this.patientId && this.isOpen) {
      this.loadPatientDetail();
    }
    if (changes['isOpen'] && this.isOpen && this.patientId) {
      this.loadPatientDetail();
    }
  }

  loadPatientDetail(): void {
    if (!this.patientId) return;
    
    this.isLoading = true;
    this.medecinDataService.getPatientDetail(this.patientId).subscribe({
      next: (detail) => {
        this.selectedPatient = detail;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement patient:', err);
        this.isLoading = false;
      }
    });
  }

  onClose(): void {
    this.close.emit();
  }

  onOverlayClick(): void {
    this.close.emit();
  }

  formatDate(dateStr: string | undefined): string {
    if (!dateStr) return '-';
    return this.medecinDataService.formatDate(dateStr);
  }

  formatDateTime(dateStr: string): string {
    return this.medecinDataService.formatDateTime(dateStr);
  }

  getAge(naissance: string | undefined): string {
    if (!naissance) return '-';
    const birthDate = new Date(naissance);
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const m = today.getMonth() - birthDate.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    return `${age} ans`;
  }

  getSexeLabel(sexe: string | undefined): string {
    if (!sexe) return '-';
    return sexe === 'M' ? 'Homme' : sexe === 'F' ? 'Femme' : sexe;
  }

  onStartConsultation(): void {
    this.startConsultation.emit();
  }

  onHospitaliser(): void {
    if (this.patientId) {
      this.hospitaliser.emit(this.patientId);
    }
  }

  onPrescrireExamen(): void {
    if (this.patientId) {
      this.prescrireExamen.emit(this.patientId);
    }
  }

  onFaireOrdonnance(): void {
    if (this.patientId) {
      this.faireOrdonnance.emit(this.patientId);
    }
  }

  // Afficher les détails d'une consultation
  viewConsultationDetails(consultation: ConsultationHistoriqueDto): void {
    if (!consultation.idConsultation) return;
    
    this.isLoadingConsultation = true;
    this.showConsultationModal = true;
    
    this.consultationService.getConsultation(consultation.idConsultation).subscribe({
      next: (data: any) => {
        this.selectedConsultation = data;
        this.isLoadingConsultation = false;
      },
      error: (err: any) => {
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
