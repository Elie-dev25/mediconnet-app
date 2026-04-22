import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { AssuranceService } from './assurance.service';
import { createHttpClientMock, paramsFromCall } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('AssuranceService', () => {
  let service: AssuranceService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/assurance`;
  const covBase = `${environment.apiUrl}/assurances`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new AssuranceService(httpMock.http);
  });

  describe('assurances', () => {
    it('getAssurances without filter', async () => {
      await firstValueFrom(service.getAssurances());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.keys().length).toBe(0);
    });
    it('getAssurances with all filters', async () => {
      await firstValueFrom(
        service.getAssurances({
          typeAssurance: 'privee',
          zoneCouverture: 'national',
          isActive: false,
          recherche: 'abc',
          page: 2,
          pageSize: 20,
        })
      );
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('typeAssurance')).toBe('privee');
      expect(params?.get('zoneCouverture')).toBe('national');
      expect(params?.get('isActive')).toBe('false');
      expect(params?.get('recherche')).toBe('abc');
      expect(params?.get('page')).toBe('2');
      expect(params?.get('pageSize')).toBe('20');
    });
    it('getAssurancesActives', async () => {
      await firstValueFrom(service.getAssurancesActives());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/actives`);
    });
    it('getAssuranceById', async () => {
      await firstValueFrom(service.getAssuranceById(3));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/3`);
    });
    it('createAssurance', async () => {
      const req = { nom: 'A', typeAssurance: 'privee' };
      await firstValueFrom(service.createAssurance(req));
      expect(httpMock.post).toHaveBeenCalledWith(base, req);
    });
    it('updateAssurance', async () => {
      const req = { nom: 'A', typeAssurance: 'privee' };
      await firstValueFrom(service.updateAssurance(5, req));
      expect(httpMock.put).toHaveBeenCalledWith(`${base}/5`, req);
    });
    it('toggleAssuranceStatus', async () => {
      await firstValueFrom(service.toggleAssuranceStatus(5));
      expect(httpMock.patch).toHaveBeenCalledWith(`${base}/5/toggle-status`, {});
    });
    it('deleteAssurance', async () => {
      await firstValueFrom(service.deleteAssurance(5));
      expect(httpMock.delete).toHaveBeenCalledWith(`${base}/5`);
    });
  });

  describe('patient assurance', () => {
    it('getPatientAssurance', async () => {
      await firstValueFrom(service.getPatientAssurance(7));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/patient/7`);
    });
    it('updatePatientAssurance', async () => {
      const req = { assuranceId: 3 };
      await firstValueFrom(service.updatePatientAssurance(7, req));
      expect(httpMock.put).toHaveBeenCalledWith(`${base}/patient/7`, req);
    });
    it('removePatientAssurance', async () => {
      await firstValueFrom(service.removePatientAssurance(7));
      expect(httpMock.delete).toHaveBeenCalledWith(`${base}/patient/7`);
    });
  });

  describe('couvertures', () => {
    it('getAssurancesAvecCouvertures', async () => {
      await firstValueFrom(service.getAssurancesAvecCouvertures());
      expect(httpMock.get).toHaveBeenCalledWith(`${covBase}/avec-couvertures`);
    });
    it('getCouvertures', async () => {
      await firstValueFrom(service.getCouvertures(3));
      expect(httpMock.get).toHaveBeenCalledWith(`${covBase}/3/couvertures`);
    });
    it('upsertCouverture', async () => {
      const req = { typePrestation: 'consultation', tauxCouverture: 80 };
      await firstValueFrom(service.upsertCouverture(3, req));
      expect(httpMock.put).toHaveBeenCalledWith(`${covBase}/3/couvertures`, req);
    });
    it('batchUpdateCouvertures', async () => {
      const reqs = [{ typePrestation: 'consultation', tauxCouverture: 80 }];
      await firstValueFrom(service.batchUpdateCouvertures(3, reqs));
      expect(httpMock.put).toHaveBeenCalledWith(`${covBase}/3/couvertures/batch`, reqs);
    });
    it('deleteCouverture', async () => {
      await firstValueFrom(service.deleteCouverture(9));
      expect(httpMock.delete).toHaveBeenCalledWith(`${covBase}/couvertures/9`);
    });
  });

  describe('patient status', () => {
    it('getPatientsInsuranceStatus without filter', async () => {
      await firstValueFrom(service.getPatientsInsuranceStatus());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.keys().length).toBe(0);
    });
    it('getPatientsInsuranceStatus with all filters', async () => {
      await firstValueFrom(
        service.getPatientsInsuranceStatus({
          statutAssurance: 'valide',
          assuranceId: 1,
          recherche: 'x',
          joursAvertissement: 30,
          page: 1,
          pageSize: 50,
        })
      );
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('statutAssurance')).toBe('valide');
      expect(params?.get('assuranceId')).toBe('1');
      expect(params?.get('recherche')).toBe('x');
      expect(params?.get('joursAvertissement')).toBe('30');
      expect(params?.get('page')).toBe('1');
      expect(params?.get('pageSize')).toBe('50');
    });
  });
});
