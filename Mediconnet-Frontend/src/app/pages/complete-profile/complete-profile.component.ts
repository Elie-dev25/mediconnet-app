/**
 * @deprecated Ce composant est obsolète depuis que le profil est complété lors de l'inscription.
 * La route /complete-profile redirige maintenant vers /register dans app.routes.ts.
 * Ce fichier est conservé pour référence mais ne doit plus être utilisé.
 * 
 * Pour les patients créés par l'accueil, utiliser le flux first-login (/auth/first-login).
 */
import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { ALL_ICONS_PROVIDER } from '../../shared/icons';
import { ProfileService, ProfileFormOptions, CompleteProfileRequest } from '../../services/profile.service';
import { AuthService } from '../../services/auth.service';
import { trigger, transition, style, animate } from '@angular/animations';

/** @deprecated */
interface Step {
  id: number;
  title: string;
  icon: string;
  completed: boolean;
}

@Component({
  selector: 'app-complete-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './complete-profile.component.html',
  styleUrl: './complete-profile.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [
    trigger('slideIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateX(30px)' }),
        animate('250ms ease-out', style({ opacity: 1, transform: 'translateX(0)' }))
      ])
    ]),
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('200ms ease-out', style({ opacity: 1 }))
      ])
    ])
  ]
})
export class CompleteProfileComponent implements OnInit {
  currentStep = 1;
  totalSteps = 5;
  isLoading = false;
  isSubmitting = false;
  errorMessage: string | null = null;

  steps: Step[] = [
    { id: 1, title: 'Informations personnelles', icon: 'user', completed: false },
    { id: 2, title: 'Informations médicales', icon: 'heart-pulse', completed: false },
    { id: 3, title: 'Habitudes de vie', icon: 'activity', completed: false },
    { id: 4, title: 'Contacts d\'urgence', icon: 'phone', completed: false },
    { id: 5, title: 'Déclaration sur l\'honneur', icon: 'shield-check', completed: false }
  ];

  // Formulaires pour chaque étape
  personalInfoForm!: FormGroup;
  medicalInfoForm!: FormGroup;
  lifestyleInfoForm!: FormGroup;
  emergencyContactForm!: FormGroup;
  declarationHonneurForm!: FormGroup;

  // Options des formulaires
  formOptions: ProfileFormOptions = {
    regions: [],
    groupesSanguins: [],
    situationsMatrimoniales: [],
    maladiesChroniquesOptions: [],
    frequencesAlcool: []
  };

  userName = '';

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private profileService: ProfileService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {
    this.initializeForms();
  }

  ngOnInit(): void {
    // Vérifier que l'utilisateur est bien un patient auto-inscrit
    const user = this.authService.getCurrentUser();
    if (!user || user.role !== 'patient') {
      this.router.navigate(['/login']);
      return;
    }

    // Ce composant est uniquement pour les patients auto-inscrits
    // (profileCompleted = false)
    if (user.profileCompleted === true) {
      // Si le profil est déjà complété, vérifier s'il doit changer son mot de passe
      if (user.mustChangePassword === true) {
        // Patient créé à l'accueil → rediriger vers first-login
        this.router.navigate(['/auth/first-login']);
      } else {
        // Profil complété et pas de changement requis → dashboard
        this.router.navigate(['/patient/dashboard']);
      }
      return;
    }

    this.loadFormOptions();
    this.loadUserInfo();
  }

