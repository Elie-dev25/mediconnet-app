import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DashboardLayoutComponent, ResultatExamenSidebarComponent } from '../../../shared';
import { LucideAngularModule } from 'lucide-angular';
import { PATIENT_MENU_ITEMS } from '../shared';
import { ExamenResultatsService, ResultatExamenList } from '../../../services/examen-resultats.service';

@Component({
  selector: 'app-patient-resultats-examens',
  standalone: true,
  imports: [
    CommonModule,
    DashboardLayoutComponent,
    LucideAngularModule,
    ResultatExamenSidebarComponent
  ],
  templateUrl: './resultats-examens.component.html',
  styleUrls: ['./resultats-examens.component.scss']
})
export class PatientResultatsExamensComponent implements OnInit {
  menuItems = PATIENT_MENU_ITEMS;

  examens: ResultatExamenList[] = [];
  isLoading = false;
  errorMessage = '';

  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  // Sidebar
  showResultatSidebar = false;
  selectedExamenId: number | null = null;

  constructor(private examenResultatsService: ExamenResultatsService) {}

  ngOnInit(): void {
    this.loadResultats();
  }

  loadResultats(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.examenResultatsService.getMesResultats(this.currentPage, this.pageSize).subscribe({
      next: (response) => {
        if (response.success) {
          this.examens = response.data.examens;
          this.totalCount = response.data.totalCount;
          this.totalPages = response.data.totalPages;
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement résultats:', err);
        this.errorMessage = 'Erreur lors du chargement des résultats';
        this.isLoading = false;
      }
    });
  }

  openResultat(examen: ResultatExamenList): void {
    this.selectedExamenId = examen.idBulletinExamen;
    this.showResultatSidebar = true;
  }

  closeResultatSidebar(): void {
    this.showResultatSidebar = false;
    this.selectedExamenId = null;
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadResultats();
    }
  }

  formatDate(date: string | undefined): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  get pages(): number[] {
    const pages: number[] = [];
    const start = Math.max(1, this.currentPage - 2);
    const end = Math.min(this.totalPages, this.currentPage + 2);
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  }
}
