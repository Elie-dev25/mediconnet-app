import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type TypeAlerte = 'storage_health' | 'disk_space' | 'corruption' | 'access_denied' | 'suspicious_activity' | 'backup_failed' | 'system_error';
export type SeveriteAlerte = 'info' | 'warning' | 'critical' | 'emergency';

export interface AlerteSysteme {
  idAlerte: number;
  typeAlerte: TypeAlerte;
  message: string;
  severite: SeveriteAlerte;
  source: string;
  details?: any;
  acquittee: boolean;
  acquitteePar?: number;
  dateAcquittement?: Date;
  createdAt: Date;
}

export interface AlertesPagedResult {
  alertes: AlerteSysteme[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AlertesStats {
  totalAlertes: number;
  alertesActives: number;
  alertesCritiques: number;
  alertesAcquittees: number;
  parType: { type: string; count: number }[];
  parSeverite: { severite: string; count: number }[];
}

export interface AlertesFilter {
  page?: number;
  pageSize?: number;
  type?: TypeAlerte;
  severite?: SeveriteAlerte;
  acquittee?: boolean;
  dateFrom?: Date;
  dateTo?: Date;
}

export interface StorageHealth {
  diskUsagePercent: number;
  diskFreeGb: number;
  diskTotalGb: number;
  totalDocuments: number;
  documentsActifs: number;
  documentsArchives: number;
  documentsQuarantaine: number;
  totalSizeGb: number;
  lastVerificationDate?: Date;
  integrityStatus: 'ok' | 'warning' | 'critical';
}

@Injectable({
  providedIn: 'root'
})
export class AlertesSystemeService {
  private apiUrl = `${environment.apiUrl}/alertes`;

  constructor(private http: HttpClient) {}

  /**
   * Récupère les alertes actives (non acquittées)
   */
  getAlertesActives(): Observable<AlerteSysteme[]> {
    return this.http.get<AlerteSysteme[]>(`${this.apiUrl}/actives`);
  }

  /**
   * Récupère toutes les alertes avec pagination et filtres
   */
  getAlertes(filter: AlertesFilter = {}): Observable<AlertesPagedResult> {
    let params = new HttpParams();

    if (filter.page) params = params.set('page', filter.page.toString());
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    if (filter.type) params = params.set('type', filter.type);
    if (filter.severite) params = params.set('severite', filter.severite);
    if (filter.acquittee !== undefined) params = params.set('acquittee', filter.acquittee.toString());
    if (filter.dateFrom) params = params.set('dateFrom', filter.dateFrom.toISOString());
    if (filter.dateTo) params = params.set('dateTo', filter.dateTo.toISOString());

    return this.http.get<AlertesPagedResult>(this.apiUrl, { params });
  }

  /**
   * Acquitte une alerte
   */
  acquitterAlerte(idAlerte: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${idAlerte}/acquitter`, {});
  }

  /**
   * Crée une nouvelle alerte (usage interne/admin)
   */
  creerAlerte(alerte: Partial<AlerteSysteme>): Observable<AlerteSysteme> {
    return this.http.post<AlerteSysteme>(this.apiUrl, alerte);
  }

  /**
   * Récupère les statistiques des alertes
   */
  getStats(): Observable<AlertesStats> {
    return this.http.get<AlertesStats>(`${this.apiUrl}/stats`);
  }

  /**
   * Récupère l'état de santé du stockage
   */
  getStorageHealth(): Observable<StorageHealth> {
    return this.http.get<StorageHealth>(`${environment.apiUrl}/documents/disk-space`);
  }

  /**
   * Retourne l'icône correspondant au type d'alerte
   */
  getAlerteIcon(type: TypeAlerte): string {
    const icons: Record<TypeAlerte, string> = {
      'storage_health': 'hard-drive',
      'disk_space': 'database',
      'corruption': 'file-warning',
      'access_denied': 'shield-x',
      'suspicious_activity': 'user-x',
      'backup_failed': 'cloud-off',
      'system_error': 'alert-octagon'
    };
    return icons[type] || 'alert-circle';
  }

  /**
   * Retourne la classe CSS pour la sévérité
   */
  getSeveriteClass(severite: SeveriteAlerte): string {
    const classes: Record<SeveriteAlerte, string> = {
      'info': 'badge-info',
      'warning': 'badge-warning',
      'critical': 'badge-error',
      'emergency': 'badge-emergency'
    };
    return classes[severite] || 'badge-default';
  }

  /**
   * Retourne le label français pour le type d'alerte
   */
  getTypeLabel(type: TypeAlerte): string {
    const labels: Record<TypeAlerte, string> = {
      'storage_health': 'Santé stockage',
      'disk_space': 'Espace disque',
      'corruption': 'Corruption fichier',
      'access_denied': 'Accès refusé',
      'suspicious_activity': 'Activité suspecte',
      'backup_failed': 'Échec sauvegarde',
      'system_error': 'Erreur système'
    };
    return labels[type] || type;
  }

  /**
   * Retourne le label français pour la sévérité
   */
  getSeveriteLabel(severite: SeveriteAlerte): string {
    const labels: Record<SeveriteAlerte, string> = {
      'info': 'Information',
      'warning': 'Avertissement',
      'critical': 'Critique',
      'emergency': 'Urgence'
    };
    return labels[severite] || severite;
  }
}
