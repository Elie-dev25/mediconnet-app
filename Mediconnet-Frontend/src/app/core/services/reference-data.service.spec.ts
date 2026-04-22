import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { ReferenceDataService } from './reference-data.service';
import { createHttpClientMock } from '../../../test-helpers';
import { environment } from '../../../environments/environment';

describe('ReferenceDataService', () => {
  let service: ReferenceDataService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/reference`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new ReferenceDataService(httpMock.http);
  });

  it('getAllReferenceData GETs /all and caches subsequent calls', async () => {
    await firstValueFrom(service.getAllReferenceData());
    await firstValueFrom(service.getAllReferenceData());
    // shareReplay ensures the HTTP call occurs only once
    expect(httpMock.get).toHaveBeenCalledTimes(1);
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/all`);
  });

  it('invalidateCache forces a new HTTP call', async () => {
    await firstValueFrom(service.getAllReferenceData());
    service.invalidateCache();
    await firstValueFrom(service.getAllReferenceData());
    expect(httpMock.get).toHaveBeenCalledTimes(2);
  });

  it('getTypesPrestations GETs /types-prestation', async () => {
    await firstValueFrom(service.getTypesPrestations());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/types-prestation`);
  });

  it('getCategoriesBeneficiaires GETs /categories-beneficiaires', async () => {
    await firstValueFrom(service.getCategoriesBeneficiaires());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/categories-beneficiaires`);
  });

  it('getModesPaiement GETs /modes-paiement', async () => {
    await firstValueFrom(service.getModesPaiement());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/modes-paiement`);
  });

  it('getZonesCouverture GETs /zones-couverture', async () => {
    await firstValueFrom(service.getZonesCouverture());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/zones-couverture`);
  });

  it('getTypesCouvertureSante GETs /types-couverture-sante', async () => {
    await firstValueFrom(service.getTypesCouvertureSante());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/types-couverture-sante`);
  });
});
