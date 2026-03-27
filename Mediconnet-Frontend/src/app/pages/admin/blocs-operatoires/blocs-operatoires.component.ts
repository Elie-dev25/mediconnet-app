import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DashboardLayoutComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { ADMIN_MENU_ITEMS, ADMIN_SIDEBAR_TITLE } from '../shared/admin-menu.config';
import {
  BlocOperatoireService,
  BlocOperatoireDto,
  BlocOperatoireListDto,
  CreateBlocOperatoireRequest,
  UpdateBlocOperatoireRequest,
  ReservationBlocListDto,
  AgendaBlocDto,
  STATUTS_BLOC
} from '../../../services/bloc-operatoire.service';

type TabType = 'liste' | 'agenda';

@Component({
  selector: 'app-blocs-operatoires',
  standalone: true,
  imports: [CommonModule, FormsModule, DashboardLayoutComponent, LucideAngularModule],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './blocs-operatoires.component.html',
  styleUrl: './blocs-operatoires.component.scss'
})
export class BlocsOperatoiresComponent implements OnInit {
  menuItems = ADMIN_MENU_ITEMS;
  sidebarTitle = ADMIN_SIDEBAR_TITLE;

  activeTab: TabType = 'liste';
  
  // Liste des blocs
  blocs: BlocOperatoireListDto[] = [];
  isLoading = false;
  error: string | null = null;
  successMessage: string | null = null;

  // Bloc sélectionné pour détails
  selectedBloc: BlocOperatoireDto | null = null;
  showDetailsPanel = false;

  // Modal création/édition
  showBlocModal = false;
  editingBloc: BlocOperatoireDto | null = null;
  blocForm: CreateBlocOperatoireRequest = {
    nom: '',
    description: '',
    localisation: '',
    capacite: undefined,
    equipements: ''
  };

  // Confirmation suppression
  showDeleteConfirm = false;
  blocToDelete: BlocOperatoireListDto | null = null;

  // Agenda
  selectedDate: string = new Date().toISOString().split('T')[0];
  agendas: AgendaBlocDto[] = [];
  isLoadingAgenda = false;
  selectedAgendaBloc: number | null = null;

  // Réservations du bloc sélectionné
  reservations: ReservationBlocListDto[] = [];
  isLoadingReservations = false;

  constructor(private blocService: BlocOperatoireService) {}

  ngOnInit(): void {
    this.loadBlocs();
  }

  setActiveTab(tab: TabType): void {
    this.activeTab = tab;
    if (tab === 'agenda') {
      this.loadAgendas();
    }
  }

  // ==================== GESTION DES BLOCS ====================

  loadBlocs(): void {
    this.isLoading = true;
    this.error = null;
    this.blocService.getAllBlocs().subscribe({
      next: (blocs) => {
        this.blocs = blocs;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement blocs:', err);
        this.error = 'Impossible de charger les blocs opératoires';
        this.isLoading = false;
      }
    });
  }

  openBlocModal(bloc?: BlocOperatoireDto): void {
    if (bloc) {
      this.editingBloc = bloc;
      this.blocForm = {
        nom: bloc.nom,
        description: bloc.description || '',
        localisation: bloc.localisation || '',
        capacite: bloc.capacite,
        equipements: bloc.equipements || ''
      };
    } else {
      this.editingBloc = null;
      this.blocForm = {
        nom: '',
        description: '',
        localisation: '',
        capacite: undefined,
        equipements: ''
      };
    }
    this.showBlocModal = true;
  }

  closeBlocModal(): void {
    this.showBlocModal = false;
    this.editingBloc = null;
  }

  saveBloc(): void {
    if (!this.blocForm.nom.trim()) {
      this.error = 'Le nom du bloc est requis';
      return;
    }

    this.isLoading = true;
    this.error = null;

    if (this.editingBloc) {
      const updateRequest: UpdateBlocOperatoireRequest = {
        nom: this.blocForm.nom,
        description: this.blocForm.description,
        localisation: this.blocForm.localisation,
        capacite: this.blocForm.capacite,
        equipements: this.blocForm.equipements
      };

      this.blocService.updateBloc(this.editingBloc.idBloc, updateRequest).subscribe({
        next: () => {
          this.successMessage = 'Bloc opératoire mis à jour avec succès';
          this.closeBlocModal();
          this.loadBlocs();
          this.clearMessages();
        },
        error: (err) => {
          console.error('Erreur mise à jour bloc:', err);
          this.error = err.error?.message || 'Erreur lors de la mise à jour';
          this.isLoading = false;
        }
      });
    } else {
      this.blocService.createBloc(this.blocForm).subscribe({
        next: () => {
          this.successMessage = 'Bloc opératoire créé avec succès';
          this.closeBlocModal();
          this.loadBlocs();
          this.clearMessages();
        },
        error: (err) => {
          console.error('Erreur création bloc:', err);
          this.error = err.error?.message || 'Erreur lors de la création';
          this.isLoading = false;
        }
      });
    }
  }

  confirmDelete(bloc: BlocOperatoireListDto): void {
    this.blocToDelete = bloc;
    this.showDeleteConfirm = true;
  }

  cancelDelete(): void {
    this.blocToDelete = null;
    this.showDeleteConfirm = false;
  }

