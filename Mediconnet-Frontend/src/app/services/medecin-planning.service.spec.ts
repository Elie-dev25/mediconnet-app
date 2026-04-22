import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import { MedecinPlanningService } from './medecin-planning.service';
import { createHttpClientMock, paramsFromCall } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('MedecinPlanningService', () => {
  let service: MedecinPlanningService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/medecin/planning`;
  const rdvBase = `${environment.apiUrl}/rendezvous/medecin`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new MedecinPlanningService(httpMock.http);
  });

  describe('dashboard + creneaux', () => {
    it('getDashboard', async () => {
      await firstValueFrom(service.getDashboard());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/dashboard`);
    });
    it('getSemaineType', async () => {
      await firstValueFrom(service.getSemaineType());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/semaine-type`);
    });
    it('getSemainePlanning without date', async () => {
      await firstValueFrom(service.getSemainePlanning());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.keys().length).toBe(0);
    });
    it('getSemainePlanning with date', async () => {
      await firstValueFrom(service.getSemainePlanning('2026-01-01'));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('date')).toBe('2026-01-01');
    });
    it('getCreneauxJour', async () => {
      await firstValueFrom(service.getCreneauxJour(1));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/creneaux/1`);
    });
    it('createCreneau', async () => {
      const req = { jourSemaine: 1, heureDebut: '08:00', heureFin: '12:00' };
      await firstValueFrom(service.createCreneau(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/creneaux`, req);
    });
    it('updateCreneau', async () => {
      const req = { jourSemaine: 1, heureDebut: '08:00', heureFin: '12:00' };
      await firstValueFrom(service.updateCreneau(5, req));
      expect(httpMock.put).toHaveBeenCalledWith(`${base}/creneaux/5`, req);
    });
    it('deleteCreneau', async () => {
      await firstValueFrom(service.deleteCreneau(5));
      expect(httpMock.delete).toHaveBeenCalledWith(`${base}/creneaux/5`);
    });
    it('toggleCreneau PATCHes', async () => {
      await firstValueFrom(service.toggleCreneau(5));
      expect(httpMock.patch).toHaveBeenCalledWith(`${base}/creneaux/5/toggle`, {});
    });
  });

  describe('indisponibilites', () => {
    it('getIndisponibilites without dates', async () => {
      await firstValueFrom(service.getIndisponibilites());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.keys().length).toBe(0);
    });
    it('getIndisponibilites with dates', async () => {
      await firstValueFrom(service.getIndisponibilites('2026-01-01', '2026-01-31'));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('dateDebut')).toBe('2026-01-01');
      expect(params?.get('dateFin')).toBe('2026-01-31');
    });
    it('createIndisponibilite', async () => {
      const req = { dateDebut: '2026-01-01', dateFin: '2026-01-10', type: 'conge' };
      await firstValueFrom(service.createIndisponibilite(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/indisponibilites`, req);
    });
    it('deleteIndisponibilite', async () => {
      await firstValueFrom(service.deleteIndisponibilite(8));
      expect(httpMock.delete).toHaveBeenCalledWith(`${base}/indisponibilites/8`);
    });
  });

  describe('calendrier', () => {
    it('getCalendrierSemaine without date', async () => {
      await firstValueFrom(service.getCalendrierSemaine());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.keys().length).toBe(0);
    });
    it('getCalendrierSemaine with date', async () => {
      await firstValueFrom(service.getCalendrierSemaine('2026-01-01'));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('date')).toBe('2026-01-01');
    });
    it('getCalendrierJour', async () => {
      await firstValueFrom(service.getCalendrierJour('2026-01-05'));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/calendrier/jour`, { params: { date: '2026-01-05' } });
    });
  });

  describe('RDV medecin', () => {
    it('getMedecinRdvList with all filters', async () => {
      await firstValueFrom(service.getMedecinRdvList('2026-01-01', '2026-01-31', 'planifie'));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('dateDebut')).toBe('2026-01-01');
      expect(params?.get('dateFin')).toBe('2026-01-31');
      expect(params?.get('statut')).toBe('planifie');
    });
    it('getMedecinRdvJour', async () => {
      await firstValueFrom(service.getMedecinRdvJour('2026-01-05'));
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('date')).toBe('2026-01-05');
    });
    it('updateStatutRdv', async () => {
      await firstValueFrom(service.updateStatutRdv(3, 'termine'));
      expect(httpMock.patch).toHaveBeenCalledWith(`${rdvBase}/3/statut`, { statut: 'termine' });
    });
    it('getRdvEnAttente', async () => {
      await firstValueFrom(service.getRdvEnAttente());
      expect(httpMock.get).toHaveBeenCalledWith(`${rdvBase}/en-attente`);
    });
    it('validerRdv', async () => {
      await firstValueFrom(service.validerRdv(5));
      expect(httpMock.post).toHaveBeenCalledWith(`${rdvBase}/valider`, { idRendezVous: 5 });
    });
    it('annulerRdvMedecin', async () => {
      await firstValueFrom(service.annulerRdvMedecin(5, 'malade'));
      expect(httpMock.post).toHaveBeenCalledWith(`${rdvBase}/annuler`, { idRendezVous: 5, motif: 'malade' });
    });
    it('suggererCreneau', async () => {
      await firstValueFrom(service.suggererCreneau(5, '2026-01-01T10:00:00', 'msg'));
      expect(httpMock.post).toHaveBeenCalledWith(`${rdvBase}/suggerer-creneau`, {
        idRendezVous: 5,
        nouveauCreneau: '2026-01-01T10:00:00',
        message: 'msg',
      });
    });
  });

  describe('consultation directe / spontanee', () => {
    it('creerConsultationDirecte', async () => {
      await firstValueFrom(service.creerConsultationDirecte(7));
      expect(httpMock.post).toHaveBeenCalledWith(`${environment.apiUrl}/medecin/rdv/7/creer-consultation`, {});
    });
    it('creerConsultationSpontanee with motif', async () => {
      await firstValueFrom(service.creerConsultationSpontanee(3, 'urgence'));
      expect(httpMock.post).toHaveBeenCalledWith(`${environment.apiUrl}/medecin/patient/3/consultation-spontanee`, { motif: 'urgence' });
    });
    it('creerConsultationSpontanee without motif', async () => {
      await firstValueFrom(service.creerConsultationSpontanee(3));
      expect(httpMock.post).toHaveBeenCalledWith(`${environment.apiUrl}/medecin/patient/3/consultation-spontanee`, { motif: undefined });
    });
  });

  describe('helpers', () => {
    it('getJourNom returns correct day name', () => {
      expect(service.getJourNom(1)).toBe('Lundi');
      expect(service.getJourNom(7)).toBe('Dimanche');
      expect(service.getJourNom(99)).toBe('');
      expect(service.getJourNom(0)).toBe('');
    });
    it('getTypeIndispoOptions returns all 4 types', () => {
      const options = service.getTypeIndispoOptions();
      expect(options).toHaveLength(4);
      expect(options.map(o => o.value)).toEqual(['conge', 'maladie', 'formation', 'autre']);
    });
    it('formatDate returns formatted string', () => {
      expect(service.formatDate('2026-03-18')).toMatch(/\d{1,2}/);
    });
    it('formatTime returns HH:MM', () => {
      expect(service.formatTime('2026-03-18T10:30:00')).toMatch(/\d{2}:\d{2}/);
    });
  });
});
