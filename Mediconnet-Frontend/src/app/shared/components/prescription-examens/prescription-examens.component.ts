import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { ConsultationCompleteService, LaboratoireDto } from '../../../services/consultation-complete.service';
import { 
  CategorieExamenDefinition, 
  getExamensFromTitreAffiche, 
  getExamensForSpecialite 
} from '../../data/examens-par-specialite';

export interface ExamenPrescription {
  typeExamen: string;
  nomExamen: string;
  description?: string;
  urgence: boolean;
  notes?: string;
  idLaboratoire?: number;
}

export interface ExamenCheckboxItem {
  nom: string;
  selected: boolean;
  urgence: boolean;
  notes: string;
  idLaboratoire: number | null;
}

export interface CategorieExamen {
  type: string;
  label: string;
  icon: string;
  expanded: boolean;
  examens: ExamenCheckboxItem[];
}

@Component({
  selector: 'app-prescription-examens',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LucideAngularModule],
  templateUrl: './prescription-examens.component.html',
  styleUrl: './prescription-examens.component.scss'
})
export class PrescriptionExamensComponent implements OnInit, OnChanges {
  @Input() examens: ExamenPrescription[] = [];
  @Input() collapsed = true;
  /**
   * Spécialité du médecin (ex: "gynecologie", "chirurgie_generale")
   * Si non fourni, utilise titreAffiche ou médecine générale par défaut
   */
  @Input() specialite?: string;
  /**
   * Titre affiché de l'utilisateur (ex: "Médecin - Gynécologie")
   * Utilisé pour extraire la spécialité si specialite n'est pas fourni
   */
  @Input() titreAffiche?: string;
  
  @Output() examensChange = new EventEmitter<ExamenPrescription[]>();
  @Output() collapsedChange = new EventEmitter<boolean>();

  laboratoires: LaboratoireDto[] = [];
  categories: CategorieExamen[] = [];
  
  // Champ personnalisé
  customExamenType = '';
  customExamenNom = '';

  // Types d'examens dynamiques (chargés selon la spécialité)
  typesExamen: { value: string; label: string; icon: string }[] = [];

  constructor(
    private fb: FormBuilder,
    private consultationService: ConsultationCompleteService
  ) {}

