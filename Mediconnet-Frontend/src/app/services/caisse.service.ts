import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ==================== INTERFACES ====================

export interface CaisseKpi {
  revenuJour: number;
  nombreTransactionsJour: number;
  facturesEnAttente: number;
  soldeCaisse: number;
  ecartCaisse: number;
  remboursementsJour: number;
  annulationsJour: number;
  caisseOuverte: boolean;
  idSessionActive?: number;
}

export interface FactureListItem {
  idFacture: number;
  numeroFacture: string;
  patientNom: string;
  numeroDossier?: string;
  montantTotal: number;
  montantRestant: number;
  statut: string;
  dateCreation: string;
  dateEcheance?: string;
  // Informations assurance
  couvertureAssurance: boolean;
  tauxCouverture?: number;
  montantAssurance?: number;
  nomAssurance?: string;
}

export interface LigneFacture {
  idLigne: number;
  description: string;
  code?: string;
  quantite: number;
  prixUnitaire: number;
  montant: number;
  categorie?: string;
}

export interface Facture {
  idFacture: number;
  numeroFacture: string;
  idPatient: number;
  patientNom: string;
  patientPrenom: string;
  numeroDossier?: string;
  montantTotal: number;
  montantPaye: number;
  montantRestant: number;
  statut: string;
  typeFacture?: string;
  dateCreation: string;
  dateEcheance?: string;
  serviceNom?: string;
  // Informations assurance
  couvertureAssurance: boolean;
  tauxCouverture?: number;
  montantAssurance?: number;
  montantPatient?: number; // Calculé: montantTotal - montantAssurance
  nomAssurance?: string;
  lignes: LigneFacture[];
}

export interface Transaction {
  idTransaction: number;
  numeroTransaction: string;
  idFacture: number;
  numeroFacture: string;
  patientNom: string;
  numeroDossier?: string;
  montant: number;
  modePaiement: string;
  statut: string;
  reference?: string;
  dateTransaction: string;
  caissierNom: string;
  montantRecu?: number;
  renduMonnaie?: number;
}

export interface CreateTransactionRequest {
  idFacture: number;
  montant: number;
  modePaiement: string;
  montantRecu?: number;
  reference?: string;
  notes?: string;
  idempotencyToken?: string;
}

export interface AnnulerTransactionRequest {
  idTransaction: number;
  motif: string;
}

export interface SessionCaisse {
  idSession: number;
  caissierNom: string;
  montantOuverture: number;
  montantFermeture?: number;
  montantSysteme?: number;
  ecart?: number;
  dateOuverture: string;
  dateFermeture?: string;
  statut: string;
  nombreTransactions: number;
  totalEncaisse: number;
}

export interface OuvrirCaisseRequest {
  montantOuverture: number;
  notes?: string;
}

export interface FermerCaisseRequest {
  montantFermeture: number;
  notes?: string;
}

export interface PatientSearchResult {
  idPatient: number;
  nom: string;
  prenom: string;
  numeroDossier?: string;
  telephone?: string;
  facturesEnAttente: number;
}

export interface RepartitionPaiement {
  modePaiement: string;
  montant: number;
  nombre: number;
  pourcentage: number;
}

export interface RevenuParService {
  serviceNom: string;
  montant: number;
}

export interface FactureRetard {
  idFacture: number;
  numeroFacture: string;
  patientNom: string;
  montantRestant: number;
  joursRetard: number;
}

export interface RecuTransaction {
  numeroRecu: string;
  numeroTransaction: string;
  numeroFacture: string;
  dateTransaction: string;
  patientNom: string;
  patientPrenom: string;
  numeroDossier?: string;
  telephone?: string;
  montantTotal: number;
  montantPaye: number;
  montantRecu?: number;
  renduMonnaie?: number;
  modePaiement: string;
  reference?: string;
  couvertureAssurance: boolean;
  nomAssurance?: string;
  tauxCouverture?: number;
  montantAssurance?: number;
  montantPatient: number;
  typeFacture?: string;
  serviceNom?: string;
  medecinNom?: string;
  lignes: LigneFacture[];
  caissierNom: string;
  nomEtablissement: string;
  adresseEtablissement: string;
  telephoneEtablissement: string;
}

// ==================== SERVICE ====================

