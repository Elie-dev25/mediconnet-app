import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { of } from 'rxjs';
import { PatientProfileService } from './patient-profile.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('PatientProfileService', () => {
  let service: PatientProfileService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/patient/profile`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new PatientProfileService(httpMock.http);
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('getProfile GETs /profile and pushes to profile$', async () => {
    const profileData = { idUser: 1, nom: 'X', prenom: 'Y' } as never;
    httpMock.get.mockReturnValueOnce(of(profileData));
    const received: unknown[] = [];
    service.profile$.subscribe(v => received.push(v));
    await firstValueFrom(service.getProfile());
    expect(httpMock.get).toHaveBeenCalledWith(base);
    expect(received[received.length - 1]).toBe(profileData);
  });

  it('checkProfileStatus GETs /profile/status and pushes to profileStatus$', async () => {
    const status = { isComplete: true, missingFields: [], message: 'ok' };
    httpMock.get.mockReturnValueOnce(of(status));
    const received: unknown[] = [];
    service.profileStatus$.subscribe(v => received.push(v));
    await firstValueFrom(service.checkProfileStatus());
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/status`);
    expect(received[received.length - 1]).toEqual(status);
  });

  it('updateProfile PUTs /profile and refreshes profile when complete', async () => {
    httpMock.put.mockReturnValueOnce(of({ message: 'ok', isComplete: true }));
    httpMock.get.mockReturnValueOnce(of({} as never));
    const received: unknown[] = [];
    service.profileStatus$.subscribe(v => received.push(v));
    await firstValueFrom(service.updateProfile({ telephone: '612345678' }));
    expect(httpMock.put).toHaveBeenCalledWith(base, { telephone: '612345678' });
    expect(httpMock.get).toHaveBeenCalled();
    const last = received[received.length - 1] as { isComplete?: boolean };
    expect(last.isComplete).toBe(true);
  });

  it('updateProfile does not push when incomplete', async () => {
    httpMock.put.mockReturnValueOnce(of({ message: 'ok', isComplete: false }));
    httpMock.get.mockReturnValueOnce(of({} as never));
    const received: unknown[] = [];
    service.profileStatus$.subscribe(v => received.push(v));
    await firstValueFrom(service.updateProfile({}));
    // Only the initial null emission
    expect(received).toEqual([null]);
  });

  it('hasShownProfileAlert returns false when nothing stored', () => {
    expect(service.hasShownProfileAlert()).toBe(false);
  });

  it('markProfileAlertShown then hasShownProfileAlert is true', () => {
    service.markProfileAlertShown();
    expect(service.hasShownProfileAlert()).toBe(true);
  });

  it('resetProfileAlert clears the flag', () => {
    service.markProfileAlertShown();
    service.resetProfileAlert();
    expect(service.hasShownProfileAlert()).toBe(false);
  });
});
