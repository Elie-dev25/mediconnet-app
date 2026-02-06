import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { LucideAngularModule } from 'lucide-angular';
import { environment } from '../../../../environments/environment';
import { SoinsComplementairesComponent, SoinComplementaire } from '../soins-complementaires/soins-complementaires.component';

@Component({
  selector: 'app-soin-hospitalisation-panel',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, SoinsComplementairesComponent],
  templateUrl: './soin-hospitalisation-panel.component.html',
  styleUrl: './soin-hospitalisation-panel.component.scss'
})
export class SoinHospitalisationPanelComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() hospitalisationId: number | null = null;
  @Input() patientNom = '';
  @Input() patientPrenom = '';
  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  @ViewChild('soinsComp') soinsComp!: SoinsComplementairesComponent;

  soins: SoinComplementaire[] = [];
  isSubmitting = false;
  error: string | null = null;
  success = false;

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']) {
      if (this.isOpen) {
        this.reset();
      }
    }
  }

  reset(): void {
    this.soins = [];
    this.error = null;
    this.success = false;
    this.isSubmitting = false;
  }

  onSoinsChange(soins: SoinComplementaire[]): void {
    this.soins = soins;
  }

  get canSubmit(): boolean {
    return !this.isSubmitting && 
           this.soins.length > 0 && 
           !!this.hospitalisationId &&
           (this.soinsComp?.isValid() ?? false);
  }

  onClose(): void {
    this.close.emit();
  }

  onOverlayClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('panel-overlay')) {
      this.onClose();
    }
  }

  submit(): void {
    if (!this.canSubmit || !this.hospitalisationId) return;

    this.isSubmitting = true;
    this.error = null;

    const soinsData = this.soinsComp?.getSoinsData() || this.soins;
    let completed = 0;
    let hasError = false;

    soinsData.forEach(soin => {
      const payload = {
        typeSoin: soin.typeSoin,
        description: soin.description,
        frequence: soin.frequence || null,
        dureeJours: soin.dureeJours || 7,
        moments: soin.moments?.join(',') || null, // Legacy
        nbFoisParJour: soin.nbFoisParJour || 1,
        horairesPersonnalises: soin.horairesPersonnalises || null,
        priorite: soin.priorite,
        instructions: soin.instructions || null,
        dateDebut: new Date().toISOString()
      };

      this.http.post<{ success: boolean; message: string }>(
        `${this.apiUrl}/medecin/hospitalisation/${this.hospitalisationId}/soins`,
        payload
      ).subscribe({
        next: (response) => {
          completed++;
          if (!response.success && !hasError) {
            hasError = true;
            this.error = response.message || 'Erreur lors de l\'ajout du soin';
          }
          this.checkCompletion(completed, soinsData.length, hasError);
        },
        error: (err) => {
          completed++;
          if (!hasError) {
            hasError = true;
            this.error = err.error?.message || 'Erreur lors de l\'ajout du soin';
          }
          this.checkCompletion(completed, soinsData.length, hasError);
        }
      });
    });
  }

  private checkCompletion(completed: number, total: number, hasError: boolean): void {
    if (completed === total) {
      this.isSubmitting = false;
      if (!hasError) {
        this.success = true;
        setTimeout(() => {
          this.saved.emit();
        }, 1500);
      }
    }
  }
}
