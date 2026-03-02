import { Directive, ElementRef, HostListener, Input, OnDestroy, OnInit, Renderer2 } from '@angular/core';
import { NgControl } from '@angular/forms';
import { Subscription } from 'rxjs';
import { SpeechRecognitionService, SupportedLanguage } from '../../services/speech-recognition.service';

/**
 * Directive pour ajouter la saisie vocale à n'importe quel champ de texte (input, textarea)
 * 
 * Usage:
 * <textarea appVoiceInput [voiceFieldName]="'monChamp'" [voiceLanguage]="'fr-FR'"></textarea>
 * <input type="text" appVoiceInput />
 * 
 * La directive ajoute automatiquement un bouton micro à côté du champ.
 * Le texte dicté est ajouté au contenu existant du champ.
 */
@Directive({
  selector: '[appVoiceInput]',
  standalone: true
})
export class VoiceInputDirective implements OnInit, OnDestroy {
  @Input() voiceFieldName?: string;
  @Input() voiceLanguage: SupportedLanguage = 'fr-FR';
  @Input() voiceConsultationId?: number;
  @Input() voiceAppendMode: boolean = true;

  private subscriptions: Subscription[] = [];
  private micButton: HTMLButtonElement | null = null;
  private isRecording = false;
  private wrapper: HTMLDivElement | null = null;

  readonly isSupported: boolean;

  constructor(
    private el: ElementRef<HTMLInputElement | HTMLTextAreaElement>,
    private renderer: Renderer2,
    private speechService: SpeechRecognitionService,
    private ngControl: NgControl | null
  ) {
    this.isSupported = this.speechService.isSupported;
  }

  ngOnInit(): void {
    if (!this.isSupported) return;

    this.createVoiceButton();
    this.setupSubscriptions();
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
    if (this.isRecording) {
      this.speechService.stop();
    }
  }

  private createVoiceButton(): void {
    const element = this.el.nativeElement;
    const parent = element.parentElement;
    if (!parent) return;

    // Créer un wrapper pour positionner le bouton
    this.wrapper = this.renderer.createElement('div');
    this.renderer.addClass(this.wrapper, 'voice-input-wrapper');
    this.renderer.setStyle(this.wrapper, 'position', 'relative');
    this.renderer.setStyle(this.wrapper, 'display', 'inline-block');
    this.renderer.setStyle(this.wrapper, 'width', '100%');

    // Insérer le wrapper avant l'élément et déplacer l'élément dedans
    this.renderer.insertBefore(parent, this.wrapper, element);
    this.renderer.appendChild(this.wrapper, element);

    // Créer le bouton micro
    this.micButton = this.renderer.createElement('button');
    this.renderer.setAttribute(this.micButton, 'type', 'button');
    this.renderer.setAttribute(this.micButton, 'title', 'Dictée vocale');
    this.renderer.addClass(this.micButton, 'voice-input-mic-btn');
    
    // Styles du bouton
    this.applyButtonStyles();

    // Icône micro (SVG inline pour éviter les dépendances)
    this.micButton!.innerHTML = this.getMicIcon();

    // Event listener
    this.renderer.listen(this.micButton, 'click', (e: Event) => {
      e.preventDefault();
      e.stopPropagation();
      this.toggleRecording();
    });

    this.renderer.appendChild(this.wrapper, this.micButton);
  }

  private applyButtonStyles(): void {
    if (!this.micButton) return;

    const styles: { [key: string]: string } = {
      'position': 'absolute',
      'right': '8px',
      'top': '8px',
      'width': '28px',
      'height': '28px',
      'border-radius': '50%',
      'border': '1.5px solid #e2e8f0',
      'background': 'white',
      'color': '#64748b',
      'cursor': 'pointer',
      'display': 'flex',
      'align-items': 'center',
      'justify-content': 'center',
      'transition': 'all 0.2s ease',
      'z-index': '10',
      'padding': '0'
    };

    Object.entries(styles).forEach(([key, value]) => {
      this.renderer.setStyle(this.micButton, key, value);
    });
  }

  private getMicIcon(): string {
    return `<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
      <path d="M12 2a3 3 0 0 0-3 3v7a3 3 0 0 0 6 0V5a3 3 0 0 0-3-3Z"></path>
      <path d="M19 10v2a7 7 0 0 1-14 0v-2"></path>
      <line x1="12" x2="12" y1="19" y2="22"></line>
    </svg>`;
  }

