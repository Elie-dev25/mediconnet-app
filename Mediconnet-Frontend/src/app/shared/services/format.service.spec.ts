import { describe, it, expect, beforeEach } from 'vitest';
import { FormatService } from './format.service';

describe('FormatService', () => {
  let service: FormatService;

  beforeEach(() => {
    service = new FormatService();
  });

  describe('formatPrice', () => {
    it('should format a number with FCFA currency', () => {
      const result = service.formatPrice(10000);
      expect(result).toContain('FCFA');
      expect(result).toMatch(/10.000|10\s000/);
    });

    it('should return "-" for undefined', () => {
      expect(service.formatPrice(undefined)).toBe('-');
    });

    it('should return "-" for null', () => {
      expect(service.formatPrice(null as unknown as number)).toBe('-');
    });

    it('should handle zero', () => {
      expect(service.formatPrice(0)).toContain('FCFA');
    });
  });

  describe('formatDate', () => {
    it('should format an ISO string date', () => {
      const result = service.formatDate('2026-03-18');
      expect(result).toContain('2026');
      expect(result).toContain('mars');
    });

    it('should format a Date object', () => {
      const result = service.formatDate(new Date(2026, 2, 18));
      expect(result).toContain('2026');
    });

    it('should return "-" for undefined', () => {
      expect(service.formatDate(undefined)).toBe('-');
    });

    it('should return "-" for empty string', () => {
      expect(service.formatDate('')).toBe('-');
    });
  });

  describe('formatDateTime', () => {
    it('should format date and time', () => {
      const result = service.formatDateTime('2026-03-18T14:30:00');
      expect(result).toContain('2026');
      expect(result).toMatch(/\d{2}:\d{2}/);
    });

    it('should return "-" for undefined', () => {
      expect(service.formatDateTime(undefined)).toBe('-');
    });
  });

  describe('formatNumber', () => {
    it('should format number with thousands separator', () => {
      const result = service.formatNumber(1234567);
      expect(result).toMatch(/1.234.567|1\s234\s567/);
    });

    it('should return "-" for undefined', () => {
      expect(service.formatNumber(undefined)).toBe('-');
    });

    it('should return "-" for null', () => {
      expect(service.formatNumber(null as unknown as number)).toBe('-');
    });
  });

  describe('formatDateLong', () => {
    it('should include weekday in long format', () => {
      const result = service.formatDateLong('2026-03-18');
      expect(result).toContain('2026');
      expect(result).toContain('mars');
    });

    it('should return "-" for undefined', () => {
      expect(service.formatDateLong(undefined)).toBe('-');
    });
  });

  describe('formatDateShort', () => {
    it('should format date in DD/MM/YYYY format', () => {
      const result = service.formatDateShort('2026-03-18');
      expect(result).toMatch(/\d{2}\/\d{2}\/\d{4}/);
    });

    it('should return "-" for undefined', () => {
      expect(service.formatDateShort(undefined)).toBe('-');
    });
  });
});
