import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { AuthService } from '../../../services/auth.service';
import { CaisseService, CaisseKpi, FactureListItem, Transaction, PatientSearchResult, SessionCaisse, RepartitionPaiement, FactureRetard } from '../../../services/caisse.service';
import { SignalRService } from '../../../services/signalr.service';
import { DashboardLayoutComponent, ModalComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { CAISSIER_MENU_ITEMS, CAISSIER_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-caissier-dashboard',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    FormsModule,
    ReactiveFormsModule,
    LucideAngularModule, 
    DashboardLayoutComponent,
    ModalComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class CaissierDashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // Menu partagé pour toutes les pages caissier
  menuItems = CAISSIER_MENU_ITEMS;
  sidebarTitle = CAISSIER_SIDEBAR_TITLE;

  userName = '';
  userRole = 'caissier';

  // KPIs
  kpis: CaisseKpi | null = null;
  isLoadingKpis = true;

  // Session Caisse
  sessionActive: SessionCaisse | null = null;

  // Transactions du jour
  transactionsJour: Transaction[] = [];
  isLoadingTransactions = true;

  // Factures en attente
  facturesEnAttente: FactureListItem[] = [];
  isLoadingFactures = true;

  // Recherche patient
  patientSearch = '';
  patientResults: PatientSearchResult[] = [];
  selectedPatient: PatientSearchResult | null = null;
  patientFactures: FactureListItem[] = [];
  isSearchingPatient = false;
  private searchSubject = new Subject<string>();

  // Formulaire paiement
  paiementForm!: FormGroup;
  selectedFactures: number[] = [];
  isSubmitting = false;
  paiementError = '';
  paiementSuccess = '';

  // Statistiques
  repartitionPaiements: RepartitionPaiement[] = [];
  facturesRetard: FactureRetard[] = [];

  // Modals
  showOuvrirCaisseModal = false;
  showFermerCaisseModal = false;
  ouvrirCaisseForm!: FormGroup;
  fermerCaisseForm!: FormGroup;
  caisseError = '';

  // Mode paiements
  modesPaiement = [
    { value: 'especes', label: 'Espèces', icon: 'banknote' },
    { value: 'carte', label: 'Carte bancaire', icon: 'credit-card' },
    { value: 'mobile', label: 'Mobile Money', icon: 'smartphone' },
    { value: 'virement', label: 'Virement', icon: 'arrow-up-right' },
    { value: 'cheque', label: 'Chèque', icon: 'file-text' },
    { value: 'assurance', label: 'Assurance', icon: 'shield' }
  ];

  constructor(
    private authService: AuthService,
    private caisseService: CaisseService,
    private signalRService: SignalRService,
    private fb: FormBuilder
  ) {
    this.initForms();
  }

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.userName = `${user.prenom || ''} ${user.nom || ''}`.trim();
    }

    this.loadData();
    this.setupSearchDebounce();
    this.subscribeRealtime();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private subscribeRealtime(): void {
    this.signalRService.factureEvents$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        // Rafraîchir uniquement ce qui dépend des factures
        this.loadKpis();
        this.loadTransactionsJour();
        this.loadFacturesEnAttente();
      });
  }

  private initForms(): void {
    this.paiementForm = this.fb.group({
      modePaiement: ['especes', Validators.required],
      montant: [0, [Validators.required, Validators.min(1)]],
      montantRecu: [null],
      reference: [''],
      notes: ['']
    });

    this.ouvrirCaisseForm = this.fb.group({
      montantOuverture: [0, [Validators.required, Validators.min(0)]],
      notes: ['']
    });

    this.fermerCaisseForm = this.fb.group({
      montantFermeture: [0, [Validators.required, Validators.min(0)]],
      notes: ['']
    });
  }

  private setupSearchDebounce(): void {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(query => {
      if (query.length >= 2) {
        this.rechercherPatients(query);
      } else {
        this.patientResults = [];
      }
    });
  }

  loadData(): void {
    this.loadKpis();
    this.loadSessionActive();
    this.loadTransactionsJour();
    this.loadFacturesEnAttente();
    this.loadStatistiques();
  }

  loadKpis(): void {
    this.isLoadingKpis = true;
    this.caisseService.getKpis().subscribe({
      next: (kpis) => {
        this.kpis = kpis;
        this.isLoadingKpis = false;
      },
      error: (err) => {
        console.error('Erreur chargement KPIs:', err);
        this.isLoadingKpis = false;
      }
    });
  }

  loadSessionActive(): void {
    this.caisseService.getSessionActive().subscribe({
      next: (session) => {
        this.sessionActive = session;
      },
      error: (err) => console.error('Erreur chargement session:', err)
    });
  }

  loadTransactionsJour(): void {
    this.isLoadingTransactions = true;
    this.caisseService.getTransactionsJour().subscribe({
      next: (transactions) => {
        this.transactionsJour = transactions;
        this.isLoadingTransactions = false;
      },
      error: (err) => {
        console.error('Erreur chargement transactions:', err);
        this.isLoadingTransactions = false;
      }
    });
  }

  loadFacturesEnAttente(): void {
    this.isLoadingFactures = true;
    this.caisseService.getFacturesEnAttente().subscribe({
      next: (factures) => {
        this.facturesEnAttente = factures;
        this.isLoadingFactures = false;
      },
      error: (err) => {
        console.error('Erreur chargement factures:', err);
        this.isLoadingFactures = false;
      }
    });
  }

  loadStatistiques(): void {
    const today = new Date().toISOString().split('T')[0];
    
    this.caisseService.getRepartitionPaiements(today, today).subscribe({
      next: (data) => this.repartitionPaiements = data,
      error: (err) => console.error('Erreur stats repartition:', err)
    });

    this.caisseService.getFacturesEnRetard(5).subscribe({
      next: (data) => this.facturesRetard = data,
      error: (err) => console.error('Erreur factures retard:', err)
    });
  }

  // ==================== RECHERCHE PATIENT ====================

  onPatientSearchChange(): void {
    this.searchSubject.next(this.patientSearch);
  }

  rechercherPatients(query: string): void {
    this.isSearchingPatient = true;
    this.caisseService.rechercherPatients(query).subscribe({
      next: (results) => {
        this.patientResults = results;
        this.isSearchingPatient = false;
      },
      error: (err) => {
        console.error('Erreur recherche patient:', err);
        this.isSearchingPatient = false;
      }
    });
  }

  selectPatient(patient: PatientSearchResult): void {
    this.selectedPatient = patient;
    this.patientSearch = `${patient.prenom} ${patient.nom}`;
    this.patientResults = [];
    this.selectedFactures = [];
    
    // Charger les factures du patient
    this.caisseService.getFacturesPatient(patient.idPatient).subscribe({
      next: (factures) => {
        this.patientFactures = factures;
        this.updateMontantTotal();
      },
      error: (err) => console.error('Erreur chargement factures patient:', err)
    });
  }

  clearPatient(): void {
    this.selectedPatient = null;
    this.patientSearch = '';
    this.patientResults = [];
    this.patientFactures = [];
    this.selectedFactures = [];
    this.paiementForm.patchValue({ montant: 0 });
  }

  /**
   * Sélectionne directement une facture depuis la liste des factures en attente
   */
  selectFactureDirecte(facture: FactureListItem): void {
    // Créer un patient virtuel à partir des infos de la facture
    const nameParts = (facture.patientNom || 'Inconnu').split(' ');
    const prenom = nameParts[0] || '';
    const nom = nameParts.slice(1).join(' ') || nameParts[0] || '';
    
    this.selectedPatient = {
      idPatient: 0, // On n'a pas l'ID du patient ici
      nom: nom,
      prenom: prenom,
      numeroDossier: facture.numeroDossier || undefined,
      facturesEnAttente: 1
    };
    
    this.patientSearch = facture.patientNom || 'Patient';
    this.patientResults = [];
    
    // Définir la facture sélectionnée
    this.patientFactures = [facture];
    this.selectedFactures = [facture.idFacture];
    this.updateMontantTotal();
  }

  // ==================== SÉLECTION FACTURES ====================

  toggleFactureSelection(idFacture: number): void {
    const index = this.selectedFactures.indexOf(idFacture);
    if (index === -1) {
      this.selectedFactures.push(idFacture);
    } else {
      this.selectedFactures.splice(index, 1);
    }
    this.updateMontantTotal();
  }

  isFactureSelected(idFacture: number): boolean {
    return this.selectedFactures.includes(idFacture);
  }

  selectAllFactures(): void {
    this.selectedFactures = this.patientFactures.map(f => f.idFacture);
    this.updateMontantTotal();
  }

  updateMontantTotal(): void {
    const total = this.patientFactures
      .filter(f => this.selectedFactures.includes(f.idFacture))
      .reduce((sum, f) => sum + f.montantRestant, 0);
    this.paiementForm.patchValue({ montant: total });
  }

  // ==================== PAIEMENT ====================

  get renduMonnaie(): number {
    const montantRecu = this.paiementForm.value.montantRecu || 0;
    const montant = this.paiementForm.value.montant || 0;
    return montantRecu > montant ? montantRecu - montant : 0;
  }

  submitPaiement(): void {
    if (this.paiementForm.invalid || this.selectedFactures.length === 0) {
      this.paiementError = 'Veuillez sélectionner au moins une facture';
      return;
    }

    this.isSubmitting = true;
    this.paiementError = '';
    this.paiementSuccess = '';

    // Pour simplifier, on traite la première facture sélectionnée
    // Dans une version plus complète, on pourrait gérer les paiements multiples
    const idFacture = this.selectedFactures[0];
    const formData = this.paiementForm.value;

    this.caisseService.creerTransaction({
      idFacture,
      montant: formData.montant,
      modePaiement: formData.modePaiement,
      montantRecu: formData.montantRecu,
      reference: formData.reference,
      notes: formData.notes,
      idempotencyToken: crypto.randomUUID()
    }).subscribe({
      next: (res) => {
        this.paiementSuccess = res.message;
        this.isSubmitting = false;
        
        // Proposer d'imprimer le reçu
        if (res.transaction?.idTransaction) {
          this.proposerImpressionRecu(res.transaction.idTransaction);
        }
        
        this.clearPatient();
        this.loadData();
        setTimeout(() => this.paiementSuccess = '', 5000);
      },
      error: (err) => {
        this.paiementError = err.error?.message || 'Erreur lors du paiement';
        this.isSubmitting = false;
      }
    });
  }

  proposerImpressionRecu(idTransaction: number): void {
    if (confirm('Paiement effectué avec succès!\n\nVoulez-vous imprimer le reçu?')) {
      this.imprimerRecu(idTransaction);
    }
  }

  imprimerRecu(idTransaction: number): void {
    this.caisseService.getRecuTransaction(idTransaction).subscribe({
      next: (recu) => {
        this.caisseService.imprimerRecu(recu);
      },
      error: (err) => {
        console.error('Erreur récupération reçu:', err);
        alert('Impossible de générer le reçu');
      }
    });
  }

  // ==================== SESSION CAISSE ====================

  openOuvrirCaisseModal(): void {
    this.showOuvrirCaisseModal = true;
    this.ouvrirCaisseForm.reset({ montantOuverture: 0 });
    this.caisseError = '';
  }

  closeOuvrirCaisseModal(): void {
    this.showOuvrirCaisseModal = false;
  }

  ouvrirCaisse(): void {
    if (this.ouvrirCaisseForm.invalid) return;

    this.caisseService.ouvrirCaisse(this.ouvrirCaisseForm.value).subscribe({
      next: (res) => {
        this.sessionActive = res.session;
        this.closeOuvrirCaisseModal();
        this.loadKpis();
      },
      error: (err) => {
        this.caisseError = err.error?.message || 'Erreur ouverture caisse';
      }
    });
  }

  openFermerCaisseModal(): void {
    this.showFermerCaisseModal = true;
    this.fermerCaisseForm.reset({ 
      montantFermeture: (this.sessionActive?.montantOuverture || 0) + (this.sessionActive?.totalEncaisse || 0) 
    });
    this.caisseError = '';
  }

  closeFermerCaisseModal(): void {
    this.showFermerCaisseModal = false;
  }

  fermerCaisse(): void {
    if (this.fermerCaisseForm.invalid) return;

    this.caisseService.fermerCaisse(this.fermerCaisseForm.value).subscribe({
      next: () => {
        this.sessionActive = null;
        this.closeFermerCaisseModal();
        this.loadKpis();
      },
      error: (err) => {
        this.caisseError = err.error?.message || 'Erreur fermeture caisse';
      }
    });
  }

  // ==================== HELPERS ====================

  formatMontant(montant: number): string {
    return this.caisseService.formatMontant(montant);
  }

  getStatutClass(statut: string): string {
    const classes: { [key: string]: string } = {
      'en_attente': 'status-pending',
      'partiel': 'status-partial',
      'payee': 'status-paid',
      'complete': 'status-paid',
      'annule': 'status-cancelled',
      'annulee': 'status-cancelled'
    };
    return classes[statut] || '';
  }

  getStatutLabel(statut: string): string {
    return this.caisseService.getStatutLabel(statut);
  }

  getModePaiementLabel(mode: string): string {
    return this.caisseService.getModePaiementLabel(mode);
  }

  // ==================== CALCULS ASSURANCE ====================

  /**
   * Vérifie si les factures sélectionnées ont une couverture assurance
   */
  hasAssuranceCoverage(): boolean {
    return this.patientFactures
      .filter(f => this.selectedFactures.includes(f.idFacture))
      .some(f => f.couvertureAssurance === true);
  }

  /**
   * Retourne le taux de couverture assurance moyen
   */
  getAssuranceTaux(): number {
    const facturesAssurance = this.patientFactures
      .filter(f => this.selectedFactures.includes(f.idFacture))
      .filter(f => f.tauxCouverture);
    
    if (facturesAssurance.length === 0) return 0;
    
    const totalTaux = facturesAssurance.reduce((sum, f) => sum + (f.tauxCouverture || 0), 0);
    return Math.round(totalTaux / facturesAssurance.length);
  }

  /**
   * Retourne le montant total des factures sélectionnées (avant déduction assurance)
   */
  getTotalMontant(): number {
    return this.patientFactures
      .filter(f => this.selectedFactures.includes(f.idFacture))
      .reduce((sum, f) => sum + f.montantTotal, 0);
  }

  /**
   * Retourne le total couvert par l'assurance
   */
  getTotalAssurance(): number {
    return this.patientFactures
      .filter(f => this.selectedFactures.includes(f.idFacture))
      .reduce((sum, f) => sum + (f.montantAssurance || 0), 0);
  }

  /**
   * Retourne le montant à payer par le patient
   */
  getTotalPatient(): number {
    return this.patientFactures
      .filter(f => this.selectedFactures.includes(f.idFacture))
      .reduce((sum, f) => sum + f.montantRestant, 0);
  }

  refresh(): void {
    this.loadData();
  }
}
