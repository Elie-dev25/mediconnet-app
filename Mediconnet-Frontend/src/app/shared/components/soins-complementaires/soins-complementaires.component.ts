import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';

export interface SoinComplementaire {
  typeSoin: string;
  description: string;
  frequence?: string;
  dureeJours?: number;
  moments?: string[]; // Legacy
  nbFoisParJour?: number;
  horairesPersonnalises?: string; // JSON array ["08:00","12:00"]
  horairesAuto?: { seance: number; heure: string }[];
  useHorairesPerso?: boolean;
  priorite: 'normale' | 'haute' | 'urgente';
  instructions?: string;
}

@Component({
  selector: 'app-soins-complementaires',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LucideAngularModule],
  templateUrl: './soins-complementaires.component.html',
  styleUrl: './soins-complementaires.component.scss'
})
export class SoinsComplementairesComponent implements OnInit {
  @Input() soins: SoinComplementaire[] = [];
  @Output() soinsChange = new EventEmitter<SoinComplementaire[]>();

  soinsForm!: FormGroup;

  typesSoins = [
    { value: 'surveillance', label: 'Surveillance', icon: 'eye' },
    { value: 'nursing', label: 'Soins infirmiers', icon: 'stethoscope' },
    { value: 'kinesitherapie', label: 'Kinésithérapie', icon: 'activity' },
    { value: 'nutrition', label: 'Nutrition / Diététique', icon: 'utensils' },
    { value: 'hygiene', label: 'Hygiène / Confort', icon: 'droplets' },
    { value: 'psychologique', label: 'Soutien psychologique', icon: 'heart' },
    { value: 'autre', label: 'Autre', icon: 'circle-plus' }
  ];

  soinsParType: { [key: string]: string[] } = {
    surveillance: [
      'Surveillance des constantes vitales (toutes les 4h)',
      'Surveillance des constantes vitales (toutes les 2h)',
      'Surveillance horaire',
      'Surveillance neurologique (score Glasgow)',
      'Surveillance glycémique',
      'Surveillance diurèse',
      'Surveillance de la douleur (EVA)',
      'Surveillance du pansement',
      'Surveillance post-opératoire',
      'Autre'
    ],
    nursing: [
      'Pose de voie veineuse périphérique',
      'Surveillance de la perfusion',
      'Soins de sonde urinaire',
      'Soins de sonde nasogastrique',
      'Pansement simple',
      'Pansement complexe',
      'Injection sous-cutanée',
      'Injection intramusculaire',
      'Aérosols',
      'Aspiration trachéale',
      'Oxygénothérapie',
      'Autre'
    ],
    kinesitherapie: [
      'Kinésithérapie respiratoire',
      'Mobilisation passive',
      'Mobilisation active',
      'Lever précoce',
      'Marche accompagnée',
      'Rééducation fonctionnelle',
      'Autre'
    ],
    nutrition: [
      'Régime sans sel',
      'Régime diabétique',
      'Régime hyperprotidique',
      'Alimentation mixée',
      'Hydratation renforcée',
      'À jeun',
      'Alimentation entérale',
      'Autre'
    ],
    hygiene: [
      'Aide à la toilette',
      'Toilette complète au lit',
      'Changement de position (anti-escarres)',
      'Prévention des escarres',
      'Soins de bouche',
      'Autre'
    ],
    psychologique: [
      'Accompagnement psychologique',
      'Gestion de l\'anxiété',
      'Éducation thérapeutique',
      'Soutien familial',
      'Autre'
    ],
    autre: []
  };

  frequences = [
    'Une fois',
    'Quotidien',
    '2 fois par jour',
    '3 fois par jour',
    'Toutes les 4 heures',
    'Toutes les 2 heures',
    'Toutes les heures',
    'Selon besoin',
    'Continu'
  ];

  momentsList = [
    { value: 'matin', label: 'Matin (8h)', icon: 'sun' },
    { value: 'midi', label: 'Midi (12h)', icon: 'sun' },
    { value: 'soir', label: 'Soir (18h)', icon: 'cloud' },
    { value: 'nuit', label: 'Nuit (22h)', icon: 'moon' }
  ];

  nbFoisOptions = [
    { value: 1, label: '1 fois/jour' },
    { value: 2, label: '2 fois/jour' },
    { value: 3, label: '3 fois/jour' },
    { value: 4, label: '4 fois/jour' },
    { value: 6, label: '6 fois/jour' },
    { value: 8, label: '8 fois/jour' }
  ];

  // Cache pour les horaires auto générés
  horairesAutoCache: { [key: number]: { seance: number; heure: string }[] } = {};

