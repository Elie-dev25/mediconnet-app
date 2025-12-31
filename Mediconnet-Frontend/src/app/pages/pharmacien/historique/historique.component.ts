import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, SidebarComponent, MenuItem, ALL_ICONS_PROVIDER } from '../../../shared';
import { PHARMACIEN_MENU_ITEMS, PHARMACIEN_SIDEBAR_TITLE } from '../shared';
import { 
  PharmacieStockService, 
  MouvementStock,
  MouvementStockFilter,
  PagedResult 
} from '../../../services/pharmacie-stock.service';

@Component({
  selector: 'app-pharmacien-historique',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, SidebarComponent],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './historique.component.html',
  styleUrls: ['./historique.component.scss']
})
export class PharmacienHistoriqueComponent implements OnInit {
  menuItems: MenuItem[] = PHARMACIEN_MENU_ITEMS;
  sidebarTitle = PHARMACIEN_SIDEBAR_TITLE;

  mouvements: MouvementStock[] = [];
  totalItems = 0;
  currentPage = 1;
  pageSize = 20;
  totalPages = 0;

  filter: MouvementStockFilter = {
    page: 1,
    pageSize: 20
  };

  dateDebut = '';
  dateFin = '';
  typeMouvement = '';

  isLoading = false;

  typesMouvement = [
    { value: '', label: 'Tous les types' },
    { value: 'entree', label: 'Entrées' },
    { value: 'sortie', label: 'Sorties' },
    { value: 'ajustement', label: 'Ajustements' },
    { value: 'perte', label: 'Pertes' },
    { value: 'retour', label: 'Retours' }
  ];

  constructor(private stockService: PharmacieStockService) {}

  ngOnInit(): void {
    this.loadMouvements();
  }

  loadMouvements(): void {
    this.isLoading = true;
    
    const filter: MouvementStockFilter = {
      typeMouvement: this.typeMouvement || undefined,
      dateDebut: this.dateDebut || undefined,
      dateFin: this.dateFin || undefined,
      page: this.currentPage,
      pageSize: this.pageSize
    };

    this.stockService.getMouvements(filter).subscribe({
      next: (result) => {
        this.mouvements = result.items;
        this.totalItems = result.totalItems;
        this.totalPages = result.totalPages;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Erreur chargement mouvements', error);
        this.isLoading = false;
      }
    });
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadMouvements();
  }

  clearFilters(): void {
    this.dateDebut = '';
    this.dateFin = '';
    this.typeMouvement = '';
    this.currentPage = 1;
    this.loadMouvements();
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadMouvements();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadMouvements();
    }
  }

  getTypeIcon(type: string): string {
    switch (type) {
      case 'entree': return 'arrow-down-circle';
      case 'sortie': return 'arrow-up-circle';
      case 'ajustement': return 'settings-2';
      case 'perte': return 'alert-triangle';
      case 'retour': return 'rotate-ccw';
      default: return 'circle';
    }
  }

  getTypeClass(type: string): string {
    switch (type) {
      case 'entree': return 'type-entree';
      case 'sortie': return 'type-sortie';
      case 'ajustement': return 'type-ajustement';
      case 'perte': return 'type-perte';
      case 'retour': return 'type-retour';
      default: return '';
    }
  }

  getTypeLabel(type: string): string {
    switch (type) {
      case 'entree': return 'Entrée';
      case 'sortie': return 'Sortie';
      case 'ajustement': return 'Ajustement';
      case 'perte': return 'Perte';
      case 'retour': return 'Retour';
      default: return type;
    }
  }

  getQuantitySign(type: string): string {
    return type === 'entree' || type === 'retour' ? '+' : '-';
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
