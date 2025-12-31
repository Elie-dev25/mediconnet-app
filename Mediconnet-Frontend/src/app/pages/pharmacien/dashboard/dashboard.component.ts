import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { DashboardLayoutComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { PHARMACIEN_MENU_ITEMS, PHARMACIEN_SIDEBAR_TITLE } from '../shared';
import { PharmacieStockService, PharmacieKpi, AlerteStock } from '../../../services/pharmacie-stock.service';

@Component({
  selector: 'app-pharmacien-dashboard',
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
export class PharmacienDashboardComponent implements OnInit {
  menuItems = PHARMACIEN_MENU_ITEMS;
  sidebarTitle = PHARMACIEN_SIDEBAR_TITLE;

  userName = '';
  userRole = 'pharmacien';
  today = new Date();

  kpis: PharmacieKpi = {
    totalMedicaments: 0,
    medicamentsEnAlerte: 0,
    medicamentsEnRupture: 0,
    medicamentsPerimesProches: 0,
    ordonnancesEnAttente: 0,
    dispensationsJour: 0,
    valeurStock: 0,
    commandesEnCours: 0
  };

  alertes: AlerteStock[] = [];
  isLoading = true;

  constructor(
    private authService: AuthService,
    private stockService: PharmacieStockService
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.userName = `${user.prenom} ${user.nom}`;
    }
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.isLoading = true;
    
    this.stockService.getKpis().subscribe({
      next: (kpis) => {
        this.kpis = kpis;
      },
      error: (err) => console.error('Erreur KPIs', err)
    });

    this.stockService.getAlertes().subscribe({
      next: (alertes) => {
        this.alertes = alertes.slice(0, 5);
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur alertes', err);
        this.isLoading = false;
      }
    });
  }

  formatPrice(value: number): string {
    return value.toLocaleString('fr-FR') + ' FCFA';
  }

  getAlertIcon(type: string): string {
    switch (type) {
      case 'rupture': return 'alert-circle';
      case 'alerte': return 'alert-triangle';
      case 'peremption': return 'clock';
      default: return 'info';
    }
  }

  getAlertClass(type: string): string {
    switch (type) {
      case 'rupture': return 'alert-danger';
      case 'alerte': return 'alert-warning';
      case 'peremption': return 'alert-info';
      default: return '';
    }
  }
}