  dureeOptions = [
    { value: 1, label: '1 jour' },
    { value: 3, label: '3 jours' },
    { value: 5, label: '5 jours' },
    { value: 7, label: '7 jours' },
    { value: 10, label: '10 jours' },
    { value: 14, label: '14 jours' },
    { value: 21, label: '21 jours' },
    { value: 30, label: '30 jours' }
  ];

  constructor(private fb: FormBuilder) {
    this.initForm();
  }

  ngOnInit(): void {
    if (this.soins.length > 0) {
      this.populateSoins();
    }
  }

  private initForm(): void {
    this.soinsForm = this.fb.group({
      soins: this.fb.array([])
    });

    this.soinsForm.valueChanges.subscribe(() => {
      this.emitChanges();
    });
  }

  private populateSoins(): void {
    this.soins.forEach(soin => this.addSoin(soin));
  }

  get soinsArray(): FormArray {
    return this.soinsForm.get('soins') as FormArray;
  }

  addSoin(soin?: SoinComplementaire): void {
    const index = this.soinsArray.length;
    this.soinsArray.push(this.fb.group({
      typeSoin: [soin?.typeSoin || '', Validators.required],
      typeSoinAutre: [''],
      description: [soin?.description || '', Validators.required],
      descriptionAutre: [''],
      frequence: [soin?.frequence || ''],
      dureeJours: [soin?.dureeJours || 7],
      moments: [soin?.moments || ['matin']], // Legacy
      nbFoisParJour: [soin?.nbFoisParJour || 1],
      useHorairesPerso: [soin?.useHorairesPerso || false],
      horairesPerso: [soin?.horairesPersonnalises ? JSON.parse(soin.horairesPersonnalises) : ['08:00']],
      priorite: [soin?.priorite || 'normale'],
      instructions: [soin?.instructions || '']
    }));
    // Générer les horaires auto pour ce soin
    this.updateHorairesAuto(index, 1);
  }

  toggleMoment(index: number, moment: string): void {
    const control = this.soinsArray.at(index)?.get('moments');
    if (control) {
      const currentMoments: string[] = control.value || [];
      const idx = currentMoments.indexOf(moment);
      if (idx > -1) {
        if (currentMoments.length > 1) {
          currentMoments.splice(idx, 1);
        }
      } else {
        currentMoments.push(moment);
      }
      control.setValue([...currentMoments]);
    }
  }

  isMomentSelected(index: number, moment: string): boolean {
    const control = this.soinsArray.at(index)?.get('moments');
    return control?.value?.includes(moment) || false;
  }

  removeSoin(index: number): void {
    this.soinsArray.removeAt(index);
  }

  getSoinsSuggestions(index: number): string[] {
    const typeSoin = this.soinsArray.at(index)?.get('typeSoin')?.value;
    return this.soinsParType[typeSoin] || [];
  }

  isTypeSoinAutre(index: number): boolean {
    const control = this.soinsArray.at(index);
    return control?.get('typeSoin')?.value === 'autre';
  }

  isDescriptionAutre(index: number): boolean {
    const control = this.soinsArray.at(index);
    return control?.get('description')?.value === 'Autre';
  }

  onTypeChange(index: number): void {
    const control = this.soinsArray.at(index);
    if (control) {
      control.get('description')?.setValue('');
      control.get('descriptionAutre')?.setValue('');
      if (control.get('typeSoin')?.value !== 'autre') {
        control.get('typeSoinAutre')?.setValue('');
      }
    }
  }

  onDescriptionChange(index: number): void {
    const control = this.soinsArray.at(index);
    if (control && control.get('description')?.value !== 'Autre') {
      control.get('descriptionAutre')?.setValue('');
    }
  }

  getTypeIcon(type: string): string {
    return this.typesSoins.find(t => t.value === type)?.icon || 'circle-plus';
  }

  getPrioriteClass(priorite: string): string {
    switch (priorite) {
      case 'urgente': return 'priorite-urgente';
      case 'haute': return 'priorite-haute';
      default: return 'priorite-normale';
    }
  }

  // ==================== Gestion des séances ====================

  onNbFoisChange(index: number, nbFois: number): void {
    this.updateHorairesAuto(index, nbFois);
    // Mettre à jour les horaires perso si nécessaire
    const control = this.soinsArray.at(index);
    const currentHoraires: string[] = control?.get('horairesPerso')?.value || [];
    if (currentHoraires.length < nbFois) {
      // Ajouter des horaires par défaut
      const newHoraires = [...currentHoraires];
      while (newHoraires.length < nbFois) {
        newHoraires.push('08:00');
      }
      control?.get('horairesPerso')?.setValue(newHoraires);
    } else if (currentHoraires.length > nbFois) {
      control?.get('horairesPerso')?.setValue(currentHoraires.slice(0, nbFois));
    }
  }

