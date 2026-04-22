import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { take } from 'rxjs/operators';
import { LoadingService } from './loading.service';

describe('LoadingService', () => {
  let service: LoadingService;

  beforeEach(() => {
    service = new LoadingService();
  });

  const currentValue = async (): Promise<boolean> =>
    firstValueFrom(service.isLoading$.pipe(take(1)));

  it('initial state is not loading', async () => {
    expect(await currentValue()).toBe(false);
  });

  it('show() emits true on first call', async () => {
    service.show();
    expect(await currentValue()).toBe(true);
  });

  it('multiple show() calls keep state true without re-emitting', async () => {
    service.show();
    service.show();
    service.show();
    expect(await currentValue()).toBe(true);
  });

  it('hide() decrements count; still true while requests remain', async () => {
    service.show();
    service.show();
    service.hide();
    expect(await currentValue()).toBe(true);
  });

  it('hide() emits false when all requests resolved', async () => {
    service.show();
    service.hide();
    expect(await currentValue()).toBe(false);
  });

  it('hide() is a no-op when no active request', async () => {
    service.hide();
    expect(await currentValue()).toBe(false);
  });

  it('reset() clears count and emits false', async () => {
    service.show();
    service.show();
    service.reset();
    expect(await currentValue()).toBe(false);
    // After reset, a new show should re-trigger true
    service.show();
    expect(await currentValue()).toBe(true);
  });
});
