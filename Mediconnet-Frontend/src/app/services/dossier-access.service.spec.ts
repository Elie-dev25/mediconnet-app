import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { DossierAccessService } from './dossier-access.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('DossierAccessService', () => {
  let service: DossierAccessService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const baseUrl = `${environment.apiUrl}/medecin/dossier-access`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new DossierAccessService(httpMock.http);
  });

  it('sendValidationCode posts patient id', async () => {
    await firstValueFrom(service.sendValidationCode(42));
    expect(httpMock.post).toHaveBeenCalledWith(`${baseUrl}/send-code`, { idPatient: 42 });
  });

  it('verifyCode posts patient id and code', async () => {
    await firstValueFrom(service.verifyCode(7, 'ABC123'));
    expect(httpMock.post).toHaveBeenCalledWith(`${baseUrl}/verify-code`, {
      idPatient: 7,
      code: 'ABC123',
    });
  });
});
