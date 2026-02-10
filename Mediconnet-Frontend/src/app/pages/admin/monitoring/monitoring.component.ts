import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, interval } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { 
  AlertesSystemeService, 
  AlerteSysteme, 
  AlertesStats,
  StorageHealth,
  TypeAlerte,
  SeveriteAlerte
} from '../../../services/alertes-systeme.service';
import { 
  DashboardLayoutComponent,
  WelcomeBannerComponent,
  LucideAngularModule,
  ALL_ICONS_PROVIDER
} from '../../../shared';
import { ADMIN_MENU_ITEMS, ADMIN_SIDEBAR_TITLE } from '../shared';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-monitoring',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DashboardLayoutComponent,
    WelcomeBannerComponent,
    LucideAngularModule
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './monitoring.component.html',
  styleUrl: './monitoring.component.scss'
})
export class MonitoringComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  menuItems = ADMIN_MENU_ITEMS;
  sidebarTitle = ADMIN_SIDEBAR_TITLE;

  userName = '';
  userRole = 'administrateur';

  alertesActives: AlerteSysteme[] = [];
  stats: AlertesStats | null = null;
  storageHealth: StorageHealth | null = null;

  isLoading = true;
  isAcquitting = false;
  selectedAlerte: AlerteSysteme | null = null;

  filterType: TypeAlerte | '' = '';
  filterSeverite: SeveriteAlerte | '' = '';

  constructor(
    private alertesService: AlertesSystemeService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.userName = `${user.prenom || ''} ${user.nom || ''}`.trim();
    }

    this.loadData();

    // Rafraîchir toutes les 30 secondes
    interval(30000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.loadData(false));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadData(showLoading = true): void {
    if (showLoading) this.isLoading = true;

    this.alertesService.getAlertesActives().subscribe({
      next: (alertes) => {
        this.alertesActives = alertes;
      },
      error: (err) => console.error('Erreur chargement alertes', err)
    });

    this.alertesService.getStats().subscribe({
      next: (stats) => {
        this.stats = stats;
      },
      error: (err) => console.error('Erreur chargement stats', err)
    });

    this.alertesService.getStorageHealth().subscribe({
      next: (health) => {
        this.storageHealth = health;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement storage health', err);
        this.isLoading = false;
      }
    });
  }

  acquitterAlerte(alerte: AlerteSysteme): void {
    this.isAcquitting = true;
    this.alertesService.acquitterAlerte(alerte.idAlerte).subscribe({
      next: () => {
        this.alertesActives = this.alertesActives.filter(a => a.idAlerte !== alerte.idAlerte);
        this.isAcquitting = false;
        this.loadData(false);
      },
      error: (err) => {
        console.error('Erreur acquittement', err);
        this.isAcquitting = false;
      }
    });
  }

  getIcon(type: TypeAlerte): string {
    return this.alertesService.getAlerteIcon(type);
  }

  getSeveriteClass(severite: SeveriteAlerte): string {
    return this.alertesService.getSeveriteClass(severite);
  }

  getTypeLabel(type: TypeAlerte): string {
    return this.alertesService.getTypeLabel(type);
  }

  getSeveriteLabel(severite: SeveriteAlerte): string {
    return this.alertesService.getSeveriteLabel(severite);
  }

  getStorageStatusClass(): string {
    if (!this.storageHealth) return '';
    if (this.storageHealth.diskUsagePercent >= 90) return 'status-critical';
    if (this.storageHealth.diskUsagePercent >= 80) return 'status-warning';
    return 'status-ok';
  }

  getStorageStatusLabel(): string {
    if (!this.storageHealth) return 'Inconnu';
    if (this.storageHealth.diskUsagePercent >= 90) return 'Critique';
    if (this.storageHealth.diskUsagePercent >= 80) return 'Attention';
    return 'Normal';
  }

  get filteredAlertes(): AlerteSysteme[] {
    return this.alertesActives.filter(a => {
      if (this.filterType && a.typeAlerte !== this.filterType) return false;
      if (this.filterSeverite && a.severite !== this.filterSeverite) return false;
      return true;
    });
  }

  get alertesCritiques(): number {
    return this.alertesActives.filter(a => a.severite === 'critical' || a.severite === 'emergency').length;
  }

  formatDate(date: Date | string): string {
    return new Date(date).toLocaleString('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatSize(sizeGb: number): string {
    if (sizeGb >= 1) return `${sizeGb.toFixed(2)} Go`;
    return `${(sizeGb * 1024).toFixed(0)} Mo`;
  }
}
