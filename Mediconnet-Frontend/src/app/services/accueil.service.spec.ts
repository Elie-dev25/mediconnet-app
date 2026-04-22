import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { AccueilService } from './accueil.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('AccueilService', () => {
  let service: AccueilService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/accueil`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new AccueilService(httpMock.http);
  });

  it('getProfile GETs /profile', async () => {
    await firstValueFrom(service.getProfile());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/profile`);
  });

  it('getDashboard GETs /dashboard', async () => {
    await firstValueFrom(service.getDashboard());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/dashboard`);
  });

  it('rechercherPatient without filters calls endpoint with empty params', async () => {
    await firstValueFrom(service.rechercherPatient());
    const call = httpMock.get.mock.calls[0];
    expect(call[0]).toBe(`${base}/patients/recherche`);
    const opts = call[1] as { params: { terme?: string } };
    expect(opts.params).toEqual({});
  });

  it('rechercherPatient passes terme when provided', async () => {
    await firstValueFrom(service.rechercherPatient('dupont'));
    const opts = httpMock.get.mock.calls[0][1] as { params: { terme?: string; numeroDossier?: string; telephone?: string } };
    expect(opts.params.terme).toBe('dupont');
    expect(opts.params.numeroDossier).toBeUndefined();
    expect(opts.params.telephone).toBeUndefined();
  });

  it('rechercherPatient passes all three filter params when provided', async () => {
    await firstValueFrom(service.rechercherPatient('a', 'D001', '612345678'));
    const opts = httpMock.get.mock.calls[0][1] as { params: { terme: string; numeroDossier: string; telephone: string } };
    expect(opts.params.terme).toBe('a');
    expect(opts.params.numeroDossier).toBe('D001');
    expect(opts.params.telephone).toBe('612345678');
  });

  it('enregistrerArrivee POSTs /patients/arrivee with data', async () => {
    const data = { nom: 'X', prenom: 'Y' };
    await firstValueFrom(service.enregistrerArrivee(data));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/patients/arrivee`, data);
  });

  it('getRdvAujourdHui GETs /rdv/aujourdhui', async () => {
    await firstValueFrom(service.getRdvAujourdHui());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/rdv/aujourdhui`);
  });

  it('marquerArriveeRdv POSTs /rdv/marquer-arrivee with id', async () => {
    await firstValueFrom(service.marquerArriveeRdv(42));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/rdv/marquer-arrivee`, { idRendezVous: 42 });
  });
});
