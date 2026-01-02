import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { 
  LucideAngularModule, 
  LUCIDE_ICONS, 
  LucideIconProvider,
  Bell, BellRing, Check, CheckCheck, Trash2, X,
  Calendar, CreditCard, Stethoscope, AlertTriangle, HeartPulse,
  Package, Settings, MessageCircle, CheckCircle
} from 'lucide-angular';
import { NotificationService, Notification } from '../../../services/notification.service';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  providers: [
    { 
      provide: LUCIDE_ICONS, 
      useValue: new LucideIconProvider({ 
        Bell, BellRing, Check, CheckCheck, Trash2, X,
        Calendar, CreditCard, Stethoscope, AlertTriangle, HeartPulse,
        Package, Settings, MessageCircle, CheckCircle
      })
    }
  ],
  template: `
    <div class="notification-bell-container" [class.open]="isOpen">
      <!-- Bouton cloche -->
      <button class="bell-button" (click)="toggleDropdown($event)" [class.has-unread]="unreadCount > 0">
        <lucide-icon [name]="unreadCount > 0 ? 'bell-ring' : 'bell'" [size]="22"></lucide-icon>
        <span class="badge" *ngIf="unreadCount > 0">{{ unreadCount > 99 ? '99+' : unreadCount }}</span>
      </button>

      <!-- Dropdown des notifications -->
      <div class="notifications-dropdown" *ngIf="isOpen">
        <!-- Header -->
        <div class="dropdown-header">
          <h3>Notifications</h3>
          <div class="header-actions">
            <button class="action-btn" (click)="markAllAsRead()" *ngIf="unreadCount > 0" title="Tout marquer comme lu">
              <lucide-icon name="check-check" [size]="16"></lucide-icon>
            </button>
            <button class="action-btn" (click)="deleteAllRead()" title="Supprimer les notifications lues">
              <lucide-icon name="trash-2" [size]="16"></lucide-icon>
            </button>
          </div>
        </div>

        <!-- Liste des notifications -->
        <div class="notifications-list" *ngIf="!loading; else loadingTpl">
          <div 
            *ngFor="let notif of notifications; trackBy: trackByNotification"
            class="notification-item"
            [class.unread]="!notif.lu"
            [class.priority-haute]="notif.priorite === 'haute'"
            [class.priority-urgente]="notif.priorite === 'urgente'"
            (click)="onNotificationClick(notif)"
          >
            <div class="notif-icon" [style.background-color]="getIconBackground(notif.type)">
              <lucide-icon [name]="getIcon(notif.icone || notif.type)" [size]="16"></lucide-icon>
            </div>
            <div class="notif-content">
              <div class="notif-title">{{ notif.titre }}</div>
              <div class="notif-message">{{ notif.message }}</div>
              <div class="notif-time">{{ getTempsEcoule(notif.dateCreation) }}</div>
            </div>
            <div class="notif-actions">
              <button class="notif-action" (click)="markAsRead(notif, $event)" *ngIf="!notif.lu" title="Marquer comme lu">
                <lucide-icon name="check" [size]="14"></lucide-icon>
              </button>
              <button class="notif-action delete" (click)="deleteNotification(notif, $event)" title="Supprimer">
                <lucide-icon name="x" [size]="14"></lucide-icon>
              </button>
            </div>
          </div>

          <!-- État vide -->
          <div class="empty-state" *ngIf="notifications.length === 0">
            <lucide-icon name="bell" [size]="40"></lucide-icon>
            <p>Aucune notification</p>
          </div>
        </div>

        <!-- Loading -->
        <ng-template #loadingTpl>
          <div class="loading-state">
            <div class="spinner"></div>
            <p>Chargement...</p>
          </div>
        </ng-template>

        <!-- Footer -->
        <div class="dropdown-footer" *ngIf="notifications.length > 0">
          <button class="view-all-btn" (click)="viewAllNotifications()">
            Voir toutes les notifications
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .notification-bell-container {
      position: relative;
    }

    .bell-button {
      position: relative;
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      border: none;
      background: transparent;
      border-radius: 10px;
      cursor: pointer;
      color: #64748b;
      transition: all 0.2s;

      &:hover {
        background: #f1f5f9;
        color: #334155;
      }

      &.has-unread {
        color: #667eea;
        animation: bell-shake 0.5s ease-in-out;
      }
    }

    @keyframes bell-shake {
      0%, 100% { transform: rotate(0); }
      25% { transform: rotate(10deg); }
      50% { transform: rotate(-10deg); }
      75% { transform: rotate(5deg); }
    }

    .badge {
      position: absolute;
      top: 4px;
      right: 4px;
      min-width: 18px;
      height: 18px;
      padding: 0 5px;
      background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
      color: white;
      font-size: 10px;
      font-weight: 600;
      border-radius: 9px;
      display: flex;
      align-items: center;
      justify-content: center;
      box-shadow: 0 2px 4px rgba(239, 68, 68, 0.3);
    }

    .notifications-dropdown {
      position: absolute;
      top: calc(100% + 8px);
      right: 0;
      width: 380px;
      max-height: 500px;
      background: white;
      border-radius: 16px;
      box-shadow: 0 10px 40px rgba(0, 0, 0, 0.15);
      overflow: hidden;
      z-index: 1000;
      animation: dropdown-appear 0.2s ease-out;
    }

    @keyframes dropdown-appear {
      from {
        opacity: 0;
        transform: translateY(-10px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .dropdown-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 16px 20px;
      border-bottom: 1px solid #e2e8f0;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;

      h3 {
        margin: 0;
        font-size: 16px;
        font-weight: 600;
      }

      .header-actions {
        display: flex;
        gap: 8px;
      }

      .action-btn {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 28px;
        height: 28px;
        border: none;
        background: rgba(255, 255, 255, 0.2);
        border-radius: 6px;
        cursor: pointer;
        color: white;
        transition: all 0.2s;

        &:hover {
          background: rgba(255, 255, 255, 0.3);
        }
      }
    }

    .notifications-list {
      max-height: 360px;
      overflow-y: auto;
    }

    .notification-item {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 14px 20px;
      cursor: pointer;
      transition: all 0.2s;
      border-bottom: 1px solid #f1f5f9;

      &:hover {
        background: #f8fafc;
      }

      &.unread {
        background: #f0f4ff;

        &:hover {
          background: #e8edff;
        }
      }

      &.priority-haute {
        border-left: 3px solid #f59e0b;
      }

      &.priority-urgente {
        border-left: 3px solid #ef4444;
        background: #fef2f2;

        &:hover {
          background: #fee2e2;
        }
      }
    }

    .notif-icon {
      flex-shrink: 0;
      width: 36px;
      height: 36px;
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
    }

    .notif-content {
      flex: 1;
      min-width: 0;
    }

    .notif-title {
      font-weight: 600;
      font-size: 13px;
      color: #1e293b;
      margin-bottom: 4px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .notif-message {
      font-size: 12px;
      color: #64748b;
      line-height: 1.4;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .notif-time {
      font-size: 11px;
      color: #94a3b8;
      margin-top: 6px;
    }

    .notif-actions {
      display: flex;
      flex-direction: column;
      gap: 4px;
      opacity: 0;
      transition: opacity 0.2s;
    }

    .notification-item:hover .notif-actions {
      opacity: 1;
    }

    .notif-action {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 24px;
      height: 24px;
      border: none;
      background: #e2e8f0;
      border-radius: 6px;
      cursor: pointer;
      color: #64748b;
      transition: all 0.2s;

      &:hover {
        background: #667eea;
        color: white;
      }

      &.delete:hover {
        background: #ef4444;
      }
    }

    .empty-state, .loading-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 40px 20px;
      color: #94a3b8;

      lucide-icon {
        margin-bottom: 12px;
        opacity: 0.5;
      }

      p {
        margin: 0;
        font-size: 14px;
      }
    }

    .spinner {
      width: 32px;
      height: 32px;
      border: 3px solid #e2e8f0;
      border-top-color: #667eea;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
      margin-bottom: 12px;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .dropdown-footer {
      padding: 12px 20px;
      border-top: 1px solid #e2e8f0;
      background: #f8fafc;
    }

    .view-all-btn {
      width: 100%;
      padding: 10px;
      border: none;
      background: transparent;
      color: #667eea;
      font-size: 13px;
      font-weight: 600;
      cursor: pointer;
      border-radius: 8px;
      transition: all 0.2s;

      &:hover {
        background: #e0e7ff;
      }
    }

    @media (max-width: 480px) {
      .notifications-dropdown {
        position: fixed;
        top: 60px;
        left: 10px;
        right: 10px;
        width: auto;
        max-height: calc(100vh - 80px);
      }
    }
  `]
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  isOpen = false;
  notifications: Notification[] = [];
  unreadCount = 0;
  loading = false;
  
  private destroy$ = new Subject<void>();

  constructor(
    private notificationService: NotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // S'abonner aux notifications
    this.notificationService.notifications$
      .pipe(takeUntil(this.destroy$))
      .subscribe(notifications => {
        this.notifications = notifications.slice(0, 10); // Afficher les 10 dernières
      });

    // S'abonner au compteur
    this.notificationService.unreadCount$
      .pipe(takeUntil(this.destroy$))
      .subscribe(count => {
        this.unreadCount = count;
      });

    // S'abonner au loading
    this.notificationService.loading$
      .pipe(takeUntil(this.destroy$))
      .subscribe(loading => {
        this.loading = loading;
      });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.notification-bell-container')) {
      this.isOpen = false;
    }
  }

  toggleDropdown(event: MouseEvent): void {
    event.stopPropagation();
    this.isOpen = !this.isOpen;
    
    if (this.isOpen && this.notifications.length === 0) {
      this.loadNotifications();
    }
  }

  loadNotifications(): void {
    this.notificationService.getNotifications({ pageSize: 10 }).subscribe();
  }

  onNotificationClick(notif: Notification): void {
    if (!notif.lu) {
      this.notificationService.markAsRead(notif.idNotification).subscribe();
    }
    
    if (notif.lien) {
      this.router.navigateByUrl(notif.lien);
      this.isOpen = false;
    }
  }

  markAsRead(notif: Notification, event: MouseEvent): void {
    event.stopPropagation();
    this.notificationService.markAsRead(notif.idNotification).subscribe();
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe();
  }

  deleteNotification(notif: Notification, event: MouseEvent): void {
    event.stopPropagation();
    this.notificationService.delete(notif.idNotification).subscribe();
  }

  deleteAllRead(): void {
    this.notificationService.deleteAllRead().subscribe();
  }

  viewAllNotifications(): void {
    this.isOpen = false;
    // Navigation vers une page dédiée aux notifications si elle existe
    // this.router.navigate(['/notifications']);
  }

  getTempsEcoule(date: Date | string): string {
    return this.notificationService.getTempsEcoule(date);
  }

  getIcon(iconOrType: string): string {
    const iconMap: Record<string, string> = {
      'calendar': 'calendar',
      'credit-card': 'credit-card',
      'stethoscope': 'stethoscope',
      'alert-triangle': 'alert-triangle',
      'heart-pulse': 'heart-pulse',
      'package': 'package',
      'settings': 'settings',
      'message-circle': 'message-circle',
      'check-circle': 'check-circle',
      'bell': 'bell',
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
    return iconMap[iconOrType] || 'bell';
  }

  getIconBackground(type: string): string {
    const colors: Record<string, string> = {
      'rdv': '#3b82f6',
      'facture': '#10b981',
      'consultation': '#8b5cf6',
      'alerte': '#f59e0b',
      'alerte_medicale': '#ef4444',
      'stock': '#6366f1',
      'systeme': '#64748b',
      'message': '#06b6d4',
      'rappel': '#667eea',
      'validation': '#22c55e'
    };
    return colors[type] || '#667eea';
  }

  trackByNotification(index: number, notif: Notification): number {
    return notif.idNotification;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
