import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { ConsultationCompleteService, LaboratoireDto } from '../../../services/consultation-complete.service';

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
export class PrescriptionExamensComponent implements OnInit {
  @Input() examens: ExamenPrescription[] = [];
  @Output() examensChange = new EventEmitter<ExamenPrescription[]>();

  laboratoires: LaboratoireDto[] = [];
  categories: CategorieExamen[] = [];
  
  // Champ personnalisé
  customExamenType = '';
  customExamenNom = '';

  typesExamen = [
    { value: 'biologie', label: 'Biologie / Analyses', icon: 'test-tube' },
    { value: 'imagerie', label: 'Imagerie médicale', icon: 'scan-line' },
    { value: 'cardiologie', label: 'Cardiologie', icon: 'heart-pulse' },
    { value: 'neurologie', label: 'Neurologie', icon: 'brain-circuit' }
  ];

  examensParType: { [key: string]: string[] } = {
    biologie: [
      'NFS (Numération Formule Sanguine)',
      'Glycémie à jeun',
      'HbA1c',
      'Bilan lipidique complet',
      'Bilan rénal (Urée, Créatinine)',
      'Bilan hépatique',
      'Ionogramme sanguin',
      'CRP (Protéine C-Réactive)',
      'VS (Vitesse de Sédimentation)',
      'TSH / T3 / T4',
      'Bilan martial (Fer, Ferritine)',
      'Groupe sanguin / Rhésus',
      'TP / INR',
      'D-Dimères',
      'Troponine',
      'BNP / NT-proBNP',
      'ECBU',
      'Hémocultures',
      'Sérologies'
    ],
    imagerie: [
      'Radiographie thoracique',
      'Radiographie osseuse',
      'Échographie abdominale',
      'Échographie pelvienne',
      'Échographie cardiaque',
      'Scanner thoracique',
      'Scanner abdomino-pelvien',
      'Scanner cérébral',
      'IRM cérébrale',
      'IRM lombaire',
      'IRM articulaire',
      'Mammographie',
      'Doppler veineux',
      'Doppler artériel'
    ],
    cardiologie: [
      'ECG (Électrocardiogramme)',
      'Holter ECG 24h',
      'Holter tensionnel (MAPA)',
      'Échocardiographie',
      'Épreuve d\'effort',
      'Coronarographie'
    ],
    neurologie: [
      'EEG (Électroencéphalogramme)',
      'EMG (Électromyogramme)',
      'Potentiels évoqués',
      'Ponction lombaire'
    ]
  };

  constructor(
    private fb: FormBuilder,
    private consultationService: ConsultationCompleteService
  ) {}

  ngOnInit(): void {
    this.initCategories();
    this.loadLaboratoires();
    if (this.examens.length > 0) {
      this.populateFromInput();
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

  private initCategories(): void {
    this.categories = this.typesExamen.map(type => ({
      type: type.value,
      label: type.label,
      icon: type.icon,
      expanded: true,
      examens: (this.examensParType[type.value] || []).map(nom => ({
        nom,
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