  deleteBloc(): void {
    if (!this.blocToDelete) return;

    this.isLoading = true;
    this.blocService.deleteBloc(this.blocToDelete.idBloc).subscribe({
      next: () => {
        this.successMessage = 'Bloc opératoire supprimé avec succès';
        this.cancelDelete();
        this.loadBlocs();
        this.clearMessages();
      },
      error: (err) => {
        console.error('Erreur suppression bloc:', err);
        this.error = err.error?.message || 'Impossible de supprimer ce bloc (réservations actives)';
        this.isLoading = false;
        this.cancelDelete();
      }
    });
  }

  // ==================== DÉTAILS DU BLOC ====================

  viewBlocDetails(bloc: BlocOperatoireListDto): void {
    this.isLoading = true;
    this.blocService.getBlocById(bloc.idBloc).subscribe({
      next: (details) => {
        this.selectedBloc = details;
        this.showDetailsPanel = true;
        this.loadBlocReservations(bloc.idBloc);
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement détails:', err);
        this.error = 'Impossible de charger les détails du bloc';
        this.isLoading = false;
      }
    });
  }

  closeDetailsPanel(): void {
    this.showDetailsPanel = false;
    this.selectedBloc = null;
    this.reservations = [];
  }

  loadBlocReservations(idBloc: number): void {
    this.isLoadingReservations = true;
    const today = new Date().toISOString().split('T')[0];
    const nextMonth = new Date();
    nextMonth.setMonth(nextMonth.getMonth() + 1);
    const dateFin = nextMonth.toISOString().split('T')[0];

    this.blocService.getReservationsByBloc(idBloc, today, dateFin).subscribe({
      next: (reservations) => {
        this.reservations = reservations;
        this.isLoadingReservations = false;
      },
      error: (err) => {
        console.error('Erreur chargement réservations:', err);
        this.isLoadingReservations = false;
      }
    });
  }

  toggleBlocStatus(bloc: BlocOperatoireDto): void {
    const newStatut = bloc.statut === 'maintenance' ? 'libre' : 'maintenance';
    this.blocService.updateBloc(bloc.idBloc, { statut: newStatut }).subscribe({
      next: (updated) => {
        this.selectedBloc = updated;
        this.loadBlocs();
        this.successMessage = `Statut mis à jour: ${this.getStatutLabel(newStatut)}`;
        this.clearMessages();
      },
      error: (err) => {
        console.error('Erreur mise à jour statut:', err);
        this.error = 'Erreur lors de la mise à jour du statut';
      }
    });
  }

  toggleBlocActif(bloc: BlocOperatoireDto): void {
    this.blocService.updateBloc(bloc.idBloc, { actif: !bloc.actif }).subscribe({
      next: (updated) => {
        this.selectedBloc = updated;
        this.loadBlocs();
        this.successMessage = bloc.actif ? 'Bloc désactivé' : 'Bloc activé';
        this.clearMessages();
      },
      error: (err) => {
        console.error('Erreur mise à jour actif:', err);
        this.error = 'Erreur lors de la mise à jour';
      }
    });
  }

  // ==================== AGENDA ====================

  loadAgendas(): void {
    this.isLoadingAgenda = true;
    this.blocService.getAgendaTousBlocs(this.selectedDate).subscribe({
      next: (agendas) => {
        this.agendas = agendas;
        this.isLoadingAgenda = false;
      },
      error: (err) => {
        console.error('Erreur chargement agendas:', err);
        this.isLoadingAgenda = false;
      }
    });
  }

  onDateChange(): void {
    this.loadAgendas();
  }

  goToPreviousDay(): void {
    const date = new Date(this.selectedDate);
    date.setDate(date.getDate() - 1);
    this.selectedDate = date.toISOString().split('T')[0];
    this.loadAgendas();
  }

  goToNextDay(): void {
    const date = new Date(this.selectedDate);
    date.setDate(date.getDate() + 1);
    this.selectedDate = date.toISOString().split('T')[0];
    this.loadAgendas();
  }

  goToToday(): void {
    this.selectedDate = new Date().toISOString().split('T')[0];
    this.loadAgendas();
  }

  selectAgendaBloc(idBloc: number): void {
    this.selectedAgendaBloc = this.selectedAgendaBloc === idBloc ? null : idBloc;
  }

  // ==================== HELPERS ====================

  getStatutLabel(statut: string): string {
    return this.blocService.getStatutLabel(statut);
  }

  getStatutClass(statut: string): string {
    const color = this.blocService.getStatutColor(statut);
    return `badge-${color}`;
  }

  getReservationStatutLabel(statut: string): string {
    return this.blocService.getReservationStatutLabel(statut);
  }

  getReservationStatutClass(statut: string): string {
    const color = this.blocService.getReservationStatutColor(statut);
    return `badge-${color}`;
  }

  formatDuree(minutes: number): string {
    return this.blocService.formatDuree(minutes);
  }

  getReservedCount(agenda: AgendaBlocDto): number {
    return (agenda.creneaux || []).filter(c => c.estReserve).length;
  }

  getAvailableCount(agenda: AgendaBlocDto): number {
    return (agenda.creneaux || []).filter(c => !c.estReserve).length;
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    });
  }

  formatDateShort(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  private clearMessages(): void {
    setTimeout(() => {
      this.successMessage = null;
      this.error = null;
    }, 3000);
  }

  get statuts() {
    return STATUTS_BLOC;
  }
}
