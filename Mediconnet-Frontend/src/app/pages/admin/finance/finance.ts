import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FactureAssuranceService, FactureAssurance, FactureAssuranceStats, FactureAssuranceFilter, STATUTS_FACTURE, TYPES_FACTURE } from '../../../services/facture-assurance.service';
import { AssuranceService } from '../../../services/assurance.service';
import { DashboardLayoutComponent } from '../../../shared';
import { ADMIN_MENU_ITEMS, ADMIN_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-finance',
  standalone: true,
  imports: [CommonModule, FormsModule, DashboardLayoutComponent],
  templateUrl: './finance.html',
  styleUrl: './finance.css',
})
export class Finance implements OnInit {
  menuItems = ADMIN_MENU_ITEMS;
  sidebarTitle = ADMIN_SIDEBAR_TITLE;

  activeTab: 'general' | 'factures' = 'general';
  
  // Stats
  stats: FactureAssuranceStats | null = null;
  loadingStats = false;
  
  // Factures
  factures: FactureAssurance[] = [];
  loadingFactures = false;
  
  // Filtres
  filter: FactureAssuranceFilter = {};
  assurances: any[] = [];
  
  // Sélection
  selectedFactures: Set<number> = new Set();
  selectAll = false;
  
  // Modal statut
  showStatutModal = false;
  selectedFacture: FactureAssurance | null = null;
  newStatut = '';
  statutNotes = '';
  updatingStatut = false;
  
  // Constantes
  statuts = STATUTS_FACTURE;
  types = TYPES_FACTURE;

  constructor(
    private factureService: FactureAssuranceService,
    private assuranceService: AssuranceService
  ) {}

  ngOnInit(): void {
    this.loadStats();
    this.loadFactures();
    this.loadAssurances();
  }

  loadStats(): void {
    this.loadingStats = true;
    this.factureService.getStatistiques().subscribe({
      next: (res) => {
        this.stats = res.data;
        this.loadingStats = false;
      },
      error: () => this.loadingStats = false
    });
  }

  loadFactures(): void {
    this.loadingFactures = true;
    this.factureService.getFactures(this.filter).subscribe({
      next: (res) => {
        this.factures = res.data;
        this.loadingFactures = false;
        this.selectedFactures.clear();
        this.selectAll = false;
      },
      error: () => this.loadingFactures = false
    });
  }

  loadAssurances(): void {
    this.assuranceService.getAssurances().subscribe({
      next: (res: any) => this.assurances = res.data || res
    });
  }

  applyFilter(): void {
    this.loadFactures();
  }

  resetFilter(): void {
    this.filter = {};
    this.loadFactures();
  }

  toggleSelectAll(): void {
    if (this.selectAll) {
      this.factures.forEach(f => this.selectedFactures.add(f.idFacture));
    } else {
      this.selectedFactures.clear();
    }
  }

  toggleSelect(id: number): void {
    if (this.selectedFactures.has(id)) {
      this.selectedFactures.delete(id);
    } else {
      this.selectedFactures.add(id);
    }
    this.selectAll = this.selectedFactures.size === this.factures.length;
  }

  isSelected(id: number): boolean {
    return this.selectedFactures.has(id);
  }

  envoyerFacture(facture: FactureAssurance): void {
    if (!confirm(`Envoyer la facture ${facture.numeroFacture} à ${facture.assuranceEmail}?`)) return;
    
    this.factureService.envoyerFacture(facture.idFacture).subscribe({
      next: (res) => {
        alert(res.message);
        this.loadFactures();
        this.loadStats();
      },
      error: (err) => alert(err.error?.message || 'Erreur lors de l\'envoi')
    });
  }

  envoyerSelection(): void {
    if (this.selectedFactures.size === 0) return;
    if (!confirm(`Envoyer ${this.selectedFactures.size} facture(s) aux assurances?`)) return;
    
    this.factureService.envoyerLot(Array.from(this.selectedFactures)).subscribe({
      next: (res) => {
        alert(res.message);
        this.loadFactures();
        this.loadStats();
      },
      error: (err) => alert(err.error?.message || 'Erreur lors de l\'envoi')
    });
  }

  telechargerPdf(facture: FactureAssurance): void {
    this.factureService.telechargerPdf(facture.idFacture).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Facture_${facture.numeroFacture}.pdf`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => alert('Erreur lors du téléchargement')
    });
  }

  openStatutModal(facture: FactureAssurance): void {
    this.selectedFacture = facture;
    this.newStatut = facture.statut;
    this.statutNotes = '';
    this.showStatutModal = true;
  }

  closeStatutModal(): void {
    this.showStatutModal = false;
    this.selectedFacture = null;
  }

  updateStatut(): void {
    if (!this.selectedFacture || !this.newStatut) return;
    
    this.updatingStatut = true;
    this.factureService.updateStatut(this.selectedFacture.idFacture, {
      statut: this.newStatut,
      notes: this.statutNotes || undefined
    }).subscribe({
      next: () => {
        this.closeStatutModal();
        this.loadFactures();
        this.loadStats();
        this.updatingStatut = false;
      },
      error: (err) => {
        alert(err.error?.message || 'Erreur lors de la mise à jour');
        this.updatingStatut = false;
      }
    });
  }

  getStatutLabel(statut: string): string {
    return this.factureService.getStatutLabel(statut);
  }

  getStatutColor(statut: string): string {
    return this.factureService.getStatutColor(statut);
  }

  getTypeLabel(type: string): string {
    return this.factureService.getTypeLabel(type);
  }

  formatMontant(montant: number): string {
    return new Intl.NumberFormat('fr-FR').format(montant) + ' FCFA';
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('fr-FR');
  }
}
