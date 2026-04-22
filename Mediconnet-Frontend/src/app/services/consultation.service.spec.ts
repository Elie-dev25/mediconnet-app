import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { ConsultationService } from './consultation.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('ConsultationService', () => {
  let service: ConsultationService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/accueil`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new ConsultationService(httpMock.http);
  });

  it('enregistrerConsultation POSTs /consultations/enregistrer', async () => {
    const req = { idPatient: 1, motif: 'x', idMedecin: 2, prixConsultation: 5000 };
    await firstValueFrom(service.enregistrerConsultation(req));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/consultations/enregistrer`, req);
  });

  it('getMedecinsDisponibles GETs /medecins/disponibles', async () => {
    await firstValueFrom(service.getMedecinsDisponibles());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/medecins/disponibles`);
  });

  it('getMedecinsFiltres with no filter sends empty params', async () => {
    await firstValueFrom(service.getMedecinsFiltres());
    const opts = httpMock.get.mock.calls[0][1] as { params: Record<string, unknown> };
    expect(opts.params).toEqual({});
  });

  it('getMedecinsFiltres with idService and idSpecialite', async () => {
    await firstValueFrom(service.getMedecinsFiltres(3, 5));
    const opts = httpMock.get.mock.calls[0][1] as { params: { idService: number; idSpecialite: number } };
    expect(opts.params.idService).toBe(3);
    expect(opts.params.idSpecialite).toBe(5);
  });

  it('getServices GETs /services', async () => {
    await firstValueFrom(service.getServices());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/services`);
  });

  it('getSpecialites GETs /specialites', async () => {
    await firstValueFrom(service.getSpecialites());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/specialites`);
  });

  it('getMedecinsAvecDisponibilite applies filters', async () => {
    await firstValueFrom(service.getMedecinsAvecDisponibilite(3));
    const opts = httpMock.get.mock.calls[0][1] as { params: Record<string, unknown> };
    expect(opts.params).toEqual({ idService: 3 });
  });

  it('verifierPaiementValide GETs /verifier-paiement/:p/:m', async () => {
    await firstValueFrom(service.verifierPaiementValide(1, 2));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/verifier-paiement/1/2`);
  });

  it('getCreneauxMedecinJour GETs /medecins/:id/creneaux-jour', async () => {
    await firstValueFrom(service.getCreneauxMedecinJour(42));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/medecins/42/creneaux-jour`);
  });
});
