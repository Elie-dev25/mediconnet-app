import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

export interface PasswordCriteria {
  hasMinLength: boolean;
  hasUppercase: boolean;
  hasLowercase: boolean;
  hasDigit: boolean;
  hasSpecialChar: boolean;
}

export type PasswordStrengthLevel = 'weak' | 'medium' | 'strong';

@Component({
  selector: 'app-password-strength-indicator',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './password-strength-indicator.component.html',
  styleUrls: ['./password-strength-indicator.component.scss']
})
export class PasswordStrengthIndicatorComponent implements OnChanges {
  @Input() password: string = '';
  @Input() showCriteria: boolean = true;

  strengthLevel: PasswordStrengthLevel = 'weak';
  strengthScore: number = 0;
  strengthLabel: string = 'Faible';
  strengthColor: string = '#ef4444';
  criteria: PasswordCriteria = {
    hasMinLength: false,
    hasUppercase: false,
    hasLowercase: false,
    hasDigit: false,
    hasSpecialChar: false
  };

  private readonly MIN_LENGTH = 8;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['password']) {
      this.evaluatePassword();
    }
  }

  private evaluatePassword(): void {
    const password = this.password || '';

    // Évaluer les critères
    this.criteria = {
      hasMinLength: password.length >= this.MIN_LENGTH,
      hasUppercase: /[A-Z]/.test(password),
      hasLowercase: /[a-z]/.test(password),
      hasDigit: /\d/.test(password),
      hasSpecialChar: /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?~`]/.test(password)
    };

    // Calculer le score
    this.strengthScore = this.calculateScore(password);

    // Déterminer le niveau
    this.determineStrengthLevel();
  }

  private calculateScore(password: string): number {
    if (!password) return 0;

    let score = 0;

    // Points pour la longueur (max 40)
    score += Math.min(password.length * 4, 40);

    // Points pour les majuscules
    if (this.criteria.hasUppercase) score += 10;

    // Points pour les minuscules
    if (this.criteria.hasLowercase) score += 10;

    // Points pour les chiffres
    if (this.criteria.hasDigit) score += 10;

    // Points pour les caractères spéciaux
    if (this.criteria.hasSpecialChar) score += 15;

    // Bonus pour la diversité
    const uniqueChars = new Set(password).size;
    score += Math.min(uniqueChars, 15);

    // Pénalité pour les caractères répétés
    const repeatedChars = password.length - uniqueChars;
    score -= repeatedChars;

    return Math.max(0, Math.min(100, score));
  }

  private determineStrengthLevel(): void {
    const allRequiredMet = this.criteria.hasMinLength && 
                           this.criteria.hasUppercase && 
                           this.criteria.hasLowercase && 
                           this.criteria.hasDigit;

    if (this.strengthScore >= 70 && allRequiredMet && this.criteria.hasSpecialChar) {
      this.strengthLevel = 'strong';
      this.strengthLabel = 'Fort';
      this.strengthColor = '#10b981';
    } else if (this.strengthScore >= 50 && allRequiredMet) {
      this.strengthLevel = 'medium';
      this.strengthLabel = 'Moyen';
      this.strengthColor = '#f59e0b';
    } else {
      this.strengthLevel = 'weak';
      this.strengthLabel = 'Faible';
      this.strengthColor = '#ef4444';
    }
  }

  get isValid(): boolean {
    return this.criteria.hasMinLength && 
           this.criteria.hasUppercase && 
           this.criteria.hasLowercase && 
           this.criteria.hasDigit;
  }

  get progressWidth(): number {
    return this.strengthScore;
  }
}
