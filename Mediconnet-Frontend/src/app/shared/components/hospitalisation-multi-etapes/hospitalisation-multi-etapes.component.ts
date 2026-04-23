import { Component, Input, Output, EventEmitter, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { PrescriptionMedicamentsComponent, MedicamentPrescription } from '../prescription-medicaments/prescription-medicaments.component';
import { PrescriptionExamensComponent, ExamenPrescription } from '../prescription-examens/prescription-examens.component';
import { SoinsComplementairesComponent, SoinComplementaire } from '../soins-complementaires/soins-complementaires.component';
import { HospitalisationService, OrdonnerHospitalisationCompleteRequest } from '../../../services/hospitalisation.service';
import { ConsultationCompleteService, MedicamentDto, ExamenPrescritDto } from '../../../services/consultation-complete.service';
import { AuthService } from '../../../services/auth.service';

type EtapeHospitalisation = 'hospitalisation' | 'examens' | 'medicaments';

export interface HospitalisationPatientInfo {
  idPatient: number;
  nom: string;
  prenom: string;
  numeroDossier?: string;
}

@Component({
  selector: 'app-hospitalisation-multi-etapes',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    LucideAngularModule,
    PrescriptionMedicamentsComponent,
    PrescriptionExamensComponent,
    SoinsComplementairesComponent
  ],
  templateUrl: './hospitalisation-multi-etapes.component.html',
  styleUrl: './hospitalisation-multi-etapes.component.scss'
})
export class HospitalisationMultiEtapesComponent implements OnInit {
  @Input() patient!: HospitalisationPatientInfo;
  @Input() idConsultation?: number;
  @Output() completed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  @ViewChild(PrescriptionMedicamentsComponent) medicamentsComp!: PrescriptionMedicamentsComponent;
  @ViewChild(PrescriptionExamensComponent) examensComp!: PrescriptionExamensComponent;
  @ViewChild(SoinsComplementairesComponent) soinsComp!: SoinsComplementairesComponent;

  etapeActuelle: EtapeHospitalisation = 'hospitalisation';
  etapes: { key: EtapeHospitalisation; label: string; icon: string }[] = [
    { key: 'hospitalisation', label: 'Hospitalisation', icon: 'bed' },
    { key: 'examens', label: 'Examens', icon: 'test-tube' },
    { key: 'medicaments', label: 'Médicaments', icon: 'pill' }
  ];

  // Données étape 1 - Hospitalisation
  motif = '';
  urgence = 'normale';
  diagnosticPrincipal = '';
  notes = '';
  dateSortiePrevue = '';
  soinsComplementaires: SoinComplementaire[] = [];

  // Données étape 2 - Examens
  examens: ExamenPrescription[] = [];
  examensConsultation: ExamenPrescritDto[] = []; // Examens déjà prescrits lors de la consultation

  // Données étape 3 - Médicaments
  medicaments: MedicamentPrescription[] = [];
  medicamentsConsultation: MedicamentDto[] = []; // Médicaments déjà prescrits lors de la consultation

  // État
  isSubmitting = false;
  error: string | null = null;
  success = false;

  // Titre affiché de l'utilisateur (pour filtrer les examens par spécialité)
  userTitreAffiche: string = '';

  constructor(
    private hospitalisationService: HospitalisationService,
    private consultationService: ConsultationCompleteService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadUserTitreAffiche();
    if (this.idConsultation) {
      this.loadConsultationData();
    }
  }

  private loadUserTitreAffiche(): void {
    const user = this.authService.getCurrentUser();
    this.userTitreAffiche = user?.titreAffiche || '';
  }

