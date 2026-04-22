import { describe, it, expect, beforeEach, vi } from 'vitest';
import { Router } from '@angular/router';
import { AuthNavigationService } from './auth-navigation.service';

describe('AuthNavigationService', () => {
  let service: AuthNavigationService;
  let router: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    router = { navigate: vi.fn() };
    service = new AuthNavigationService(router as unknown as Router);
  });

  it('navigateToLanding navigates to /', () => {
    service.navigateToLanding();
    expect(router.navigate).toHaveBeenCalledWith(['/']);
  });

  it('navigateToLogin navigates to /login', () => {
    service.navigateToLogin();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('navigateToRegister navigates to /register', () => {
    service.navigateToRegister();
    expect(router.navigate).toHaveBeenCalledWith(['/register']);
  });

  it('goBack navigates to /', () => {
    service.goBack();
    expect(router.navigate).toHaveBeenCalledWith(['/']);
  });
});
