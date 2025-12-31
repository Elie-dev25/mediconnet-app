import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { finalize } from 'rxjs/operators';
import { ALL_ICONS_PROVIDER } from '../../../shared';
import { FirstLoginService, FirstLoginPatientInfo } from '../../../services/first-login.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-first-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './first-login.component.html',
  styleUrls: ['./first-login.component.scss']
})
export class FirstLoginComponent implements OnInit {
  
  // Données du patient
  patientInfo: FirstLoginPatientInfo | null = null;
  isLoading = true;
  loadError: string | null = null;
  
  // Formulaire déclaration
  declarationForm!: FormGroup;
  
  // États
  isSubmitting = false;
  errorMessage: string | null = null;

  constructor(
    private formBuilder: FormBuilder,
    private router: Router,
    private firstLoginService: FirstLoginService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    // Vérifier que l'utilisateur est bien un patient créé à l'accueil
    const user = this.authService.getCurrentUser();
    if (!user || user.role !== 'patient') {
      this.router.navigate(['/login']);
      return;
    }

    // Ce composant est uniquement pour les patients créés à l'accueil
    // (profileCompleted = true, mustChangePassword = true)
    if (user.profileCompleted !== true || user.mustChangePassword !== true) {
      // Si c'est un patient auto-inscrit, rediriger vers complete-profile
      if (user.profileCompleted !== true) {
        this.router.navigate(['/complete-profile']);
      } else {
        // Sinon, rediriger vers le dashboard
        this.router.navigate(['/patient/dashboard']);
      }
      return;
    }

    this.initForm();
    this.loadPatientInfo();
  }

  private initForm(): void {
    this.declarationForm = this.formBuilder.group({
      declarationAcceptee: [false, [Validators.requiredTrue]]
    });
  }

  loadPatientInfo(): void {
    this.isLoading = true;
    this.loadError = null;
    
    this.firstLoginService.getFirstLoginInfo().pipe(
      finalize(() => {
        this.isLoading = false;
      })
    ).subscribe({
      next: (info) => {
        this.patientInfo = info;
        
        // Si la déclaration est déjà acceptée mais doit changer le mot de passe
        if (info.declarationHonneurAcceptee && info.mustChangePassword) {
          this.router.navigate(['/auth/change-password']);
        } else if (info.declarationHonneurAcceptee && !info.mustChangePassword) {
          // Tout est déjà complété, rediriger vers le dashboard
          this.router.navigate(['/patient/dashboard']); 
        }
      },
      error: (err) => {
        this.loadError = 'Impossible de charger vos informations. Veuillez réessayer.';
        console.error('Erreur chargement infos patient:', err);
      }
    });
  }

  /**
   * Valide la déclaration et redirige vers le changement de mot de passe
   */
  submitDeclaration(): void {
    if (this.isSubmitting) {
      return;
    }

    if (this.declarationForm.invalid) {
      this.errorMessage = 'Vous devez accepter la déclaration sur l\'honneur';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = null;

    this.firstLoginService.acceptDeclaration({ declarationHonneurAcceptee: true }).pipe(
      finalize(() => {
        this.isSubmitting = false;
      })
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.authService.updateUserInfo({ declarationHonneurAcceptee: true });
          this.router.navigate(['/auth/change-password']);
        } else {
          this.errorMessage = response.message || 'Erreur lors de la validation';
        }
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Une erreur est survenue';
        console.error('Erreur validation:', err);
      }
    });
  }

  // ============================================
  // Helpers pour l'affichage
  // ============================================
  
  formatDate(dateStr?: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { day: '2-digit', month: 'long', year: 'numeric' });
  }

  formatSexe(sexe?: string): string {
    if (sexe === 'M') return 'Masculin';
    if (sexe === 'F') return 'Féminin';
    return '-';
  }

  formatBoolean(value?: boolean): string {
    if (value === true) return 'Oui';
    if (value === false) return 'Non';
    return '-';
  }
}
