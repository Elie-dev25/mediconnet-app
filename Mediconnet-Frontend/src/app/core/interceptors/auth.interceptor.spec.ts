import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpRequest, HttpResponse, HttpErrorResponse } from '@angular/common/http';
import { lastValueFrom, of, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { StatePreservationService } from '../services/state-preservation.service';

describe('authInterceptor', () => {
  let router: { navigate: ReturnType<typeof vi.fn>; url: string };
  let stateService: { saveRedirectUrl: ReturnType<typeof vi.fn>; saveState: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    localStorage.clear();
    router = { navigate: vi.fn(), url: '/medecin/dashboard' };
    stateService = { saveRedirectUrl: vi.fn(), saveState: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: router },
        { provide: StatePreservationService, useValue: stateService },
      ],
    });
  });

  afterEach(async () => {
    // Wait for the interceptor's internal 1000ms setTimeout to reset its
    // module-level `isRedirecting` flag so that subsequent tests run with a fresh state.
    await new Promise<void>((resolve) => setTimeout(resolve, 1100));
    localStorage.clear();
    TestBed.resetTestingModule();
  });

  it('attaches Authorization header when token present', async () => {
    localStorage.setItem('auth_token', 'T123');
    const req = new HttpRequest('GET', '/api/x');
    const next = vi.fn((r: HttpRequest<unknown>) => {
      expect(r.headers.get('Authorization')).toBe('Bearer T123');
      return of(new HttpResponse({ status: 200 }));
    });
    const result$ = TestBed.runInInjectionContext(() => authInterceptor(req, next));
    await lastValueFrom(result$);
    expect(next).toHaveBeenCalled();
  });

  it('does not attach Authorization header without token', async () => {
    const req = new HttpRequest('GET', '/api/x');
    const next = vi.fn((r: HttpRequest<unknown>) => {
      expect(r.headers.has('Authorization')).toBe(false);
      return of(new HttpResponse({ status: 200 }));
    });
    const result$ = TestBed.runInInjectionContext(() => authInterceptor(req, next));
    await lastValueFrom(result$);
  });

  it('on 401: clears storage tokens and rethrows error', async () => {
    localStorage.setItem('auth_token', 'T');
    localStorage.setItem('auth_user', 'U');
    router.url = '/medecin/dashboard';
    const err = new HttpErrorResponse({ status: 401 });
    const next = vi.fn(() => throwError(() => err));
    const result$ = TestBed.runInInjectionContext(() => authInterceptor(new HttpRequest('GET', '/api/x'), next));
    await expect(lastValueFrom(result$)).rejects.toBe(err);
    expect(localStorage.getItem('auth_token')).toBeNull();
    expect(localStorage.getItem('auth_user')).toBeNull();
  });

  it('on non-401 error: just rethrows', async () => {
    const err = new HttpErrorResponse({ status: 500 });
    const next = vi.fn(() => throwError(() => err));
    const result$ = TestBed.runInInjectionContext(() => authInterceptor(new HttpRequest('GET', '/api/x'), next));
    await expect(lastValueFrom(result$)).rejects.toBe(err);
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
