import { Component, Input, Output, EventEmitter, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { PrescriptionExamensComponent, ExamenPrescription } from '../prescription-examens/prescription-examens.component';

@Component({
  selector: 'app-examen-hospitalisation-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, PrescriptionExamensComponent],
  templateUrl: './examen-hospitalisation-panel.component.html',
  styleUrls: ['./examen-hospitalisation-panel.component.scss']
})
export class ExamenHospitalisationPanelComponent implements OnInit {
  @Input() isOpen = false;
  @Input() hospitalisationId: number | null = null;
  @Input() patientNom = '';
  @Input() patientPrenom = '';

  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  @ViewChild(PrescriptionExamensComponent) examensComp!: PrescriptionExamensComponent;

  examens: ExamenPrescription[] = [];
  notes = '';

  isSubmitting = false;
  error: string | null = null;
  success = false;

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {}

  onExamensChange(examens: ExamenPrescription[]): void {
    this.examens = examens;
  }

  onClose(): void {
    this.resetForm();
    this.close.emit();
  }

  onOverlayClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('panel-overlay')) {
      this.onClose();
    }
  }

  resetForm(): void {
    this.examens = [];
    this.notes = '';
    this.error = null;
    this.success = false;
  }

  submit(): void {
    if (!this.hospitalisationId) {
      this.error = 'ID d\'hospitalisation manquant';
      return;
    }

    const examensData = this.examensComp?.getExamensData() || this.examens;

    if (examensData.length === 0) {
      this.error = 'Veuillez sélectionner au moins un examen';
      return;
    }

    this.isSubmitting = true;
    this.error = null;

    const payload = {
      examens: examensData.map(e => ({
        typeExamen: e.typeExamen,
        nomExamen: e.nomExamen,
        description: e.description,
        urgence: e.urgence,
        notes: e.notes,
        idLaboratoire: e.idLaboratoire
      })),
      notes: this.notes || undefined
    };

    this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/medecin/hospitalisation/${this.hospitalisationId}/examens`,
      payload
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = true;
          setTimeout(() => {
            this.saved.emit();
            this.onClose();
          }, 1500);
        } else {
          this.error = response.message || 'Erreur lors de l\'enregistrement';
        }
        this.isSubmitting = false;
      },
      error: (err) => {
        console.error('Erreur examens hospitalisation:', err);
        this.error = err.error?.message || 'Erreur lors de la prescription des examens';
        this.isSubmitting = false;
      }
    });
  }

  get canSubmit(): boolean {
    return this.examens.length > 0 && !this.isSubmitting;
  }
}
