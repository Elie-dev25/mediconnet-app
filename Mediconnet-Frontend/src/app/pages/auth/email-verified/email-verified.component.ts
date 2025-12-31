import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { 
  LucideAngularModule, 
  LUCIDE_ICONS, 
  LucideIconProvider,
  CheckCircle, XCircle, Mail, ArrowRight, AlertTriangle, RefreshCw
} from 'lucide-angular';
import { AuthLayoutWrapperComponent } from '../../../shared/components/auth-layout-wrapper/auth-layout-wrapper.component';
import { trigger, transition, style, animate } from '@angular/animations';

@Component({
  selector: 'app-email-verified',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, AuthLayoutWrapperComponent],
  providers: [
    { 
      provide: LUCIDE_ICONS, 
      useValue: new LucideIconProvider({ CheckCircle, XCircle, Mail, ArrowRight, AlertTriangle, RefreshCw })
    }
  ],
  templateUrl: './email-verified.component.html',
  styleUrl: './email-verified.component.scss',
  animations: [
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(20px)' }),
        animate('600ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ])
  ]
})
export class EmailVerifiedComponent implements OnInit {
  success = false;
  errorCode: string | null = null;
  errorMessage = '';

  constructor(
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    // Récupérer les paramètres de l'URL
    this.route.queryParams.subscribe(params => {
      this.success = params['success'] === 'true';
      this.errorCode = params['error'] || null;
      
      if (this.errorCode) {
        this.errorMessage = this.getErrorMessage(this.errorCode);
      }
    });
  }

  private getErrorMessage(code: string): string {
    const messages: { [key: string]: string } = {
      'TOKEN_NOT_FOUND': 'Le lien de confirmation est invalide ou a expiré.',
      'TOKEN_EXPIRED': 'Le lien de confirmation a expiré. Veuillez demander un nouveau lien.',
      'TOKEN_ALREADY_USED': 'Ce lien a déjà été utilisé. Votre email est déjà confirmé.',
      'missing_token': 'Le lien de confirmation est incomplet.',
      'server_error': 'Une erreur serveur s\'est produite. Veuillez réessayer.',
      'EMAIL_ALREADY_CONFIRMED': 'Votre adresse email est déjà confirmée.'
    };
    return messages[code] || 'Une erreur s\'est produite lors de la confirmation.';
  }

  continueToLogin(): void {
    this.router.navigate(['/auth/login']);
  }

  goToRegister(): void {
    this.router.navigate(['/auth/register']);
  }
}
