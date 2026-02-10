import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { AuthService } from '../../../services/auth.service';
import { LaborantinService, ExamenLaborantin, ExamenDetails, ExamensFilters } from '../../../services/laborantin.service';
import { DashboardLayoutComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { LABORANTIN_MENU_ITEMS, LABORANTIN_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-laborantin-examens',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    LucideAngularModule,
    DashboardLayoutComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './examens.component.html',
  styleUrls: ['./examens.component.scss']
})
export class LaborantinExamensComponent implements OnInit, OnDestroy {
  menuItems = LABORANTIN_MENU_ITEMS;
  sidebarTitle = LABORANTIN_SIDEBAR_TITLE;

  // Liste des examens
  examens: ExamenLaborantin[] = [];
  isLoading = true;
  
  // Pagination
  currentPage = 1;
  pageSize = 20;
  totalCount = 0;
  totalPages = 0;

  // Filtres
  filters: ExamensFilters = {};
  searchTerm = '';
  private searchSubject = new Subject<string>();

  // Sidebar détails
  showDetailsSidebar = false;
  selectedExamen: ExamenDetails | null = null;
  isLoadingDetails = false;

  // Sidebar résultat
  showResultatSidebar = false;
  resultatTexte = '';
  commentaire = '';
  fichiersResultat: File[] = [];
  isSubmittingResultat = false;
  
  // Messages
  successMessage = '';
  errorMessage = '';

  private destroy$ = new Subject<void>();

