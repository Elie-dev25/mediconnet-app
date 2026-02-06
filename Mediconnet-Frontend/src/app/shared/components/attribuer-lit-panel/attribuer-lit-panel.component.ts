import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { LucideAngularModule } from 'lucide-angular';
import { HospitalisationService } from '../../../services/hospitalisation.service';
import { environment } from '../../../../environments/environment';

export interface HospitalisationEnAttenteInfo {
  idAdmission: number;
  patientNom: string;
  patientPrenom: string;
  motif?: string;
  urgence?: string;
  dateEntree?: string;
}

export interface StandardDto {
  idStandard: number;
  nom: string;
  description?: string;
  prixJournalier: number;
  localisation?: string;
  chambresDisponibles: number;
}

export interface ChambreDto {
  idChambre: number;
  numero: string;
  standardNom: string;
  prixJournalier: number;
  localisation?: string;
  litsDisponibles: LitDisponible[];
}

interface LitDisponible {
  idLit: number;
  numero: string;
}

@Component({
  selector: 'app-attribuer-lit-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './attribuer-lit-panel.component.html',
  styleUrl: './attribuer-lit-panel.component.scss'
})
export class AttribuerLitPanelComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() hospitalisation: HospitalisationEnAttenteInfo | null = null;
  @Input() context: 'medecin' | 'infirmier' = 'infirmier'; // Contexte pour déterminer l'API à utiliser
  @Output() close = new EventEmitter<void>();
  @Output() attributed = new EventEmitter<void>();

  // Étape 1: Standards
  standards: StandardDto[] = [];
  selectedStandardId: number | null = null;
  isLoadingStandards = false;

  // Étape 2: Chambres
  chambres: ChambreDto[] = [];
  selectedChambreId: number | null = null;
  isLoadingChambres = false;

  // Étape 3: Lits
  litsDisponibles: LitDisponible[] = [];
  selectedLitId: number | null = null;

  // Dropdown states
  isChambreDropdownOpen = false;

  isAttribuant = false;
  error: string | null = null;

  private apiUrl = environment.apiUrl;

  constructor(
    private hospitalisationService: HospitalisationService,
    private http: HttpClient
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen && this.hospitalisation) {
      this.loadStandards();
    }
    if (changes['isOpen'] && !this.isOpen) {
      this.reset();
    }
  }

  private reset(): void {
    this.selectedStandardId = null;
    this.selectedChambreId = null;
    this.selectedLitId = null;
    this.standards = [];
    this.chambres = [];
    this.litsDisponibles = [];
    this.error = null;
    this.isChambreDropdownOpen = false;
  }

  private getBaseUrl(): string {
    return this.context === 'medecin' ? `${this.apiUrl}/medecin` : `${this.apiUrl}/infirmier`;
  }

  loadStandards(): void {
    this.isLoadingStandards = true;
    this.error = null;
    
    this.http.get<{ success: boolean; data: StandardDto[] }>(`${this.getBaseUrl()}/hospitalisation/standards`).subscribe({
      next: (response: { success: boolean; data: StandardDto[] }) => {
        this.standards = response.data || [];
        this.isLoadingStandards = false;
      },
      error: (err: any) => {
        console.error('Erreur chargement standards:', err);
        this.error = 'Impossible de charger les standards';
        this.isLoadingStandards = false;
      }
    });
  }

  onStandardSelected(standardId: number): void {
    this.selectedStandardId = standardId;
    this.selectedChambreId = null;
    this.selectedLitId = null;
    this.chambres = [];
    this.litsDisponibles = [];
    this.loadChambres(standardId);
  }

  loadChambres(standardId: number): void {
    this.isLoadingChambres = true;
    this.error = null;
    
    this.http.get<{ success: boolean; data: ChambreDto[] }>(`${this.getBaseUrl()}/hospitalisation/chambres/${standardId}`).subscribe({
      next: (response: { success: boolean; data: ChambreDto[] }) => {
        this.chambres = response.data || [];
        this.isLoadingChambres = false;
      },
      error: (err: any) => {
        console.error('Erreur chargement chambres:', err);
        this.error = 'Impossible de charger les chambres';
        this.isLoadingChambres = false;
      }
    });
  }

  onChambreSelected(chambre: ChambreDto): void {
    this.selectedChambreId = chambre.idChambre;
    this.selectedLitId = null;
    this.litsDisponibles = chambre.litsDisponibles || [];
  }

  onChambreChange(chambreId: number | null): void {
    this.selectedLitId = null;
    if (chambreId) {
      const chambre = this.chambres.find(c => c.idChambre === chambreId);
      if (chambre) {
        this.litsDisponibles = chambre.litsDisponibles || [];
      } else {
        this.litsDisponibles = [];
      }
    } else {
      this.litsDisponibles = [];
    }
  }

  toggleChambreDropdown(): void {
    this.isChambreDropdownOpen = !this.isChambreDropdownOpen;
  }

  selectChambre(chambre: ChambreDto): void {
    this.selectedChambreId = chambre.idChambre;
    this.selectedLitId = null;
    this.litsDisponibles = chambre.litsDisponibles || [];
    this.isChambreDropdownOpen = false;
  }

  onLitSelected(litId: number | null): void {
    this.selectedLitId = litId;
  }

  attribuerLit(): void {
    if (!this.hospitalisation || !this.selectedLitId) return;

    this.isAttribuant = true;
    this.error = null;

    this.hospitalisationService.attribuerLit({
      idAdmission: this.hospitalisation.idAdmission,
      idLit: this.selectedLitId
    }, this.context).subscribe({
      next: (response) => {
        if (response.success) {
          this.attributed.emit();
          this.closePanel();
        } else {
          this.error = response.message || 'Erreur lors de l\'attribution';
        }
        this.isAttribuant = false;
      },
      error: (err) => {
        console.error('Erreur attribution lit:', err);
        this.error = err.error?.message || 'Erreur lors de l\'attribution du lit';
        this.isAttribuant = false;
      }
    });
  }

  closePanel(): void {
    this.close.emit();
  }

  getUrgenceClass(urgence?: string): string {
    switch (urgence?.toLowerCase()) {
      case 'critique': return 'urgence-critique';
      case 'urgente': return 'urgence-urgente';
      case 'haute': return 'urgence-haute';
      case 'moyenne': return 'urgence-moyenne';
      default: return 'urgence-normale';
    }
  }

  getSelectedStandard(): StandardDto | undefined {
    return this.standards.find(s => s.idStandard === this.selectedStandardId);
  }

  getSelectedChambre(): ChambreDto | undefined {
    return this.chambres.find(c => c.idChambre === this.selectedChambreId);
  }

  getSelectedLit(): LitDisponible | undefined {
    return this.litsDisponibles.find(l => l.idLit === this.selectedLitId);
  }
}
