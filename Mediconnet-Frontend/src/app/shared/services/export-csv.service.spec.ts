import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { ExportCsvService } from './export-csv.service';

describe('ExportCsvService', () => {
  let service: ExportCsvService;
  let createObjectURL: ReturnType<typeof vi.fn>;
  let revokeObjectURL: ReturnType<typeof vi.fn>;
  let clickSpy: ReturnType<typeof vi.fn>;
  let appendSpy: ReturnType<typeof vi.fn>;
  let removeSpy: ReturnType<typeof vi.fn>;
  const originalURL = (globalThis as { URL?: unknown }).URL;
  const originalBlob = (globalThis as { Blob?: unknown }).Blob;
  const originalDocument = (globalThis as { document?: unknown }).document;

  beforeEach(() => {
    service = new ExportCsvService();
    createObjectURL = vi.fn(() => 'blob:fake-url');
    revokeObjectURL = vi.fn();
    clickSpy = vi.fn();
    appendSpy = vi.fn();
    removeSpy = vi.fn();

    (globalThis as { URL: unknown }).URL = { createObjectURL, revokeObjectURL };
    (globalThis as { Blob: unknown }).Blob = class {
      parts: unknown[];
      opts: unknown;
      constructor(parts: unknown[], opts: unknown) {
        this.parts = parts;
        this.opts = opts;
      }
    };

    const fakeLink = {
      download: '',
      style: { visibility: '' } as { visibility: string },
      setAttribute: vi.fn(),
      click: clickSpy,
    };
    const doc = {
      createElement: vi.fn(() => fakeLink),
      body: { appendChild: appendSpy, removeChild: removeSpy },
    };
    (globalThis as { document: unknown }).document = doc;
  });

  afterEach(() => {
    vi.restoreAllMocks();
    (globalThis as { URL?: unknown }).URL = originalURL;
    (globalThis as { Blob?: unknown }).Blob = originalBlob;
    (globalThis as { document?: unknown }).document = originalDocument;
  });

  it('returns early and warns for empty data', () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    service.exportToCsv([]);
    expect(warn).toHaveBeenCalled();
    expect(clickSpy).not.toHaveBeenCalled();
  });

  it('downloads CSV with default options', () => {
    service.exportToCsv([{ a: 1, b: 2 }]);
    expect(createObjectURL).toHaveBeenCalled();
    expect(clickSpy).toHaveBeenCalled();
    expect(appendSpy).toHaveBeenCalled();
    expect(removeSpy).toHaveBeenCalled();
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:fake-url');
  });

  it('uses headers from options', () => {
    service.exportToCsv([{ a: 1, b: 2 }], { headers: ['a'], filename: 'x.csv' });
    expect(clickSpy).toHaveBeenCalled();
  });

  it('escapes values containing separator / quotes / newlines', () => {
    // Capture the blob content
    let capturedContent: string | undefined;
    (globalThis as { Blob: unknown }).Blob = class {
      constructor(parts: string[]) {
        capturedContent = parts[0];
      }
    };
    service.exportToCsv([{ a: 'hello, world', b: 'say "hi"', c: 'line1\nline2' }]);
    expect(capturedContent).toContain('"hello, world"');
    expect(capturedContent).toContain('"say ""hi"""');
    expect(capturedContent).toContain('"line1\nline2"');
  });

  it('handles undefined values as empty strings', () => {
    let capturedContent: string | undefined;
    (globalThis as { Blob: unknown }).Blob = class {
      constructor(parts: string[]) {
        capturedContent = parts[0];
      }
    };
    service.exportToCsv([{ a: 1, b: undefined as unknown }]);
    expect(capturedContent).toMatch(/\n1,$/);
  });

  it('falls back to console.error when browser does not support download', () => {
    const fakeLink = {
      style: { visibility: '' },
      setAttribute: vi.fn(),
      click: clickSpy,
      // Note: no `download` property
    };
    (globalThis as { document: unknown }).document = {
      createElement: vi.fn(() => fakeLink),
      body: { appendChild: appendSpy, removeChild: removeSpy },
    };
    const err = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    service.exportToCsv([{ a: 1 }]);
    expect(err).toHaveBeenCalled();
  });

  it('exportHistoriqueMouvements calls exportToCsv with dated filename', () => {
    const spy = vi.spyOn(service, 'exportToCsv').mockImplementation(() => undefined);
    service.exportHistoriqueMouvements([{ id: 1 }]);
    expect(spy).toHaveBeenCalledWith(
      [{ id: 1 }],
      expect.objectContaining({ filename: expect.stringMatching(/^historique-mouvements-\d{8}\.csv$/) })
    );
  });

  it('exportHistoriqueCommandes uses commandes headers', () => {
    const spy = vi.spyOn(service, 'exportToCsv').mockImplementation(() => undefined);
    service.exportHistoriqueCommandes([{ id: 1 }]);
    expect(spy).toHaveBeenCalledWith(
      [{ id: 1 }],
      expect.objectContaining({ filename: expect.stringMatching(/^historique-commandes-\d{8}\.csv$/) })
    );
  });

  it('exportHistoriqueDispensations uses dispensations headers', () => {
    const spy = vi.spyOn(service, 'exportToCsv').mockImplementation(() => undefined);
    service.exportHistoriqueDispensations([{ id: 1 }]);
    expect(spy).toHaveBeenCalledWith(
      [{ id: 1 }],
      expect.objectContaining({ filename: expect.stringMatching(/^historique-dispensations-\d{8}\.csv$/) })
    );
  });

  it('exportHistoriqueConsolide uses consolide headers', () => {
    const spy = vi.spyOn(service, 'exportToCsv').mockImplementation(() => undefined);
    service.exportHistoriqueConsolide([{ id: 1 }]);
    expect(spy).toHaveBeenCalledWith(
      [{ id: 1 }],
      expect.objectContaining({ filename: expect.stringMatching(/^historique-consolide-\d{8}\.csv$/) })
    );
  });
});