@Injectable({
  providedIn: 'root'
})
export class CaisseService {
  private apiUrl = `${environment.apiUrl}/caisse`;

  constructor(private http: HttpClient) {}

  // ==================== KPIs ====================

  getKpis(): Observable<CaisseKpi> {
    return this.http.get<CaisseKpi>(`${this.apiUrl}/kpis`);
  }

  // ==================== FACTURES ====================

  getFacturesEnAttente(): Observable<FactureListItem[]> {
    return this.http.get<FactureListItem[]>(`${this.apiUrl}/factures/en-attente`);
  }

  getFacturesPatient(idPatient: number): Observable<FactureListItem[]> {
    return this.http.get<FactureListItem[]>(`${this.apiUrl}/factures/patient/${idPatient}`);
  }

  getFacture(id: number): Observable<Facture> {
    return this.http.get<Facture>(`${this.apiUrl}/factures/${id}`);
  }

  // ==================== TRANSACTIONS ====================

  getTransactionsJour(): Observable<Transaction[]> {
    return this.http.get<Transaction[]>(`${this.apiUrl}/transactions/jour`);
  }

  getTransactions(dateDebut?: string, dateFin?: string, modePaiement?: string): Observable<Transaction[]> {
    let params = new HttpParams();
    if (dateDebut) params = params.set('dateDebut', dateDebut);
    if (dateFin) params = params.set('dateFin', dateFin);
    if (modePaiement) params = params.set('modePaiement', modePaiement);
    return this.http.get<Transaction[]>(`${this.apiUrl}/transactions`, { params });
  }

  creerTransaction(request: CreateTransactionRequest): Observable<{ message: string; transaction: Transaction }> {
    return this.http.post<{ message: string; transaction: Transaction }>(`${this.apiUrl}/transactions`, request);
  }

