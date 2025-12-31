import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { DashboardLayoutComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { BIOLOGISTE_MENU_ITEMS, BIOLOGISTE_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-biologiste-dashboard',
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
export class BiologisteDashboardComponent implements OnInit {
  menuItems = BIOLOGISTE_MENU_ITEMS;
  sidebarTitle = BIOLOGISTE_SIDEBAR_TITLE;

  userName = '';
  userRole = 'biologiste';

  // Date du jour
  today = new Date();

  // Stats placeholder
  stats = {
    analysesEnCours: 0,
    resultatsAValider: 0,
    examensJour: 0,
    urgences: 0
  };

  // Analyses récentes (placeholder)
  analysesRecentes: any[] = [];
  isLoading = true;

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.userName = `${user.prenom} ${user.nom}`;
    }
    
    // Simulate loading
    setTimeout(() => {
      this.isLoading = false;
    }, 500);
  }
}
