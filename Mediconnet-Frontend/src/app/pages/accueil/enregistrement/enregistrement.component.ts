import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { LucideAngularModule, DashboardLayoutComponent, PatientSearchComponent, ALL_ICONS_PROVIDER } from '../../../shared';
import { ACCUEIL_MENU_ITEMS, ACCUEIL_SIDEBAR_TITLE } from '../shared';
import { PatientBasicInfo } from '../../../services/patient.service';
import { 
  ConsultationService, 
  ServiceHospitalier, 
  MedecinAvecDisponibilite,
  MedecinsDisponibiliteResponse,
  VerifierPaiementResponse,
  CreneauJourDto,
  CreneauxMedecinJourResponse
} from '../../../services/consultation.service';

interface Step {
  id: number;
  title: string;
  icon: string;
  completed: boolean;
}

@Component({
  selector: 'app-accueil-enregistrement',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    LucideAngularModule,
    DashboardLayoutComponent,
    PatientSearchComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './enregistrement.component.html',
  styleUrls: ['./enregistrement.component.scss']
})
export class EnregistrementComponent implements OnInit {
  @ViewChild(PatientSearchComponent) patientSearchComponent!: PatientSearchComponent;

  menuItems = ACCUEIL_MENU_ITEMS;
  sidebarTitle = ACCUEIL_SIDEBAR_TITLE;

  // Stepper (sans étape Assurance - gérée à la création du patient)
  currentStep = 1;
  steps: Step[] = [
    { id: 1, title: 'Patient', icon: 'user-search', completed: false },
    { id: 2, title: 'Service', icon: 'building-2', completed: false },
    { id: 3, title: 'Médecin', icon: 'stethoscope', completed: false },
    { id: 4, title: 'Confirmation', icon: 'clipboard-check', completed: false }
  ];

  // Données
  selectedPatient: PatientBasicInfo | null = null;
  services: ServiceHospitalier[] = [];
  medecinsDisponibilite: MedecinAvecDisponibilite[] = [];
  selectedMedecin: MedecinAvecDisponibilite | null = null;
  
  // Formulaires
  serviceForm!: FormGroup;
  consultationForm!: FormGroup;
  
  // États de chargement
  isLoadingServices = false;
  isLoadingMedecins = false;
  isSubmitting = false;
  
  // Stats disponibilité
  totalDisponibles = 0;
  totalOccupes = 0;
  totalAbsents = 0;
  
  successMessage: string | null = null;
  errorMessage: string | null = null;

  // Vérification paiement valide (règle 14 jours)
  paiementValide = false;
  paiementInfo: VerifierPaiementResponse | null = null;
  isCheckingPaiement = false;

  // Créneaux du médecin
  creneauxMedecin: CreneauJourDto[] = [];
  selectedCreneau: CreneauJourDto | null = null;
  isLoadingCreneaux = false;
  coutConsultation = 0;

  // Modal de confirmation
  showSuccessModal = false;
  confirmationData: {
    patientNom: string;
    patientPrenom: string;
    medecinNom: string;
    medecinPrenom: string;
    service: string;
    motif: string;
    prix: number;
    numeroPaiement: string;
  } | null = null;

  constructor(
    private fb: FormBuilder,
    private consultationService: ConsultationService
  ) {}

  ngOnInit(): void {
    this.initForms();
    this.loadServices();
  }

  private initForms(): void {
    this.serviceForm = this.fb.group({
      idService: ['']
    });

    this.consultationForm = this.fb.group({
      motif: ['', [Validators.required, Validators.minLength(3)]],
      prixConsultation: [0, [Validators.min(0)]]
    });

    // Écouter les changements pour recharger les médecins
    this.serviceForm.valueChanges.subscribe(() => {
      if (this.currentStep === 3) {
        this.loadMedecinsDisponibilite();
      }
    });
  }

  loadServices(): void {
    this.isLoadingServices = true;
    this.consultationService.getServices().pipe(
      finalize(() => {
        this.isLoadingServices = false;
      })
    ).subscribe({
      next: (services) => {
        this.services = services;
      },
      error: (err) => {
        console.error('Erreur chargement services:', err);
      }
    });
  }