  constructor(
    private authService: AuthService,
    private laborantinService: LaborantinService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    // Écouter les changements de recherche avec debounce
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(term => {
      this.filters.recherche = term;
      this.currentPage = 1;
      this.loadExamens();
    });

    // Lire les paramètres de l'URL
    this.route.queryParams.pipe(takeUntil(this.destroy$)).subscribe(params => {
      if (params['statut']) {
        this.filters.statut = params['statut'];
      }
      if (params['urgence'] === 'true') {
        this.filters.urgence = true;
      }
      if (params['id']) {
        this.openDetails(parseInt(params['id']));
      }
      if (params['action'] === 'resultat' && params['id']) {
        this.openResultatSidebar(parseInt(params['id']));
      }
      this.loadExamens();
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadExamens(): void {
    this.isLoading = true;
    this.laborantinService.getExamens({
      ...this.filters,
      page: this.currentPage,
      pageSize: this.pageSize
    }).subscribe({
      next: (response) => {
        if (response.success) {
          this.examens = response.data.examens;
          this.totalCount = response.data.totalCount;
          this.totalPages = response.data.totalPages;
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement examens:', err);
        this.errorMessage = 'Erreur lors du chargement des examens';
        this.isLoading = false;
      }
    });
  }

  onSearchChange(term: string): void {
    this.searchSubject.next(term);
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadExamens();
  }

  clearFilters(): void {
    this.filters = {};
    this.searchTerm = '';
    this.currentPage = 1;
    this.loadExamens();
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadExamens();
    }
  }

  // Sidebar Détails
  openDetails(idBulletin: number): void {
    this.showDetailsSidebar = true;
    this.isLoadingDetails = true;
    this.selectedExamen = null;

    this.laborantinService.getExamenDetails(idBulletin).subscribe({
      next: (response) => {
        if (response.success) {
          this.selectedExamen = response.data;
        }
        this.isLoadingDetails = false;
      },
      error: (err) => {
        console.error('Erreur chargement détails:', err);
        this.errorMessage = 'Erreur lors du chargement des détails';
        this.isLoadingDetails = false;
      }
    });
  }

  closeDetailsSidebar(): void {
    this.showDetailsSidebar = false;
    this.selectedExamen = null;
  }

  // Sidebar Résultat
  openResultatSidebar(idBulletin: number): void {
    this.openDetails(idBulletin);
    this.showResultatSidebar = true;
    this.resultatTexte = '';
    this.commentaire = '';
    this.fichiersResultat = [];
  }

  closeResultatSidebar(): void {
    this.showResultatSidebar = false;
    this.resultatTexte = '';
    this.commentaire = '';
    this.fichiersResultat = [];
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      // Ajouter les nouveaux fichiers sans supprimer les existants
      const newFiles = Array.from(input.files);
      this.fichiersResultat = [...this.fichiersResultat, ...newFiles];
      // Réinitialiser l'input pour permettre de resélectionner le même fichier
      input.value = '';
    }
  }

  removeFile(index: number): void {
    this.fichiersResultat.splice(index, 1);
  }

  demarrerExamen(idBulletin: number): void {
    this.laborantinService.demarrerExamen(idBulletin).subscribe({
      next: (response) => {
        if (response.success) {
          this.successMessage = 'Examen démarré avec succès';
          this.loadExamens();
          if (this.selectedExamen?.idBulletinExamen === idBulletin) {
            this.openDetails(idBulletin);
          }
          setTimeout(() => this.successMessage = '', 3000);
        }
      },
      error: (err) => {
        console.error('Erreur démarrage examen:', err);
        this.errorMessage = err.error?.message || 'Erreur lors du démarrage';
        setTimeout(() => this.errorMessage = '', 5000);
      }
    });
  }

  submitResultat(): void {
    if (!this.selectedExamen || !this.resultatTexte.trim()) {
      this.errorMessage = 'Veuillez saisir le résultat';
      return;
    }

    this.isSubmittingResultat = true;
    this.errorMessage = '';

    this.laborantinService.enregistrerResultatComplet(
      this.selectedExamen.idBulletinExamen,
      this.resultatTexte,
      this.commentaire || null,
      this.fichiersResultat
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.successMessage = 'Résultat enregistré avec succès';
          this.closeResultatSidebar();
          this.closeDetailsSidebar();
          this.loadExamens();
          setTimeout(() => this.successMessage = '', 3000);
        }
        this.isSubmittingResultat = false;
      },
      error: (err) => {
        console.error('Erreur enregistrement résultat:', err);
        this.errorMessage = err.error?.message || 'Erreur lors de l\'enregistrement';
        this.isSubmittingResultat = false;
        setTimeout(() => this.errorMessage = '', 5000);
      }
    });
  }

  // Helpers
  getStatutClass(statut: string): string {
    switch (statut) {
      case 'prescrit': return 'status-pending';
      case 'en_cours': return 'status-progress';
      case 'termine': return 'status-done';
      case 'annule': return 'status-cancelled';
      default: return 'status-pending';
    }
  }

  getStatutLabel(statut: string): string {
    switch (statut) {
      case 'prescrit': return 'En attente';
      case 'en_cours': return 'En cours';
      case 'termine': return 'Terminé';
      case 'annule': return 'Annulé';
      default: return statut;
    }
  }

  formatPatientName(examen: ExamenLaborantin | ExamenDetails): string {
    if ('patientPrenom' in examen && examen.patientPrenom && examen.patientNom) {
      return `${examen.patientPrenom} ${examen.patientNom}`;
    }
    if ('patient' in examen && examen.patient) {
      return `${examen.patient.prenom} ${examen.patient.nom}`;
    }
    return 'Patient inconnu';
  }

  formatMedecinName(examen: ExamenLaborantin | ExamenDetails): string {
    if ('medecinPrenom' in examen && examen.medecinPrenom && examen.medecinNom) {
      return `Dr. ${examen.medecinPrenom} ${examen.medecinNom}`;
    }
    if ('medecin' in examen && examen.medecin) {
      return `Dr. ${examen.medecin.prenom} ${examen.medecin.nom}`;
    }
    return 'Médecin inconnu';
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  calculateAge(dateNaissance: string | undefined): string {
    if (!dateNaissance) return '';
    const birth = new Date(dateNaissance);
    const today = new Date();
    let age = today.getFullYear() - birth.getFullYear();
    const m = today.getMonth() - birth.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) {
      age--;
    }
    return `${age} ans`;
  }

  downloadDocument(uuid: string, filename: string): void {
    this.laborantinService.downloadDocument(uuid).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Erreur téléchargement:', err);
        this.errorMessage = 'Erreur lors du téléchargement';
      }
    });
  }
}
