import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { AdminSettingsService } from './admin-settings.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('AdminSettingsService', () => {
  let service: AdminSettingsService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/admin/settings`;
  const stdBase = `${environment.apiUrl}/standard-chambre`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new AdminSettingsService(httpMock.http);
  });

  describe('chambres', () => {
    it('getChambres', async () => {
      await firstValueFrom(service.getChambres());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/chambres`);
    });
    it('getChambre by id', async () => {
      await firstValueFrom(service.getChambre(3));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/chambres/3`);
    });
    it('createChambre POSTs', async () => {
      const req = { numero: '101', capacite: 2 };
      await firstValueFrom(service.createChambre(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/chambres`, req);
    });
    it('updateChambre PUTs', async () => {
      await firstValueFrom(service.updateChambre(5, { numero: '102' }));
      expect(httpMock.put).toHaveBeenCalledWith(`${base}/chambres/5`, { numero: '102' });
    });
    it('deleteChambre DELETEs', async () => {
      await firstValueFrom(service.deleteChambre(8));
      expect(httpMock.delete).toHaveBeenCalledWith(`${base}/chambres/8`);
    });
    it('getChambresStats GETs /chambres/stats', async () => {
      await firstValueFrom(service.getChambresStats());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/chambres/stats`);
    });
  });

  describe('lits', () => {
    it('addLit POSTs /chambres/:id/lits', async () => {
      await firstValueFrom(service.addLit(2, { numero: 'A' }));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/chambres/2/lits`, { numero: 'A' });
    });
    it('updateLit PUTs /lits/:id', async () => {
      await firstValueFrom(service.updateLit(5, { statut: 'libre' }));
      expect(httpMock.put).toHaveBeenCalledWith(`${base}/lits/5`, { statut: 'libre' });
    });
    it('deleteLit DELETEs /lits/:id', async () => {
      await firstValueFrom(service.deleteLit(9));
      expect(httpMock.delete).toHaveBeenCalledWith(`${base}/lits/9`);
    });
  });

  describe('laboratoires', () => {
    it('getLaboratoires', async () => {
      await firstValueFrom(service.getLaboratoires());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/laboratoires`);
    });
  });

  describe('standards', () => {
    it('getStandards', async () => {
      await firstValueFrom(service.getStandards());
      expect(httpMock.get).toHaveBeenCalledWith(stdBase);
    });
    it('getStandardsForSelect', async () => {
      await firstValueFrom(service.getStandardsForSelect());
      expect(httpMock.get).toHaveBeenCalledWith(`${stdBase}/select`);
    });
    it('getStandard by id', async () => {
      await firstValueFrom(service.getStandard(7));
      expect(httpMock.get).toHaveBeenCalledWith(`${stdBase}/7`);
    });
    it('createStandard', async () => {
      const req = { nom: 'VIP', prixJournalier: 50000, privileges: ['wifi'] };
      await firstValueFrom(service.createStandard(req));
      expect(httpMock.post).toHaveBeenCalledWith(stdBase, req);
    });
    it('updateStandard', async () => {
      await firstValueFrom(service.updateStandard(2, { actif: false }));
      expect(httpMock.put).toHaveBeenCalledWith(`${stdBase}/2`, { actif: false });
    });
    it('deleteStandard', async () => {
      await firstValueFrom(service.deleteStandard(4));
      expect(httpMock.delete).toHaveBeenCalledWith(`${stdBase}/4`);
    });
  });
});
