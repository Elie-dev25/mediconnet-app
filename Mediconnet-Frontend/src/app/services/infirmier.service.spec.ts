import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { InfirmierService } from './infirmier.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('InfirmierService', () => {
  let service: InfirmierService;
  let httpMock: ReturnType<typeof createHttpClientMock>;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new InfirmierService(httpMock.http);
  });

  it('getFileAttente calls the correct endpoint', async () => {
    await firstValueFrom(service.getFileAttente());
    expect(httpMock.get).toHaveBeenCalledWith(`${environment.apiUrl}/infirmier/file-attente`);
  });
});
