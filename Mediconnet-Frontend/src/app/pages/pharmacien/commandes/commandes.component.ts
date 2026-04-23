import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, DashboardLayoutComponent, MenuItem, ALL_ICONS_PROVIDER } from '../../../shared';
import { PHARMACIEN_MENU_ITEMS, PHARMACIEN_SIDEBAR_TITLE } from '../shared';
import { 
  PharmacieStockService, 
  CommandePharmacie,
  CreateCommandeRequest,
  CreateCommandeLigneRequest,
  ReceptionCommandeRequest,
  Fournisseur,
  MedicamentStock,
  CommandeLigne
} from '../../../services/pharmacie-stock.service';
import { FormatService } from '../../../shared/services/format.service';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { ModalManagerBase } from '../../../shared/base/modal-manager.base';

@Component({
  selector: 'app-pharmacien-commandes',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, DashboardLayoutComponent, PaginationComponent],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './commandes.component.html',
  styleUrls: ['./commandes.component.scss']
})
export class PharmacienCommandesComponent extends ModalManagerBase implements OnInit {
  menuItems: MenuItem[] = PHARMACIEN_MENU_ITEMS;
  sidebarTitle = PHARMACIEN_SIDEBAR_TITLE;

  commandes: CommandePharmacie[] = [];
  totalItems = 0;
  currentPage = 1;
  pageSize = 15;
  totalPages = 0;

  searchTerm = '';
  selectedStatut = '';
  isLoading = false;

  // Modal names constants
  readonly MODALS = {
    CREATE: 'create',
    EDIT: 'edit',
    DETAILS: 'details',
    RECEPTION: 'reception',
    DELETE: 'delete'
  };

  // Modal states (getters for template)
  get showCreateModal(): boolean { return this.isModalOpen(this.MODALS.CREATE); }
  get showEditModal(): boolean { return this.isModalOpen(this.MODALS.EDIT); }
  get showDetailsModal(): boolean { return this.isModalOpen(this.MODALS.DETAILS); }
  get showReceptionModal(): boolean { return this.isModalOpen(this.MODALS.RECEPTION); }
  get showDeleteModal(): boolean { return this.isModalOpen(this.MODALS.DELETE); }

  selectedCommande: CommandePharmacie | null = null;

  // Form data
  commandeForm: CreateCommandeRequest = {
    idFournisseur: 0,
    dateReceptionPrevue: '',
    notes: '',
    lignes: []
  };

  receptionForm: ReceptionCommandeRequest = {
    lignes: []
  };

  // Data for forms
  fournisseurs: Fournisseur[] = [];
  medicaments: MedicamentStock[] = [];
  selectedMedicament: MedicamentStock | null = null;

  // Form fields for adding ligne
  ligneForm: CreateCommandeLigneRequest = {
    idMedicament: 0,
    quantiteCommandee: 0,
    prixAchat: 0
  };

  statuts = [
    { value: '', label: 'Tous' },
    { value: 'brouillon', label: 'Brouillon' },
    { value: 'envoyee', label: 'Envoyée' },
    { value: 'partiellement_recue', label: 'Partiellement reçue' },
    { value: 'recue', label: 'Reçue' },
    { value: 'annulee', label: 'Annulée' }
  ];

  constructor(
    private stockService: PharmacieStockService,
    public formatService: FormatService
  ) {
    super();
  }

  ngOnInit(): void {
    this.loadCommandes();
    this.loadFournisseurs();
    this.loadMedicaments();
  }

  loadCommandes(): void {
    this.isLoading = true;
    this.stockService.getCommandes(
      this.selectedStatut || undefined,
      this.currentPage,
      this.pageSize
    ).subscribe({
      next: (result) => {
        this.commandes = result.items;
        this.totalItems = result.totalItems;
        this.totalPages = result.totalPages;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Erreur chargement commandes', error);
        this.isLoading = false;
      }
    });
  }

  loadFournisseurs(): void {
    this.stockService.getFournisseurs(true).subscribe({
      next: (fournisseurs) => {
        this.fournisseurs = fournisseurs;
      },
      error: (error) => console.error('Erreur chargement fournisseurs', error)
    });
  }

  loadMedicaments(): void {
    this.stockService.getMedicaments('', '', 1, 1000).subscribe({
      next: (result) => {
        this.medicaments = result.items;
      },
      error: (error) => console.error('Erreur chargement médicaments', error)
    });
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadCommandes();
  }

  onStatutChange(): void {
    this.currentPage = 1;
    this.loadCommandes();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadCommandes();
  }

  // Create modal
  openCreateModal(): void {
    this.resetForm();
    this.openModal(this.MODALS.CREATE);
  }

  closeCreateModal(): void {
    this.closeModal(this.MODALS.CREATE);
    this.resetForm();
  }

  addLigne(): void {
    if (!this.ligneForm.idMedicament || !this.ligneForm.quantiteCommandee || !this.ligneForm.prixAchat) {
      return;
    }

    const medicament = this.medicaments.find(m => m.idMedicament === this.ligneForm.idMedicament);
    if (!medicament) return;

    // Vérifier si le médicament n'est pas déjà ajouté
    const existingLigne = this.commandeForm.lignes.find(l => l.idMedicament === this.ligneForm.idMedicament);
    if (existingLigne) {
      existingLigne.quantiteCommandee += this.ligneForm.quantiteCommandee;
    } else {
      this.commandeForm.lignes.push({
        idMedicament: this.ligneForm.idMedicament,
        quantiteCommandee: this.ligneForm.quantiteCommandee,
        prixAchat: this.ligneForm.prixAchat
      });
    }

    this.resetLigneForm();
  }

