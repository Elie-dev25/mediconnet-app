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
      ? recu.lignes.map(l => `<tr><td>${l.description}</td><td style="text-align:right">${this.formatMontant(l.montant)}</td></tr>`).join('')
      : `<tr><td>${recu.typeFacture || 'Consultation'}</td><td style="text-align:right">${this.formatMontant(recu.montantTotal)}</td></tr>`;

    return `<!DOCTYPE html><html><head><meta charset="UTF-8"><title>Reçu ${recu.numeroRecu}</title>
<style>body{font-family:'Courier New',monospace;font-size:12px;margin:20px}.header{text-align:center;border-bottom:1px dashed #000;padding-bottom:10px;margin-bottom:10px}.header h2{margin:0 0 5px;font-size:16px}.header p{margin:2px 0;font-size:11px}.info{margin:10px 0}.info-row{display:flex;justify-content:space-between;margin:3px 0}.info-row .label{font-weight:bold}table{width:100%;border-collapse:collapse;margin:10px 0}th,td{padding:5px;text-align:left;border-bottom:1px dotted #ccc}.total{border-top:2px solid #000;font-weight:bold;font-size:14px}.footer{text-align:center;margin-top:20px;font-size:10px;border-top:1px dashed #000;padding-top:10px}.assurance{background:#f5f5f5;padding:5px;margin:5px 0;font-size:11px}@media print{body{margin:0}}</style></head>
<body><div class="header"><h2>${recu.nomEtablissement}</h2><p>${recu.adresseEtablissement}</p><p>Tél: ${recu.telephoneEtablissement}</p></div>
<div style="text-align:center;font-weight:bold;font-size:14px;margin:10px 0">REÇU DE PAIEMENT</div>
<div style="text-align:center;margin-bottom:10px">N° ${recu.numeroRecu}</div>
<div class="info"><div class="info-row"><span class="label">Date:</span><span>${dateFormatee}</span></div>
<div class="info-row"><span class="label">Patient:</span><span>${recu.patientPrenom} ${recu.patientNom}</span></div>
${recu.numeroDossier ? `<div class="info-row"><span class="label">Dossier:</span><span>${recu.numeroDossier}</span></div>` : ''}
${recu.medecinNom ? `<div class="info-row"><span class="label">Médecin:</span><span>${recu.medecinNom}</span></div>` : ''}</div>
<table><thead><tr><th>Description</th><th style="text-align:right">Montant</th></tr></thead><tbody>${lignesHtml}</tbody></table>
${recu.couvertureAssurance ? `<div class="assurance"><div>Assurance: ${recu.nomAssurance || '-'} (${recu.tauxCouverture}%)</div><div>Prise en charge: ${this.formatMontant(recu.montantAssurance || 0)}</div><div>À payer par patient: ${this.formatMontant(recu.montantPatient)}</div></div>` : ''}
<div class="info"><div class="info-row total"><span>TOTAL:</span><span>${this.formatMontant(recu.montantTotal)}</span></div>
<div class="info-row"><span class="label">Montant payé:</span><span>${this.formatMontant(recu.montantPaye)}</span></div>
${recu.montantRecu ? `<div class="info-row"><span class="label">Montant reçu:</span><span>${this.formatMontant(recu.montantRecu)}</span></div>` : ''}
${recu.renduMonnaie ? `<div class="info-row"><span class="label">Rendu monnaie:</span><span>${this.formatMontant(recu.renduMonnaie)}</span></div>` : ''}
<div class="info-row"><span class="label">Mode:</span><span>${this.getModePaiementLabel(recu.modePaiement)}</span></div>
${recu.reference ? `<div class="info-row"><span class="label">Réf:</span><span>${recu.reference}</span></div>` : ''}</div>
<div class="footer"><p>Caissier: ${recu.caissierNom}</p><p>Merci de votre confiance!</p><p>Ce reçu fait foi de paiement</p></div></body></html>`;
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
