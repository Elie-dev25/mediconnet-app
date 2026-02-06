import { Injectable, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, fromEvent, merge, Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';

export interface IdleConfig {
  idleTimeoutSeconds: number;
  warningTimeoutSeconds: number;
}

@Injectable({
  providedIn: 'root'
})
export class IdleService {
  private readonly DEFAULT_IDLE_TIMEOUT = 15 * 60; // 15 minutes
  private readonly DEFAULT_WARNING_TIMEOUT = 60; // 1 minute avant déconnexion
  
  private idleTimer: any;
  private warningTimer: any;
  private destroy$ = new Subject<void>();
  
  private isIdleSubject = new BehaviorSubject<boolean>(false);
  public isIdle$ = this.isIdleSubject.asObservable();
  
  private showWarningSubject = new BehaviorSubject<boolean>(false);
  public showWarning$ = this.showWarningSubject.asObservable();
  
  private remainingSecondsSubject = new BehaviorSubject<number>(0);
  public remainingSeconds$ = this.remainingSecondsSubject.asObservable();
  
  private config: IdleConfig = {
    idleTimeoutSeconds: this.DEFAULT_IDLE_TIMEOUT,
    warningTimeoutSeconds: this.DEFAULT_WARNING_TIMEOUT
  };
  
  private isWatching = false;
  private countdownInterval: any;

  constructor(
    private ngZone: NgZone,
    private router: Router
  ) {}

  /**
   * Configure les délais d'inactivité
   */
  configure(config: Partial<IdleConfig>): void {
    this.config = { ...this.config, ...config };
  }

  /**
   * Démarre la surveillance de l'activité utilisateur
   */
  startWatching(): void {
    if (this.isWatching) return;
    this.isWatching = true;

    console.log('🔍 IdleService: Démarrage de la surveillance d\'inactivité');
    console.log(`⏱️ Timeout: ${this.config.idleTimeoutSeconds}s, Warning: ${this.config.warningTimeoutSeconds}s`);

    this.ngZone.runOutsideAngular(() => {
      const activityEvents$ = merge(
        fromEvent(document, 'mousemove'),
        fromEvent(document, 'mousedown'),
        fromEvent(document, 'keydown'),
        fromEvent(document, 'scroll'),
        fromEvent(document, 'touchstart'),
        fromEvent(document, 'click')
      );

      activityEvents$.pipe(
        debounceTime(500),
        takeUntil(this.destroy$)
      ).subscribe((event: Event) => {
        // Ne pas réinitialiser si le warning est affiché (sauf si c'est un clic sur le bouton "Rester connecté")
        if (this.showWarningSubject.value) {
          const target = event.target as HTMLElement;
          // Ignorer les événements sauf les clics sur les boutons du modal
          if (!target?.closest('.btn-stay-active')) {
            return;
          }
        }
        this.ngZone.run(() => this.resetTimer());
      });
    });

    this.resetTimer();
  }

  /**
   * Arrête la surveillance
   */
  stopWatching(): void {
    this.isWatching = false;
    this.clearTimers();
    this.destroy$.next();
    this.destroy$.complete();
    this.destroy$ = new Subject<void>();
  }

  /**
   * Réinitialise le timer d'inactivité
   */
  resetTimer(): void {
    this.clearTimers();
    this.isIdleSubject.next(false);
    this.showWarningSubject.next(false);
    this.remainingSecondsSubject.next(0);

    const idleTime = (this.config.idleTimeoutSeconds - this.config.warningTimeoutSeconds) * 1000;
    
    this.idleTimer = setTimeout(() => {
      this.showWarning();
    }, idleTime);
  }

  /**
   * Affiche l'avertissement avant déconnexion
   */
  private showWarning(): void {
    console.log('⚠️ IdleService: Affichage du warning d\'inactivité');
    this.showWarningSubject.next(true);
    this.remainingSecondsSubject.next(this.config.warningTimeoutSeconds);
    
    this.countdownInterval = setInterval(() => {
      const remaining = this.remainingSecondsSubject.value - 1;
      this.remainingSecondsSubject.next(remaining);
      
      if (remaining <= 0) {
        this.onTimeout();
      }
    }, 1000);
  }

  /**
   * Appelé quand le timeout est atteint
   */
  private onTimeout(): void {
    this.clearTimers();
    this.isIdleSubject.next(true);
    this.showWarningSubject.next(false);
  }

  /**
   * L'utilisateur confirme qu'il est toujours actif
   */
  stayActive(): void {
    this.resetTimer();
  }

  /**
   * Nettoie tous les timers
   */
  private clearTimers(): void {
    if (this.idleTimer) {
      clearTimeout(this.idleTimer);
      this.idleTimer = null;
    }
    if (this.warningTimer) {
      clearTimeout(this.warningTimer);
      this.warningTimer = null;
    }
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
      this.countdownInterval = null;
    }
  }

  /**
   * Obtient le temps restant avant déconnexion (pour affichage)
   */
  getRemainingTime(): number {
    return this.remainingSecondsSubject.value;
  }

  /**
   * Vérifie si l'utilisateur est en état d'inactivité
   */
  isIdle(): boolean {
    return this.isIdleSubject.value;
  }
}
