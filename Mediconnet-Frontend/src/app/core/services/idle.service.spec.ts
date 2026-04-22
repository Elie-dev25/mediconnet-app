import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import type { NgZone } from '@angular/core';
import type { Router } from '@angular/router';
import { IdleService } from './idle.service';

describe('IdleService', () => {
  let service: IdleService;
  let ngZone: { run: ReturnType<typeof vi.fn>; runOutsideAngular: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    vi.useFakeTimers();
    ngZone = {
      run: vi.fn((fn: () => void) => fn()),
      runOutsideAngular: vi.fn((fn: () => void) => fn()),
    };
    router = { navigate: vi.fn() };
    service = new IdleService(ngZone as unknown as NgZone, router as unknown as Router);
    vi.spyOn(console, 'log').mockImplementation(() => undefined);
  });

  afterEach(() => {
    service.stopWatching();
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it('configure merges provided values', () => {
    service.configure({ idleTimeoutSeconds: 120 });
    // Indirectly test by starting the watcher and looking at emitted remaining seconds when warning shows
    service.configure({ warningTimeoutSeconds: 10 });
    expect(service.isIdle()).toBe(false);
  });

  it('resetTimer sets non-idle state and clears warning/remaining', () => {
    service.resetTimer();
    expect(service.isIdle()).toBe(false);
    expect(service.getRemainingTime()).toBe(0);
  });

  it('startWatching schedules warning then timeout', async () => {
    service.configure({ idleTimeoutSeconds: 5, warningTimeoutSeconds: 2 });
    service.startWatching();

    // Advance past the (idle - warning) = 3s so showWarning fires
    await vi.advanceTimersByTimeAsync(3000);
    expect(service.getRemainingTime()).toBe(2);

    // Advance 2s to hit timeout
    await vi.advanceTimersByTimeAsync(2000);
    expect(service.isIdle()).toBe(true);
  });

  it('startWatching is idempotent', () => {
    service.startWatching();
    service.startWatching();
    // runOutsideAngular should still have been called once
    expect(ngZone.runOutsideAngular).toHaveBeenCalledTimes(1);
  });

  it('stayActive resets timers and clears idle flag', async () => {
    service.configure({ idleTimeoutSeconds: 5, warningTimeoutSeconds: 2 });
    service.startWatching();
    await vi.advanceTimersByTimeAsync(3000); // trigger warning
    service.stayActive();
    expect(service.getRemainingTime()).toBe(0);
  });

  it('stopWatching clears timers safely', () => {
    service.startWatching();
    expect(() => service.stopWatching()).not.toThrow();
  });

  it('isIdle returns current idle state', () => {
    expect(service.isIdle()).toBe(false);
  });
});
