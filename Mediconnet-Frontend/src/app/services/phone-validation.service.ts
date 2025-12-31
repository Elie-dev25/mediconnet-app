import { Injectable } from '@angular/core';
import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Configuration d'un pays pour la validation téléphonique
 */
export interface PhoneCountryConfig {
  code: string;           // Code ISO du pays (CM, FR, CI, etc.)
  name: string;           // Nom du pays
  dialCode: string;       // Indicatif téléphonique (+237, +33, etc.)
  flag: string;           // Emoji du drapeau
  pattern: RegExp;        // Pattern de validation (sans indicatif)
  length: number;         // Longueur du numéro (sans indicatif)
  placeholder: string;    // Placeholder pour l'input
  format: (value: string) => string; // Fonction de formatage
}

/**
 * Résultat de validation d'un numéro de téléphone
 */
export interface PhoneValidationResult {
  isValid: boolean;
  formattedNumber: string;
  fullNumber: string;     // Avec indicatif
  errorMessage?: string;
}

@Injectable({
  providedIn: 'root'
})
export class PhoneValidationService {
  
  /**
   * Configuration des pays supportés
   */
  private readonly countries: PhoneCountryConfig[] = [
    {
      code: 'CM',
      name: 'Cameroun',
      dialCode: '+237',
      flag: '🇨🇲',
      pattern: /^6[0-9]{8}$/,
      length: 9,
      placeholder: '6 XX XX XX XX',
      format: (value: string) => {
        const cleaned = value.replace(/\D/g, '');
        if (cleaned.length <= 1) return cleaned;
        if (cleaned.length <= 3) return `${cleaned.slice(0, 1)} ${cleaned.slice(1)}`;
        if (cleaned.length <= 5) return `${cleaned.slice(0, 1)} ${cleaned.slice(1, 3)} ${cleaned.slice(3)}`;
        if (cleaned.length <= 7) return `${cleaned.slice(0, 1)} ${cleaned.slice(1, 3)} ${cleaned.slice(3, 5)} ${cleaned.slice(5)}`;
        return `${cleaned.slice(0, 1)} ${cleaned.slice(1, 3)} ${cleaned.slice(3, 5)} ${cleaned.slice(5, 7)} ${cleaned.slice(7, 9)}`;
      }
    },
    {
      code: 'FR',
      name: 'France',
      dialCode: '+33',
      flag: '🇫🇷',
      pattern: /^[1-9][0-9]{8}$/,
      length: 9,
      placeholder: 'X XX XX XX XX',
      format: (value: string) => {
        const cleaned = value.replace(/\D/g, '');
        if (cleaned.length <= 1) return cleaned;
        if (cleaned.length <= 3) return `${cleaned.slice(0, 1)} ${cleaned.slice(1)}`;
        if (cleaned.length <= 5) return `${cleaned.slice(0, 1)} ${cleaned.slice(1, 3)} ${cleaned.slice(3)}`;
        if (cleaned.length <= 7) return `${cleaned.slice(0, 1)} ${cleaned.slice(1, 3)} ${cleaned.slice(3, 5)} ${cleaned.slice(5)}`;
        return `${cleaned.slice(0, 1)} ${cleaned.slice(1, 3)} ${cleaned.slice(3, 5)} ${cleaned.slice(5, 7)} ${cleaned.slice(7, 9)}`;
      }
    },
    {
      code: 'CI',
      name: 'Côte d\'Ivoire',
      dialCode: '+225',
      flag: '🇨🇮',
      pattern: /^[0-9]{10}$/,
      length: 10,
      placeholder: 'XX XX XX XX XX',
      format: (value: string) => {
        const cleaned = value.replace(/\D/g, '');
        if (cleaned.length <= 2) return cleaned;
        if (cleaned.length <= 4) return `${cleaned.slice(0, 2)} ${cleaned.slice(2)}`;
        if (cleaned.length <= 6) return `${cleaned.slice(0, 2)} ${cleaned.slice(2, 4)} ${cleaned.slice(4)}`;
        if (cleaned.length <= 8) return `${cleaned.slice(0, 2)} ${cleaned.slice(2, 4)} ${cleaned.slice(4, 6)} ${cleaned.slice(6)}`;
        return `${cleaned.slice(0, 2)} ${cleaned.slice(2, 4)} ${cleaned.slice(4, 6)} ${cleaned.slice(6, 8)} ${cleaned.slice(8, 10)}`;
      }
    },
    {
      code: 'SN',
      name: 'Sénégal',
      dialCode: '+221',
      flag: '🇸🇳',
      pattern: /^7[0-9]{8}$/,
      length: 9,
      placeholder: '7X XXX XX XX',
      format: (value: string) => {
        const cleaned = value.replace(/\D/g, '');
        if (cleaned.length <= 2) return cleaned;
        if (cleaned.length <= 5) return `${cleaned.slice(0, 2)} ${cleaned.slice(2)}`;
        if (cleaned.length <= 7) return `${cleaned.slice(0, 2)} ${cleaned.slice(2, 5)} ${cleaned.slice(5)}`;
        return `${cleaned.slice(0, 2)} ${cleaned.slice(2, 5)} ${cleaned.slice(5, 7)} ${cleaned.slice(7, 9)}`;
      }
    },
    {
      code: 'GA',
      name: 'Gabon',
      dialCode: '+241',
      flag: '🇬🇦',
      pattern: /^[0-9]{7,8}$/,
      length: 8,
      placeholder: 'XX XX XX XX',
      format: (value: string) => {
        const cleaned = value.replace(/\D/g, '');
        if (cleaned.length <= 2) return cleaned;
        if (cleaned.length <= 4) return `${cleaned.slice(0, 2)} ${cleaned.slice(2)}`;
        if (cleaned.length <= 6) return `${cleaned.slice(0, 2)} ${cleaned.slice(2, 4)} ${cleaned.slice(4)}`;
        return `${cleaned.slice(0, 2)} ${cleaned.slice(2, 4)} ${cleaned.slice(4, 6)} ${cleaned.slice(6, 8)}`;
      }
    },
    {
      code: 'CD',
      name: 'RD Congo',
      dialCode: '+243',
      flag: '🇨🇩',
      pattern: /^[89][0-9]{8}$/,
      length: 9,
      placeholder: 'X XX XXX XXXX',
      format: (value: string) => {
        const cleaned = value.replace(/\D/g, '');
        if (cleaned.length <= 1) return cleaned;
        if (cleaned.length <= 3) return `${cleaned.slice(0, 1)} ${cleaned.slice(1)}`;
        if (cleaned.length <= 6) return `${cleaned.slice(0, 1)} ${cleaned.slice(1, 3)} ${cleaned.slice(3)}`;
        return `${cleaned.slice(0, 1)} ${cleaned.slice(1, 3)} ${cleaned.slice(3, 6)} ${cleaned.slice(6, 9)}`;
      }
    }
  ];

