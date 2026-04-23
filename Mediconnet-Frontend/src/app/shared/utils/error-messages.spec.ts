import { describe, it, expect } from 'vitest';
import {
  ErrorMessages,
  extractErrorMessage,
  champObligatoire,
  longueurMaxDepassee,
  valeurHorsLimites,
  fichierTropVolumineux,
} from './error-messages';

describe('error-messages', () => {
  describe('extractErrorMessage', () => {
    it('returns default when error is null/undefined', () => {
      expect(extractErrorMessage(null)).toBe(ErrorMessages.RESEAU.ERREUR_SERVEUR);
      expect(extractErrorMessage(undefined)).toBe(ErrorMessages.RESEAU.ERREUR_SERVEUR);
    });
    it('returns custom default when provided', () => {
      expect(extractErrorMessage(null, 'X')).toBe('X');
    });
    it('extracts string error body', () => {
      expect(extractErrorMessage({ error: 'raw msg' })).toBe('raw msg');
    });
    it('extracts error.error.message', () => {
      expect(extractErrorMessage({ error: { message: 'inner' } })).toBe('inner');
    });
    it('extracts first validation error (ASP.NET errors object)', () => {
      expect(extractErrorMessage({ error: { errors: { field1: ['bad'], other: ['ignored'] } } })).toBe('bad');
    });
    it('falls back to error.message', () => {
      expect(extractErrorMessage({ message: 'top-level' })).toBe('top-level');
    });
    it('maps HTTP statuses', () => {
      expect(extractErrorMessage({ status: 400 })).toBe('Requête invalide');
      expect(extractErrorMessage({ status: 401 })).toBe(ErrorMessages.AUTH.NON_CONNECTE);
      expect(extractErrorMessage({ status: 403 })).toBe(ErrorMessages.AUTH.NON_AUTORISE);
      expect(extractErrorMessage({ status: 404 })).toBe('Ressource non trouvée');
      expect(extractErrorMessage({ status: 408 })).toBe(ErrorMessages.RESEAU.TIMEOUT);
      expect(extractErrorMessage({ status: 500 })).toBe(ErrorMessages.RESEAU.ERREUR_SERVEUR);
      expect(extractErrorMessage({ status: 503 })).toBe('Service temporairement indisponible');
      expect(extractErrorMessage({ status: 999 })).toBe(ErrorMessages.RESEAU.ERREUR_SERVEUR);
    });
    it('returns default when no useful data', () => {
      expect(extractErrorMessage({})).toBe(ErrorMessages.RESEAU.ERREUR_SERVEUR);
    });
    it('handles error.errors without array (no match)', () => {
      // first key value is not array => falls through to message, then status, then default
      expect(extractErrorMessage({ error: { errors: { field1: 'bad' } } })).toBe(ErrorMessages.RESEAU.ERREUR_SERVEUR);
    });
  });

  describe('generators', () => {
    it('champObligatoire', () => {
      expect(champObligatoire('Nom')).toBe('Le champ "Nom" est obligatoire');
    });
    it('longueurMaxDepassee', () => {
      expect(longueurMaxDepassee('Nom', 100)).toBe('Le champ "Nom" ne doit pas dépasser 100 caractères');
    });
    it('valeurHorsLimites', () => {
      expect(valeurHorsLimites('Age', 0, 120)).toBe('La valeur de "Age" doit être entre 0 et 120');
    });
    it('fichierTropVolumineux', () => {
      expect(fichierTropVolumineux(5)).toBe('Le fichier ne doit pas dépasser 5 Mo');
    });
  });
});
