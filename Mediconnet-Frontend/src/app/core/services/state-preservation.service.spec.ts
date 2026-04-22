import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { Subject } from 'rxjs';
import { NavigationEnd, type Router } from '@angular/router';
import { StatePreservationService } from './state-preservation.service';

describe('StatePreservationService', () => {
  let service: StatePreservationService;
  let events$: Subject<unknown>;
  let router: { events: Subject<unknown>; navigateByUrl: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    events$ = new Subject<unknown>();
    router = { events: events$, navigateByUrl: vi.fn() };
    service = new StatePreservationService(router as unknown as Router);
    sessionStorage.clear();
  });

  afterEach(() => {
    sessionStorage.clear();
    vi.restoreAllMocks();
  });

  const triggerNav = (url: string) => {
    events$.next(new NavigationEnd(1, url, url));
  };

  describe('redirect URL', () => {
    it('saveRedirectUrl stores protected route', () => {
      triggerNav('/dashboard');
      service.saveRedirectUrl();
      expect(service.getRedirectUrl()).toBe('/dashboard');
    });

    it('saveRedirectUrl does not store public routes', () => {
      service.saveRedirectUrl('/auth/login');
      expect(service.getRedirectUrl()).toBeNull();
    });

    it('saveRedirectUrl accepts explicit url', () => {
      service.saveRedirectUrl('/patients/42');
      expect(service.getRedirectUrl()).toBe('/patients/42');
    });

    it('clearRedirectUrl removes stored url', () => {
      service.saveRedirectUrl('/x');
      service.clearRedirectUrl();
      expect(service.getRedirectUrl()).toBeNull();
    });
  });

  describe('state save/restore', () => {
    it('saveState persists current url and timestamp', () => {
      triggerNav('/foo');
      service.saveState({ extra: 1 });
      const state = service.getState();
      expect(state?.url).toBe('/foo');
      expect(state?.customData).toEqual({ extra: 1 });
    });

    it('getState returns null when nothing stored', () => {
      expect(service.getState()).toBeNull();
    });

    it('getState returns null and clears when expired', () => {
      sessionStorage.setItem(
        'preserved_state',
        JSON.stringify({ url: '/x', timestamp: Date.now() - (31 * 60 * 1000) })
      );
      expect(service.getState()).toBeNull();
      expect(sessionStorage.getItem('preserved_state')).toBeNull();
    });

    it('getState returns null on invalid JSON', () => {
      sessionStorage.setItem('preserved_state', '{invalid json');
      expect(service.getState()).toBeNull();
    });

    it('clearState removes stored state', () => {
      triggerNav('/foo');
      service.saveState();
      service.clearState();
      expect(service.getState()).toBeNull();
    });
  });

  describe('form data', () => {
    it('saveFormData then getFormData roundtrip', () => {
      service.saveFormData('f1', { a: 1 });
      expect(service.getFormData('f1')).toEqual({ a: 1 });
    });

    it('getFormData returns null if nothing stored', () => {
      expect(service.getFormData('missing')).toBeNull();
    });

    it('getFormData returns null and clears when expired', () => {
      sessionStorage.setItem(
        'form_f2',
        JSON.stringify({ data: { a: 1 }, timestamp: Date.now() - 31 * 60 * 1000 })
      );
      expect(service.getFormData('f2')).toBeNull();
      expect(sessionStorage.getItem('form_f2')).toBeNull();
    });

    it('getFormData handles invalid JSON', () => {
      sessionStorage.setItem('form_bad', '{bad');
      expect(service.getFormData('bad')).toBeNull();
    });

    it('clearFormData removes stored form', () => {
      service.saveFormData('f3', { a: 1 });
      service.clearFormData('f3');
      expect(service.getFormData('f3')).toBeNull();
    });
  });

  describe('workflow step', () => {
    it('save/getWorkflowStep roundtrip', () => {
      service.saveWorkflowStep('w1', 3, { data: 'x' });
      expect(service.getWorkflowStep('w1')).toEqual({ step: 3, data: { data: 'x' } });
    });

    it('getWorkflowStep returns null if nothing', () => {
      expect(service.getWorkflowStep('missing')).toBeNull();
    });

    it('getWorkflowStep returns null on expired entry', () => {
      sessionStorage.setItem(
        'workflow_w2',
        JSON.stringify({ step: 2, data: null, timestamp: Date.now() - 31 * 60 * 1000 })
      );
      expect(service.getWorkflowStep('w2')).toBeNull();
    });

    it('getWorkflowStep handles invalid JSON', () => {
      sessionStorage.setItem('workflow_bad', '{bad');
      expect(service.getWorkflowStep('bad')).toBeNull();
    });

    it('clearWorkflowStep removes stored step', () => {
      service.saveWorkflowStep('w3', 1);
      service.clearWorkflowStep('w3');
      expect(service.getWorkflowStep('w3')).toBeNull();
    });
  });

  describe('redirectAfterLogin', () => {
    it('navigates to stored URL and clears it', () => {
      service.saveRedirectUrl('/patients');
      service.redirectAfterLogin();
      expect(router.navigateByUrl).toHaveBeenCalledWith('/patients');
      expect(service.getRedirectUrl()).toBeNull();
    });

    it('does nothing when no URL stored', () => {
      service.redirectAfterLogin();
      expect(router.navigateByUrl).not.toHaveBeenCalled();
    });

    it('does not navigate to public route', () => {
      sessionStorage.setItem('redirect_after_login', '/auth/login');
      service.redirectAfterLogin();
      expect(router.navigateByUrl).not.toHaveBeenCalled();
    });
  });

  describe('restoreScrollPosition', () => {
    it('calls window.scrollTo after delay', () => {
      vi.useFakeTimers();
      const scrollSpy = vi.spyOn(window, 'scrollTo').mockImplementation(() => undefined);
      service.restoreScrollPosition({
        url: '/',
        timestamp: Date.now(),
        scrollPosition: { x: 10, y: 20 },
      });
      vi.advanceTimersByTime(101);
      expect(scrollSpy).toHaveBeenCalledWith(10, 20);
      vi.useRealTimers();
    });

    it('does nothing when scrollPosition missing', () => {
      vi.useFakeTimers();
      const scrollSpy = vi.spyOn(window, 'scrollTo').mockImplementation(() => undefined);
      service.restoreScrollPosition({ url: '/', timestamp: Date.now() });
      vi.advanceTimersByTime(101);
      expect(scrollSpy).not.toHaveBeenCalled();
      vi.useRealTimers();
    });
  });

  describe('saveBeforeIdleLogout + clearAllSessionData', () => {
    it('saveBeforeIdleLogout saves redirect and state', () => {
      triggerNav('/hospitalisations');
      service.saveBeforeIdleLogout();
      expect(service.getRedirectUrl()).toBe('/hospitalisations');
      expect(service.getState()).not.toBeNull();
    });

    it('clearAllSessionData removes all tracked keys only', () => {
      service.saveFormData('f1', { a: 1 });
      service.saveWorkflowStep('w1', 1);
      sessionStorage.setItem('unrelated', 'keep me');
      service.saveRedirectUrl('/x');
      service.clearAllSessionData();
      expect(service.getFormData('f1')).toBeNull();
      expect(service.getWorkflowStep('w1')).toBeNull();
      expect(service.getRedirectUrl()).toBeNull();
      expect(sessionStorage.getItem('unrelated')).toBe('keep me');
    });
  });

  it('tracks currentUrl from NavigationEnd events', () => {
    triggerNav('/dashboard/patients');
    service.saveRedirectUrl();
    expect(service.getRedirectUrl()).toBe('/dashboard/patients');
  });
});
