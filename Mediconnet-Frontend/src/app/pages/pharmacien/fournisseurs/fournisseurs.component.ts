import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, DashboardLayoutComponent, MenuItem, ALL_ICONS_PROVIDER } from '../../../shared';
import { PHARMACIEN_MENU_ITEMS, PHARMACIEN_SIDEBAR_TITLE } from '../shared';
import { 
  PharmacieStockService, 
  Fournisseur, 
  CreateFournisseurRequest,
  UpdateFournisseurRequest
} from '../../../services/pharmacie-stock.service';
import { FormatService } from '../../../shared/services/format.service';
import { ModalManagerBase } from '../../../shared/base/modal-manager.base';

@Component({
  selector: 'app-pharmacien-fournisseurs',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, DashboardLayoutComponent],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './fournisseurs.component.html',
  styleUrls: ['./fournisseurs.component.scss']
})
export class PharmacienFournisseursComponent extends ModalManagerBase implements OnInit {
  menuItems: MenuItem[] = PHARMACIEN_MENU_ITEMS;
  sidebarTitle = PHARMACIEN_SIDEBAR_TITLE;

  fournisseurs: Fournisseur[] = [];
  fournisseursFiltres: Fournisseur[] = [];
  isLoading = false;
  searchQuery = '';
  showActifsOnly = true;

  // Modal names constants
  readonly MODALS = {
    CREATE: 'create',
    EDIT: 'edit',
    DELETE: 'delete'
  };

  // Modal states (getters for template)
  get showCreateModal(): boolean { return this.isModalOpen(this.MODALS.CREATE); }
  get showEditModal(): boolean { return this.isModalOpen(this.MODALS.EDIT); }
  get showDeleteModal(): boolean { return this.isModalOpen(this.MODALS.DELETE); }
  selectedFournisseur: Fournisseur | null = null;

  // Form data
  fournisseurForm: CreateFournisseurRequest = {
    nomFournisseur: '',
    contactNom: '',
    contactEmail: '',
    contactTelephone: '',
    adresse: '',
    conditionsPaiement: '',
    delaiLivraisonJours: 7,
    actif: true
  };

  editForm: UpdateFournisseurRequest = {};

  constructor(
    private stockService: PharmacieStockService,
    public formatService: FormatService
  ) {
    super();
  }

  ngOnInit(): void {
    this.loadFournisseurs();
  }

  loadFournisseurs(): void {
    this.isLoading = true;
    this.stockService.getFournisseurs(this.showActifsOnly ? true : undefined).subscribe({
      next: (fournisseurs) => {
        this.fournisseurs = fournisseurs;
        this.fournisseursFiltres = fournisseurs;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Erreur chargement fournisseurs', error);
        this.isLoading = false;
      }
    });
  }

  onSearch(): void {
    if (this.searchQuery.trim()) {
      const query = this.searchQuery.toLowerCase();
      this.fournisseursFiltres = this.fournisseurs.filter(f => 
        f.nomFournisseur.toLowerCase().includes(query) ||
        f.contactNom?.toLowerCase().includes(query) ||
        f.contactEmail?.toLowerCase().includes(query) ||
        f.contactTelephone?.includes(query)
      );
    } else {
      this.fournisseursFiltres = this.fournisseurs;
    }
  }

  onToggleActifs(): void {
    this.showActifsOnly = !this.showActifsOnly;
    this.loadFournisseurs();
  }

  openCreateModal(): void {
    this.fournisseurForm = {
      nomFournisseur: '',
      contactNom: '',
      contactEmail: '',
      contactTelephone: '',
      adresse: '',
      conditionsPaiement: '',
      delaiLivraisonJours: 7,
      actif: true
    };
    this.openModal(this.MODALS.CREATE);
  }

  closeCreateModal(): void {
    this.closeModal(this.MODALS.CREATE);
  }

  createFournisseur(): void {
    if (!this.fournisseurForm.nomFournisseur.trim()) return;

    this.stockService.createFournisseur(this.fournisseurForm).subscribe({
      next: () => {
        this.closeCreateModal();
        this.loadFournisseurs();
      },
      error: (error) => {
        console.error('Erreur création fournisseur', error);
        alert('Erreur lors de la création du fournisseur');
      }
    });
  }

  openEditModal(fournisseur: Fournisseur): void {
    this.selectedFournisseur = fournisseur;
    this.editForm = {
      nomFournisseur: fournisseur.nomFournisseur,
      contactNom: fournisseur.contactNom,
      contactEmail: fournisseur.contactEmail,
      contactTelephone: fournisseur.contactTelephone,
      adresse: fournisseur.adresse,
      conditionsPaiement: fournisseur.conditionsPaiement,
      delaiLivraisonJours: fournisseur.delaiLivraisonJours,
      actif: fournisseur.actif
    };
    this.openModal(this.MODALS.EDIT);
  }

  closeEditModal(): void {
    this.closeModal(this.MODALS.EDIT);
    this.selectedFournisseur = null;
    this.editForm = {};
  }

  updateFournisseur(): void {
    if (!this.selectedFournisseur || !this.editForm.nomFournisseur?.trim()) return;

    this.stockService.updateFournisseur(this.selectedFournisseur.idFournisseur, this.editForm).subscribe({
      next: () => {
        this.closeEditModal();
        this.loadFournisseurs();
      },
      error: (error) => {
        console.error('Erreur mise à jour fournisseur', error);
        alert('Erreur lors de la mise à jour du fournisseur');
      }
    });
  }

  openDeleteModal(fournisseur: Fournisseur): void {
    this.selectedFournisseur = fournisseur;
    this.openModal(this.MODALS.DELETE);
  }

  closeDeleteModal(): void {
    this.closeModal(this.MODALS.DELETE);
    this.selectedFournisseur = null;
  }

  deleteFournisseur(): void {
    if (!this.selectedFournisseur) return;

    this.stockService.deleteFournisseur(this.selectedFournisseur.idFournisseur).subscribe({
      next: () => {
        this.closeDeleteModal();
        this.loadFournisseurs();
      },
      error: (error) => {
        console.error('Erreur suppression fournisseur', error);
        alert('Erreur lors de la suppression du fournisseur');
      }
    });
  }

  toggleActif(fournisseur: Fournisseur): void {
    this.stockService.toggleFournisseurStatut(fournisseur.idFournisseur).subscribe({
      next: () => {
        this.loadFournisseurs();
      },
      error: (error) => {
        console.error('Erreur changement statut fournisseur', error);
      }
    });
  }

  getStatutClass(actif: boolean): string {
    return actif ? 'status-active' : 'status-inactive';
  }

  getStatutLabel(actif: boolean): string {
    return actif ? 'Actif' : 'Inactif';
  }

}
