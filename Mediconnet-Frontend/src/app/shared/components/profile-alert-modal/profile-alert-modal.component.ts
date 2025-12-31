/**
 * @deprecated Ce composant n'est plus utilisé depuis que le profil est complété lors de l'inscription.
 * L'alerte de profil incomplet n'est plus nécessaire.
 * Ce fichier est conservé pour référence mais ne doit plus être utilisé.
 */
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { 
  LucideAngularModule, 
  LUCIDE_ICONS, 
  LucideIconProvider,
  AlertCircle, X, UserCog, ChevronRight
} from 'lucide-angular';
import { trigger, transition, style, animate } from '@angular/animations';

/** @deprecated */
@Component({
  selector: 'app-profile-alert-modal',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  providers: [
    { 
      provide: LUCIDE_ICONS, 
      useValue: new LucideIconProvider({ AlertCircle, X, UserCog, ChevronRight })
    }
  ],
  templateUrl: './profile-alert-modal.component.html',
  styleUrl: './profile-alert-modal.component.scss',
  animations: [
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('200ms ease-out', style({ opacity: 1 }))
      ]),
      transition(':leave', [
        animate('150ms ease-in', style({ opacity: 0 }))
      ])
    ]),
    trigger('slideIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'scale(0.9) translateY(-20px)' }),
        animate('250ms ease-out', style({ opacity: 1, transform: 'scale(1) translateY(0)' }))
      ]),
      transition(':leave', [
        animate('150ms ease-in', style({ opacity: 0, transform: 'scale(0.9) translateY(-20px)' }))
      ])
    ])
  ]
})
export class ProfileAlertModalComponent {
  @Input() isOpen = false;
  @Input() missingFields: string[] = [];
  
  @Output() cancel = new EventEmitter<void>();
  @Output() complete = new EventEmitter<void>();

  onOverlayClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-overlay')) {
      this.onCancel();
    }
  }

  onCancel(): void {
    this.cancel.emit();
  }

  onComplete(): void {
    this.complete.emit();
  }
}
