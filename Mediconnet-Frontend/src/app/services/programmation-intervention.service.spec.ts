import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { ProgrammationInterventionService } from './programmation-intervention.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('ProgrammationInterventionService', () => {
  let service: ProgrammationInterventionService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/programmation-intervention`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new ProgrammationInterventionService(httpMock.http);
  });

  it('getMyProgrammations without statut calls GET base with no params', async () => {
    await firstValueFrom(service.getMyProgrammations());
    expect(httpMock.get).toHaveBeenCalledWith(base);
  });

  it('getMyProgrammations with statut passes params', async () => {
    await firstValueFrom(service.getMyProgrammations('planifiee'));
    expect(httpMock.get).toHaveBeenCalledWith(base, { params: { statut: 'planifiee' } });
  });

  it('getProgrammation GETs /:id', async () => {
    await firstValueFrom(service.getProgrammation(7));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/7`);
  });

  it('getByConsultation GETs /consultation/:id', async () => {
    await firstValueFrom(service.getByConsultation(3));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/consultation/3`);
  });

  it('createProgrammation POSTs base with request', async () => {
    const req = { idConsultation: 1, typeIntervention: 'programmee', consentementEclaire: true };
    await firstValueFrom(service.createProgrammation(req));
    expect(httpMock.post).toHaveBeenCalledWith(base, req);
  });

  it('updateProgrammation PUTs /:id', async () => {
    await firstValueFrom(service.updateProgrammation(9, { statut: 'planifiee' }));
    expect(httpMock.put).toHaveBeenCalledWith(`${base}/9`, { statut: 'planifiee' });
  });

  it('annulerProgrammation POSTs /:id/annuler with motif', async () => {
    await firstValueFrom(service.annulerProgrammation(3, 'reason'));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/3/annuler`, { motif: 'reason' });
  });

  it('annulerProgrammation works without motif', async () => {
    await firstValueFrom(service.annulerProgrammation(3));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/3/annuler`, { motif: undefined });
  });

  it('validerConsentement POSTs /:id/consentement with empty body', async () => {
    await firstValueFrom(service.validerConsentement(5));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/5/consentement`, {});
  });

  describe('label helpers', () => {
    it('getStatutLabel returns known label', () => {
      expect(service.getStatutLabel('planifiee')).toBe('Planifiée');
      expect(service.getStatutLabel('realisee')).toBe('Réalisée');
    });

    it('getStatutLabel falls back to raw value for unknown', () => {
      expect(service.getStatutLabel('x')).toBe('x');
    });

    it('getStatutColor returns known color', () => {
      expect(service.getStatutColor('annulee')).toBe('#ef4444');
    });

    it('getStatutColor falls back to gray', () => {
      expect(service.getStatutColor('unknown')).toBe('#6b7280');
    });
  });
});
