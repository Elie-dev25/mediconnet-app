import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../../../services/auth.service';
import { InfirmierQueueItem, InfirmierService } from '../../../services/infirmier.service';
import { SignalRService } from '../../../services/signalr.service';
import { 
  DashboardLayoutComponent, 
  WelcomeBannerComponent,
  StatsGridComponent,
  StatItem,
  LucideAngularModule,
  ALL_ICONS_PROVIDER,
  AttribuerLitPanelComponent,
  HospitalisationEnAttenteInfo
} from '../../../shared';
import { HospitalisationService } from '../../../services/hospitalisation.service';
import { INFIRMIER_MENU_ITEMS, INFIRMIER_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-infirmier-dashboard',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    LucideAngularModule, 
    DashboardLayoutComponent,
    WelcomeBannerComponent,
    StatsGridComponent,
    AttribuerLitPanelComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class InfirmierDashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // Menu partagé pour toutes les pages infirmier
  menuItems = INFIRMIER_MENU_ITEMS;
  sidebarTitle = INFIRMIER_SIDEBAR_TITLE;

  userName = '';
  userRole = 'infirmier';

  stats: StatItem[] = [
    { icon: 'users', label: 'Patients', value: 0, colorClass: 'patients' },
    { icon: 'syringe', label: 'Soins', value: 0, colorClass: 'soins' },
    { icon: 'calendar', label: 'RDV aujourd\'hui', value: 0, colorClass: 'rdv' },
    { icon: 'bed-double', label: 'Hospitalisés', value: 0, colorClass: 'examens' }
  ];

  isLoadingQueue = false;
  queue: InfirmierQueueItem[] = [];
  errorQueue: string | null = null;

  // Hospitalisations en attente (Major)
  isMajor = false;
  hospitalisationsEnAttente: any[] = [];
  isLoadingHospitalisations = false;
  errorHospitalisations: string | null = null;

  // Panel attribution lit
  showAttribuerLitPanel = false;
  selectedHospitalisation: HospitalisationEnAttenteInfo | null = null;

  constructor(
    private authService: AuthService,
    private infirmierService: InfirmierService,
    private hospitalisationService: HospitalisationService,
    private signalRService: SignalRService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.userName = `${user.prenom || ''} ${user.nom || ''}`.trim();
    }

    this.loadQueue();
    this.loadHospitalisationsEnAttente();

    this.signalRService.vitalsEvents$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.loadQueue();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadQueue(): void {
    this.isLoadingQueue = true;
    this.errorQueue = null;

    this.infirmierService.getFileAttente().subscribe({
      next: (res) => {
        this.queue = res.data || [];
        this.isLoadingQueue = false;
        const count = this.queue.length;
        this.stats = this.stats.map(s =>
          s.label === 'Patients' ? { ...s, value: count } : s
        );
      },
      error: (err) => {
        this.isLoadingQueue = false;
        this.errorQueue = err?.error?.message || 'Erreur lors du chargement de la file';
      }
    });
  }

  goToPriseParametres(item: InfirmierQueueItem): void {
    this.router.navigate(['/infirmier/prise-parametres', item.idPatient]);
  }

  // ==================== HOSPITALISATIONS EN ATTENTE ====================

  loadHospitalisationsEnAttente(): void {
    this.isLoadingHospitalisations = true;
    this.errorHospitalisations = null;

    this.hospitalisationService.getPatientsHospitalises().subscribe({
      next: (response: any) => {
        if (response.success) {
          this.isMajor = response.isMajor || false;
          // Filtrer uniquement les hospitalisations en attente de lit
          this.hospitalisationsEnAttente = (response.data || []).filter(
            (h: any) => h.statut === 'EN_ATTENTE'
          );
          // Mettre à jour les stats
          this.stats = this.stats.map(s =>
            s.label === 'Hospitalisés' ? { ...s, value: this.hospitalisationsEnAttente.length } : s
          );
        }
        this.isLoadingHospitalisations = false;
      },
      error: (err) => {
        console.error('Erreur chargement hospitalisations:', err);
        this.errorHospitalisations = 'Impossible de charger les hospitalisations';
        this.isLoadingHospitalisations = false;
      }
    });
  }

  openAttribuerLitPanel(hospitalisation: any): void {
    this.selectedHospitalisation = {
      idAdmission: hospitalisation.idAdmission,
      patientNom: hospitalisation.patient?.nom || hospitalisation.patientNom || '',
      patientPrenom: hospitalisation.patient?.prenom || hospitalisation.patientPrenom || '',
      motif: hospitalisation.motif,
      urgence: hospitalisation.urgence,
      dateEntree: hospitalisation.dateEntree
    };
    this.showAttribuerLitPanel = true;
  }

  closeAttribuerLitPanel(): void {
    this.showAttribuerLitPanel = false;
    this.selectedHospitalisation = null;
  }

  onLitAttribue(): void {
    this.closeAttribuerLitPanel();
    this.loadHospitalisationsEnAttente();
  }

  getUrgenceClass(urgence?: string): string {
    switch (urgence?.toLowerCase()) {
      case 'haute': return 'urgence-haute';
      case 'moyenne': return 'urgence-moyenne';
      default: return 'urgence-normale';
    }
  }

  formatDateTime(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
