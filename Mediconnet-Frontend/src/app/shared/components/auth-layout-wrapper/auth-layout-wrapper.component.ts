import { Component, Input, SecurityContext } from '@angular/core';
import { CommonModule } from '@angular/common';
import { trigger, transition, style, animate } from '@angular/animations';
import { DomSanitizer, SafeStyle } from '@angular/platform-browser';

@Component({
  selector: 'app-auth-layout-wrapper',
  templateUrl: './auth-layout-wrapper.component.html',
  styleUrls: ['./auth-layout-wrapper.component.scss'],
  standalone: true,
  imports: [CommonModule],
  animations: [
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('300ms ease-in', style({ opacity: 1 }))
      ])
    ]),
    // Animation pour le contenu qui entre par la droite
    trigger('slideInRight', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateX(50px)' }),
        animate('500ms ease-out', style({ opacity: 1, transform: 'translateX(0)' }))
      ]),
      transition(':leave', [
        animate('300ms ease-in', style({ opacity: 0, transform: 'translateX(-50px)' }))
      ])
    ])
  ]
})
export class AuthLayoutWrapperComponent {
  @Input() showDecorative = true;
  @Input() backgroundImage = 'assets/images/accueil.jpeg';
  
  backgroundImageStyle: SafeStyle;

  constructor(private sanitizer: DomSanitizer) {
    this.backgroundImageStyle = this.sanitizer.bypassSecurityTrustStyle(
      `url(${this.backgroundImage})`
    );
  }

  ngOnChanges() {
    this.backgroundImageStyle = this.sanitizer.bypassSecurityTrustStyle(
      `url(${this.backgroundImage})`
    );
  }
}
