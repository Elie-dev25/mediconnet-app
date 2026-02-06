import { Component, OnInit, OnDestroy, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { IdleService } from '../../../core/services/idle.service';
import { AuthService } from '../../../services/auth.service';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-idle-warning-modal',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `
    <div class="idle-modal-overlay" *ngIf="showWarning" (click)="stayActive()">
      <div class="idle-modal" (click)="$event.stopPropagation()">
        <div class="idle-modal-icon">
          <lucide-icon name="clock" [size]="48"></lucide-icon>
        </div>
        <h2>Session sur le point d'expirer</h2>
        <p>
          Vous êtes inactif depuis un moment. Pour des raisons de sécurité, 
          vous serez automatiquement déconnecté dans :
        </p>
        <div class="countdown">
          <span class="countdown-value">{{ remainingSeconds }}</span>
          <span class="countdown-label">secondes</span>
        </div>
        <div class="idle-modal-actions">
          <button class="btn-stay-active" (click)="stayActive()">
            <lucide-icon name="check" [size]="18"></lucide-icon>
            Rester connecté
          </button>
          <button class="btn-logout" (click)="logout()">
            <lucide-icon name="log-out" [size]="18"></lucide-icon>
            Se déconnecter
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .idle-modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0, 0, 0, 0.6);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 10000;
      backdrop-filter: blur(4px);
      animation: fadeIn 0.2s ease-out;
    }

    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }

    .idle-modal {
      background: white;
      border-radius: 16px;
      padding: 2rem;
      max-width: 420px;
      width: 90%;
      text-align: center;
      box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
      animation: slideUp 0.3s ease-out;
    }

    @keyframes slideUp {
      from { 
        opacity: 0;
        transform: translateY(20px);
      }
      to { 
        opacity: 1;
        transform: translateY(0);
      }
    }

    .idle-modal-icon {
      width: 80px;
      height: 80px;
      background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 1.5rem;
      color: white;
      animation: pulse 2s infinite;
    }

    @keyframes pulse {
      0%, 100% { transform: scale(1); }
      50% { transform: scale(1.05); }
    }

    h2 {
      color: #1f2937;
      font-size: 1.5rem;
      font-weight: 600;
      margin: 0 0 1rem;
    }

    p {
      color: #6b7280;
      font-size: 0.95rem;
      line-height: 1.6;
      margin: 0 0 1.5rem;
    }

    .countdown {
      background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);
      border-radius: 12px;
      padding: 1.5rem;
      margin-bottom: 1.5rem;
    }

    .countdown-value {
      display: block;
      font-size: 3rem;
      font-weight: 700;
      color: #d97706;
      line-height: 1;
    }

    .countdown-label {
      display: block;
      font-size: 0.875rem;
      color: #92400e;
      margin-top: 0.25rem;
    }

    .idle-modal-actions {
      display: flex;
      gap: 1rem;
      justify-content: center;
    }

    button {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 1.5rem;
      border-radius: 8px;
      font-size: 0.95rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;
      border: none;
    }

    .btn-stay-active {
      background: linear-gradient(135deg, #10b981 0%, #059669 100%);
      color: white;
    }

    .btn-stay-active:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(16, 185, 129, 0.4);
    }

    .btn-logout {
      background: #f3f4f6;
      color: #6b7280;
    }

    .btn-logout:hover {
      background: #e5e7eb;
    }
  `]
})
export class IdleWarningModalComponent implements OnInit, OnDestroy {
  showWarning = false;
  remainingSeconds = 60;
  
  private subscriptions: Subscription[] = [];

  constructor(
    private idleService: IdleService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.subscriptions.push(
      this.idleService.showWarning$.subscribe(show => {
        this.showWarning = show;
        this.cdr.detectChanges();
      }),
      this.idleService.remainingSeconds$.subscribe(seconds => {
        this.remainingSeconds = seconds;
        this.cdr.detectChanges();
      }),
      this.idleService.isIdle$.subscribe(isIdle => {
        if (isIdle) {
          this.authService.logoutDueToInactivity();
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  stayActive(): void {
    this.idleService.stayActive();
  }

  logout(): void {
    this.authService.logout(false);
  }
}
