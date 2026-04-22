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
  RISQUES_OPERATOIRES,
  STATUTS_PROGRAMMATION
} from '../../../services/programmation-intervention.service';
import {
  BlocOperatoireService,
  BlocOperatoireListDto,
  DisponibiliteBlocDto
} from '../../../services/bloc-operatoire.service';
import {
  CoordinationInterventionService,
  AnesthesisteDisponibilite,
  CreneauDisponible,
  ProposerCoordinationRequest
} from '../../../services/coordination-intervention.service';

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
  statutsProgrammation = STATUTS_PROGRAMMATION;

  minDate: string = '';
  
  // Annulation
  showAnnulationConfirm = false;
  motifAnnulation = '';
  isAnnulating = false;

  // Blocs opératoires
  blocs: BlocOperatoireListDto[] = [];
  disponibilites: DisponibiliteBlocDto[] = [];
  isLoadingBlocs = false;
  isCheckingDisponibilite = false;
  selectedBlocId: number | null = null;
  chirurgienDisponible = true;
  messageIndisponibilite: string | null = null;
  
  // Gestion des conflits RDV
  hasInterventionConflict = false;
  hasRdvConflicts = false;
  rdvsEnConflit: { idRendezVous: number; dateHeure: string; duree: number; patientNom: string; patientPrenom: string; motif?: string }[] = [];
  showRdvConflictModal = false;
  isConfirmingAnnulation = false;

  // Coordination anesthésiste
  currentStep: 'info' | 'anesthesiste' | 'confirmation' = 'info';
  anesthesistes: AnesthesisteDisponibilite[] = [];
  selectedAnesthesiste: AnesthesisteDisponibilite | null = null;
  creneauxAnesthesiste: CreneauDisponible[] = [];
  isLoadingAnesthesistes = false;
  isLoadingCreneaux = false;
  isProposingCoordination = false;
  coordinationError: string | null = null;
  coordinationSuccess = false;

  constructor(
    private fb: FormBuilder,
    private programmationService: ProgrammationInterventionService,
    private blocService: BlocOperatoireService,
    private coordinationService: CoordinationInterventionService
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
          // Stocker la programmation créée pour permettre la coordination
          this.existingProgrammation = {
            idProgrammation: response.idProgrammation,
            idConsultation: this.consultationId,
            idPatient: 0,
            idChirurgien: 0,
            typeIntervention: formValue.typeIntervention,
            indicationOperatoire: formValue.indicationOperatoire,
            statut: 'en_attente_coordination',
            consentementEclaire: formValue.consentementEclaire,
            createdAt: new Date().toISOString()
          };
          // Passer automatiquement à l'étape de sélection d'anesthésiste
          this.goToAnesthesisteStep();
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
      this.chirurgienDisponible = true;
      this.messageIndisponibilite = null;
      this.hasInterventionConflict = false;
      this.hasRdvConflicts = false;
      this.rdvsEnConflit = [];
      return;
    }

    this.isCheckingDisponibilite = true;
    this.blocService.getDisponibilites(datePrevue, heureDebut, dureeEstimee).subscribe({
      next: (response) => {
        this.disponibilites = response.blocs;
        this.chirurgienDisponible = response.chirurgienDisponible;
        this.hasInterventionConflict = response.hasInterventionConflict;
        this.hasRdvConflicts = response.hasRdvConflicts;
        this.rdvsEnConflit = response.rdvsEnConflit || [];
        this.messageIndisponibilite = response.messageIndisponibilite || null;
        this.isCheckingDisponibilite = false;
      },
      error: (err) => {
        console.error('Erreur vérification disponibilité:', err);
        this.isCheckingDisponibilite = false;
      }
    });
  }

  selectBloc(idBloc: number): void {
    // Bloquer si conflit d'intervention (bloquant)
    if (this.hasInterventionConflict) return;
    const bloc = this.disponibilites.find(d => d.idBloc === idBloc);
    if (bloc && bloc.estDisponible) {
      this.selectedBlocId = idBloc;
      this.form.patchValue({ idBloc: idBloc });
    }
  }

  // Ouvrir la modal de confirmation d'annulation des RDV
  openRdvConflictModal(): void {
    this.showRdvConflictModal = true;
  }

  // Fermer la modal
  closeRdvConflictModal(): void {
    this.showRdvConflictModal = false;
  }

  // Confirmer l'annulation des RDV en conflit
  confirmerAnnulationRdv(): void {
    const datePrevue = this.form.get('datePrevue')?.value;
    const heureDebut = this.form.get('heureDebut')?.value;
    const dureeEstimee = this.form.get('dureeEstimee')?.value;

    if (!datePrevue || !heureDebut || !dureeEstimee) return;

    this.isConfirmingAnnulation = true;
    
    const request = {
      date: datePrevue,
      heureDebut: heureDebut,
      dureeMinutes: dureeEstimee,
      patientIntervention: this.patientNom || 'Patient',
      nomChirurgien: 'Chirurgien' // Sera récupéré côté backend
    };

    this.blocService.confirmerAnnulationRdv(request).subscribe({
      next: () => {
        this.isConfirmingAnnulation = false;
        this.showRdvConflictModal = false;
        this.hasRdvConflicts = false;
        this.rdvsEnConflit = [];
        this.messageIndisponibilite = null;
        // Recharger les disponibilités
        this.checkDisponibilite();
      },
      error: (err) => {
        console.error('Erreur annulation RDV:', err);
        this.isConfirmingAnnulation = false;
      }
    });
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

  // ==================== COORDINATION ANESTHÉSISTE ====================

  goToAnesthesisteStep(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.currentStep = 'anesthesiste';
    this.loadAnesthesistes();
  }

  goBackToInfo(): void {
    this.currentStep = 'info';
    this.selectedAnesthesiste = null;
    this.creneauxAnesthesiste = [];
  }

  private loadAnesthesistes(): void {
    const datePrevue = this.form.get('datePrevue')?.value;
    const dureeEstimee = this.form.get('dureeEstimee')?.value || 60;

    if (!datePrevue) {
      this.anesthesistes = [];
      return;
    }

    this.isLoadingAnesthesistes = true;
    const dateDebut = datePrevue;
    const dateFin = new Date(new Date(datePrevue).getTime() + 7 * 24 * 60 * 60 * 1000)
      .toISOString().split('T')[0];

    this.coordinationService.getAnesthesistesDisponibles(dateDebut, dateFin, dureeEstimee)
      .subscribe({
        next: (anesthesistes) => {
          this.anesthesistes = anesthesistes;
          this.isLoadingAnesthesistes = false;
        },
        error: (err) => {
          console.error('Erreur chargement anesthésistes:', err);
          this.isLoadingAnesthesistes = false;
        }
      });
  }

  selectAnesthesiste(anesth: AnesthesisteDisponibilite): void {
    this.selectedAnesthesiste = anesth;
    this.loadCreneauxAnesthesiste(anesth.idMedecin);
  }

  private loadCreneauxAnesthesiste(idAnesthesiste: number): void {
    const datePrevue = this.form.get('datePrevue')?.value;
    if (!datePrevue) return;

    this.isLoadingCreneaux = true;
    const dateDebut = datePrevue;
    const dateFin = new Date(new Date(datePrevue).getTime() + 14 * 24 * 60 * 60 * 1000)
      .toISOString().split('T')[0];

    this.coordinationService.getCreneauxAnesthesiste(idAnesthesiste, dateDebut, dateFin)
      .subscribe({
        next: (creneaux) => {
          this.creneauxAnesthesiste = creneaux.filter(c => c.estDisponible);
          this.isLoadingCreneaux = false;
        },
        error: (err) => {
          console.error('Erreur chargement créneaux:', err);
          this.isLoadingCreneaux = false;
        }
      });
  }

  selectCreneau(creneau: CreneauDisponible): void {
    this.form.patchValue({
      datePrevue: creneau.date.split('T')[0],
      heureDebut: creneau.heureDebut
    });
  }

  proposerCoordination(): void {
    if (!this.selectedAnesthesiste || !this.existingProgrammation) {
      this.coordinationError = 'Veuillez sélectionner un anesthésiste';
      return;
    }

    const formValue = this.form.value;
    if (!formValue.datePrevue || !formValue.heureDebut || !formValue.dureeEstimee) {
      this.coordinationError = 'Veuillez renseigner la date, l\'heure et la durée';
      return;
    }

    this.isProposingCoordination = true;
    this.coordinationError = null;

    const request: ProposerCoordinationRequest = {
      idProgrammation: this.existingProgrammation.idProgrammation,
      idAnesthesiste: this.selectedAnesthesiste.idMedecin,
      dateProposee: new Date(formValue.datePrevue).toISOString(),
      heureProposee: formValue.heureDebut,
      dureeEstimee: formValue.dureeEstimee,
      notesChirurgien: formValue.notesAnesthesie
    };

    this.coordinationService.proposerCoordination(request).subscribe({
      next: (response) => {
        this.isProposingCoordination = false;
        if (response.success) {
          this.coordinationSuccess = true;
          this.currentStep = 'confirmation';
        } else {
          this.coordinationError = response.message;
        }
      },
      error: (err) => {
        this.isProposingCoordination = false;
        this.coordinationError = err.error?.message || 'Erreur lors de la proposition';
      }
    });
  }

  formatCreneauDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { weekday: 'long', day: 'numeric', month: 'long' });
  }

  formatDuree(minutes: number): string {
    return this.coordinationService.formatDuree(minutes);
  }

  getCreneauxGroupedByDate(): { date: string; creneaux: CreneauDisponible[] }[] {
    const grouped = new Map<string, CreneauDisponible[]>();
    
    for (const creneau of this.creneauxAnesthesiste) {
      const dateKey = creneau.date.split('T')[0];
      if (!grouped.has(dateKey)) {
        grouped.set(dateKey, []);
      }
      grouped.get(dateKey)!.push(creneau);
    }

    return Array.from(grouped.entries()).map(([date, creneaux]) => ({ date, creneaux }));
  }

  // Helper methods for time-based styling
  isMorning(heure: string): boolean {
    const hour = Number.parseInt(heure.split(':')[0]);
    return hour >= 6 && hour < 12;
  }

  isAfternoon(heure: string): boolean {
    const hour = Number.parseInt(heure.split(':')[0]);
    return hour >= 12 && hour < 18;
  }

  isEvening(heure: string): boolean {
    const hour = Number.parseInt(heure.split(':')[0]);
    return hour >= 18 && hour < 22;
  }

  // ==================== STATUT & ANNULATION ====================

  getStatutLabel(statut: string): string {
    return this.programmationService.getStatutLabel(statut);
  }

  getStatutColor(statut: string): string {
    return this.programmationService.getStatutColor(statut);
  }

  /**
   * Vérifie si l'intervention peut être modifiée
   * Le chirurgien peut modifier même après validation par l'anesthésiste
   */
  canModify(): boolean {
    if (!this.existingProgrammation) return true;
    // Seules les interventions réalisées ou annulées ne peuvent pas être modifiées
    return !['realisee', 'annulee'].includes(this.existingProgrammation.statut);
  }

  /**
   * Vérifie si l'intervention peut être annulée
   */
  canCancel(): boolean {
    if (!this.existingProgrammation) return false;
    // On ne peut pas annuler une intervention déjà réalisée ou annulée
    return !['realisee', 'annulee'].includes(this.existingProgrammation.statut);
  }

  /**
   * Ouvre la confirmation d'annulation
   */
  openAnnulationConfirm(): void {
    this.showAnnulationConfirm = true;
    this.motifAnnulation = '';
  }

  /**
   * Ferme la confirmation d'annulation
   */
  closeAnnulationConfirm(): void {
    this.showAnnulationConfirm = false;
    this.motifAnnulation = '';
  }

  /**
   * Confirme l'annulation de l'intervention
   */
  confirmAnnulation(): void {
    if (!this.existingProgrammation) return;

    this.isAnnulating = true;
    this.programmationService.annulerProgrammation(
      this.existingProgrammation.idProgrammation,
      this.motifAnnulation || undefined
    ).subscribe({
      next: () => {
        this.isAnnulating = false;
        this.showAnnulationConfirm = false;
        // Mettre à jour le statut local
        if (this.existingProgrammation) {
          this.existingProgrammation.statut = 'annulee';
          this.existingProgrammation.motifAnnulation = this.motifAnnulation;
        }
        this.saved.emit(this.existingProgrammation!.idProgrammation);
      },
      error: (err) => {
        this.isAnnulating = false;
        this.error = err.error?.message || 'Erreur lors de l\'annulation';
      }
    });
  }

  /**
   * Sauvegarde les modifications (mise à jour)
   */
  saveModifications(): void {
    if (this.form.invalid || !this.existingProgrammation) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.error = null;

    const formValue = this.form.value;

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
  }
}
