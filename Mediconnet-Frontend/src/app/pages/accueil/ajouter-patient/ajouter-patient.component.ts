import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { finalize } from 'rxjs/operators';
import { DashboardLayoutComponent, PhoneInputComponent, ALL_ICONS_PROVIDER } from '../../../shared';
import { ACCUEIL_MENU_ITEMS, ACCUEIL_SIDEBAR_TITLE } from '../shared';
import { ReceptionPatientService, CreatePatientByReceptionRequest, CreatePatientByReceptionResponse } from '../../../services/reception-patient.service';
import { AssuranceService, AssuranceListItem } from '../../../services/assurance.service';

@Component({
  selector: 'app-ajouter-patient',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    LucideAngularModule,
    DashboardLayoutComponent,
    PhoneInputComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './ajouter-patient.component.html',
  styleUrls: ['./ajouter-patient.component.scss']
})
export class AjouterPatientComponent implements OnInit {
  menuItems = ACCUEIL_MENU_ITEMS;
  sidebarTitle = ACCUEIL_SIDEBAR_TITLE;

  // Stepper
  currentStep = 1;
  totalSteps = 5;

  // Formulaires
  personalInfoForm!: FormGroup;
  medicalInfoForm!: FormGroup;
  lifestyleForm!: FormGroup;
  emergencyForm!: FormGroup;
  assuranceForm!: FormGroup;

  // Assurance
  assurances: AssuranceListItem[] = [];
  isLoadingAssurances = false;

  // États
  isSubmitting = false;
  errorMessage: string | null = null;
  successResponse: CreatePatientByReceptionResponse | null = null;

  // Options
  situationsMatrimoniales = ['Célibataire', 'Marié(e)', 'Divorcé(e)', 'Veuf/Veuve', 'Union libre'];
  groupesSanguins = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'];
  frequencesAlcool = ['Jamais', 'Occasionnellement', 'Régulièrement', 'Quotidiennement'];
  maladiesChroniquesOptions = [
    'Diabète', 'Hypertension', 'Asthme', 'Cardiopathie', 
    'Insuffisance rénale', 'VIH/SIDA', 'Hépatite', 'Cancer', 'Autre'
  ];

  // Sélections multiples
  selectedMaladies: string[] = [];

  constructor(
    private formBuilder: FormBuilder,
    private receptionPatientService: ReceptionPatientService,
    private assuranceService: AssuranceService
  ) {}

  ngOnInit(): void {
    this.initForms();
    this.loadAssurances();
  }

  loadAssurances(): void {
    this.isLoadingAssurances = true;
    this.assuranceService.getAssurancesActives().pipe(
      finalize(() => {
        this.isLoadingAssurances = false;
      })
    ).subscribe({
      next: (assurances) => {
        this.assurances = assurances;
      },
      error: (err) => {
        console.error('Erreur chargement assurances:', err);
      }
    });
  }

  private initForms(): void {
    // Étape 1: Informations personnelles
    this.personalInfoForm = this.formBuilder.group({
      nom: ['', [Validators.required, Validators.minLength(2)]],
      prenom: ['', [Validators.required, Validators.minLength(2)]],
      dateNaissance: ['', Validators.required],
      sexe: ['', Validators.required],
      telephone: ['', Validators.required],
      email: ['', Validators.email],
      adresse: ['', Validators.required],
      nationalite: ['Cameroun'],
      regionOrigine: [''],
      ethnie: [''],
      profession: [''],
      situationMatrimoniale: ['']
    });

    // Étape 2: Informations médicales
    this.medicalInfoForm = this.formBuilder.group({
      groupeSanguin: [''],
      maladiesChroniques: [''],
      operationsChirurgicales: [false],
      operationsDetails: [''],
      allergiesConnues: [false],
      allergiesDetails: [''],
      antecedentsFamiliaux: [false],
      antecedentsFamiliauxDetails: ['']
    });

    // Étape 3: Habitudes de vie
    this.lifestyleForm = this.formBuilder.group({
      consommationAlcool: [false],
      frequenceAlcool: [''],
      tabagisme: [false],
      activitePhysique: [false]
    });

    // Étape 4: Contacts d'urgence
    this.emergencyForm = this.formBuilder.group({
      nbEnfants: [null],
      personneContact: [''],
      numeroContact: ['']
    });

    // Étape 5: Assurance
    this.assuranceForm = this.formBuilder.group({
      estAssure: [false],
      assuranceId: [''],
      numeroCarteAssurance: [''],
      dateDebutValidite: [''],
      dateFinValidite: [''],
      couvertureAssurance: ['']
    });

    // Écouter le changement du statut assuré
    this.assuranceForm.get('estAssure')?.valueChanges.subscribe(estAssure => {
      if (estAssure) {
        this.assuranceForm.get('assuranceId')?.setValidators([Validators.required]);
      } else {
        this.assuranceForm.get('assuranceId')?.clearValidators();
        this.assuranceForm.patchValue({
          assuranceId: '',
          numeroCarteAssurance: '',
          dateDebutValidite: '',
          dateFinValidite: '',
          couvertureAssurance: ''
        });
      }
      this.assuranceForm.get('assuranceId')?.updateValueAndValidity();
    });
  }

