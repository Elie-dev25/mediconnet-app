import { Injectable, OnDestroy, Inject, forwardRef } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { takeUntil, tap, catchError } from 'rxjs/operators';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { NotificationSoundService } from './notification-sound.service';

// ==================== INTERFACES ====================

export interface Notification {
  idNotification: number;
  idUser: number;
  type: string;
  titre: string;
  message: string;
  lien?: string;
  icone?: string;
  priorite: string;
  lu: boolean;
  dateLecture?: Date;
  dateCreation: Date;
  metadata?: string;
  tempsEcoule?: string;
}

export interface NotificationFilter {
  type?: string;
  lu?: boolean;
  priorite?: string;
  dateDebut?: Date;
  dateFin?: Date;
  page?: number;
  pageSize?: number;
}

export interface NotificationListResult {
  notifications: Notification[];
  totalCount: number;
  unreadCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateNotificationRequest {
  idUser: number;
  type: string;
  titre: string;
  message: string;
  lien?: string;
  icone?: string;
  priorite?: string;
  dateExpiration?: Date;
  metadata?: string;
  sendRealTime?: boolean;
}

// ==================== SERVICE ====================

@Injectable({
  providedIn: 'root'
})
export class NotificationService implements OnDestroy {
  private readonly apiUrl = `${environment.apiUrl}/notification`;
  private readonly hubUrl = `${environment.apiUrl.replace('/api', '')}/hubs/notifications`;
  
  private hubConnection: signalR.HubConnection | null = null;
  private destroy$ = new Subject<void>();
  
  // État réactif
  private notificationsSubject = new BehaviorSubject<Notification[]>([]);
  private unreadCountSubject = new BehaviorSubject<number>(0);
  private loadingSubject = new BehaviorSubject<boolean>(false);
  private connectedSubject = new BehaviorSubject<boolean>(false);

  // Observables publics
  public notifications$ = this.notificationsSubject.asObservable();
  public unreadCount$ = this.unreadCountSubject.asObservable();
  public loading$ = this.loadingSubject.asObservable();
  public connected$ = this.connectedSubject.asObservable();

  constructor(
    private http: HttpClient,
    private soundService: NotificationSoundService
  ) {}

  // ==================== CONNEXION SIGNALR ====================

  /**
   * Initialise la connexion SignalR pour les notifications temps réel
   */
  async startConnection(token: string): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Gestionnaires d'événements SignalR
    this.hubConnection.on('NewNotification', (notification: Notification) => {
      this.handleNewNotification(notification);
    });

    this.hubConnection.on('UnreadCountUpdate', (count: number) => {
      this.unreadCountSubject.next(count);
    });

    this.hubConnection.on('NotificationMarkedAsRead', (notificationId: number) => {
      this.updateNotificationReadStatus(notificationId, true);
    });

    this.hubConnection.onreconnecting(() => {
      console.log('Reconnexion aux notifications...');
      this.connectedSubject.next(false);
    });

    this.hubConnection.onreconnected(() => {
      console.log('Reconnecté aux notifications');
      this.connectedSubject.next(true);
      this.loadUnreadCount();
    });

    this.hubConnection.onclose(() => {
      console.log('Déconnecté des notifications');
      this.connectedSubject.next(false);
    });

    try {
      await this.hubConnection.start();
      console.log('Connecté aux notifications temps réel');
      this.connectedSubject.next(true);
      await this.loadUnreadCount();
    } catch (err) {
      console.error('Erreur connexion SignalR notifications:', err);
      this.connectedSubject.next(false);
    }
  }

