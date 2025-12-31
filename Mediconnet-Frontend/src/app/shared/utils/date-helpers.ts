/**
 * Utilitaires de formatage de dates
 * Évite la duplication de code entre les composants
 */

/**
 * Formate une date en format français complet
 * Exemple: "15 janvier 2024"
 */
export function formatDate(dateStr: string | Date | undefined | null): string {
  if (!dateStr) return 'Non renseigné';
  try {
    const date = typeof dateStr === 'string' ? new Date(dateStr) : dateStr;
    return date.toLocaleDateString('fr-FR', { 
      day: 'numeric', 
      month: 'long', 
      year: 'numeric' 
    });
  } catch {
    return String(dateStr);
  }
}

/**
 * Formate une date en format court
 * Exemple: "15 janv. 2024"
 */
export function formatDateShort(dateStr: string | Date | undefined | null): string {
  if (!dateStr) return 'Non renseigné';
  try {
    const date = typeof dateStr === 'string' ? new Date(dateStr) : dateStr;
    return date.toLocaleDateString('fr-FR', { 
      day: '2-digit', 
      month: 'short', 
      year: 'numeric' 
    });
  } catch {
    return String(dateStr);
  }
}

/**
 * Formate une heure
 * Exemple: "14:30"
 */
export function formatTime(dateStr: string | Date | undefined | null): string {
  if (!dateStr) return '';
  try {
    const date = typeof dateStr === 'string' ? new Date(dateStr) : dateStr;
    return date.toLocaleTimeString('fr-FR', { 
      hour: '2-digit', 
      minute: '2-digit' 
    });
  } catch {
    return '';
  }
}

/**
 * Formate une date avec l'heure
 * Exemple: "15 janvier 2024 à 14:30"
 */
export function formatDateTime(dateStr: string | Date | undefined | null): string {
  if (!dateStr) return 'Non renseigné';
  try {
    const date = typeof dateStr === 'string' ? new Date(dateStr) : dateStr;
    return date.toLocaleDateString('fr-FR', { 
      day: 'numeric', 
      month: 'long', 
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  } catch {
    return String(dateStr);
  }
}

/**
 * Formate une date avec le jour de la semaine
 * Exemple: "Lundi 15 janvier 2024"
 */
export function formatDateWithWeekday(dateStr: string | Date | undefined | null): string {
  if (!dateStr) return 'Non renseigné';
  try {
    const date = typeof dateStr === 'string' ? new Date(dateStr) : dateStr;
    return date.toLocaleDateString('fr-FR', { 
      weekday: 'long',
      day: 'numeric', 
      month: 'long',
      year: 'numeric'
    });
  } catch {
    return String(dateStr);
  }
}

/**
 * Formate une plage horaire
 * Exemple: "14:30-15:00"
 */
export function formatTimeRange(startDate: string | Date, durationMinutes: number): string {
  try {
    const start = typeof startDate === 'string' ? new Date(startDate) : startDate;
    const end = new Date(start.getTime() + durationMinutes * 60000);
    
    const startTime = start.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
    const endTime = end.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
    
    return `${startTime}-${endTime}`;
  } catch {
    return '';
  }
}

/**
 * Calcule le temps relatif (il y a X jours, dans X jours)
 */
export function getRelativeTime(dateStr: string | Date): string {
  try {
    const date = typeof dateStr === 'string' ? new Date(dateStr) : dateStr;
    const now = new Date();
    const diffMs = date.getTime() - now.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    
    if (diffDays === 0) return "Aujourd'hui";
    if (diffDays === 1) return 'Demain';
    if (diffDays === -1) return 'Hier';
    if (diffDays > 1 && diffDays <= 7) return `Dans ${diffDays} jours`;
    if (diffDays < -1 && diffDays >= -7) return `Il y a ${Math.abs(diffDays)} jours`;
    
    return formatDateShort(date);
  } catch {
    return '';
  }
}
