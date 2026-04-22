import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import { RendezVousService } from './rendez-vous.service';
import { createHttpClientMock, paramsFromCall } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('RendezVousService', () => {
  let service: RendezVousService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/rendezvous`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new RendezVousService(httpMock.http);
  });

  describe('HTTP', () => {
    it('getStats', async () => {
      await firstValueFrom(service.getStats());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/stats`);
    });
    it('getUpcoming', async () => {
      await firstValueFrom(service.getUpcoming());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/a-venir`);
    });
    it('getHistory default limite=20', async () => {
      await firstValueFrom(service.getHistory());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/historique`, { params: { limite: '20' } });
    });
    it('getHistory custom limite', async () => {
      await firstValueFrom(service.getHistory(50));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/historique`, { params: { limite: '50' } });
    });
    it('getById', async () => {
      await firstValueFrom(service.getById(3));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/3`);
    });
    it('create', async () => {
      const req = { idMedecin: 1, dateHeure: '2026-01-01T10:00:00' };
      await firstValueFrom(service.create(req));
      expect(httpMock.post).toHaveBeenCalledWith(base, req);
    });
    it('update', async () => {
      await firstValueFrom(service.update(4, { duree: 30 }));
      expect(httpMock.put).toHaveBeenCalledWith(`${base}/4`, { duree: 30 });
    });
    it('annuler', async () => {
      await firstValueFrom(service.annuler({ idRendezVous: 5, motif: 'x' }));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/annuler`, { idRendezVous: 5, motif: 'x' });
    });
    it('getPropositions', async () => {
      await firstValueFrom(service.getPropositions());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/patient/propositions`);
    });
    it('accepterProposition', async () => {
      await firstValueFrom(service.accepterProposition(7));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/patient/accepter-proposition`, { idRendezVous: 7 });
    });
    it('refuserProposition with motif', async () => {
      await firstValueFrom(service.refuserProposition(7, 'busy'));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/patient/refuser-proposition`, {
        idRendezVous: 7,
        motif: 'busy',
      });
    });
    it('getServices', async () => {
      await firstValueFrom(service.getServices());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/services`);
    });
    it('getMedecins without serviceId', async () => {
      await firstValueFrom(service.getMedecins());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params).toBeInstanceOf(HttpParams);
      expect(params?.keys().length).toBe(0);
    });
    it('getMedecins with serviceId', async () => {
      await firstValueFrom(service.getMedecins(3));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('serviceId')).toBe('3');
    });
    it('getCreneaux', async () => {
      await firstValueFrom(service.getCreneaux(5, '2026-01-01', '2026-01-31'));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/creneaux/5`, {
        params: { dateDebut: '2026-01-01', dateFin: '2026-01-31' },
      });
    });
  });

  describe('helpers', () => {
    it('getStatutLabel known / fallback', () => {
      expect(service.getStatutLabel('planifie')).toBe('Planifié');
      expect(service.getStatutLabel('confirme')).toBe('Confirmé');
      expect(service.getStatutLabel('xxx')).toBe('xxx');
    });
    it('getTypeRdvLabel known / fallback', () => {
      expect(service.getTypeRdvLabel('consultation')).toBe('Consultation');
      expect(service.getTypeRdvLabel('urgence')).toBe('Urgence');
      expect(service.getTypeRdvLabel('xxx')).toBe('xxx');
    });
    it('formatDate returns formatted string', () => {
      const r = service.formatDate('2026-03-18T10:00:00');
      expect(r).toContain('2026');
    });
    it('formatTime returns HH:MM format', () => {
      expect(service.formatTime('2026-03-18T10:30:00')).toMatch(/\d{2}:\d{2}/);
    });
    it('formatDateTime combines date and time', () => {
      expect(service.formatDateTime('2026-03-18T10:00:00')).toContain(' à ');
    });
    it('formatShortDate returns short format', () => {
      const r = service.formatShortDate('2026-03-18');
      expect(r).toMatch(/\d{2}/);
    });
    it('toLocalISOString converts local date', () => {
      const date = new Date(2026, 0, 15, 10, 30);
      const iso = service.toLocalISOString(date);
      expect(iso).toContain('2026-01-15');
      expect(iso).toContain('10:30');
    });
    it('isToday returns true for today', () => {
      const today = new Date();
      expect(service.isToday(today.toISOString())).toBe(true);
    });
    it('isToday returns false for other day', () => {
      expect(service.isToday('2020-01-01T10:00:00')).toBe(false);
    });
    it('isPast returns true for past date', () => {
      expect(service.isPast('2020-01-01T10:00:00')).toBe(true);
    });
    it('isPast returns false for future', () => {
      const future = new Date(Date.now() + 86400000).toISOString();
      expect(service.isPast(future)).toBe(false);
    });
    it('isFuture is inverse of isPast', () => {
      const future = new Date(Date.now() + 86400000).toISOString();
      expect(service.isFuture(future)).toBe(true);
      expect(service.isFuture('2020-01-01T10:00:00')).toBe(false);
    });
  });
});
