import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-welcome-banner',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './welcome-banner.component.html',
  styleUrl: './welcome-banner.component.scss'
})
export class WelcomeBannerComponent {
  @Input() userName = '';
  @Input() userRole = '';
  @Input() subtitle = 'Passez une excellente journée';

  get greeting(): string {
    const hour = new Date().getHours();
    if (hour < 12) return 'Bonjour';
    if (hour < 18) return 'Bon après-midi';
    return 'Bonsoir';
  }

  get displayName(): string {
    if (!this.userName) return '';
    
    // Ajouter le préfixe selon le rôle
    switch (this.userRole) {
      case 'medecin':
        return `Dr. ${this.userName}`;
      case 'infirmier':
        return `Inf. ${this.userName}`;
      default:
        return this.userName;
    }
  }
}
