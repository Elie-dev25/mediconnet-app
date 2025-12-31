import { Component, forwardRef, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { 
  ControlValueAccessor, 
  NG_VALUE_ACCESSOR, 
  NG_VALIDATORS,
  Validator,
  AbstractControl,
  ValidationErrors,
  FormsModule
} from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { PhoneValidationService, PhoneCountryConfig } from '../../../services/phone-validation.service';

/**
 * Composant partagé pour la saisie de numéros de téléphone
 * avec sélection du pays et validation automatique
 */
@Component({
  selector: 'app-phone-input',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './phone-input.component.html',
  styleUrls: ['./phone-input.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => PhoneInputComponent),
      multi: true
    },
    {
      provide: NG_VALIDATORS,
      useExisting: forwardRef(() => PhoneInputComponent),
      multi: true
    }
  ]
})
export class PhoneInputComponent implements ControlValueAccessor, Validator, OnInit, OnDestroy {
  
  @Input() label: string = 'Téléphone';
  @Input() required: boolean = false;
  @Input() disabled: boolean = false;
  @Input() showLabel: boolean = true;
  @Input() showIcon: boolean = true;
  @Input() defaultCountry: string = 'CM';
  
  // État interne
  phoneNumber: string = '';
  selectedCountryCode: string = 'CM';
  selectedCountry: PhoneCountryConfig | undefined;
  countries: PhoneCountryConfig[] = [];
  
  isDropdownOpen: boolean = false;
  isTouched: boolean = false;
  errorMessage: string | null = null;
  
  // Callbacks pour ControlValueAccessor
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};
  private onValidatorChange: () => void = () => {};

  constructor(private phoneService: PhoneValidationService) {}

  ngOnInit(): void {
    this.countries = this.phoneService.getCountries();
    this.selectedCountryCode = this.defaultCountry;
    this.selectedCountry = this.phoneService.getCountryByCode(this.selectedCountryCode);
    
    // Fermer le dropdown quand on clique ailleurs
    document.addEventListener('click', this.onDocumentClick.bind(this));
  }

  ngOnDestroy(): void {
    document.removeEventListener('click', this.onDocumentClick.bind(this));
  }

  private onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.phone-input-container')) {
      this.isDropdownOpen = false;
    }
  }

  // ============================================
  // ControlValueAccessor Implementation
  // ============================================
  
  writeValue(value: string): void {
    if (value) {
      // Essayer d'extraire le pays et le numéro local
      const extracted = this.phoneService.extractLocalNumber(value);
      if (extracted) {
        this.selectedCountryCode = extracted.countryCode;
        this.selectedCountry = this.phoneService.getCountryByCode(extracted.countryCode);
        this.phoneNumber = this.phoneService.format(extracted.localNumber, extracted.countryCode);
      } else {
        // Sinon, utiliser la valeur brute
        this.phoneNumber = this.phoneService.format(value, this.selectedCountryCode);
      }
    } else {
      this.phoneNumber = '';
    }
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  // ============================================
  // Validator Implementation
  // ============================================
  
  validate(control: AbstractControl): ValidationErrors | null {
    if (!this.phoneNumber || this.phoneNumber.trim() === '') {
      if (this.required) {
        this.errorMessage = 'Numéro de téléphone requis';
        return { required: true };
      }
      this.errorMessage = null;
      return null;
    }

    const result = this.phoneService.validate(this.phoneNumber, this.selectedCountryCode);
    
    if (!result.isValid) {
      this.errorMessage = result.errorMessage || 'Numéro invalide';
      return { phoneInvalid: true, message: result.errorMessage };
    }
    
    this.errorMessage = null;
    return null;
  }

  registerOnValidatorChange(fn: () => void): void {
    this.onValidatorChange = fn;
  }

  // ============================================
  // Event Handlers
  // ============================================
  
  onPhoneInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    let value = input.value;
    
    // Garder uniquement les chiffres pour le traitement
    const cleaned = value.replace(/\D/g, '');
    
    // Limiter à la longueur maximale du pays
    const maxLength = this.selectedCountry?.length || 15;
    const truncated = cleaned.slice(0, maxLength);
    
    // Formater le numéro
    this.phoneNumber = this.phoneService.format(truncated, this.selectedCountryCode);
    
    // Émettre la valeur complète avec indicatif
    const fullNumber = this.phoneService.getFullNumber(truncated, this.selectedCountryCode);
    this.onChange(fullNumber);
    
    // Mettre à jour la position du curseur
    setTimeout(() => {
      input.value = this.phoneNumber;
    });
    
    this.onValidatorChange();
  }

  onBlur(): void {
    this.isTouched = true;
    this.onTouched();
  }

  toggleDropdown(event: Event): void {
    event.stopPropagation();
    if (!this.disabled) {
      this.isDropdownOpen = !this.isDropdownOpen;
    }
  }

  selectCountry(country: PhoneCountryConfig): void {
    this.selectedCountryCode = country.code;
    this.selectedCountry = country;
    this.isDropdownOpen = false;
    
    // Reformater le numéro avec le nouveau pays
    if (this.phoneNumber) {
      const cleaned = this.phoneNumber.replace(/\D/g, '');
      this.phoneNumber = this.phoneService.format(cleaned, country.code);
      
      // Émettre la nouvelle valeur
      const fullNumber = this.phoneService.getFullNumber(cleaned, country.code);
      this.onChange(fullNumber);
    }
    
    this.onValidatorChange();
  }

  // ============================================
  // Helpers
  // ============================================
  
  get placeholder(): string {
    return this.selectedCountry?.placeholder || 'Numéro de téléphone';
  }

  get hasError(): boolean {
    return this.isTouched && !!this.errorMessage;
  }
}
