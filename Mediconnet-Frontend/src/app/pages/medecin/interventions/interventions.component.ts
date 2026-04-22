import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { Subject, takeUntil } from 'rxjs';
import { DashboardLayoutComponent } from '../../../shared/components/dashboard-layout/dashboard-layout.component';
import { CoordinationAnesthesistePanelComponent } from '../../../shared/components/coordination-anesthesiste-panel/coordination-anesthesiste-panel.component';
import { CoordinationChirurgienPanelComponent } from '../../../shared/components/coordination-chirurgien-panel/coordination-chirurgien-panel.component';
import {
  CoordinationInterventionService,
  CoordinationIntervention,
  CoordinationStats,
  CoordinationFilter
} from '../../../services/coordination-intervention.service';
import { AuthService } from '../../../services/auth.service';
import { MEDECIN_MENU_ITEMS, MEDECIN_SIDEBAR_TITLE } from '../shared/medecin-menu.config';
import { ALL_ICONS_PROVIDER } from '../../../shared/icons';

type TabType = 'toutes' | 'en_attente' | 'modifiees' | 'validees';

@Component({
  selector: 'app-interventions',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LucideAngularModule,
    DashboardLayoutComponent,
    CoordinationAnesthesistePanelComponent,
    CoordinationChirurgienPanelComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './interventions.component.html',
  styleUrls: ['./interventions.component.scss']
})
export class InterventionsComponent implements OnInit, OnDestroy {
  menuItems = MEDECIN_MENU_ITEMS;
  sidebarTitle = MEDECIN_SIDEBAR_TITLE;

  private destroy$ = new Subject<void>();

  // Rôle utilisateur
  isAnesthesiste = false;
  isChirurgien = false;
  userRole: 'anesthesiste' | 'chirurgien' = 'chirurgien';

  // État
  isLoading = false;
  error: string | null = null;

  // Onglets
  activeTab: TabType = 'toutes';

  // Données
  coordinations: CoordinationIntervention[] = [];
  stats: CoordinationStats = {
    enAttente: 0,
    validees: 0,
    modifiees: 0,
    refusees: 0,
    total: 0
  };

  // Filtres
  filterStatut = '';
  filterDateDebut = '';
  filterDateFin = '';
  searchTerm = '';

  // Panel de détails
  selectedCoordination: CoordinationIntervention | null = null;
  showDetailPanel = false;