  annulerTransaction(request: AnnulerTransactionRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/transactions/annuler`, request);
  }

  // ==================== SESSION CAISSE ====================

  getSessionActive(): Observable<SessionCaisse | null> {
    return this.http.get<SessionCaisse | null>(`${this.apiUrl}/session/active`);
  }

  getHistoriqueSessions(limite: number = 10): Observable<SessionCaisse[]> {
    return this.http.get<SessionCaisse[]>(`${this.apiUrl}/session/historique`, {
      params: { limite: limite.toString() }
    });
  }

  ouvrirCaisse(request: OuvrirCaisseRequest): Observable<{ message: string; session: SessionCaisse }> {
    return this.http.post<{ message: string; session: SessionCaisse }>(`${this.apiUrl}/session/ouvrir`, request);
  }

  fermerCaisse(request: FermerCaisseRequest): Observable<{ message: string; session: SessionCaisse }> {
    return this.http.post<{ message: string; session: SessionCaisse }>(`${this.apiUrl}/session/fermer`, request);
  }

  // ==================== RECHERCHE PATIENT ====================

  rechercherPatients(query: string): Observable<PatientSearchResult[]> {
    return this.http.get<PatientSearchResult[]>(`${this.apiUrl}/patients/recherche`, {
      params: { q: query }
    });
  }

  // ==================== STATISTIQUES ====================

  getRepartitionPaiements(dateDebut?: string, dateFin?: string): Observable<RepartitionPaiement[]> {
    let params = new HttpParams();
    if (dateDebut) params = params.set('dateDebut', dateDebut);
    if (dateFin) params = params.set('dateFin', dateFin);
    return this.http.get<RepartitionPaiement[]>(`${this.apiUrl}/stats/repartition`, { params });
  }

  getRevenusParService(date?: string): Observable<RevenuParService[]> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<RevenuParService[]>(`${this.apiUrl}/stats/revenus-service`, { params });
  }

  getFacturesEnRetard(limite: number = 5): Observable<FactureRetard[]> {
    return this.http.get<FactureRetard[]>(`${this.apiUrl}/stats/factures-retard`, {
      params: { limite: limite.toString() }
    });
  }

  // ==================== REÇU ====================

  getRecuTransaction(idTransaction: number): Observable<RecuTransaction> {
    return this.http.get<RecuTransaction>(`${this.apiUrl}/transactions/${idTransaction}/recu`);
  }

  imprimerRecu(recu: RecuTransaction): void {
    const contenu = this.genererContenuRecu(recu);
    const fenetre = window.open('', '_blank', 'width=400,height=600');
    if (fenetre) {
      fenetre.document.write(contenu);
      fenetre.document.close();
      fenetre.focus();
      setTimeout(() => fenetre.print(), 250);
    }
  }

  private genererContenuRecu(recu: RecuTransaction): string {
    const dateFormatee = new Date(recu.dateTransaction).toLocaleString('fr-FR', {
      day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit'
    });
    let lignesHtml = recu.lignes?.length > 0
      ? recu.lignes.map(l => `<tr><td>${l.description}</td><td class="amount">${this.formatMontant(l.montant)}</td></tr>`).join('')
      : `<tr><td>${recu.typeFacture || 'Consultation'}</td><td class="amount">${this.formatMontant(recu.montantTotal)}</td></tr>`;

    return `<!DOCTYPE html>
<html lang="fr">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Reçu ${recu.numeroRecu}</title>
  <style>
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
      font-size: 13px;
      color: #333;
      background: #f8f9fa;
      padding: 20px;
    }
    .receipt {
      max-width: 380px;
      margin: 0 auto;
      background: white;
      border-radius: 12px;
      box-shadow: 0 4px 20px rgba(0,0,0,0.1);
      overflow: hidden;
    }
    .header {
      background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
      color: white;
      padding: 24px 20px;
      text-align: center;
    }
    .header h1 {
      font-size: 20px;
      font-weight: 600;
      margin-bottom: 8px;
    }
    .header p {
      font-size: 12px;
      opacity: 0.9;
      margin: 2px 0;
    }
    .badge {
      display: inline-block;
      background: rgba(255,255,255,0.2);
      padding: 6px 16px;
      border-radius: 20px;
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 1px;
      margin-top: 12px;
    }
    .receipt-number {
      background: #f1f5f9;
      padding: 12px 20px;
      text-align: center;
      border-bottom: 1px solid #e2e8f0;
    }
    .receipt-number span {
      font-size: 16px;
      font-weight: 700;
      color: #1e40af;
    }
    .content { padding: 20px; }
    .section {
      margin-bottom: 16px;
      padding-bottom: 16px;
      border-bottom: 1px dashed #e2e8f0;
    }
    .section:last-child { border-bottom: none; margin-bottom: 0; padding-bottom: 0; }
    .section-title {
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      color: #64748b;
      margin-bottom: 10px;
      letter-spacing: 0.5px;
    }
    .info-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 6px 0;
    }
    .info-row .label {
      color: #64748b;
      font-size: 12px;
    }
    .info-row .value {
      font-weight: 500;
      color: #1e293b;
    }
    table {
      width: 100%;
      border-collapse: collapse;
    }
    th, td {
      padding: 10px 0;
      text-align: left;
      border-bottom: 1px solid #f1f5f9;
    }
    th {
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      color: #64748b;
    }
    td.amount, th.amount { text-align: right; }
    .total-section {
      background: #f8fafc;
      margin: 0 -20px;
      padding: 16px 20px;
      border-radius: 8px;
    }
    .total-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 8px 0;
    }
    .total-row.main {
      font-size: 18px;
      font-weight: 700;
      color: #059669;
      border-top: 2px solid #e2e8f0;
      padding-top: 12px;
      margin-top: 8px;
    }
    .assurance-box {
      background: #fef3c7;
      border: 1px solid #fcd34d;
      border-radius: 8px;
      padding: 12px;
      margin-top: 12px;
    }
    .assurance-box .title {
      font-weight: 600;
      color: #92400e;
      font-size: 12px;
      margin-bottom: 6px;
    }
    .assurance-box .detail {
      font-size: 11px;
      color: #78350f;
    }
    .footer {
      background: #f8fafc;
      padding: 20px;
      text-align: center;
      border-top: 1px solid #e2e8f0;
    }
    .footer .caissier {
      font-size: 12px;
      color: #64748b;
      margin-bottom: 8px;
    }
    .footer .thanks {
      font-size: 14px;
      font-weight: 600;
      color: #1e40af;
      margin-bottom: 4px;
    }
    .footer .legal {
      font-size: 10px;
      color: #94a3b8;
    }
    .checkmark {
      width: 48px;
      height: 48px;
      background: #dcfce7;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 12px;
    }
    .checkmark svg {
      width: 24px;
      height: 24px;
      color: #16a34a;
    }
    @media print {
      body { background: white; padding: 0; }
      .receipt { box-shadow: none; border-radius: 0; }
    }
  </style>
