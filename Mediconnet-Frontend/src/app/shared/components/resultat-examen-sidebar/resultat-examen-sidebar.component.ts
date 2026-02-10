import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule, ALL_ICONS_PROVIDER } from '../../index';
import { ExamenResultatsService, ResultatExamenDetail, DocumentResultat } from '../../../services/examen-resultats.service';

@Component({
  selector: 'app-resultat-examen-sidebar',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './resultat-examen-sidebar.component.html',
  styleUrls: ['./resultat-examen-sidebar.component.scss']
})
export class ResultatExamenSidebarComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() idBulletinExamen: number | null = null;
  @Output() close = new EventEmitter<void>();

  resultat: ResultatExamenDetail | null = null;
  isLoading = false;
  errorMessage = '';
  downloadingUuid: string | null = null;

  constructor(private examenResultatsService: ExamenResultatsService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['idBulletinExamen'] && this.idBulletinExamen && this.isOpen) {
      this.loadResultat();
    }
    if (changes['isOpen'] && !this.isOpen) {
      this.resultat = null;
      this.errorMessage = '';
    }
  }

  loadResultat(): void {
    if (!this.idBulletinExamen) return;

    this.isLoading = true;
    this.errorMessage = '';

    this.examenResultatsService.getResultatExamen(this.idBulletinExamen).subscribe({
      next: (response) => {
        if (response.success) {
          this.resultat = response.data;
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement résultat:', err);
        this.errorMessage = err.status === 403 
          ? 'Vous n\'avez pas accès à ce résultat' 
          : 'Erreur lors du chargement du résultat';
        this.isLoading = false;
      }
    });
  }

  downloadDocument(doc: DocumentResultat): void {
    if (!this.idBulletinExamen) return;

    this.downloadingUuid = doc.uuid;

    this.examenResultatsService.downloadDocument(this.idBulletinExamen, doc.uuid).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = doc.nomFichier;
        a.click();
        window.URL.revokeObjectURL(url);
        this.downloadingUuid = null;
      },
      error: (err) => {
        console.error('Erreur téléchargement:', err);
        this.downloadingUuid = null;
      }
    });
  }

  viewDocument(doc: DocumentResultat): void {
    if (!this.idBulletinExamen) return;

    this.examenResultatsService.downloadDocument(this.idBulletinExamen, doc.uuid).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        window.open(url, '_blank');
      },
      error: (err) => {
        console.error('Erreur visualisation:', err);
      }
    });
  }

  closeSidebar(): void {
    this.close.emit();
  }

  formatFileSize(bytes: number): string {
    return this.examenResultatsService.formatFileSize(bytes);
  }

  getFileIcon(mimeType: string): string {
    return this.examenResultatsService.getFileIcon(mimeType);
  }

  formatDate(date: string | undefined): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatPatientName(): string {
    if (!this.resultat?.patient) return 'Patient inconnu';
    return `${this.resultat.patient.prenom} ${this.resultat.patient.nom}`;
  }

  formatMedecinName(): string {
    if (!this.resultat?.medecin) return 'Médecin inconnu';
    return `Dr. ${this.resultat.medecin.prenom} ${this.resultat.medecin.nom}`;
  }

  canViewInBrowser(mimeType: string): boolean {
    return mimeType.startsWith('image/') || mimeType === 'application/pdf';
  }
}
