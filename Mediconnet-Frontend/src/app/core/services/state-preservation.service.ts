import { Injectable } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

export interface PreservedState {
  url: string;
  timestamp: number;
  formData?: any;
  workflowStep?: number;
  scrollPosition?: { x: number; y: number };
  customData?: any;
}

@Injectable({
  providedIn: 'root'
})
export class StatePreservationService {
  private readonly STATE_KEY = 'preserved_state';
  private readonly REDIRECT_URL_KEY = 'redirect_after_login';
  private readonly STATE_EXPIRY_MS = 30 * 60 * 1000; // 30 minutes
  
  private currentUrl: string = '/';

  constructor(private router: Router) {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: NavigationEnd) => {
      this.currentUrl = event.urlAfterRedirects;
    });
  }

  /**
   * Sauvegarde l'URL de redirection pour après la connexion
   */
  saveRedirectUrl(url?: string): void {
    const urlToSave = url || this.currentUrl;
    if (urlToSave && !this.isPublicRoute(urlToSave)) {
      sessionStorage.setItem(this.REDIRECT_URL_KEY, urlToSave);
    }
  }

  /**
   * Récupère l'URL de redirection
   */
  getRedirectUrl(): string | null {
    return sessionStorage.getItem(this.REDIRECT_URL_KEY);
  }

  /**
   * Efface l'URL de redirection
   */
  clearRedirectUrl(): void {
    sessionStorage.removeItem(this.REDIRECT_URL_KEY);
  }

  /**
   * Sauvegarde l'état complet de la page
   */
  saveState(customData?: any): void {
    const state: PreservedState = {
      url: this.currentUrl,
      timestamp: Date.now(),
      scrollPosition: {
        x: window.scrollX,
        y: window.scrollY
      },
      customData
    };
    
    sessionStorage.setItem(this.STATE_KEY, JSON.stringify(state));
  }

  /**
   * Sauvegarde les données de formulaire
   */
  saveFormData(formId: string, data: any): void {
    const key = `form_${formId}`;
    sessionStorage.setItem(key, JSON.stringify({
      data,
      timestamp: Date.now()
    }));
  }

  /**
   * Récupère les données de formulaire
   */
  getFormData(formId: string): any | null {
    const key = `form_${formId}`;
    const stored = sessionStorage.getItem(key);
    if (!stored) return null;
    
    try {
      const parsed = JSON.parse(stored);
      if (Date.now() - parsed.timestamp > this.STATE_EXPIRY_MS) {
        sessionStorage.removeItem(key);
        return null;
      }
      return parsed.data;
    } catch {
      return null;
    }
  }

  /**
   * Efface les données de formulaire
   */
  clearFormData(formId: string): void {
    sessionStorage.removeItem(`form_${formId}`);
  }

  /**
   * Sauvegarde l'étape du workflow
   */
  saveWorkflowStep(workflowId: string, step: number, data?: any): void {
    const key = `workflow_${workflowId}`;
    sessionStorage.setItem(key, JSON.stringify({
      step,
      data,
      timestamp: Date.now()
    }));
  }

  /**
   * Récupère l'étape du workflow
   */
  getWorkflowStep(workflowId: string): { step: number; data?: any } | null {
    const key = `workflow_${workflowId}`;
    const stored = sessionStorage.getItem(key);
    if (!stored) return null;
    
    try {
      const parsed = JSON.parse(stored);
      if (Date.now() - parsed.timestamp > this.STATE_EXPIRY_MS) {
        sessionStorage.removeItem(key);
        return null;
      }
      return { step: parsed.step, data: parsed.data };
    } catch {
      return null;
    }
  }

  /**
   * Efface l'étape du workflow
   */
  clearWorkflowStep(workflowId: string): void {
    sessionStorage.removeItem(`workflow_${workflowId}`);
  }

  /**
   * Récupère l'état sauvegardé
   */
  getState(): PreservedState | null {
    const stored = sessionStorage.getItem(this.STATE_KEY);
    if (!stored) return null;
    
    try {
      const state: PreservedState = JSON.parse(stored);
      if (Date.now() - state.timestamp > this.STATE_EXPIRY_MS) {
        this.clearState();
        return null;
      }
      return state;
    } catch {
      return null;
    }
  }

  /**
   * Efface l'état sauvegardé
   */
  clearState(): void {
    sessionStorage.removeItem(this.STATE_KEY);
  }

  /**
   * Restaure la position de scroll
   */
  restoreScrollPosition(state: PreservedState): void {
    if (state.scrollPosition) {
      setTimeout(() => {
        window.scrollTo(state.scrollPosition!.x, state.scrollPosition!.y);
      }, 100);
    }
  }

  /**
   * Redirige vers l'URL sauvegardée après connexion
   */
  redirectAfterLogin(): void {
    const redirectUrl = this.getRedirectUrl();
    this.clearRedirectUrl();
    
    if (redirectUrl && !this.isPublicRoute(redirectUrl)) {
      this.router.navigateByUrl(redirectUrl);
    }
  }

  /**
   * Vérifie si c'est une route publique
   */
  private isPublicRoute(url: string): boolean {
    const publicRoutes = [
      '/',
      '/auth/login',
      '/auth/register',
      '/auth/confirm-email',
      '/auth/forgot-password',
      '/auth/reset-password'
    ];
    return publicRoutes.some(route => url === route || url.startsWith(route + '?'));
  }

  /**
   * Sauvegarde complète avant déconnexion pour inactivité
   */
  saveBeforeIdleLogout(): void {
    this.saveRedirectUrl();
    this.saveState();
  }

  /**
   * Nettoie toutes les données de session (déconnexion volontaire)
   */
  clearAllSessionData(): void {
    const keysToRemove: string[] = [];
    for (let i = 0; i < sessionStorage.length; i++) {
      const key = sessionStorage.key(i);
      if (key && (key.startsWith('form_') || key.startsWith('workflow_') || 
          key === this.STATE_KEY || key === this.REDIRECT_URL_KEY)) {
        keysToRemove.push(key);
      }
    }
    keysToRemove.forEach(key => sessionStorage.removeItem(key));
  }
}
