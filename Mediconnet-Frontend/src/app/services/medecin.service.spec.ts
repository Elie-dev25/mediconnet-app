import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { MedecinService } from './medecin.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('MedecinService', () => {
  let service: MedecinService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/medecin`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new MedecinService(httpMock.http);
  });

  it('getProfile GETs /profile', async () => {
    await firstValueFrom(service.getProfile());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/profile`);
  });

  it('getDashboard GETs /dashboard', async () => {
    await firstValueFrom(service.getDashboard());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/dashboard`);
  });

  it('updateProfile PUTs /profile', async () => {
    const req = { telephone: '600000000' };
    await firstValueFrom(service.updateProfile(req));
    expect(httpMock.put).toHaveBeenCalledWith(`${base}/profile`, req);
  });

  it('getAgenda GETs /agenda with date params', async () => {
    await firstValueFrom(service.getAgenda('2026-01-01', '2026-01-31'));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/agenda`, {
      params: { dateDebut: '2026-01-01', dateFin: '2026-01-31' },
    });
  });

  it('getRdvAujourdHui GETs /rdv/aujourdhui', async () => {
    await firstValueFrom(service.getRdvAujourdHui());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/rdv/aujourdhui`);
  });

  it('getProchainRdv uses default limite of 5', async () => {
    await firstValueFrom(service.getProchainRdv());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/rdv/prochains`, {
      params: { limite: '5' },
    });
  });

  it('getProchainRdv uses provided limite', async () => {
    await firstValueFrom(service.getProchainRdv(10));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/rdv/prochains`, {
      params: { limite: '10' },
    });
  });
});
