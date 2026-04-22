import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import { FactureAssuranceService } from './facture-assurance.service';
import { createHttpClientMock, paramsFromCall } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('FactureAssuranceService', () => {
  let service: FactureAssuranceService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/factures-assurance`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new FactureAssuranceService(httpMock.http);
  });

  it('getFactures without filter uses empty params', async () => {
    await firstValueFrom(service.getFactures());
    expect(httpMock.get).toHaveBeenCalled();
    const params = paramsFromCall(httpMock.get.mock.calls[0]);
    expect(params).toBeInstanceOf(HttpParams);
    expect(params?.keys().length).toBe(0);
  });

  it('getFactures with filter sets only provided params', async () => {
    await firstValueFrom(
      service.getFactures({ idAssurance: 3, statut: 'payee', typeFacture: 'consultation', recherche: 'abc', limit: 50 })
    );
    const params = paramsFromCall(httpMock.get.mock.calls[0]);
    expect(params?.get('idAssurance')).toBe('3');
    expect(params?.get('statut')).toBe('payee');
    expect(params?.get('typeFacture')).toBe('consultation');
    expect(params?.get('recherche')).toBe('abc');
    expect(params?.get('limit')).toBe('50');
    expect(params?.get('dateDebut')).toBeNull();
  });

  it('getFactures with dateDebut and dateFin passes them through', async () => {
    await firstValueFrom(service.getFactures({ dateDebut: '2026-01-01', dateFin: '2026-01-31' }));
    const params = paramsFromCall(httpMock.get.mock.calls[0]);
    expect(params?.get('dateDebut')).toBe('2026-01-01');
    expect(params?.get('dateFin')).toBe('2026-01-31');
  });

  it('getFacture GETs /:id', async () => {
    await firstValueFrom(service.getFacture(7));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/7`);
  });

  it('getStatistiques GETs /stats', async () => {
    await firstValueFrom(service.getStatistiques());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/stats`);
  });

  it('envoyerFacture POSTs /:id/envoyer with empty body', async () => {
    await firstValueFrom(service.envoyerFacture(5));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/5/envoyer`, {});
  });

  it('envoyerLot POSTs /envoyer-lot with ids', async () => {
    await firstValueFrom(service.envoyerLot([1, 2, 3]));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/envoyer-lot`, { factureIds: [1, 2, 3] });
  });

  it('updateStatut PUTs /:id/statut', async () => {
    const req = { statut: 'payee', notes: 'OK' };
    await firstValueFrom(service.updateStatut(9, req));
    expect(httpMock.put).toHaveBeenCalledWith(`${base}/9/statut`, req);
  });

  it('telechargerPdf GETs /:id/pdf with responseType blob', async () => {
    await firstValueFrom(service.telechargerPdf(4));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/4/pdf`, { responseType: 'blob' });
  });

  describe('label helpers', () => {
    it('getStatutLabel returns known label', () => {
      expect(service.getStatutLabel('payee')).toBe('Payée');
      expect(service.getStatutLabel('en_attente')).toBe('En attente');
    });

    it('getStatutLabel falls back to raw value for unknown', () => {
      expect(service.getStatutLabel('xxx')).toBe('xxx');
    });

    it('getStatutColor returns known color', () => {
      expect(service.getStatutColor('payee')).toBe('success');
      expect(service.getStatutColor('rejetee')).toBe('danger');
    });

    it('getStatutColor falls back to secondary', () => {
      expect(service.getStatutColor('unknown')).toBe('secondary');
    });

    it('getTypeLabel returns known label', () => {
      expect(service.getTypeLabel('consultation')).toBe('Consultation');
      expect(service.getTypeLabel('pharmacie')).toBe('Pharmacie');
    });

    it('getTypeLabel falls back to raw value', () => {
      expect(service.getTypeLabel('xxx')).toBe('xxx');
    });
  });
});
