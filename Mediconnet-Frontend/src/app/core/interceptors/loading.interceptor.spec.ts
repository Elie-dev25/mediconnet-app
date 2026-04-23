import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpRequest, HttpResponse, HttpHeaders } from '@angular/common/http';
import { of, lastValueFrom } from 'rxjs';
import { loadingInterceptor } from './loading.interceptor';
import { LoadingService } from '../../services/loading.service';

describe('loadingInterceptor', () => {
  let loadingService: { show: ReturnType<typeof vi.fn>; hide: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    loadingService = { show: vi.fn(), hide: vi.fn() };
    TestBed.configureTestingModule({
      providers: [{ provide: LoadingService, useValue: loadingService }],
    });
  });

  afterEach(() => TestBed.resetTestingModule());

  it('shows loader then hides on success', async () => {
    const req = new HttpRequest('GET', '/api/x');
    const next = vi.fn(() => of(new HttpResponse({ status: 200 })));

    const result$ = TestBed.runInInjectionContext(() => loadingInterceptor(req, next));
    await lastValueFrom(result$);
    expect(loadingService.show).toHaveBeenCalled();
    expect(loadingService.hide).toHaveBeenCalled();
  });

  it('skips loader when X-Skip-Loader header is set and strips it', async () => {
    const req = new HttpRequest('GET', '/api/x', null, {
      headers: new HttpHeaders({ 'X-Skip-Loader': 'true' }),
    });
    const next = vi.fn((cleaned: HttpRequest<unknown>) => {
      expect(cleaned.headers.has('X-Skip-Loader')).toBe(false);
      return of(new HttpResponse({ status: 200 }));
    });

    const result$ = TestBed.runInInjectionContext(() => loadingInterceptor(req, next));
    await lastValueFrom(result$);
    expect(loadingService.show).not.toHaveBeenCalled();
    expect(loadingService.hide).not.toHaveBeenCalled();
  });
});
