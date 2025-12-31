import { Injectable, NgZone } from '@angular/core';
import { BehaviorSubject, Observable, Subject } from 'rxjs';

export interface SpeechRecognitionEvent {
  transcript: string;
  isFinal: boolean;
  confidence: number;
  language?: string;
}

export interface VoiceSessionInfo {
  sessionId: string;
  startTime: Date;
  endTime?: Date;
  consultationId?: number;
  fieldName?: string;
  userId?: number;
}

export type SupportedLanguage = 'fr-FR' | 'en-US' | 'auto';

@Injectable({
  providedIn: 'root'
})
export class SpeechRecognitionService {
  private recognition: any = null;
  private isListening$ = new BehaviorSubject<boolean>(false);
  private transcript$ = new Subject<SpeechRecognitionEvent>();
  private error$ = new Subject<string>();
  private currentSession: VoiceSessionInfo | null = null;
  private shouldContinue = false;
  private restartTimeout: any = null;
  private isStarting = false; // Flag pour éviter les appels multiples pendant le démarrage async
  
  readonly isSupported: boolean;
  private currentLanguage: SupportedLanguage = 'fr-FR';

  constructor(private ngZone: NgZone) {
    const SpeechRecognition = (window as any).SpeechRecognition || 
                              (window as any).webkitSpeechRecognition;
    
    this.isSupported = !!SpeechRecognition;
    
    if (this.isSupported) {
      this.recognition = new SpeechRecognition();
      this.setupRecognition();
    }
  }

  private setupRecognition(): void {
    if (!this.recognition) return;

    this.recognition.continuous = true;
    this.recognition.interimResults = true;
    this.recognition.maxAlternatives = 1;

    this.recognition.onstart = () => {
      this.ngZone.run(() => {
        console.log('[VoiceRecognition] Started - Language:', this.currentLanguage);
        this.isListening$.next(true);
      });
    };

    this.recognition.onend = () => {
      this.ngZone.run(() => {
        console.log('[VoiceRecognition] Ended - shouldContinue:', this.shouldContinue);
        
        // Redémarrer automatiquement si l'utilisateur n'a pas arrêté manuellement
        if (this.shouldContinue) {
          this.clearRestartTimeout();
          this.restartTimeout = setTimeout(() => {
            if (this.shouldContinue) {
              console.log('[VoiceRecognition] Auto-restarting...');
              try {
                this.recognition.start();
              } catch (e) {
                console.error('[VoiceRecognition] Restart failed:', e);
                this.shouldContinue = false;
                this.isListening$.next(false);
              }
            }
          }, 100);
        } else {
          this.isListening$.next(false);
          if (this.currentSession) {
            this.currentSession.endTime = new Date();
          }
        }
      });
    };

    this.recognition.onresult = (event: any) => {
      this.ngZone.run(() => {
        const result = event.results[event.results.length - 1];
        const transcript = result[0].transcript;
        const isFinal = result.isFinal;
        const confidence = result[0].confidence || 0;

        console.log('[VoiceRecognition] Result:', { transcript, isFinal, confidence });

        this.transcript$.next({
          transcript,
          isFinal,
          confidence,
          language: this.currentLanguage
        });
      });
    };

    this.recognition.onerror = (event: any) => {
      this.ngZone.run(() => {
        console.error('[VoiceRecognition] Error:', event.error);
        let errorMessage = 'Erreur de reconnaissance vocale';
        let shouldStop = true;
        
        switch (event.error) {
          case 'no-speech':
            errorMessage = 'Aucune parole détectée - Parlez dans le microphone';
            shouldStop = false; // Continue listening
            break;
          case 'audio-capture':
            errorMessage = 'Microphone non disponible';
            break;
          case 'not-allowed':
            errorMessage = 'Accès au microphone refusé. Autorisez l\'accès dans les paramètres du navigateur.';
            break;
          case 'network':
            errorMessage = 'Erreur réseau - Vérifiez votre connexion';
            break;
          case 'aborted':
            errorMessage = 'Reconnaissance annulée';
            shouldStop = false;
            break;
          case 'service-not-available':
            errorMessage = 'Service de reconnaissance vocale non disponible';
            break;
        }
        
        if (shouldStop) {
          this.shouldContinue = false;
          this.isListening$.next(false);
        }
        
        this.error$.next(errorMessage);
      });
    };

    this.recognition.onspeechstart = () => {
      console.log('[VoiceRecognition] Speech detected');
    };

    this.recognition.onspeechend = () => {
      console.log('[VoiceRecognition] Speech ended');
    };
  }

  private clearRestartTimeout(): void {
    if (this.restartTimeout) {
      clearTimeout(this.restartTimeout);
      this.restartTimeout = null;
    }
  }

  /**
   * Définit la langue de reconnaissance
   */
  setLanguage(lang: SupportedLanguage): void {
    this.currentLanguage = lang;
    if (this.recognition) {
      // Pour 'auto', on utilise fr-FR par défaut mais le service détecte automatiquement
      this.recognition.lang = lang === 'auto' ? 'fr-FR' : lang;
    }
  }