  updateHorairesAuto(index: number, nbFois: number): void {
    // Générer des horaires répartis sur la journée (6h-22h)
    const horaires: { seance: number; heure: string }[] = [];
    const heureDebut = 6;
    const heureFin = 22;
    const plageMinutes = (heureFin - heureDebut) * 60;

    if (nbFois === 1) {
      horaires.push({ seance: 1, heure: '08:00' });
    } else {
      const intervalleMinutes = plageMinutes / nbFois;
      for (let i = 0; i < nbFois; i++) {
        const minutesDepuisDebut = Math.floor(intervalleMinutes * i + intervalleMinutes / 2);
        const heure = heureDebut + Math.floor(minutesDepuisDebut / 60);
        const minutes = Math.floor((minutesDepuisDebut % 60) / 30) * 30;
        horaires.push({ 
          seance: i + 1, 
          heure: `${heure.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}` 
        });
      }
    }
    this.horairesAutoCache[index] = horaires;
  }

  getHorairesAuto(index: number): { seance: number; heure: string }[] {
    return this.horairesAutoCache[index] || [];
  }

  toggleHorairesMode(index: number): void {
    const control = this.soinsArray.at(index);
    const current = control?.get('useHorairesPerso')?.value;
    control?.get('useHorairesPerso')?.setValue(!current);
  }

  updateHorairePerso(index: number, seanceIndex: number, heure: string): void {
    const control = this.soinsArray.at(index);
    const horaires: string[] = [...(control?.get('horairesPerso')?.value || [])];
    horaires[seanceIndex] = heure;
    control?.get('horairesPerso')?.setValue(horaires);
  }

  getHorairesPerso(index: number): string[] {
    return this.soinsArray.at(index)?.get('horairesPerso')?.value || [];
  }

  private emitChanges(): void {
    const soins: SoinComplementaire[] = this.soinsArray.controls.map((control, index) => {
      const usePerso = control.get('useHorairesPerso')?.value;
      const nbFois = control.get('nbFoisParJour')?.value || 1;
      const horairesPerso = control.get('horairesPerso')?.value || [];
      
      return {
        typeSoin: control.get('typeSoin')?.value === 'autre' 
          ? control.get('typeSoinAutre')?.value 
          : control.get('typeSoin')?.value,
        description: control.get('description')?.value === 'Autre' 
          ? control.get('descriptionAutre')?.value 
          : control.get('description')?.value,
        frequence: control.get('frequence')?.value,
        dureeJours: control.get('dureeJours')?.value,
        moments: control.get('moments')?.value,
        nbFoisParJour: nbFois,
        useHorairesPerso: usePerso,
        horairesPersonnalises: usePerso ? JSON.stringify(horairesPerso) : undefined,
        horairesAuto: this.horairesAutoCache[index],
        priorite: control.get('priorite')?.value,
        instructions: control.get('instructions')?.value
      };
    });
    this.soinsChange.emit(soins);
  }

  getSoinsData(): SoinComplementaire[] {
    return this.soinsArray.controls.map((control, index) => {
      const usePerso = control.get('useHorairesPerso')?.value;
      const nbFois = control.get('nbFoisParJour')?.value || 1;
      const horairesPerso = control.get('horairesPerso')?.value || [];
      
      return {
        typeSoin: control.get('typeSoin')?.value === 'autre' 
          ? control.get('typeSoinAutre')?.value 
          : control.get('typeSoin')?.value,
        description: control.get('description')?.value === 'Autre' 
          ? control.get('descriptionAutre')?.value 
          : control.get('description')?.value,
        frequence: control.get('frequence')?.value,
        dureeJours: control.get('dureeJours')?.value,
        moments: control.get('moments')?.value,
        nbFoisParJour: nbFois,
        useHorairesPerso: usePerso,
        horairesPersonnalises: usePerso ? JSON.stringify(horairesPerso) : undefined,
        horairesAuto: this.horairesAutoCache[index],
        priorite: control.get('priorite')?.value,
        instructions: control.get('instructions')?.value
      };
    });
  }

  isValid(): boolean {
    if (!this.soinsForm.valid) return false;
    
    for (let i = 0; i < this.soinsArray.length; i++) {
      const control = this.soinsArray.at(i);
      // Vérifier que typeSoinAutre est rempli quand type "autre" est sélectionné
      if (control.get('typeSoin')?.value === 'autre') {
        const typeSoinAutre = control.get('typeSoinAutre')?.value?.trim();
        if (!typeSoinAutre) return false;
      }
      // Vérifier que descriptionAutre est rempli quand "Autre" est sélectionné
      if (control.get('description')?.value === 'Autre') {
        const descriptionAutre = control.get('descriptionAutre')?.value?.trim();
        if (!descriptionAutre) return false;
      }
    }
    return true;
  }
}
