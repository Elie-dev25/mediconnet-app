import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, SidebarComponent, MenuItem, ALL_ICONS_PROVIDER } from '../../../shared';
import { PHARMACIEN_MENU_ITEMS, PHARMACIEN_SIDEBAR_TITLE } from '../shared';
import { 
  PharmacieStockService, 
  OrdonnancePharmacie,
  MedicamentPrescrit,
  CreateDispensationRequest,
  DispensationLigneRequest,
  PagedResult 
} from '../../../services/pharmacie-stock.service';

@Component({
  selector: 'app-pharmacien-ordonnances',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, SidebarComponent],
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

  showDispensationModal = false;
  selectedOrdonnance: OrdonnancePharmacie | null = null;
  dispensationLignes: { med: MedicamentPrescrit; quantite: number; selected: boolean }[] = [];
  dispensationNotes = '';

  constructor(private stockService: PharmacieStockService) {}

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

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadOrdonnances();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadOrdonnances();
    }
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

  getStatutBadgeClass(statut: string): string {
    switch (statut) {
      case 'complete': return 'badge-success';
      case 'partielle': return 'badge-warning';
      default: return 'badge-info';
    }
  }

  getStatutLabel(statut: string): string {
    switch (statut) {
      case 'complete': return 'Complète';
      case 'partielle': return 'Partielle';
      default: return 'En attente';
    }
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  formatPrice(price?: number): string {
    if (price === undefined || price === null) return '-';
    return price.toLocaleString('fr-FR') + ' FCFA';
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
