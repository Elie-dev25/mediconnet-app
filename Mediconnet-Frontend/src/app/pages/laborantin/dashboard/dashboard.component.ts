import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { LaborantinService, LaborantinStats, ExamenLaborantin } from '../../../services/laborantin.service';
import { DashboardLayoutComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { LABORANTIN_MENU_ITEMS, LABORANTIN_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-laborantin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    LucideAngularModule,
    DashboardLayoutComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class LaborantinDashboardComponent implements OnInit {
  menuItems = LABORANTIN_MENU_ITEMS;
  sidebarTitle = LABORANTIN_SIDEBAR_TITLE;

  userName = '';
  userRole = 'laborantin';

  // Date du jour
  today = new Date();

  // Stats
  stats: LaborantinStats = {
    examensEnAttente: 0,
    examensEnCours: 0,
    examensTerminesAujourdhui: 0,
    urgences: 0,
    totalExamensJour: 0
  };

  // Examens en attente
  examensEnAttente: ExamenLaborantin[] = [];
  isLoading = true;
  isLoadingExamens = true;

  constructor(
    private authService: AuthService,
    private laborantinService: LaborantinService
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.userName = `${user.prenom} ${user.nom}`;
    }
    
    this.loadStats();
    this.loadExamensEnAttente();
  }

  loadStats(): void {
    this.isLoading = true;
    this.laborantinService.getStats().subscribe({
      next: (response) => {
        if (response.success) {
          this.stats = response.data;
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement stats:', err);
        this.isLoading = false;
      }
    });
  }

  loadExamensEnAttente(): void {
    this.isLoadingExamens = true;
    this.laborantinService.getExamensEnAttente(10).subscribe({
      next: (response) => {
        if (response.success) {
          this.examensEnAttente = response.data;
        }
        this.isLoadingExamens = false;
      },
      error: (err) => {
        console.error('Erreur chargement examens:', err);
        this.isLoadingExamens = false;
      }
    });
  }

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

  formatPatientName(examen: ExamenLaborantin): string {
    if (examen.patientPrenom && examen.patientNom) {
      return `${examen.patientPrenom} ${examen.patientNom}`;
    }
    return 'Patient inconnu';
  }

  formatMedecinName(examen: ExamenLaborantin): string {
    if (examen.medecinPrenom && examen.medecinNom) {
      return `Dr. ${examen.medecinPrenom} ${examen.medecinNom}`;
    }
    return 'Médecin inconnu';
  }
}
