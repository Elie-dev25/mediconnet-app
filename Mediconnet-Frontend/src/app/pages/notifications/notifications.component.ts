import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { 
  LucideAngularModule, 
  LUCIDE_ICONS, 
  LucideIconProvider,
  Bell, BellRing, Check, CheckCheck, Trash2, X, ArrowLeft,
  Calendar, CreditCard, Stethoscope, AlertTriangle, HeartPulse,
  Package, Settings, MessageCircle, CheckCircle, Filter
} from 'lucide-angular';
import { NotificationService, Notification, NotificationFilter } from '../../services/notification.service';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  providers: [
    { 
      provide: LUCIDE_ICONS, 
      useValue: new LucideIconProvider({ 
        Bell, BellRing, Check, CheckCheck, Trash2, X, ArrowLeft,
        Calendar, CreditCard, Stethoscope, AlertTriangle, HeartPulse,
        Package, Settings, MessageCircle, CheckCircle, Filter
      })
    }
  ],
  template: `
    <div class="notifications-page">
      <!-- Header -->
      <div class="page-header">
        <button class="back-btn" (click)="goBack()">
          <lucide-icon name="arrow-left" [size]="20"></lucide-icon>
        </button>
        <h1>Mes notifications</h1>
        <div class="header-actions">
          <button class="action-btn" (click)="markAllAsRead()" *ngIf="unreadCount > 0">
            <lucide-icon name="check-check" [size]="18"></lucide-icon>
            Tout marquer comme lu
          </button>
          <button class="action-btn danger" (click)="deleteAllRead()">
            <lucide-icon name="trash-2" [size]="18"></lucide-icon>
            Supprimer les lues
          </button>
        </div>
      </div>

      <!-- Filtres -->
      <div class="filters-bar">
        <button 
          class="filter-btn" 
          [class.active]="currentFilter === 'all'"
          (click)="setFilter('all')">
          Toutes ({{ totalCount }})
        </button>
        <button 
          class="filter-btn" 
          [class.active]="currentFilter === 'unread'"
          (click)="setFilter('unread')">
          Non lues ({{ unreadCount }})
        </button>
        <button 
          class="filter-btn" 
          [class.active]="currentFilter === 'read'"
          (click)="setFilter('read')">
          Lues
        </button>
      </div>

      <!-- Liste des notifications -->
      <div class="notifications-container">
        <div class="notifications-list" *ngIf="!loading; else loadingTpl">
          <div 
            *ngFor="let notif of filteredNotifications; trackBy: trackByNotification"
            class="notification-card"
            [class.unread]="!notif.lu"
            [class.priority-haute]="notif.priorite === 'haute'"
            [class.priority-urgente]="notif.priorite === 'urgente'"
            (click)="onNotificationClick(notif)"
          >
            <div class="notif-icon" [style.background-color]="getIconBackground(notif.type)">
              <lucide-icon [name]="getIcon(notif.icone || notif.type)" [size]="20"></lucide-icon>
            </div>
            <div class="notif-content">
              <div class="notif-header">
                <span class="notif-title">{{ notif.titre }}</span>
                <span class="notif-time">{{ getTempsEcoule(notif.dateCreation) }}</span>
              </div>
              <div class="notif-message">{{ notif.message }}</div>
              <div class="notif-meta">
                <span class="notif-type">{{ getTypeLabel(notif.type) }}</span>
                <span class="notif-priority" *ngIf="notif.priorite !== 'normale'">{{ notif.priorite }}</span>
              </div>
            </div>
            <div class="notif-actions">
              <button class="notif-action" (click)="markAsRead(notif, $event)" *ngIf="!notif.lu" title="Marquer comme lu">
                <lucide-icon name="check-circle-2" [size]="16"></lucide-icon>
              </button>
              <button class="notif-action delete" (click)="deleteNotification(notif, $event)" title="Supprimer">
                <lucide-icon name="trash-2" [size]="16"></lucide-icon>
              </button>
            </div>
          </div>

          <!-- État vide -->
          <div class="empty-state" *ngIf="filteredNotifications.length === 0">
            <lucide-icon name="bell" [size]="60"></lucide-icon>
            <h3>Aucune notification</h3>
            <p>Vous n'avez pas de notifications {{ currentFilter === 'unread' ? 'non lues' : currentFilter === 'read' ? 'lues' : '' }}</p>
          </div>
        </div>

        <!-- Loading -->
        <ng-template #loadingTpl>
          <div class="loading-state">
            <div class="spinner"></div>
            <p>Chargement des notifications...</p>
          </div>
        </ng-template>
      </div>
    </div>
  `,
  styles: [`
    .notifications-page {
      min-height: 100vh;
      background: #f8fafc;
      padding: 24px;
    }

    .page-header {
      display: flex;
      align-items: center;
      gap: 16px;
      margin-bottom: 24px;
      flex-wrap: wrap;

      h1 {
        margin: 0;
        font-size: 24px;
        font-weight: 700;
        color: #1e293b;
        flex: 1;
      }
    }

    .back-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      border: none;
      background: white;
      border-radius: 10px;
      cursor: pointer;
      color: #64748b;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
      transition: all 0.2s;

      &:hover {
        background: #f1f5f9;
        color: #334155;
      }
    }

    .header-actions {
      display: flex;
      gap: 12px;
    }

    .action-btn {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 10px 16px;
      border: none;
      background: white;
      border-radius: 10px;
      cursor: pointer;
      color: #64748b;
      font-size: 13px;
      font-weight: 500;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
      transition: all 0.2s;

      &:hover {
        background: #667eea;
        color: white;
      }

      &.danger:hover {
        background: #ef4444;
      }
    }

    .filters-bar {
      display: flex;
      gap: 8px;
      margin-bottom: 20px;
      padding: 8px;
      background: white;
      border-radius: 12px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
    }

    .filter-btn {
      padding: 10px 20px;
      border: none;
      background: transparent;
      border-radius: 8px;
      cursor: pointer;
      color: #64748b;
      font-size: 14px;
      font-weight: 500;
      transition: all 0.2s;

      &:hover {
        background: #f1f5f9;
      }

      &.active {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white;
      }
    }

    .notifications-container {
      background: white;
      border-radius: 16px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
      overflow: hidden;
    }

    .notifications-list {
      max-height: calc(100vh - 240px);
      overflow-y: auto;
    }

    .notification-card {
      display: flex;
      align-items: flex-start;
      gap: 16px;
      padding: 20px 24px;
      cursor: pointer;
      transition: all 0.2s;
      border-bottom: 1px solid #f1f5f9;

      &:hover {
        background: #f8fafc;
      }

      &:last-child {
        border-bottom: none;
      }

      &.unread {
        background: #f0f4ff;

        &:hover {
          background: #e8edff;
        }
      }

      &.priority-haute {
        border-left: 4px solid #f59e0b;
      }

      &.priority-urgente {
        border-left: 4px solid #ef4444;
        background: #fef2f2;

        &:hover {
          background: #fee2e2;
        }
      }
    }

    .notif-icon {
      flex-shrink: 0;
      width: 48px;
      height: 48px;
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
    }

    .notif-content {
      flex: 1;
      min-width: 0;
    }

    .notif-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 6px;
    }

    .notif-title {
      font-weight: 600;
      font-size: 15px;
      color: #1e293b;
    }

    .notif-time {
      font-size: 12px;
      color: #94a3b8;
      white-space: nowrap;
    }

    .notif-message {
      font-size: 14px;
      color: #64748b;
      line-height: 1.5;
      margin-bottom: 8px;
    }

    .notif-meta {
      display: flex;
      gap: 8px;
    }

    .notif-type {
      font-size: 11px;
      color: #667eea;
      background: #e0e7ff;
      padding: 2px 8px;
      border-radius: 4px;
      text-transform: uppercase;
      font-weight: 600;
    }

    .notif-priority {
      font-size: 11px;
      color: #f59e0b;
      background: #fef3c7;
      padding: 2px 8px;
      border-radius: 4px;
      text-transform: uppercase;
      font-weight: 600;
    }

    .notif-actions {
      display: flex;
      flex-direction: column;
      gap: 8px;
      opacity: 0;
      transition: opacity 0.2s;
    }

    .notification-card:hover .notif-actions {
      opacity: 1;
    }

    .notif-action {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      border: none;
      background: #e2e8f0;
      border-radius: 8px;
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
      padding: 60px 20px;
      color: #94a3b8;

      lucide-icon {
        margin-bottom: 16px;
        opacity: 0.5;
      }

      h3 {
        margin: 0 0 8px 0;
        font-size: 18px;
        color: #64748b;
      }

      p {
        margin: 0;
        font-size: 14px;
      }
    }

    .spinner {
      width: 40px;
      height: 40px;
      border: 4px solid #e2e8f0;
      border-top-color: #667eea;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
      margin-bottom: 16px;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    @media (max-width: 768px) {
      .notifications-page {
        padding: 16px;
      }

      .page-header {
        flex-direction: column;
        align-items: flex-start;
      }

      .header-actions {
        width: 100%;
        flex-wrap: wrap;
      }

      .action-btn {
        flex: 1;
        justify-content: center;
      }

      .filters-bar {
        flex-wrap: wrap;
      }

      .filter-btn {
        flex: 1;
        text-align: center;
      }
    }
  `]
})
export class NotificationsComponent implements OnInit, OnDestroy {
  notifications: Notification[] = [];
  unreadCount = 0;
  totalCount = 0;
  loading = false;
  currentFilter: 'all' | 'unread' | 'read' = 'all';
  
