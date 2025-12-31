import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { DashboardLayoutComponent, LucideAngularModule, ALL_ICONS_PROVIDER, PatientSearchComponent, WelcomeBannerComponent } from '../../../shared';
import { INFIRMIER_MENU_ITEMS, INFIRMIER_SIDEBAR_TITLE } from '../shared/infirmier-menu.config';
import { PatientService, PatientBasicInfo } from '../../../services/patient.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-infirmier-patients',
  standalone: true,
  imports: [
    CommonModule,
    LucideAngularModule,
    DashboardLayoutComponent,
    PatientSearchComponent,
    WelcomeBannerComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './patients.component.html',
  styleUrls: ['./patients.component.scss']
})
export class InfirmierPatientsComponent implements OnInit {
  menuItems = INFIRMIER_MENU_ITEMS;
  sidebarTitle = INFIRMIER_SIDEBAR_TITLE;
  userName = '';
  
  patients: PatientBasicInfo[] = [];
  filteredPatients: PatientBasicInfo[] = [];
  isLoading = false;
  errorMessage: string | null = null;
  searchPerformed = false;

  constructor(
    private router: Router,
    private patientService: PatientService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.loadRecentPatients();
  }

  loadUserInfo(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.userName = user.prenom || user.nom || '';
    }
  }

  loadRecentPatients(): void {
    this.isLoading = true;
    this.errorMessage = null;
    this.searchPerformed = false;

    this.patientService.getRecentPatients(6).subscribe({
      next: (response) => {
        if (response.success) {
          this.patients = response.patients;
          this.filteredPatients = response.patients;
        } else {
          this.errorMessage = response.message;
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement patients récents:', err);
        this.errorMessage = 'Impossible de charger les patients récents';
        this.isLoading = false;
      }
    });
  }

  onPatientSelected(patient: PatientBasicInfo): void {
    this.viewPatientDetails(patient);
  }

  viewPatientDetails(patient: PatientBasicInfo): void {
    // TODO: Implémenter la page de détails du patient
    console.log('Voir détails patient:', patient);
  }

  takeVitals(patient: PatientBasicInfo): void {
    this.router.navigate(['/infirmier/prise-parametres', patient.idUser]);
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { day: '2-digit', month: 'long', year: 'numeric' });
  }

  calculateAge(dateStr?: string): number | null {
    if (!dateStr) return null;
    const birthDate = new Date(dateStr);
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    return age;
  }
}
