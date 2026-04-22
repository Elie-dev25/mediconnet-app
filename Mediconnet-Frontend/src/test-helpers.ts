import { HttpClient, HttpParams } from '@angular/common/http';
import { vi } from 'vitest';
import { of } from 'rxjs';

/**
 * Builds a minimal HttpClient mock with vi.fn() for all common methods.
 * Each method returns an Observable of the mock value or `undefined` by default.
 */
export function createHttpClientMock() {
  const get = vi.fn((_url: string, _opts?: unknown) => of(null as unknown));
  const post = vi.fn((_url: string, _body: unknown, _opts?: unknown) => of(null as unknown));
  const put = vi.fn((_url: string, _body: unknown, _opts?: unknown) => of(null as unknown));
  const patch = vi.fn((_url: string, _body: unknown, _opts?: unknown) => of(null as unknown));
  const del = vi.fn((_url: string, _opts?: unknown) => of(null as unknown));
  const request = vi.fn((_method: string, _url: string, _opts?: unknown) => of(null as unknown));

  const mock = {
    get,
    post,
    put,
    patch,
    delete: del,
    request,
  } as unknown as HttpClient;

  return { http: mock, get, post, put, patch, delete: del, request };
}

/** Safely extracts the HttpParams from an options arg. */
export function paramsFromCall(call: unknown[]): HttpParams | undefined {
  const opts = call.find((a): a is { params?: HttpParams } => {
    return typeof a === 'object' && a !== null && 'params' in a;
  });
  return opts?.params;
}
