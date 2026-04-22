import { describe, it, expect, beforeEach } from 'vitest';
import { FormControl } from '@angular/forms';
import { PhoneValidationService } from './phone-validation.service';

describe('PhoneValidationService', () => {
  let service: PhoneValidationService;

  beforeEach(() => {
    service = new PhoneValidationService();
  });

  describe('getCountries & getCountryByCode', () => {
    it('should return all supported countries', () => {
      const countries = service.getCountries();
      expect(countries.length).toBeGreaterThan(0);
      expect(countries.map(c => c.code)).toContain('CM');
      expect(countries.map(c => c.code)).toContain('FR');
    });

    it('should find country by ISO code', () => {
      const cm = service.getCountryByCode('CM');
      expect(cm).toBeDefined();
      expect(cm?.dialCode).toBe('+237');
    });

    it('should return undefined for unknown country code', () => {
      expect(service.getCountryByCode('XX')).toBeUndefined();
    });

    it('should return Cameroon as default country', () => {
      expect(service.getDefaultCountry().code).toBe('CM');
    });
  });

  describe('validate - Cameroon', () => {
    it('should validate a valid Cameroon number', () => {
      const result = service.validate('612345678', 'CM');
      expect(result.isValid).toBe(true);
      expect(result.fullNumber).toBe('+237612345678');
    });

    it('should reject empty number', () => {
      const result = service.validate('', 'CM');
      expect(result.isValid).toBe(false);
      expect(result.errorMessage).toContain('requis');
    });

    it('should reject number with wrong length', () => {
      const result = service.validate('61234', 'CM');
      expect(result.isValid).toBe(false);
      expect(result.errorMessage).toContain('9 chiffres');
    });

    it('should reject Cameroon number not starting with 6', () => {
      const result = service.validate('712345678', 'CM');
      expect(result.isValid).toBe(false);
      expect(result.errorMessage).toContain('commencer par 6');
    });

    it('should strip non-digit characters before validation', () => {
      const result = service.validate('6 12 34 56 78', 'CM');
      expect(result.isValid).toBe(true);
    });
  });

  describe('validate - unsupported country', () => {
    it('should return error for unsupported country', () => {
      const result = service.validate('123', 'XX');
      expect(result.isValid).toBe(false);
      expect(result.errorMessage).toBe('Pays non supporté');
    });
  });

  describe('validate - France', () => {
    it('should validate valid French number', () => {
      const result = service.validate('612345678', 'FR');
      expect(result.isValid).toBe(true);
      expect(result.fullNumber).toBe('+33612345678');
    });

    it('should reject French number starting with 0', () => {
      const result = service.validate('012345678', 'FR');
      expect(result.isValid).toBe(false);
    });
  });

  describe('format', () => {
    it('should format Cameroon number', () => {
      const formatted = service.format('612345678', 'CM');
      expect(formatted).toBe('6 12 34 56 78');
    });

    it('should return original for unknown country', () => {
      expect(service.format('12345', 'XX')).toBe('12345');
    });
  });

  describe('getFullNumber', () => {
    it('should prepend dial code', () => {
      expect(service.getFullNumber('612345678', 'CM')).toBe('+237612345678');
    });

    it('should return original for unknown country', () => {
      expect(service.getFullNumber('123', 'XX')).toBe('123');
    });
  });

  describe('extractLocalNumber', () => {
    it('should extract local number from full Cameroon number', () => {
      const result = service.extractLocalNumber('+237612345678');
      expect(result).toEqual({ countryCode: 'CM', localNumber: '612345678' });
    });

    it('should return null for unrecognized format', () => {
      expect(service.extractLocalNumber('+99912345')).toBeNull();
    });
  });

  describe('createValidator', () => {
    it('should return null for empty control (handled by required)', () => {
      const validator = service.createValidator('CM');
      expect(validator(new FormControl(''))).toBeNull();
    });

    it('should return null for valid number', () => {
      const validator = service.createValidator('CM');
      expect(validator(new FormControl('612345678'))).toBeNull();
    });

    it('should return error object for invalid number', () => {
      const validator = service.createValidator('CM');
      const result = validator(new FormControl('71234'));
      expect(result).not.toBeNull();
      expect(result?.['phoneInvalid']).toBe(true);
    });
  });
});
