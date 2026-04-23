import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, SidebarComponent, MenuItem, ALL_ICONS_PROVIDER } from '../../../shared';
import { DashboardLayoutComponent } from '../../../shared/components/dashboard-layout/dashboard-layout.component';
import { PHARMACIEN_MENU_ITEMS, PHARMACIEN_SIDEBAR_TITLE } from '../shared';
import { 
  PharmacieStockService, 
  MouvementStock,
  MouvementStockFilter,
  CommandePharmacie,
  Dispensation
} from '../../../services/pharmacie-stock.service';
import { ExportCsvService } from '../../../shared/services/export-csv.service';
import { ExportPdfService } from '../../../shared/services/export-pdf.service';
import { FormatService } from '../../../shared/services/format.service';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';


export enum HistoriqueTab {
  MOUVEMENTS = 'mouvements',
  COMMANDES = 'commandes',
  DISPENSATIONS = 'dispensations',
  CONSOLIDE = 'consolide'
}

export interface HistoriqueConsolideItem {
  id: string;
  type: 'mouvement' | 'commande' | 'dispensation';
  date: string;
  description: string;
  details: any;
  utilisateur?: string;
  montant?: number;
  statut?: string;
}

@Component({
  selector: 'app-pharmacien-historique',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, SidebarComponent, DashboardLayoutComponent, PaginationComponent],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './historique.component.html',
  styleUrls: ['./historique.component.scss']
})
export class PharmacienHistoriqueComponent implements OnInit {
  menuItems: MenuItem[] = PHARMACIEN_MENU_ITEMS;
  sidebarTitle = PHARMACIEN_SIDEBAR_TITLE;
  Math = Math;

  // Onglet actif
  activeTab: HistoriqueTab = HistoriqueTab.MOUVEMENTS;
  readonly HistoriqueTab = HistoriqueTab;

  // Données mouvements (existant)
  mouvements: MouvementStock[] = [];
  totalItems = 0;
  currentPage = 1;
  pageSize = 20;
  totalPages = 0;

  // Données commandes
  commandes: CommandePharmacie[] = [];
  totalCommandes = 0;
  currentPageCommandes = 1;
  totalPagesCommandes = 0;

  // Données dispensations
  dispensations: Dispensation[] = [];
  totalDispensations = 0;
  currentPageDispensations = 1;
  totalPagesDispensations = 0;

  // Données consolidées
  historiqueConsolide: HistoriqueConsolideItem[] = [];
  totalConsolide = 0;
  currentPageConsolide = 1;
  totalPagesConsolide = 0;

  // Menu déroulant d'export
  activeExportDropdown: string | null = null;

  // Filtres mouvements (existant)
  filter: MouvementStockFilter = {
    page: 1,
    pageSize: 20
  };
  dateDebut = '';
  dateFin = '';
  typeMouvement = '';

  // Filtres commandes
  statutCommande = '';
  fournisseurFilter = '';
  dateDebutCommande = '';
  dateFinCommande = '';

  // Filtres consolidés
  dateDebutConsolide = '';
  dateFinConsolide = '';
  typeActivite = '';
  rechercheTexte = '';
  statutGlobal = '';

  // Filtres dispensations
  dateDebutDispensation = '';
  dateFinDispensation = '';
  patientFilter = '';
  statutDispensation = '';

  isLoading = false;

  typesMouvement = [
    { value: '', label: 'Tous les types' },
    { value: 'entree', label: 'Entrées' },
    { value: 'sortie', label: 'Sorties' },
    { value: 'ajustement', label: 'Ajustements' },
    { value: 'perte', label: 'Pertes' },
    { value: 'retour', label: 'Retours' }
  ];

  constructor(
    private stockService: PharmacieStockService,
    private exportCsvService: ExportCsvService,
    private exportPdfService: ExportPdfService,
    public formatService: FormatService
  ) {}

  ngOnInit(): void {
    this.loadMouvements();
  }

