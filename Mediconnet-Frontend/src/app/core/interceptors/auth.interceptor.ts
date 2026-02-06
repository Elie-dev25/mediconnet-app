import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { StatePreservationService } from '../services/state-preservation.service';

// Flag pour éviter les redirections multiples
let isRedirecting = false;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const stateService = inject(StatePreservationService);
  
  // La clé utilisee par AuthService est 'auth_token'
  const token = localStorage.getItem('auth_token');
  
  const requestToSend = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(requestToSend).pipe(
    catchError((err) => {
      if (err?.status === 401 && !isRedirecting) {
        // Éviter les redirections multiples si plusieurs requêtes échouent
        isRedirecting = true;
        
        console.warn('⚠️ Session expirée ou invalide, sauvegarde du contexte et déconnexion...');
        
        // Sauvegarder l'URL actuelle et l'état avant déconnexion
        const currentUrl = router.url;
        const publicRoutes = ['/', '/auth/login', '/auth/register', '/auth/confirm-email'];
        
        if (!publicRoutes.some(route => currentUrl.startsWith(route))) {
          // Sauvegarder le contexte avant déconnexion
          stateService.saveRedirectUrl(currentUrl);
          stateService.saveState();
        }
        
        localStorage.removeItem('auth_token');
        localStorage.removeItem('auth_user');
        
        // Rediriger seulement si on n'est pas déjà sur une page publique
        if (!publicRoutes.some(route => currentUrl.startsWith(route))) {
          router.navigate(['/auth/login'], { 
            queryParams: { sessionExpired: 'true' } 
          });
        }
        
        // Reset le flag après un court délai
        setTimeout(() => { isRedirecting = false; }, 1000);
      }
      return throwError(() => err);
    })
  );
};
