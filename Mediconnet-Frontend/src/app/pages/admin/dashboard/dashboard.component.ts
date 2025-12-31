import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { 
  DashboardLayoutComponent, 
  WelcomeBannerComponent,
  StatsGridComponent,
  StatItem,
  LucideAngularModule,
  ALL_ICONS_PROVIDER
} from '../../../shared';
import { ADMIN_MENU_ITEMS, ADMIN_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-admin-dashboard',
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
  styleUrl: './dashboard.component.scss'
})
export class AdminDashboardComponent implements OnInit {
  // Menu partagé pour toutes les pages admin
  menuItems = ADMIN_MENU_ITEMS;
  sidebarTitle = ADMIN_SIDEBAR_TITLE;

  userName = '';
  userRole = 'administrateur';

  stats: StatItem[] = [
    { icon: 'users', label: 'Utilisateurs', value: 0, colorClass: 'users' },
    { icon: 'building-2', label: 'Services', value: 0, colorClass: 'services' },
    { icon: 'user-plus', label: 'Médecins', value: 0, colorClass: 'medecins' },
    { icon: 'shield', label: 'Système', value: 'Actif', colorClass: 'security' }
  ];

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.userName = `${user.prenom || ''} ${user.nom || ''}`.trim();
    }
  }
}
