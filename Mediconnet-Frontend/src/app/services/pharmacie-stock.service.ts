import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface PharmacieKpi {
  totalMedicaments: number;
  medicamentsEnAlerte: number;
  medicamentsEnRupture: number;
  medicamentsPerimesProches: number;
  ordonnancesEnAttente: number;
  dispensationsJour: number;
  valeurStock: number;
  commandesEnCours: number;
}

export interface AlerteStock {
  type: string;
  idMedicament: number;
  nomMedicament: string;
  stockActuel?: number;
  seuilAlerte?: number;
  datePeremption?: string;
  joursRestants?: number;
  priorite: string;
}

export interface MedicamentStock {
  idMedicament: number;
  nom: string;
  dosage?: string;
  formeGalenique?: string;
  laboratoire?: string;
  stock?: number;
  seuilStock?: number;
  prix?: number;
  datePeremption?: string;
  emplacementRayon?: string;
  codeATC?: string;
  actif: boolean;
  conditionnement?: string;
  temperatureConservation?: string;
  statutStock: string;
  joursAvantPeremption?: number;
}

export interface CreateMedicamentRequest {
  nom: string;
  dosage?: string;
  formeGalenique?: string;
  laboratoire?: string;
  stock: number;
  seuilStock: number;
  prix: number;
  datePeremption?: string;
  emplacementRayon?: string;
  codeATC?: string;
  conditionnement?: string;
  temperatureConservation?: string;
}

export interface UpdateMedicamentRequest {
  nom?: string;
  dosage?: string;
  formeGalenique?: string;
  laboratoire?: string;
  seuilStock?: number;
  prix?: number;
  datePeremption?: string;
  emplacementRayon?: string;
  codeATC?: string;
  conditionnement?: string;
  temperatureConservation?: string;
  actif?: boolean;
}

export interface AjustementStockRequest {
  idMedicament: number;
  quantite: number;
  typeMouvement: string;
  motif?: string;
}

export interface MouvementStock {
  idMouvement: number;
  idMedicament: number;
  nomMedicament: string;
  typeMouvement: string;
  quantite: number;
  dateMouvement: string;
  motif?: string;
  referenceType?: string;
  referenceId?: number;
  stockApresMouvement: number;
  nomUtilisateur: string;
}

export interface MouvementStockFilter {
  idMedicament?: number;
  typeMouvement?: string;
  dateDebut?: string;
  dateFin?: string;
  page?: number;
  pageSize?: number;
}

export interface Fournisseur {
  idFournisseur: number;
  nomFournisseur: string;
  contactNom?: string;
  contactEmail?: string;
  contactTelephone?: string;
  adresse?: string;
  conditionsPaiement?: string;
  delaiLivraisonJours: number;
  actif: boolean;
}

export interface CreateFournisseurRequest {
  nomFournisseur: string;
  contactNom?: string;
  contactEmail?: string;
  contactTelephone?: string;
  adresse?: string;
  conditionsPaiement?: string;
  delaiLivraisonJours: number;
}

export interface CommandePharmacie {
  idCommande: number;
  idFournisseur: number;
  nomFournisseur: string;
  dateCommande: string;
  dateReceptionPrevue?: string;
  dateReceptionReelle?: string;
  statut: string;
  montantTotal: number;
  notes?: string;
  nomUtilisateur: string;
  lignes: CommandeLigne[];
}

export interface CommandeLigne {
  idLigneCommande: number;
  idMedicament: number;
  nomMedicament: string;
  quantiteCommandee: number;
  quantiteRecue: number;
  prixAchat: number;
  datePeremption?: string;
  numeroLot?: string;
}

export interface CreateCommandeRequest {
  idFournisseur: number;
  dateReceptionPrevue?: string;
  notes?: string;
  lignes: CreateCommandeLigneRequest[];
}

export interface CreateCommandeLigneRequest {
  idMedicament: number;
  quantiteCommandee: number;
  prixAchat: number;
}

export interface ReceptionCommandeRequest {
  lignes: ReceptionLigneRequest[];
}

export interface ReceptionLigneRequest {
  idLigneCommande: number;
  quantiteRecue: number;
  datePeremption?: string;
  numeroLot?: string;
}

export interface OrdonnancePharmacie {
  idOrdonnance: number;
  date: string;
  idPatient: number;
  nomPatient: string;
  nomMedecin: string;
  commentaire?: string;
  statut: string;
  medicaments: MedicamentPrescrit[];
}

export interface MedicamentPrescrit {
  idMedicament: number;
  nomMedicament: string;
  dosage?: string;
  quantitePrescrite: number;
  quantiteDispensee: number;
  posologie?: string;
  dureeTraitement?: string;
  stockDisponible?: number;
  prixUnitaire?: number;
}

export interface CreateDispensationRequest {
  idPrescription: number;
  notes?: string;
  lignes: DispensationLigneRequest[];
}

export interface DispensationLigneRequest {
  idMedicament: number;
  quantiteDispensee: number;
  numeroLot?: string;
}

export interface Dispensation {
  idDispensation: number;
  idPrescription: number;
  nomPatient: string;
  nomPharmacien: string;
  dateDispensation: string;
  statut: string;
  notes?: string;
  montantTotal: number;
  lignes: DispensationLigne[];
}

export interface DispensationLigne {
  idLigne: number;
  idMedicament: number;
  nomMedicament: string;
  quantitePrescrite: number;
  quantiteDispensee: number;
  prixUnitaire?: number;
  montantTotal?: number;
  numeroLot?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalItems: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root'
})
export class PharmacieStockService {
  private apiUrl = `${environment.apiUrl}/pharmacie`;

