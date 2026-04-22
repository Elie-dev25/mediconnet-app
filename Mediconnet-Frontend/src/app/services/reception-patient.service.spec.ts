import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { ReceptionPatientService } from './reception-patient.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('ReceptionPatientService', () => {
  let service: ReceptionPatientService;
  let httpMock: ReturnType<typeof createHttpClientMock>;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new ReceptionPatientService(httpMock.http);
  });

  it('createPatient POSTs to /reception/patients', async () => {
    const req = {
      nom: 'Dupont',
      prenom: 'Jean',
      dateNaissance: '1980-01-01',
      sexe: 'M',
      telephone: '612345678',
      adresse: 'Yaoundé',
    };
    await firstValueFrom(service.createPatient(req));
    expect(httpMock.post).toHaveBeenCalledWith(`${environment.apiUrl}/reception/patients`, req);
  });
});
