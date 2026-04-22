import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom, of } from 'rxjs';
import { DossierMedicalService } from './dossier-medical.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('DossierMedicalService', () => {
  let service: DossierMedicalService;
  let httpMock: ReturnType<typeof createHttpClientMock>;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new DossierMedicalService(httpMock.http);
  });

  describe('getMonDossierMedical', () => {
    it('GETs /patient/dossier-medical and transforms response', async () => {
      const apiResp = {
        idUser: 1,
        nom: 'Dupont',
        prenom: 'Jean',
        numeroDossier: 'D001',
        consultations: [{ idConsultation: 1, motif: 'Fièvre', dateHeure: '2026-01-01', medecinNom: 'Dr X' }],
        ordonnances: [{ idOrdonnance: 1, medicaments: [{ nom: 'Paracétamol', quantite: 2 }] }],
        examens: [{ idExamen: 1, nomExamen: 'NFS' }],
        maladiesChroniques: 'Diabète',
        allergiesConnues: true,
        allergiesDetails: 'Arachides',
      };
      httpMock.get.mockReturnValueOnce(of(apiResp));

      const data = await firstValueFrom(service.getMonDossierMedical());
      expect(httpMock.get).toHaveBeenCalledWith(`${environment.apiUrl}/patient/dossier-medical`);
      expect(data.patient.idUser).toBe(1);
      expect(data.patient.nom).toBe('Dupont');
      expect(data.stats?.totalConsultations).toBe(1);
      expect(data.stats?.totalOrdonnances).toBe(1);
      expect(data.stats?.totalExamens).toBe(1);
      expect(data.stats?.derniereVisite).toBe('2026-01-01');
      expect(data.consultations[0].motif).toBe('Fièvre');
      expect(data.ordonnances[0].medicaments[0].dosage).toBe('2');
      expect(data.antecedents).toHaveLength(1);
      expect(data.allergies).toHaveLength(1);
      expect(data.allergies[0].allergene).toBe('Arachides');
    });

    it('handles empty arrays and missing data', async () => {
      httpMock.get.mockReturnValueOnce(of({ idUser: 2, nom: 'X', prenom: 'Y' }));
      const data = await firstValueFrom(service.getMonDossierMedical());
      expect(data.stats?.totalConsultations).toBe(0);
      expect(data.stats?.totalOrdonnances).toBe(0);
      expect(data.stats?.totalExamens).toBe(0);
      expect(data.stats?.derniereVisite).toBeUndefined();
      expect(data.antecedents).toEqual([]);
      expect(data.allergies).toEqual([]);
    });
  });

  describe('getDossierPatient', () => {
    it('GETs /consultation/dossier-patient/:id and maps medecin response', async () => {
      const apiResp = {
        idPatient: 3,
        nom: 'X',
        prenom: 'Y',
        numeroDossier: 'P3',
        consultations: [{ idConsultation: 2, motif: 'test', dateHeure: '2026-02-01' }],
        antecedentsFamiliaux: true,
        antecedentsFamiliauxDetails: 'Diabète familial',
        operationsChirurgicales: true,
        operationsDetails: 'Appendicectomie 2020',
      };
      httpMock.get.mockReturnValueOnce(of(apiResp));

      const data = await firstValueFrom(service.getDossierPatient(3));
      expect(httpMock.get).toHaveBeenCalledWith(`${environment.apiUrl}/consultation/dossier-patient/3`);
      expect(data.patient.idUser).toBe(3);
      expect(data.consultations).toHaveLength(1);
      expect(data.antecedents).toHaveLength(2);
      expect(data.antecedents[0].type).toBe('Antécédents familiaux');
      expect(data.antecedents[1].type).toBe('Opérations chirurgicales');
    });
  });

  describe('exam transformation urgent flag', () => {
    it('marks urgent when urgence is "urgente" or "critique"', async () => {
      httpMock.get.mockReturnValueOnce(
        of({
          idUser: 1,
          examens: [
            { idBulletinExamen: 1, urgence: 'urgente' },
            { idBulletinExamen: 2, urgence: 'critique' },
            { idBulletinExamen: 3, urgence: 'normal' },
          ],
        })
      );
      const data = await firstValueFrom(service.getMonDossierMedical());
      expect(data.examens[0].urgent).toBe(true);
      expect(data.examens[1].urgent).toBe(true);
      expect(data.examens[2].urgent).toBe(false);
    });
  });
});
