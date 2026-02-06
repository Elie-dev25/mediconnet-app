import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DashboardLayoutComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { ADMIN_MENU_ITEMS, ADMIN_SIDEBAR_TITLE } from '../shared/admin-menu.config';
import { 
  AdminSettingsService, 
  ChambreAdminDto, 
  LitAdminDto,
  ChambresStats,
  CreateChambreRequest,
  CreateLitRequest,
  StandardChambreDto,
  CreateStandardChambreRequest,
  StandardChambreSelectDto
} from '../../../services/admin-settings.service';
import { 
  AuditService, 
  AuditLogDto, 
  AuditStatsDto, 
  AuditLogsFilter 
} from '../../../services/audit.service';

type TabType = 'chambres' | 'laboratoires' | 'logs';
type ChambreSubTabType = 'chambres' | 'standards';

@Component({
  selector: 'app-admin-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, DashboardLayoutComponent, LucideAngularModule],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class AdminSettingsComponent implements OnInit {
  menuItems = ADMIN_MENU_ITEMS;
  sidebarTitle = ADMIN_SIDEBAR_TITLE;

  activeTab: TabType = 'chambres';
  chambreSubTab: ChambreSubTabType = 'chambres';
  
  // Chambres
  chambres: ChambreAdminDto[] = [];
  stats: ChambresStats | null = null;
  isLoading = false;
  error: string | null = null;

  // Modal Chambre
  showChambreModal = false;
  editingChambre: ChambreAdminDto | null = null;
  chambreForm: CreateChambreRequest = {
    numero: '',
    capacite: 1,
    etat: 'bon',
    statut: 'actif'
  };

  // Modal Lit
  showLitModal = false;
  selectedChambre: ChambreAdminDto | null = null;
  editingLit: LitAdminDto | null = null;
  litForm: CreateLitRequest = {
    numero: '',
    statut: 'libre'
  };

  // Expanded chambres
  expandedChambres: Set<number> = new Set();

  // Confirmation suppression
  showDeleteConfirm = false;
  deleteTarget: { type: 'chambre' | 'lit' | 'standard'; id: number; label: string } | null = null;

  // Standards de chambre
  standards: StandardChambreDto[] = [];
  standardsForSelect: StandardChambreSelectDto[] = [];
  isLoadingStandards = false;
  showStandardModal = false;
  editingStandard: StandardChambreDto | null = null;
  standardForm: CreateStandardChambreRequest = {
    nom: '',
    description: '',
    prixJournalier: 0,
    privileges: [],
    localisation: ''
  };
  newPrivilege = '';

  // Audit Logs
  auditLogs: AuditLogDto[] = [];
  auditStats: AuditStatsDto | null = null;
  isLoadingLogs = false;
  availableActions: string[] = [];
  availableResources: string[] = [];
  logsFilter: AuditLogsFilter = {
    page: 1,
    pageSize: 50
  };
  logsPagination = {
    page: 1,
    pageSize: 50,
    totalCount: 0,
    totalPages: 0
  };

  constructor(
    private settingsService: AdminSettingsService,
    public auditService: AuditService
  ) {}

  ngOnInit(): void {
    this.loadChambres();
  }

  setActiveTab(tab: TabType): void {
    this.activeTab = tab;
    if (tab === 'chambres') {
      this.loadChambres();
      this.loadStandardsForSelect();
      if (this.chambreSubTab === 'standards') {
        this.loadStandards();
      }
    } else if (tab === 'logs') {
      this.loadLogsData();
    }
  }

  setChambreSubTab(subTab: ChambreSubTabType): void {
    this.chambreSubTab = subTab;
    if (subTab === 'standards') {
      this.loadStandards();
    } else {
      this.loadChambres();
      this.loadStandardsForSelect();
    }
  }

  // ==================== AUDIT LOGS ====================

  loadLogsData(): void {
    this.loadLogs();
    this.loadAuditStats();
    this.loadAvailableFilters();
  }

  loadLogs(): void {
    this.isLoadingLogs = true;
    this.auditService.getLogs(this.logsFilter).subscribe({
      next: (result) => {
        this.auditLogs = result.logs;
        this.logsPagination = {
          page: result.page,
          pageSize: result.pageSize,
          totalCount: result.totalCount,
          totalPages: result.totalPages
        };
        this.isLoadingLogs = false;
      },
      error: (err) => {
        console.error('Erreur chargement logs:', err);
        this.isLoadingLogs = false;
      }
    });
  }

  loadAuditStats(): void {
    this.auditService.getStats(7).subscribe({
      next: (stats) => {
        this.auditStats = stats;
      },
      error: (err) => {
        console.error('Erreur chargement stats audit:', err);
      }
    });
  }

  loadAvailableFilters(): void {
    this.auditService.getAvailableActions().subscribe({
      next: (actions) => this.availableActions = actions,
      error: (err) => console.error('Erreur chargement actions:', err)
    });
    
    this.auditService.getAvailableResourceTypes().subscribe({
      next: (resources) => this.availableResources = resources,
      error: (err) => console.error('Erreur chargement resources:', err)
    });
  }

  resetLogsFilter(): void {
    this.logsFilter = {
      page: 1,
      pageSize: 50
    };
    this.loadLogs();
  }

  goToLogsPage(page: number): void {
    if (page >= 1 && page <= this.logsPagination.totalPages) {
      this.logsFilter.page = page;
      this.loadLogs();
    }
  }

  // ==================== STANDARDS DE CHAMBRE ====================

  loadStandards(): void {
    this.isLoadingStandards = true;
    this.settingsService.getStandards().subscribe({
      next: (response) => {
        this.standards = response.data;
        this.isLoadingStandards = false;
      },
      error: (err) => {
        console.error('Erreur chargement standards:', err);
        this.error = 'Impossible de charger les standards';
        this.isLoadingStandards = false;
      }
    });
  }

  loadStandardsForSelect(): void {
    this.settingsService.getStandardsForSelect().subscribe({
      next: (response) => {
        this.standardsForSelect = response.data;
      },
      error: (err) => {
        console.error('Erreur chargement standards pour select:', err);
      }
    });
  }

  openStandardModal(standard?: StandardChambreDto): void {
    if (standard) {
      this.editingStandard = standard;
      this.standardForm = {
        nom: standard.nom,
        description: standard.description || '',
        prixJournalier: standard.prixJournalier,
        privileges: [...standard.privileges],
        localisation: standard.localisation || ''
      };
    } else {
      this.editingStandard = null;
      this.standardForm = {
        nom: '',
        description: '',
        prixJournalier: 0,
        privileges: [],
        localisation: ''
      };
    }
    this.newPrivilege = '';
    this.showStandardModal = true;
  }

  closeStandardModal(): void {
    this.showStandardModal = false;
    this.editingStandard = null;
  }

  addPrivilege(): void {
    if (this.newPrivilege.trim() && !this.standardForm.privileges.includes(this.newPrivilege.trim())) {
      this.standardForm.privileges.push(this.newPrivilege.trim());
      this.newPrivilege = '';
    }
  }

  removePrivilege(index: number): void {
    this.standardForm.privileges.splice(index, 1);
  }

  saveStandard(): void {
    if (!this.standardForm.nom.trim() || this.standardForm.prixJournalier <= 0) return;

    this.isLoadingStandards = true;

    if (this.editingStandard) {
      this.settingsService.updateStandard(this.editingStandard.idStandard, this.standardForm).subscribe({
        next: (response) => {
          if (response.success) {
            this.loadStandards();
            this.closeStandardModal();
          } else {
            this.error = response.message || 'Erreur lors de la mise à jour';
          }
          this.isLoadingStandards = false;
        },
        error: () => {
          this.error = 'Erreur lors de la mise à jour';
          this.isLoadingStandards = false;
        }
      });
    } else {
      this.settingsService.createStandard(this.standardForm).subscribe({
        next: (response) => {
          if (response.success) {
            this.loadStandards();
            this.closeStandardModal();
          } else {
            this.error = response.message || 'Erreur lors de la création';
          }
          this.isLoadingStandards = false;
        },
        error: () => {
          this.error = 'Erreur lors de la création';
          this.isLoadingStandards = false;
        }
      });
    }
  }

  confirmDeleteStandard(standard: StandardChambreDto): void {
    this.deleteTarget = {
      type: 'standard',
      id: standard.idStandard,
      label: `Standard "${standard.nom}"`
    };
    this.showDeleteConfirm = true;
  }

  toggleStandardActif(standard: StandardChambreDto): void {
    this.settingsService.updateStandard(standard.idStandard, { actif: !standard.actif }).subscribe({
      next: () => {
        this.loadStandards();
      },
      error: () => {
        this.error = 'Erreur lors de la mise à jour';
      }
    });
  }

  // ==================== CHAMBRES ====================

  loadChambres(): void {
    this.isLoading = true;
    this.error = null;
    
    this.settingsService.getChambres().subscribe({
      next: (response) => {
        this.chambres = response.chambres;
        this.stats = response.stats;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur:', err);
        this.error = 'Impossible de charger les chambres';
        this.isLoading = false;
      }
    });
  }

  toggleChambreExpand(chambreId: number): void {
    if (this.expandedChambres.has(chambreId)) {
      this.expandedChambres.delete(chambreId);
    } else {
      this.expandedChambres.add(chambreId);
    }
  }

  isChambreExpanded(chambreId: number): boolean {
    return this.expandedChambres.has(chambreId);
  }

  openChambreModal(chambre?: ChambreAdminDto): void {
    this.loadStandardsForSelect();
    if (chambre) {
      this.editingChambre = chambre;
      this.chambreForm = {
        numero: chambre.numero,
        capacite: chambre.capacite,
        etat: chambre.etat,
        statut: chambre.statut,
        idStandard: chambre.idStandard
      };
    } else {
      this.editingChambre = null;
      this.chambreForm = {
        numero: '',
        capacite: 1,
        etat: 'bon',
        statut: 'actif',
        idStandard: undefined
      };
    }
    this.showChambreModal = true;
  }

  closeChambreModal(): void {
    this.showChambreModal = false;
    this.editingChambre = null;
  }

  saveChambre(): void {
    if (!this.chambreForm.numero.trim()) return;

    this.isLoading = true;
    
    if (this.editingChambre) {
      this.settingsService.updateChambre(this.editingChambre.idChambre, this.chambreForm).subscribe({
        next: (response) => {
          if (response.success) {
            this.loadChambres();
            this.closeChambreModal();
          } else {
            this.error = response.message;
          }
          this.isLoading = false;
        },
        error: () => {
          this.error = 'Erreur lors de la mise à jour';
          this.isLoading = false;
        }
      });
    } else {
      this.settingsService.createChambre(this.chambreForm).subscribe({
        next: (response) => {
          if (response.success) {
            this.loadChambres();
            this.closeChambreModal();
          } else {
            this.error = response.message;
          }
          this.isLoading = false;
        },
        error: () => {
          this.error = 'Erreur lors de la création';
          this.isLoading = false;
        }
      });
    }
  }

  confirmDeleteChambre(chambre: ChambreAdminDto): void {
    this.deleteTarget = {
      type: 'chambre',
      id: chambre.idChambre,
      label: `Chambre ${chambre.numero}`
    };
    this.showDeleteConfirm = true;
  }

  // ==================== LITS ====================

  openLitModal(chambre: ChambreAdminDto, lit?: LitAdminDto): void {
    this.selectedChambre = chambre;
    if (lit) {
      this.editingLit = lit;
      this.litForm = {
        numero: lit.numero,
        statut: lit.statut
      };
    } else {
      this.editingLit = null;
      this.litForm = {
        numero: '',
        statut: 'libre'
      };
    }
    this.showLitModal = true;
  }

  closeLitModal(): void {
    this.showLitModal = false;
    this.selectedChambre = null;
    this.editingLit = null;
  }

  saveLit(): void {
    if (!this.litForm.numero.trim() || !this.selectedChambre) return;

    this.isLoading = true;

    if (this.editingLit) {
      this.settingsService.updateLit(this.editingLit.idLit, this.litForm).subscribe({
        next: (response) => {
          if (response.success) {
            this.loadChambres();
            this.closeLitModal();
          } else {
            this.error = response.message;
          }
          this.isLoading = false;
        },
        error: () => {
          this.error = 'Erreur lors de la mise à jour';
          this.isLoading = false;
        }
      });
    } else {
      this.settingsService.addLit(this.selectedChambre.idChambre, this.litForm).subscribe({
        next: (response) => {
          if (response.success) {
            this.loadChambres();
            this.closeLitModal();
            // Expand la chambre pour voir le nouveau lit
            this.expandedChambres.add(this.selectedChambre!.idChambre);
          } else {
            this.error = response.message;
          }
          this.isLoading = false;
        },
        error: () => {
          this.error = 'Erreur lors de la création';
          this.isLoading = false;
        }
      });
    }
  }

  confirmDeleteLit(lit: LitAdminDto): void {
    this.deleteTarget = {
      type: 'lit',
      id: lit.idLit,
      label: `Lit ${lit.numero}`
    };
    this.showDeleteConfirm = true;
  }

  // ==================== DELETE ====================

  cancelDelete(): void {
    this.showDeleteConfirm = false;
    this.deleteTarget = null;
  }

  executeDelete(): void {
    if (!this.deleteTarget) return;

    this.isLoading = true;

    if (this.deleteTarget.type === 'chambre') {
      this.settingsService.deleteChambre(this.deleteTarget.id).subscribe({
        next: (response) => {
          if (response.success) {
            this.loadChambres();
          } else {
            this.error = response.message;
          }
          this.isLoading = false;
          this.cancelDelete();
        },
        error: () => {
          this.error = 'Erreur lors de la suppression';
          this.isLoading = false;
          this.cancelDelete();
        }
      });
    } else if (this.deleteTarget.type === 'standard') {
      this.settingsService.deleteStandard(this.deleteTarget.id).subscribe({
        next: (response) => {
          if (response.success) {
            this.loadStandards();
          } else {
            this.error = response.message;
          }
          this.isLoading = false;
          this.cancelDelete();
        },
        error: (err) => {
          this.error = err.error?.message || 'Erreur lors de la suppression';
          this.isLoading = false;
          this.cancelDelete();
        }
      });
    } else {
      this.settingsService.deleteLit(this.deleteTarget.id).subscribe({
        next: (response) => {
          if (response.success) {
            this.loadChambres();
          } else {
            this.error = response.message;
          }
          this.isLoading = false;
          this.cancelDelete();
        },
        error: () => {
          this.error = 'Erreur lors de la suppression';
          this.isLoading = false;
          this.cancelDelete();
        }
      });
    }
  }

  // ==================== HELPERS ====================

  getStatutLabel(statut: string): string {
    const labels: Record<string, string> = {
      'libre': 'Libre',
      'occupe': 'Occupé',
      'hors_service': 'Hors service',
      'actif': 'Actif',
      'inactif': 'Inactif'
    };
    return labels[statut] || statut;
  }

  getStatutClass(statut: string): string {
    const classes: Record<string, string> = {
      'libre': 'status-libre',
      'occupe': 'status-occupe',
      'hors_service': 'status-hors-service',
      'actif': 'status-actif',
      'inactif': 'status-inactif'
    };
    return classes[statut] || '';
  }

  getEtatLabel(etat: string): string {
    const labels: Record<string, string> = {
      'bon': 'Bon état',
      'moyen': 'État moyen',
      'mauvais': 'Mauvais état',
      'renovation': 'En rénovation'
    };
    return labels[etat] || etat;
  }

  dismissError(): void {
    this.error = null;
  }
}
