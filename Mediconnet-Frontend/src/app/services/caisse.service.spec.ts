import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { CaisseService, type RecuTransaction } from './caisse.service';
import { createHttpClientMock, paramsFromCall } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('CaisseService', () => {
  let service: CaisseService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/caisse`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new CaisseService(httpMock.http);
  });

  describe('HTTP', () => {
    it('getKpis', async () => {
      await firstValueFrom(service.getKpis());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/kpis`);
    });
    it('getFacturesEnAttente', async () => {
      await firstValueFrom(service.getFacturesEnAttente());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/factures/en-attente`);
    });
    it('getFacturesPatient', async () => {
      await firstValueFrom(service.getFacturesPatient(3));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/factures/patient/3`);
    });
    it('getFacture', async () => {
      await firstValueFrom(service.getFacture(5));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/factures/5`);
    });
    it('getTransactionsJour', async () => {
      await firstValueFrom(service.getTransactionsJour());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/transactions/jour`);
    });
    it('getTransactions with filters', async () => {
      await firstValueFrom(service.getTransactions('2026-01-01', '2026-01-31', 'especes'));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('dateDebut')).toBe('2026-01-01');
      expect(params?.get('dateFin')).toBe('2026-01-31');
      expect(params?.get('modePaiement')).toBe('especes');
    });
    it('getTransactions no filters', async () => {
      await firstValueFrom(service.getTransactions());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.keys().length).toBe(0);
    });
    it('creerTransaction', async () => {
      const req = { idFacture: 1, montant: 100, modePaiement: 'especes' };
      await firstValueFrom(service.creerTransaction(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/transactions`, req);
    });
    it('annulerTransaction', async () => {
      const req = { idTransaction: 1, motif: 'x' };
      await firstValueFrom(service.annulerTransaction(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/transactions/annuler`, req);
    });
    it('getSessionActive', async () => {
      await firstValueFrom(service.getSessionActive());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/session/active`);
    });
    it('getHistoriqueSessions default limit', async () => {
      await firstValueFrom(service.getHistoriqueSessions());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/session/historique`, { params: { limite: '10' } });
    });
    it('getHistoriqueSessions custom limit', async () => {
      await firstValueFrom(service.getHistoriqueSessions(25));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/session/historique`, { params: { limite: '25' } });
    });
    it('ouvrirCaisse', async () => {
      const req = { montantOuverture: 1000 };
      await firstValueFrom(service.ouvrirCaisse(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/session/ouvrir`, req);
    });
    it('fermerCaisse', async () => {
      const req = { montantFermeture: 2000 };
      await firstValueFrom(service.fermerCaisse(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/session/fermer`, req);
    });
    it('rechercherPatients', async () => {
      await firstValueFrom(service.rechercherPatients('dup'));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/patients/recherche`, { params: { q: 'dup' } });
    });
    it('getRepartitionPaiements', async () => {
      await firstValueFrom(service.getRepartitionPaiements('2026-01-01', '2026-01-31'));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('dateDebut')).toBe('2026-01-01');
      expect(params?.get('dateFin')).toBe('2026-01-31');
    });
    it('getRepartitionPaiements no filters', async () => {
      await firstValueFrom(service.getRepartitionPaiements());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.keys().length).toBe(0);
    });
    it('getRevenusParService with date', async () => {
      await firstValueFrom(service.getRevenusParService('2026-01-15'));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('date')).toBe('2026-01-15');
    });
    it('getRevenusParService without date', async () => {
      await firstValueFrom(service.getRevenusParService());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.keys().length).toBe(0);
    });
    it('getFacturesEnRetard default', async () => {
      await firstValueFrom(service.getFacturesEnRetard());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/stats/factures-retard`, { params: { limite: '5' } });
    });
    it('getFacturesEnRetard custom', async () => {
      await firstValueFrom(service.getFacturesEnRetard(10));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/stats/factures-retard`, { params: { limite: '10' } });
    });
    it('getRecuTransaction', async () => {
      await firstValueFrom(service.getRecuTransaction(5));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/transactions/5/recu`);
    });
  });

  describe('helpers', () => {
    it('formatMontant formats FCFA', () => {
      const res = service.formatMontant(10000);
      expect(res).toMatch(/FCFA/);
      expect(res).toMatch(/10/);
    });
    it('getStatutLabel', () => {
      expect(service.getStatutLabel('en_attente')).toBe('En attente');
      expect(service.getStatutLabel('partiel')).toBe('Partiel');
      expect(service.getStatutLabel('payee')).toBe('Payée');
      expect(service.getStatutLabel('annulee')).toBe('Annulée');
      expect(service.getStatutLabel('remboursee')).toBe('Remboursée');
      expect(service.getStatutLabel('complete')).toBe('Complète');
      expect(service.getStatutLabel('annule')).toBe('Annulé');
      expect(service.getStatutLabel('rembourse')).toBe('Remboursé');
      expect(service.getStatutLabel('x')).toBe('x');
    });
    it('getModePaiementLabel', () => {
      expect(service.getModePaiementLabel('especes')).toBe('Espèces');
      expect(service.getModePaiementLabel('carte')).toBe('Carte bancaire');
      expect(service.getModePaiementLabel('virement')).toBe('Virement');
      expect(service.getModePaiementLabel('cheque')).toBe('Chèque');
      expect(service.getModePaiementLabel('assurance')).toBe('Assurance');
      expect(service.getModePaiementLabel('mobile')).toBe('Mobile Money');
      expect(service.getModePaiementLabel('x')).toBe('x');
    });
  });

  describe('imprimerRecu', () => {
    const makeRecu = (over: Partial<RecuTransaction> = {}): RecuTransaction => ({
      numeroRecu: 'R1',
      numeroTransaction: 'T1',
      numeroFacture: 'F1',
      dateTransaction: '2026-01-15T10:00:00',
      patientNom: 'Dupont',
      patientPrenom: 'Jean',
      montantTotal: 10000,
      montantPaye: 10000,
      modePaiement: 'especes',
      couvertureAssurance: false,
      montantPatient: 10000,
      lignes: [{ idLigne: 1, description: 'Consultation', quantite: 1, prixUnitaire: 10000, montant: 10000 }],
      caissierNom: 'Jean Caissier',
      nomEtablissement: 'Clinique X',
      adresseEtablissement: 'Adresse',
      telephoneEtablissement: '+237 690 00 00 00',
      ...over,
    });

    let openSpy: ReturnType<typeof vi.spyOn>;
    let mockWin: { document: { write: ReturnType<typeof vi.fn>; close: ReturnType<typeof vi.fn> }; focus: ReturnType<typeof vi.fn>; print: ReturnType<typeof vi.fn> };

    beforeEach(() => {
      vi.useFakeTimers();
      mockWin = {
        document: { write: vi.fn(), close: vi.fn() },
        focus: vi.fn(),
        print: vi.fn(),
      };
      openSpy = vi.spyOn(window, 'open').mockReturnValue(mockWin as never);
    });

    afterEach(() => {
      vi.useRealTimers();
      openSpy.mockRestore();
    });

    it('opens a window, writes HTML and schedules print', () => {
      service.imprimerRecu(makeRecu());
      expect(openSpy).toHaveBeenCalled();
      expect(mockWin.document.write).toHaveBeenCalled();
      const html = mockWin.document.write.mock.calls[0][0] as string;
      expect(html).toContain('R1');
      expect(html).toContain('Clinique X');
      vi.advanceTimersByTime(300);
      expect(mockWin.print).toHaveBeenCalled();
    });

    it('handles empty lignes by falling back to typeFacture', () => {
      service.imprimerRecu(makeRecu({ lignes: [], typeFacture: 'Hospitalisation' }));
      const html = mockWin.document.write.mock.calls[0][0] as string;
      expect(html).toContain('Hospitalisation');
    });

    it('includes assurance block when couvert', () => {
      service.imprimerRecu(makeRecu({
        couvertureAssurance: true,
        nomAssurance: 'CNAM',
        tauxCouverture: 80,
        montantAssurance: 8000,
      }));
      const html = mockWin.document.write.mock.calls[0][0] as string;
      expect(html).toContain('CNAM');
      expect(html).toContain('Couverture Assurance');
    });

    it('includes optional fields when provided', () => {
      service.imprimerRecu(makeRecu({
        numeroDossier: 'D1',
        medecinNom: 'Dr Y',
        montantRecu: 12000,
        renduMonnaie: 2000,
        reference: 'REF123',
      }));
      const html = mockWin.document.write.mock.calls[0][0] as string;
      expect(html).toContain('D1');
      expect(html).toContain('Dr Y');
      expect(html).toContain('REF123');
    });

    it('no-ops when window.open returns null', () => {
      openSpy.mockReturnValueOnce(null as never);
      expect(() => service.imprimerRecu(makeRecu())).not.toThrow();
    });
  });
});
