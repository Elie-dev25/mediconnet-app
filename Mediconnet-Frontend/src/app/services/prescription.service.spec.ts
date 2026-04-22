import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import { PrescriptionService } from './prescription.service';
import { createHttpClientMock, paramsFromCall } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('PrescriptionService', () => {
  let service: PrescriptionService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/prescription`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new PrescriptionService(httpMock.http);
  });

  describe('creation', () => {
    it('creerOrdonnance POSTs base', async () => {
      const req = { idPatient: 1, medicaments: [] };
      await firstValueFrom(service.creerOrdonnance(req));
      expect(httpMock.post).toHaveBeenCalledWith(base, req);
    });
    it('creerOrdonnanceConsultation POSTs /consultation/:id', async () => {
      const req = { medicaments: [] };
      await firstValueFrom(service.creerOrdonnanceConsultation(3, req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/consultation/3`, req);
    });
    it('creerOrdonnanceHospitalisation POSTs /hospitalisation/:id', async () => {
      const req = { medicaments: [] };
      await firstValueFrom(service.creerOrdonnanceHospitalisation(5, req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/hospitalisation/5`, req);
    });
    it('creerOrdonnanceDirecte POSTs /directe', async () => {
      const req = { idPatient: 1, medicaments: [] };
      await firstValueFrom(service.creerOrdonnanceDirecte(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/directe`, req);
    });
  });

  describe('lecture', () => {
    it('getOrdonnance', async () => {
      await firstValueFrom(service.getOrdonnance(7));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/7`);
    });
    it('getOrdonnanceByConsultation', async () => {
      await firstValueFrom(service.getOrdonnanceByConsultation(3));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/consultation/3`);
    });
    it('getOrdonnancesPatient', async () => {
      await firstValueFrom(service.getOrdonnancesPatient(8));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/patient/8`);
    });
    it('getOrdonnancesHospitalisation', async () => {
      await firstValueFrom(service.getOrdonnancesHospitalisation(4));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/hospitalisation/4`);
    });
  });

  describe('rechercherOrdonnances', () => {
    it('no filter sends empty params', async () => {
      await firstValueFrom(service.rechercherOrdonnances({}));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params).toBeInstanceOf(HttpParams);
      expect(params?.keys().length).toBe(0);
    });
    it('all filters set', async () => {
      const df = new Date('2026-01-01T00:00:00Z');
      const dt = new Date('2026-01-31T00:00:00Z');
      await firstValueFrom(
        service.rechercherOrdonnances({
          idPatient: 1,
          idMedecin: 2,
          idConsultation: 3,
          idHospitalisation: 4,
          statut: 'active',
          typeContexte: 'consultation',
          dateDebut: df,
          dateFin: dt,
          page: 2,
          pageSize: 50,
        })
      );
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('idPatient')).toBe('1');
      expect(params?.get('idMedecin')).toBe('2');
      expect(params?.get('idConsultation')).toBe('3');
      expect(params?.get('idHospitalisation')).toBe('4');
      expect(params?.get('statut')).toBe('active');
      expect(params?.get('typeContexte')).toBe('consultation');
      expect(params?.get('dateDebut')).toBe(df.toISOString());
      expect(params?.get('dateFin')).toBe(dt.toISOString());
      expect(params?.get('page')).toBe('2');
      expect(params?.get('pageSize')).toBe('50');
    });
  });

  describe('modifications', () => {
    it('mettreAJourOrdonnance PUTs /:id', async () => {
      const req = { medicaments: [] };
      await firstValueFrom(service.mettreAJourOrdonnance(5, req));
      expect(httpMock.put).toHaveBeenCalledWith(`${base}/5`, req);
    });
    it('annulerOrdonnance POSTs /:id/annuler with motif', async () => {
      await firstValueFrom(service.annulerOrdonnance(5, 'raison'));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/5/annuler`, { motif: 'raison' });
    });
  });

  it('validerPrescription POSTs /valider', async () => {
    const req = { idPatient: 1, medicaments: [] };
    await firstValueFrom(service.validerPrescription(req));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/valider`, req);
  });

  describe('helpers', () => {
    it('getTypeContexteLabel', () => {
      expect(service.getTypeContexteLabel('consultation')).toBe('Consultation');
      expect(service.getTypeContexteLabel('hospitalisation')).toBe('Hospitalisation');
      expect(service.getTypeContexteLabel('directe')).toBe('Prescription directe');
      expect(service.getTypeContexteLabel('x')).toBe('x');
    });
    it('getStatutLabel', () => {
      expect(service.getStatutLabel('active')).toBe('Active');
      expect(service.getStatutLabel('dispensee')).toBe('Dispensée');
      expect(service.getStatutLabel('partielle')).toBe('Partiellement dispensée');
      expect(service.getStatutLabel('annulee')).toBe('Annulée');
      expect(service.getStatutLabel('expiree')).toBe('Expirée');
      expect(service.getStatutLabel('x')).toBe('x');
    });
    it('getStatutClass', () => {
      expect(service.getStatutClass('active')).toBe('status-active');
      expect(service.getStatutClass('dispensee')).toBe('status-success');
      expect(service.getStatutClass('partielle')).toBe('status-warning');
      expect(service.getStatutClass('annulee')).toBe('status-danger');
      expect(service.getStatutClass('expiree')).toBe('status-muted');
      expect(service.getStatutClass('x')).toBe('');
    });
    it('getAlerteSeveriteClass', () => {
      expect(service.getAlerteSeveriteClass('info')).toBe('alert-info');
      expect(service.getAlerteSeveriteClass('warning')).toBe('alert-warning');
      expect(service.getAlerteSeveriteClass('error')).toBe('alert-danger');
      expect(service.getAlerteSeveriteClass('x')).toBe('');
    });
    it('getAlerteSeveriteIcon', () => {
      expect(service.getAlerteSeveriteIcon('info')).toBe('info');
      expect(service.getAlerteSeveriteIcon('warning')).toBe('alert-triangle');
      expect(service.getAlerteSeveriteIcon('error')).toBe('alert-circle');
      expect(service.getAlerteSeveriteIcon('x')).toBe('info');
    });
  });
});
