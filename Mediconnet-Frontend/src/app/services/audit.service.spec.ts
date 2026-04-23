import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { AuditService } from './audit.service';
import { createHttpClientMock, paramsFromCall } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('AuditService', () => {
  let service: AuditService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/admin/audit`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new AuditService(httpMock.http);
  });

  describe('HTTP', () => {
    it('getLogs with no filter', async () => {
      await firstValueFrom(service.getLogs());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.keys().length).toBe(0);
      expect(httpMock.get.mock.calls[0][0]).toBe(`${base}/logs`);
    });

    it('getLogs with all filters', async () => {
      const dateFrom = new Date('2026-01-01T00:00:00Z');
      const dateTo = new Date('2026-01-31T00:00:00Z');
      await firstValueFrom(
        service.getLogs({
          page: 2,
          pageSize: 50,
          action: 'LOGIN',
          resourceType: 'User',
          userId: 3,
          dateFrom,
          dateTo,
          successOnly: true,
        })
      );
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('page')).toBe('2');
      expect(params?.get('pageSize')).toBe('50');
      expect(params?.get('action')).toBe('LOGIN');
      expect(params?.get('resourceType')).toBe('User');
      expect(params?.get('userId')).toBe('3');
      expect(params?.get('dateFrom')).toBe(dateFrom.toISOString());
      expect(params?.get('dateTo')).toBe(dateTo.toISOString());
      expect(params?.get('successOnly')).toBe('true');
    });

    it('getStats default days=7', async () => {
      await firstValueFrom(service.getStats());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/logs/stats`, { params: { days: '7' } });
    });

    it('getStats custom days', async () => {
      await firstValueFrom(service.getStats(30));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/logs/stats`, { params: { days: '30' } });
    });

    it('getAvailableActions', async () => {
      await firstValueFrom(service.getAvailableActions());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/logs/actions`);
    });

    it('getAvailableResourceTypes', async () => {
      await firstValueFrom(service.getAvailableResourceTypes());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/logs/resources`);
    });
  });

  describe('formatAction', () => {
    it('should return known label for known action', () => {
      expect(service.formatAction('LOGIN_SUCCESS')).toBe('Connexion réussie');
      expect(service.formatAction('LOGIN_FAILED')).toBe('Échec de connexion');
      expect(service.formatAction('LOGOUT')).toBe('Déconnexion');
      expect(service.formatAction('PROFILE_UPDATED')).toBe('Profil mis à jour');
    });

    it('should fallback to underscore-replaced string for unknown action', () => {
      expect(service.formatAction('CUSTOM_UNKNOWN_ACTION')).toBe('CUSTOM UNKNOWN ACTION');
    });

    it('should handle single word without underscore', () => {
      expect(service.formatAction('UNKNOWN')).toBe('UNKNOWN');
    });
  });

  describe('getActionBadgeClass', () => {
    it('should return badge-error when success=false regardless of action', () => {
      expect(service.getActionBadgeClass('LOGIN_SUCCESS', false)).toBe('badge-error');
      expect(service.getActionBadgeClass('ANYTHING', false)).toBe('badge-error');
    });

    it('should return badge-info for LOGIN/AUTH actions', () => {
      expect(service.getActionBadgeClass('LOGIN_SUCCESS', true)).toBe('badge-info');
      expect(service.getActionBadgeClass('AUTH_FAILURE', true)).toBe('badge-info');
    });

    it('should return badge-success for CREATE/ENREGISTR', () => {
      expect(service.getActionBadgeClass('PATIENT_CREATED', true)).toBe('badge-success');
      expect(service.getActionBadgeClass('CONSULTATION_ENREGISTREE', true)).toBe('badge-success');
    });

    it('should return badge-warning for UPDATE/CHANGE', () => {
      expect(service.getActionBadgeClass('PROFILE_UPDATED', true)).toBe('badge-warning');
      expect(service.getActionBadgeClass('PASSWORD_CHANGED', true)).toBe('badge-warning');
    });

    it('should return badge-error for DELETE/REMOVE', () => {
      expect(service.getActionBadgeClass('RECORD_DELETED', true)).toBe('badge-error');
      expect(service.getActionBadgeClass('ITEM_REMOVED', true)).toBe('badge-error');
    });

    it('should return badge-warning for SENSITIVE', () => {
      expect(service.getActionBadgeClass('SENSITIVE_DATA_ACCESS', true)).toBe('badge-warning');
    });

    it('should return badge-default for unknown actions', () => {
      expect(service.getActionBadgeClass('UNKNOWN_ACTION', true)).toBe('badge-default');
    });
  });
});