  ngOnInit(): void {
    this.initCategoriesFromSpecialite();
    this.loadLaboratoires();
    if (this.examens.length > 0) {
      this.populateFromInput();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    // Réinitialiser les catégories si la spécialité ou le titreAffiche change
    if (changes['specialite'] || changes['titreAffiche']) {
      if (!changes['specialite']?.firstChange && !changes['titreAffiche']?.firstChange) {
        this.initCategoriesFromSpecialite();
        if (this.examens.length > 0) {
          this.populateFromInput();
        }
      }
    }
  }

  private loadLaboratoires(): void {
    this.consultationService.getLaboratoires().subscribe({
      next: (labs) => this.laboratoires = labs,
      error: (err) => console.error('Erreur chargement laboratoires:', err)
    });
  }

  getLaboratoiresByType(type: string): LaboratoireDto[] {
    return this.laboratoires.filter(lab => lab.type === type);
  }

  /**
   * Initialise les catégories d'examens en fonction de la spécialité du médecin
   */
  private initCategoriesFromSpecialite(): void {
    // Récupérer les définitions d'examens selon la spécialité
    let categoriesDefinition: CategorieExamenDefinition[];
    
    if (this.specialite) {
      categoriesDefinition = getExamensForSpecialite(this.specialite);
    } else if (this.titreAffiche) {
      categoriesDefinition = getExamensFromTitreAffiche(this.titreAffiche);
    } else {
      // Fallback: médecine générale
      categoriesDefinition = getExamensForSpecialite('default');
    }

    // Mettre à jour les types d'examens pour le dropdown personnalisé
    this.typesExamen = categoriesDefinition.map(cat => ({
      value: cat.type,
      label: cat.label,
      icon: cat.icon
    }));

    // Construire les catégories avec les examens
    this.categories = categoriesDefinition.map(catDef => ({
      type: catDef.type,
      label: catDef.label,
      icon: catDef.icon,
      expanded: false,
      examens: catDef.examens.map(exam => ({
        nom: exam.nom,
        selected: false,
        urgence: false,
        notes: '',
        idLaboratoire: null
      }))
    }));
  }

  private populateFromInput(): void {
    this.examens.forEach(exam => {
      const category = this.categories.find(c => c.type === exam.typeExamen);
      if (category) {
        const examenItem = category.examens.find(e => e.nom === exam.nomExamen);
        if (examenItem) {
          examenItem.selected = true;
          examenItem.urgence = exam.urgence;
          examenItem.notes = exam.notes || '';
          examenItem.idLaboratoire = exam.idLaboratoire || null;
        } else {
          // Examen personnalisé non trouvé dans la liste, l'ajouter
          category.examens.push({
            nom: exam.nomExamen,
            selected: true,
            urgence: exam.urgence,
            notes: exam.notes || '',
            idLaboratoire: exam.idLaboratoire || null
          });
        }
      }
    });
  }

  toggleCategory(category: CategorieExamen): void {
    category.expanded = !category.expanded;
  }

  toggleCollapsed(): void {
    this.collapsed = !this.collapsed;
    this.collapsedChange.emit(this.collapsed);
  }

  toggleExamen(examen: ExamenCheckboxItem): void {
    examen.selected = !examen.selected;
    if (!examen.selected) {
      examen.urgence = false;
      examen.notes = '';
      examen.idLaboratoire = null;
    }
    this.emitChanges();
  }

  toggleUrgence(examen: ExamenCheckboxItem, event: Event): void {
    event.stopPropagation();
    examen.urgence = !examen.urgence;
    this.emitChanges();
  }

  updateNotes(examen: ExamenCheckboxItem, notes: string): void {
    examen.notes = notes;
    this.emitChanges();
  }

  updateLaboratoire(examen: ExamenCheckboxItem, idLabo: number | null): void {
    examen.idLaboratoire = idLabo;
    this.emitChanges();
  }

  selectAllInCategory(category: CategorieExamen): void {
    const allSelected = category.examens.every(e => e.selected);
    category.examens.forEach(e => e.selected = !allSelected);
    this.emitChanges();
  }

  deselectAllInCategory(category: CategorieExamen): void {
    category.examens.forEach(e => {
      e.selected = false;
      e.urgence = false;
      e.notes = '';
      e.idLaboratoire = null;
    });
    this.emitChanges();
  }

  getSelectedCount(category: CategorieExamen): number {
    return category.examens.filter(e => e.selected).length;
  }

  getTotalSelectedCount(): number {
    return this.categories.reduce((sum, cat) => sum + this.getSelectedCount(cat), 0);
  }

  getSelectedExamens(): ExamenCheckboxItem[] {
    return this.categories.flatMap(cat => 
      cat.examens.filter(e => e.selected).map(e => ({ ...e, type: cat.type }))
    );
  }

  addCustomExamen(): void {
    if (!this.customExamenType || !this.customExamenNom.trim()) return;
    
    const category = this.categories.find(c => c.type === this.customExamenType);
    if (category) {
      category.examens.push({
        nom: this.customExamenNom.trim(),
        selected: true,
        urgence: false,
        notes: '',
        idLaboratoire: null
      });
      this.customExamenNom = '';
      this.emitChanges();
    }
  }

  getTypeIcon(type: string): string {
    return this.typesExamen.find(t => t.value === type)?.icon || 'file-plus';
  }

  private emitChanges(): void {
    const examens: ExamenPrescription[] = [];
    this.categories.forEach(category => {
      category.examens.filter(e => e.selected).forEach(examen => {
        examens.push({
          typeExamen: category.type,
          nomExamen: examen.nom,
          urgence: examen.urgence,
          notes: examen.notes,
          idLaboratoire: examen.idLaboratoire || undefined
        });
      });
    });
    this.examensChange.emit(examens);
  }

  getExamensData(): ExamenPrescription[] {
    const examens: ExamenPrescription[] = [];
    this.categories.forEach(category => {
      category.examens.filter(e => e.selected).forEach(examen => {
        examens.push({
          typeExamen: category.type,
          nomExamen: examen.nom,
          urgence: examen.urgence,
          notes: examen.notes,
          idLaboratoire: examen.idLaboratoire || undefined
        });
      });
    });
    return examens;
  }

  isValid(): boolean {
    return this.getTotalSelectedCount() > 0;
  }
}
