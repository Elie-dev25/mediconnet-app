import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { authGuard, roleGuard, profileCompleteGuard } from './auth.guard';
import { AuthService } from '../../services/auth.service';

describe('auth.guard', () => {
  let authService: { isAuthenticated: ReturnType<typeof vi.fn>; getCurrentUser: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    authService = { isAuthenticated: vi.fn(), getCurrentUser: vi.fn() };
    router = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
      ],
    });
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  const execGuard = (fn: () => boolean): boolean => {
    return TestBed.runInInjectionContext(fn) as boolean;
  };

  describe('authGuard', () => {
    it('returns true when authenticated', () => {
      authService.isAuthenticated.mockReturnValue(true);
      const result = execGuard(() => authGuard({} as never, {} as never) as boolean);
      expect(result).toBe(true);
    });

    it('navigates to /login and returns false when not authenticated', () => {
      authService.isAuthenticated.mockReturnValue(false);
      const result = execGuard(() => authGuard({} as never, {} as never) as boolean);
      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('roleGuard', () => {
    it('redirects to /login when not authenticated', () => {
      authService.isAuthenticated.mockReturnValue(false);
      const guard = roleGuard(['medecin']);
      const result = execGuard(() => guard({} as never, {} as never) as boolean);
      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('returns true when user role matches allowed roles', () => {
      authService.isAuthenticated.mockReturnValue(true);
      authService.getCurrentUser.mockReturnValue({ role: 'medecin' });
      const guard = roleGuard(['medecin', 'admin']);
      const result = execGuard(() => guard({} as never, {} as never) as boolean);
      expect(result).toBe(true);
    });

    it('redirects patient to first-login when declaration not accepted', () => {
      authService.isAuthenticated.mockReturnValue(true);
      authService.getCurrentUser.mockReturnValue({ role: 'patient', mustChangePassword: true, declarationHonneurAcceptee: false });
      const guard = roleGuard(['patient']);
      const result = execGuard(() => guard({} as never, {} as never) as boolean);
      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/auth/first-login']);
    });

    it('redirects patient to change-password when declaration accepted but password not changed', () => {
      authService.isAuthenticated.mockReturnValue(true);
      authService.getCurrentUser.mockReturnValue({ role: 'patient', mustChangePassword: true, declarationHonneurAcceptee: true });
      const guard = roleGuard(['patient']);
      const result = execGuard(() => guard({} as never, {} as never) as boolean);
      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/auth/change-password']);
    });

    it('redirects to role dashboard when role not in allowed list', () => {
      authService.isAuthenticated.mockReturnValue(true);
      authService.getCurrentUser.mockReturnValue({ role: 'medecin' });
      const guard = roleGuard(['admin']);
      const result = execGuard(() => guard({} as never, {} as never) as boolean);
      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/medecin/dashboard']);
    });

    it('falls back to /login when role unknown', () => {
      authService.isAuthenticated.mockReturnValue(true);
      authService.getCurrentUser.mockReturnValue({ role: 'alien' });
      const guard = roleGuard(['admin']);
      const result = execGuard(() => guard({} as never, {} as never) as boolean);
      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('handles no user object', () => {
      authService.isAuthenticated.mockReturnValue(true);
      authService.getCurrentUser.mockReturnValue(null);
      const guard = roleGuard(['admin']);
      const result = execGuard(() => guard({} as never, {} as never) as boolean);
      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('profileCompleteGuard', () => {
    it('redirects to /login when not authenticated', () => {
      authService.isAuthenticated.mockReturnValue(false);
      const result = execGuard(() => profileCompleteGuard({} as never, {} as never) as boolean);
      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('returns true when authenticated', () => {
      authService.isAuthenticated.mockReturnValue(true);
      const result = execGuard(() => profileCompleteGuard({} as never, {} as never) as boolean);
      expect(result).toBe(true);
    });
  });
});
