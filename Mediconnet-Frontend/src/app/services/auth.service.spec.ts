import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { firstValueFrom, of, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { createHttpClientMock } from '../../test-helpers';

// Helper: build a JWT-like token with optional exp (in seconds)
const buildJwt = (exp?: number): string => {
  const payload = exp !== undefined ? { exp } : {};
  const b64 = (o: unknown) => btoa(JSON.stringify(o));
  return `header.${b64(payload)}.sig`;
};

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  let router: { navigate: ReturnType<typeof vi.fn> };
  let notificationService: { startConnection: ReturnType<typeof vi.fn>; stopConnection: ReturnType<typeof vi.fn> };
  let soundService: { initAfterUserInteraction: ReturnType<typeof vi.fn> };
  let statePreservationService: {
    saveBeforeIdleLogout: ReturnType<typeof vi.fn>;
    clearAllSessionData: ReturnType<typeof vi.fn>;
    getRedirectUrl: ReturnType<typeof vi.fn>;
    redirectAfterLogin: ReturnType<typeof vi.fn>;
  };
  let idleService: {
    configure: ReturnType<typeof vi.fn>;
    startWatching: ReturnType<typeof vi.fn>;
    stopWatching: ReturnType<typeof vi.fn>;
  };

  const createService = () => {
    return new AuthService(
      httpMock.http,
      router as never,
      notificationService as never,
      soundService as never,
      statePreservationService as never,
      idleService as never
    );
  };

  beforeEach(() => {
    localStorage.clear();
    httpMock = createHttpClientMock();
    router = { navigate: vi.fn() };
    notificationService = { startConnection: vi.fn(), stopConnection: vi.fn() };
    soundService = { initAfterUserInteraction: vi.fn() };
    statePreservationService = {
      saveBeforeIdleLogout: vi.fn(),
      clearAllSessionData: vi.fn(),
      getRedirectUrl: vi.fn(),
      redirectAfterLogin: vi.fn(),
    };
    idleService = { configure: vi.fn(), startWatching: vi.fn(), stopWatching: vi.fn() };
    service = createService();
  });

  afterEach(() => localStorage.clear());

  describe('constructor / loadUserFromStorage', () => {
    it('does nothing when no token in storage', () => {
      expect(service.isAuthenticated()).toBe(false);
      expect(service.getCurrentUser()).toBeNull();
    });

    it('restores session from storage with valid token', () => {
      const token = buildJwt(Date.now() / 1000 + 3600);
      localStorage.setItem('auth_token', token);
      localStorage.setItem('auth_user', JSON.stringify({ email: 'a@b.c' }));
      const s = createService();
      expect(s.isAuthenticated()).toBe(true);
      expect(s.getCurrentUser()).toEqual({ email: 'a@b.c' });
      expect(notificationService.startConnection).toHaveBeenCalledWith(token);
    });

    it('clears storage when token is expired', () => {
      localStorage.setItem('auth_token', buildJwt(Date.now() / 1000 - 3600));
      localStorage.setItem('auth_user', JSON.stringify({ email: 'x' }));
      createService();
      expect(localStorage.getItem('auth_token')).toBeNull();
      expect(localStorage.getItem('auth_user')).toBeNull();
    });

    it('clears storage when user JSON is corrupted', () => {
      localStorage.setItem('auth_token', buildJwt(Date.now() / 1000 + 3600));
      localStorage.setItem('auth_user', 'not-json');
      createService();
      expect(localStorage.getItem('auth_token')).toBeNull();
    });

    it('clears storage when token exists but user missing', () => {
      localStorage.setItem('auth_token', buildJwt(Date.now() / 1000 + 3600));
      createService();
      expect(localStorage.getItem('auth_token')).toBeNull();
    });

    it('treats malformed token as expired', () => {
      localStorage.setItem('auth_token', 'not-a-jwt');
      localStorage.setItem('auth_user', JSON.stringify({ email: 'x' }));
      createService();
      expect(localStorage.getItem('auth_token')).toBeNull();
    });

    it('treats token without exp as expired', () => {
      localStorage.setItem('auth_token', buildJwt());
      localStorage.setItem('auth_user', JSON.stringify({ email: 'x' }));
      createService();
      expect(localStorage.getItem('auth_token')).toBeNull();
    });
  });

  describe('login', () => {
    it('stores token+user, starts connections, and emits authenticated', async () => {
      const token = buildJwt(Date.now() / 1000 + 3600);
      httpMock.post.mockReturnValueOnce(of({
        token,
        idUser: '1',
        nom: 'Dupont',
        prenom: 'Jean',
        email: 'a@b.c',
        role: 'medecin',
      }));
      const res = await firstValueFrom(service.login({ identifier: 'a@b.c', password: 'x' }));
      expect(res.token).toBe(token);
      expect(localStorage.getItem('auth_token')).toBe(token);
      expect(service.isAuthenticated()).toBe(true);
      expect(notificationService.startConnection).toHaveBeenCalledWith(token);
      expect(soundService.initAfterUserInteraction).toHaveBeenCalled();
      expect(idleService.startWatching).toHaveBeenCalled();
    });

    it('does not store token when response has no token (email not confirmed)', async () => {
      httpMock.post.mockReturnValueOnce(of({ message: 'Check email', nom: 'X', prenom: 'Y', email: 'e', role: 'patient', idUser: '1' } as never));
      await firstValueFrom(service.login({ identifier: 'a', password: 'b' }));
      expect(localStorage.getItem('auth_token')).toBeNull();
      expect(service.isAuthenticated()).toBe(false);
    });

    it('maps 401 errors to "Identifiants incorrects"', async () => {
      httpMock.post.mockReturnValueOnce(throwError(() => ({ status: 401 })));
      await expect(firstValueFrom(service.login({ identifier: 'a', password: 'b' }))).rejects.toThrow(/Identifiants incorrects/);
    });

    it('maps 409/400 errors to duplicate message', async () => {
      httpMock.post.mockReturnValueOnce(throwError(() => ({ status: 409 })));
      await expect(firstValueFrom(service.login({ identifier: 'a', password: 'b' }))).rejects.toThrow(/email ou numéro/);
    });

    it('uses server error message when available', async () => {
      httpMock.post.mockReturnValueOnce(throwError(() => ({ status: 500, error: { message: 'Server down' } })));
      await expect(firstValueFrom(service.login({ identifier: 'a', password: 'b' }))).rejects.toThrow('Server down');
    });

    it('handles ErrorEvent instances', async () => {
      const errorEvent = new ErrorEvent('fail', { message: 'Network fail' });
      httpMock.post.mockReturnValueOnce(throwError(() => ({ error: errorEvent })));
      await expect(firstValueFrom(service.login({ identifier: 'a', password: 'b' }))).rejects.toThrow('Network fail');
    });

    it('falls back to generic message when no info provided', async () => {
      httpMock.post.mockReturnValueOnce(throwError(() => ({ status: 500 })));
      await expect(firstValueFrom(service.login({ identifier: 'a', password: 'b' }))).rejects.toThrow(/Une erreur est survenue/);
    });
  });

  describe('register', () => {
    it('calls /register and handles success', async () => {
      const token = buildJwt(Date.now() / 1000 + 3600);
      httpMock.post.mockReturnValueOnce(of({ token, idUser: '1', nom: 'N', prenom: 'P', email: 'e', role: 'patient' }));
      await firstValueFrom(service.register({ firstName: 'a', lastName: 'b', email: 'e', telephone: 't', password: 'p', confirmPassword: 'p' }));
      expect(localStorage.getItem('auth_token')).toBe(token);
    });

    it('propagates errors', async () => {
      httpMock.post.mockReturnValueOnce(throwError(() => ({ status: 400 })));
      await expect(firstValueFrom(service.register({}))).rejects.toThrow();
    });
  });

  describe('resendConfirmationEmail', () => {
    it('POSTs /resend-confirmation', async () => {
      httpMock.post.mockReturnValueOnce(of({ ok: true }));
      await firstValueFrom(service.resendConfirmationEmail('a@b.c'));
      expect(httpMock.post).toHaveBeenCalledWith('/api/auth/resend-confirmation', { email: 'a@b.c' });
    });
  });

  describe('logout', () => {
    it('clears storage and navigates to / on voluntary logout', () => {
      localStorage.setItem('auth_token', 'x');
      localStorage.setItem('auth_user', 'y');
      service.logout();
      expect(localStorage.getItem('auth_token')).toBeNull();
      expect(localStorage.getItem('auth_user')).toBeNull();
      expect(statePreservationService.clearAllSessionData).toHaveBeenCalled();
      expect(router.navigate).toHaveBeenCalledWith(['/']);
    });

    it('preserves state and navigates with sessionExpired=true on idle logout', () => {
      service.logout(true);
      expect(statePreservationService.saveBeforeIdleLogout).toHaveBeenCalled();
      expect(router.navigate).toHaveBeenCalledWith(['/auth/login'], { queryParams: { sessionExpired: 'true' } });
    });

    it('logoutDueToInactivity preserves state', () => {
      service.logoutDueToInactivity();
      expect(statePreservationService.saveBeforeIdleLogout).toHaveBeenCalled();
    });
  });

  describe('misc', () => {
    it('getToken returns null when no token', () => {
      expect(service.getToken()).toBeNull();
    });

    it('hasRedirectUrl returns boolean', () => {
      statePreservationService.getRedirectUrl.mockReturnValue('/x');
      expect(service.hasRedirectUrl()).toBe(true);
      statePreservationService.getRedirectUrl.mockReturnValue(null);
      expect(service.hasRedirectUrl()).toBe(false);
    });

    it('redirectAfterLogin delegates', () => {
      service.redirectAfterLogin();
      expect(statePreservationService.redirectAfterLogin).toHaveBeenCalled();
    });

    it('updateProfileCompleted updates user when present', () => {
      service['currentUserSubject'].next({ email: 'a' });
      service.updateProfileCompleted(true);
      expect(service.getCurrentUser().profileCompleted).toBe(true);
      expect(JSON.parse(localStorage.getItem('auth_user')!).profileCompleted).toBe(true);
    });

    it('updateProfileCompleted is no-op when no user', () => {
      expect(() => service.updateProfileCompleted(true)).not.toThrow();
    });

    it('updateToken stores new token and resets flags', () => {
      service['currentUserSubject'].next({ email: 'a', mustChangePassword: true, declarationHonneurAcceptee: false });
      service.updateToken('new-token');
      expect(localStorage.getItem('auth_token')).toBe('new-token');
      expect(service.getCurrentUser().mustChangePassword).toBe(false);
      expect(service.getCurrentUser().declarationHonneurAcceptee).toBe(true);
    });

    it('updateToken without user still updates token', () => {
      service.updateToken('t2');
      expect(localStorage.getItem('auth_token')).toBe('t2');
    });

    it('updateUserInfo merges partial updates', () => {
      service['currentUserSubject'].next({ email: 'a' });
      service.updateUserInfo({ profileCompleted: true });
      expect(service.getCurrentUser().profileCompleted).toBe(true);
    });

    it('updateUserInfo no-op without user', () => {
      expect(() => service.updateUserInfo({ profileCompleted: true })).not.toThrow();
    });

    it('startIdleWatching configures when authenticated', () => {
      localStorage.setItem('auth_token', buildJwt(Date.now() / 1000 + 3600));
      service.startIdleWatching();
      expect(idleService.configure).toHaveBeenCalled();
      expect(idleService.startWatching).toHaveBeenCalled();
    });

    it('startIdleWatching no-op when not authenticated', () => {
      service.startIdleWatching();
      expect(idleService.configure).not.toHaveBeenCalled();
    });
  });

  describe('changePassword', () => {
    it('POSTs /change-password on success', async () => {
      httpMock.post.mockReturnValueOnce(of({ success: true, message: 'ok' }));
      const res = await firstValueFrom(
        service.changePassword({ currentPassword: 'a', newPassword: 'b', confirmNewPassword: 'b' })
      );
      expect(res.success).toBe(true);
      expect(httpMock.post).toHaveBeenCalledWith('/api/auth/change-password', {
        currentPassword: 'a',
        newPassword: 'b',
        confirmNewPassword: 'b',
      });
    });

    it('rethrows server error body when present', async () => {
      httpMock.post.mockReturnValueOnce(throwError(() => ({ error: { success: false, message: 'bad' } })));
      await expect(
        firstValueFrom(service.changePassword({ currentPassword: 'a', newPassword: 'b', confirmNewPassword: 'b' }))
      ).rejects.toMatchObject({ message: 'bad' });
    });

    it('falls back to generic error when no body', async () => {
      httpMock.post.mockReturnValueOnce(throwError(() => ({})));
      await expect(
        firstValueFrom(service.changePassword({ currentPassword: 'a', newPassword: 'b', confirmNewPassword: 'b' }))
      ).rejects.toMatchObject({ message: expect.any(String) });
    });
  });

  describe('checkPasswordStrength', () => {
    it('POSTs /check-password-strength', async () => {
      httpMock.post.mockReturnValueOnce(of({ isValid: true, score: 4, strengthLevel: 'strong', errors: [], criteria: {} }));
      await firstValueFrom(service.checkPasswordStrength('abc'));
      expect(httpMock.post).toHaveBeenCalledWith('/api/auth/check-password-strength', { password: 'abc' });
    });

    it('rethrows error body', async () => {
      httpMock.post.mockReturnValueOnce(throwError(() => ({ error: { isValid: false, errors: ['X'] } })));
      await expect(firstValueFrom(service.checkPasswordStrength('abc'))).rejects.toMatchObject({ isValid: false });
    });

    it('falls back when no body', async () => {
      httpMock.post.mockReturnValueOnce(throwError(() => ({})));
      await expect(firstValueFrom(service.checkPasswordStrength('abc'))).rejects.toMatchObject({ isValid: false });
    });
  });
});