  private getMicOffIcon(): string {
    return `<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
      <line x1="2" x2="22" y1="2" y2="22"></line>
      <path d="M18.89 13.23A7.12 7.12 0 0 0 19 12v-2"></path>
      <path d="M5 10v2a7 7 0 0 0 12 5"></path>
      <path d="M15 9.34V5a3 3 0 0 0-5.68-1.33"></path>
      <path d="M9 9v3a3 3 0 0 0 5.12 2.12"></path>
      <line x1="12" x2="12" y1="19" y2="22"></line>
    </svg>`;
  }

  private setupSubscriptions(): void {
    // Écouter l'état d'enregistrement
    this.subscriptions.push(
      this.speechService.listening$.subscribe(isListening => {
        this.isRecording = isListening;
        this.updateButtonState();
      })
    );

    // Écouter les transcriptions
    this.subscriptions.push(
      this.speechService.transcripts$.subscribe(event => {
        if (event.isFinal) {
          this.appendText(event.transcript.trim());
        }
      })
    );
  }

  private toggleRecording(): void {
    if (this.isRecording) {
      this.speechService.stop();
    } else {
      const fieldName = this.voiceFieldName || this.el.nativeElement.name || 'field';
      this.speechService.start(
        this.voiceConsultationId,
        fieldName,
        undefined,
        this.voiceLanguage
      );
    }
  }

  private updateButtonState(): void {
    if (!this.micButton) return;

    if (this.isRecording) {
      this.renderer.setStyle(this.micButton, 'border-color', '#ef4444');
      this.renderer.setStyle(this.micButton, 'background', '#fef2f2');
      this.renderer.setStyle(this.micButton, 'color', '#ef4444');
      this.renderer.setStyle(this.micButton, 'animation', 'voice-pulse 1.5s ease-in-out infinite');
      this.micButton.innerHTML = this.getMicOffIcon();
      this.renderer.setAttribute(this.micButton, 'title', 'Arrêter la dictée');
    } else {
      this.renderer.setStyle(this.micButton, 'border-color', '#e2e8f0');
      this.renderer.setStyle(this.micButton, 'background', 'white');
      this.renderer.setStyle(this.micButton, 'color', '#64748b');
      this.renderer.setStyle(this.micButton, 'animation', 'none');
      this.micButton.innerHTML = this.getMicIcon();
      this.renderer.setAttribute(this.micButton, 'title', 'Dictée vocale');
    }
  }

  private appendText(text: string): void {
    if (!text) return;

    const element = this.el.nativeElement;
    
    if (this.ngControl && this.ngControl.control) {
      // Utiliser le FormControl si disponible
      const currentValue = this.ngControl.control.value || '';
      const newValue = this.voiceAppendMode && currentValue 
        ? `${currentValue.trim()} ${text}` 
        : text;
      this.ngControl.control.setValue(newValue);
      this.ngControl.control.markAsDirty();
    } else {
      // Fallback: manipulation directe de l'élément
      const currentValue = element.value || '';
      const newValue = this.voiceAppendMode && currentValue 
        ? `${currentValue.trim()} ${text}` 
        : text;
      element.value = newValue;
      
      // Déclencher l'événement input pour la détection de changement
      const event = new Event('input', { bubbles: true });
      element.dispatchEvent(event);
    }

    // Focus sur l'élément après la dictée
    element.focus();
  }

  @HostListener('mouseenter')
  onMouseEnter(): void {
    if (this.micButton && !this.isRecording) {
      this.renderer.setStyle(this.micButton, 'border-color', '#3b82f6');
      this.renderer.setStyle(this.micButton, 'color', '#3b82f6');
      this.renderer.setStyle(this.micButton, 'background', '#eff6ff');
    }
  }

  @HostListener('mouseleave')
  onMouseLeave(): void {
    if (this.micButton && !this.isRecording) {
      this.renderer.setStyle(this.micButton, 'border-color', '#e2e8f0');
      this.renderer.setStyle(this.micButton, 'color', '#64748b');
      this.renderer.setStyle(this.micButton, 'background', 'white');
    }
  }
}
