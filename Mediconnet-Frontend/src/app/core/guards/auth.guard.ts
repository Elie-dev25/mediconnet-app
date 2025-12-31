import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  router.navigate(['/login']);
  return false;
};

export const roleGuard = (allowedRoles: string[]): CanActivateFn => {
  return () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
      router.navigate(['/login']);
      return false;
    }

    const user = authService.getCurrentUser();
    
    // Vérifier si le patient doit compléter des étapes obligatoires
    if (user?.role === 'patient') {
      // FLUX: Patient créé à l'accueil (avec mot de passe temporaire)
      // Caractéristiques: mustChangePassword = true
      // Processus obligatoire: 1) Déclaration → 2) Changement mot de passe → 3) Dashboard
      if (user.mustChangePassword === true) {
        // Étape 1: Si la déclaration n'est pas acceptée, bloquer et rediriger vers first-login
        if (user.declarationHonneurAcceptee !== true) {
          router.navigate(['/auth/first-login']);
          return false;
        }
        // Étape 2: Déclaration acceptée, mais mot de passe non changé → bloquer et rediriger
        router.navigate(['/auth/change-password']);
        return false;
      }
      
      // NOTE: Le flux de complétion de profil séparé a été supprimé.
      // Désormais, toutes les informations sont collectées lors de l'inscription multi-étapes.
    }
    
    if (user && allowedRoles.includes(user.role)) {
      return true;
    }

    // Rediriger vers le dashboard approprié selon le rôle
    const roleRoutes: Record<string, string> = {
      'patient': '/patient/dashboard',
      'medecin': '/medecin/dashboard',
      'infirmier': '/infirmier/dashboard',
      'administrateur': '/admin/dashboard',
      'caissier': '/caissier/dashboard',
      'accueil': '/accueil/dashboard',
      'pharmacien': '/pharmacien/dashboard',
      'biologiste': '/biologiste/dashboard'
    };

    const redirectRoute = user?.role ? roleRoutes[user.role] : '/login';
    router.navigate([redirectRoute || '/login']);
    return false;
  };
};

/**
 * Guard pour vérifier si le profil est complété
 * @deprecated Ce guard n'est plus utilisé car le profil est maintenant complété lors de l'inscription.
 * Conservé pour compatibilité, retourne toujours true pour les utilisateurs authentifiés.
 */
export const profileCompleteGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    router.navigate(['/login']);
    return false;
  }

  // Le profil est maintenant complété lors de l'inscription multi-étapes
  // Ce guard laisse passer tous les utilisateurs authentifiés
  return true;
};
