import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

export type AlertType = 'success' | 'error' | 'warning' | 'info';

@Component({
  selector: 'app-alert-message',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `
    <div class="alert" [class]="'alert-' + type" *ngIf="message">
      <lucide-icon [name]="getIcon()" [size]="20"></lucide-icon>
      <span class="alert-text">{{ message }}</span>
      <button class="alert-close" *ngIf="dismissible" (click)="onDismiss.emit()">
        <lucide-icon name="x" [size]="16"></lucide-icon>
      </button>
    </div>
  `,
  styles: [`
    .alert {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.875rem 1rem;
      border-radius: 8px;
      font-size: 0.9rem;
      margin-bottom: 1rem;
    }

    .alert-text {
      flex: 1;
    }

    .alert-close {
      background: none;
      border: none;
      padding: 0.25rem;
      cursor: pointer;
      opacity: 0.7;
      border-radius: 4px;
      display: flex;
      align-items: center;
      justify-content: center;

      &:hover {
        opacity: 1;
        background: rgba(0, 0, 0, 0.1);
      }
    }

    .alert-success {
      background: rgba(34, 197, 94, 0.1);
      color: #16a34a;
      border: 1px solid rgba(34, 197, 94, 0.2);

      .alert-close {
        color: #16a34a;
      }
    }

    .alert-error {
      background: rgba(239, 68, 68, 0.1);
      color: #dc2626;
      border: 1px solid rgba(239, 68, 68, 0.2);

      .alert-close {
        color: #dc2626;
      }
    }

    .alert-warning {
      background: rgba(245, 158, 11, 0.1);
      color: #d97706;
      border: 1px solid rgba(245, 158, 11, 0.2);

      .alert-close {
        color: #d97706;
      }
    }

    .alert-info {
      background: rgba(59, 130, 246, 0.1);
      color: #2563eb;
      border: 1px solid rgba(59, 130, 246, 0.2);

      .alert-close {
        color: #2563eb;
      }
    }
  `]
})
export class AlertMessageComponent {
  @Input() message: string = '';
  @Input() type: AlertType = 'info';
  @Input() dismissible: boolean = false;
  @Output() onDismiss = new EventEmitter<void>();

  getIcon(): string {
    switch (this.type) {
      case 'success': return 'check-circle';
      case 'error': return 'alert-circle';
      case 'warning': return 'alert-triangle';
      case 'info': return 'info';
      default: return 'info';
    }
  }
}
