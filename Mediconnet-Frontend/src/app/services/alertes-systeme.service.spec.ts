import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import { AlertesSystemeService } from './alertes-systeme.service';
import { createHttpClientMock, paramsFromCall } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('AlertesSystemeService', () => {
  let service: AlertesSystemeService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/alertes`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new AlertesSystemeService(httpMock.http);
  });

  describe('HTTP', () => {
    it('getAlertesActives GETs /actives', async () => {
      await firstValueFrom(service.getAlertesActives());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/actives`);
    });

    it('getAlertes with no filter sends empty params', async () => {
      await firstValueFrom(service.getAlertes());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params).toBeInstanceOf(HttpParams);
      expect(params?.keys().length).toBe(0);
    });

    it('getAlertes sets all filter params including acquittee=false', async () => {
      const df = new Date('2026-01-01T00:00:00Z');
      const dt = new Date('2026-01-31T00:00:00Z');
      await firstValueFrom(
        service.getAlertes({ page: 2, pageSize: 50, type: 'disk_space', severite: 'warning', acquittee: false, dateFrom: df, dateTo: dt })
      );
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('page')).toBe('2');
      expect(params?.get('pageSize')).toBe('50');
      expect(params?.get('type')).toBe('disk_space');
      expect(params?.get('severite')).toBe('warning');
      expect(params?.get('acquittee')).toBe('false');
      expect(params?.get('dateFrom')).toBe(df.toISOString());
      expect(params?.get('dateTo')).toBe(dt.toISOString());
    });

    it('acquitterAlerte POSTs /:id/acquitter with empty body', async () => {
      await firstValueFrom(service.acquitterAlerte(4));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/4/acquitter`, {});
    });

    it('creerAlerte POSTs base with alerte', async () => {
      const payload = { message: 'test', severite: 'info' as const };
      await firstValueFrom(service.creerAlerte(payload));
      expect(httpMock.post).toHaveBeenCalledWith(base, payload);
    });

    it('getStats GETs /stats', async () => {
      await firstValueFrom(service.getStats());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/stats`);
    });

    it('getStorageHealth GETs /documents/disk-space', async () => {
      await firstValueFrom(service.getStorageHealth());
      expect(httpMock.get).toHaveBeenCalledWith(`${environment.apiUrl}/documents/disk-space`);
    });
  });

  describe('UI helpers', () => {
    it('getAlerteIcon returns icons for all known types', () => {
      expect(service.getAlerteIcon('storage_health')).toBe('hard-drive');
      expect(service.getAlerteIcon('disk_space')).toBe('database');
      expect(service.getAlerteIcon('corruption')).toBe('file-warning');
      expect(service.getAlerteIcon('access_denied')).toBe('shield-x');
      expect(service.getAlerteIcon('suspicious_activity')).toBe('user-x');
      expect(service.getAlerteIcon('backup_failed')).toBe('cloud-off');
      expect(service.getAlerteIcon('system_error')).toBe('alert-octagon');
    });

    it('getAlerteIcon falls back to alert-circle', () => {
      expect(service.getAlerteIcon('unknown' as never)).toBe('alert-circle');
    });

    it('getSeveriteClass returns class per severite', () => {
      expect(service.getSeveriteClass('info')).toBe('badge-info');
      expect(service.getSeveriteClass('warning')).toBe('badge-warning');
      expect(service.getSeveriteClass('critical')).toBe('badge-error');
      expect(service.getSeveriteClass('emergency')).toBe('badge-emergency');
    });

    it('getSeveriteClass falls back to badge-default', () => {
      expect(service.getSeveriteClass('x' as never)).toBe('badge-default');
    });

    it('getTypeLabel returns french label', () => {
      expect(service.getTypeLabel('storage_health')).toBe('Santé stockage');
      expect(service.getTypeLabel('backup_failed')).toBe('Échec sauvegarde');
    });

    it('getTypeLabel falls back to raw value', () => {
      expect(service.getTypeLabel('x' as never)).toBe('x');
    });

    it('getSeveriteLabel returns french label', () => {
      expect(service.getSeveriteLabel('info')).toBe('Information');
      expect(service.getSeveriteLabel('emergency')).toBe('Urgence');
    });

    it('getSeveriteLabel falls back to raw value', () => {
      expect(service.getSeveriteLabel('x' as never)).toBe('x');
    });
  });
});