  private initializeForms(): void {
    // Étape 1: Informations personnelles
    this.personalInfoForm = this.fb.group({
      dateNaissance: ['', Validators.required],
      nationalite: ['Cameroun', Validators.required],
      regionOrigine: ['', Validators.required],
      regionOrigineAutre: [''],
      ethnie: [''],
      sexe: ['', Validators.required],
      situationMatrimoniale: [''],
      nbEnfants: [0, [Validators.min(0)]],
      adresse: ['']
    });

    // Étape 2: Informations médicales
    this.medicalInfoForm = this.fb.group({
      groupeSanguin: [''],
      profession: [''],
      maladiesChroniques: [[]],
      autreMaladieChronique: [''],
      operationsChirurgicales: [false],
      operationsDetails: [''],
      allergiesConnues: [false],
      allergiesDetails: [''],
      antecedentsFamiliaux: [false],
      antecedentsFamiliauxDetails: ['']
    });

    // Étape 3: Habitudes de vie
    this.lifestyleInfoForm = this.fb.group({
      consommationAlcool: [false],
      frequenceAlcool: [''],
      tabagisme: [false],
      activitePhysique: [false]
    });

    // Étape 4: Contacts d'urgence
    this.emergencyContactForm = this.fb.group({
      personneContact: ['', Validators.required],
      numeroContact: ['', [Validators.required, Validators.pattern(/^[0-9+\s-]{9,15}$/)]]
    });

    // Étape 5: Déclaration sur l'honneur
    this.declarationHonneurForm = this.fb.group({
      acceptee: [false, Validators.requiredTrue]
    });
  }

  private loadFormOptions(): void {
    this.profileService.getFormOptions().subscribe({
      next: (options) => {
        this.formOptions = options;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Erreur chargement options:', err);
        // Options par défaut
        this.formOptions = {
          regions: ['Adamaoua', 'Centre', 'Est', 'Extrême-Nord', 'Littoral', 'Nord', 'Nord-Ouest', 'Ouest', 'Sud', 'Sud-Ouest', 'Autres'],
          groupesSanguins: ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'],
          situationsMatrimoniales: ['Célibataire', 'Marié(e)', 'Divorcé(e)', 'Veuf/Veuve', 'En couple'],
          maladiesChroniquesOptions: ['Diabète', 'Hypertension', 'Asthme', 'Insuffisance cardiaque', 'Épilepsie', 'VIH/SIDA', 'Drépanocytose', 'Aucune', 'Autres'],
          frequencesAlcool: ['Occasionnel', 'Régulier', 'Quotidien']
        };
        this.cdr.markForCheck();
      }
    });
  }

