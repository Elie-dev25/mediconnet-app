import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { ConsultationCompleteService, ConsultationDetailDto, ConsultationEnCoursDto } from '../../../services/consultation-complete.service';

export type ConsultationViewMode = 'patient' | 'medecin';

@Component({
  selector: 'app-consultation-details-view',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './consultation-details-view.component.html',
  styleUrl: './consultation-details-view.component.scss'
})
export class ConsultationDetailsViewComponent implements OnInit {
  @Input() consultationId: number | null = null;
  @Input() mode: ConsultationViewMode = 'patient';

  consultation: ConsultationDetailDto | null = null;
  isLoading = true;
  error: string | null = null;

  constructor(
    private router: Router,
    private consultationService: ConsultationCompleteService
  ) {}

  ngOnInit(): void {
    if (this.consultationId) {
      this.loadConsultation();
    } else {
      this.error = 'ID de consultation manquant';
      this.isLoading = false;
    }
  }

  loadConsultation(): void {
    if (!this.consultationId) return;
    
    this.consultationService.getConsultationDetails(this.consultationId).subscribe({
      next: (consultation) => {
        this.consultation = consultation;
        this.isLoading = false;
      },
      error: () => {
        // Fallback sur getConsultation
        this.consultationService.getConsultation(this.consultationId!).subscribe({
          next: (data) => {
            this.consultation = this.mapToDetailDto(data);
            this.isLoading = false;
          },
          error: (err) => {
            console.error('Erreur chargement consultation:', err);
            this.error = 'Impossible de charger les détails de la consultation';
            this.isLoading = false;
          }
        });
      }
    });
  }

  private mapToDetailDto(data: ConsultationEnCoursDto): ConsultationDetailDto {
    return {
      idConsultation: data.idConsultation,
      idPatient: data.idPatient,
      patientNom: data.patientNom,
      patientPrenom: data.patientPrenom,
      dateConsultation: data.dateHeure?.toString() || new Date().toISOString(),
      motif: data.motif,
      statut: data.statut || 'a_faire',
      anamnese: data.anamnese?.histoireMaladie,
      notesCliniques: data.diagnostic?.notesCliniques,
      diagnostic: data.diagnostic?.diagnosticPrincipal,
      ordonnance: data.prescriptions?.ordonnance,
      examensPrescrits: data.prescriptions?.examens?.map(e => ({
        nomExamen: e.nomExamen,
        instructions: e.notes
      })),
      questionnaire: data.anamnese?.questionsReponses
    };
  }

  goBack(): void {
    const backRoute = this.mode === 'medecin' ? '/medecin/consultations' : '/patient/dossier';
    this.router.navigate([backRoute]);
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

  formatTime(dateStr: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleTimeString('fr-FR', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getStatutLabel(statut: string): string {
    const labels: { [key: string]: string } = {
      'a_faire': 'À faire',
      'en_cours': 'En cours',
      'terminee': 'Terminée'
    };
    return labels[statut] || statut;
  }

  getStatutClass(statut: string): string {
    const classes: { [key: string]: string } = {
      'a_faire': 'status-pending',
      'en_cours': 'status-progress',
      'terminee': 'status-completed'
    };
    return classes[statut] || '';
  }

  printConsultation(): void {
    window.print();
  }
}
