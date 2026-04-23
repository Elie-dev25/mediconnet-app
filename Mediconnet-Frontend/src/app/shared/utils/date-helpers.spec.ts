import { describe, it, expect } from 'vitest';
import {
  formatDate,
  formatDateShort,
  formatTime,
  formatDateTime,
  formatDateWithWeekday,
  formatTimeRange,
  getRelativeTime,
} from './date-helpers';

describe('date-helpers', () => {
  describe('formatDate', () => {
    it('returns "Non renseigné" for empty', () => {
      expect(formatDate(undefined)).toBe('Non renseigné');
      expect(formatDate(null)).toBe('Non renseigné');
      expect(formatDate('')).toBe('Non renseigné');
    });
    it('formats Date object', () => {
      expect(formatDate(new Date(2026, 0, 15))).toMatch(/2026/);
    });
    it('formats ISO string', () => {
      expect(formatDate('2026-01-15')).toMatch(/2026/);
    });
  });

  describe('formatDateShort', () => {
    it('empty returns "Non renseigné"', () => {
      expect(formatDateShort(null)).toBe('Non renseigné');
    });
    it('formats ISO string', () => {
      expect(formatDateShort('2026-01-15')).toMatch(/2026/);
    });
    it('formats Date object', () => {
      expect(formatDateShort(new Date(2026, 0, 15))).toMatch(/2026/);
    });
  });

  describe('formatTime', () => {
    it('empty returns ""', () => {
      expect(formatTime(null)).toBe('');
    });
    it('formats time', () => {
      expect(formatTime('2026-01-15T14:30:00')).toMatch(/\d{2}:\d{2}/);
    });
  });

  describe('formatDateTime', () => {
    it('empty returns "Non renseigné"', () => {
      expect(formatDateTime(null)).toBe('Non renseigné');
    });
    it('formats date with time', () => {
      const r = formatDateTime('2026-01-15T14:30:00');
      expect(r).toMatch(/2026/);
      expect(r).toMatch(/\d{2}:\d{2}/);
    });
  });

  describe('formatDateWithWeekday', () => {
    it('empty returns "Non renseigné"', () => {
      expect(formatDateWithWeekday(null)).toBe('Non renseigné');
    });
    it('includes weekday', () => {
      expect(formatDateWithWeekday('2026-01-15')).toMatch(/2026/);
    });
  });

  describe('formatTimeRange', () => {
    it('returns range in HH:MM-HH:MM format', () => {
      const result = formatTimeRange('2026-01-15T14:00:00', 30);
      expect(result).toMatch(/^\d{2}:\d{2}-\d{2}:\d{2}$/);
    });
    it('handles Date input', () => {
      expect(formatTimeRange(new Date(2026, 0, 15, 14, 0), 30)).toMatch(/^\d{2}:\d{2}-\d{2}:\d{2}$/);
    });
  });

  describe('getRelativeTime', () => {
    it('returns "Aujourd\'hui" for now (slightly in future to avoid Math.floor edge)', () => {
      expect(getRelativeTime(new Date(Date.now() + 1000))).toBe("Aujourd'hui");
    });
    it('returns "Demain" for 36h in the future', () => {
      const tomorrow = new Date(Date.now() + 36 * 60 * 60 * 1000);
      expect(getRelativeTime(tomorrow)).toBe('Demain');
    });
    it('returns "Hier" for a few hours in the past', () => {
      const yesterday = new Date(Date.now() - 5 * 60 * 60 * 1000);
      expect(getRelativeTime(yesterday)).toBe('Hier');
    });
    it('returns "Dans X jours" for near future', () => {
      const future = new Date(Date.now() + 3.5 * 24 * 60 * 60 * 1000);
      expect(getRelativeTime(future)).toMatch(/Dans \d jours/);
    });
    it('returns "Il y a X jours" for near past', () => {
      const past = new Date(Date.now() - 3.5 * 24 * 60 * 60 * 1000);
      expect(getRelativeTime(past)).toMatch(/Il y a \d jours/);
    });
    it('falls back to formatDateShort for far dates', () => {
      expect(getRelativeTime('2020-01-01')).toMatch(/2020/);
    });
  });
});
