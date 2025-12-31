import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { ALL_ICONS_PROVIDER } from '../../icons';
import { ParametreDto, ParametreService } from '../../../services/parametre.service';

/**
 * Composant partagé pour le formulaire de paramètres vitaux
 * Utilisé par l'accueil et l'infirmière
 */
@Component({
  selector: 'app-parametres-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './parametres-form.component.html',
  styleUrls: ['./parametres-form.component.scss']
})
export class ParametresFormComponent implements OnInit {
  @Input() consultationId!: number;
  @Input() patientNom: string = '';
  @Input() existingData: ParametreDto | null = null;
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() compact: boolean = false;

  @Output() saved = new EventEmitter<ParametreDto>();
  @Output() cancelled = new EventEmitter<void>();

  parametreForm!: FormGroup;
  isLoading = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  // Valeurs calculées
  imcValue: number | null = null;
  imcInterpretation: { label: string; color: string } = { label: '-', color: 'gray' };
  tensionInterpretation: { label: string; color: string } = { label: '-', color: 'gray' };
  temperatureInterpretation: { label: string; color: string } = { label: '-', color: 'gray' };

  constructor(
    private fb: FormBuilder,
    private parametreService: ParametreService
  ) {}

  ngOnInit(): void {
    this.initForm();
    if (this.existingData) {
      this.patchForm(this.existingData);
    }
  }

  private initForm(): void {
    this.parametreForm = this.fb.group({
      poids: [null, [Validators.required, Validators.min(0.5), Validators.max(500)]],
      temperature: [null, [Validators.required, Validators.min(30), Validators.max(45)]],
      tensionSystolique: [null, [Validators.required, Validators.min(60), Validators.max(250)]],
      tensionDiastolique: [null, [Validators.required, Validators.min(40), Validators.max(150)]],
      taille: [null, [Validators.min(20), Validators.max(300)]]
    });

    // Écouter les changements pour calculer l'IMC et interpréter les valeurs
    this.parametreForm.valueChanges.subscribe(values => {
      this.updateCalculations(values);
    });
  }

  private patchForm(data: ParametreDto): void {
    this.parametreForm.patchValue({
      poids: data.poids,
      temperature: data.temperature,
      tensionSystolique: data.tensionSystolique,
      tensionDiastolique: data.tensionDiastolique,
      taille: data.taille
    });
  }

  private updateCalculations(values: any): void {
    // IMC
    this.imcValue = this.parametreService.calculerIMC(values.poids, values.taille);
    this.imcInterpretation = this.parametreService.interpreterIMC(this.imcValue);

    // Tension
    this.tensionInterpretation = this.parametreService.interpreterTension(
      values.tensionSystolique, 
      values.tensionDiastolique
    );

    // Température
    this.temperatureInterpretation = this.parametreService.interpreterTemperature(values.temperature);
  }

  onSubmit(): void {
    if (this.parametreForm.invalid || this.isLoading) return;

    // Validation personnalisée : systolique > diastolique
    const sys = this.parametreForm.value.tensionSystolique;
    const dia = this.parametreForm.value.tensionDiastolique;
    if (sys && dia && sys <= dia) {
      this.errorMessage = 'La tension systolique doit être supérieure à la diastolique';
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;
    this.successMessage = null;

    const request = {
      idConsultation: this.consultationId,
      ...this.parametreForm.value
    };

    this.parametreService.createOrUpdate(request).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success && response.data) {
          this.successMessage = 'Paramètres enregistrés avec succès';
          this.saved.emit(response.data);
        } else {
          this.errorMessage = response.message || 'Erreur lors de l\'enregistrement';
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Erreur serveur';
        if (err.error?.errors) {
          this.errorMessage = err.error.errors.join(', ');
        }
      }
    });
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  get f() {
    return this.parametreForm.controls;
  }

  hasError(field: string): boolean {
    const control = this.parametreForm.get(field);
    return control ? control.invalid && control.touched : false;
  }
}