  private destroy$ = new Subject<void>();

  get filteredNotifications(): Notification[] {
    switch (this.currentFilter) {
      case 'unread':
        return this.notifications.filter(n => !n.lu);
      case 'read':
        return this.notifications.filter(n => n.lu);
      default:
        return this.notifications;
    }
  }

  constructor(
    private notificationService: NotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // S'abonner aux notifications
    this.notificationService.notifications$
      .pipe(takeUntil(this.destroy$))
      .subscribe(notifications => {
        this.notifications = notifications;
        this.totalCount = notifications.length;
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

    // Charger toutes les notifications
    this.loadNotifications();
  }

  loadNotifications(): void {
    this.notificationService.getNotifications({ pageSize: 50 }).subscribe();
  }

  setFilter(filter: 'all' | 'unread' | 'read'): void {
    this.currentFilter = filter;
  }

  goBack(): void {
    window.history.back();
  }

  onNotificationClick(notif: Notification): void {
    if (!notif.lu) {
      this.notificationService.markAsRead(notif.idNotification).subscribe();
    }
    
    if (notif.lien) {
      this.router.navigateByUrl(notif.lien);
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

  getTempsEcoule(date: Date | string): string {
    return this.notificationService.getTempsEcoule(date);
  }

  getIcon(iconOrType: string): string {
    const iconMap: Record<string, string> = {
      'rdv': 'calendar',
      'facture': 'credit-card',
      'consultation': 'stethoscope',
      'alerte': 'alert-triangle',
      'alerte_medicale': 'heart-pulse',
      'stock': 'package',
      'systeme': 'settings',
      'message': 'message-circle',
      'rappel': 'bell',
      'validation': 'check-circle-2',
      'annulation': 'x'
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
      'validation': '#22c55e',
      'annulation': '#ef4444'
    };
    return colors[type] || '#667eea';
  }

  getTypeLabel(type: string): string {
    const labels: Record<string, string> = {
      'rdv': 'Rendez-vous',
      'facture': 'Facture',
      'consultation': 'Consultation',
      'alerte': 'Alerte',
      'alerte_medicale': 'Alerte médicale',
      'stock': 'Stock',
      'systeme': 'Système',
      'message': 'Message',
      'rappel': 'Rappel',
      'validation': 'Validation',
      'annulation': 'Annulation'
    };
    return labels[type] || type;
  }

  trackByNotification(index: number, notif: Notification): number {
    return notif.idNotification;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
