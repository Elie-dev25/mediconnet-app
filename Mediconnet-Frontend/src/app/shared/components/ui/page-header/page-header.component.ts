import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `
    <div class="page-header">
      <button class="btn-back" (click)="onBack.emit()" *ngIf="showBackButton">
        <lucide-icon name="arrow-left" [size]="20"></lucide-icon>
        {{ backLabel }}
      </button>
      <div class="header-content">
        <h1>
          <lucide-icon [name]="icon" [size]="28" *ngIf="icon"></lucide-icon>
          {{ title }}
        </h1>
        <p class="subtitle" *ngIf="subtitle">{{ subtitle }}</p>
      </div>
      <div class="header-actions">
        <ng-content></ng-content>
      </div>
    </div>
  `,
  styles: [`
    .page-header {
      display: flex;
      align-items: flex-start;
      gap: 1.5rem;
      margin-bottom: 1.5rem;
      flex-wrap: wrap;
    }

    .btn-back {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.6rem 1rem;
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      color: #64748b;
      cursor: pointer;
      transition: all 0.2s;
      font-size: 0.9rem;
      flex-shrink: 0;

      &:hover {
        background: #f8fafc;
        color: #667eea;
      }
    }

    .header-content {
      flex: 1;
      min-width: 200px;

      h1 {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        margin: 0;
        font-size: 1.5rem;
        font-weight: 700;
        color: #1e293b;

        lucide-icon {
          color: #667eea;
        }
      }

      .subtitle {
        margin: 0.5rem 0 0 0;
        color: #64748b;
        font-size: 0.95rem;
      }
    }

    .header-actions {
      display: flex;
      gap: 0.75rem;
      align-items: center;
    }

    @media (max-width: 768px) {
      .page-header {
        flex-direction: column;
        gap: 1rem;
      }

      .header-content h1 {
        font-size: 1.25rem;
      }
    }
  `]
})
export class PageHeaderComponent {
  @Input() title: string = '';
  @Input() subtitle: string = '';
  @Input() icon: string = '';
  @Input() showBackButton: boolean = true;
  @Input() backLabel: string = 'Retour';
  @Output() onBack = new EventEmitter<void>();
}