  loadMedecinsDisponibilite(): void {
    this.isLoadingMedecins = true;
    this.selectedMedecin = null;
    
    const idService = this.serviceForm.get('idService')?.value;
    
    this.consultationService.getMedecinsAvecDisponibilite(
      idService ? Number(idService) : undefined
    ).pipe(
      finalize(() => {
        this.isLoadingMedecins = false;
      })
    ).subscribe({
      next: (response) => {
        this.medecinsDisponibilite = response.medecins;
        this.totalDisponibles = response.totalDisponibles;
        this.totalOccupes = response.totalOccupes;
        this.totalAbsents = response.totalAbsents;
      },
      error: (err) => {
        console.error('Erreur chargement médecins:', err);
        this.errorMessage = 'Impossible de charger la liste des médecins';
      }
    });
  }

  // Navigation
  onPatientSelected(patient: PatientBasicInfo): void {
    this.selectedPatient = patient;
    this.steps[0].completed = true;
    this.successMessage = null;
    this.errorMessage = null;
    // Passer automatiquement à l'étape suivante
    setTimeout(() => this.nextStep(), 300);
  }

  nextStep(): void {
    if (this.canProceed()) {
      this.steps[this.currentStep - 1].completed = true;
      this.currentStep++;
      
      // Charger les médecins quand on arrive à l'étape 3 (Médecin)
      if (this.currentStep === 3) {
        this.loadMedecinsDisponibilite();
      }
    }
  }