  constructor(private http: HttpClient) {}

  // ==================== KPIs & Dashboard ====================

  getKpis(): Observable<PharmacieKpi> {
    return this.http.get<PharmacieKpi>(`${this.apiUrl}/kpis`);
  }

  getAlertes(): Observable<AlerteStock[]> {
    return this.http.get<AlerteStock[]>(`${this.apiUrl}/alertes`);
  }

  // ==================== Médicaments/Stock ====================

  getMedicaments(search?: string, statut?: string, page = 1, pageSize = 20): Observable<PagedResult<MedicamentStock>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (search) params = params.set('search', search);
    if (statut) params = params.set('statut', statut);

    return this.http.get<PagedResult<MedicamentStock>>(`${this.apiUrl}/medicaments`, { params });
  }

  /**
   * Recherche de médicaments pour autocomplete (retourne les 10 premiers résultats)
   */
  searchMedicamentsForAutocomplete(search: string): Observable<MedicamentStock[]> {
    if (!search || search.length < 2) {
      return new Observable(observer => {
        observer.next([]);
        observer.complete();
      });
    }
    
    let params = new HttpParams()
      .set('search', search)
      .set('page', '1')
      .set('pageSize', '10');

    return this.http.get<PagedResult<MedicamentStock>>(`${this.apiUrl}/medicaments`, { params }).pipe(
      map(result => result.items)
    );
  }

  getMedicamentById(id: number): Observable<MedicamentStock> {
    return this.http.get<MedicamentStock>(`${this.apiUrl}/medicaments/${id}`);
  }

  createMedicament(request: CreateMedicamentRequest): Observable<MedicamentStock> {
    return this.http.post<MedicamentStock>(`${this.apiUrl}/medicaments`, request);
  }

  updateMedicament(id: number, request: UpdateMedicamentRequest): Observable<MedicamentStock> {
    return this.http.put<MedicamentStock>(`${this.apiUrl}/medicaments/${id}`, request);
  }

  deleteMedicament(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/medicaments/${id}`);
  }

  ajusterStock(request: AjustementStockRequest): Observable<MouvementStock> {
    return this.http.post<MouvementStock>(`${this.apiUrl}/stock/ajustement`, request);
  }

  // ==================== Mouvements ====================

  getMouvements(filter: MouvementStockFilter): Observable<PagedResult<MouvementStock>> {
    let params = new HttpParams();
    
    if (filter.idMedicament) params = params.set('idMedicament', filter.idMedicament.toString());
    if (filter.typeMouvement) params = params.set('typeMouvement', filter.typeMouvement);
    if (filter.dateDebut) params = params.set('dateDebut', filter.dateDebut);
    if (filter.dateFin) params = params.set('dateFin', filter.dateFin);
    params = params.set('page', (filter.page || 1).toString());
    params = params.set('pageSize', (filter.pageSize || 20).toString());

    return this.http.get<PagedResult<MouvementStock>>(`${this.apiUrl}/mouvements`, { params });
  }

  // ==================== Fournisseurs ====================

  getFournisseurs(actif?: boolean): Observable<Fournisseur[]> {
    let params = new HttpParams();
    if (actif !== undefined) params = params.set('actif', actif.toString());
    return this.http.get<Fournisseur[]>(`${this.apiUrl}/fournisseurs`, { params });
  }

  createFournisseur(request: CreateFournisseurRequest): Observable<Fournisseur> {
    return this.http.post<Fournisseur>(`${this.apiUrl}/fournisseurs`, request);
  }

  updateFournisseur(id: number, request: CreateFournisseurRequest): Observable<Fournisseur> {
    return this.http.put<Fournisseur>(`${this.apiUrl}/fournisseurs/${id}`, request);
  }

  // ==================== Commandes ====================

  getCommandes(statut?: string, page = 1, pageSize = 20): Observable<PagedResult<CommandePharmacie>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (statut) params = params.set('statut', statut);

    return this.http.get<PagedResult<CommandePharmacie>>(`${this.apiUrl}/commandes`, { params });
  }

  getCommandeById(id: number): Observable<CommandePharmacie> {
    return this.http.get<CommandePharmacie>(`${this.apiUrl}/commandes/${id}`);
  }

  createCommande(request: CreateCommandeRequest): Observable<CommandePharmacie> {
    return this.http.post<CommandePharmacie>(`${this.apiUrl}/commandes`, request);
  }

  receptionnerCommande(id: number, request: ReceptionCommandeRequest): Observable<CommandePharmacie> {
    return this.http.post<CommandePharmacie>(`${this.apiUrl}/commandes/${id}/reception`, request);
  }

  annulerCommande(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/commandes/${id}/annuler`, {});
  }

  // ==================== Ordonnances/Dispensations ====================

  getOrdonnances(search?: string, page = 1, pageSize = 20): Observable<PagedResult<OrdonnancePharmacie>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (search) params = params.set('search', search);

    return this.http.get<PagedResult<OrdonnancePharmacie>>(`${this.apiUrl}/ordonnances`, { params });
  }

  dispenserOrdonnance(request: CreateDispensationRequest): Observable<Dispensation> {
    return this.http.post<Dispensation>(`${this.apiUrl}/dispensations`, request);
  }

  getDispensations(dateDebut?: string, dateFin?: string, page = 1, pageSize = 20): Observable<PagedResult<Dispensation>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (dateDebut) params = params.set('dateDebut', dateDebut);
    if (dateFin) params = params.set('dateFin', dateFin);

    return this.http.get<PagedResult<Dispensation>>(`${this.apiUrl}/dispensations`, { params });
  }
}
