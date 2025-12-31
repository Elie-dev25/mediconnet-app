import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

export interface PatientInfo {
  nom: string;
  prenom: string;
  numeroDossier?: string;
  dateNaissance?: string;
  sexe?: string;
  groupeSanguin?: string;
  telephone?: string;
  email?: string;
}

@Component({
  selector: 'app-patient-card',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `
    <div class="patient-card" [class.compact]="compact">
      <div class="patient-avatar" [class.small]="compact">
        <lucide-icon name="user" [size]="compact ? 24 : 32"></lucide-icon>
      </div>
      <div class="patient-info">
        <h2>{{ patient.prenom }} {{ patient.nom }}</h2>
        <div class="patient-meta">
          <span class="meta-item" *ngIf="patient.numeroDossier">
            <lucide-icon name="folder" [size]="14"></lucide-icon>
            {{ patient.numeroDossier }}
          </span>
          <span class="meta-item" *ngIf="patient.dateNaissance">
            <lucide-icon name="calendar" [size]="14"></lucide-icon>
            {{ calculateAge(patient.dateNaissance) }} ans
          </span>
          <span class="meta-item" *ngIf="patient.sexe">
            <lucide-icon name="user" [size]="14"></lucide-icon>
            {{ patient.sexe === 'M' ? 'Homme' : 'Femme' }}
          </span>
          <span class="meta-item blood-type" *ngIf="patient.groupeSanguin">
            <lucide-icon name="droplet" [size]="14"></lucide-icon>
            {{ patient.groupeSanguin }}
          </span>
        </div>
      </div>
      <div class="patient-actions">
        <ng-content></ng-content>
      </div>
    </div>
  `,
  styles: [`
    .patient-card {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1.25rem;
      background: white;
      border-radius: 12px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);

      &.compact {
        padding: 0.875rem;
        gap: 0.75rem;
      }
    }

    .patient-avatar {
      width: 64px;
      height: 64px;
      border-radius: 50%;
      background: linear-gradient(135deg, #667eea 0%, #5a67d8 100%);
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      flex-shrink: 0;

      &.small {
        width: 48px;
        height: 48px;
      }
    }

    .patient-info {
      flex: 1;
      min-width: 0;

      h2 {
        margin: 0 0 0.5rem 0;
        font-size: 1.25rem;
        font-weight: 600;
        color: #1e293b;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }
    }

    .patient-meta {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
    }

    .meta-item {
      display: flex;
      align-items: center;
      gap: 0.35rem;
      font-size: 0.85rem;
      color: #64748b;

      lucide-icon {
        color: #94a3b8;
      }

      &.blood-type {
        color: #dc2626;
        font-weight: 600;
      }
    }

    .patient-actions {
      display: flex;
      gap: 0.5rem;
      flex-shrink: 0;
    }

    @media (max-width: 768px) {
      .patient-card {
        flex-direction: column;
        text-align: center;

        .patient-meta {
          justify-content: center;
        }
      }
    }
  `]
})
export class PatientCardComponent {
  @Input() patient!: PatientInfo;
  @Input() compact: boolean = false;

  calculateAge(dateNaissance: string): number {
    const today = new Date();
    const birthDate = new Date(dateNaissance);
    let age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    return age;
  }
}
