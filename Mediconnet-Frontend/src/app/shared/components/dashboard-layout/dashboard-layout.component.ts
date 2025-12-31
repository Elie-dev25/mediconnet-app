import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { HeaderComponent } from '../header/header.component';
import { SidebarComponent, MenuItem } from '../sidebar/sidebar.component';
import { FeatureUnavailableModalComponent } from '../feature-unavailable-modal/feature-unavailable-modal.component';
import { AuthService } from '../../../services/auth.service';
// NOTE: Les modaux de profil ont été supprimés car le profil est maintenant complété lors de l'inscription
// import { ProfileAlertModalComponent } from '../profile-alert-modal/profile-alert-modal.component';
// import { ProfileFormModalComponent } from '../profile-form-modal/profile-form-modal.component';
// import { PatientProfileService, PatientProfile, ProfileStatus } from '../../../services/patient-profile.service';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent, SidebarComponent, FeatureUnavailableModalComponent],
  templateUrl: './dashboard-layout.component.html',
  styleUrl: './dashboard-layout.component.scss'
})
export class DashboardLayoutComponent implements OnInit {
  @Input() sidebarTitle = 'Menu';
  @Input() menuItems: MenuItem[] = [];

  userName = '';
  userRole = '';
  sidebarMobileOpen = false;

  // Feature unavailable modal
  showFeatureUnavailable = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      // Construire le nom complet à partir de prenom et nom
      this.userName = user.prenom && user.nom 
        ? `${user.prenom} ${user.nom}` 
        : (user.username || user.email || 'Utilisateur');
      this.userRole = user.role || '';
    }
  }

  // NOTE: La navigation vers le profil se fait maintenant via la page dédiée /patient/profile
  openProfileForm(): void {
    this.router.navigate([`/${this.userRole}/profile`]);
  }

  toggleMobileSidebar(): void {
    this.sidebarMobileOpen = !this.sidebarMobileOpen;
  }

  logout(): void {
    this.authService.logout();
  }

  onFeatureUnavailable(featureName: string): void {
    this.showFeatureUnavailable = true;
  }

  onFeatureUnavailableClose(): void {
    this.showFeatureUnavailable = false;
  }
}
