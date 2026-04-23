import { describe, it, expect } from 'vitest';
import {
  FrenchDatePipe,
  FrenchDateShortPipe,
  FrenchTimePipe,
  FrenchDateTimePipe,
  FrenchDateWeekdayPipe,
  RelativeTimePipe,
} from './date-format.pipe';

describe('Date format pipes', () => {
  it('FrenchDatePipe formats date', () => {
    const pipe = new FrenchDatePipe();
    expect(pipe.transform('2026-01-15')).toMatch(/2026/);
    expect(pipe.transform(null)).toBe('Non renseigné');
  });

  it('FrenchDateShortPipe formats short date', () => {
    const pipe = new FrenchDateShortPipe();
    expect(pipe.transform('2026-01-15')).toMatch(/2026/);
    expect(pipe.transform(undefined)).toBe('Non renseigné');
  });

  it('FrenchTimePipe formats time', () => {
    const pipe = new FrenchTimePipe();
    expect(pipe.transform('2026-01-15T14:30:00')).toMatch(/\d{2}:\d{2}/);
    expect(pipe.transform(null)).toBe('');
  });

  it('FrenchDateTimePipe formats date and time', () => {
    const pipe = new FrenchDateTimePipe();
    const result = pipe.transform('2026-01-15T14:30:00');
    expect(result).toMatch(/2026/);
    expect(pipe.transform(null)).toBe('Non renseigné');
  });

  it('FrenchDateWeekdayPipe formats with weekday', () => {
    const pipe = new FrenchDateWeekdayPipe();
    expect(pipe.transform('2026-01-15')).toMatch(/2026/);
    expect(pipe.transform(null)).toBe('Non renseigné');
  });

  it('RelativeTimePipe returns relative time', () => {
    const pipe = new RelativeTimePipe();
    expect(pipe.transform(new Date(Date.now() + 1000))).toBe("Aujourd'hui");
  });
});
