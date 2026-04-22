import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { UserService } from './user.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('UserService', () => {
  let service: UserService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/admin`;
  const affBase = `${environment.apiUrl}/affectations-service`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new UserService(httpMock.http);
  });

  it('getUsers', async () => {
    await firstValueFrom(service.getUsers());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/users`);
  });

  it('createUser', async () => {
    const req = { nom: 'x', prenom: 'y', email: 'e', telephone: 't', password: 'p', role: 'medecin' };
    await firstValueFrom(service.createUser(req));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/users`, req);
  });

  it('getSpecialites', async () => {
    await firstValueFrom(service.getSpecialites());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/specialites`);
  });

  it('getSpecialitesInfirmiers uses actives endpoint', async () => {
    await firstValueFrom(service.getSpecialitesInfirmiers());
    expect(httpMock.get).toHaveBeenCalledWith(`${environment.apiUrl}/specialites-infirmiers/actives`);
  });

  it('getServices', async () => {
    await firstValueFrom(service.getServices());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/services`);
  });

  it('getLaboratoires', async () => {
    await firstValueFrom(service.getLaboratoires());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/laboratoires`);
  });

  it('deleteUser', async () => {
    await firstValueFrom(service.deleteUser(7));
    expect(httpMock.delete).toHaveBeenCalledWith(`${base}/users/7`);
  });

  it('getUserDetails', async () => {
    await firstValueFrom(service.getUserDetails(9));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/users/9/details`);
  });

  it('updateInfirmierStatut', async () => {
    await firstValueFrom(service.updateInfirmierStatut(3, 'actif'));
    expect(httpMock.put).toHaveBeenCalledWith(`${base}/infirmiers/3/statut`, { statut: 'actif' });
  });

  it('nommerInfirmierMajor', async () => {
    await firstValueFrom(service.nommerInfirmierMajor(3, 5));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/infirmiers/3/nommer-major`, { idService: 5 });
  });

  it('revoquerInfirmierMajor with motif', async () => {
    await firstValueFrom(service.revoquerInfirmierMajor(3, 'raison'));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/infirmiers/3/revoquer-major`, { motif: 'raison' });
  });

  it('revoquerInfirmierMajor without motif', async () => {
    await firstValueFrom(service.revoquerInfirmierMajor(3));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/infirmiers/3/revoquer-major`, { motif: undefined });
  });

  it('updateInfirmierAccreditations', async () => {
    await firstValueFrom(service.updateInfirmierAccreditations(3, 'IDE,Urgences'));
    expect(httpMock.put).toHaveBeenCalledWith(`${base}/infirmiers/3/accreditations`, { accreditations: 'IDE,Urgences' });
  });

  it('getHistoriqueAffectationsMedecin', async () => {
    await firstValueFrom(service.getHistoriqueAffectationsMedecin(11));
    expect(httpMock.get).toHaveBeenCalledWith(`${affBase}/medecin/11/historique`);
  });

  it('getHistoriqueAffectationsInfirmier', async () => {
    await firstValueFrom(service.getHistoriqueAffectationsInfirmier(11));
    expect(httpMock.get).toHaveBeenCalledWith(`${affBase}/infirmier/11/historique`);
  });

  it('changerServiceMedecin', async () => {
    const req = { idNouveauService: 5, motif: 'reaffect' };
    await firstValueFrom(service.changerServiceMedecin(11, req));
    expect(httpMock.put).toHaveBeenCalledWith(`${affBase}/medecin/11/changer-service`, req);
  });

  it('changerServiceInfirmier', async () => {
    const req = { idNouveauService: 5 };
    await firstValueFrom(service.changerServiceInfirmier(11, req));
    expect(httpMock.put).toHaveBeenCalledWith(`${affBase}/infirmier/11/changer-service`, req);
  });
});
