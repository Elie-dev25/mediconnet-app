import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, SidebarComponent, MenuItem, ALL_ICONS_PROVIDER } from '../../../shared';
import { PHARMACIEN_MENU_ITEMS, PHARMACIEN_SIDEBAR_TITLE } from '../shared';
import { 
  PharmacieStockService, 
  MedicamentStock, 
  CreateMedicamentRequest,
  UpdateMedicamentRequest,
  AjustementStockRequest,
  PagedResult 
} from '../../../services/pharmacie-stock.service';

@Component({
  selector: 'app-pharmacien-stock',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, SidebarComponent],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './stock.component.html',
  styleUrls: ['./stock.component.scss']
})
export class PharmacienStockComponent implements OnInit {
  menuItems: MenuItem[] = PHARMACIEN_MENU_ITEMS;
  sidebarTitle = PHARMACIEN_SIDEBAR_TITLE;

  medicaments: MedicamentStock[] = [];
  totalItems = 0;
  currentPage = 1;
  pageSize = 15;
  totalPages = 0;

  searchTerm = '';
  selectedStatut = '';
  isLoading = false;

  // Modal states
  showCreateModal = false;
  showEditModal = false;
  showAjustementModal = false;
  showDeleteModal = false;

  selectedMedicament: MedicamentStock | null = null;

  // Form data
  medicamentForm: CreateMedicamentRequest = {
    nom: '',
    dosage: '',
    formeGalenique: '',
    laboratoire: '',
    stock: 0,
    seuilStock: 10,
    prix: 0,
    emplacementRayon: '',
    codeATC: '',
    conditionnement: '',
    temperatureConservation: ''
  };

  ajustementForm: AjustementStockRequest = {
    idMedicament: 0,
    quantite: 0,
    typeMouvement: 'entree',
    motif: ''
  };

  formesGaleniques = [
    'comprime', 'sirop', 'injectable', 'pommade', 'gel', 'collyre', 
    'suppositoire', 'capsule', 'sachet', 'solution', 'suspension'
  ];

  constructor(private stockService: PharmacieStockService) {}

  ngOnInit(): void {
    this.loadMedicaments();
  }

  loadMedicaments(): void {
    this.isLoading = true;
    this.stockService.getMedicaments(
      this.searchTerm || undefined, 
      this.selectedStatut || undefined,
      this.currentPage,
      this.pageSize
    ).subscribe({
      next: (result) => {
        this.medicaments = result.items;
        this.totalItems = result.totalItems;
        this.totalPages = result.totalPages;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Erreur chargement médicaments', error);
        this.isLoading = false;
      }
    });
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadMedicaments();
  }

  onStatutChange(): void {
    this.currentPage = 1;
    this.loadMedicaments();
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadMedicaments();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadMedicaments();
    }
  }

  // Create modal
  openCreateModal(): void {
    this.resetForm();
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
    this.resetForm();
  }

  createMedicament(): void {
    if (!this.medicamentForm.nom) return;

    this.stockService.createMedicament(this.medicamentForm).subscribe({
      next: () => {
        this.closeCreateModal();
        this.loadMedicaments();
      },
      error: (error) => console.error('Erreur création', error)
    });
  }

  // Edit modal
  openEditModal(med: MedicamentStock): void {
    this.selectedMedicament = med;
    this.medicamentForm = {
      nom: med.nom,
      dosage: med.dosage || '',
      formeGalenique: med.formeGalenique || '',
      laboratoire: med.laboratoire || '',
      stock: med.stock || 0,
      seuilStock: med.seuilStock || 10,
      prix: med.prix || 0,
      datePeremption: med.datePeremption?.split('T')[0],
      emplacementRayon: med.emplacementRayon || '',
      codeATC: med.codeATC || '',
      conditionnement: med.conditionnement || '',
      temperatureConservation: med.temperatureConservation || ''
    };
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.selectedMedicament = null;
    this.resetForm();
  }

  updateMedicament(): void {
    if (!this.selectedMedicament) return;

    const request: UpdateMedicamentRequest = {
      nom: this.medicamentForm.nom,
      dosage: this.medicamentForm.dosage,
      formeGalenique: this.medicamentForm.formeGalenique,
      laboratoire: this.medicamentForm.laboratoire,
      seuilStock: this.medicamentForm.seuilStock,
      prix: this.medicamentForm.prix,
      datePeremption: this.medicamentForm.datePeremption,
      emplacementRayon: this.medicamentForm.emplacementRayon,
      codeATC: this.medicamentForm.codeATC,
      conditionnement: this.medicamentForm.conditionnement,
      temperatureConservation: this.medicamentForm.temperatureConservation
    };

    this.stockService.updateMedicament(this.selectedMedicament.idMedicament, request).subscribe({
      next: () => {
        this.closeEditModal();
        this.loadMedicaments();
      },
      error: (error) => console.error('Erreur mise à jour', error)
    });
  }

  // Ajustement stock modal
  openAjustementModal(med: MedicamentStock): void {
    this.selectedMedicament = med;
    this.ajustementForm = {
      idMedicament: med.idMedicament,
      quantite: 0,
      typeMouvement: 'entree',
      motif: ''
    };
    this.showAjustementModal = true;
  }

  closeAjustementModal(): void {
    this.showAjustementModal = false;
    this.selectedMedicament = null;
  }

  ajusterStock(): void {
    if (!this.ajustementForm.quantite) return;

    this.stockService.ajusterStock(this.ajustementForm).subscribe({
      next: () => {
        this.closeAjustementModal();
        this.loadMedicaments();
      },
      error: (error) => console.error('Erreur ajustement', error)
    });
  }

  // Delete modal
  openDeleteModal(med: MedicamentStock): void {
    this.selectedMedicament = med;
    this.showDeleteModal = true;
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
    this.selectedMedicament = null;
  }

  deleteMedicament(): void {
    if (!this.selectedMedicament) return;

    this.stockService.deleteMedicament(this.selectedMedicament.idMedicament).subscribe({
      next: () => {
        this.closeDeleteModal();
        this.loadMedicaments();
      },
      error: (error) => console.error('Erreur suppression', error)
    });
  }

  private resetForm(): void {
    this.medicamentForm = {
      nom: '',
      dosage: '',
      formeGalenique: '',
      laboratoire: '',
      stock: 0,
      seuilStock: 10,
      prix: 0,
      emplacementRayon: '',
      codeATC: '',
      conditionnement: '',
      temperatureConservation: ''
    };
  }

  getStatutBadgeClass(statut: string): string {
    switch (statut) {
      case 'rupture': return 'badge-danger';
      case 'alerte': return 'badge-warning';
      default: return 'badge-success';
    }
  }

  getStatutLabel(statut: string): string {
    switch (statut) {
      case 'rupture': return 'Rupture';
      case 'alerte': return 'Stock bas';
      default: return 'Normal';
    }
  }

  formatDate(date?: string): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('fr-FR');
  }

  formatPrice(price?: number): string {
    if (price === undefined || price === null) return '-';
    return price.toLocaleString('fr-FR') + ' FCFA';
  }
}