  /**
   * Charger les données de la consultation pour préremplir le diagnostic
   * et afficher les prescriptions existantes
   */
  private loadConsultationData(): void {
    if (!this.idConsultation) return;

    this.consultationService.getConsultation(this.idConsultation).subscribe({
      next: (consultation) => {
        // Préremplir le diagnostic principal depuis la consultation
        if (consultation.diagnostic?.diagnosticPrincipal) {
          this.diagnosticPrincipal = consultation.diagnostic.diagnosticPrincipal;
        }

        // Récupérer les examens déjà prescrits
        if (consultation.planTraitement?.examensPrescrits) {
          this.examensConsultation = consultation.planTraitement.examensPrescrits;
        }

        // Récupérer les médicaments déjà prescrits
        if (consultation.planTraitement?.ordonnance?.medicaments) {
          this.medicamentsConsultation = consultation.planTraitement.ordonnance.medicaments;
        } else if (consultation.prescriptions?.ordonnance?.medicaments) {
          this.medicamentsConsultation = consultation.prescriptions.ordonnance.medicaments;
        }
      },
      error: (err) => console.error('Erreur chargement consultation:', err)
    });
  }

  get etapeIndex(): number {
    return this.etapes.findIndex(e => e.key === this.etapeActuelle);
  }

  get isFirstStep(): boolean {
    return this.etapeIndex === 0;
  }

  get isLastStep(): boolean {
    return this.etapeIndex === this.etapes.length - 1;
  }

  get canProceed(): boolean {
    switch (this.etapeActuelle) {
      case 'hospitalisation':
        return !!this.motif?.trim();
      case 'examens':
      case 'medicaments':
        return true;
      default:
        return false;
    }
  }

  getMinDateSortie(): string {
    const today = new Date();
    return today.toISOString().split('T')[0];
  }

  goToEtape(etape: EtapeHospitalisation): void {
    const targetIndex = this.etapes.findIndex(e => e.key === etape);
    if (targetIndex <= this.etapeIndex || this.canProceed) {
      this.etapeActuelle = etape;
    }
  }

  previousStep(): void {
    if (!this.isFirstStep) {
      const prevIndex = this.etapeIndex - 1;
      this.etapeActuelle = this.etapes[prevIndex].key;
    }
  }

  nextStep(): void {
    if (this.canProceed && !this.isLastStep) {
      const nextIndex = this.etapeIndex + 1;
      this.etapeActuelle = this.etapes[nextIndex].key;
    }
  }

  onSoinsChange(soins: SoinComplementaire[]): void {
    this.soinsComplementaires = soins;
  }

  onExamensChange(examens: ExamenPrescription[]): void {
    this.examens = examens;
  }

  onMedicamentsChange(medicaments: MedicamentPrescription[]): void {
    this.medicaments = medicaments;
  }

  submit(): void {
    if (!this.canProceed || this.isSubmitting) return;

    this.isSubmitting = true;
    this.error = null;

    // Récupérer les données des composants enfants
    const soinsData = this.soinsComp?.getSoinsData() || this.soinsComplementaires;
    const examensData = this.examensComp?.getExamensData() || this.examens;
    const medicamentsData = this.medicamentsComp?.getMedicamentsData() || this.medicaments;

    const request: OrdonnerHospitalisationCompleteRequest = {
      idPatient: this.patient.idPatient,
      idConsultation: this.idConsultation,
      motif: this.motif,
      urgence: this.urgence,
      diagnosticPrincipal: this.diagnosticPrincipal || undefined,
      soinsComplementaires: soinsData.length > 0 ? soinsData : undefined,
      notes: this.notes || undefined,
      dateSortiePrevue: this.dateSortiePrevue || undefined,
      examens: examensData.length > 0 ? examensData : undefined,
      medicaments: medicamentsData.length > 0 ? medicamentsData : undefined
    };

    this.hospitalisationService.ordonnerHospitalisationComplete(request).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = true;
          setTimeout(() => {
            this.completed.emit();
          }, 2000);
        } else {
          this.error = response.message || 'Erreur lors de la création';
        }
        this.isSubmitting = false;
      },
      error: (err) => {
        console.error('Erreur hospitalisation:', err);
        this.error = err.error?.message || 'Erreur lors de la création de l\'hospitalisation';
        this.isSubmitting = false;
      }
    });
  }

  cancel(): void {
    this.cancelled.emit();
  }

  isEtapeCompleted(etape: EtapeHospitalisation): boolean {
    const targetIndex = this.etapes.findIndex(e => e.key === etape);
    return targetIndex < this.etapeIndex;
  }

  isEtapeActive(etape: EtapeHospitalisation): boolean {
    return etape === this.etapeActuelle;
  }
}
