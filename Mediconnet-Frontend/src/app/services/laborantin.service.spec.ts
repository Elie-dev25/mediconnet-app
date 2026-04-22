import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import { LaborantinService } from './laborantin.service';
import { createHttpClientMock, paramsFromCall } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('LaborantinService', () => {
  let service: LaborantinService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/laborantin`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new LaborantinService(httpMock.http);
  });

  it('getStats GETs /stats', async () => {
    await firstValueFrom(service.getStats());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/stats`);
  });

  it('getExamens without filters sends empty params', async () => {
    await firstValueFrom(service.getExamens());
    const params = paramsFromCall(httpMock.get.mock.calls[0]);
    expect(params).toBeInstanceOf(HttpParams);
    expect(params?.keys().length).toBe(0);
  });

  it('getExamens sets all provided filters incl. urgence=false', async () => {
    await firstValueFrom(
      service.getExamens({ statut: 'en_cours', urgence: false, recherche: 'abc', dateDebut: '2026-01-01', dateFin: '2026-01-31', page: 2, pageSize: 50 })
    );
    const params = paramsFromCall(httpMock.get.mock.calls[0]);
    expect(params?.get('statut')).toBe('en_cours');
    expect(params?.get('urgence')).toBe('false');
    expect(params?.get('recherche')).toBe('abc');
    expect(params?.get('dateDebut')).toBe('2026-01-01');
    expect(params?.get('dateFin')).toBe('2026-01-31');
    expect(params?.get('page')).toBe('2');
    expect(params?.get('pageSize')).toBe('50');
  });

  it('getExamensEnAttente uses default limit of 10', async () => {
    await firstValueFrom(service.getExamensEnAttente());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/examens/en-attente`, { params: { limit: '10' } });
  });

  it('getExamensEnAttente uses provided limit', async () => {
    await firstValueFrom(service.getExamensEnAttente(25));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/examens/en-attente`, { params: { limit: '25' } });
  });

  it('getExamenDetails GETs /examens/:id', async () => {
    await firstValueFrom(service.getExamenDetails(9));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/examens/9`);
  });

  it('demarrerExamen POSTs /examens/:id/demarrer with empty body', async () => {
    await firstValueFrom(service.demarrerExamen(3));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/examens/3/demarrer`, {});
  });

  it('enregistrerResultat POSTs /examens/:id/resultat with texte and commentaire', async () => {
    await firstValueFrom(service.enregistrerResultat(5, 'result', 'note'));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/examens/5/resultat`, {
      resultatTexte: 'result',
      commentaire: 'note',
    });
  });

  it('enregistrerResultat works without commentaire', async () => {
    await firstValueFrom(service.enregistrerResultat(5, 'result'));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/examens/5/resultat`, {
      resultatTexte: 'result',
      commentaire: undefined,
    });
  });

  it('enregistrerResultatComplet POSTs FormData', async () => {
    const file = new File(['x'], 'a.pdf', { type: 'application/pdf' });
    await firstValueFrom(service.enregistrerResultatComplet(7, 'texte', 'com', [file]));
    const call = httpMock.post.mock.calls[0];
    expect(call[0]).toBe(`${base}/examens/7/resultat-complet`);
    expect(call[1]).toBeInstanceOf(FormData);
    const form = call[1] as FormData;
    expect(form.get('resultatTexte')).toBe('texte');
    expect(form.get('commentaire')).toBe('com');
    expect(form.getAll('fichiers')).toHaveLength(1);
  });

  it('enregistrerResultatComplet omits commentaire when null', async () => {
    await firstValueFrom(service.enregistrerResultatComplet(7, 'texte', null, []));
    const form = httpMock.post.mock.calls[0][1] as FormData;
    expect(form.get('commentaire')).toBeNull();
    expect(form.get('resultatTexte')).toBe('texte');
  });

  it('getLaboratoires GETs /laboratoires', async () => {
    await firstValueFrom(service.getLaboratoires());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/laboratoires`);
  });

  it('downloadDocument GETs with responseType blob', async () => {
    await firstValueFrom(service.downloadDocument('uuid-x'));
    expect(httpMock.get).toHaveBeenCalledWith(`${environment.apiUrl}/api/documents/uuid-x/download`, {
      responseType: 'blob',
    });
  });
});