  /**
   * Récupère tous les pays supportés
   */
  getCountries(): PhoneCountryConfig[] {
    return [...this.countries];
  }

  /**
   * Récupère un pays par son code ISO
   */
  getCountryByCode(code: string): PhoneCountryConfig | undefined {
    return this.countries.find(c => c.code === code);
  }

  /**
   * Récupère le pays par défaut (Cameroun)
   */
  getDefaultCountry(): PhoneCountryConfig {
    return this.countries.find(c => c.code === 'CM')!;
  }

  /**
   * Valide un numéro de téléphone pour un pays donné
   */
  validate(phoneNumber: string, countryCode: string = 'CM'): PhoneValidationResult {
    const country = this.getCountryByCode(countryCode);
    
    if (!country) {
      return {
        isValid: false,
        formattedNumber: phoneNumber,
        fullNumber: phoneNumber,
        errorMessage: 'Pays non supporté'
      };
    }

    // Nettoyer le numéro (garder uniquement les chiffres)
    const cleaned = phoneNumber.replace(/\D/g, '');

    // Vérifier la longueur
    if (cleaned.length === 0) {
      return {
        isValid: false,
        formattedNumber: '',
        fullNumber: '',
        errorMessage: 'Numéro de téléphone requis'
      };
    }

    if (cleaned.length !== country.length) {
      return {
        isValid: false,
        formattedNumber: country.format(cleaned),
        fullNumber: `${country.dialCode}${cleaned}`,
        errorMessage: `Le numéro doit contenir ${country.length} chiffres`
      };
    }

    // Vérifier le format
    if (!country.pattern.test(cleaned)) {
      return {
        isValid: false,
        formattedNumber: country.format(cleaned),
        fullNumber: `${country.dialCode}${cleaned}`,
        errorMessage: this.getFormatErrorMessage(countryCode)
      };
    }

    return {
      isValid: true,
      formattedNumber: country.format(cleaned),
      fullNumber: `${country.dialCode}${cleaned}`
    };
  }

  /**
   * Formatte un numéro pour un pays donné
   */
  format(phoneNumber: string, countryCode: string = 'CM'): string {
    const country = this.getCountryByCode(countryCode);
    if (!country) return phoneNumber;
    
    const cleaned = phoneNumber.replace(/\D/g, '');
    return country.format(cleaned);
  }

  /**
   * Récupère le numéro complet avec indicatif
   */
  getFullNumber(phoneNumber: string, countryCode: string = 'CM'): string {
    const country = this.getCountryByCode(countryCode);
    if (!country) return phoneNumber;
    
    const cleaned = phoneNumber.replace(/\D/g, '');
    return `${country.dialCode}${cleaned}`;
  }

  /**
   * Extrait le numéro local depuis un numéro complet
   */
  extractLocalNumber(fullNumber: string): { countryCode: string; localNumber: string } | null {
    for (const country of this.countries) {
      if (fullNumber.startsWith(country.dialCode)) {
        return {
          countryCode: country.code,
          localNumber: fullNumber.replace(country.dialCode, '')
        };
      }
    } 
    return null;
  }

  /**
   * Crée un validateur Angular pour les formulaires réactifs
   */
  createValidator(countryCode: string = 'CM'): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null; // Laisser le validateur required gérer ce cas
      }

      const result = this.validate(control.value, countryCode);
      
      if (!result.isValid) {
        return { 
          phoneInvalid: true,
          message: result.errorMessage 
        };
      }
      
      return null;
    };
  }

  /**
   * Message d'erreur spécifique au format du pays
   */
  private getFormatErrorMessage(countryCode: string): string {
    switch (countryCode) {
      case 'CM':
        return 'Le numéro doit commencer par 6 (ex: 6XX XX XX XX)';
      case 'FR':
        return 'Format invalide (ex: 6 XX XX XX XX)';
      case 'SN':
        return 'Le numéro doit commencer par 7';
      case 'CD':
        return 'Le numéro doit commencer par 8 ou 9';
      default:
        return 'Format de numéro invalide';
    }
  }
}
