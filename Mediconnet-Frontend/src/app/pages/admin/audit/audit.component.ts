import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { 
  AuditService, 
  AuditLogDto, 
  AuditLogsPagedResult,
  AuditStatsDto,
  AuditLogsFilter
} from '../../../services/audit.service';
import { 
  DashboardLayoutComponent,
  WelcomeBannerComponent,
  LucideAngularModule,
  ALL_ICONS_PROVIDER
} from '../../../shared';
import { ADMIN_MENU_ITEMS, ADMIN_SIDEBAR_TITLE } from '../shared';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DashboardLayoutComponent,
    WelcomeBannerComponent,
    LucideAngularModule
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './audit.component.html',
  styleUrl: './audit.component.scss'
})
export class AuditComponent implements OnInit {
  menuItems = ADMIN_MENU_ITEMS;
  sidebarTitle = ADMIN_SIDEBAR_TITLE;

  userName = '';
  userRole = 'administrateur';

  logs: AuditLogDto[] = [];
  stats: AuditStatsDto | null = null;
  availableActions: string[] = [];
  availableResources: string[] = [];

  isLoading = true;
  currentPage = 1;
  pageSize = 20;
  totalPages = 1;
  totalCount = 0;

  // Filtres
  filterAction = '';
  filterResource = '';
  filterSuccess: string = '';
  filterDateFrom = '';
  filterDateTo = '';

  constructor(
    private auditService: AuditService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.userName = `${user.prenom || ''} ${user.nom || ''}`.trim();
    }

    this.loadFilters();
    this.loadStats();
    this.loadLogs();
  }

  loadFilters(): void {
    this.auditService.getAvailableActions().subscribe({
      next: (actions) => this.availableActions = actions,
      error: (err) => console.error('Erreur chargement actions', err)
    });

    this.auditService.getAvailableResourceTypes().subscribe({
      next: (resources) => this.availableResources = resources,
      error: (err) => console.error('Erreur chargement resources', err)
    });
  }

  loadStats(): void {
    this.auditService.getStats(7).subscribe({
      next: (stats) => this.stats = stats,
      error: (err) => console.error('Erreur chargement stats', err)
    });
  }

  loadLogs(): void {
    this.isLoading = true;

    const filter: AuditLogsFilter = {
      page: this.currentPage,
      pageSize: this.pageSize
    };

    if (this.filterAction) filter.action = this.filterAction;
    if (this.filterResource) filter.resourceType = this.filterResource;
    if (this.filterSuccess !== '') filter.successOnly = this.filterSuccess === 'true';
    if (this.filterDateFrom) filter.dateFrom = new Date(this.filterDateFrom);
    if (this.filterDateTo) filter.dateTo = new Date(this.filterDateTo);

    this.auditService.getLogs(filter).subscribe({
      next: (result: AuditLogsPagedResult) => {
        this.logs = result.logs;
        this.totalPages = result.totalPages;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement logs', err);
        this.isLoading = false;
      }
    });
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.loadLogs();
  }

  resetFilters(): void {
    this.filterAction = '';
    this.filterResource = '';
    this.filterSuccess = '';
    this.filterDateFrom = '';
    this.filterDateTo = '';
    this.currentPage = 1;
    this.loadLogs();
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadLogs();
    }
  }

  formatAction(action: string): string {
    return this.auditService.formatAction(action);
  }

  getActionBadgeClass(action: string, success: boolean): string {
    return this.auditService.getActionBadgeClass(action, success);
  }

  formatDate(date: Date | string): string {
    return new Date(date).toLocaleString('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  }

  getActionIcon(action: string): string {
    if (action.includes('LOGIN') || action.includes('AUTH')) return 'log-in';
    if (action.includes('LOGOUT')) return 'log-out';
    if (action.includes('CREATE') || action.includes('ENREGISTR')) return 'plus-circle';
    if (action.includes('UPDATE') || action.includes('CHANGE') || action.includes('MODIF')) return 'edit';
    if (action.includes('DELETE') || action.includes('REMOVE')) return 'trash-2';
    if (action.includes('SENSITIVE') || action.includes('ACCESS')) return 'eye';
    if (action.includes('PASSWORD')) return 'key';
    return 'activity';
  }

  get visiblePages(): number[] {
    const pages: number[] = [];
    const start = Math.max(1, this.currentPage - 2);
    const end = Math.min(this.totalPages, this.currentPage + 2);
    
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  }
}
