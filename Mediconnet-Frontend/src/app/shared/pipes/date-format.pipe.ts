import { Pipe, PipeTransform } from '@angular/core';
import { 
  formatDate, 
  formatDateShort, 
  formatTime, 
  formatDateTime, 
  formatDateWithWeekday,
  getRelativeTime 
} from '../utils/date-helpers';

/**
 * Pipe pour formater une date en format français complet
 * Exemple: "15 janvier 2024"
 * Usage: {{ date | frDate }}
 */
@Pipe({
  name: 'frDate',
  standalone: true
})
export class FrenchDatePipe implements PipeTransform {
  transform(value: string | Date | undefined | null): string {
    return formatDate(value);
  }
}

/**
 * Pipe pour formater une date en format court
 * Exemple: "15 janv. 2024"
 * Usage: {{ date | frDateShort }}
 */
@Pipe({
  name: 'frDateShort',
  standalone: true
})
export class FrenchDateShortPipe implements PipeTransform {
  transform(value: string | Date | undefined | null): string {
    return formatDateShort(value);
  }
}

/**
 * Pipe pour formater une heure
 * Exemple: "14:30"
 * Usage: {{ date | frTime }}
 */
@Pipe({
  name: 'frTime',
  standalone: true
})
export class FrenchTimePipe implements PipeTransform {
  transform(value: string | Date | undefined | null): string {
    return formatTime(value);
  }
}

/**
 * Pipe pour formater une date avec l'heure
 * Exemple: "15 janvier 2024 à 14:30"
 * Usage: {{ date | frDateTime }}
 */
@Pipe({
  name: 'frDateTime',
  standalone: true
})
export class FrenchDateTimePipe implements PipeTransform {
  transform(value: string | Date | undefined | null): string {
    return formatDateTime(value);
  }
}

/**
 * Pipe pour formater une date avec le jour de la semaine
 * Exemple: "Lundi 15 janvier 2024"
 * Usage: {{ date | frDateWeekday }}
 */
@Pipe({
  name: 'frDateWeekday',
  standalone: true
})
export class FrenchDateWeekdayPipe implements PipeTransform {
  transform(value: string | Date | undefined | null): string {
    return formatDateWithWeekday(value);
  }
}

/**
 * Pipe pour afficher le temps relatif
 * Exemple: "Aujourd'hui", "Demain", "Il y a 3 jours"
 * Usage: {{ date | relativeTime }}
 */
@Pipe({
  name: 'relativeTime',
  standalone: true
})
export class RelativeTimePipe implements PipeTransform {
  transform(value: string | Date): string {
    return getRelativeTime(value);
  }
}
