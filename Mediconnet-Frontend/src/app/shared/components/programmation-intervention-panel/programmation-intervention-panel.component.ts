import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { 
  ProgrammationInterventionService, 
  ProgrammationInterventionDto,
  CreateProgrammationRequest,
  TYPES_INTERVENTION,
  CLASSIFICATIONS_ASA,
  RISQUES_OPERATOIRES
} from '../../../services/programmation-intervention.service';
import {
  BlocOperatoireService,
  BlocOperatoireListDto,
  DisponibiliteBlocDto
} from '../../../services/bloc-operatoire.service';

@Component({
  selector: 'app-programmation-intervention-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LucideAngularModule],
  templateUrl: './programmation-intervention-panel.component.html',
  styleUrl: './programmation-intervention-panel.component.scss'
})
export class ProgrammationInterventionPanelComponent implements OnInit {
  @Input() consultationId!: number;
  @Input() patientNom: string = '';
  @Input() indicationFromExamen: string = '';
  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<number>();

  form!: FormGroup;
  isLoading = false;
  isSaving = false;
  error: string | null = null;
  existingProgrammation: ProgrammationInterventionDto | null = null;

  typesIntervention = TYPES_INTERVENTION;
  classificationsAsa = CLASSIFICATIONS_ASA;
  risquesOperatoires = RISQUES_OPERATOIRES;

  minDate: string = '';

  // Blocs opératoires
  blocs: BlocOperatoireListDto[] = [];
  disponibilites: DisponibiliteBlocDto[] = [];
  isLoadingBlocs = false;
  isCheckingDisponibilite = false;
  selectedBlocId: number | null = null;

  constructor(
    private fb: FormBuilder,
    private programmationService: ProgrammationInterventionService,
    private blocService: BlocOperatoireService
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.setMinDate();
    this.loadExistingProgrammation();
    this.loadBlocs();
  }

  private initForm(): void {
    this.form = this.fb.group({
      typeIntervention: ['programmee', Validators.required],
      classificationAsa: [''],
      risqueOperatoire: [''],
      indicationOperatoire: [this.indicationFromExamen || '', Validators.required],
      techniquePrevue: [''],
      datePrevue: [''],
      heureDebut: [''],
      dureeEstimee: [null],
      idBloc: [null],
      notesAnesthesie: [''],
      bilanPreoperatoire: [''],
      instructionsPatient: [''],
      consentementEclaire: [false],
      notes: ['']
    });

    // Surveiller les changements pour vérifier la disponibilité
    this.form.get('datePrevue')?.valueChanges.subscribe(() => this.checkDisponibilite());
    this.form.get('heureDebut')?.valueChanges.subscribe(() => this.checkDisponibilite());
    this.form.get('dureeEstimee')?.valueChanges.subscribe(() => this.checkDisponibilite());
  }

  private setMinDate(): void {
    const today = new Date();
    this.minDate = today.toISOString().split('T')[0];
  }

