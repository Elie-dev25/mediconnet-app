import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { Router } from '@angular/router';
import { NotificationService } from './notification.service';

/**
 * Interface pour les données de connexion
 * Accepte email OU telephone comme identifiant
 */
export interface LoginCredentials {
  identifier: string;  // email ou telephone
  password: string;
}

/**
 * Interface pour l'enregistrement
 */
export interface RegisterCredentials {
  firstName: string;
  lastName: string;
  email: string;
  telephone: string;
  password: string;
  confirmPassword: string;
}

/**
 * Interface pour la réponse d'authentification
 */
export interface AuthResponse {
  token: string;
  idUser: string;
  nom: string;
  prenom: string;
  email: string;
  telephone?: string;
  role: string;
  message?: string;
}

/**
 * Interface pour le changement de mot de passe
 */
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

/**
 * Interface pour la réponse de changement de mot de passe
 */
export interface ChangePasswordResponse {
  success: boolean;
  message: string;
}

/**
 * Interface pour les critères de mot de passe
 */
export interface PasswordCriteria {
  hasMinLength: boolean;
  hasUppercase: boolean;
  hasLowercase: boolean;
  hasDigit: boolean;
  hasSpecialChar: boolean;
}

/**
 * Interface pour la robustesse du mot de passe
 */
export interface PasswordStrengthResponse {
  isValid: boolean;
  score: number;
  strengthLevel: string;
  errors: string[];
  criteria: PasswordCriteria;
}

