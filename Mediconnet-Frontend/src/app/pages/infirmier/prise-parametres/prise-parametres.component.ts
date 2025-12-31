import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { 
  LucideAngularModule, 
  DashboardLayoutComponent,
  ALL_ICONS_PROVIDER,
  PageHeaderComponent,
  PatientCardComponent,
  FormSectionComponent,
  AlertMessageComponent,
  LoadingStateComponent,
  EmptyStateComponent,
  PatientInfo
} from '../../../shared';
import { INFIRMIER_MENU_ITEMS, INFIRMIER_SIDEBAR_TITLE } from '../shared/infirmier-menu.config';
import { ParametreService, ParametreDto, CreateParametreByPatientRequest } from '../../../services/parametre.service';
import { PatientService, PatientBasicInfo } from '../../../services/patient.service';

@Component({
  selector: 'app-prise-parametres',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    LucideAngularModule,
    DashboardLayoutComponent,
    PageHeaderComponent,
    PatientCardComponent,
    FormSectionComponent,
    AlertMessageComponent,
    LoadingStateComponent,
    EmptyStateComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './prise-parametres.component.html',
  styleUrls: ['./prise-parametres.component.scss']
})
export class PriseParametresComponent implements OnInit {
  menuItems = INFIRMIER_MENU_ITEMS;
  sidebarTitle = INFIRMIER_SIDEBAR_TITLE;

  patientId: number = 0;
  patient: PatientBasicInfo | null = null;
  patientCardData: PatientInfo | null = null;
  isLoadingPatient = true;
  patientError: string | null = null;

  parametreForm!: FormGroup;
  isSubmitting = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  // Valeurs calculées
  imcValue: number | null = null;
  imcInterpretation: { label: string; color: string } = { label: '-', color: 'gray' };
  tensionInterpretation: { label: string; color: string } = { label: '-', color: 'gray' };
  temperatureInterpretation: { label: string; color: string } = { label: '-', color: 'gray' };

  // Historique
  historique: ParametreDto[] = [];
  loadingHistorique = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private parametreService: ParametreService,
    private patientService: PatientService
  ) {}

  ngOnInit(): void {
    this.initForm();
    
    this.route.params.subscribe(params => {
      this.patientId = +params['patientId'];
      if (this.patientId) {
        this.loadPatient();
        this.loadHistorique();
      }
    });
  }

  private initForm(): void {
    this.parametreForm = this.fb.group({
      poids: [null, [Validators.required, Validators.min(0.5), Validators.max(500)]],
      temperature: [null, [Validators.required, Validators.min(30), Validators.max(45)]],
      tensionSystolique: [null, [Validators.required, Validators.min(60), Validators.max(250)]],
      tensionDiastolique: [null, [Validators.required, Validators.min(40), Validators.max(150)]],
      taille: [null, [Validators.min(20), Validators.max(300)]]
    });

    this.parametreForm.valueChanges.subscribe(values => {
      this.updateCalculations(values);
    });
  }

  private loadPatient(): void {
    this.isLoadingPatient = true;
    this.patientError = null;

    this.patientService.getPatientById(this.patientId).subscribe({
      next: (response) => {
        if (response.success && response.patient) {
          this.patient = response.patient;
          this.patientCardData = {
            nom: response.patient.nom,
            prenom: response.patient.prenom,
            numeroDossier: response.patient.numeroDossier,
            dateNaissance: response.patient.dateNaissance,
            sexe: response.patient.sexe,
            groupeSanguin: response.patient.groupeSanguin
          };
        } else {
          this.patientError = 'Patient non trouvé';
        }
        this.isLoadingPatient = false;
      },
      error: (err) => {
        console.error('Erreur chargement patient:', err);
        this.patientError = 'Impossible de charger les informations du patient';
        this.isLoadingPatient = false;
      }
    });
  }

  loadHistorique(): void {
    this.loadingHistorique = true;
    this.parametreService.getHistoriquePatient(this.patientId).subscribe({
      next: (response) => {
        this.historique = response.data || [];
        this.loadingHistorique = false;
      },
      error: (err) => {
        console.error('Erreur chargement historique:', err);
        this.loadingHistorique = false;
      }
    });
  }

  private updateCalculations(values: any): void {
    this.imcValue = this.parametreService.calculerIMC(values.poids, values.taille);
    this.imcInterpretation = this.parametreService.interpreterIMC(this.imcValue);
    this.tensionInterpretation = this.parametreService.interpreterTension(
      values.tensionSystolique, 
      values.tensionDiastolique
    );
    this.temperatureInterpretation = this.parametreService.interpreterTemperature(values.temperature);
  }

  onSubmit(): void {
    if (this.parametreForm.invalid || this.isSubmitting) return;

    const sys = this.parametreForm.value.tensionSystolique;
    const dia = this.parametreForm.value.tensionDiastolique;
    if (sys && dia && sys <= dia) {
      this.errorMessage = 'La tension systolique doit être supérieure à la diastolique';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = null;
    this.successMessage = null;

    const request: CreateParametreByPatientRequest = {
      idPatient: this.patientId,
      ...this.parametreForm.value
    };

    this.parametreService.createByPatient(request).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        if (response.success) {
          this.successMessage = 'Paramètres enregistrés avec succès';
          this.parametreForm.reset();
          this.loadHistorique();
        } else {
          this.errorMessage = response.message || 'Erreur lors de l\'enregistrement';
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = err.error?.message || 'Erreur serveur';
        if (err.error?.errors) {
          this.errorMessage = err.error.errors.join(', ');
        }
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/infirmier/patients']);
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  calculateAge(dateStr?: string): number | null {
    if (!dateStr) return null;
    const birthDate = new Date(dateStr);
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    return age;
  }

  getIMCStatus(imc: number | null): { label: string; color: string } {
    return this.parametreService.interpreterIMC(imc);
  }

  getTensionStatus(sys: number | null, dia: number | null): { label: string; color: string } {
    return this.parametreService.interpreterTension(sys, dia);
  }

  get f() {
    return this.parametreForm.controls;
  }

  hasError(field: string): boolean {
    const control = this.parametreForm.get(field);
    return control ? control.invalid && control.touched : false;
  }
}
