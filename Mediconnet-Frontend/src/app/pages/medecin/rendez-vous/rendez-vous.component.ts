import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DashboardLayoutComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { MEDECIN_MENU_ITEMS, MEDECIN_SIDEBAR_TITLE } from '../shared';
import { MedecinPlanningService, RdvPlanningDto } from '../../../services/medecin-planning.service';

@Component({
  selector: 'app-medecin-rendez-vous',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LucideAngularModule,
    DashboardLayoutComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './rendez-vous.component.html',
  styleUrl: './rendez-vous.component.scss'
})
export class MedecinRendezVousComponent implements OnInit {
  // Menu partagé pour toutes les pages médecin
  menuItems = MEDECIN_MENU_ITEMS;
  sidebarTitle = MEDECIN_SIDEBAR_TITLE;

  // État
  isLoading = true;
  activeFilter: 'jour' | 'semaine' | 'tous' = 'jour';
  statutFilter = '';
  searchTerm = '';

  // Données
  rendezVous: RdvPlanningDto[] = [];
  filteredRdv: RdvPlanningDto[] = [];

  // Date actuelle
  currentDate = new Date();

  constructor(private planningService: MedecinPlanningService) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;

    if (this.activeFilter === 'jour') {
      this.planningService.getMedecinRdvJour(this.toLocalDateString(this.currentDate)).subscribe({
        next: (rdvs) => {
          this.rendezVous = rdvs;
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
        ? this.toLocalDateString(this.getStartOfWeek(this.currentDate))
        : undefined;
      const dateFin = this.activeFilter === 'semaine'
        ? this.toLocalDateString(this.getEndOfWeek(this.currentDate))
        : undefined;

      this.planningService.getMedecinRdvList(dateDebut, dateFin, this.statutFilter || undefined).subscribe({
        next: (rdvs) => {
          this.rendezVous = rdvs;
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

  setActiveFilter(filter: 'jour' | 'semaine' | 'tous'): void {
    this.activeFilter = filter;
    this.loadData();
  }

  applyFilters(): void {
    let filtered = [...this.rendezVous];

    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(rdv => 
        rdv.patientNom.toLowerCase().includes(term) ||
        rdv.patientPrenom.toLowerCase().includes(term) ||
        (rdv.numeroDossier && rdv.numeroDossier.toLowerCase().includes(term))
      );
    }

    if (this.statutFilter) {
      filtered = filtered.filter(rdv => rdv.statut === this.statutFilter);
    }

    this.filteredRdv = filtered;
  }

  // Navigation date
  prevDay(): void {
    const newDate = new Date(this.currentDate);
    newDate.setDate(newDate.getDate() - 1);
    newDate.setHours(0, 0, 0, 0);
    this.currentDate = newDate;
    this.loadData();
  }

  nextDay(): void {
    const newDate = new Date(this.currentDate);
    newDate.setDate(newDate.getDate() + 1);
    newDate.setHours(0, 0, 0, 0);
    this.currentDate = newDate;
    this.loadData();
  }

  goToToday(): void {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    this.currentDate = today;
    this.loadData();
  }

  // Actions sur RDV
  updateStatut(rdv: RdvPlanningDto, statut: string): void {
    this.planningService.updateStatutRdv(rdv.idRendezVous, statut).subscribe({
      next: () => {
        rdv.statut = statut;
      },
      error: (err) => alert(err.error?.message || 'Erreur')
    });
  }

  // Helpers
  private toLocalDateString(date: Date): string {
    const d = new Date(date);
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    const hh = String(d.getHours()).padStart(2, '0');
    const mi = String(d.getMinutes()).padStart(2, '0');
    const ss = String(d.getSeconds()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}T${hh}:${mi}:${ss}`;
  }

  private getStartOfWeek(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    d.setDate(diff);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  private getEndOfWeek(date: Date): Date {
    const start = this.getStartOfWeek(new Date(date));
    start.setDate(start.getDate() + 6);
    start.setHours(23, 59, 59, 999);
    return start;
  }

  formatDate(dateStr: string): string {
    return this.planningService.formatDate(dateStr);
  }

  formatTime(dateStr: string): string {
    return this.planningService.formatTime(dateStr);
  }

  get dateLabel(): string {
    if (this.activeFilter === 'jour') {
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      const currentDateNormalized = new Date(this.currentDate);
      currentDateNormalized.setHours(0, 0, 0, 0);
      
      if (currentDateNormalized.getTime() === today.getTime()) {
        return "Aujourd'hui - " + this.currentDate.toLocaleDateString('fr-FR', { 
          day: 'numeric', month: 'long', year: 'numeric' 
        });
      }
      return this.currentDate.toLocaleDateString('fr-FR', { 
        weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' 
      });
    }
    return '';
  }

  getStatutLabel(statut: string): string {
    const labels: { [key: string]: string } = {
      'planifie': 'Planifié',
      'confirme': 'Confirmé',
      'en_cours': 'En cours',
      'termine': 'Terminé',
      'annule': 'Annulé',
      'absent': 'Absent'
    };
    return labels[statut] || statut;
  }

  getStatutClass(statut: string): string {
    const classes: { [key: string]: string } = {
      'planifie': 'status-planned',
      'confirme': 'status-confirmed',
      'en_cours': 'status-progress',
      'termine': 'status-completed',
      'annule': 'status-cancelled',
      'absent': 'status-absent'
    };
    return classes[statut] || '';
  }

  refresh(): void {
    this.loadData();
  }

  get rdvCount(): { total: number; confirmes: number; enAttente: number } {
    return {
      total: this.rendezVous.length,
      confirmes: this.rendezVous.filter(r => r.statut === 'confirme' || r.statut === 'termine').length,
      enAttente: this.rendezVous.filter(r => r.statut === 'planifie').length
    };
  }
}