  private loadExistingProgrammation(): void {
    this.isLoading = true;
    this.programmationService.getByConsultation(this.consultationId).subscribe({
      next: (response) => {
        if (response.exists && response.programmation) {
          this.existingProgrammation = response.programmation;
          this.patchForm(response.programmation);
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement programmation:', err);
        this.isLoading = false;
      }
    });
  }

  private patchForm(prog: ProgrammationInterventionDto): void {
    this.form.patchValue({
      typeIntervention: prog.typeIntervention,
      classificationAsa: prog.classificationAsa || '',
      risqueOperatoire: prog.risqueOperatoire || '',
      indicationOperatoire: prog.indicationOperatoire || '',
      techniquePrevue: prog.techniquePrevue || '',
      datePrevue: prog.datePrevue ? prog.datePrevue.split('T')[0] : '',
      heureDebut: prog.heureDebut || '',
      dureeEstimee: prog.dureeEstimee,
      notesAnesthesie: prog.notesAnesthesie || '',
      bilanPreoperatoire: prog.bilanPreoperatoire || '',
      instructionsPatient: prog.instructionsPatient || '',
      consentementEclaire: prog.consentementEclaire,
      notes: prog.notes || ''
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.error = null;

    const formValue = this.form.value;

    if (this.existingProgrammation) {
      // Mise à jour
      this.programmationService.updateProgrammation(this.existingProgrammation.idProgrammation, {
        typeIntervention: formValue.typeIntervention,
        classificationAsa: formValue.classificationAsa || undefined,
        risqueOperatoire: formValue.risqueOperatoire || undefined,
        indicationOperatoire: formValue.indicationOperatoire,
        techniquePrevue: formValue.techniquePrevue || undefined,
        datePrevue: formValue.datePrevue ? new Date(formValue.datePrevue).toISOString() : undefined,
        heureDebut: formValue.heureDebut || undefined,
        dureeEstimee: formValue.dureeEstimee,
        notesAnesthesie: formValue.notesAnesthesie || undefined,
        bilanPreoperatoire: formValue.bilanPreoperatoire || undefined,
        instructionsPatient: formValue.instructionsPatient || undefined,
        consentementEclaire: formValue.consentementEclaire,
        notes: formValue.notes || undefined
      }).subscribe({
        next: () => {
          this.isSaving = false;
          this.saved.emit(this.existingProgrammation!.idProgrammation);
        },
        error: (err) => {
          this.isSaving = false;
          this.error = err.error?.message || 'Erreur lors de la mise à jour';
        }
      });
    } else {
      // Création
      const request: CreateProgrammationRequest = {
        idConsultation: this.consultationId,
        typeIntervention: formValue.typeIntervention,
        classificationAsa: formValue.classificationAsa || undefined,
        risqueOperatoire: formValue.risqueOperatoire || undefined,
        indicationOperatoire: formValue.indicationOperatoire,
        techniquePrevue: formValue.techniquePrevue || undefined,
        datePrevue: formValue.datePrevue ? new Date(formValue.datePrevue).toISOString() : undefined,
        heureDebut: formValue.heureDebut || undefined,
        dureeEstimee: formValue.dureeEstimee || undefined,
        notesAnesthesie: formValue.notesAnesthesie || undefined,
        bilanPreoperatoire: formValue.bilanPreoperatoire || undefined,
        instructionsPatient: formValue.instructionsPatient || undefined,
        consentementEclaire: formValue.consentementEclaire,
        notes: formValue.notes || undefined
      };

      this.programmationService.createProgrammation(request).subscribe({
        next: (response) => {
          this.isSaving = false;
          this.saved.emit(response.idProgrammation);
        },
        error: (err) => {
          this.isSaving = false;
          this.error = err.error?.message || 'Erreur lors de la création';
        }
      });
    }
  }

  onClose(): void {
    this.closed.emit();
  }

  getTypeLabel(value: string): string {
    return this.typesIntervention.find(t => t.value === value)?.label || value;
  }

  getAsaLabel(value: string): string {
    return this.classificationsAsa.find(a => a.value === value)?.label || value;
  }

  getRisqueLabel(value: string): string {
    return this.risquesOperatoires.find(r => r.value === value)?.label || value;
  }

  // ==================== BLOCS OPÉRATOIRES ====================

  private loadBlocs(): void {
    this.isLoadingBlocs = true;
    this.blocService.getAllBlocs().subscribe({
      next: (blocs) => {
        this.blocs = blocs.filter(b => b.actif && b.statut !== 'maintenance');
        this.isLoadingBlocs = false;
      },
      error: (err) => {
        console.error('Erreur chargement blocs:', err);
        this.isLoadingBlocs = false;
      }
    });
  }

  checkDisponibilite(): void {
    const datePrevue = this.form.get('datePrevue')?.value;
    const heureDebut = this.form.get('heureDebut')?.value;
    const dureeEstimee = this.form.get('dureeEstimee')?.value;

    if (!datePrevue || !heureDebut || !dureeEstimee) {
      this.disponibilites = [];
      return;
    }

    this.isCheckingDisponibilite = true;
    this.blocService.getDisponibilites(datePrevue, heureDebut, dureeEstimee).subscribe({
      next: (disponibilites) => {
        this.disponibilites = disponibilites;
        this.isCheckingDisponibilite = false;
      },
      error: (err) => {
        console.error('Erreur vérification disponibilité:', err);
        this.isCheckingDisponibilite = false;
      }
    });
  }

  selectBloc(idBloc: number): void {
    const bloc = this.disponibilites.find(d => d.idBloc === idBloc);
    if (bloc && bloc.estDisponible) {
      this.selectedBlocId = idBloc;
      this.form.patchValue({ idBloc: idBloc });
    }
  }

  getBlocDisponibilite(idBloc: number): DisponibiliteBlocDto | undefined {
    return this.disponibilites.find(d => d.idBloc === idBloc);
  }

  isBlocDisponible(idBloc: number): boolean {
    const dispo = this.getBlocDisponibilite(idBloc);
    return dispo?.estDisponible ?? true;
  }

  getBlocStatutClass(bloc: BlocOperatoireListDto): string {
    if (bloc.statut === 'maintenance') return 'badge-warning';
    if (bloc.statut === 'occupe') return 'badge-danger';
    return 'badge-success';
  }

  getBlocStatutLabel(bloc: BlocOperatoireListDto): string {
    return this.blocService.getStatutLabel(bloc.statut);
  }
}
