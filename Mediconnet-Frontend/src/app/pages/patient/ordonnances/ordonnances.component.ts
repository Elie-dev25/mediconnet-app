import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';

import { OrdonnancesPatientService, DossierPharmaceutiqueDto, OrdonnancePatientDto, FiltreOrdonnancesPatientRequest } from '../../../services/ordonnances-patient.service';
import { 
  DashboardLayoutComponent,
  LucideAngularModule,
  ALL_ICONS_PROVIDER
} from '../../../shared';
import { PATIENT_MENU_ITEMS, PATIENT_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-ordonnances',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DashboardLayoutComponent,
    LucideAngularModule
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './ordonnances.component.html',
  styleUrls: ['./ordonnances.component.scss']
})
export class OrdonnancesComponent implements OnInit, OnDestroy {
  dossierPharmaceutique: DossierPharmaceutiqueDto | null = null;
  selectedOrdonnance: OrdonnancePatientDto | null = null;
  loading = true;
  error: string | null = null;

  // Filtres
  filtre: FiltreOrdonnancesPatientRequest = {
    tri: 'date_desc',
    page: 1,
    pageSize: 20
  };

  // Affichage
  affichageMode: 'liste' | 'cartes' = 'liste';
  showDetails = false;

  // Menu sidebar
  menuItems = PATIENT_MENU_ITEMS;
  sidebarTitle = PATIENT_SIDEBAR_TITLE;

  private subscriptions: Subscription[] = [];