  private loadUserInfo(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.userName = `${user.prenom} ${user.nom}`;
    }
  }

  get currentForm(): FormGroup {
    switch (this.currentStep) {
      case 1: return this.personalInfoForm;
      case 2: return this.medicalInfoForm;
      case 3: return this.lifestyleInfoForm;
      case 5: return this.declarationHonneurForm;
      case 4: return this.emergencyContactForm;
      default: return this.personalInfoForm;
    }
  }

  get isCurrentStepValid(): boolean {
    return this.currentForm.valid;
  }

  get progressPercentage(): number {
    return ((this.currentStep - 1) / (this.totalSteps - 1)) * 100;
  }

  nextStep(): void {
    if (this.currentStep < this.totalSteps && this.isCurrentStepValid) {
      this.steps[this.currentStep - 1].completed = true;
      this.currentStep++;
      this.cdr.markForCheck();
    }
  }

  previousStep(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
      this.cdr.markForCheck();
    }
  }

  goToStep(stepId: number): void {
    // On peut aller à une étape précédente ou à la prochaine si l'actuelle est valide
    if (stepId < this.currentStep || (stepId === this.currentStep + 1 && this.isCurrentStepValid)) {
      if (stepId > this.currentStep) {
        this.steps[this.currentStep - 1].completed = true;
      }
      this.currentStep = stepId;
      this.cdr.markForCheck();
    }
  }

  toggleMaladieChronique(maladie: string): void {
    const current = this.medicalInfoForm.get('maladiesChroniques')?.value as string[];
    const index = current.indexOf(maladie);
    
    if (index === -1) {
      // Si on sélectionne "Aucune", on désélectionne tout le reste
      if (maladie === 'Aucune') {
        this.medicalInfoForm.patchValue({ maladiesChroniques: ['Aucune'] });
      } else {
        // Sinon, on retire "Aucune" si elle était sélectionnée
        const filtered = current.filter(m => m !== 'Aucune');
        this.medicalInfoForm.patchValue({ maladiesChroniques: [...filtered, maladie] });
      }
    } else {
      this.medicalInfoForm.patchValue({ 
        maladiesChroniques: current.filter(m => m !== maladie) 
      });
    }
    this.cdr.markForCheck();
  }

  isMaladieSelected(maladie: string): boolean {
    const current = this.medicalInfoForm.get('maladiesChroniques')?.value as string[];
    return current.includes(maladie);
  }

  submitProfile(): void {
    if (!this.isCurrentStepValid || this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = null;

    const personalInfo = this.personalInfoForm.value;
    const medicalInfo = this.medicalInfoForm.value;
    const lifestyleInfo = this.lifestyleInfoForm.value;
    const emergencyContact = this.emergencyContactForm.value;
    const declarationHonneur = this.declarationHonneurForm.value;

    // Gérer la région "Autres"
    const regionOrigine = personalInfo.regionOrigine === 'Autres' 
      ? personalInfo.regionOrigineAutre 
      : personalInfo.regionOrigine;

    const request: CompleteProfileRequest = {
      personalInfo: {
        dateNaissance: personalInfo.dateNaissance,
        nationalite: personalInfo.nationalite,
        regionOrigine: regionOrigine,
        ethnie: personalInfo.ethnie || undefined,
        sexe: personalInfo.sexe,
        situationMatrimoniale: personalInfo.situationMatrimoniale || undefined,
        nbEnfants: personalInfo.nbEnfants || undefined,
        adresse: personalInfo.adresse || undefined
      },
      medicalInfo: {
        groupeSanguin: medicalInfo.groupeSanguin || undefined,
        profession: medicalInfo.profession || undefined,
        maladiesChroniques: medicalInfo.maladiesChroniques || [],
        autreMaladieChronique: medicalInfo.autreMaladieChronique || undefined,
        operationsChirurgicales: medicalInfo.operationsChirurgicales,
        operationsDetails: medicalInfo.operationsDetails || undefined,
        allergiesConnues: medicalInfo.allergiesConnues,
        allergiesDetails: medicalInfo.allergiesDetails || undefined,
        antecedentsFamiliaux: medicalInfo.antecedentsFamiliaux,
        antecedentsFamiliauxDetails: medicalInfo.antecedentsFamiliauxDetails || undefined
      },
      lifestyleInfo: {
        consommationAlcool: lifestyleInfo.consommationAlcool,
        frequenceAlcool: lifestyleInfo.frequenceAlcool || undefined,
        tabagisme: lifestyleInfo.tabagisme,
        activitePhysique: lifestyleInfo.activitePhysique
      },
      emergencyContact: {
        personneContact: emergencyContact.personneContact,
        numeroContact: emergencyContact.numeroContact
      },
      declarationHonneur: {
        acceptee: declarationHonneur.acceptee
      }
    };

    this.profileService.completeProfile(request).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        if (response.success) {
          // Marquer toutes les étapes comme complétées
          this.steps.forEach(s => s.completed = true);
          
          // Mettre à jour le statut du profil et de la déclaration dans le service auth
          this.authService.updateUserInfo({ 
            profileCompleted: true, 
            declarationHonneurAcceptee: true 
          });
          
          this.cdr.markForCheck();
          
          // Rediriger vers le dashboard patient
          setTimeout(() => {
            this.router.navigate(['/patient']);
          }, 1000);
        } else {
          this.errorMessage = response.message || 'Une erreur est survenue';
          this.cdr.markForCheck();
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = err.error?.message || 'Une erreur est survenue lors de la sauvegarde';
        this.cdr.markForCheck();
      }
    });
  }

  get showRegionAutreInput(): boolean {
    return this.personalInfoForm.get('regionOrigine')?.value === 'Autres';
  }

  get showMaladieAutreInput(): boolean {
    const maladies = this.medicalInfoForm.get('maladiesChroniques')?.value as string[];
    return maladies?.includes('Autres') ?? false;
  }
}
