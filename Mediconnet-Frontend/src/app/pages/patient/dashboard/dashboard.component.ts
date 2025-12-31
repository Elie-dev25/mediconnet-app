import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../../services/auth.service';
import { PatientService, VisiteDto, PatientDashboardDto } from '../../../services/patient.service';
import { 
  DashboardLayoutComponent, 
  WelcomeBannerComponent,
  StatsGridComponent,
  StatItem,
  LucideAngularModule,
  ALL_ICONS_PROVIDER,
  formatDateWithWeekday,
  formatTime
} from '../../../shared';
import { PATIENT_MENU_ITEMS, PATIENT_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-patient-dashboard',
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
export class PatientDashboardComponent implements OnInit {
  // Menu partagé pour toutes les pages patient
  menuItems = PATIENT_MENU_ITEMS;
  sidebarTitle = PATIENT_SIDEBAR_TITLE;

  userName = '';
  userRole = 'patient';
  isLoading = true;

  // Données du dashboard
  visitesAVenir: VisiteDto[] = [];
  visitesPassees: VisiteDto[] = [];

  stats: StatItem[] = [
    { icon: 'calendar', label: 'Visites à venir', value: 0, colorClass: 'rdv' },
    { icon: 'check-circle', label: 'Visites passées', value: 0, colorClass: 'consultations' },
    { icon: 'pill', label: 'Ordonnances', value: 0, colorClass: 'ordonnances' },
    { icon: 'flask-conical', label: 'Examens', value: 0, colorClass: 'examens' }
  ];

  constructor(
    private authService: AuthService,
    private patientService: PatientService
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.userName = `${user.prenom || ''} ${user.nom || ''}`.trim();
    }
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading = true;
    this.patientService.getDashboard().pipe(
      finalize(() => {
        this.isLoading = false;
      })
    ).subscribe({
      next: (data: PatientDashboardDto) => {
        this.visitesAVenir = data.visitesAVenir;
        this.visitesPassees = data.visitesPassees;
        
        // Mettre à jour les stats
        this.stats = [
          { icon: 'calendar', label: 'Visites à venir', value: data.stats.rendezVousAVenir, colorClass: 'rdv' },
          { icon: 'check-circle', label: 'Visites passées', value: data.stats.rendezVousPasses, colorClass: 'consultations' },
          { icon: 'pill', label: 'Ordonnances', value: data.stats.ordonnances, colorClass: 'ordonnances' },
          { icon: 'flask-conical', label: 'Examens', value: data.stats.examens, colorClass: 'examens' }
        ];
        
      },
      error: (err) => {
        console.error('Erreur chargement dashboard:', err);
      }
    });
  }

  formatDate(dateStr: string): string {
    return formatDateWithWeekday(dateStr);
  }

  formatTime(dateStr: string): string {
    return formatTime(dateStr);
  }

  getStatutLabel(statut: string): string {
    const labels: Record<string, string> = {
      'planifie': 'Planifié',
      'confirme': 'Confirmé',
      'en_cours': 'En cours',
      'termine': 'Terminé',
      'annule': 'Annulé'
    };
    return labels[statut] || statut;
  }

  getTypeLabel(type: string): string {
    const labels: Record<string, string> = {
      'consultation': 'Consultation',
      'suivi': 'Suivi',
      'urgence': 'Urgence',
      'examen': 'Examen',
      'vaccination': 'Vaccination'
    };
    return labels[type] || type;
  }
}