</head>
<body>
  <div class="receipt">
    <div class="header">
      <h1>${recu.nomEtablissement}</h1>
      <p>${recu.adresseEtablissement}</p>
      <p>Tél: ${recu.telephoneEtablissement}</p>
      <div class="badge">Reçu de paiement</div>
    </div>
    
    <div class="receipt-number">
      <span>N° ${recu.numeroRecu}</span>
    </div>
    
    <div class="content">
      <div class="section">
        <div class="section-title">Informations</div>
        <div class="info-row">
          <span class="label">Date</span>
          <span class="value">${dateFormatee}</span>
        </div>
        <div class="info-row">
          <span class="label">Patient</span>
          <span class="value">${recu.patientPrenom} ${recu.patientNom}</span>
        </div>
        ${recu.numeroDossier ? `<div class="info-row"><span class="label">N° Dossier</span><span class="value">${recu.numeroDossier}</span></div>` : ''}
        ${recu.medecinNom ? `<div class="info-row"><span class="label">Médecin</span><span class="value">${recu.medecinNom}</span></div>` : ''}
      </div>
      
      <div class="section">
        <div class="section-title">Détails</div>
        <table>
          <thead>
            <tr><th>Description</th><th class="amount">Montant</th></tr>
          </thead>
          <tbody>${lignesHtml}</tbody>
        </table>
      </div>
      
      ${recu.couvertureAssurance ? `
      <div class="assurance-box">
        <div class="title">🏥 Couverture Assurance</div>
        <div class="detail">
          <strong>${recu.nomAssurance || '-'}</strong> (${recu.tauxCouverture}%)<br>
          Prise en charge: ${this.formatMontant(recu.montantAssurance || 0)}<br>
          À payer: ${this.formatMontant(recu.montantPatient)}
        </div>
      </div>` : ''}
      
      <div class="section">
        <div class="total-section">
          <div class="total-row">
            <span>Montant payé</span>
            <span>${this.formatMontant(recu.montantPaye)}</span>
          </div>
          ${recu.montantRecu ? `<div class="total-row"><span>Montant reçu</span><span>${this.formatMontant(recu.montantRecu)}</span></div>` : ''}
          ${recu.renduMonnaie ? `<div class="total-row"><span>Rendu monnaie</span><span>${this.formatMontant(recu.renduMonnaie)}</span></div>` : ''}
          <div class="total-row">
            <span>Mode de paiement</span>
            <span>${this.getModePaiementLabel(recu.modePaiement)}</span>
          </div>
          ${recu.reference ? `<div class="total-row"><span>Référence</span><span>${recu.reference}</span></div>` : ''}
          <div class="total-row main">
            <span>TOTAL</span>
            <span>${this.formatMontant(recu.montantTotal)}</span>
          </div>
        </div>
      </div>
    </div>
    
    <div class="footer">
      <div class="checkmark">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
        </svg>
      </div>
      <div class="caissier">Caissier: ${recu.caissierNom}</div>
      <div class="thanks">Merci de votre confiance!</div>
      <div class="legal">Ce reçu fait foi de paiement</div>
    </div>
  </div>
</body>
</html>`;
  }

  // ==================== HELPERS ====================

  formatMontant(montant: number): string {
    return new Intl.NumberFormat('fr-FR', {
      style: 'decimal',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(montant) + ' FCFA';
  }

  getStatutLabel(statut: string): string {
    const labels: { [key: string]: string } = {
      'en_attente': 'En attente',
      'partiel': 'Partiel',
      'payee': 'Payée',
      'annulee': 'Annulée',
      'remboursee': 'Remboursée',
      'complete': 'Complète',
      'annule': 'Annulé',
      'rembourse': 'Remboursé'
    };
    return labels[statut] || statut;
  }

  getModePaiementLabel(mode: string): string {
    const labels: { [key: string]: string } = {
      'especes': 'Espèces',
      'carte': 'Carte bancaire',
      'virement': 'Virement',
      'cheque': 'Chèque',
      'assurance': 'Assurance',
      'mobile': 'Mobile Money'
    };
    return labels[mode] || mode;
  }
}
