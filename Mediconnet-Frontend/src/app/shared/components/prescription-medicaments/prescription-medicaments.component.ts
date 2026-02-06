import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { PharmacieStockService, MedicamentStock } from '../../../services/pharmacie-stock.service';

export interface MedicamentPrescription {
  nomMedicament: string;
  dosage?: string;
  posologie?: string;
  posologieAutre?: string;
  formePharmaceutique?: string;
  formeAutre?: string;
  voieAdministration?: string;
  voieAutre?: string;
  dureeTraitement?: string;
  instructions?: string;
  quantite?: number;
}

@Component({
  selector: 'app-prescription-medicaments',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LucideAngularModule],
  templateUrl: './prescription-medicaments.component.html',
  styleUrl: './prescription-medicaments.component.scss'
})
export class PrescriptionMedicamentsComponent implements OnInit, OnDestroy {
  @Input() medicaments: MedicamentPrescription[] = [];
  @Output() medicamentsChange = new EventEmitter<MedicamentPrescription[]>();

  medicamentsForm!: FormGroup;
  
  // Autocomplete
  medicamentSuggestions: MedicamentStock[] = [];
  activeMedicamentIndex: number | null = null;
  private searchMedicament$ = new Subject<{index: number, term: string}>();

  // Options prédéfinies
  posologiesOptions = [
    '1 fois par jour',
    '2 fois par jour',
    '3 fois par jour',
    '4 fois par jour',
    'Matin et soir',
    'Matin, midi et soir',
    'Avant les repas',
    'Après les repas',
    'Au coucher',
    'Selon besoin',
    'Autre'
  ];

  formesPharmaceutiques = [
    'Comprimé',
    'Gélule',
    'Sirop',
    'Solution buvable',
    'Ampoule injectable',
    'Pommade',
    'Crème',
    'Gel',
    'Suppositoire',
    'Collyre',
    'Spray nasal',
    'Inhalateur',
    'Patch',
    'Sachet',
    'Autre'
  ];

  voiesAdministration = [
    'Voie orale',
    'Voie intraveineuse (IV)',
    'Voie intramusculaire (IM)',
    'Voie sous-cutanée (SC)',
    'Voie rectale',
    'Voie cutanée',
    'Voie ophtalmique',
    'Voie nasale',
    'Voie inhalée',
    'Voie sublinguale',
    'Autre'
  ];

  constructor(
    private fb: FormBuilder,
    private pharmacieService: PharmacieStockService
  ) {
    this.initForm();
  }

  ngOnInit(): void {
    this.setupMedicamentAutocomplete();
    if (this.medicaments.length > 0) {
      this.populateMedicaments();
    }
  }

  ngOnDestroy(): void {
    this.searchMedicament$.complete();
  }

  private initForm(): void {
    this.medicamentsForm = this.fb.group({
      medicaments: this.fb.array([])
    });

    // Écouter les changements
    this.medicamentsForm.valueChanges.subscribe(() => {
      this.emitChanges();
    });
  }

  private populateMedicaments(): void {
    this.medicaments.forEach(med => this.addMedicament(med));
  }

  private setupMedicamentAutocomplete(): void {
    this.searchMedicament$.pipe(
      debounceTime(300),
      distinctUntilChanged((prev, curr) => prev.term === curr.term && prev.index === curr.index),
      switchMap(({index, term}) => {
        this.activeMedicamentIndex = index;
        return this.pharmacieService.searchMedicamentsForAutocomplete(term);
      })
    ).subscribe(results => {
      this.medicamentSuggestions = results;
    });
  }

  get medicamentsArray(): FormArray {
    return this.medicamentsForm.get('medicaments') as FormArray;
  }

  addMedicament(med?: MedicamentPrescription): void {
    this.medicamentsArray.push(this.fb.group({
      nomMedicament: [med?.nomMedicament || '', Validators.required],
      dosage: [med?.dosage || ''],
      posologie: [med?.posologie || ''],
      posologieAutre: [med?.posologieAutre || ''],
      formePharmaceutique: [med?.formePharmaceutique || ''],
      formeAutre: [med?.formeAutre || ''],
      voieAdministration: [med?.voieAdministration || ''],
      voieAutre: [med?.voieAutre || ''],
      dureeTraitement: [med?.dureeTraitement || ''],
      instructions: [med?.instructions || ''],
      quantite: [med?.quantite || null]
    }));
  }

  removeMedicament(index: number): void {
    this.medicamentsArray.removeAt(index);
  }

  onMedicamentSearch(index: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchMedicament$.next({ index, term: input.value });
  }

  selectMedicament(index: number, medicament: MedicamentStock): void {
    const control = this.medicamentsArray.at(index);
    if (control) {
      control.patchValue({
        nomMedicament: medicament.nom + (medicament.dosage ? ' ' + medicament.dosage : ''),
        dosage: medicament.dosage || '',
        formePharmaceutique: medicament.formeGalenique || ''
      });
    }
    this.medicamentSuggestions = [];
    this.activeMedicamentIndex = null;
  }

  hideMedicamentSuggestions(): void {
    setTimeout(() => {
      this.medicamentSuggestions = [];
      this.activeMedicamentIndex = null;
    }, 200);
  }

  isPosologieAutre(index: number): boolean {
    const control = this.medicamentsArray.at(index);
    return control?.get('posologie')?.value === 'Autre';
  }

  isFormeAutre(index: number): boolean {
    const control = this.medicamentsArray.at(index);
    return control?.get('formePharmaceutique')?.value === 'Autre';
  }

  isVoieAutre(index: number): boolean {
    const control = this.medicamentsArray.at(index);
    return control?.get('voieAdministration')?.value === 'Autre';
  }

  private emitChanges(): void {
    const medicaments: MedicamentPrescription[] = this.medicamentsArray.controls.map(control => ({
      nomMedicament: control.get('nomMedicament')?.value,
      dosage: control.get('dosage')?.value,
      posologie: control.get('posologie')?.value === 'Autre' 
        ? control.get('posologieAutre')?.value 
        : control.get('posologie')?.value,
      formePharmaceutique: control.get('formePharmaceutique')?.value === 'Autre'
        ? control.get('formeAutre')?.value
        : control.get('formePharmaceutique')?.value,
      voieAdministration: control.get('voieAdministration')?.value === 'Autre'
        ? control.get('voieAutre')?.value
        : control.get('voieAdministration')?.value,
      dureeTraitement: control.get('dureeTraitement')?.value,
      instructions: control.get('instructions')?.value,
      quantite: control.get('quantite')?.value
    }));
    this.medicamentsChange.emit(medicaments);
  }

  getMedicamentsData(): MedicamentPrescription[] {
    return this.medicamentsArray.controls.map(control => ({
      nomMedicament: control.get('nomMedicament')?.value,
      dosage: control.get('dosage')?.value,
      posologie: control.get('posologie')?.value === 'Autre' 
        ? control.get('posologieAutre')?.value 
        : control.get('posologie')?.value,
      formePharmaceutique: control.get('formePharmaceutique')?.value === 'Autre'
        ? control.get('formeAutre')?.value
        : control.get('formePharmaceutique')?.value,
      voieAdministration: control.get('voieAdministration')?.value === 'Autre'
        ? control.get('voieAutre')?.value
        : control.get('voieAdministration')?.value,
      dureeTraitement: control.get('dureeTraitement')?.value,
      instructions: control.get('instructions')?.value,
      quantite: control.get('quantite')?.value
    }));
  }

  isValid(): boolean {
    return this.medicamentsForm.valid;
  }
}
