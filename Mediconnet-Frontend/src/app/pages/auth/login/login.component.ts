import { 
  Component, 
  OnInit, 
  ChangeDetectionStrategy, 
  OnDestroy,
  ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { trigger, transition, style, animate } from '@angular/animations';
import { AuthService, LoginCredentials } from '../../../services/auth.service';
import { AuthNavigationService } from '../../../services/auth-navigation.service';
import { AuthLayoutWrapperComponent } from '../../../shared/components/auth-layout-wrapper/auth-layout-wrapper.component';
import { LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    LucideAngularModule, 
    AuthLayoutWrapperComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(20px)' }),
        animate('600ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ]),
    trigger('slideInRight', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateX(50px)' }),
        animate('500ms 100ms ease-out', style({ opacity: 1, transform: 'translateX(0)' }))
      ])
    ])
  ]
})
export class LoginComponent implements OnInit, OnDestroy {
  
  loginForm!: FormGroup;
  isLoading = false;
  errorMessage: string | null = null;
  showPassword = false;
  
  // État pour email non confirmé
  emailNotConfirmed = false;
  unconfirmedEmail = '';
  isResendingEmail = false;
  resendSuccess = false;
  
  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private authNavigation: AuthNavigationService,
    private cdr: ChangeDetectorRef
  ) {
    this.initializeForm();
  }

  ngOnInit(): void {
    // Initialisation si nécessaire
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForm(): void {
    this.loginForm = this.formBuilder.group({
      identifier: ['', [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  get identifier() {
    return this.loginForm.get('identifier');
  }

  get password() {
    return this.loginForm.get('password');
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    if (this.loginForm.invalid || this.isLoading) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;

    const credentials: LoginCredentials = {
      identifier: this.identifier?.value,
      password: this.password?.value
    };

    this.authService.login(credentials)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.isLoading = false;
          
          // Vérifier si l'email n'est pas confirmé
          if (response.message === 'EMAIL_NOT_CONFIRMED' || response.requiresEmailConfirmation) {
            this.emailNotConfirmed = true;
            this.unconfirmedEmail = response.email || credentials.identifier;
            this.cdr.markForCheck();
            return;
          }
          
          // Vérifier qu'on a bien un token
          if (!response.token) {
            this.errorMessage = 'Erreur de connexion. Veuillez réessayer.';
            this.cdr.markForCheck();
            return;
          }
          
          // Récupérer l'utilisateur
          const user = this.authService.getCurrentUser();
          
          // FLUX PATIENT CRÉÉ À L'ACCUEIL: profileCompleted=true, mustChangePassword=true
          // Doit d'abord accepter la déclaration, puis changer le mot de passe
          if (user?.role === 'patient' && response.profileCompleted && response.mustChangePassword) {
            // Vérifier si la déclaration sur l'honneur est acceptée
            if (!response.declarationHonneurAcceptee) {
              this.router.navigate(['/auth/first-login']);
              return;
            }
            // Déclaration acceptée, rediriger vers changement de mot de passe
            this.router.navigate(['/auth/change-password']);
            return;
          }
          
          // Vérifier si l'utilisateur doit changer son mot de passe (autres rôles)
          if (response.mustChangePassword) {
            this.router.navigate(['/auth/change-password']);
            return;
          }
          
          // FLUX PATIENT AUTO-INSCRIT: profileCompleted=false
          if (user?.role === 'patient' && !response.profileCompleted) {
            this.router.navigate(['/complete-profile']);
            return;
          }
          
          // Rediriger vers le dashboard selon le role
          const dashboardRoute = this.getDashboardRoute(user?.role);
          this.router.navigate([dashboardRoute]);
        },
        error: (error: any) => {
          this.isLoading = false;
          this.errorMessage = error.message || 'Erreur de connexion';
          this.cdr.markForCheck();
        }
      });
  }

  /**
   * Renvoyer l'email de confirmation
   */
  resendConfirmationEmail(): void {
    if (this.isResendingEmail || !this.unconfirmedEmail) return;
    
    this.isResendingEmail = true;
    this.resendSuccess = false;
    
    this.authService.resendConfirmationEmail(this.unconfirmedEmail)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.isResendingEmail = false;
          this.resendSuccess = true;
          this.cdr.markForCheck();
        },
        error: () => {
          this.isResendingEmail = false;
          this.cdr.markForCheck();
        }
      });
  }

  /**
   * Retour au formulaire de login
   */
  backToLogin(): void {
    this.emailNotConfirmed = false;
    this.resendSuccess = false;
    this.cdr.markForCheck();
  }

  private getDashboardRoute(role: string): string {
    const routes: Record<string, string> = {
      'patient': '/patient',
      'medecin': '/medecin',
      'infirmier': '/infirmier',
      'administrateur': '/admin',
      'caissier': '/caissier',
      'accueil': '/accueil',
      'pharmacien': '/pharmacien',
      'biologiste': '/biologiste'
    };
    return routes[role] || '/patient';
  }

  goToRegister(): void {
    this.authNavigation.navigateToRegister();
  }

  goToLanding(): void {
    this.authNavigation.navigateToLanding();
  }

  hasError(fieldName: string): boolean {
    const field = this.loginForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getErrorMessage(fieldName: string): string {
    const field = this.loginForm.get(fieldName);
    
    if (field?.hasError('required')) {
      if (fieldName === 'identifier') {
        return 'Email ou téléphone requis';
      }
      return `${fieldName} est requis`;
    }
    if (field?.hasError('minlength')) {
      const minLength = field.errors?.['minlength'].requiredLength;
      return `${fieldName} doit contenir au moins ${minLength} caractères`;
    }
    
    return '';
  }
}