  prevStep(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
    }
  }

  goToStep(stepId: number): void {
    // On peut aller à une étape précédente ou à l'étape suivante si les conditions sont remplies
    if (stepId < this.currentStep) {
      this.currentStep = stepId;
    } else if (stepId === this.currentStep + 1 && this.canProceed()) {
      this.nextStep();
    }
  }

  canProceed(): boolean {
    switch (this.currentStep) {
      case 1:
        return this.selectedPatient !== null;
      case 2:
        return true; // Service et spécialité sont optionnels
      case 3:
        return this.selectedMedecin !== null && this.selectedCreneau !== null;
      case 4:
        // Si paiement valide, seul le motif est requis
        if (this.paiementValide) {
          return this.consultationForm.get('motif')?.valid ?? false;
        }
        // Sinon, le formulaire complet doit être valide avec un prix > 0
        const prix = this.consultationForm.get('prixConsultation')?.value;
        return this.consultationForm.valid && prix > 0;
      default:
        return false;
    }
  }

  selectMedecin(medecin: MedecinAvecDisponibilite): void {
    if (!medecin.estDisponible) {
      return; // Ne pas permettre la sélection d'un médecin non disponible
    }
    this.selectedMedecin = medecin;
    this.selectedCreneau = null;
    this.steps[3].completed = false; // Compléter seulement après sélection du créneau
    
    // Charger les créneaux du médecin
    this.loadCreneauxMedecin();
  }

  loadCreneauxMedecin(): void {
    if (!this.selectedMedecin) return;
    
    this.isLoadingCreneaux = true;
    this.creneauxMedecin = [];
    
    this.consultationService.getCreneauxMedecinJour(this.selectedMedecin.idMedecin)
      .pipe(finalize(() => this.isLoadingCreneaux = false))
      .subscribe({
        next: (response) => {
          this.creneauxMedecin = response.creneaux;
          this.coutConsultation = response.coutConsultation;
          // Pré-remplir le prix de consultation
          this.consultationForm.patchValue({ prixConsultation: response.coutConsultation });
        },
        error: (err) => {
          console.error('Erreur chargement créneaux:', err);
          this.errorMessage = 'Impossible de charger les créneaux du médecin';
        }
      });
  }

  selectCreneau(creneau: CreneauJourDto): void {
    if (!creneau.selectionnable) return;
    this.selectedCreneau = creneau;
    this.steps[2].completed = true;
    
    // Vérifier si le paiement est encore valide pour ce médecin
    this.verifierPaiement();
  }

  verifierPaiement(): void {
    if (!this.selectedPatient || !this.selectedMedecin) {
      return;
    }

    this.isCheckingPaiement = true;
    this.consultationService.verifierPaiementValide(
      this.selectedPatient.idUser,
      this.selectedMedecin.idMedecin
    ).pipe(
      finalize(() => this.isCheckingPaiement = false)
    ).subscribe({
      next: (response) => {
        this.paiementValide = response.paiementValide;
        this.paiementInfo = response;
        
        // Si paiement valide, mettre le prix à 0 automatiquement
        if (response.paiementValide) {
          this.consultationForm.patchValue({ prixConsultation: 0 });
        }
      },
      error: (err) => {
        console.error('Erreur vérification paiement:', err);
        this.paiementValide = false;
        this.paiementInfo = null;
      }
    });
  }

  confirmMedecinAndProceed(): void {
    if (this.selectedMedecin) {
      this.nextStep();
    }
  }

  clearPatient(): void {
    this.selectedPatient = null;
    this.selectedMedecin = null;
    this.selectedCreneau = null;
    this.creneauxMedecin = [];
    this.coutConsultation = 0;
    this.currentStep = 1;
    this.steps.forEach(s => s.completed = false);
    this.serviceForm.reset();
    this.consultationForm.reset();
    this.successMessage = null;
    this.errorMessage = null;
    this.paiementValide = false;
    this.paiementInfo = null;
    
    if (this.patientSearchComponent) {
      this.patientSearchComponent.clearSearch();
    }
  }

  getSelectedServiceName(): string {
    const id = this.serviceForm.get('idService')?.value;
    const service = this.services.find(s => s.idService === Number(id));
    return service?.nomService || 'Tous les services';
  }


  submitConsultation(): void {
    if (!this.selectedPatient || !this.selectedMedecin || this.consultationForm.invalid) {
      return;
    }

    if (this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = null;
    this.successMessage = null;

    const formData = this.consultationForm.value;
    const request = {
      idPatient: this.selectedPatient.idUser,
      motif: formData.motif,
      idMedecin: this.selectedMedecin.idMedecin,
      prixConsultation: Number(formData.prixConsultation),
      dateHeureCreneau: this.selectedCreneau?.dateHeure
    };

    this.consultationService.enregistrerConsultation(request).pipe(
      finalize(() => {
        this.isSubmitting = false;
      })
    ).subscribe({
      next: (response) => {
        if (response.success) {
          // Préparer les données pour le modal de confirmation
          this.confirmationData = {
            patientNom: this.selectedPatient!.nom,
            patientPrenom: this.selectedPatient!.prenom,
            medecinNom: this.selectedMedecin!.nom,
            medecinPrenom: this.selectedMedecin!.prenom,
            service: this.selectedMedecin!.service || 'Non spécifié',
            motif: formData.motif,
            prix: Number(formData.prixConsultation),
            numeroPaiement: response.numeroPaiement
          };
          
          // Afficher le modal de confirmation
          this.showSuccessModal = true;
        } else {
          this.errorMessage = response.message || 'Erreur lors de l\'enregistrement';
        }
      },
      error: (err) => {
        console.error('Erreur enregistrement consultation:', err);
        
        if (err.status === 0) {
          this.errorMessage = 'Impossible de contacter le serveur. Vérifiez votre connexion.';
        } else if (err.status === 401) {
          this.errorMessage = 'Vous n\'êtes pas authentifié. Veuillez vous reconnecter.';
        } else if (err.status === 400) {
          this.errorMessage = err.error?.message || 'Données invalides. Vérifiez les informations saisies.';
        } else if (err.status === 500) {
          this.errorMessage = 'Erreur serveur. Veuillez réessayer plus tard.';
        } else {
          this.errorMessage = err.error?.message || 'Une erreur est survenue lors de l\'enregistrement';
        }
      }
    });
  }

  formatTempsAttente(minutes?: number): string {
    if (!minutes) return 'Immédiat';
    if (minutes < 60) return `~${minutes} min`;
    const heures = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return `~${heures}h${mins > 0 ? mins : ''}`;
  }

  closeSuccessModal(): void {
    this.showSuccessModal = false;
    this.confirmationData = null;
    this.clearPatient();
  }

  printConfirmation(): void {
    window.print();
  }
}