  // Gestion des onglets
  switchTab(tab: HistoriqueTab): void {
    this.activeTab = tab;
    switch (tab) {
      case HistoriqueTab.MOUVEMENTS:
        this.loadMouvements();
        break;
      case HistoriqueTab.COMMANDES:
        this.loadCommandes();
        break;
      case HistoriqueTab.DISPENSATIONS:
        this.loadDispensations();
        break;
      case HistoriqueTab.CONSOLIDE:
        this.loadConsolide();
        break;
    }
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

  onPageChangeMouvements(page: number): void {
    this.currentPage = page;
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

  // Méthodes pour les commandes
  loadCommandes(): void {
    this.isLoading = true;
    this.stockService.getCommandes(
      this.statutCommande || undefined,
      this.currentPageCommandes,
      this.pageSize
    ).subscribe({
      next: (result) => {
        this.commandes = result.items;
        this.totalCommandes = result.totalItems;
        this.totalPagesCommandes = result.totalPages;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Erreur chargement commandes', error);
        this.isLoading = false;
      }
    });
  }

  onCommandesFilterChange(): void {
    this.currentPageCommandes = 1;
    this.loadCommandes();
  }

  onPageChangeCommandes(page: number): void {
    this.currentPageCommandes = page;
    this.loadCommandes();
  }

  // Méthodes pour les dispensations
  loadDispensations(): void {
    this.isLoading = true;
    const dateDebut = this.dateDebutDispensation || undefined;
    const dateFin = this.dateFinDispensation || undefined;
    
    this.stockService.getDispensations(dateDebut, dateFin, this.currentPageDispensations, this.pageSize).subscribe({
      next: (result) => {
        this.dispensations = result.items;
        this.totalDispensations = result.totalItems;
        this.totalPagesDispensations = result.totalPages;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Erreur chargement dispensations', error);
        this.isLoading = false;
      }
    });
  }

  onDispensationsFilterChange(): void {
    this.currentPageDispensations = 1;
    this.loadDispensations();
  }

  onPageChangeDispensations(page: number): void {
    this.currentPageDispensations = page;
    this.loadDispensations();
  }

  // Utilitaires pour commandes
  getStatutCommandeIcon(statut: string): string {
    switch (statut) {
      case 'brouillon': return 'edit';
      case 'envoyee': return 'send';
      case 'partiellement_recue': return 'package';
      case 'recue': return 'check-circle';
      case 'annulee': return 'x-circle';
      default: return 'file-text';
    }
  }

  getStatutCommandeClass(statut: string): string {
    switch (statut) {
      case 'brouillon': return 'statut-brouillon';
      case 'envoyee': return 'statut-envoyee';
      case 'partiellement_recue': return 'statut-partielle';
      case 'recue': return 'statut-recue';
      case 'annulee': return 'statut-annulee';
      default: return '';
    }
  }

  getStatutCommandeLabel(statut: string): string {
    switch (statut) {
      case 'brouillon': return 'Brouillon';
      case 'envoyee': return 'Envoyée';
      case 'partiellement_recue': return 'Partiellement reçue';
      case 'recue': return 'Reçue';
      case 'annulee': return 'Annulée';
      default: return statut;
    }
  }

  // Utilitaires pour dispensations
  getStatutDispensationIcon(statut: string): string {
    switch (statut) {
      case 'en_attente': return 'clock';
      case 'en_cours': return 'play-circle';
      case 'complete': return 'check-circle';
      case 'annulee': return 'x-circle';
      default: return 'file-text';
    }
  }

  getStatutDispensationClass(statut: string): string {
    switch (statut) {
      case 'en_attente': return 'statut-attente';
      case 'en_cours': return 'statut-cours';
      case 'complete': return 'statut-complete';
      case 'annulee': return 'statut-annulee';
      default: return '';
    }
  }

  getStatutDispensationLabel(statut: string): string {
    switch (statut) {
      case 'en_attente': return 'En attente';
      case 'en_cours': return 'En cours';
      case 'complete': return 'Complète';
      case 'annulee': return 'Annulée';
      default: return statut;
    }
  }

  // Méthodes pour la vue consolidée
  loadConsolide(): void {
    this.isLoading = true;
    
    // Charger toutes les données en parallèle
    const mouvements$ = this.stockService.getMouvements({
      page: 1,
      pageSize: 1000,
      dateDebut: this.dateDebutConsolide || undefined,
      dateFin: this.dateFinConsolide || undefined
    });

    const commandes$ = this.stockService.getCommandes(
      undefined,
      1,
      1000
    );

    const dispensations$ = this.stockService.getDispensations(
      this.dateDebutConsolide || undefined,
      this.dateFinConsolide || undefined,
      1,
      1000
    );

    // Combiner et transformer les données
    Promise.all([
      this.toPromise(mouvements$),
      this.toPromise(commandes$),
      this.toPromise(dispensations$)
    ]).then((results: any[]) => {
      const [mouvementsResult, commandesResult, dispensationsResult] = results;
      const consolide: HistoriqueConsolideItem[] = [];
      
      // Ajouter les mouvements
      mouvementsResult.items.forEach((mvt: any) => {
        consolide.push({
          id: `mvt-${mvt.idMouvement}`,
          type: 'mouvement',
          date: mvt.dateMouvement,
          description: `${this.getTypeLabel(mvt.typeMouvement)} - ${mvt.nomMedicament}`,
          details: mvt,
          utilisateur: mvt.nomUtilisateur,
          montant: undefined,
          statut: mvt.typeMouvement
        });
      });

      // Ajouter les commandes
      commandesResult.items.forEach((cmd: any) => {
        consolide.push({
          id: `cmd-${cmd.idCommande}`,
          type: 'commande',
          date: cmd.dateCommande,
          description: `Commande #${cmd.idCommande} - ${cmd.nomFournisseur}`,
          details: cmd,
          utilisateur: cmd.nomUtilisateur,
          montant: cmd.montantTotal,
          statut: cmd.statut
        });
      });

      // Ajouter les dispensations
      dispensationsResult.items.forEach((disp: any) => {
        consolide.push({
          id: `disp-${disp.idDispensation}`,
          type: 'dispensation',
          date: disp.dateDispensation,
          description: `Dispensation - ${disp.nomPatient}`,
          details: disp,
          utilisateur: disp.nomPharmacien,
          montant: disp.montantTotal,
          statut: disp.statut
        });
      });

      // Trier par date (plus récent en premier)
      this.historiqueConsolide = consolide.sort((a, b) => 
        new Date(b.date).getTime() - new Date(a.date).getTime()
      );

      // Appliquer les filtres
      this.applyConsolideFilters();
      
      this.totalConsolide = this.historiqueConsolide.length;
      this.isLoading = false;
    }).catch((error: any) => {
      console.error('Erreur chargement consolidé', error);
      this.isLoading = false;
    });
  }

  private toPromise<T>(observable: any): Promise<T> {
    return new Promise((resolve, reject) => {
      observable.subscribe({
        next: resolve,
        error: reject
      });
    });
  }

  applyConsolideFilters(): void {
    let filtered = [...this.historiqueConsolide];

    // Filtrer par type d'activité
    if (this.typeActivite) {
      filtered = filtered.filter(item => item.type === this.typeActivite);
    }

    // Filtrer par recherche texte
    if (this.rechercheTexte) {
      const search = this.rechercheTexte.toLowerCase();
      filtered = filtered.filter(item => 
        item.description.toLowerCase().includes(search) ||
        item.details?.nomMedicament?.toLowerCase().includes(search) ||
        item.details?.nomPatient?.toLowerCase().includes(search) ||
        item.details?.nomFournisseur?.toLowerCase().includes(search)
      );
    }

    // Filtrer par statut
    if (this.statutGlobal) {
      filtered = filtered.filter(item => item.statut === this.statutGlobal);
    }

    this.historiqueConsolide = filtered;
    this.totalConsolide = filtered.length;
  }

  onConsolideFilterChange(): void {
    this.applyConsolideFilters();
  }

  onConsolideDateChange(): void {
    this.loadConsolide();
  }

  clearConsolideFilters(): void {
    this.dateDebutConsolide = '';
    this.dateFinConsolide = '';
    this.typeActivite = '';
    this.rechercheTexte = '';
    this.statutGlobal = '';
    this.loadConsolide();
  }

  // Utilitaires pour la vue consolidée
  getConsolideIcon(type: string): string {
    switch (type) {
      case 'mouvement': return 'arrow-up-down';
      case 'commande': return 'file-text';
      case 'dispensation': return 'pill';
      default: return 'circle';
    }
  }

  getConsolideClass(type: string): string {
    switch (type) {
      case 'mouvement': return 'type-mouvement';
      case 'commande': return 'type-commande';
      case 'dispensation': return 'type-dispensation';
      default: return '';
    }
  }

  getConsolideLabel(type: string): string {
    switch (type) {
      case 'mouvement': return 'Mouvement';
      case 'commande': return 'Commande';
      case 'dispensation': return 'Dispensation';
      default: return type;
    }
  }

  // Méthodes pour les badges de statut consolidés
  getStatutBadgeClass(type: string, statut: string): string {
    if (type === 'mouvement') {
      return this.getTypeClass(statut);
    } else if (type === 'commande') {
      return this.getStatutCommandeClass(statut);
    } else if (type === 'dispensation') {
      return this.getStatutDispensationClass(statut);
    }
    return '';
  }

  getStatutBadgeIcon(type: string, statut: string): string {
    if (type === 'mouvement') {
      return this.getTypeIcon(statut);
    } else if (type === 'commande') {
      return this.getStatutCommandeIcon(statut);
    } else if (type === 'dispensation') {
      return this.getStatutDispensationIcon(statut);
    }
    return 'circle';
  }

  getStatutBadgeLabel(type: string, statut: string): string {
    if (type === 'mouvement') {
      return this.getTypeLabel(statut);
    } else if (type === 'commande') {
      return this.getStatutCommandeLabel(statut);
    } else if (type === 'dispensation') {
      return this.getStatutDispensationLabel(statut);
    }
    return statut;
  }

  // Export CSV
  exportMouvements(): void {
    const data: any[] = Array.from(this.mouvements).map((m: any) => ({
      id: m.id,
      dateMouvement: m.dateMouvement,
      typeMouvement: m.typeMouvement,
      quantite: m.quantite,
      motif: m.motif,
      medicamentNom: m.medicament?.nom,
      utilisateurNom: m.utilisateur?.nom
    }));
    this.exportCsvService.exportHistoriqueMouvements(data);
  }

  exportCommandes(): void {
    const data: any[] = Array.from(this.commandes).map((c: any) => ({
      id: c.id,
      dateCommande: c.dateCommande,
      statut: c.statut,
      fournisseurNom: c.fournisseur?.nom,
      medicamentsCount: c.medicaments?.length || 0,
      utilisateurNom: c.utilisateur?.nom
    }));
    this.exportCsvService.exportHistoriqueCommandes(data);
  }

  exportDispensations(): void {
    const data: any[] = Array.from(this.dispensations).map((d: any) => ({
      id: d.id,
      dateDispensation: d.dateDispensation,
      ordonnanceId: d.ordonnanceId,
      medicamentsCount: d.medicaments?.length || 0,
      patientNom: d.patient?.nom,
      utilisateurNom: d.utilisateur?.nom
    }));
    this.exportCsvService.exportHistoriqueDispensations(data);
  }

  exportConsolide(): void {
    const data: any[] = Array.from(this.historiqueConsolide).map((item: any) => ({
      id: item.id,
      date: item.date,
      type: item.type,
      sousType: item.details?.typeMouvement || item.details?.statut || '',
      description: item.description,
      statut: item.statut,
      utilisateurNom: item.utilisateur,
      details: JSON.stringify(item.details)
    }));
    this.exportCsvService.exportHistoriqueConsolide(data);
  }

  // Gestion du menu déroulant d'export
  toggleExportDropdown(type: string): void {
    this.activeExportDropdown = this.activeExportDropdown === type ? null : type;
  }

  // Méthodes d'export PDF
  exportMouvementsPdf(): void {
    const data: any[] = Array.from(this.mouvements).map((m: any) => ({
      id: m.id,
      dateMouvement: m.dateMouvement,
      typeMouvement: m.typeMouvement,
      quantite: m.quantite,
      motif: m.motif,
      medicamentNom: m.medicament?.nom,
      utilisateurNom: m.utilisateur?.nom
    }));
    this.exportPdfService.exportHistoriqueMouvements(data);
    this.activeExportDropdown = null;
  }

  exportCommandesPdf(): void {
    const data: any[] = Array.from(this.commandes).map((c: any) => ({
      id: c.id,
      dateCommande: c.dateCommande,
      statut: c.statut,
      fournisseurNom: c.fournisseur?.nom,
      medicamentsCount: c.medicaments?.length || 0,
      utilisateurNom: c.utilisateur?.nom
    }));
    this.exportPdfService.exportHistoriqueCommandes(data);
    this.activeExportDropdown = null;
  }

  exportDispensationsPdf(): void {
    const data: any[] = Array.from(this.dispensations).map((d: any) => ({
      id: d.id,
      dateDispensation: d.dateDispensation,
      ordonnanceId: d.ordonnanceId,
      medicamentsCount: d.medicaments?.length || 0,
      patientNom: d.patient?.nom,
      utilisateurNom: d.utilisateur?.nom
    }));
    this.exportPdfService.exportHistoriqueDispensations(data);
    this.activeExportDropdown = null;
  }

  exportConsolidePdf(): void {
    const data: any[] = Array.from(this.historiqueConsolide).map((item: any) => ({
      id: item.id,
      date: item.date,
      type: item.type,
      sousType: item.details?.typeMouvement || item.details?.statut || '',
      description: item.description,
      statut: item.statut,
      utilisateurNom: item.utilisateur
    }));
    this.exportPdfService.exportHistoriqueConsolide(data);
    this.activeExportDropdown = null;
  }
}
