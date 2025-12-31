import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-form-section',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `
    <div class="form-section">
      <div class="section-header" *ngIf="title">
        <h3>
          <lucide-icon [name]="icon" [size]="20" *ngIf="icon"></lucide-icon>
          {{ title }}
        </h3>
        <div class="section-actions">
          <ng-content select="[section-actions]"></ng-content>
        </div>
      </div>
      <div class="section-content">
        <ng-content></ng-content>
      </div>
    </div>
  `,
  styles: [`
    .form-section {
      background: white;
      border-radius: 12px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
      overflow: hidden;
    }

    .section-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem 1.5rem;
      border-bottom: 1px solid #e2e8f0;
      background: #f8fafc;
      gap: 1rem;
      flex-wrap: wrap;

      h3 {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        margin: 0;
        font-size: 1.1rem;
        font-weight: 600;
        color: #1e293b;

        lucide-icon {
          color: #667eea;
        }
      }
    }

    .section-actions {
      display: flex;
      gap: 0.5rem;
    }

    .section-content {
      padding: 1.5rem;
    }

    @media (max-width: 768px) {
      .section-header {
        padding: 0.875rem 1rem;
        
        h3 {
          font-size: 1rem;
        }
      }

      .section-content {
        padding: 1rem;
      }
    }
  `]
})
export class FormSectionComponent {
  @Input() title: string = '';
  @Input() icon: string = '';
}