  /**
   * Arrête la connexion SignalR
   */
  async stopConnection(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.hubConnection = null;
      this.connectedSubject.next(false);
    }
  }

  // ==================== API CALLS ====================

  /**
   * Récupère les notifications de l'utilisateur
   */
  getNotifications(filter?: NotificationFilter): Observable<NotificationListResult> {
    let params = new HttpParams();
    
    if (filter) {
      if (filter.type) params = params.set('type', filter.type);
      if (filter.lu !== undefined) params = params.set('lu', filter.lu.toString());
      if (filter.priorite) params = params.set('priorite', filter.priorite);
      if (filter.page) params = params.set('page', filter.page.toString());
      if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    }

    this.loadingSubject.next(true);
    
    return this.http.get<NotificationListResult>(this.apiUrl, { params }).pipe(
      tap(result => {
        this.notificationsSubject.next(result.notifications);
        this.unreadCountSubject.next(result.unreadCount);
        this.loadingSubject.next(false);
      }),
      catchError(err => {
        this.loadingSubject.next(false);
        throw err;
      })
    );
  }

  /**
   * Charge le nombre de notifications non lues
   */
  loadUnreadCount(): Promise<void> {
    return new Promise((resolve, reject) => {
      this.http.get<{ count: number }>(`${this.apiUrl}/unread-count`).subscribe({
        next: (result) => {
          this.unreadCountSubject.next(result.count);
          resolve();
        },
        error: (err) => {
          console.error('Erreur chargement compteur notifications:', err);
          reject(err);
        }
      });
    });
  }

  /**
   * Récupère une notification par ID
   */
  getById(id: number): Observable<Notification> {
    return this.http.get<Notification>(`${this.apiUrl}/${id}`);
  }

  /**
   * Marque une notification comme lue
   */
  markAsRead(id: number): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${id}/read`, {}).pipe(
      tap(() => {
        this.updateNotificationReadStatus(id, true);
        const currentCount = this.unreadCountSubject.value;
        if (currentCount > 0) {
          this.unreadCountSubject.next(currentCount - 1);
        }
      })
    );
  }

  /**
   * Marque toutes les notifications comme lues
   */
  markAllAsRead(): Observable<{ message: string; count: number }> {
    return this.http.patch<{ message: string; count: number }>(`${this.apiUrl}/read-all`, {}).pipe(
      tap(() => {
        const notifications = this.notificationsSubject.value.map(n => ({ ...n, lu: true }));
        this.notificationsSubject.next(notifications);
        this.unreadCountSubject.next(0);
      })
    );
  }

  /**
   * Marque plusieurs notifications comme lues
   */
  markMultipleAsRead(ids: number[]): Observable<{ message: string; count: number }> {
    return this.http.patch<{ message: string; count: number }>(`${this.apiUrl}/read-multiple`, { ids }).pipe(
      tap(() => {
        const notifications = this.notificationsSubject.value.map(n => 
          ids.includes(n.idNotification) ? { ...n, lu: true } : n
        );
        this.notificationsSubject.next(notifications);
      })
    );
  }

  /**
   * Supprime une notification
   */
  delete(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`).pipe(
      tap(() => {
        const notifications = this.notificationsSubject.value.filter(n => n.idNotification !== id);
        this.notificationsSubject.next(notifications);
      })
    );
  }

  /**
   * Supprime toutes les notifications lues
   */
  deleteAllRead(): Observable<{ message: string; count: number }> {
    return this.http.delete<{ message: string; count: number }>(`${this.apiUrl}/read`).pipe(
      tap(() => {
        const notifications = this.notificationsSubject.value.filter(n => !n.lu);
        this.notificationsSubject.next(notifications);
      })
    );
  }

  // ==================== HELPERS ====================

  private handleNewNotification(notification: Notification): void {
    const currentNotifications = this.notificationsSubject.value;
    this.notificationsSubject.next([notification, ...currentNotifications]);
    
    // Jouer le son de notification selon la priorité
    this.soundService.playNotificationSound(notification.priorite);
    
    // Afficher une notification système si disponible
    this.showBrowserNotification(notification);
  }

  private updateNotificationReadStatus(id: number, lu: boolean): void {
    const notifications = this.notificationsSubject.value.map(n => 
      n.idNotification === id ? { ...n, lu } : n
    );
    this.notificationsSubject.next(notifications);
  }

  /**
   * Affiche une notification navigateur si autorisé
   */
  private async showBrowserNotification(notification: Notification): Promise<void> {
    // Vérifier si les notifications navigateur sont supportées et autorisées
    if (!('Notification' in window)) return;
    
    if (Notification.permission === 'granted') {
      try {
        new Notification(notification.titre, {
          body: notification.message,
          icon: '/assets/images/logo-icon.png',
          tag: `notif-${notification.idNotification}`,
          silent: true // Le son est géré par notre service
        });
      } catch (e) {
        // Service Worker non disponible ou erreur
      }
    }
  }

  /**
   * Demande la permission pour les notifications navigateur
   */
  async requestBrowserNotificationPermission(): Promise<boolean> {
    if (!('Notification' in window)) return false;
    
    if (Notification.permission === 'granted') return true;
    if (Notification.permission === 'denied') return false;
    
    const permission = await Notification.requestPermission();
    return permission === 'granted';
  }

  /**
   * Calcule le temps écoulé depuis la création
   */
  getTempsEcoule(dateCreation: Date | string): string {
    const date = new Date(dateCreation);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return "À l'instant";
    if (diffMins < 60) return `Il y a ${diffMins} min`;
    if (diffHours < 24) return `Il y a ${diffHours}h`;
    if (diffDays < 7) return `Il y a ${diffDays}j`;
    return date.toLocaleDateString('fr-FR');
  }

  /**
   * Retourne l'icône par défaut selon le type
   */
  getIconForType(type: string): string {
    const icons: Record<string, string> = {
      'rdv': 'calendar',
      'facture': 'credit-card',
      'consultation': 'stethoscope',
      'alerte': 'alert-triangle',
      'alerte_medicale': 'heart-pulse',
      'stock': 'package',
      'systeme': 'settings',
      'message': 'message-circle',
      'rappel': 'bell',
      'validation': 'check-circle'
    };
    return icons[type] || 'bell';
  }

  /**
   * Retourne la couleur selon la priorité
   */
  getColorForPriority(priorite: string): string {
    const colors: Record<string, string> = {
      'basse': '#64748b',
      'normale': '#3b82f6',
      'haute': '#f59e0b',
      'urgente': '#ef4444'
    };
    return colors[priorite] || colors['normale'];
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.stopConnection();
  }
}
