import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { MedecinDataService } from './medecin-data.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('MedecinDataService', () => {
  let service: MedecinDataService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/medecin`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new MedecinDataService(httpMock.http);
  });

  it('getConsultationStats', async () => {
    await firstValueFrom(service.getConsultationStats());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/consultations/stats`);
  });

  it('getConsultations no filter', async () => {
    await firstValueFrom(service.getConsultations());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/consultations`, { params: {} });
  });

  it('getConsultations with all filters', async () => {
    await firstValueFrom(service.getConsultations('2026-01-01', '2026-01-31', 'terminee'));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/consultations`, {
      params: { dateDebut: '2026-01-01', dateFin: '2026-01-31', statut: 'terminee' },
    });
  });

  it('getConsultationsJour no filter', async () => {
    await firstValueFrom(service.getConsultationsJour());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/consultations/jour`, { params: {} });
  });

  it('getConsultationsJour with date', async () => {
    await firstValueFrom(service.getConsultationsJour('2026-01-15'));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/consultations/jour`, { params: { date: '2026-01-15' } });
  });

  it('getPatientStats', async () => {
    await firstValueFrom(service.getPatientStats());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/patients/stats`);
  });

  it('getPatients with defaults', async () => {
    await firstValueFrom(service.getPatients());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/patients`, { params: { page: 1, pageSize: 50 } });
  });

  it('getPatients with recherche + custom page', async () => {
    await firstValueFrom(service.getPatients('dup', 2, 25));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/patients`, {
      params: { page: 2, pageSize: 25, recherche: 'dup' },
    });
  });

  it('getPatientDetail', async () => {
    await firstValueFrom(service.getPatientDetail(7));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/patients/7`);
  });

  it('getPatientsHospitalises', async () => {
    await firstValueFrom(service.getPatientsHospitalises());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/patients/hospitalises`);
  });

  it('getHospitalisationDetail', async () => {
    await firstValueFrom(service.getHospitalisationDetail(5));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/hospitalisation/5`);
  });

  it('getStandardsForHospitalisation', async () => {
    await firstValueFrom(service.getStandardsForHospitalisation());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/hospitalisation/standards`);
  });

  it('getChambresDisponiblesByStandard', async () => {
    await firstValueFrom(service.getChambresDisponiblesByStandard(3));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/hospitalisation/chambres/3`);
  });

  it('createHospitalisation', async () => {
    const req = { idPatient: 1, idLit: 5 };
    await firstValueFrom(service.createHospitalisation(req));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/hospitalisation`, req);
  });

  it('ajouterSoin', async () => {
    const req = { typeSoin: 'pansement', description: 'x', priorite: 'normale' };
    await firstValueFrom(service.ajouterSoin(5, req));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/hospitalisation/5/soins`, req);
  });

  it('getSoinsHospitalisation', async () => {
    await firstValueFrom(service.getSoinsHospitalisation(5));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/hospitalisation/5/soins`);
  });

  describe('helpers', () => {
    it('formatDate returns "" for empty', () => {
      expect(service.formatDate('')).toBe('');
    });
    it('formatDate formats', () => {
      expect(service.formatDate('2026-01-15')).toMatch(/2026/);
    });
    it('formatTime returns "" for empty', () => {
      expect(service.formatTime('')).toBe('');
    });
    it('formatTime formats', () => {
      expect(service.formatTime('2026-01-15T14:30:00')).toMatch(/\d{2}:\d{2}/);
    });
    it('formatDateTime returns "" for empty', () => {
      expect(service.formatDateTime('')).toBe('');
    });
    it('formatDateTime formats', () => {
      expect(service.formatDateTime('2026-01-15T14:30:00')).toMatch(/\d/);
    });
  });
});
