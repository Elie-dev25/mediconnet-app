import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, DashboardLayoutComponent, MenuItem, ALL_ICONS_PROVIDER } from '../../../shared';
import { PHARMACIEN_MENU_ITEMS, PHARMACIEN_SIDEBAR_TITLE } from '../shared';
import { 
  PharmacieStockService, 
  OrdonnancePharmacie,
  MedicamentPrescrit,
  CreateDispensationRequest,
  DispensationLigneRequest,
  PagedResult,
  ValidationOrdonnanceResult,
  DelivranceResult
} from '../../../services/pharmacie-stock.service';
import { FormatService } from '../../../shared/services/format.service';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-pharmacien-ordonnances',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, DashboardLayoutComponent, PaginationComponent],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './ordonnances.component.html',
  styleUrls: ['./ordonnances.component.scss']
})
export class PharmacienOrdonnancesComponent implements OnInit {
  menuItems: MenuItem[] = PHARMACIEN_MENU_ITEMS;
  sidebarTitle = PHARMACIEN_SIDEBAR_TITLE;
  Math = Math;

  ordonnances: OrdonnancePharmacie[] = [];
  totalItems = 0;
  currentPage = 1;
  pageSize = 10;
  totalPages = 0;

  searchTerm = '';
  isLoading = false;
  isProcessing = false;

  showDispensationModal = false;
  selectedOrdonnance: OrdonnancePharmacie | null = null;
  dispensationLignes: { med: MedicamentPrescrit; quantite: number; selected: boolean }[] = [];
  dispensationNotes = '';

  constructor(
    private stockService: PharmacieStockService,
    public formatService: FormatService
  ) {}

  ngOnInit(): void {
    this.loadOrdonnances();
  }

  loadOrdonnances(): void {
    this.isLoading = true;
    this.stockService.getOrdonnances(
      this.searchTerm || undefined,
      this.currentPage,
      this.pageSize
    ).subscribe({
      next: (result) => {
        this.ordonnances = result.items;
        this.totalItems = result.totalItems;
        this.totalPages = result.totalPages;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Erreur chargement ordonnances', error);
        this.isLoading = false;
      }
    });
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadOrdonnances();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadOrdonnances();
  }

  openDispensationModal(ord: OrdonnancePharmacie): void {
    this.selectedOrdonnance = ord;
    this.dispensationLignes = ord.medicaments.map(med => ({
      med,
      quantite: Math.min(med.quantitePrescrite - med.quantiteDispensee, med.stockDisponible || 0),
      selected: (med.stockDisponible || 0) > 0 && (med.quantitePrescrite - med.quantiteDispensee) > 0
    }));
    this.dispensationNotes = '';
    this.showDispensationModal = true;
  }

  closeDispensationModal(): void {
    this.showDispensationModal = false;
    this.selectedOrdonnance = null;
    this.dispensationLignes = [];
  }

  dispenser(): void {
    if (!this.selectedOrdonnance) return;

    const lignes: DispensationLigneRequest[] = this.dispensationLignes
      .filter(l => l.selected && l.quantite > 0)
      .map(l => ({
        idMedicament: l.med.idMedicament,
        quantiteDispensee: l.quantite
      }));

    if (lignes.length === 0) return;

    const request: CreateDispensationRequest = {
      idPrescription: this.selectedOrdonnance.idOrdonnance,
      notes: this.dispensationNotes || undefined,
      lignes
    };

    this.stockService.dispenserOrdonnance(request).subscribe({
      next: () => {
        this.closeDispensationModal();
        this.loadOrdonnances();
      },
      error: (error) => {
        console.error('Erreur dispensation', error);
        alert(error.error?.message || 'Erreur lors de la dispensation');
      }
    });
  }

  // ==================== Nouveau Workflow Pharmacie ====================

  validerOrdonnance(ord: OrdonnancePharmacie): void {
    if (this.isProcessing) return;
    
    this.isProcessing = true;
    this.stockService.validerOrdonnance(ord.idOrdonnance).subscribe({
      next: (result) => {
        this.isProcessing = false;
        if (result.success) {
          alert(`Ordonnance validée avec succès!\n\nFacture: ${result.numeroFacture}\nMontant total: ${this.formatService.formatPrice(result.montantTotal)}\nPart assurance: ${this.formatService.formatPrice(result.montantAssurance)}\nPart patient: ${this.formatService.formatPrice(result.montantPatient)}\n\nLe patient peut maintenant aller payer à la caisse.`);
          this.loadOrdonnances();
        } else {
          alert(`Erreur: ${result.message}`);
        }
      },
      error: (error) => {
        this.isProcessing = false;
        console.error('Erreur validation ordonnance', error);
        alert(error.error?.message || 'Erreur lors de la validation de l\'ordonnance');
      }
    });
  }

  delivrerOrdonnance(ord: OrdonnancePharmacie): void {
    if (this.isProcessing) return;
    
    if (!ord.estPayee) {
      alert('La facture n\'est pas encore payée. Le patient doit d\'abord payer à la caisse.');
      return;
    }

    this.isProcessing = true;
    this.stockService.delivrerOrdonnance(ord.idOrdonnance).subscribe({
      next: (result) => {
        this.isProcessing = false;
        if (result.success) {
          const lignesInfo = result.lignesDelivrees
            .map(l => `- ${l.nomMedicament}: ${l.quantiteDelivree} unités (stock restant: ${l.stockRestant})`)
            .join('\n');
          alert(`Médicaments délivrés avec succès!\n\n${lignesInfo}`);
          this.loadOrdonnances();
        } else {
          const erreurs = result.erreurs?.join('\n') || result.message;
          alert(`Erreur: ${erreurs}`);
        }
      },
      error: (error) => {
        this.isProcessing = false;
        console.error('Erreur délivrance ordonnance', error);
        alert(error.error?.message || 'Erreur lors de la délivrance des médicaments');
      }
    });
  }

  getStatutBadgeClass(statut: string): string {
    switch (statut) {
      case 'dispensee': return 'badge-success';
      case 'payee': return 'badge-success';
      case 'validee': return 'badge-info';
      case 'partielle': return 'badge-warning';
      case 'active': return 'badge-primary';
      case 'annulee': return 'badge-danger';
      case 'expiree': return 'badge-danger';
      default: return 'badge-secondary';
    }
  }

  getStatutLabel(statut: string): string {
    switch (statut) {
      case 'dispensee': return 'Délivrée';
      case 'payee': return 'Payée';
      case 'validee': return 'Validée';
      case 'partielle': return 'Partielle';
      case 'active': return 'Active';
      case 'annulee': return 'Annulée';
      case 'expiree': return 'Expirée';
      default: return statut;
    }
  }

  getRestant(med: MedicamentPrescrit): number {
    return med.quantitePrescrite - med.quantiteDispensee;
  }

  getTotalDispensation(): number {
    return this.dispensationLignes
      .filter(l => l.selected && l.quantite > 0)
      .reduce((sum, l) => sum + (l.quantite * (l.med.prixUnitaire || 0)), 0);
  }

  canDispense(med: MedicamentPrescrit): boolean {
    return (med.stockDisponible || 0) > 0 && this.getRestant(med) > 0;
  }
}
