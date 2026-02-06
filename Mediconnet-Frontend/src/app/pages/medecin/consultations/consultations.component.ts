import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DashboardLayoutComponent, ModalComponent, LucideAngularModule, ALL_ICONS_PROVIDER, ConsultationQuestionnaireFormComponent } from '../../../shared';
import { MEDECIN_MENU_ITEMS, MEDECIN_SIDEBAR_TITLE } from '../shared';
import { MedecinDataService, ConsultationDto, ConsultationStatsDto } from '../../../services/medecin-data.service';
import { MedecinPlanningService } from '../../../services/medecin-planning.service';
import { forkJoin } from 'rxjs';

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
  searchTerm = '';
  activeTab: 'aujourdhui' | 'semaine' | 'historique' = 'aujourdhui';

  // Données séparées par section
  consultationsAujourdhui: ConsultationDto[] = [];
  consultationsSemaine: ConsultationDto[] = [];
  consultationsHistorique: ConsultationDto[] = [];
  stats: ConsultationStatsDto | null = null;

  // Date
  currentDate = new Date();

  showQuestionnaire = false;
  selectedForQuestionnaire: ConsultationDto | null = null;

  constructor(
    private medecinDataService: MedecinDataService,
    private planningService: MedecinPlanningService,
    private router: Router
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
    const today = new Date();
    const startOfWeek = this.getStartOfWeek(today);
    const endOfWeek = this.getEndOfWeek(today);

    // Charger les 3 sections en parallèle
    forkJoin({
      aujourdhui: this.medecinDataService.getConsultationsJour(today.toISOString()),
      semaine: this.medecinDataService.getConsultations(startOfWeek.toISOString(), endOfWeek.toISOString()),
      historique: this.medecinDataService.getConsultations() // Toutes les consultations
    }).subscribe({
      next: (data) => {
        const todayStr = today.toDateString();
        
        // Aujourd'hui: consultations du jour
        this.consultationsAujourdhui = this.filterBySearch(data.aujourdhui);
        
        // Cette semaine: consultations de la semaine SAUF aujourd'hui
        this.consultationsSemaine = this.filterBySearch(
          data.semaine.filter(c => new Date(c.dateConsultation).toDateString() !== todayStr)
        );
        
        // Historique: consultations passées (heure passée) OU terminées
        const now = new Date();
        this.consultationsHistorique = this.filterBySearch(
          data.historique.filter(c => {
            const consultDate = new Date(c.dateConsultation);
            // Une consultation est dans l'historique si:
            // 1. Elle est terminée OU
            // 2. Sa date/heure est passée (même d'une minute)
            return c.statut === 'terminee' || consultDate < now;
          })
        ).sort((a, b) => new Date(b.dateConsultation).getTime() - new Date(a.dateConsultation).getTime());
        
        // Retirer de "Aujourd'hui" les consultations passées ou terminées
        this.consultationsAujourdhui = this.consultationsAujourdhui.filter(c => {
          const consultDate = new Date(c.dateConsultation);
          return c.statut !== 'terminee' && consultDate >= now;
        });
        
        // Retirer de "Cette semaine" les consultations passées ou terminées
        this.consultationsSemaine = this.consultationsSemaine.filter(c => {
          const consultDate = new Date(c.dateConsultation);
          return c.statut !== 'terminee' && consultDate >= now;
        });
        
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur:', err);
        this.isLoading = false;
      }
    });
  }

  filterBySearch(consultations: ConsultationDto[]): ConsultationDto[] {
    if (!this.searchTerm.trim()) return consultations;
    const term = this.searchTerm.toLowerCase();
    return consultations.filter(c => 
      c.patientNom.toLowerCase().includes(term) ||
      c.patientPrenom.toLowerCase().includes(term) ||
      (c.numeroDossier && c.numeroDossier.toLowerCase().includes(term)) ||
      (c.motif && c.motif.toLowerCase().includes(term))
    );
  }

  onSearchChange(): void {
    this.loadData();
  }

  // Navigation vers la page de détails
  voirConsultation(consultation: ConsultationDto): void {
    if (consultation.idConsultation && consultation.idConsultation > 0) {
      this.router.navigate(['/medecin/consultation-details', consultation.idConsultation]);
    }
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
    const today = new Date();
    if (this.currentDate.toDateString() === today.toDateString()) {
      return "Aujourd'hui";
    }
    return this.currentDate.toLocaleDateString('fr-FR', { 
      weekday: 'long', day: 'numeric', month: 'long' 
    });
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

  setActiveTab(tab: 'aujourdhui' | 'semaine' | 'historique'): void {
    this.activeTab = tab;
  }

  get currentTabConsultations(): ConsultationDto[] {
    switch (this.activeTab) {
      case 'aujourdhui': return this.consultationsAujourdhui;
      case 'semaine': return this.consultationsSemaine;
      case 'historique': return this.consultationsHistorique;
      default: return [];
    }
  }

  get currentTabCount(): number {
    return this.currentTabConsultations.length;
  }

  get emptyMessage(): string {
    switch (this.activeTab) {
      case 'aujourdhui': return "Aucune consultation prévue aujourd'hui";
      case 'semaine': return 'Aucune consultation prévue cette semaine';
      case 'historique': return "Aucune consultation dans l'historique";
      default: return 'Aucune consultation';
    }
  }
}
