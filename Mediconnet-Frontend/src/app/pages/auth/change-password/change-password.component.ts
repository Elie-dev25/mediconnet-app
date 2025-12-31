import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { finalize } from 'rxjs/operators';
import { ALL_ICONS_PROVIDER } from '../../../shared';
import { AuthService, ChangePasswordRequest } from '../../../services/auth.service';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.scss']
})
export class ChangePasswordComponent implements OnInit {
  passwordForm!: FormGroup;
  isLoading = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;
  
  showCurrentPassword = false;
  showNewPassword = false;
  showConfirmPassword = false;

  userName = '';
  userRole = '';

  // Indicateur de force du mot de passe
  passwordStrength: { width: string; class: string; label: string } = {
    width: '0%',
    class: '',
    label: ''
  };

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Vérifier si l'utilisateur est connecté
    const user = this.authService.getCurrentUser();
    if (!user) {
      this.router.navigate(['/login']);
      return;
    }

    this.userName = `${user.prenom || ''} ${user.nom || ''}`.trim();
    this.userRole = user.role;

    // Si patient créé à l'accueil, vérifier que la déclaration est acceptée
    if (user.role === 'patient' && user.profileCompleted === true && user.mustChangePassword === true) {
      if (user.declarationHonneurAcceptee !== true) {
        // Rediriger vers first-login si déclaration non acceptée
        this.router.navigate(['/auth/first-login']);
        return;
      }
    }

    this.initForm();
  }

  private initForm(): void {
    this.passwordForm = this.fb.group({
      currentPassword: ['', [Validators.required, Validators.minLength(6)]],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]]
    }, {
      validators: this.passwordMatchValidator
    });

    // Écouter les changements du mot de passe pour calculer la force
    this.passwordForm.get('newPassword')?.valueChanges.subscribe(value => {
      this.updatePasswordStrength(value);
    });
  }

  private passwordMatchValidator(form: FormGroup) {
    const newPassword = form.get('newPassword');
    const confirmPassword = form.get('confirmPassword');
    
    if (newPassword && confirmPassword && newPassword.value !== confirmPassword.value) {
      confirmPassword.setErrors({ mismatch: true });
    }
    return null;
  }

  /**
   * Calcule la force du mot de passe
   */
  private updatePasswordStrength(password: string): void {
    if (!password) {
      this.passwordStrength = { width: '0%', class: '', label: '' };
      return;
    }

    let score = 0;
    
    // Longueur
    if (password.length >= 8) score++;
    if (password.length >= 12) score++;
    
    // Majuscules
    if (/[A-Z]/.test(password)) score++;
    
    // Minuscules
    if (/[a-z]/.test(password)) score++;
    
    // Chiffres
    if (/[0-9]/.test(password)) score++;
    
    // Caractères spéciaux
    if (/[^A-Za-z0-9]/.test(password)) score++;

    if (score <= 2) {
      this.passwordStrength = { width: '33%', class: 'weak', label: 'Faible' };
    } else if (score <= 4) {
      this.passwordStrength = { width: '66%', class: 'medium', label: 'Moyen' };
    } else {
      this.passwordStrength = { width: '100%', class: 'strong', label: 'Fort' };
    }
  }

  togglePasswordVisibility(field: 'current' | 'new' | 'confirm'): void {
    switch (field) {
      case 'current':
        this.showCurrentPassword = !this.showCurrentPassword;
        break;
      case 'new':
        this.showNewPassword = !this.showNewPassword;
        break;
      case 'confirm':
        this.showConfirmPassword = !this.showConfirmPassword;
        break;
    }
  }

  onSubmit(): void {
    if (this.passwordForm.invalid || this.isLoading) return;

    this.isLoading = true;
    this.errorMessage = null;
    this.successMessage = null;

    const { currentPassword, newPassword, confirmPassword } = this.passwordForm.value;

    const request: ChangePasswordRequest = {
      currentPassword,
      newPassword,
      confirmNewPassword: confirmPassword
    };

    this.authService.changePassword(request).pipe(
      finalize(() => {
        this.isLoading = false;
      })
    ).subscribe({
      next: (response: any) => {
        if (response.success) {
          // Mettre à jour les informations utilisateur locales
          this.authService.updateUserInfo({ mustChangePassword: false });
          
          this.successMessage = 'Mot de passe modifié avec succès. Redirection...';
          
          // Rediriger vers le dashboard après 1.5 secondes
          setTimeout(() => {
            const user = this.authService.getCurrentUser();
            const route = this.getDashboardRoute(user?.role);
            this.router.navigate([route]);
          }, 1500);
        } else {
          this.errorMessage = response.message || 'Erreur lors du changement de mot de passe';
        }
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Erreur lors du changement de mot de passe';
      }
    });
  }

  private getDashboardRoute(role: string | undefined): string {
    const routes: Record<string, string> = {
      'patient': '/patient',
      'medecin': '/medecin',
      'infirmier': '/infirmier',
      'administrateur': '/admin',
      'caissier': '/caissier',
      'accueil': '/accueil'
    };
    return routes[role || ''] || '/';
  }

  get f() {
    return this.passwordForm.controls;
  }
}
