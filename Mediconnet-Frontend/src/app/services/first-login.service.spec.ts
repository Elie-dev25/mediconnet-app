import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { FirstLoginService } from './first-login.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('FirstLoginService', () => {
  let service: FirstLoginService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/reception/first-login`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new FirstLoginService(httpMock.http);
  });

  it('getFirstLoginInfo GETs /info', async () => {
    await firstValueFrom(service.getFirstLoginInfo());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/info`);
  });

  it('validateFirstLogin POSTs /validate', async () => {
    const req = {
      declarationHonneurAcceptee: true,
      newPassword: 'newPass123',
      confirmPassword: 'newPass123',
    };
    await firstValueFrom(service.validateFirstLogin(req));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/validate`, req);
  });

  it('checkFirstLoginRequired GETs /check', async () => {
    await firstValueFrom(service.checkFirstLoginRequired());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/check`);
  });

  it('acceptDeclaration POSTs /accept-declaration', async () => {
    const req = { declarationHonneurAcceptee: true };
    await firstValueFrom(service.acceptDeclaration(req));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/accept-declaration`, req);
  });
});
