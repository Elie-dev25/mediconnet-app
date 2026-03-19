import { Injectable } from '@angular/core';

/**
 * Service centralisé pour le formatage des données
 * Élimine la duplication des méthodes de formatage dans les composants
 */
@Injectable({ 
  providedIn: 'root' 
})
export class FormatService {
  
  /**
   * Formate un prix en FCFA
   * @param price - Le prix à formater
   * @returns Le prix formaté avec la devise FCFA
   */
  formatPrice(price?: number): string {
    if (price === undefined || price === null) return '-';
    return price.toLocaleString('fr-FR') + ' FCFA';
  }

  /**
   * Formate une date au format français court
   * @param date - La date à formater (string ISO ou Date)
   * @returns La date formatée (ex: 18 mars 2026)
   */
  formatDate(date?: string | Date): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  /**
   * Formate une date avec l'heure
   * @param date - La date à formater (string ISO ou Date)
   * @returns La date et l'heure formatées
   */
  formatDateTime(date?: string | Date): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  /**
   * Formate un nombre avec séparateurs de milliers
   * @param value - Le nombre à formater
   * @returns Le nombre formaté
   */
  formatNumber(value?: number): string {
    if (value === undefined || value === null) return '-';
    return value.toLocaleString('fr-FR');
  }

  /**
   * Formate une date au format français complet
   * @param date - La date à formater
   * @returns La date formatée (ex: lundi 18 mars 2026)
   */
  formatDateLong(date?: string | Date): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('fr-FR', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    });
  }

  /**
   * Formate une date au format court (JJ/MM/AAAA)
   * @param date - La date à formater
   * @returns La date formatée (ex: 18/03/2026)
   */
  formatDateShort(date?: string | Date): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('fr-FR');
  }
}