  constructor(
    public ordonnancesService: OrdonnancesPatientService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.loadDossierPharmaceutique();
    
    // Écouter les changements de paramètres de route
    const routeSub = this.route.queryParams.subscribe(params => {
      if (params['statut']) {
        this.filtre.statut = params['statut'];
        this.loadDossierPharmaceutique();
      }
      if (params['contexte']) {
        this.filtre.typeContexte = params['contexte'];
        this.loadDossierPharmaceutique();
      }
    });
    this.subscriptions.push(routeSub);
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  /**
   * Charge le dossier pharmaceutique du patient
   */
  loadDossierPharmaceutique(): void {
    this.loading = true;
    this.error = null;

    const sub = this.ordonnancesService.getDossierPharmaceutique(this.filtre).subscribe({
      next: (dossier) => {
        this.dossierPharmaceutique = dossier;
        this.loading = false;
      },
      error: (err) => {
        console.error('Erreur lors du chargement du dossier pharmaceutique:', err);
        this.error = 'Impossible de charger vos ordonnances. Veuillez réessayer.';
        this.loading = false;
      }
    });
    this.subscriptions.push(sub);
  }

  /**
   * Sélectionne une ordonnance pour afficher les détails
   */
  selectOrdonnance(ordonnance: OrdonnancePatientDto): void {
    this.selectedOrdonnance = ordonnance;
    this.showDetails = true;
  }

  /**
   * Ferme les détails de l'ordonnance
   */
  closeDetails(): void {
    this.selectedOrdonnance = null;
    this.showDetails = false;
  }

  /**
   * Applique les filtres et recharge les données
   */
  appliquerFiltres(): void {
    this.filtre.page = 1; // Reset à la première page
    this.loadDossierPharmaceutique();
  }

  /**
   * Réinitialise tous les filtres
   */
  reinitialiserFiltres(): void {
    this.filtre = {
      tri: 'date_desc',
      page: 1,
      pageSize: 20
    };
    this.loadDossierPharmaceutique();
  }

  /**
   * Change le mode d'affichage
   */
  setAffichageMode(mode: 'liste' | 'cartes'): void {
    this.affichageMode = mode;
  }

  /**
   * Change le tri
   */
  setTri(tri: string | undefined): void {
    this.filtre.tri = tri || 'date_desc';
    this.loadDossierPharmaceutique();
  }

  /**
   * Imprime l'ordonnance sélectionnée
   */
  imprimerOrdonnance(): void {
    if (!this.selectedOrdonnance) return;

    const printContent = this.generatePrintContent(this.selectedOrdonnance);
    const printWindow = window.open('', '_blank');
    
    if (printWindow) {
      printWindow.document.write(printContent);
      printWindow.document.close();
      printWindow.print();
    }
  }

  /**
   * Génère le contenu HTML pour l'impression
   */
  private generatePrintContent(ordonnance: OrdonnancePatientDto): string {
    const medicamentsHtml = ordonnance.medicaments.map(med => `
      <tr>
        <td>${med.nomMedicament}</td>
        <td>${med.dosage || '-'}</td>
        <td>${med.quantitePrescrite}</td>
        <td>${med.posologie || '-'}</td>
        <td>${med.frequence || '-'}</td>
        <td>${med.dureeTraitement || '-'}</td>
        <td>${med.instructions || '-'}</td>
        <td class="text-center">
          <span class="badge ${this.ordonnancesService.getStatutDelivranceClass(med.statutDelivrance)}">
            ${this.ordonnancesService.getStatutDelivranceLabel(med.statutDelivrance)}
          </span>
        </td>
      </tr>
    `).join('');

    return `
      <!DOCTYPE html>
      <html>
      <head>
        <title>Ordonnance - ${this.dossierPharmaceutique?.nomPatient}</title>
        <style>
          body { font-family: Arial, sans-serif; margin: 20px; color: #333; }
          .header { text-align: center; border-bottom: 2px solid #007bff; padding-bottom: 20px; margin-bottom: 30px; }
          .header h1 { color: #007bff; margin: 0; }
          .header p { margin: 5px 0; color: #666; }
          .info-section { margin-bottom: 25px; }
          .info-section h3 { color: #007bff; border-bottom: 1px solid #ddd; padding-bottom: 5px; }
          .info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin-top: 10px; }
          .info-item { margin-bottom: 8px; }
          .info-item strong { color: #333; }
          .badge { padding: 4px 8px; border-radius: 4px; font-size: 12px; font-weight: bold; }
          .badge-success { background-color: #d4edda; color: #155724; }
          .badge-warning { background-color: #fff3cd; color: #856404; }
          .badge-info { background-color: #d1ecf1; color: #0c5460; }
          .badge-danger { background-color: #f8d7da; color: #721c24; }
          .badge-secondary { background-color: #e2e3e5; color: #383d41; }
          table { width: 100%; border-collapse: collapse; margin-top: 20px; }
          th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
          th { background-color: #f8f9fa; font-weight: bold; }
          .text-center { text-align: center; }
          .footer { margin-top: 40px; padding-top: 20px; border-top: 1px solid #ddd; text-align: center; color: #666; font-size: 12px; }
          @media print { body { margin: 10px; } }
        </style>
      </head>
      <body>
        <div class="header">
          <h1>ORDONNANCE MÉDICALE</h1>
          <p><strong>MédiConnect - Système de Gestion Médicale</strong></p>
        </div>

        <div class="info-section">
          <h3>Informations Patient</h3>
          <div class="info-grid">
            <div class="info-item"><strong>Nom:</strong> ${this.dossierPharmaceutique?.nomPatient}</div>
            <div class="info-item"><strong>Date:</strong> ${this.ordonnancesService.formatDate(ordonnance.datePrescription)}</div>
          </div>
        </div>

        <div class="info-section">
          <h3>Informations Médecin</h3>
          <div class="info-grid">
            <div class="info-item"><strong>Médecin:</strong> ${ordonnance.nomMedecin}</div>
            <div class="info-item"><strong>Spécialité:</strong> ${ordonnance.specialiteMedecin || '-'}</div>
            <div class="info-item"><strong>Service:</strong> ${ordonnance.service || '-'}</div>
            <div class="info-item"><strong>Contexte:</strong> ${this.ordonnancesService.getTypeContexteLabel(ordonnance.typeContexte)}</div>
          </div>
        </div>

        ${ordonnance.diagnostic ? `
        <div class="info-section">
          <h3>Diagnostic</h3>
          <p>${ordonnance.diagnostic}</p>
        </div>
        ` : ''}

        ${ordonnance.notes ? `
        <div class="info-section">
          <h3>Notes</h3>
          <p>${ordonnance.notes}</p>
        </div>
        ` : ''}

        <div class="info-section">
          <h3>Médicaments Prescrits</h3>
          <table>
            <thead>
              <tr>
                <th>Médicament</th>
                <th>Dosage</th>
                <th>Quantité</th>
                <th>Posologie</th>
                <th>Fréquence</th>
                <th>Durée</th>
                <th>Instructions</th>
                <th>Statut</th>
              </tr>
            </thead>
            <tbody>
              ${medicamentsHtml}
            </tbody>
          </table>
        </div>

        ${ordonnance.dateDelivrance ? `
        <div class="info-section">
          <h3>Informations de Délivrance</h3>
          <div class="info-grid">
            <div class="info-item"><strong>Date de délivrance:</strong> ${this.ordonnancesService.formatDateTime(ordonnance.dateDelivrance)}</div>
            <div class="info-item"><strong>Pharmacien:</strong> ${ordonnance.nomPharmacien || '-'}</div>
            <div class="info-item"><strong>Statut:</strong> 
              <span class="badge ${this.ordonnancesService.getStatutDelivranceClass(ordonnance.statutDelivrance)}">
                ${this.ordonnancesService.getStatutDelivranceLabel(ordonnance.statutDelivrance)}
              </span>
            </div>
          </div>
        </div>
        ` : ''}

        <div class="footer">
          <p>Document généré le ${new Date().toLocaleDateString('fr-FR')} à ${new Date().toLocaleTimeString('fr-FR')}</p>
          <p>MédiConnect - Plateforme de Gestion Médicale</p>
        </div>
      </body>
      </html>
    `;
  }

  // Getters pour le template
  get statutOptions() {
    return [
      { value: '', label: 'Tous les statuts' },
      { value: 'active', label: 'Actives' },
      { value: 'dispensee', label: 'Délivrées' },
      { value: 'partielle', label: 'Partielles' },
      { value: 'annulee', label: 'Annulées' },
      { value: 'expiree', label: 'Expirées' }
    ];
  }

  get contexteOptions() {
    return [
      { value: '', label: 'Tous les contextes' },
      { value: 'consultation', label: 'Consultations' },
      { value: 'hospitalisation', label: 'Hospitalisations' },
      { value: 'directe', label: 'Prescriptions directes' }
    ];
  }

  get triOptions() {
    return [
      { value: 'date_desc', label: 'Date (plus récent)' },
      { value: 'date_asc', label: 'Date (plus ancien)' },
      { value: 'medecin', label: 'Médecin' }
    ];
  }
}
