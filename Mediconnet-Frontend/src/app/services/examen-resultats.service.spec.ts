import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { ExamenResultatsService } from './examen-resultats.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('ExamenResultatsService', () => {
  let service: ExamenResultatsService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/examens/resultats`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new ExamenResultatsService(httpMock.http);
  });

  describe('HTTP methods', () => {
    it('getResultatExamen GETs /:id', async () => {
      await firstValueFrom(service.getResultatExamen(42));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/42`);
    });

    it('downloadDocument GETs with responseType blob', async () => {
      await firstValueFrom(service.downloadDocument(5, 'uuid-123'));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/5/documents/uuid-123/download`, {
        responseType: 'blob',
      });
    });

    it('getMesResultats uses default pagination', async () => {
      await firstValueFrom(service.getMesResultats());
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/patient/mes-resultats`, {
        params: { page: '1', pageSize: '20' },
      });
    });

    it('getMesResultats uses custom pagination', async () => {
      await firstValueFrom(service.getMesResultats(3, 50));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/patient/mes-resultats`, {
        params: { page: '3', pageSize: '50' },
      });
    });

    it('getResultatsPatient uses patient id and pagination', async () => {
      await firstValueFrom(service.getResultatsPatient(7, 2, 25));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/medecin/patient/7`, {
        params: { page: '2', pageSize: '25' },
      });
    });
  });

  describe('formatFileSize', () => {
    it('returns 0 B for zero bytes', () => {
      expect(service.formatFileSize(0)).toBe('0 B');
    });

    it('formats bytes', () => {
      expect(service.formatFileSize(500)).toMatch(/500 B/);
    });

    it('formats kilobytes', () => {
      expect(service.formatFileSize(1536)).toMatch(/1\.5 KB/);
    });

    it('formats megabytes', () => {
      expect(service.formatFileSize(2 * 1024 * 1024)).toMatch(/2 MB/);
    });

    it('formats gigabytes', () => {
      expect(service.formatFileSize(3 * 1024 * 1024 * 1024)).toMatch(/3 GB/);
    });
  });

  describe('getFileIcon', () => {
    it('returns image for image/* mime', () => {
      expect(service.getFileIcon('image/png')).toBe('image');
      expect(service.getFileIcon('image/jpeg')).toBe('image');
    });

    it('returns file-text for application/pdf', () => {
      expect(service.getFileIcon('application/pdf')).toBe('file-text');
    });

    it('returns file-text for word documents', () => {
      expect(service.getFileIcon('application/msword')).toBe('file-text');
      expect(service.getFileIcon('application/wordprocessingml')).toBe('file-text');
    });

    it('returns file-spreadsheet for excel / spreadsheet', () => {
      expect(service.getFileIcon('application/vnd.ms-excel')).toBe('file-spreadsheet');
      expect(service.getFileIcon('application/spreadsheet')).toBe('file-spreadsheet');
    });

    it('returns file for unknown mime', () => {
      expect(service.getFileIcon('application/octet-stream')).toBe('file');
    });
  });
});
