import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { Subscription } from 'rxjs';
import { SpeechRecognitionService, SpeechRecognitionEvent } from '../../../services/speech-recognition.service';

@Component({
  selector: 'app-voice-input',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => VoiceInputComponent),
      multi: true
    }
  ],
  template: `
    <div class="voice-input-container" [class.recording]="isRecording" [class.unsupported]="!isSupported">
      <!-- Bouton micro principal -->
      <button 
        type="button"
        class="voice-btn"
        [class.active]="isRecording"
        [class.disabled]="!isSupported"
        [disabled]="!isSupported"
        (click)="toggleRecording()"
        [title]="getButtonTitle()">
        <lucide-icon 
          [name]="isRecording ? 'mic-off' : 'mic'" 
          [size]="iconSize">
        </lucide-icon>
        <span class="pulse-ring" *ngIf="isRecording"></span>
      </button>

      <!-- Indicateur d'enregistrement -->
      <div class="recording-indicator" *ngIf="isRecording">
        <span class="recording-dot"></span>
        <span class="recording-text">Dictée en cours...</span>
        <span class="recording-time">{{ recordingDuration }}s</span>
      </div>

      <!-- Transcription en temps réel (optionnel) -->
      <div class="interim-transcript" *ngIf="showInterimTranscript && interimTranscript">
        <span class="interim-text">{{ interimTranscript }}</span>
      </div>

      <!-- Message d'erreur -->
      <div class="error-message" *ngIf="errorMessage">
        <lucide-icon name="alert-circle" [size]="14"></lucide-icon>
        {{ errorMessage }}
      </div>

      <!-- Message de non-support -->
      <div class="unsupported-message" *ngIf="!isSupported && showUnsupportedMessage">
        <lucide-icon name="mic-off" [size]="14"></lucide-icon>
        Saisie vocale non disponible
      </div>
    </div>
  `,
  styles: [`
    .voice-input-container {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      position: relative;
    }

    .voice-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      border-radius: 50%;
      border: 2px solid #e2e8f0;
      background: white;
      color: #64748b;
      cursor: pointer;
      transition: all 0.2s ease;
      position: relative;
      overflow: visible;

      &:hover:not(.disabled) {
        border-color: #3b82f6;
        color: #3b82f6;
        background: #eff6ff;
      }

      &.active {
        border-color: #ef4444;
        background: #fef2f2;
        color: #ef4444;
        animation: pulse-btn 1.5s ease-in-out infinite;
      }

      &.disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .pulse-ring {
      position: absolute;
      width: 100%;
      height: 100%;
      border-radius: 50%;
      border: 2px solid #ef4444;
      animation: pulse-ring 1.5s ease-out infinite;
      pointer-events: none;
    }

    @keyframes pulse-ring {
      0% {
        transform: scale(1);
        opacity: 1;
      }
      100% {
        transform: scale(1.8);
        opacity: 0;
      }
    }

    @keyframes pulse-btn {
      0%, 100% {
        transform: scale(1);
      }
      50% {
        transform: scale(1.05);
      }
    }

    .recording-indicator {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.25rem 0.75rem;
      background: #fef2f2;
      border: 1px solid #fecaca;
      border-radius: 20px;
      font-size: 0.8rem;
      color: #dc2626;
    }

    .recording-dot {
      width: 8px;
      height: 8px;
      background: #ef4444;
      border-radius: 50%;
      animation: blink 1s ease-in-out infinite;
    }

    @keyframes blink {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.3; }
    }

    .recording-text {
      font-weight: 500;
    }

    .recording-time {
      font-family: monospace;
      font-size: 0.75rem;
      color: #991b1b;
    }

    .interim-transcript {
      position: absolute;
      bottom: calc(100% + 8px);
      left: 0;
      right: 0;
      min-width: 200px;
      padding: 0.5rem 0.75rem;
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
      font-size: 0.85rem;
      color: #475569;
      z-index: 10;

      .interim-text {
        font-style: italic;
        color: #64748b;
      }
    }

    .error-message {
      display: flex;
      align-items: center;
      gap: 0.25rem;
      padding: 0.25rem 0.5rem;
      background: #fef2f2;
      border-radius: 4px;
      font-size: 0.75rem;
      color: #dc2626;
    }

    .unsupported-message {
      display: flex;
      align-items: center;
      gap: 0.25rem;
      font-size: 0.75rem;
      color: #94a3b8;
    }

    /* Mode compact */
    :host-context(.voice-input-compact) {
      .voice-btn {
        width: 28px;
        height: 28px;
      }

      .recording-indicator {
        padding: 0.125rem 0.5rem;
        font-size: 0.7rem;
      }
    }

    /* Mode inline (à côté d'un textarea) */
    :host-context(.voice-input-inline) {
      .voice-input-container {
        position: absolute;
        right: 8px;
        top: 8px;
      }

      .voice-btn {
        width: 32px;
        height: 32px;
        background: rgba(255, 255, 255, 0.9);
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      }
    }
  `]
})
export class VoiceInputComponent implements OnInit, OnDestroy, ControlValueAccessor {
  @Input() consultationId?: number;
  @Input() fieldName?: string;
  @Input() userId?: number;
  @Input() iconSize: number = 18;
  @Input() showInterimTranscript: boolean = false;
  @Input() showUnsupportedMessage: boolean = true;
  @Input() appendMode: boolean = true; // true = ajoute au texte existant, false = remplace
  
