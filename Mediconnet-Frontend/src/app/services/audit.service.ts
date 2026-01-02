import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AuditLogDto {
  id: number;
  userId: number;
  userName?: string;
  action: string;
  resourceType: string;
  resourceId?: number;
  details?: string;
  ipAddress?: string;
  success: boolean;
  createdAt: Date;
}

export interface AuditLogsPagedResult {
  logs: AuditLogDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AuditStatByDay {
  date: Date;
  count: number;
  failures: number;
}

export interface AuditStatByAction {
  action: string;
  count: number;
}

export interface AuditStatsDto {
  totalLogs: number;
  totalSuccess: number;
  totalFailures: number;
  authFailures: number;
  sensitiveAccess: number;
  logsByDay: AuditStatByDay[];
  topActions: AuditStatByAction[];
}

export interface AuditLogsFilter {
  page?: number;
  pageSize?: number;
  action?: string;
  resourceType?: string;
  userId?: number;
  dateFrom?: Date;
  dateTo?: Date;
  successOnly?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AuditService {
  private apiUrl = `${environment.apiUrl}/admin/audit`;

  constructor(private http: HttpClient) {}

  /**
   * Récupère les logs d'audit avec pagination et filtres
   */
  getLogs(filter: AuditLogsFilter = {}): Observable<AuditLogsPagedResult> {
    let params = new HttpParams();

    if (filter.page) params = params.set('page', filter.page.toString());
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    if (filter.action) params = params.set('action', filter.action);
    if (filter.resourceType) params = params.set('resourceType', filter.resourceType);
    if (filter.userId) params = params.set('userId', filter.userId.toString());
    if (filter.dateFrom) params = params.set('dateFrom', filter.dateFrom.toISOString());
    if (filter.dateTo) params = params.set('dateTo', filter.dateTo.toISOString());
    if (filter.successOnly !== undefined) params = params.set('successOnly', filter.successOnly.toString());

    return this.http.get<AuditLogsPagedResult>(`${this.apiUrl}/logs`, { params });
  }

  /**
   * Récupère les statistiques d'audit
   */
  getStats(days: number = 7): Observable<AuditStatsDto> {
    return this.http.get<AuditStatsDto>(`${this.apiUrl}/logs/stats`, {
      params: { days: days.toString() }
    });
  }

  /**
   * Récupère les actions disponibles pour le filtrage
   */
  getAvailableActions(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/logs/actions`);
  }

  /**
   * Récupère les types de ressources disponibles pour le filtrage
   */
  getAvailableResourceTypes(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/logs/resources`);
  }

  /**
   * Formatte une action pour l'affichage
   */
  formatAction(action: string): string {
    const actionLabels: { [key: string]: string } = {
      'LOGIN_SUCCESS': 'Connexion réussie',
      'LOGIN_FAILED': 'Échec de connexion',
      'AUTH_FAILURE': 'Échec d\'authentification',
      'LOGOUT': 'Déconnexion',
      'PATIENT_CREATED_BY_RECEPTION': 'Patient créé (accueil)',
      'CONSULTATION_ENREGISTREE': 'Consultation enregistrée',
      'FIRST_LOGIN_COMPLETED': 'Première connexion validée',
      'DECLARATION_ACCEPTED': 'Déclaration acceptée',
      'SENSITIVE_DATA_ACCESS': 'Accès données sensibles',
      'PASSWORD_CHANGED': 'Mot de passe modifié',
      'PROFILE_UPDATED': 'Profil mis à jour'
    };
    return actionLabels[action] || action.replace(/_/g, ' ');
  }

  /**
   * Retourne la classe CSS pour le badge d'action
   */
  getActionBadgeClass(action: string, success: boolean): string {
    if (!success) return 'badge-error';
    
    if (action.includes('LOGIN') || action.includes('AUTH')) return 'badge-info';
    if (action.includes('CREATE') || action.includes('ENREGISTR')) return 'badge-success';
    if (action.includes('UPDATE') || action.includes('CHANGE')) return 'badge-warning';
    if (action.includes('DELETE') || action.includes('REMOVE')) return 'badge-error';
    if (action.includes('SENSITIVE')) return 'badge-warning';
    
    return 'badge-default';
  }
}
