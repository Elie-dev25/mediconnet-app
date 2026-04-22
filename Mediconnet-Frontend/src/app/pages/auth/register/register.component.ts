import { 
  Component, 
  OnInit, 
  ChangeDetectionStrategy, 
  OnDestroy,
  ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { 
  FormBuilder, 
  FormGroup, 
  ReactiveFormsModule, 
  Validators,
  AbstractControl,
  ValidationErrors
} from '@angular/forms';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { trigger, transition, style, animate } from '@angular/animations';
import { AuthService } from '../../../services/auth.service';
import { AuthNavigationService } from '../../../services/auth-navigation.service';
import { PasswordStrengthIndicatorComponent } from '../../../shared/components/password-strength-indicator/password-strength-indicator.component';
import { PhoneInputComponent } from '../../../shared/components/phone-input/phone-input.component';
import { AuthLayoutWrapperComponent } from '../../../shared/components/auth-layout-wrapper/auth-layout-wrapper.component';
import { LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';

interface Step {
  id: number;
  title: string;
  icon: string;
}

function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password');
  const confirmPassword = control.get('confirmPassword');
  if (!password || !confirmPassword) return null;
  return password.value === confirmPassword.value ? null : { passwordMismatch: true };
}

/**
 * Validateur pour vérifier que le contact d'urgence est différent du contact principal
 */
function emergencyContactDifferentValidator(mainPhone: () => string): (control: AbstractControl) => ValidationErrors | null {
  return (control: AbstractControl): ValidationErrors | null => {
    const emergencyPhone = control.value;
    const mainPhoneValue = mainPhone();
    
    if (!emergencyPhone || !mainPhoneValue) return null;
    
    // Normaliser les numéros (enlever espaces, tirets, +)
    const normalizePhone = (phone: string) => phone.replaceAll(/[\s\-+]/g, '');
    
    if (normalizePhone(emergencyPhone) === normalizePhone(mainPhoneValue)) {
      return { sameAsMainContact: true };
    }
    return null;
  };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule, PasswordStrengthIndicatorComponent, PhoneInputComponent, AuthLayoutWrapperComponent],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [
    trigger('slideIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateX(20px)' }),
        animate('300ms ease-out', style({ opacity: 1, transform: 'translateX(0)' }))
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
export class RegisterComponent implements OnInit, OnDestroy {
  currentStep = 1;
  totalSteps = 5;
  isLoading = false;
  errorMessage: string | null = null;
  showPassword = false;
  showConfirmPassword = false;
  passwordValue = '';
  
  registrationSuccess = false;
  requiresEmailConfirmation = false;
  registeredEmail = '';
  
  steps: Step[] = [
    { id: 1, title: 'Compte', icon: 'user-plus' },
    { id: 2, title: 'Identité', icon: 'user' },
    { id: 3, title: 'Santé', icon: 'heart-pulse' },
    { id: 4, title: 'Habitudes', icon: 'activity' },
    { id: 5, title: 'Finalisation', icon: 'shield-check' }
  ];

  // Formulaires par étape
  accountForm!: FormGroup;
  identityForm!: FormGroup;
  healthForm!: FormGroup;
  lifestyleForm!: FormGroup;
  finalizationForm!: FormGroup;

  // Options
  regions = ['Adamaoua', 'Centre', 'Est', 'Extrême-Nord', 'Littoral', 'Nord', 'Nord-Ouest', 'Ouest', 'Sud', 'Sud-Ouest'];
  groupesSanguins = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-', 'Inconnu'];
  situationsMatrimoniales = ['Célibataire', 'Marié(e)', 'Divorcé(e)', 'Veuf/Veuve'];
  maladiesOptions = ['Diabète', 'Hypertension', 'Asthme', 'Insuffisance cardiaque', 'Épilepsie', 'Drépanocytose', 'Aucune'];
  frequencesAlcool = ['Jamais', 'Occasionnellement', 'Régulièrement', 'Quotidiennement'];
  
  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private authNavigation: AuthNavigationService,
    private cdr: ChangeDetectorRef
  ) {
    this.initializeForms();
  }

  ngOnInit(): void {}

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForms(): void {
    // Étape 1: Compte
    this.accountForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      telephone: ['', [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: passwordMatchValidator });

    // Étape 2: Identité (inclut nombre d'enfants et ethnie)
    this.identityForm = this.fb.group({
      dateNaissance: ['', Validators.required],
      sexe: ['', Validators.required],
      nationalite: ['Cameroun'],
      regionOrigine: [''],
      adresse: [''],
      situationMatrimoniale: [''],
      profession: [''],
      nbEnfants: [null],
      ethnie: ['']
    });

    // Étape 3: Santé
    this.healthForm = this.fb.group({
      groupeSanguin: [''],
      maladiesChroniques: [[]],
      operationsChirurgicales: [false],
      operationsDetails: [''],
      allergiesConnues: [false],
      allergiesDetails: [''],
      antecedentsFamiliaux: [false],
      antecedentsFamiliauxDetails: ['']
    });

    // Étape 4: Habitudes de vie
    this.lifestyleForm = this.fb.group({
      consommationAlcool: [false],
      frequenceAlcool: [''],
      tabagisme: [false],
      frequenceTabac: [''],
      activitePhysique: [false]
    });

    // Étape 5: Finalisation (contact d'urgence + déclaration sur l'honneur)
    this.finalizationForm = this.fb.group({
      personneContact: ['', Validators.required],
      numeroContact: ['', [
        Validators.required, 
        Validators.pattern(/^[0-9+\s-]{9,15}$/),
        emergencyContactDifferentValidator(() => this.accountForm?.get('telephone')?.value || '')
      ]],
      declarationAcceptee: [false, Validators.requiredTrue]
    });

    // Configurer les validations conditionnelles
    this.setupConditionalValidations();
  }

  /**
   * Configure les validations conditionnelles pour les champs dépendants
   */
  private setupConditionalValidations(): void {
    // Santé: opérations chirurgicales -> détails obligatoires
    this.healthForm.get('operationsChirurgicales')?.valueChanges.subscribe(checked => {
      const detailsControl = this.healthForm.get('operationsDetails');
      if (checked) {
        detailsControl?.setValidators([Validators.required]);
      } else {
        detailsControl?.clearValidators();
        detailsControl?.setValue('');
      }
      detailsControl?.updateValueAndValidity();
    });

    // Santé: allergies connues -> détails obligatoires
    this.healthForm.get('allergiesConnues')?.valueChanges.subscribe(checked => {
      const detailsControl = this.healthForm.get('allergiesDetails');
      if (checked) {
        detailsControl?.setValidators([Validators.required]);
      } else {
        detailsControl?.clearValidators();
        detailsControl?.setValue('');
      }
      detailsControl?.updateValueAndValidity();
    });

    // Santé: antécédents familiaux -> détails obligatoires
    this.healthForm.get('antecedentsFamiliaux')?.valueChanges.subscribe(checked => {
      const detailsControl = this.healthForm.get('antecedentsFamiliauxDetails');
      if (checked) {
        detailsControl?.setValidators([Validators.required]);
      } else {
        detailsControl?.clearValidators();
        detailsControl?.setValue('');
      }
      detailsControl?.updateValueAndValidity();
    });

    // Habitudes: consommation d'alcool -> fréquence obligatoire
    this.lifestyleForm.get('consommationAlcool')?.valueChanges.subscribe(checked => {
      const frequenceControl = this.lifestyleForm.get('frequenceAlcool');
      if (checked) {
        frequenceControl?.setValidators([Validators.required]);
      } else {
        frequenceControl?.clearValidators();
        frequenceControl?.setValue('');
      }
      frequenceControl?.updateValueAndValidity();
    });

    // Habitudes: tabagisme -> fréquence obligatoire
    this.lifestyleForm.get('tabagisme')?.valueChanges.subscribe(checked => {
      const frequenceControl = this.lifestyleForm.get('frequenceTabac');
      if (checked) {
        frequenceControl?.setValidators([Validators.required]);
      } else {
        frequenceControl?.clearValidators();
        frequenceControl?.setValue('');
      }
      frequenceControl?.updateValueAndValidity();
    });

    // Revalider le numéro de contact d'urgence quand le téléphone principal change
    this.accountForm.get('telephone')?.valueChanges.subscribe(() => {
      this.finalizationForm.get('numeroContact')?.updateValueAndValidity();
    });
  }

  get currentForm(): FormGroup {
    switch (this.currentStep) {
      case 1: return this.accountForm;
      case 2: return this.identityForm;
      case 3: return this.healthForm;
      case 4: return this.lifestyleForm;
      case 5: return this.finalizationForm;
      default: return this.accountForm;
    }
  }

  get isCurrentStepValid(): boolean {
    return this.currentForm.valid;
  }

  get progressPercentage(): number {
    return ((this.currentStep - 1) / (this.totalSteps - 1)) * 100;
  }

  nextStep(): void {
    // Marquer tous les champs du formulaire courant comme touched pour afficher les erreurs
    this.currentForm.markAllAsTouched();
    
    if (this.currentStep < this.totalSteps && this.isCurrentStepValid) {
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
    if (stepId < this.currentStep) {
      this.currentStep = stepId;
      this.cdr.markForCheck();
    } else if (stepId === this.currentStep + 1 && this.isCurrentStepValid) {
      this.currentStep = stepId;
      this.cdr.markForCheck();
    }
  }

  onPasswordInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.passwordValue = input.value;
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  toggleMaladie(maladie: string): void {
    const current = this.healthForm.get('maladiesChroniques')?.value as string[];
    const index = current.indexOf(maladie);
    
    if (index === -1) {
      if (maladie === 'Aucune') {
        this.healthForm.patchValue({ maladiesChroniques: ['Aucune'] });
      } else {
        const filtered = current.filter(m => m !== 'Aucune');
        this.healthForm.patchValue({ maladiesChroniques: [...filtered, maladie] });
      }
    } else {
      this.healthForm.patchValue({ maladiesChroniques: current.filter(m => m !== maladie) });
    }
    this.cdr.markForCheck();
  }

  isMaladieSelected(maladie: string): boolean {
    const current = this.healthForm.get('maladiesChroniques')?.value as string[];
    return current?.includes(maladie) ?? false;
  }

  onSubmit(): void {
    // Marquer tous les champs comme touched pour afficher les erreurs
    this.currentForm.markAllAsTouched();
    
    if (!this.isCurrentStepValid || this.isLoading) return;

    this.isLoading = true;
    this.errorMessage = null;

    const account = this.accountForm.value;
    const identity = this.identityForm.value;
    const health = this.healthForm.value;
    const lifestyle = this.lifestyleForm.value;
    const finalization = this.finalizationForm.value;

    const request = {
      // Compte
      firstName: account.firstName,
      lastName: account.lastName,
      email: account.email,
      telephone: account.telephone,
      password: account.password,
      confirmPassword: account.confirmPassword,
      // Identité
      dateNaissance: identity.dateNaissance,
      sexe: identity.sexe,
      nationalite: identity.nationalite,
      regionOrigine: identity.regionOrigine,
      adresse: identity.adresse,
      situationMatrimoniale: identity.situationMatrimoniale,
      profession: identity.profession,
      nbEnfants: identity.nbEnfants,
      ethnie: identity.ethnie,
      // Santé
      groupeSanguin: health.groupeSanguin,
      maladiesChroniques: health.maladiesChroniques,
      operationsChirurgicales: health.operationsChirurgicales,
      operationsDetails: health.operationsDetails,
      allergiesConnues: health.allergiesConnues,
      allergiesDetails: health.allergiesDetails,
      antecedentsFamiliaux: health.antecedentsFamiliaux,
      antecedentsFamiliauxDetails: health.antecedentsFamiliauxDetails,
      // Habitudes de vie
      consommationAlcool: lifestyle.consommationAlcool,
      frequenceAlcool: lifestyle.frequenceAlcool,
      tabagisme: lifestyle.tabagisme,
      activitePhysique: lifestyle.activitePhysique,
      // Urgence et déclaration
      personneContact: finalization.personneContact,
      numeroContact: finalization.numeroContact,
      declarationHonneurAcceptee: finalization.declarationAcceptee
    };

    this.authService.register(request)
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.markForCheck();
        }),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (response: any) => {
          if (response.requiresEmailConfirmation && !response.token) {
            this.registrationSuccess = true;
            this.requiresEmailConfirmation = true;
            this.registeredEmail = response.email || account.email;
            this.cdr.markForCheck();
          } else {
            this.router.navigate(['/patient']);
          }
        },
        error: (error: any) => {
          this.errorMessage = error.message || 'Erreur lors de l\'inscription';
          this.cdr.markForCheck();
        }
      });
  }

  goToLogin(): void {
    this.authNavigation.navigateToLogin();
  }

  goToLanding(): void {
    this.authNavigation.navigateToLanding();
  }

  hasError(form: FormGroup, fieldName: string): boolean {
    const field = form.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getErrorMessage(form: FormGroup, fieldName: string): string {
    const field = form.get(fieldName);
    if (field?.hasError('required')) return 'Ce champ est requis';
    if (field?.hasError('minlength')) return `Minimum ${field.errors?.['minlength'].requiredLength} caractères`;
    if (field?.hasError('email')) return 'Email invalide';
    if (field?.hasError('pattern')) return 'Format invalide';
    if (field?.hasError('sameAsMainContact')) return 'Le contact d\'urgence doit être différent de votre numéro principal';
    return '';
  }

  passwordMismatch(): boolean {
    return !!(this.accountForm.hasError('passwordMismatch') && 
      this.accountForm.get('confirmPassword')?.touched);
  }
}
