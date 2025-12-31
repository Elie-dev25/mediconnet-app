import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthNavigationService } from '../../../services/auth-navigation.service';
import { AuthLayoutWrapperComponent } from '../../../shared/components/auth-layout-wrapper/auth-layout-wrapper.component';
import { trigger, transition, style, animate } from '@angular/animations';

/**
 * Composant Landing Page réutilisable
 * Affiche la page d'accueil avec boutons de connexion/inscription
 */
@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, AuthLayoutWrapperComponent],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [
    trigger('fadeInUp', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(20px)' }),
        animate('600ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ]),
    trigger('slideIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateX(-30px)' }),
        animate('800ms ease-out', style({ opacity: 1, transform: 'translateX(0)' }))
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
export class LandingComponent implements OnInit {

  constructor(private authNavigation: AuthNavigationService) {}

  ngOnInit(): void {
    // Logique d'initialisation si nécessaire
  }

  /**
   * Navigation vers la page de connexion
   */
  navigateToLogin(): void {
    this.authNavigation.navigateToLogin();
  }

  /**
   * Navigation vers la page d'inscription
   */
  navigateToRegister(): void {
    this.authNavigation.navigateToRegister();
  }
}