/**
 * Service d'authentification centralisé
 * Gère la connexion, l'enregistrement et l'état de l'authentification
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = '/api/auth';
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USER_KEY = 'auth_user';
  
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasToken());
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  private currentUserSubject = new BehaviorSubject<any>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    private http: HttpClient, 
    private router: Router,
    private notificationService: NotificationService
  ) {
    this.loadUserFromStorage();
  }

  /**
   * Connexion utilisateur
   */
  login(credentials: LoginCredentials): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/login`, credentials).pipe(
      tap(response => this.handleAuthSuccess(response)),
      catchError(error => this.handleError(error))
    );
  }

  /**
   * Enregistrement utilisateur
   */
  register(credentials: any): Observable<AuthResponse> {
    console.log('🚀 AuthService.register() - Appel API');
    console.log('URL cible:', `${this.API_URL}/register`);
    console.log('Données:', credentials);
    
    return this.http.post<AuthResponse>(`${this.API_URL}/register`, credentials).pipe(
      tap(response => {
        console.log('✅ AuthService: Réponse reçue:', response);
        this.handleAuthSuccess(response);
      }),
      catchError(error => {
        console.error('❌ AuthService: Erreur HTTP:', error);
        return this.handleError(error);
      })
    );
  }

  /**
   * Renvoyer l'email de confirmation
   */
  resendConfirmationEmail(email: string): Observable<any> {
    return this.http.post<any>(`${this.API_URL}/resend-confirmation`, { email }).pipe(
      catchError(error => this.handleError(error))
    );
  }

  /**
   * Déconnexion utilisateur
   */
  logout(): void {
    // Déconnecter SignalR des notifications
    this.notificationService.stopConnection();
    
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.isAuthenticatedSubject.next(false);
    this.currentUserSubject.next(null);
    this.router.navigate(['/']);
  }

  /**
   * Obtenir le token d'authentification
   */
  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  /**
   * Vérifier si l'utilisateur est authentifié
   */
  isAuthenticated(): boolean {
    return this.hasToken();
  }

  /**
   * Obtenir l'utilisateur courant
   */
  getCurrentUser(): any {
    return this.currentUserSubject.value;
  }

  /**
   * Gestion succès authentification
   */
  private handleAuthSuccess(response: AuthResponse): void {
    // Ne stocker le token que s'il existe (pas de token si email non confirmé)
    if (response.token) {
      localStorage.setItem(this.TOKEN_KEY, response.token);
      this.isAuthenticatedSubject.next(true);
      
      // Note: declarationHonneurAcceptee doit être false par défaut pour les patients créés à l'accueil
      // Pour les auto-inscrits, cette valeur n'est pas pertinente car ils passent par complete-profile
      const user = {
        idUser: response.idUser,
        nom: response.nom,
        prenom: response.prenom,
        email: response.email,
        telephone: response.telephone,
        role: response.role,
        emailConfirmed: (response as any).emailConfirmed ?? true,
        profileCompleted: (response as any).profileCompleted ?? false,
        mustChangePassword: (response as any).mustChangePassword ?? false,
        declarationHonneurAcceptee: (response as any).declarationHonneurAcceptee ?? false,
        requiresFirstLogin: (response as any).requiresFirstLogin ?? false
      };
      
      // Stocker l'utilisateur dans localStorage pour persistance
      localStorage.setItem(this.USER_KEY, JSON.stringify(user));
      this.currentUserSubject.next(user);
      
      // Initialiser la connexion SignalR pour les notifications
      this.notificationService.startConnection(response.token);
    }
    // Si pas de token (ex: email non confirmé), ne rien stocker
  }

  /**
   * Met à jour le statut de complétion du profil
   */
  updateProfileCompleted(completed: boolean): void {
    const user = this.getCurrentUser();
    if (user) {
      user.profileCompleted = completed;
      localStorage.setItem(this.USER_KEY, JSON.stringify(user));
      this.currentUserSubject.next(user);
    }
  }

  /**
   * Met à jour le token d'authentification (après première connexion par exemple)
   */
  updateToken(newToken: string): void {
    localStorage.setItem(this.TOKEN_KEY, newToken);
    this.isAuthenticatedSubject.next(true);
    
    // Mettre à jour les flags de l'utilisateur
    const user = this.getCurrentUser();
    if (user) {
      user.mustChangePassword = false;
      user.declarationHonneurAcceptee = true;
      localStorage.setItem(this.USER_KEY, JSON.stringify(user));
      this.currentUserSubject.next(user);
    }
  }

  /**
   * Met à jour partiellement les informations utilisateur stockées localement
   */
  updateUserInfo(updates: Partial<{ mustChangePassword: boolean; declarationHonneurAcceptee: boolean; profileCompleted: boolean }>): void {
    const user = this.getCurrentUser();
    if (user) {
      const updatedUser = { ...user, ...updates };
      localStorage.setItem(this.USER_KEY, JSON.stringify(updatedUser));
      this.currentUserSubject.next(updatedUser);
    }
  }

  /**
   * Gestion des erreurs
   */
  private handleError(error: any): Observable<never> {
    let errorMessage = 'Une erreur est survenue lors de l\'authentification';
    
    if (error.error instanceof ErrorEvent) {
      errorMessage = error.error.message;
    } else if (error.error?.message) {
      // Message d'erreur du serveur (400, 500, etc.)
      errorMessage = error.error.message;
    } else if (error.status === 401) {
      errorMessage = 'Identifiants incorrects';
    } else if (error.status === 409 || error.status === 400) {
      errorMessage = 'Cet email ou numéro de téléphone est déjà utilisé';
    }
    
    console.error('Auth error:', errorMessage, error);
    return throwError(() => new Error(errorMessage));
  }

  /**
   * Vérifier la présence du token
   */
  private hasToken(): boolean {
    return !!localStorage.getItem(this.TOKEN_KEY);
  }

  /**
   * Charger l'utilisateur depuis le stockage local
   */
  private loadUserFromStorage(): void {
    const token = this.getToken();
    const userJson = localStorage.getItem(this.USER_KEY);
    
    if (token && userJson) {
      try {
        const user = JSON.parse(userJson);
        this.currentUserSubject.next(user);
        this.isAuthenticatedSubject.next(true);
        console.log('✅ Session restaurée:', user.email);
        
        // Reconnecter SignalR pour les notifications
        this.notificationService.startConnection(token);
      } catch (e) {
        console.error('Erreur parsing user:', e);
        this.clearStorage();
      }
    } else if (token) {
      // Token existe mais pas d'user, nettoyer le stockage
      console.warn('Token sans user, nettoyage...');
      this.clearStorage();
    }
  }

  /**
   * Nettoyer le stockage sans navigation
   */
  private clearStorage(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.isAuthenticatedSubject.next(false);
    this.currentUserSubject.next(null);
  }

  /**
   * Changer le mot de passe de l'utilisateur connecté
   */
  changePassword(request: ChangePasswordRequest): Observable<ChangePasswordResponse> {
    return this.http.post<ChangePasswordResponse>(`${this.API_URL}/change-password`, request).pipe(
      tap(response => {
        if (response.success) {
          console.log('✅ Mot de passe changé avec succès');
        }
      }),
      catchError(error => {
        console.error('❌ Erreur lors du changement de mot de passe:', error);
        return throwError(() => error.error || { success: false, message: 'Erreur lors du changement de mot de passe' });
      })
    );
  }

  /**
   * Vérifier la robustesse d'un mot de passe
   */
  checkPasswordStrength(password: string): Observable<PasswordStrengthResponse> {
    return this.http.post<PasswordStrengthResponse>(`${this.API_URL}/check-password-strength`, { password }).pipe(
      catchError(error => {
        console.error('❌ Erreur lors de la vérification:', error);
        return throwError(() => error.error || { isValid: false, errors: ['Erreur de vérification'] });
      })
    );
  }
}
