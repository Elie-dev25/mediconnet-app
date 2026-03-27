import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, DashboardLayoutComponent, MenuItem, ALL_ICONS_PROVIDER } from '../../../shared';
import { PHARMACIEN_MENU_ITEMS, PHARMACIEN_SIDEBAR_TITLE } from '../shared';
import { 
  PharmacieStockService, 
  VenteDirecte,
  VenteDirecteLigne,
  CreateVenteDirecteRequest,
  VenteDirecteLigneRequest,
  VenteDirecteResult,
  VenteDirecteFilter,
  MedicamentStock,
  PagedResult
} from '../../../services/pharmacie-stock.service';
import { FormatService } from '../../../shared/services/format.service';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

interface LigneVente {
  medicament: MedicamentStock;
  quantite: number;
  prixUnitaire: number;
  montantTotal: number;
}

@Component({
  selector: 'app-pharmacien-ventes-directes',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, DashboardLayoutComponent, PaginationComponent],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './ventes-directes.component.html',
  styleUrls: ['./ventes-directes.component.scss']
})
export class PharmacienVentesDirectesComponent implements OnInit {
  menuItems: MenuItem[] = PHARMACIEN_MENU_ITEMS;
  sidebarTitle = PHARMACIEN_SIDEBAR_TITLE;

  // Liste des ventes
  ventes: VenteDirecte[] = [];
  totalItems = 0;
  currentPage = 1;
  pageSize = 10;
  totalPages = 0;

  // Filtres
  filterDateDebut = '';
  filterDateFin = '';
  filterNomClient = '';
  filterNumeroTicket = '';

  // État
  isLoading = false;
  isProcessing = false;

  // Modal création vente
  showCreateModal = false;
  medicamentsDisponibles: MedicamentStock[] = [];
  medicamentsFiltres: MedicamentStock[] = [];
  searchMedicament = '';
  lignesVente: LigneVente[] = [];
  nomClient = '';
  telephoneClient = '';
  notes = '';
  modePaiement = 'especes';

  // Modal détail vente
  showDetailModal = false;
  selectedVente: VenteDirecte | null = null;

  // Modal succès
  showSuccessModal = false;
  lastResult: VenteDirecteResult | null = null;

  constructor(
    private stockService: PharmacieStockService,
    public formatService: FormatService
  ) {}

  ngOnInit(): void {
    this.loadVentes();
    this.loadMedicaments();
  }

  // ==================== Chargement des données ====================

  loadVentes(): void {
    this.isLoading = true;
    const filter: VenteDirecteFilter = {
      dateDebut: this.filterDateDebut || undefined,
      dateFin: this.filterDateFin || undefined,
      nomClient: this.filterNomClient || undefined,
      numeroTicket: this.filterNumeroTicket || undefined,
      page: this.currentPage,
      pageSize: this.pageSize
    };

    this.stockService.getVentesDirectes(filter).subscribe({
      next: (result) => {
        this.ventes = result.items;
        this.totalItems = result.totalItems;
        this.totalPages = result.totalPages;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Erreur chargement ventes directes', error);
        this.isLoading = false;
      }
    });
  }

  loadMedicaments(): void {
    this.stockService.getMedicaments('', '', 1, 1000).subscribe({
      next: (result) => {
        this.medicamentsDisponibles = result.items.filter(m => m.actif && (m.stock ?? 0) > 0);
        this.medicamentsFiltres = [...this.medicamentsDisponibles];
      },
      error: (error) => console.error('Erreur chargement médicaments', error)
    });
  }

  // ==================== Filtres et pagination ====================

  onSearch(): void {
    this.currentPage = 1;
    this.loadVentes();
  }

  resetFilters(): void {
    this.filterDateDebut = '';
    this.filterDateFin = '';
    this.filterNomClient = '';
    this.filterNumeroTicket = '';
    this.currentPage = 1;
    this.loadVentes();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadVentes();
  }

  // ==================== Modal création vente ====================

  openCreateModal(): void {
    this.resetForm();
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
    this.resetForm();
  }

  resetForm(): void {
    this.lignesVente = [];
    this.nomClient = '';
    this.telephoneClient = '';
    this.notes = '';
    this.modePaiement = 'especes';
    this.searchMedicament = '';
    this.medicamentsFiltres = [...this.medicamentsDisponibles];
  }

  filterMedicaments(): void {
    const search = this.searchMedicament.toLowerCase();
    this.medicamentsFiltres = this.medicamentsDisponibles.filter(m => 
      m.nom.toLowerCase().includes(search) ||
      (m.dosage && m.dosage.toLowerCase().includes(search))
    );
  }