  constructor(
    private coordinationService: CoordinationInterventionService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.checkUserRole();
    this.loadStats();
    this.loadCoordinations();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private checkUserRole(): void {
    const user = this.authService.getCurrentUser();
    // Spécialité 3 = Anesthésiologie
    this.isAnesthesiste = user?.idSpecialite === 3;
    this.isChirurgien = !this.isAnesthesiste;
    this.userRole = this.isAnesthesiste ? 'anesthesiste' : 'chirurgien';
  }

  loadStats(): void {
    // Les stats sont calculées localement après chargement des coordinations
  }

  private calculateStats(): void {
    this.stats = {
      enAttente: this.coordinations.filter(c => c.statut === 'proposee').length,
      modifiees: this.coordinations.filter(c => c.statut === 'modifiee').length,
      validees: this.coordinations.filter(c => c.statut === 'validee').length,
      refusees: this.coordinations.filter(c => c.statut === 'refusee' || c.statut === 'contre_proposition_refusee').length,
      total: this.coordinations.length
    };
  }

  loadCoordinations(): void {
    this.isLoading = true;
    this.error = null;

    const filter: CoordinationFilter = {};

    // Appliquer les filtres selon l'onglet
    if (this.filterStatut) {
      filter.statut = this.filterStatut;
    }

    if (this.filterDateDebut) {
      filter.dateDebut = this.filterDateDebut;
    }
    if (this.filterDateFin) {
      filter.dateFin = this.filterDateFin;
    }

    const request$ = this.isAnesthesiste 
      ? this.coordinationService.getMesCoordinationsAnesthesiste(filter)
      : this.coordinationService.getMesCoordinationsChirurgien(filter);

    request$
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (coordinations) => {
          this.coordinations = coordinations;
          this.calculateStats();
          this.applyTabFilter();
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Erreur chargement coordinations:', err);
          this.error = 'Impossible de charger les interventions';
          this.isLoading = false;
        }
      });
  }

  filteredCoordinations: CoordinationIntervention[] = [];

  private applyTabFilter(): void {
    let filtered = [...this.coordinations];

    // Filtre par onglet
    switch (this.activeTab) {
      case 'en_attente':
        filtered = filtered.filter(c => c.statut === 'proposee');
        break;
      case 'modifiees':
        filtered = filtered.filter(c => c.statut === 'modifiee');
        break;
      case 'validees':
        filtered = filtered.filter(c => c.statut === 'validee');
        break;
    }

    // Filtre par recherche
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(c =>
        c.nomPatient.toLowerCase().includes(term) ||
        c.nomChirurgien.toLowerCase().includes(term) ||
        c.nomAnesthesiste.toLowerCase().includes(term) ||
        c.indicationOperatoire?.toLowerCase().includes(term)
      );
    }

    this.filteredCoordinations = filtered;
  }

  setActiveTab(tab: TabType): void {
    this.activeTab = tab;
    this.applyTabFilter();
  }

  onSearch(): void {
    this.applyTabFilter();
  }

  clearFilters(): void {
    this.filterStatut = '';
    this.filterDateDebut = '';
    this.filterDateFin = '';
    this.searchTerm = '';
    this.loadCoordinations();
  }

  openDetailPanel(coordination: CoordinationIntervention): void {
    this.selectedCoordination = coordination;
    this.showDetailPanel = true;
  }

  closeDetailPanel(): void {
    this.showDetailPanel = false;
    this.selectedCoordination = null;
  }

  onActionCompleted(event: { action: string; success: boolean } | string): void {
    setTimeout(() => {
      this.closeDetailPanel();
      this.loadCoordinations();
    }, 1500);
  }

  onRelancerAvecAutre(coordination: CoordinationIntervention): void {
    // Fermer le panel et rediriger vers la programmation pour relancer
    this.closeDetailPanel();
    // TODO: Implémenter la relance avec un autre anesthésiste
    console.log('Relancer avec autre anesthésiste pour:', coordination.idProgrammation);
  }

  // Helpers
  getStatutLabel(statut: string): string {
    return this.coordinationService.getStatutLabel(statut);
  }

  getStatutClass(statut: string): string {
    return this.coordinationService.getStatutClass(statut);
  }

  formatDuree(minutes: number): string {
    return this.coordinationService.formatDuree(minutes);
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('fr-FR', {
      weekday: 'short',
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    });
  }

  formatTime(heureStr: string): string {
    return heureStr;
  }

  getUrgencyClass(coordination: CoordinationIntervention): string {
    const dateProposee = new Date(coordination.dateProposee);
    const today = new Date();
    const diffDays = Math.ceil((dateProposee.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

    if (diffDays <= 2) return 'urgent';
    if (diffDays <= 7) return 'soon';
    return 'normal';
  }

  getRisqueClass(risque: string | undefined): string {
    if (!risque) return 'unknown';
    const r = risque.toLowerCase();
    if (r === 'eleve' || r === 'élevé') return 'high';
    if (r === 'modere' || r === 'modéré') return 'medium';
    return 'low';
  }

  getAsaClass(asa: string | undefined): string {
    if (!asa) return '';
    const num = Number.parseInt(asa.replace('ASA', '').trim());
    if (num >= 4) return 'asa-high';
    if (num >= 3) return 'asa-medium';
    return 'asa-low';
  }

  trackByCoordination(index: number, coord: CoordinationIntervention): number {
    return coord.idCoordination;
  }
}