  /**
   * Retourne la langue actuelle
   */
  getLanguage(): SupportedLanguage {
    return this.currentLanguage;
  }

  /**
   * Démarre la reconnaissance vocale
   * @param consultationId ID de la consultation (traçabilité)
   * @param fieldName Nom du champ ciblé
   * @param userId ID de l'utilisateur
   * @param language Langue de reconnaissance (défaut: fr-FR)
   */
  start(consultationId?: number, fieldName?: string, userId?: number, language?: SupportedLanguage): boolean {
    console.log('[VoiceRecognition] start() called', { consultationId, fieldName, language });
    
    if (!this.isSupported) {
      console.error('[VoiceRecognition] Not supported');
      this.error$.next('La reconnaissance vocale n\'est pas supportée par ce navigateur. Utilisez Chrome ou Edge.');
      return false;
    }

    // Éviter les appels multiples pendant le démarrage async ou si déjà en écoute
    if (this.isListening$.value || this.isStarting) {
      console.log('[VoiceRecognition] Already listening or starting, ignoring start');
      return false;
    }

    this.isStarting = true;

    // Définir la langue si spécifiée
    if (language) {
      this.setLanguage(language);
    }
    
    // Appliquer la langue au recognition
    this.recognition.lang = this.currentLanguage === 'auto' ? 'fr-FR' : this.currentLanguage;

    // Créer une nouvelle session
    this.currentSession = {
      sessionId: this.generateSessionId(),
      startTime: new Date(),
      consultationId,
      fieldName,
      userId
    };

    this.shouldContinue = true;
    this.clearRestartTimeout();

    // Demander d'abord la permission du microphone
    navigator.mediaDevices.getUserMedia({ audio: true })
      .then((stream) => {
        console.log('[VoiceRecognition] Microphone permission granted');
        // Arrêter le stream immédiatement, on utilise l'API SpeechRecognition
        stream.getTracks().forEach(track => track.stop());
        
        try {
          console.log('[VoiceRecognition] Starting recognition with language:', this.recognition.lang);
          this.recognition.start();
          this.isStarting = false; // Reset après démarrage réussi
        } catch (e: any) {
          console.error('[VoiceRecognition] Start error:', e);
          this.shouldContinue = false;
          this.isStarting = false; // Reset en cas d'erreur
          
          if (!e.message?.includes('already started')) {
            this.error$.next('Impossible de démarrer la reconnaissance vocale.');
          }
        }
      })
      .catch((err) => {
        console.error('[VoiceRecognition] Microphone permission denied:', err);
        this.shouldContinue = false;
        this.isStarting = false; // Reset en cas d'erreur permission
        this.error$.next('Accès au microphone refusé. Autorisez l\'accès dans les paramètres du navigateur.');
      });

    return true;
  }

  /**
   * Arrête la reconnaissance vocale
   */
  stop(): void {
    console.log('[VoiceRecognition] Stop requested');
    this.shouldContinue = false;
    this.isStarting = false; // Reset le flag de démarrage
    this.clearRestartTimeout();
    
    if (this.recognition) {
      try {
        this.recognition.stop();
      } catch (e) {
        console.log('[VoiceRecognition] Stop error (ignored):', e);
      }
    }
    
    // Forcer l'état à false immédiatement
    this.isListening$.next(false);
  }

  /**
   * Bascule l'état de la reconnaissance vocale
   */
  toggle(consultationId?: number, fieldName?: string, userId?: number, language?: SupportedLanguage): boolean {
    if (this.isListening$.value || this.shouldContinue) {
      this.stop();
      return false;
    } else {
      return this.start(consultationId, fieldName, userId, language);
    }
  }

  /**
   * Observable indiquant si l'écoute est active
   */
  get listening$(): Observable<boolean> {
    return this.isListening$.asObservable();
  }

  /**
   * Observable des transcriptions
   */
  get transcripts$(): Observable<SpeechRecognitionEvent> {
    return this.transcript$.asObservable();
  }

  /**
   * Observable des erreurs
   */
  get errors$(): Observable<string> {
    return this.error$.asObservable();
  }

  /**
   * Retourne les informations de la session courante
   */
  getCurrentSession(): VoiceSessionInfo | null {
    return this.currentSession;
  }

  /**
   * Vérifie si la reconnaissance vocale est en cours
   */
  get isListening(): boolean {
    return this.isListening$.value;
  }

  /**
   * Génère un ID de session unique
   */
  private generateSessionId(): string {
    return `voice_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Demande l'autorisation d'accès au microphone
   */
  async requestMicrophonePermission(): Promise<boolean> {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      // Arrêter immédiatement le stream
      stream.getTracks().forEach(track => track.stop());
      return true;
    } catch (e) {
      this.error$.next('Permission microphone refusée');
      return false;
    }
  }
}
