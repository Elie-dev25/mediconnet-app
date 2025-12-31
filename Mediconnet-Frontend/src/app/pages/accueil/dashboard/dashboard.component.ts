import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { 
  LucideAngularModule, 
  DashboardLayoutComponent, 
  WelcomeBannerComponent,
  StatsGridComponent,
  StatItem,
  ALL_ICONS_PROVIDER 
} from '../../../shared';
import { ACCUEIL_MENU_ITEMS, ACCUEIL_SIDEBAR_TITLE } from '../shared';
import { AccueilService, AccueilDashboardDto, RdvAccueilDto } from '../../../services/accueil.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-accueil-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    LucideAngularModule,
    DashboardLayoutComponent,
    WelcomeBannerComponent,
    StatsGridComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class AccueilDashboardComponent implements OnInit {
  menuItems = ACCUEIL_MENU_ITEMS;
  sidebarTitle = ACCUEIL_SIDEBAR_TITLE;

  userName = '';
  userRole = 'accueil';

  dashboard: AccueilDashboardDto | null = null;
  prochainRdvs: RdvAccueilDto[] = [];
  loading = true;
  error: string | null = null;

  stats: StatItem[] = [];

  constructor(
    private accueilService: AccueilService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.userName = `${user.prenom || ''} ${user.nom || ''}`.trim();
    }
    this.loadDashboard();
    this.loadProchainRdvs();
  }

  loadDashboard(): void {
    this.accueilService.getDashboard().subscribe({
      next: (data) => {
        this.dashboard = data;
        this.updateStats(data);
        this.loading = false;
      },
      error: (err) => {
        console.error('Erreur chargement dashboard:', err);
        this.error = 'Erreur lors du chargement des statistiques';
        this.loading = false;
      }
    });
  }

  private updateStats(data: AccueilDashboardDto): void {
    this.stats = [
      { icon: 'user-plus', label: 'Patients enregistrés', value: data.patientsEnregistresAujourdHui, colorClass: 'primary' },
      { icon: 'calendar-check', label: 'RDV prévus', value: data.rdvPrevusAujourdHui, colorClass: 'info' },
      { icon: 'check-circle', label: 'Patients arrivés', value: data.rdvEnCours, colorClass: 'success' },
      { icon: 'clock', label: 'En attente', value: data.patientsEnAttente, colorClass: 'warning' }
    ];
  }

  loadProchainRdvs(): void {
    this.accueilService.getRdvAujourdHui().subscribe({
      next: (rdvs) => {
        // Filtrer les 5 prochains RDV non arrives
        const now = new Date();
        this.prochainRdvs = rdvs
          .filter(r => new Date(r.dateHeure) > now && !r.patientArrive)
          .slice(0, 5);
      },
      error: (err) => console.error('Erreur chargement RDV:', err)
    });
  }

  marquerArrivee(rdv: RdvAccueilDto): void {
    this.accueilService.marquerArriveeRdv(rdv.idRendezVous).subscribe({
      next: () => {
        rdv.patientArrive = true;
        rdv.statut = 'confirme';
      },
      error: (err) => console.error('Erreur:', err)
    });
  }

  formatHeure(dateHeure: string): string {
    return new Date(dateHeure).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
  }

  getStatutClass(statut: string): string {
    const classes: Record<string, string> = {
      'planifie': 'statut-planifie',
      'confirme': 'statut-confirme',
      'en_cours': 'statut-en-cours',
      'termine': 'statut-termine',
      'annule': 'statut-annule'
    };
    return classes[statut] || '';
  }

  getStatutLabel(statut: string): string {
    const labels: Record<string, string> = {
      'planifie': 'Planifie',
      'confirme': 'Arrive',
      'en_cours': 'En cours',
      'termine': 'Termine',
      'annule': 'Annule'
    };
    return labels[statut] || statut;
  }
}