  // ============================================
  // Navigation Stepper
  // ============================================

  nextStep(): void {
    if (this.isCurrentStepValid()) {
      if (this.currentStep < this.totalSteps) {
        this.currentStep++;
        this.errorMessage = null;
      }
    } else {
      this.errorMessage = 'Veuillez remplir tous les champs obligatoires';
    }
  }

  previousStep(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
      this.errorMessage = null;
    }
  }

  isCurrentStepValid(): boolean {
    switch (this.currentStep) {
      case 1: return this.personalInfoForm.valid;
      case 2: return true; // Optionnel
      case 3: return true; // Optionnel
      case 4: return true; // Optionnel
      case 5: // Assurance: si assuré, doit avoir sélectionné une assurance
        const estAssure = this.assuranceForm.get('estAssure')?.value;
        if (estAssure) {
          return !!this.assuranceForm.get('assuranceId')?.value;
        }
        return true;
      default: return false;
    }
  }

  getSelectedAssuranceName(): string {
    const id = this.assuranceForm.get('assuranceId')?.value;
    if (!id) return '';
    const assurance = this.assurances.find(a => a.idAssurance === Number(id));
    return assurance?.nom || '';
  }

  // ============================================
  // Gestion des maladies chroniques
  // ============================================

  toggleMaladie(maladie: string): void {
    const index = this.selectedMaladies.indexOf(maladie);
    if (index > -1) {
      this.selectedMaladies.splice(index, 1);
    } else {
      this.selectedMaladies.push(maladie);
    }
    this.medicalInfoForm.patchValue({
      maladiesChroniques: this.selectedMaladies.join(', ')
    });
  }

  isMaladieSelected(maladie: string): boolean {
    return this.selectedMaladies.includes(maladie);
  }

  // ============================================
  // Soumission
  // ============================================

  submitForm(): void {
    if (this.isSubmitting) {
      return;
    }

    if (!this.personalInfoForm.valid) {
      this.errorMessage = 'Les informations personnelles sont incomplètes';
      this.currentStep = 1;
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = null;

    const assuranceData = this.assuranceForm.value;
    const request: CreatePatientByReceptionRequest = {
      // Infos personnelles
      ...this.personalInfoForm.value,
      // Infos médicales
      ...this.medicalInfoForm.value,
      // Habitudes
      ...this.lifestyleForm.value,
      // Contacts
      ...this.emergencyForm.value,
      // Assurance (si assuré)
      ...(assuranceData.estAssure ? {
        assuranceId: Number(assuranceData.assuranceId),
        numeroCarteAssurance: assuranceData.numeroCarteAssurance || undefined,
        dateDebutValidite: assuranceData.dateDebutValidite || undefined,
        dateFinValidite: assuranceData.dateFinValidite || undefined,
        couvertureAssurance: assuranceData.couvertureAssurance ? Number(assuranceData.couvertureAssurance) : undefined
      } : {})
    };

    this.receptionPatientService.createPatient(request).pipe(
      finalize(() => {
        this.isSubmitting = false;
      })
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.successResponse = response;
        } else {
          this.errorMessage = response.message || 'Erreur lors de la création du patient';
        }
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Une erreur est survenue';
        console.error('Erreur création patient:', err);
      }
    });
  }

  // ============================================
  // Actions post-succès
  // ============================================

  resetForm(): void {
    this.successResponse = null;
    this.currentStep = 1;
    this.selectedMaladies = [];
    this.initForms();
    this.errorMessage = null;
  }

  // ============================================
  // Helpers
  // ============================================

  get stepTitle(): string {
    switch (this.currentStep) {
      case 1: return 'Informations personnelles';
      case 2: return 'Informations médicales';
      case 3: return 'Habitudes de vie';
      case 4: return 'Contacts d\'urgence';
      default: return '';
    }
  }

  get stepIcon(): string {
    switch (this.currentStep) {
      case 1: return 'user';
      case 2: return 'heart-pulse';
      case 3: return 'activity';
      case 4: return 'phone';
      default: return 'circle';
    }
  }

  get maxDate(): string {
    const today = new Date();
    return today.toISOString().split('T')[0];
  }

  get patientFullName(): string {
    const nom = this.personalInfoForm?.get('nom')?.value || '';
    const prenom = this.personalInfoForm?.get('prenom')?.value || '';
    return `${prenom} ${nom}`.trim();
  }
}
