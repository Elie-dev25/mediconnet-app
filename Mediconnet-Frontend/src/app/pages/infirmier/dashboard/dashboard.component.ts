import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../../../services/auth.service';
import { InfirmierQueueItem, InfirmierService } from '../../../services/infirmier.service';
import { SignalRService } from '../../../services/signalr.service';
import { 
  DashboardLayoutComponent, 
  WelcomeBannerComponent,
  StatsGridComponent,
  StatItem,
  ModalComponent,
  LucideAngularModule,
  ALL_ICONS_PROVIDER,
  ParametresFormComponent
} from '../../../shared';
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
    ParametresFormComponent,
    ModalComponent
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

  showParamForm = false;
  selectedItem: InfirmierQueueItem | null = null;

  constructor(
    private authService: AuthService,
    private infirmierService: InfirmierService,
    private signalRService: SignalRService
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.userName = `${user.prenom || ''} ${user.nom || ''}`.trim();
    }

    this.loadQueue();

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

  openParamForm(item: InfirmierQueueItem): void {
    this.selectedItem = item;
    this.showParamForm = true;
  }

  closeParamForm(): void {
    this.showParamForm = false;
    this.selectedItem = null;
  }

  onParametresSaved(): void {
    this.closeParamForm();
    this.loadQueue();
  }
}
