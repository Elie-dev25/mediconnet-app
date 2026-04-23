import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { PrescriptionService, CreateOrdonnanceDirecteRequest } from '../../../services/prescription.service';
import { PrescriptionMedicamentsComponent, MedicamentPrescription } from '../prescription-medicaments/prescription-medicaments.component';

@Component({
  selector: 'app-ordonnance-directe-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, PrescriptionMedicamentsComponent],
  templateUrl: './ordonnance-directe-panel.component.html',
  styleUrls: ['./ordonnance-directe-panel.component.scss']
})
export class OrdonnanceDirectePanelComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() patientId: number | null = null;
  @Input() patientNom = '';
  @Input() patientPrenom = '';

  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  @ViewChild(PrescriptionMedicamentsComponent) medicamentsComp!: PrescriptionMedicamentsComponent;

  medicaments: MedicamentPrescription[] = [];
  notes = '';
  dureeValiditeJours = 90;
  renouvelable = false;
  nombreRenouvellements: number | null = null;

  isSubmitting = false;
  error: string | null = null;
  success = false;

  constructor(private prescriptionService: PrescriptionService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      this.resetForm();
    }
  }

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
    this.dureeValiditeJours = 90;
    this.renouvelable = false;
    this.nombreRenouvellements = null;
    this.error = null;
    this.success = false;
  }

  submit(): void {
    if (!this.patientId) {
      this.error = 'ID du patient manquant';
      return;
    }

    const medicamentsData = this.medicamentsComp?.getMedicamentsData() || this.medicaments;

    if (medicamentsData.length === 0) {
      this.error = 'Veuillez ajouter au moins un médicament';
      return;
    }

    this.isSubmitting = true;
    this.error = null;

    const request: CreateOrdonnanceDirecteRequest = {
      idPatient: this.patientId,
      medicaments: medicamentsData.map(m => ({
        nomMedicament: m.nomMedicament,
        dosage: m.dosage,
        quantite: m.quantite || 1,
        posologie: m.posologie,
        dureeTraitement: m.dureeTraitement,
        voieAdministration: m.voieAdministration,
        formePharmaceutique: m.formePharmaceutique,
        instructions: m.instructions
      })),
      notes: this.notes || undefined
    };

    this.prescriptionService.creerOrdonnanceDirecte(request).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = true;
          this.isSubmitting = false;
          setTimeout(() => {
            this.saved.emit();
            this.onClose();
          }, 1500);
        } else {
          this.error = response.message || 'Erreur lors de la création de l\'ordonnance';
          this.isSubmitting = false;
        }
      },
      error: (err) => {
        console.error('Erreur création ordonnance:', err);
        this.error = err.error?.message || 'Erreur serveur lors de la création de l\'ordonnance';
        this.isSubmitting = false;
      }
    });
  }
}
