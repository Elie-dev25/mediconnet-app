import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { CoordinationInterventionService } from './coordination-intervention.service';
import { createHttpClientMock, paramsFromCall } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('CoordinationInterventionService', () => {
  let service: CoordinationInterventionService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/coordination-intervention`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new CoordinationInterventionService(httpMock.http);
  });

  describe('anesthesistes', () => {
    it('getAnesthesistesDisponibles default duree=60', async () => {
      await firstValueFrom(service.getAnesthesistesDisponibles('2026-01-01', '2026-01-31'));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('dateDebut')).toBe('2026-01-01');
      expect(params?.get('dateFin')).toBe('2026-01-31');
      expect(params?.get('dureeMinutes')).toBe('60');
    });
    it('getAnesthesistesDisponibles custom duree', async () => {
      await firstValueFrom(service.getAnesthesistesDisponibles('2026-01-01', '2026-01-31', 120));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('dureeMinutes')).toBe('120');
    });
    it('getCreneauxAnesthesiste', async () => {
      await firstValueFrom(service.getCreneauxAnesthesiste(3, '2026-01-01', '2026-01-31'));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(httpMock.get.mock.calls[0][0]).toBe(`${base}/anesthesistes/3/creneaux`);
      expect(params?.get('dateDebut')).toBe('2026-01-01');
      expect(params?.get('dateFin')).toBe('2026-01-31');
    });
  });

  describe('actions chirurgien', () => {
    it('proposerCoordination', async () => {
      const req = { idProgrammation: 1, idAnesthesiste: 2, dateProposee: '2026-01-01', heureProposee: '10:00', dureeEstimee: 60 };
      await firstValueFrom(service.proposerCoordination(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/proposer`, req);
    });
    it('accepterContreProposition', async () => {
      const req = { idCoordination: 5 };
      await firstValueFrom(service.accepterContreProposition(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/accepter-contre-proposition`, req);
    });
    it('refuserContreProposition', async () => {
      const req = { idCoordination: 5, motifRefus: 'x' };
      await firstValueFrom(service.refuserContreProposition(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/refuser-contre-proposition`, req);
    });
    it('getMesCoordinationsChirurgien without filter', async () => {
      await firstValueFrom(service.getMesCoordinationsChirurgien());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.keys().length).toBe(0);
    });
    it('getMesCoordinationsChirurgien with filter', async () => {
      await firstValueFrom(
        service.getMesCoordinationsChirurgien({ statut: 'proposee', dateDebut: '2026-01-01', dateFin: '2026-01-31' })
      );
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('statut')).toBe('proposee');
      expect(params?.get('dateDebut')).toBe('2026-01-01');
      expect(params?.get('dateFin')).toBe('2026-01-31');
    });
  });

  describe('actions anesthesiste', () => {
    it('validerCoordination', async () => {
      const req = { idCoordination: 5 };
      await firstValueFrom(service.validerCoordination(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/valider`, req);
    });
    it('modifierCoordination', async () => {
      const req = { idCoordination: 5, dateContreProposee: '2026-01-02', heureContreProposee: '11:00', commentaireAnesthesiste: 'x' };
      await firstValueFrom(service.modifierCoordination(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/modifier`, req);
    });
    it('refuserCoordination', async () => {
      const req = { idCoordination: 5, motifRefus: 'x' };
      await firstValueFrom(service.refuserCoordination(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/refuser`, req);
    });
    it('getMesCoordinationsAnesthesiste without filter', async () => {
      await firstValueFrom(service.getMesCoordinationsAnesthesiste());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.keys().length).toBe(0);
    });
    it('getMesCoordinationsAnesthesiste with filter', async () => {
      await firstValueFrom(service.getMesCoordinationsAnesthesiste({ statut: 'validee' }));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('statut')).toBe('validee');
    });
    it('getDemandesEnAttente', async () => {
      await firstValueFrom(service.getDemandesEnAttente());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/anesthesiste/demandes-en-attente`);
    });
    it('getStatsAnesthesiste', async () => {
      await firstValueFrom(service.getStatsAnesthesiste());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/anesthesiste/stats`);
    });
  });

  describe('common actions', () => {
    it('annulerCoordination', async () => {
      const req = { idCoordination: 5, motifAnnulation: 'x' };
      await firstValueFrom(service.annulerCoordination(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/annuler`, req);
    });
    it('getCoordination', async () => {
      await firstValueFrom(service.getCoordination(5));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/5`);
    });
    it('getHistorique', async () => {
      await firstValueFrom(service.getHistorique(5));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/5/historique`);
    });
  });

  describe('helpers', () => {
    it('getStatutLabel', () => {
      expect(service.getStatutLabel('proposee')).toBe('Proposée');
      expect(service.getStatutLabel('validee')).toBe('Validée');
      expect(service.getStatutLabel('modifiee')).toBe('Contre-proposition');
      expect(service.getStatutLabel('refusee')).toBe('Refusée');
      expect(service.getStatutLabel('annulee')).toBe('Annulée');
      expect(service.getStatutLabel('x')).toBe('x');
    });
    it('getStatutClass', () => {
      expect(service.getStatutClass('proposee')).toBe('warning');
      expect(service.getStatutClass('validee')).toBe('success');
      expect(service.getStatutClass('modifiee')).toBe('info');
      expect(service.getStatutClass('refusee')).toBe('danger');
      expect(service.getStatutClass('annulee')).toBe('secondary');
      expect(service.getStatutClass('x')).toBe('secondary');
    });
    it('getTypeActionLabel', () => {
      expect(service.getTypeActionLabel('proposition')).toBe('Proposition initiale');
      expect(service.getTypeActionLabel('validation')).toBe('Validation');
      expect(service.getTypeActionLabel('modification')).toBe('Contre-proposition');
      expect(service.getTypeActionLabel('refus')).toBe('Refus');
      expect(service.getTypeActionLabel('annulation')).toBe('Annulation');
      expect(service.getTypeActionLabel('acceptation_contre_proposition')).toBe('Acceptation contre-proposition');
      expect(service.getTypeActionLabel('x')).toBe('x');
    });
    it('formatDuree', () => {
      expect(service.formatDuree(45)).toBe('45 min');
      expect(service.formatDuree(120)).toBe('2h');
      expect(service.formatDuree(90)).toBe('1h30');
    });
  });
});
