import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { PatientService } from './patient.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('PatientService', () => {
  let service: PatientService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/patient`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new PatientService(httpMock.http);
  });

  it('getProfile GETs /profile', async () => {
    await firstValueFrom(service.getProfile());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/profile`);
  });

  it('updateProfile PUTs /profile', async () => {
    await firstValueFrom(service.updateProfile({ telephone: '600' }));
    expect(httpMock.put).toHaveBeenCalledWith(`${base}/profile`, { telephone: '600' });
  });

  it('getDashboard GETs /dashboard', async () => {
    await firstValueFrom(service.getDashboard());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/dashboard`);
  });

  it('getRecentPatients uses default count=6', async () => {
    await firstValueFrom(service.getRecentPatients());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/recent?count=6`);
  });

  it('getRecentPatients uses custom count', async () => {
    await firstValueFrom(service.getRecentPatients(12));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/recent?count=12`);
  });

  it('searchPatients POSTs /search with request', async () => {
    const req = { searchTerm: 'dup', limit: 10 };
    await firstValueFrom(service.searchPatients(req));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/search`, req);
  });

  it('getDossierMedical GETs /dossier-medical', async () => {
    await firstValueFrom(service.getDossierMedical());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/dossier-medical`);
  });

  it('getPatientById GETs /:id', async () => {
    await firstValueFrom(service.getPatientById(42));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/42`);
  });
});
