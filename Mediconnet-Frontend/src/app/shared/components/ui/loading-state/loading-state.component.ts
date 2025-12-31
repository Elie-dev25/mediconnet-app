import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-loading-state',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `
    <div class="loading-state" [class.compact]="compact" [class.inline]="inline">
      <lucide-icon name="loader-2" [size]="size" class="spinner"></lucide-icon>
      <p *ngIf="message">{{ message }}</p>
    </div>
  `,
  styles: [`
    .loading-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 4rem 2rem;
      text-align: center;
      color: #64748b;

      &.compact {
        padding: 2rem 1rem;
      }

      &.inline {
        flex-direction: row;
        gap: 0.75rem;
        padding: 1rem;
      }

      lucide-icon {
        margin-bottom: 1rem;
        color: #94a3b8;
      }

      &.inline lucide-icon {
        margin-bottom: 0;
      }

      p {
        margin: 0;
        font-size: 1rem;
      }
    }

    .spinner {
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      from { transform: rotate(0deg); }
      to { transform: rotate(360deg); }
    }
  `]
})
export class LoadingStateComponent {
  @Input() message: string = 'Chargement...';
  @Input() size: number = 48;
  @Input() compact: boolean = false;
  @Input() inline: boolean = false;
}
