import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

/**
 * Configuration du son de notification
 */
export interface NotificationSoundConfig {
  enabled: boolean;
  volume: number; // 0.0 à 1.0
  soundType: 'default' | 'soft' | 'chime' | 'none';
}

const DEFAULT_CONFIG: NotificationSoundConfig = {
  enabled: true,
  volume: 0.3,
  soundType: 'default'
};

const STORAGE_KEY = 'notification_sound_config';

/**
 * Service de gestion du son des notifications
 * Configurable par l'utilisateur, respecte les bonnes pratiques UX
 */
@Injectable({
  providedIn: 'root'
})
export class NotificationSoundService {
  private config: NotificationSoundConfig;
  private audioContext: AudioContext | null = null;
  private configSubject = new BehaviorSubject<NotificationSoundConfig>(DEFAULT_CONFIG);
  
  public config$ = this.configSubject.asObservable();

  constructor() {
    this.config = this.loadConfig();
    this.configSubject.next(this.config);
  }

  /**
   * Joue le son de notification selon la configuration
   */
  async playNotificationSound(priority: string = 'normale'): Promise<void> {
    if (!this.config.enabled || this.config.soundType === 'none') {
      return;
    }

    // Vérifier si l'utilisateur a interagi avec la page (requis pour l'autoplay)
    if (!this.canPlaySound()) {
      return;
    }

    try {
      // Utiliser Web Audio API pour un son généré (pas de fichier externe nécessaire)
      await this.playGeneratedSound(priority);
    } catch (error) {
      console.warn('Impossible de jouer le son de notification:', error);
    }
  }

  /**
   * Met à jour la configuration du son
   */
  updateConfig(newConfig: Partial<NotificationSoundConfig>): void {
    this.config = { ...this.config, ...newConfig };
    this.saveConfig();
    this.configSubject.next(this.config);
  }

  /**
   * Active/désactive le son
   */
  toggleSound(): void {
    this.updateConfig({ enabled: !this.config.enabled });
  }

  /**
   * Définit le volume (0.0 à 1.0)
   */
  setVolume(volume: number): void {
    const clampedVolume = Math.max(0, Math.min(1, volume));
    this.updateConfig({ volume: clampedVolume });
  }

  /**
   * Définit le type de son
   */
  setSoundType(soundType: NotificationSoundConfig['soundType']): void {
    this.updateConfig({ soundType });
  }

  /**
   * Récupère la configuration actuelle
   */
  getConfig(): NotificationSoundConfig {
    return { ...this.config };
  }

  /**
   * Teste le son avec la configuration actuelle
   */
  async testSound(): Promise<void> {
    const wasEnabled = this.config.enabled;
    this.config.enabled = true;
    await this.playNotificationSound('normale');
    this.config.enabled = wasEnabled;
  }

  /**
   * Génère et joue un son via Web Audio API
   * Approche moderne sans fichier audio externe
   */
  private async playGeneratedSound(priority: string): Promise<void> {
    // Créer ou réutiliser le contexte audio
    if (!this.audioContext) {
      this.audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
    }

    // Reprendre si suspendu (politique autoplay des navigateurs)
    if (this.audioContext.state === 'suspended') {
      await this.audioContext.resume();
    }

    const ctx = this.audioContext;
    const now = ctx.currentTime;

    // Paramètres selon le type de son et la priorité
    const soundParams = this.getSoundParams(priority);

    // Créer l'oscillateur principal
    const oscillator = ctx.createOscillator();
    const gainNode = ctx.createGain();

    oscillator.connect(gainNode);
    gainNode.connect(ctx.destination);

    // Configurer selon le type de son
    oscillator.type = soundParams.waveType;
    oscillator.frequency.setValueAtTime(soundParams.startFreq, now);
    oscillator.frequency.exponentialRampToValueAtTime(soundParams.endFreq, now + soundParams.duration);

    // Envelope ADSR simplifié pour un son agréable
    const volume = this.config.volume * soundParams.volumeMultiplier;
    gainNode.gain.setValueAtTime(0, now);
    gainNode.gain.linearRampToValueAtTime(volume, now + 0.01); // Attack
    gainNode.gain.linearRampToValueAtTime(volume * 0.7, now + 0.05); // Decay
    gainNode.gain.linearRampToValueAtTime(volume * 0.5, now + soundParams.duration - 0.1); // Sustain
    gainNode.gain.linearRampToValueAtTime(0, now + soundParams.duration); // Release

    oscillator.start(now);
    oscillator.stop(now + soundParams.duration);

    // Pour les sons "chime", ajouter une harmonique
    if (this.config.soundType === 'chime' || priority === 'urgente') {
      this.addHarmonic(ctx, now, soundParams, 1.5);
    }
  }

  /**
   * Ajoute une harmonique pour enrichir le son
   */
  private addHarmonic(
    ctx: AudioContext, 
    startTime: number, 
    params: ReturnType<typeof this.getSoundParams>,
    freqMultiplier: number
  ): void {
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();

    osc.connect(gain);
    gain.connect(ctx.destination);

    osc.type = 'sine';
    osc.frequency.setValueAtTime(params.startFreq * freqMultiplier, startTime);
    osc.frequency.exponentialRampToValueAtTime(params.endFreq * freqMultiplier, startTime + params.duration);

    const volume = this.config.volume * params.volumeMultiplier * 0.3;
    gain.gain.setValueAtTime(0, startTime);
    gain.gain.linearRampToValueAtTime(volume, startTime + 0.02);
    gain.gain.linearRampToValueAtTime(0, startTime + params.duration);

    osc.start(startTime + 0.01);
    osc.stop(startTime + params.duration);
  }

  /**
   * Paramètres du son selon le type et la priorité
   */
  private getSoundParams(priority: string): {
    waveType: OscillatorType;
    startFreq: number;
    endFreq: number;
    duration: number;
    volumeMultiplier: number;
  } {
    // Sons plus intenses pour les priorités hautes
    const priorityMultiplier = priority === 'urgente' ? 1.3 : 
                               priority === 'haute' ? 1.1 : 1.0;

    switch (this.config.soundType) {
      case 'soft':
        return {
          waveType: 'sine',
          startFreq: 440 * priorityMultiplier,
          endFreq: 520 * priorityMultiplier,
          duration: 0.15,
          volumeMultiplier: 0.4
        };

      case 'chime':
        return {
          waveType: 'sine',
          startFreq: 880 * priorityMultiplier,
          endFreq: 660 * priorityMultiplier,
          duration: 0.25,
          volumeMultiplier: 0.35
        };

      case 'default':
      default:
        return {
          waveType: 'sine',
          startFreq: 587 * priorityMultiplier, // D5
          endFreq: 784 * priorityMultiplier,   // G5
          duration: 0.18,
          volumeMultiplier: 0.5
        };
    }
  }

  /**
   * Vérifie si le son peut être joué (interaction utilisateur requise)
   */
  private canPlaySound(): boolean {
    // Les navigateurs modernes bloquent l'autoplay sans interaction
    // Le contexte audio ne peut être créé/repris qu'après une interaction
    return document.visibilityState === 'visible';
  }

  /**
   * Charge la configuration depuis le localStorage
   */
  private loadConfig(): NotificationSoundConfig {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        return { ...DEFAULT_CONFIG, ...JSON.parse(stored) };
      }
    } catch (e) {
      console.warn('Erreur chargement config son:', e);
    }
    return { ...DEFAULT_CONFIG };
  }

  /**
   * Sauvegarde la configuration dans le localStorage
   */
  private saveConfig(): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.config));
    } catch (e) {
      console.warn('Erreur sauvegarde config son:', e);
    }
  }
}