  @Output() transcriptReceived = new EventEmitter<string>();
  @Output() recordingStarted = new EventEmitter<void>();
  @Output() recordingStopped = new EventEmitter<string>();
  @Output() error = new EventEmitter<string>();

  isRecording = false;
  isSupported = false;
  interimTranscript = '';
  errorMessage = '';
  recordingDuration = 0;

  private value = '';
  private subscriptions: Subscription[] = [];
  private durationInterval: any;
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(private speechService: SpeechRecognitionService) {
    this.isSupported = speechService.isSupported;
  }

  ngOnInit(): void {
    this.setupSubscriptions();
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
    this.clearDurationInterval();
    if (this.isRecording) {
      this.speechService.stop();
    }
  }

  private setupSubscriptions(): void {
    // Écouter l'état d'enregistrement
    this.subscriptions.push(
      this.speechService.listening$.subscribe(isListening => {
        this.isRecording = isListening;
        if (isListening) {
          this.startDurationCounter();
          this.recordingStarted.emit();
        } else {
          this.clearDurationInterval();
        }
      })
    );

    // Écouter les transcriptions
    this.subscriptions.push(
      this.speechService.transcripts$.subscribe(event => {
        this.handleTranscript(event);
      })
    );

    // Écouter les erreurs
    this.subscriptions.push(
      this.speechService.errors$.subscribe(error => {
        this.errorMessage = error;
        this.error.emit(error);
        setTimeout(() => this.errorMessage = '', 5000);
      })
    );
  }

  private handleTranscript(event: SpeechRecognitionEvent): void {
    if (event.isFinal) {
      // Transcription finale
      const newText = event.transcript.trim();
      
      if (this.appendMode && this.value) {
        // Ajouter à la fin avec un espace
        this.value = this.value.trim() + ' ' + newText;
      } else {
        this.value = newText;
      }

      this.onChange(this.value);
      this.transcriptReceived.emit(newText);
      this.interimTranscript = '';
    } else {
      // Transcription intermédiaire
      this.interimTranscript = event.transcript;
    }
  }

  toggleRecording(): void {
    if (!this.isSupported) return;

    if (this.isRecording) {
      this.speechService.stop();
      this.recordingStopped.emit(this.value);
    } else {
      this.errorMessage = '';
      this.speechService.start(this.consultationId, this.fieldName, this.userId);
    }
  }

  getButtonTitle(): string {
    if (!this.isSupported) {
      return 'Saisie vocale non disponible dans ce navigateur';
    }
    return this.isRecording ? 'Arrêter la dictée' : 'Démarrer la dictée vocale';
  }

  private startDurationCounter(): void {
    this.recordingDuration = 0;
    this.durationInterval = setInterval(() => {
      this.recordingDuration++;
    }, 1000);
  }

  private clearDurationInterval(): void {
    if (this.durationInterval) {
      clearInterval(this.durationInterval);
      this.durationInterval = null;
    }
  }

  // ControlValueAccessor implementation
  writeValue(value: string): void {
    this.value = value || '';
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    // Gérer l'état désactivé si nécessaire
  }
}
