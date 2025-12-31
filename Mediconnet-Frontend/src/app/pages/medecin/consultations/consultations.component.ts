import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DashboardLayoutComponent, ModalComponent, LucideAngularModule, ALL_ICONS_PROVIDER, ConsultationQuestionnaireFormComponent } from '../../../shared';
import { MEDECIN_MENU_ITEMS, MEDECIN_SIDEBAR_TITLE } from '../shared';
import { MedecinDataService, ConsultationDto, ConsultationStatsDto } from '../../../services/medecin-data.service';
import { MedecinPlanningService } from '../../../services/medecin-planning.service';

@Component({
  selector: 'app-medecin-consultations',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LucideAngularModule,
    DashboardLayoutComponent,
    ConsultationQuestionnaireFormComponent,
    ModalComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './consultations.component.html',
  styleUrl: './consultations.component.scss'
})
export class MedecinConsultationsComponent implements OnInit {
  // Menu partagé
  menuItems = MEDECIN_MENU_ITEMS;
  sidebarTitle = MEDECIN_SIDEBAR_TITLE;

  // État
  isLoading = true;
  activeFilter: 'jour' | 'semaine' | 'toutes' = 'jour';
  searchTerm = '';
  statutFilter = '';

  // Données
  consultations: ConsultationDto[] = [];
  filteredConsultations: ConsultationDto[] = [];
  stats: ConsultationStatsDto | null = null;

  // Date
  currentDate = new Date();

  showQuestionnaire = false;
  selectedForQuestionnaire: ConsultationDto | null = null;

  constructor(
    private medecinDataService: MedecinDataService,
    private planningService: MedecinPlanningService
  ) {}

  ngOnInit(): void {
    this.loadStats();
    this.loadData();
  }

  loadStats(): void {
    this.medecinDataService.getConsultationStats().subscribe({
      next: (stats) => this.stats = stats,
      error: (err) => console.error('Erreur stats:', err)
    });
  }

  loadData(): void {
    this.isLoading = true;

    if (this.activeFilter === 'jour') {
      this.medecinDataService.getConsultationsJour(this.currentDate.toISOString()).subscribe({
        next: (data) => {
          this.consultations = data;
          this.applyFilters();
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Erreur:', err);
          this.isLoading = false;
        }
      });
    } else {
      const dateDebut = this.activeFilter === 'semaine' 
        ? this.getStartOfWeek(this.currentDate).toISOString()
        : undefined;
      const dateFin = this.activeFilter === 'semaine'
        ? this.getEndOfWeek(this.currentDate).toISOString()
        : undefined;

      this.medecinDataService.getConsultations(dateDebut, dateFin).subscribe({
        next: (data) => {
          this.consultations = data;
          this.applyFilters();
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Erreur:', err);
          this.isLoading = false;
        }
      });
    }
  }

  setActiveFilter(filter: 'jour' | 'semaine' | 'toutes'): void {
    this.activeFilter = filter;
    this.loadData();
  }

  applyFilters(): void {
    let filtered = [...this.consultations];

    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(c => 
        c.patientNom.toLowerCase().includes(term) ||
        c.patientPrenom.toLowerCase().includes(term) ||
        (c.numeroDossier && c.numeroDossier.toLowerCase().includes(term)) ||
        (c.motif && c.motif.toLowerCase().includes(term))
      );
    }

    if (this.statutFilter) {
      filtered = filtered.filter(c => c.statut === this.statutFilter);
    }

    this.filteredConsultations = filtered;
  }

  // Navigation date
  prevDay(): void {
    this.currentDate = new Date(this.currentDate.setDate(this.currentDate.getDate() - 1));
    this.loadData();
  }

  nextDay(): void {
    this.currentDate = new Date(this.currentDate.setDate(this.currentDate.getDate() + 1));
    this.loadData();
  }

  goToToday(): void {
    this.currentDate = new Date();
    this.loadData();
  }

  // Actions
  demarrerConsultation(consultation: ConsultationDto): void {
    this.planningService.updateStatutRdv(consultation.idRendezVous, 'en_cours').subscribe({
      next: () => {
        consultation.statut = 'en_cours';
      },
      error: (err) => alert(err.error?.message || 'Erreur')
    });
  }

  openQuestionnaire(consultation: ConsultationDto): void {
    if (!consultation.idConsultation || consultation.idConsultation <= 0) {
      alert('Consultation non initialisée (id_consultation introuvable)');
      return;
    }
    this.selectedForQuestionnaire = consultation;
    this.showQuestionnaire = true;
  }

  closeQuestionnaire(): void {
    this.showQuestionnaire = false;
    this.selectedForQuestionnaire = null;
  }

  onQuestionnaireSaved(): void {
    this.loadData();
  }

  terminerConsultation(consultation: ConsultationDto): void {
    this.planningService.updateStatutRdv(consultation.idRendezVous, 'termine').subscribe({
      next: () => {
        consultation.statut = 'terminee';
        this.loadStats();
      },
      error: (err) => alert(err.error?.message || 'Erreur')
    });
  }

  // Helpers
  private getStartOfWeek(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    return new Date(d.setDate(diff));
  }

  private getEndOfWeek(date: Date): Date {
    const start = this.getStartOfWeek(date);
    return new Date(start.setDate(start.getDate() + 6));
  }

  formatDate(dateStr: string): string {
    return this.medecinDataService.formatDate(dateStr);
  }

  formatTime(dateStr: string): string {
    return this.medecinDataService.formatTime(dateStr);
  }

  get dateLabel(): string {
    if (this.activeFilter === 'jour') {
      const today = new Date();
      if (this.currentDate.toDateString() === today.toDateString()) {
        return "Aujourd'hui";
      }
      return this.currentDate.toLocaleDateString('fr-FR', { 
        weekday: 'long', day: 'numeric', month: 'long' 
      });
    }
    return '';
  }

  getStatutLabel(statut: string): string {
    const labels: { [key: string]: string } = {
      'a_faire': 'À faire',
      'en_cours': 'En cours',
      'terminee': 'Terminée'
    };
    return labels[statut] || statut;
  }

  getStatutClass(statut: string): string {
    const classes: { [key: string]: string } = {
      'a_faire': 'status-pending',
      'en_cours': 'status-progress',
      'terminee': 'status-completed'
    };
    return classes[statut] || '';
  }

  refresh(): void {
    this.loadStats();
    this.loadData();
  }
}
