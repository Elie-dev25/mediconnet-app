import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

/**
 * Interface unifiée pour un créneau horaire
 */
export interface CreneauUnifie {
  dateHeure: string;
  heureDebut: string;
  heureFin: string;
  duree: number;
  statut: 'disponible' | 'occupe' | 'passe' | 'indisponible';
  selectionnable: boolean;
}

/**
 * Composant réutilisable pour l'affichage et la sélection de créneaux horaires.
 * Utilisé dans: consultation suivi, orientation médecin, enregistrement accueil,
 * dashboard médecin (suggestion), nouveau RDV patient.
 */
@Component({
  selector: 'app-creneaux-selector',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './creneaux-selector.component.html',
  styleUrl: './creneaux-selector.component.scss'
})
export class CreneauxSelectorComponent implements OnChanges {
  /** Liste des créneaux à afficher */
  @Input() creneaux: CreneauUnifie[] = [];
  
  /** Créneau actuellement sélectionné */
  @Input() selectedCreneau: CreneauUnifie | null = null;
  
  /** Afficher l'indicateur de chargement */
  @Input() isLoading = false;
  
  /** Message à afficher quand aucun créneau n'est disponible */
  @Input() emptyMessage = 'Aucun créneau disponible';
  
  /** Afficher la légende des statuts */
  @Input() showLegend = true;
  
  /** Afficher l'info du créneau sélectionné */
  @Input() showSelectedInfo = true;
  
  /** Taille des boutons: 'sm' | 'md' | 'lg' */
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  
  /** Variante de style: 'default' | 'compact' | 'card' */
  @Input() variant: 'default' | 'compact' | 'card' = 'default';
  
  /** Événement émis lors de la sélection d'un créneau */
  @Output() creneauSelected = new EventEmitter<CreneauUnifie>();

  /** Créneaux disponibles (filtrés) */
  creneauxDisponibles: CreneauUnifie[] = [];
  
  /** Nombre de créneaux par statut */
  statsCreneaux = { disponible: 0, occupe: 0, passe: 0, indisponible: 0 };

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['creneaux']) {
      this.updateStats();
    }
  }

  private updateStats(): void {
    this.statsCreneaux = { disponible: 0, occupe: 0, passe: 0, indisponible: 0 };
    this.creneaux.forEach(c => {
      if (c.statut in this.statsCreneaux) {
        this.statsCreneaux[c.statut]++;
      }
    });
    this.creneauxDisponibles = this.creneaux.filter(c => c.selectionnable);
  }

  selectCreneau(creneau: CreneauUnifie): void {
    if (!creneau.selectionnable) return;
    this.creneauSelected.emit(creneau);
  }

  isSelected(creneau: CreneauUnifie): boolean {
    if (!this.selectedCreneau) return false;
    return this.selectedCreneau.dateHeure === creneau.dateHeure;
  }

  getCreneauClass(creneau: CreneauUnifie): string {
    const classes = ['creneau-btn', `size-${this.size}`];
    
    switch (creneau.statut) {
      case 'disponible':
        classes.push('creneau-disponible');
        break;
      case 'occupe':
        classes.push('creneau-occupe');
        break;
      case 'passe':
        classes.push('creneau-passe');
        break;
      case 'indisponible':
        classes.push('creneau-indisponible');
        break;
    }
    
    if (this.isSelected(creneau)) {
      classes.push('selected');
    }
    
    return classes.join(' ');
  }

  formatSelectedTime(): string {
    if (!this.selectedCreneau) return '';
    return `${this.selectedCreneau.heureDebut} - ${this.selectedCreneau.heureFin}`;
  }

  formatSelectedDate(): string {
    if (!this.selectedCreneau) return '';
    const date = new Date(this.selectedCreneau.dateHeure);
    return date.toLocaleDateString('fr-FR', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    });
  }
}