  removeLigne(index: number): void {
    this.commandeForm.lignes.splice(index, 1);
  }

  createCommande(): void {
    if (!this.commandeForm.idFournisseur || this.commandeForm.lignes.length === 0) {
      return;
    }

    this.stockService.createCommande(this.commandeForm).subscribe({
      next: () => {
        this.closeCreateModal();
        this.loadCommandes();
      },
      error: (error) => console.error('Erreur création commande', error)
    });
  }

  // Details modal
  openDetailsModal(commande: CommandePharmacie): void {
    this.selectedCommande = commande;
    this.openModal(this.MODALS.DETAILS);
  }

  closeDetailsModal(): void {
    this.closeModal(this.MODALS.DETAILS);
    this.selectedCommande = null;
  }

  // Reception modal
  openReceptionModal(cmd: CommandePharmacie): void {
    this.selectedCommande = cmd;
    this.receptionForm = {
      lignes: cmd.lignes.map(ligne => ({
        idLigneCommande: ligne.idLigneCommande,
        quantiteRecue: ligne.quantiteCommandee - ligne.quantiteRecue
      }))
    };
    this.openModal(this.MODALS.RECEPTION);
  }

  closeReceptionModal(): void {
    this.closeModal(this.MODALS.RECEPTION);
    this.selectedCommande = null;
  }

  receptionnerCommande(): void {
    if (!this.selectedCommande) return;

    this.stockService.receptionnerCommande(this.selectedCommande.idCommande, this.receptionForm).subscribe({
      next: () => {
        this.closeReceptionModal();
        this.loadCommandes();
      },
      error: (error) => console.error('Erreur réception commande', error)
    });
  }

  // Delete modal
  openDeleteModal(cmd: CommandePharmacie): void {
    this.selectedCommande = cmd;
    this.openModal(this.MODALS.DELETE);
  }

  closeDeleteModal(): void {
    this.closeModal(this.MODALS.DELETE);
    this.selectedCommande = null;
  }

  annulerCommande(): void {
    if (!this.selectedCommande) return;

    this.stockService.annulerCommande(this.selectedCommande.idCommande).subscribe({
      next: () => {
        this.closeDeleteModal();
        this.loadCommandes();
      },
      error: (error) => console.error('Erreur annulation commande', error)
    });
  }

  private resetForm(): void {
    this.commandeForm = {
      idFournisseur: 0,
      dateReceptionPrevue: '',
      notes: '',
      lignes: []
    };
    this.resetLigneForm();
  }

  private resetLigneForm(): void {
    this.ligneForm = {
      idMedicament: 0,
      quantiteCommandee: 0,
      prixAchat: 0
    };
    this.selectedMedicament = null;
  }

  getStatutBadgeClass(statut: string): string {
    switch (statut) {
      case 'brouillon': return 'badge-secondary';
      case 'envoyee': return 'badge-primary';
      case 'partiellement_recue': return 'badge-warning';
      case 'recue': return 'badge-success';
      case 'annulee': return 'badge-danger';
      default: return 'badge-secondary';
    }
  }

  getStatutLabel(statut: string): string {
    switch (statut) {
      case 'brouillon': return 'Brouillon';
      case 'envoyee': return 'Envoyée';
      case 'partiellement_recue': return 'Partiellement reçue';
      case 'recue': return 'Reçue';
      case 'annulee': return 'Annulée';
      default: return statut;
    }
  }

  getTotalCommande(): number {
    return this.commandeForm.lignes.reduce((total, ligne) => 
      total + (ligne.quantiteCommandee * ligne.prixAchat), 0
    );
  }

  getTotalReception(): number {
    return this.receptionForm.lignes.reduce((total, ligne) => {
      const prixAchat = this.getPrixAchat(ligne.idLigneCommande);
      return total + (ligne.quantiteRecue * prixAchat);
    }, 0);
  }

  onMedicamentChange(): void {
    this.selectedMedicament = this.medicaments.find(m => m.idMedicament === this.ligneForm.idMedicament) || null;
    if (this.selectedMedicament) {
      this.ligneForm.prixAchat = this.selectedMedicament.prix || 0;
    }
  }

  // Helper methods for template
  getLigneCommande(idLigneCommande: number): CommandeLigne | undefined {
    return this.selectedCommande?.lignes.find(l => l.idLigneCommande === idLigneCommande);
  }

  getQuantiteCommandee(idLigneCommande: number): number {
    const ligne = this.getLigneCommande(idLigneCommande);
    return ligne?.quantiteCommandee || 0;
  }

  getNomMedicament(idLigneCommande: number): string {
    const ligne = this.getLigneCommande(idLigneCommande);
    return ligne?.nomMedicament || '';
  }

  getPrixAchat(idLigneCommande: number): number {
    const ligne = this.getLigneCommande(idLigneCommande);
    return ligne?.prixAchat || 0;
  }

  getMedicamentName(idMedicament: number): string {
    const medicament = this.medicaments.find(m => m.idMedicament === idMedicament);
    return medicament?.nom || '';
  }
}
