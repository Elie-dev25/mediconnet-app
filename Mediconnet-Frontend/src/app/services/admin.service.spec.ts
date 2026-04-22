import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { AdminService } from './admin.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('AdminService', () => {
  let service: AdminService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/admin`;
  const infBase = `${environment.apiUrl}/specialites-infirmiers`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new AdminService(httpMock.http);
  });

  it('getServices GETs /admin/services', async () => {
    await firstValueFrom(service.getServices());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/services`);
  });

  it('getService GETs /admin/services/:id', async () => {
    await firstValueFrom(service.getService(3));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/services/3`);
  });

  it('createService POSTs /admin/services', async () => {
    const req = { nomService: 'Cardio' };
    await firstValueFrom(service.createService(req));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/services`, req);
  });

  it('updateService PUTs /admin/services/:id', async () => {
    const req = { nomService: 'Cardio', coutConsultation: 10000 };
    await firstValueFrom(service.updateService(5, req));
    expect(httpMock.put).toHaveBeenCalledWith(`${base}/services/5`, req);
  });

  it('deleteService DELETEs /admin/services/:id', async () => {
    await firstValueFrom(service.deleteService(9));
    expect(httpMock.delete).toHaveBeenCalledWith(`${base}/services/9`);
  });

  it('getResponsables GETs /admin/responsables', async () => {
    await firstValueFrom(service.getResponsables());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/responsables`);
  });

  it('getSpecialites GETs /admin/specialites', async () => {
    await firstValueFrom(service.getSpecialites());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/specialites`);
  });

  it('getSpecialitesInfirmiers GETs /specialites-infirmiers', async () => {
    await firstValueFrom(service.getSpecialitesInfirmiers());
    expect(httpMock.get).toHaveBeenCalledWith(infBase);
  });

  it('getSpecialitesInfirmiersActives GETs /specialites-infirmiers/actives', async () => {
    await firstValueFrom(service.getSpecialitesInfirmiersActives());
    expect(httpMock.get).toHaveBeenCalledWith(`${infBase}/actives`);
  });

  it('getSpecialiteInfirmier GETs /specialites-infirmiers/:id', async () => {
    await firstValueFrom(service.getSpecialiteInfirmier(4));
    expect(httpMock.get).toHaveBeenCalledWith(`${infBase}/4`);
  });

  it('createSpecialiteInfirmier POSTs /specialites-infirmiers', async () => {
    const req = { nom: 'Anesthésie' };
    await firstValueFrom(service.createSpecialiteInfirmier(req));
    expect(httpMock.post).toHaveBeenCalledWith(infBase, req);
  });

  it('updateSpecialiteInfirmier PUTs /specialites-infirmiers/:id', async () => {
    const req = { nom: 'Anesthésie', actif: true };
    await firstValueFrom(service.updateSpecialiteInfirmier(2, req));
    expect(httpMock.put).toHaveBeenCalledWith(`${infBase}/2`, req);
  });

  it('deleteSpecialiteInfirmier DELETEs /specialites-infirmiers/:id', async () => {
    await firstValueFrom(service.deleteSpecialiteInfirmier(12));
    expect(httpMock.delete).toHaveBeenCalledWith(`${infBase}/12`);
  });
});
