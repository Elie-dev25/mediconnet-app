import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { ParametreService } from './parametre.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('ParametreService', () => {
  let service: ParametreService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/parametre`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new ParametreService(httpMock.http);
  });

  describe('HTTP methods', () => {
    it('getByConsultation GETs /consultation/:id', async () => {
      await firstValueFrom(service.getByConsultation(12));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/consultation/12`);
    });

    it('getHistoriquePatient GETs /patient/:id/historique', async () => {
      await firstValueFrom(service.getHistoriquePatient(3));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/patient/3/historique`);
    });

    it('createOrUpdate POSTs base', async () => {
      const req = { idConsultation: 1, poids: 70 };
      await firstValueFrom(service.createOrUpdate(req));
      expect(httpMock.post).toHaveBeenCalledWith(base, req);
    });

    it('createByPatient POSTs /patient', async () => {
      const req = { idPatient: 4, poids: 65 };
      await firstValueFrom(service.createByPatient(req));
      expect(httpMock.post).toHaveBeenCalledWith(`${base}/patient`, req);
    });

    it('update PUTs /:id', async () => {
      await firstValueFrom(service.update(9, { poids: 75 }));
      expect(httpMock.put).toHaveBeenCalledWith(`${base}/9`, { poids: 75 });
    });

    it('delete DELETEs /:id', async () => {
      await firstValueFrom(service.delete(5));
      expect(httpMock.delete).toHaveBeenCalledWith(`${base}/5`);
    });
  });

  describe('calculerIMC', () => {
    it('returns null if poids missing', () => {
      expect(service.calculerIMC(null, 170)).toBeNull();
    });

    it('returns null if taille missing', () => {
      expect(service.calculerIMC(70, null)).toBeNull();
    });

    it('returns null if taille is 0', () => {
      expect(service.calculerIMC(70, 0)).toBeNull();
    });

    it('returns null if taille is negative', () => {
      expect(service.calculerIMC(70, -170)).toBeNull();
    });

    it('computes IMC correctly', () => {
      // 70 / (1.70 * 1.70) = 24.22
      expect(service.calculerIMC(70, 170)).toBe(24.22);
    });
  });

  describe('interpreterIMC', () => {
    it('gray for null', () => {
      expect(service.interpreterIMC(null)).toEqual({ label: '-', color: 'gray' });
    });
    it('warning for < 18.5', () => {
      expect(service.interpreterIMC(17).color).toBe('warning');
      expect(service.interpreterIMC(17).label).toContain('Insuffisance');
    });
    it('success for 18.5 <= imc < 25', () => {
      expect(service.interpreterIMC(22).color).toBe('success');
    });
    it('warning for 25 <= imc < 30', () => {
      expect(service.interpreterIMC(27).color).toBe('warning');
      expect(service.interpreterIMC(27).label).toBe('Surpoids');
    });
    it('danger for >= 30', () => {
      expect(service.interpreterIMC(35).color).toBe('danger');
    });
  });

  describe('interpreterTension', () => {
    it('gray if any value null', () => {
      expect(service.interpreterTension(null, 80).color).toBe('gray');
      expect(service.interpreterTension(120, null).color).toBe('gray');
    });
    it('info for hypotension', () => {
      expect(service.interpreterTension(85, 60).color).toBe('info');
      expect(service.interpreterTension(100, 55).color).toBe('info');
    });
    it('success for normale', () => {
      expect(service.interpreterTension(115, 75).color).toBe('success');
    });
    it('warning for pre-hypertension', () => {
      expect(service.interpreterTension(130, 85).color).toBe('warning');
    });
    it('danger for hypertension', () => {
      expect(service.interpreterTension(150, 95).color).toBe('danger');
    });
  });

  describe('interpreterTemperature', () => {
    it('gray if null', () => {
      expect(service.interpreterTemperature(null).color).toBe('gray');
    });
    it('info for hypothermie', () => {
      expect(service.interpreterTemperature(35).color).toBe('info');
    });
    it('success for normale', () => {
      expect(service.interpreterTemperature(37).color).toBe('success');
    });
    it('warning for febricule', () => {
      expect(service.interpreterTemperature(37.8).color).toBe('warning');
    });
    it('warning for fievre moderee', () => {
      expect(service.interpreterTemperature(38.5).color).toBe('warning');
    });
    it('danger for fievre elevee', () => {
      expect(service.interpreterTemperature(40).color).toBe('danger');
    });
  });
});
