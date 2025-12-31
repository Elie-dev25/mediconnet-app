import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { LucideAngularModule, DashboardLayoutComponent, ALL_ICONS_PROVIDER } from '../../../shared';
import { ADMIN_MENU_ITEMS, ADMIN_SIDEBAR_TITLE } from '../shared';
import { 
  AssuranceService, 
  AssuranceListItem, 
  AssuranceDetail,
  CreateAssuranceDto,
  TYPES_ASSURANCE,
  STATUTS_JURIDIQUES,
  ZONES_COUVERTURE,
  MODES_PAIEMENT
} from '../../../services/assurance.service';

interface Step {
  id: number;
  title: string;
  icon: string;
}

@Component({
  selector: 'app-admin-assurances',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    LucideAngularModule,
    DashboardLayoutComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './assurances.component.html',
  styleUrls: ['./assurances.component.scss']
})
export class AdminAssurancesComponent implements OnInit {
  menuItems = ADMIN_MENU_ITEMS;
  sidebarTitle = ADMIN_SIDEBAR_TITLE;

  // Données
  assurances: AssuranceListItem[] = [];
  selectedAssurance: AssuranceDetail | null = null;
  isLoading = false;
  
  // Filtres
  searchQuery = '';
  filterType = '';
  filterActive: boolean | null = null;

  // Options
  typesAssurance = TYPES_ASSURANCE;
  statutsJuridiques = STATUTS_JURIDIQUES;
  zonesCouverture = ZONES_COUVERTURE;
  modesPaiement = MODES_PAIEMENT;

  // Modal création/édition
  showModal = false;
  isEditing = false;
  currentStep = 1;
  steps: Step[] = [
    { id: 1, title: 'Identification', icon: 'building' },
    { id: 2, title: 'Administration', icon: 'briefcase' },
    { id: 3, title: 'Couverture', icon: 'heart-pulse' },
    { id: 4, title: 'Fonctionnement', icon: 'settings' }
  ];

  // Formulaires
  step1Form!: FormGroup;
  step2Form!: FormGroup;
  step3Form!: FormGroup;
  step4Form!: FormGroup;

