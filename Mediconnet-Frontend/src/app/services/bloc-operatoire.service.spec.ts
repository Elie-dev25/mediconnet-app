import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import { BlocOperatoireService } from './bloc-operatoire.service';
import { createHttpClientMock, paramsFromCall } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('BlocOperatoireService', () => {
  let service: BlocOperatoireService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/blocs-operatoires`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new BlocOperatoireService(httpMock.http);
  });

  describe('blocs CRUD', () => {
    it('getAllBlocs', async () => {
      await firstValueFrom(service.getAllBlocs());
      expect(httpMock.get).toHaveBeenCalledWith(base);
    });
    it('getBlocById', async () => {
      await firstValueFrom(service.getBlocById(3));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/3`);
    });
    it('createBloc', async () => {
      const req = { nom: 'Bloc A' };
      await firstValueFrom(service.createBloc(req));
      expect(httpMock.post).toHaveBeenCalledWith(base, req);
    });
    it('updateBloc', async () => {
      await firstValueFrom(service.updateBloc(5, { actif: false }));
      expect(httpMock.put).toHaveBeenCalledWith(`${base}/5`, { actif: false });
    });
    it('deleteBloc', async () => {
      await firstValueFrom(service.deleteBloc(8));
      expect(httpMock.delete).toHaveBeenCalledWith(`${base}/8`);
    });
  });

  describe('reservations', () => {
    it('getReservationsByBloc without dates', async () => {
      await firstValueFrom(service.getReservationsByBloc(2));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.keys().length).toBe(0);
    });
    it('getReservationsByBloc with dates', async () => {
      await firstValueFrom(service.getReservationsByBloc(2, '2026-01-01', '2026-01-31'));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('dateDebut')).toBe('2026-01-01');
      expect(params?.get('dateFin')).toBe('2026-01-31');
    });
    it('getReservationsByDate', async () => {
      await firstValueFrom(service.getReservationsByDate('2026-05-05'));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/reservations/date/2026-05-05`);
    });
    it('getReservationById', async () => {
      await firstValueFrom(service.getReservationById(7));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/reservations/7`);
    });
    it('createReservation', async () => {
      const req = { idBloc: 1, idProgrammation: 2, dateReservation: '2026-01-01', heureDebut: '10:00', dureeMinutes: 60 };
      await firstValueFrom(service.createReservation(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/reservations`, req);
    });
    it('updateReservation', async () => {
      await firstValueFrom(service.updateReservation(3, { statut: 'terminee' }));
      expect(httpMock.put).toHaveBeenCalledWith(`${base}/reservations/3`, { statut: 'terminee' });
    });
    it('cancelReservation', async () => {
      await firstValueFrom(service.cancelReservation(5));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/reservations/5/annuler`, {});
    });
  });

  describe('disponibilites', () => {
    it('getDisponibilites builds params', async () => {
      await firstValueFrom(service.getDisponibilites('2026-01-01', '10:00', 60));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('date')).toBe('2026-01-01');
      expect(params?.get('heureDebut')).toBe('10:00');
      expect(params?.get('dureeMinutes')).toBe('60');
    });
    it('verifierDisponibilite', async () => {
      const req = { date: '2026-01-01', heureDebut: '10:00', dureeMinutes: 60 };
      await firstValueFrom(service.verifierDisponibilite(3, req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/3/verifier-disponibilite`, req);
    });
    it('confirmerAnnulationRdv', async () => {
      const req = { date: 'x', heureDebut: '10:00', dureeMinutes: 60, patientIntervention: 'p', nomChirurgien: 'c' };
      await firstValueFrom(service.confirmerAnnulationRdv(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/confirmer-annulation-rdv`, req);
    });
  });

  describe('agenda', () => {
    it('getAgendaBloc without date', async () => {
      await firstValueFrom(service.getAgendaBloc(3));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.keys().length).toBe(0);
      expect(httpMock.get.mock.calls[0][0]).toBe(`${base}/3/agenda`);
    });
    it('getAgendaBloc with date', async () => {
      await firstValueFrom(service.getAgendaBloc(3, '2026-05-01'));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('date')).toBe('2026-05-01');
    });
    it('getAgendaTousBlocs without date', async () => {
      await firstValueFrom(service.getAgendaTousBlocs());
      expect(httpMock.get.mock.calls[0][0]).toBe(`${base}/agenda`);
    });
    it('getAgendaTousBlocs with date', async () => {
      await firstValueFrom(service.getAgendaTousBlocs('2026-05-01'));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('date')).toBe('2026-05-01');
    });
  });

  describe('helpers', () => {
    it('getStatutLabel maps known / falls back to raw', () => {
      expect(service.getStatutLabel('libre')).toBe('Libre');
      expect(service.getStatutLabel('xxx')).toBe('xxx');
    });
    it('getStatutColor maps known / falls back to secondary', () => {
      expect(service.getStatutColor('occupe')).toBe('danger');
      expect(service.getStatutColor('xxx')).toBe('secondary');
    });
    it('getReservationStatutLabel / Color', () => {
      expect(service.getReservationStatutLabel('terminee')).toBe('Terminée');
      expect(service.getReservationStatutLabel('xxx')).toBe('xxx');
      expect(service.getReservationStatutColor('annulee')).toBe('danger');
      expect(service.getReservationStatutColor('xxx')).toBe('secondary');
    });
    it('formatDuree handles minutes only', () => {
      expect(service.formatDuree(45)).toBe('45min');
    });
    it('formatDuree handles whole hours', () => {
      expect(service.formatDuree(120)).toBe('2h');
    });
    it('formatDuree handles hours and minutes', () => {
      expect(service.formatDuree(90)).toBe('1h30');
    });
  });
});