  ajouterMedicament(med: MedicamentStock): void {
    // Vérifier si déjà ajouté
    const existe = this.lignesVente.find(l => l.medicament.idMedicament === med.idMedicament);
    if (existe) {
      // Incrémenter la quantité si stock disponible
      if (existe.quantite < (med.stock ?? 0)) {
        existe.quantite++;
        existe.montantTotal = existe.quantite * existe.prixUnitaire;
      }
      return;
    }

    // Ajouter nouvelle ligne
    this.lignesVente.push({
      medicament: med,
      quantite: 1,
      prixUnitaire: med.prix ?? 0,
      montantTotal: med.prix ?? 0
    });
  }

  supprimerLigne(index: number): void {
    this.lignesVente.splice(index, 1);
  }

  updateQuantite(ligne: LigneVente): void {
    // Limiter à la quantité en stock
    const stockMax = ligne.medicament.stock ?? 0;
    if (ligne.quantite > stockMax) {
      ligne.quantite = stockMax;
    }
    if (ligne.quantite < 1) {
      ligne.quantite = 1;
    }
    ligne.montantTotal = ligne.quantite * ligne.prixUnitaire;
  }

  get montantTotalVente(): number {
    return this.lignesVente.reduce((sum, l) => sum + (l.quantite * l.prixUnitaire), 0);
  }

  get canSubmit(): boolean {
    return this.lignesVente.length > 0 && !this.isProcessing;
  }

  submitVente(): void {
    if (!this.canSubmit) return;

    this.isProcessing = true;

    const request: CreateVenteDirecteRequest = {
      lignes: this.lignesVente.map(l => ({
        idMedicament: l.medicament.idMedicament,
        quantite: l.quantite
      })),
      nomClient: this.nomClient || undefined,
      telephoneClient: this.telephoneClient || undefined,
      notes: this.notes || undefined,
      modePaiement: this.modePaiement
    };

    this.stockService.creerVenteDirecte(request).subscribe({
      next: (result) => {
        this.isProcessing = false;
        if (result.success) {
          this.lastResult = result;
          this.closeCreateModal();
          this.showSuccessModal = true;
          this.loadVentes();
          this.loadMedicaments(); // Recharger les stocks
        } else {
          alert(result.message + '\n' + result.erreurs.join('\n'));
        }
      },
      error: (error) => {
        this.isProcessing = false;
        console.error('Erreur création vente directe', error);
        alert('Erreur lors de la création de la vente');
      }
    });
  }

  // ==================== Modal détail ====================

  openDetailModal(vente: VenteDirecte): void {
    this.selectedVente = vente;
    this.showDetailModal = true;
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    this.selectedVente = null;
  }

  // ==================== Modal succès ====================

  closeSuccessModal(): void {
    this.showSuccessModal = false;
    this.lastResult = null;
  }

  // ==================== Helpers ====================

  getModePaiementLabel(mode: string): string {
    const labels: { [key: string]: string } = {
      'especes': 'Espèces',
      'carte': 'Carte bancaire',
      'mobile_money': 'Mobile Money',
      'cheque': 'Chèque'
    };
    return labels[mode] || mode;
  }

  getStatutClass(statut: string): string {
    const classes: { [key: string]: string } = {
      'en_attente': 'statut-en-attente',
      'paye': 'statut-paye',
      'delivre': 'statut-delivre',
      'terminee': 'statut-delivre',
      'annulee': 'statut-annule'
    };
    return classes[statut] || 'statut-en-attente';
  }

  getStatutLabel(statut: string): string {
    const labels: { [key: string]: string } = {
      'en_attente': 'En attente de paiement',
      'paye': 'Payé - À délivrer',
      'delivre': 'Délivré',
      'terminee': 'Délivré',
      'annulee': 'Annulée'
    };
    return labels[statut] || 'En attente';
  }

  delivrerVente(vente: VenteDirecte): void {
    if (this.isProcessing) return;
    
    this.isProcessing = true;
    this.stockService.delivrerVenteDirecte(vente.idDispensation).subscribe({
      next: (result) => {
        this.isProcessing = false;
        if (result.success) {
          // Mettre à jour le statut localement
          vente.statut = 'delivre';
          this.closeDetailModal();
          this.loadVentes();
          alert('Médicaments délivrés avec succès !');
        } else {
          alert(result.message || 'Erreur lors de la délivrance');
        }
      },
      error: (err) => {
        this.isProcessing = false;
        console.error('Erreur délivrance:', err);
        alert('Erreur lors de la délivrance des médicaments');
      }
    });
  }
}