  isSubmitting = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private fb: FormBuilder,
    private assuranceService: AssuranceService
  ) {
    this.initForms();
  }

  ngOnInit(): void {
    this.loadAssurances();
  }

  private initForms(): void {
    // Étape 1: Identification
    this.step1Form = this.fb.group({
      nom: ['', [Validators.required, Validators.minLength(3)]],
      typeAssurance: ['privee', Validators.required],
      telephoneServiceClient: [''],
      siteWeb: ['']
    });

    // Étape 2: Administration
    this.step2Form = this.fb.group({
      groupe: [''],
      paysOrigine: ['Cameroun'],
      statutJuridique: [''],
      description: ['']
    });

    // Étape 3: Couverture
    this.step3Form = this.fb.group({
      typeCouverture: [''],
      isComplementaire: [false],
      categorieBeneficiaires: ['']
    });

    // Étape 4: Fonctionnement
    this.step4Form = this.fb.group({
      conditionsAdhesion: [''],
      zoneCouverture: ['national'],
      modePaiement: [''],
      isActive: [true]
    });
  }

  loadAssurances(): void {
    this.isLoading = true;
    this.assuranceService.getAssurances({
      recherche: this.searchQuery,
      typeAssurance: this.filterType || undefined,
      isActive: this.filterActive ?? undefined
    }).pipe(
      finalize(() => {
        this.isLoading = false;
      })
    ).subscribe({
      next: (response) => {
        this.assurances = response.data;
      },
      error: (err) => {
        console.error('Erreur chargement assurances:', err);
      }
    });
  }

  onSearch(): void {
    this.loadAssurances();
  }

  onFilterChange(): void {
    this.loadAssurances();
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.filterType = '';
    this.filterActive = null;
    this.loadAssurances();
  }

  // Modal
  openCreateModal(): void {
    this.isEditing = false;
    this.selectedAssurance = null;
    this.resetForms();
    this.currentStep = 1;
    this.showModal = true;
  }

  openEditModal(assurance: AssuranceListItem): void {
    this.isEditing = true;
    this.isLoading = true;
    
    this.assuranceService.getAssuranceById(assurance.idAssurance).pipe(
      finalize(() => {
        this.isLoading = false;
      })
    ).subscribe({
      next: (detail) => {
        this.selectedAssurance = detail;
        this.populateForms(detail);
        this.currentStep = 1;
        this.showModal = true;
      },
      error: (err) => {
        console.error('Erreur chargement détail:', err);
      }
    });
  }

  closeModal(): void {
    this.showModal = false;
    this.resetForms();
    this.errorMessage = '';
    this.successMessage = '';
  }

  private resetForms(): void {
    this.step1Form.reset({ typeAssurance: 'privee' });
    this.step2Form.reset({ paysOrigine: 'Cameroun' });
    this.step3Form.reset({ isComplementaire: false });
    this.step4Form.reset({ zoneCouverture: 'national', isActive: true });
  }

  private populateForms(a: AssuranceDetail): void {
    this.step1Form.patchValue({
      nom: a.nom,
      typeAssurance: a.typeAssurance,
      telephoneServiceClient: a.telephoneServiceClient,
      siteWeb: a.siteWeb
    });
    this.step2Form.patchValue({
      groupe: a.groupe,
      paysOrigine: a.paysOrigine,
      statutJuridique: a.statutJuridique,
      description: a.description
    });
    this.step3Form.patchValue({
      typeCouverture: a.typeCouverture,
      isComplementaire: a.isComplementaire,
      categorieBeneficiaires: a.categorieBeneficiaires
    });
    this.step4Form.patchValue({
      conditionsAdhesion: a.conditionsAdhesion,
      zoneCouverture: a.zoneCouverture,
      modePaiement: a.modePaiement,
      isActive: a.isActive
    });
  }

  // Navigation stepper
  nextStep(): void {
    if (this.canProceed()) {
      this.currentStep++;
    }
  }

  prevStep(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
    }
  }

  goToStep(stepId: number): void {
    if (stepId <= this.currentStep || this.canProceed()) {
      this.currentStep = stepId;
    }
  }

  canProceed(): boolean {
    switch (this.currentStep) {
      case 1: return this.step1Form.valid;
      case 2: return true;
      case 3: return true;
      case 4: return true;
      default: return false;
    }
  }

  // Soumission
  submitForm(): void {
    if (this.isSubmitting) {
      return;
    }

    if (!this.step1Form.valid) {
      this.errorMessage = 'Veuillez compléter les champs obligatoires';
      this.currentStep = 1;
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    const data: CreateAssuranceDto = {
      ...this.step1Form.value,
      ...this.step2Form.value,
      ...this.step3Form.value,
      ...this.step4Form.value
    };

    const request = this.isEditing && this.selectedAssurance
      ? this.assuranceService.updateAssurance(this.selectedAssurance.idAssurance, data)
      : this.assuranceService.createAssurance(data);

    request.pipe(
      finalize(() => {
        this.isSubmitting = false;
      })
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.successMessage = this.isEditing ? 'Assurance mise à jour' : 'Assurance créée';
          setTimeout(() => {
            this.closeModal();
            this.loadAssurances();
          }, 1500);
        } else {
          this.errorMessage = response.message;
        }
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Erreur lors de la sauvegarde';
      }
    });
  }

  // Actions
  toggleStatus(assurance: AssuranceListItem): void {
    this.assuranceService.toggleAssuranceStatus(assurance.idAssurance).subscribe({
      next: () => this.loadAssurances(),
      error: (err) => console.error('Erreur toggle status:', err)
    });
  }

  deleteAssurance(assurance: AssuranceListItem): void {
    if (!confirm(`Supprimer l'assurance "${assurance.nom}" ?`)) return;

    this.assuranceService.deleteAssurance(assurance.idAssurance).subscribe({
      next: (response) => {
        if (response.success) {
          this.loadAssurances();
        } else {
          alert(response.message);
        }
      },
      error: (err) => {
        alert(err.error?.message || 'Erreur lors de la suppression');
      }
    });
  }

  getTypeLabel(value: string): string {
    return this.typesAssurance.find(t => t.value === value)?.label || value;
  }
}
