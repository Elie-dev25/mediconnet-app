import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import { OrdonnancesPatientService } from './ordonnances-patient.service';
import { createHttpClientMock, paramsFromCall } from '../../test-helpers';

describe('OrdonnancesPatientService', () => {
  let service: OrdonnancesPatientService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = '/api/patient';

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new OrdonnancesPatientService(httpMock.http);
  });

  describe('HTTP', () => {
    it('getDossierPharmaceutique without filter sends empty params', async () => {
      await firstValueFrom(service.getDossierPharmaceutique());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params).toBeInstanceOf(HttpParams);
      expect(params?.keys().length).toBe(0);
    });

    it('getDossierPharmaceutique with all filters', async () => {
      await firstValueFrom(
        service.getDossierPharmaceutique({
          statut: 'active',
          typeContexte: 'consultation',
          dateDebut: '2026-01-01',
          dateFin: '2026-01-31',
          idMedecin: 5,
          tri: 'date_desc',
          page: 2,
          pageSize: 25,
        })
      );
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('statut')).toBe('active');
      expect(params?.get('typeContexte')).toBe('consultation');
      expect(params?.get('dateDebut')).toBe('2026-01-01');
      expect(params?.get('dateFin')).toBe('2026-01-31');
      expect(params?.get('idMedecin')).toBe('5');
      expect(params?.get('tri')).toBe('date_desc');
      expect(params?.get('page')).toBe('2');
      expect(params?.get('pageSize')).toBe('25');
    });

    it('getOrdonnancePatient GETs /ordonnances/:id', async () => {
      await firstValueFrom(service.getOrdonnancePatient(11));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/ordonnances/11`);
    });
  });

  describe('label helpers', () => {
    it('getStatutDelivranceLabel maps known statuses', () => {
      expect(service.getStatutDelivranceLabel('non_delivre')).toBe('Non délivré');
      expect(service.getStatutDelivranceLabel('en_attente')).toBe('En attente');
      expect(service.getStatutDelivranceLabel('partiel')).toBe('Délivré partiellement');
      expect(service.getStatutDelivranceLabel('delivre')).toBe('Délivré');
      expect(service.getStatutDelivranceLabel('xxx')).toBe('xxx');
    });

    it('getStatutDelivranceClass maps known statuses', () => {
      expect(service.getStatutDelivranceClass('non_delivre')).toBe('badge-secondary');
      expect(service.getStatutDelivranceClass('en_attente')).toBe('badge-warning');
      expect(service.getStatutDelivranceClass('partiel')).toBe('badge-info');
      expect(service.getStatutDelivranceClass('delivre')).toBe('badge-success');
      expect(service.getStatutDelivranceClass('xxx')).toBe('badge-secondary');
    });

    it('getTypeContexteLabel maps known types', () => {
      expect(service.getTypeContexteLabel('consultation')).toBe('Consultation');
      expect(service.getTypeContexteLabel('hospitalisation')).toBe('Hospitalisation');
      expect(service.getTypeContexteLabel('directe')).toBe('Prescription directe');
      expect(service.getTypeContexteLabel('xxx')).toBe('xxx');
    });

    it('getTypeContexteClass maps known types', () => {
      expect(service.getTypeContexteClass('consultation')).toBe('badge-primary');
      expect(service.getTypeContexteClass('hospitalisation')).toBe('badge-danger');
      expect(service.getTypeContexteClass('directe')).toBe('badge-info');
      expect(service.getTypeContexteClass('xxx')).toBe('badge-secondary');
    });

    it('getStatutLabel maps known statuses', () => {
      expect(service.getStatutLabel('active')).toBe('Active');
      expect(service.getStatutLabel('dispensee')).toBe('Délivrée');
      expect(service.getStatutLabel('partielle')).toBe('Partielle');
      expect(service.getStatutLabel('annulee')).toBe('Annulée');
      expect(service.getStatutLabel('expiree')).toBe('Expirée');
      expect(service.getStatutLabel('xxx')).toBe('xxx');
    });

    it('getStatutClass maps known statuses', () => {
      expect(service.getStatutClass('active')).toBe('badge-success');
      expect(service.getStatutClass('dispensee')).toBe('badge-primary');
      expect(service.getStatutClass('partielle')).toBe('badge-warning');
      expect(service.getStatutClass('annulee')).toBe('badge-danger');
      expect(service.getStatutClass('expiree')).toBe('badge-secondary');
      expect(service.getStatutClass('xxx')).toBe('badge-secondary');
    });
  });

  describe('formatters', () => {
    it('formatDate returns empty string for empty input', () => {
      expect(service.formatDate('')).toBe('');
    });
    it('formatDate formats ISO date', () => {
      expect(service.formatDate('2026-03-18')).toMatch(/\d{2}\/\d{2}\/\d{4}/);
    });
    it('formatDateTime returns empty string for empty input', () => {
      expect(service.formatDateTime('')).toBe('');
    });
    it('formatDateTime includes time', () => {
      expect(service.formatDateTime('2026-03-18T14:30:00')).toMatch(/\d{2}:\d{2}/);
    });
  });
});
