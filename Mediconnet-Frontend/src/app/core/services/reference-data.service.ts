import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface TypePrestation {
  code: string;
  libelle: string;
  description?: string;
  icone?: string;
}

export interface ReferenceItem {
  id: number;
  code: string;
  libelle: string;
  description?: string;
}

export interface AllReferenceData {
  typesPrestations: TypePrestation[];
  categoriesBeneficiaires: ReferenceItem[];
  modesPaiement: ReferenceItem[];
  zonesCouverture: ReferenceItem[];
  typesCouvertureSante: ReferenceItem[];
}

@Injectable({
  providedIn: 'root'
})
export class ReferenceDataService {
  private readonly apiUrl = `${environment.apiUrl}/reference`;
  
  // Cache pour éviter les appels répétés
  private allDataCache$?: Observable<AllReferenceData>;

  constructor(private http: HttpClient) {}

  /**
   * Récupère toutes les données de référence en une seule requête (avec cache)
   */
  getAllReferenceData(): Observable<AllReferenceData> {
    if (!this.allDataCache$) {
      this.allDataCache$ = this.http.get<AllReferenceData>(`${this.apiUrl}/all`).pipe(
        shareReplay(1)
      );
    }
    return this.allDataCache$;
  }

  /**
   * Invalide le cache (à appeler après modification des données)
   */
  invalidateCache(): void {
    this.allDataCache$ = undefined;
  }

  /**
   * Types de prestation (consultation, hospitalisation, examen, pharmacie)
   */
  getTypesPrestations(): Observable<TypePrestation[]> {
    return this.http.get<TypePrestation[]>(`${this.apiUrl}/types-prestation`);
  }

  /**
   * Catégories de bénéficiaires
   */
  getCategoriesBeneficiaires(): Observable<ReferenceItem[]> {
    return this.http.get<ReferenceItem[]>(`${this.apiUrl}/categories-beneficiaires`);
  }

  /**
   * Modes de paiement
   */
  getModesPaiement(): Observable<ReferenceItem[]> {
    return this.http.get<ReferenceItem[]>(`${this.apiUrl}/modes-paiement`);
  }

  /**
   * Zones de couverture géographique
   */
  getZonesCouverture(): Observable<ReferenceItem[]> {
    return this.http.get<ReferenceItem[]>(`${this.apiUrl}/zones-couverture`);
  }

  /**
   * Types de couverture santé (hospitalisation, maternité, etc.)
   */
  getTypesCouvertureSante(): Observable<ReferenceItem[]> {
    return this.http.get<ReferenceItem[]>(`${this.apiUrl}/types-couverture-sante`);
  }
}
