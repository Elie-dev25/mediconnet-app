import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { HospitalisationService } from './hospitalisation.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('HospitalisationService', () => {
  let service: HospitalisationService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/hospitalisation`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new HospitalisationService(httpMock.http);
  });

  it('getChambres', async () => {
    await firstValueFrom(service.getChambres());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/chambres`);
  });

  it('getLitsDisponibles', async () => {
    await firstValueFrom(service.getLitsDisponibles());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/lits/disponibles`);
  });

  it('getHospitalisations without filters', async () => {
    await firstValueFrom(service.getHospitalisations());
    const opts = httpMock.get.mock.calls[0][1] as { params: Record<string, unknown> };
    expect(opts.params).toEqual({});
  });

  it('getHospitalisations with all filters', async () => {
    await firstValueFrom(service.getHospitalisations('en_cours', 5, '2026-01-01', '2026-01-31'));
    const opts = httpMock.get.mock.calls[0][1] as { params: Record<string, unknown> };
    expect(opts.params).toEqual({
      statut: 'en_cours',
      idPatient: 5,
      dateDebut: '2026-01-01',
      dateFin: '2026-01-31',
    });
  });

  it('getHospitalisation by id', async () => {
    await firstValueFrom(service.getHospitalisation(3));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/3`);
  });

  it('getHospitalisationsPatient', async () => {
    await firstValueFrom(service.getHospitalisationsPatient(7));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/patient/7`);
  });

  it('ordonnerHospitalisation', async () => {
    const req = { idPatient: 3, motif: 'x' };
    await firstValueFrom(service.ordonnerHospitalisation(req));
    expect(httpMock.post).toHaveBeenCalledWith(`${environment.apiUrl}/medecin/hospitalisation/ordonner`, req);
  });

  it('ordonnerHospitalisationComplete', async () => {
    const req = { idPatient: 3, motif: 'x', urgence: 'normale' };
    await firstValueFrom(service.ordonnerHospitalisationComplete(req));
    expect(httpMock.post).toHaveBeenCalledWith(`${environment.apiUrl}/medecin/hospitalisation/ordonner-complete`, req);
  });

  it('attribuerLit default context=infirmier', async () => {
    const req = { idAdmission: 1, idLit: 5 };
    await firstValueFrom(service.attribuerLit(req));
    expect(httpMock.post).toHaveBeenCalledWith(`${environment.apiUrl}/infirmier/hospitalisations/attribuer-lit`, req);
  });

  it('attribuerLit with context=medecin', async () => {
    const req = { idAdmission: 1, idLit: 5 };
    await firstValueFrom(service.attribuerLit(req, 'medecin'));
    expect(httpMock.post).toHaveBeenCalledWith(`${environment.apiUrl}/medecin/hospitalisation/attribuer-lit`, req);
  });

  it('getHospitalisationsEnAttente', async () => {
    await firstValueFrom(service.getHospitalisationsEnAttente());
    expect(httpMock.get).toHaveBeenCalledWith(`${environment.apiUrl}/infirmier/hospitalisations/en-attente`);
  });

  it('getPatientsHospitalises without search', async () => {
    await firstValueFrom(service.getPatientsHospitalises());
    expect(httpMock.get).toHaveBeenCalledWith(`${environment.apiUrl}/infirmier/patients/hospitalises`);
  });

  it('getPatientsHospitalises with search (URL encoded)', async () => {
    await firstValueFrom(service.getPatientsHospitalises('jean dupont'));
    expect(httpMock.get).toHaveBeenCalledWith(`${environment.apiUrl}/infirmier/patients/hospitalises?search=jean%20dupont`);
  });

  it('terminerHospitalisation', async () => {
    const req = { resumeMedical: 'patient guéri' };
    await firstValueFrom(service.terminerHospitalisation(3, req));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/3/terminer`, req);
  });

  it('getHospitalisationDetails default context=medecin', async () => {
    await firstValueFrom(service.getHospitalisationDetails(5));
    expect(httpMock.get).toHaveBeenCalledWith(`${environment.apiUrl}/medecin/hospitalisation/5/details`);
  });

  it('getHospitalisationDetails with context=infirmier', async () => {
    await firstValueFrom(service.getHospitalisationDetails(5, 'infirmier'));
    expect(httpMock.get).toHaveBeenCalledWith(`${environment.apiUrl}/infirmier/hospitalisation/5/details`);
  });
});
