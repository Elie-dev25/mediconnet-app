import { describe, it, expect, beforeEach } from 'vitest';
import { AuditService } from './audit.service';

describe('AuditService', () => {
  let service: AuditService;

  beforeEach(() => {
    // Pure methods don't need HttpClient; pass null cast for testing
    service = new AuditService(null as never);
  });

  describe('formatAction', () => {
    it('should return known label for known action', () => {
      expect(service.formatAction('LOGIN_SUCCESS')).toBe('Connexion réussie');
      expect(service.formatAction('LOGIN_FAILED')).toBe('Échec de connexion');
      expect(service.formatAction('LOGOUT')).toBe('Déconnexion');
      expect(service.formatAction('PROFILE_UPDATED')).toBe('Profil mis à jour');
    });

    it('should fallback to underscore-replaced string for unknown action', () => {
      expect(service.formatAction('CUSTOM_UNKNOWN_ACTION')).toBe('CUSTOM UNKNOWN ACTION');
    });

    it('should handle single word without underscore', () => {
      expect(service.formatAction('UNKNOWN')).toBe('UNKNOWN');
    });
  });

  describe('getActionBadgeClass', () => {
    it('should return badge-error when success=false regardless of action', () => {
      expect(service.getActionBadgeClass('LOGIN_SUCCESS', false)).toBe('badge-error');
      expect(service.getActionBadgeClass('ANYTHING', false)).toBe('badge-error');
    });

    it('should return badge-info for LOGIN/AUTH actions', () => {
      expect(service.getActionBadgeClass('LOGIN_SUCCESS', true)).toBe('badge-info');
      expect(service.getActionBadgeClass('AUTH_FAILURE', true)).toBe('badge-info');
    });

    it('should return badge-success for CREATE/ENREGISTR', () => {
      expect(service.getActionBadgeClass('PATIENT_CREATED', true)).toBe('badge-success');
      expect(service.getActionBadgeClass('CONSULTATION_ENREGISTREE', true)).toBe('badge-success');
    });

    it('should return badge-warning for UPDATE/CHANGE', () => {
      expect(service.getActionBadgeClass('PROFILE_UPDATED', true)).toBe('badge-warning');
      expect(service.getActionBadgeClass('PASSWORD_CHANGED', true)).toBe('badge-warning');
    });

    it('should return badge-error for DELETE/REMOVE', () => {
      expect(service.getActionBadgeClass('RECORD_DELETED', true)).toBe('badge-error');
      expect(service.getActionBadgeClass('ITEM_REMOVED', true)).toBe('badge-error');
    });

    it('should return badge-warning for SENSITIVE', () => {
      expect(service.getActionBadgeClass('SENSITIVE_DATA_ACCESS', true)).toBe('badge-warning');
    });

    it('should return badge-default for unknown actions', () => {
      expect(service.getActionBadgeClass('UNKNOWN_ACTION', true)).toBe('badge-default');
    });
  });
});
