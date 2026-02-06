import { Component, Input, Output, EventEmitter, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { PrescriptionMedicamentsComponent, MedicamentPrescription } from '../prescription-medicaments/prescription-medicaments.component';

@Component({
  selector: 'app-ordonnance-hospitalisation-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, PrescriptionMedicamentsComponent],
  templateUrl: './ordonnance-hospitalisation-panel.component.html',
  styleUrls: ['./ordonnance-hospitalisation-panel.component.scss']
})
export class OrdonnanceHospitalisationPanelComponent implements OnInit {
  @Input() isOpen = false;
  @Input() hospitalisationId: number | null = null;
  @Input() patientNom = '';
  @Input() patientPrenom = '';

  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  @ViewChild(PrescriptionMedicamentsComponent) medicamentsComp!: PrescriptionMedicamentsComponent;

  medicaments: MedicamentPrescription[] = [];
  notes = '';
  dureeTraitement = '';

  isSubmitting = false;
  error: string | null = null;
  success = false;

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {}

  onMedicamentsChange(medicaments: MedicamentPrescription[]): void {
    this.medicaments = medicaments;
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
    this.medicaments = [];
    this.notes = '';
    this.dureeTraitement = '';
    this.error = null;
    this.success = false;
  }

  submit(): void {
    if (!this.hospitalisationId) {
      this.error = 'ID d\'hospitalisation manquant';
      return;
    }

    const medicamentsData = this.medicamentsComp?.getMedicamentsData() || this.medicaments;

    if (medicamentsData.length === 0) {
      this.error = 'Veuillez ajouter au moins un médicament';
      return;
    }

    this.isSubmitting = true;
    this.error = null;

    const payload = {
      medicaments: medicamentsData.map(m => ({
        nomMedicament: m.nomMedicament,
        dosage: m.dosage,
        posologie: m.posologie,
        formePharmaceutique: m.formePharmaceutique,
        voieAdministration: m.voieAdministration,
        duree: m.dureeTraitement,
        instructions: m.instructions,
        quantite: m.quantite
      })),
      notes: this.notes || undefined,
      dureeTraitement: this.dureeTraitement || undefined
    };

    this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/medecin/hospitalisation/${this.hospitalisationId}/ordonnance`,
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
        console.error('Erreur ordonnance hospitalisation:', err);
        this.error = err.error?.message || 'Erreur lors de l\'enregistrement de l\'ordonnance';
        this.isSubmitting = false;
      }
    });
  }

  get canSubmit(): boolean {
    return this.medicaments.length > 0 && !this.isSubmitting;
  }
}
