import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom, of } from 'rxjs';
import { PharmacieStockService } from './pharmacie-stock.service';
import { createHttpClientMock, paramsFromCall } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('PharmacieStockService', () => {
  let service: PharmacieStockService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/pharmacie`;
  const medBase = `${environment.apiUrl}/medicament`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new PharmacieStockService(httpMock.http);
  });

  it('getKpis / getAlertes', async () => {
    await firstValueFrom(service.getKpis());
    await firstValueFrom(service.getAlertes());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/kpis`);
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/alertes`);
  });

  describe('medicaments', () => {
    it('getMedicaments defaults', async () => {
      await firstValueFrom(service.getMedicaments());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('page')).toBe('1');
      expect(params?.get('pageSize')).toBe('20');
    });
    it('getMedicaments with filters', async () => {
      await firstValueFrom(service.getMedicaments('para', 'actif', 2, 50));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('search')).toBe('para');
      expect(params?.get('statut')).toBe('actif');
      expect(params?.get('page')).toBe('2');
      expect(params?.get('pageSize')).toBe('50');
    });

    it('searchMedicamentsForAutocomplete returns [] for short query', async () => {
      const r1 = await firstValueFrom(service.searchMedicamentsForAutocomplete(''));
      expect(r1).toEqual([]);
      const r2 = await firstValueFrom(service.searchMedicamentsForAutocomplete('a'));
      expect(r2).toEqual([]);
      expect(httpMock.get).not.toHaveBeenCalled();
    });
    it('searchMedicamentsForAutocomplete maps result.items', async () => {
      httpMock.get.mockReturnValueOnce(of({ items: [{ idMedicament: 1 }], totalCount: 1, page: 1, pageSize: 10, totalPages: 1 }));
      const res = await firstValueFrom(service.searchMedicamentsForAutocomplete('para'));
      expect(res).toEqual([{ idMedicament: 1 }]);
    });

    it('getMedicamentById', async () => {
      await firstValueFrom(service.getMedicamentById(5));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/medicaments/5`);
    });
    it('getFournisseursByMedicament', async () => {
      await firstValueFrom(service.getFournisseursByMedicament(5));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/medicaments/5/fournisseurs`);
    });
    it('getHistoriqueFournisseurMedicament', async () => {
      await firstValueFrom(service.getHistoriqueFournisseurMedicament(5));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/medicaments/5/historique`);
    });
    it('createMedicament / update / delete', async () => {
      const req = { nom: 'x', stock: 0, seuilStock: 0, prix: 0 };
      await firstValueFrom(service.createMedicament(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/medicaments`, req);
      await firstValueFrom(service.updateMedicament(3, { nom: 'y' }));
      expect(httpMock.put).toHaveBeenCalledWith(`${base}/medicaments/3`, { nom: 'y' });
      await firstValueFrom(service.deleteMedicament(3));
      expect(httpMock.delete).toHaveBeenCalledWith(`${base}/medicaments/3`);
    });
    it('ajusterStock', async () => {
      const req = { idMedicament: 1, quantite: 10, typeMouvement: 'entree' };
      await firstValueFrom(service.ajusterStock(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/stock/ajustement`, req);
    });
  });

  describe('mouvements', () => {
    it('no filter fills defaults', async () => {
      await firstValueFrom(service.getMouvements({}));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('page')).toBe('1');
      expect(params?.get('pageSize')).toBe('20');
    });
    it('all filters', async () => {
      await firstValueFrom(
        service.getMouvements({ idMedicament: 1, typeMouvement: 'entree', dateDebut: '2026-01-01', dateFin: '2026-01-31', page: 2, pageSize: 50 })
      );
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('idMedicament')).toBe('1');
      expect(params?.get('typeMouvement')).toBe('entree');
      expect(params?.get('dateDebut')).toBe('2026-01-01');
      expect(params?.get('dateFin')).toBe('2026-01-31');
      expect(params?.get('page')).toBe('2');
      expect(params?.get('pageSize')).toBe('50');
    });
  });

  describe('fournisseurs', () => {
    it('getFournisseurs without filter', async () => {
      await firstValueFrom(service.getFournisseurs());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.keys().length).toBe(0);
    });
    it('getFournisseurs actif filter', async () => {
      await firstValueFrom(service.getFournisseurs(true));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('actif')).toBe('true');
    });
    it('getFournisseur', async () => {
      await firstValueFrom(service.getFournisseur(5));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/fournisseurs/5`);
    });
    it('createFournisseur / update / toggle / delete', async () => {
      await firstValueFrom(service.createFournisseur({ nomFournisseur: 'F' }));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/fournisseurs`, { nomFournisseur: 'F' });
      await firstValueFrom(service.updateFournisseur(3, { nomFournisseur: 'F2' }));
      expect(httpMock.put).toHaveBeenCalledWith(`${base}/fournisseurs/3`, { nomFournisseur: 'F2' });
      await firstValueFrom(service.toggleFournisseurStatut(3));
      expect(httpMock.patch).toHaveBeenCalledWith(`${base}/fournisseurs/3/toggle-statut`, {});
      await firstValueFrom(service.deleteFournisseur(3));
      expect(httpMock.delete).toHaveBeenCalledWith(`${base}/fournisseurs/3`);
    });
  });

  describe('commandes', () => {
    it('getCommandes defaults', async () => {
      await firstValueFrom(service.getCommandes());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('page')).toBe('1');
      expect(params?.get('pageSize')).toBe('20');
    });
    it('getCommandes with statut and pagination', async () => {
      await firstValueFrom(service.getCommandes('en_cours', 2, 50));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('statut')).toBe('en_cours');
    });
    it('getCommandeById', async () => {
      await firstValueFrom(service.getCommandeById(7));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/commandes/7`);
    });
    it('createCommande', async () => {
      await firstValueFrom(service.createCommande({} as never));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/commandes`, {});
    });
    it('receptionnerCommande', async () => {
      await firstValueFrom(service.receptionnerCommande(5, {} as never));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/commandes/5/reception`, {});
    });
    it('annulerCommande', async () => {
      await firstValueFrom(service.annulerCommande(5));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/commandes/5/annuler`, {});
    });
  });

  describe('profile/dashboard', () => {
    it('getProfile / getDashboard / updateProfile', async () => {
      await firstValueFrom(service.getProfile());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/profile`);
      await firstValueFrom(service.getDashboard());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/dashboard`);
      await firstValueFrom(service.updateProfile({ telephone: 'x' }));
      expect(httpMock.put).toHaveBeenCalledWith(`${base}/profile`, { telephone: 'x' });
    });
  });

  describe('ordonnances', () => {
    it('getOrdonnances defaults and with search', async () => {
      await firstValueFrom(service.getOrdonnances());
      let params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('page')).toBe('1');
      await firstValueFrom(service.getOrdonnances('abc', 2, 50));
      params = paramsFromCall(httpMock.get.mock.calls[1]);
      expect(params?.get('search')).toBe('abc');
    });
    it('dispenserOrdonnance', async () => {
      await firstValueFrom(service.dispenserOrdonnance({} as never));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/dispensations`, {});
    });
    it('getDispensations defaults and with dates', async () => {
      await firstValueFrom(service.getDispensations());
      await firstValueFrom(service.getDispensations('2026-01-01', '2026-01-31'));
      const params = paramsFromCall(httpMock.get.mock.calls[1]);
      expect(params?.get('dateDebut')).toBe('2026-01-01');
      expect(params?.get('dateFin')).toBe('2026-01-31');
    });
    it('validerOrdonnance / delivrerOrdonnance / getOrdonnanceDetail', async () => {
      await firstValueFrom(service.validerOrdonnance(5));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/ordonnances/5/valider`, {});
      await firstValueFrom(service.delivrerOrdonnance(5));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/ordonnances/5/delivrer`, {});
      await firstValueFrom(service.getOrdonnanceDetail(5));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/ordonnances/5`);
    });
  });

  describe('formes & voies', () => {
    it('getFormesVoiesMedicament', async () => {
      await firstValueFrom(service.getFormesVoiesMedicament(3));
      expect(httpMock.get).toHaveBeenCalledWith(`${medBase}/3/formes-voies`);
    });
    it('getAllFormesPharmaceutiques / getAllVoiesAdministration', async () => {
      await firstValueFrom(service.getAllFormesPharmaceutiques());
      expect(httpMock.get).toHaveBeenCalledWith(`${medBase}/formes`);
      await firstValueFrom(service.getAllVoiesAdministration());
      expect(httpMock.get).toHaveBeenCalledWith(`${medBase}/voies`);
    });
  });

  describe('ventes directes', () => {
    it('creerVenteDirecte', async () => {
      await firstValueFrom(service.creerVenteDirecte({ lignes: [], modePaiement: 'especes' }));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/ventes-directes`, { lignes: [], modePaiement: 'especes' });
    });
    it('getVentesDirectes with all filters', async () => {
      await firstValueFrom(
        service.getVentesDirectes({ page: 1, pageSize: 20, dateDebut: '2026-01-01', dateFin: '2026-01-31', nomClient: 'C', numeroTicket: 'T1' })
      );
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('dateDebut')).toBe('2026-01-01');
      expect(params?.get('dateFin')).toBe('2026-01-31');
      expect(params?.get('nomClient')).toBe('C');
      expect(params?.get('numeroTicket')).toBe('T1');
    });
    it('getVenteDirecteById', async () => {
      await firstValueFrom(service.getVenteDirecteById(5));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/ventes-directes/5`);
    });
    it('delivrerVenteDirecte', async () => {
      await firstValueFrom(service.delivrerVenteDirecte(5));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/ventes-directes/5/delivrer`, {});
    });
  });
});
