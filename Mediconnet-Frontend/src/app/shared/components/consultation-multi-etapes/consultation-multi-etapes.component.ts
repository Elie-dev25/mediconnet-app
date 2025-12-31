import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { Subscription } from 'rxjs';
import { 
  ConsultationCompleteService, 
  ConsultationEnCoursDto,
  AnamneseDto,
  DiagnosticDto,
  PrescriptionsDto,
  MedicamentDto,
  ExamenPrescritDto,
  RecommandationDto
} from '../../../services/consultation-complete.service';
import { QuestionsPredefiniesService, QuestionPredefinie } from '../../../services/questions-predefinies.service';
import { HospitalisationService, LitDto, ChambreDto, DemandeHospitalisationRequest } from '../../../services/hospitalisation.service';
import { SpeechRecognitionService, SupportedLanguage } from '../../../services/speech-recognition.service';
import { PharmacieStockService, MedicamentStock } from '../../../services/pharmacie-stock.service';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';

type EtapeConsultation = 'anamnese' | 'diagnostic' | 'prescriptions' | 'validation';

@Component({
  selector: 'app-consultation-multi-etapes',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LucideAngularModule],
  templateUrl: './consultation-multi-etapes.component.html',
  styleUrl: './consultation-multi-etapes.component.scss'
})
export class ConsultationMultiEtapesComponent implements OnInit, OnDestroy {
  @Input() consultationId!: number;
  @Input() patientId!: number;
  @Input() patientNom: string = '';
  @Output() completed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  etapeActuelle: EtapeConsultation = 'anamnese';
  etapes: EtapeConsultation[] = ['anamnese', 'diagnostic', 'prescriptions', 'validation'];
  
  consultation: ConsultationEnCoursDto | null = null;
  questionsPredefinies: QuestionPredefinie[] = [];
  
  isLoading = true;
  isSaving = false;
  error: string | null = null;

  // Hospitalisation
  showHospitalisationForm = false;
  litsDisponibles: LitDto[] = [];
  chambresDisponibles: ChambreDto[] = [];
  hospitalisationMotif = '';
  hospitalisationUrgence = 'normale';
  hospitalisationNotes = '';
  selectedChambreId: number | null = null;
  selectedLitId: number | null = null;
  isLoadingLits = false;
  hospitalisationDemandee = false;

  // Autocomplete médicaments
  medicamentSuggestions: MedicamentStock[] = [];
  activeMedicamentIndex: number | null = null;
  private searchMedicament$ = new Subject<{index: number, term: string}>();

  // Questions libres
  showAddQuestion = false;
  newQuestionText = '';

  // Formulaires
  anamneseForm!: FormGroup;
  diagnosticForm!: FormGroup;
  prescriptionsForm!: FormGroup;

  // Dictée vocale
  isVoiceSupported = false;
  isRecording = false;
  activeVoiceField: string | null = null;
  voiceError: string | null = null;
  selectedLanguage: SupportedLanguage = 'fr-FR';
  interimTranscript: string = '';
  hideVoiceBanner = false;
  private voiceSubscriptions: Subscription[] = [];

  constructor(
    private fb: FormBuilder,
    private consultationService: ConsultationCompleteService,
    private questionsPredefiniesService: QuestionsPredefiniesService,
    private hospitalisationService: HospitalisationService,
    private speechService: SpeechRecognitionService,
    private pharmacieService: PharmacieStockService
  ) {
    this.initForms();
    this.isVoiceSupported = this.speechService.isSupported;
  }

  ngOnInit(): void {
    this.loadConsultation();
    this.setupVoiceRecognition();
    this.setupMedicamentAutocomplete();
  }

  ngOnDestroy(): void {
    this.voiceSubscriptions.forEach(sub => sub.unsubscribe());
    this.searchMedicament$.complete();
    if (this.isRecording) {
      this.speechService.stop();
    }
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

  onMedicamentSearch(index: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchMedicament$.next({ index, term: input.value });
  }

  selectMedicament(index: number, medicament: MedicamentStock): void {
    const control = this.medicamentsArray.at(index);
    if (control) {
      control.patchValue({
        nomMedicament: medicament.nom + (medicament.dosage ? ' ' + medicament.dosage : ''),
        dosage: medicament.dosage || ''
      });
    }
    this.medicamentSuggestions = [];
    this.activeMedicamentIndex = null;
  }

  hideMedicamentSuggestions(): void {
    // Délai pour permettre le clic sur une suggestion
    setTimeout(() => {
      this.medicamentSuggestions = [];
      this.activeMedicamentIndex = null;
    }, 200);
  }

  private setupVoiceRecognition(): void {
    if (!this.isVoiceSupported) {
      console.warn('[ConsultationVoice] Voice recognition not supported');
      return;
    }

    console.log('[ConsultationVoice] Setting up voice recognition');
    
    // Définir la langue par défaut
    this.speechService.setLanguage(this.selectedLanguage);

    this.voiceSubscriptions.push(
      this.speechService.listening$.subscribe(isListening => {
        console.log('[ConsultationVoice] Listening state changed:', isListening);
        this.isRecording = isListening;
        if (!isListening) {
          this.interimTranscript = '';
        }
      })
    );

    this.voiceSubscriptions.push(
      this.speechService.transcripts$.subscribe(event => {
        console.log('[ConsultationVoice] Transcript received:', event, 'activeField:', this.activeVoiceField);
        
        if (!this.activeVoiceField) {
          console.warn('[ConsultationVoice] No active field, ignoring transcript');
          return;
        }
        
        if (event.isFinal) {
          console.log('[ConsultationVoice] Final transcript, appending to:', this.activeVoiceField);
          this.appendToField(this.activeVoiceField, event.transcript);
          this.interimTranscript = '';
        } else {
          this.interimTranscript = event.transcript;
        }
      })
    );

    this.voiceSubscriptions.push(
      this.speechService.errors$.subscribe(error => {
        console.error('[ConsultationVoice] Error:', error);
        this.voiceError = error;
        const timeout = error.includes('refusé') || error.includes('non disponible') ? 8000 : 5000;
        setTimeout(() => {
          if (this.voiceError === error) {
            this.voiceError = null;
          }
        }, timeout);
      })
    );
  }

  /**
   * Change la langue de reconnaissance vocale
   */
  setVoiceLanguage(lang: SupportedLanguage): void {
    this.selectedLanguage = lang;
    this.speechService.setLanguage(lang);
    
    // Si enregistrement en cours, redémarrer avec la nouvelle langue
    if (this.isRecording && this.activeVoiceField) {
      const field = this.activeVoiceField;
      this.speechService.stop();
      setTimeout(() => {
        this.toggleVoiceInput(field);
      }, 200);
    }
  }

  toggleVoiceInput(fieldName: string): void {
    if (!this.isVoiceSupported) {
      this.voiceError = 'La reconnaissance vocale n\'est pas supportée. Utilisez Chrome ou Edge.';
      return;
    }

    if (this.isRecording && this.activeVoiceField === fieldName) {
      // Arrêter l'enregistrement
      this.speechService.stop();
      this.activeVoiceField = null;
      this.interimTranscript = '';
    } else {
      // Arrêter l'ancien enregistrement si existe
      if (this.isRecording) {
        this.speechService.stop();
      }
      
      // Démarrer sur le nouveau champ
      this.activeVoiceField = fieldName;
      this.interimTranscript = '';
      this.voiceError = null;
      
      // Démarrer avec un petit délai pour s'assurer que l'ancien est arrêté
      setTimeout(() => {
        const started = this.speechService.start(this.consultationId, fieldName, undefined, this.selectedLanguage);
        if (!started) {
          this.activeVoiceField = null;
        }
      }, 100);
    }
  }

  private appendToField(fieldName: string, text: string): void {
    console.log('[ConsultationVoice] appendToField called:', { fieldName, text });
    
    const trimmedText = text.trim();
    if (!trimmedText) {
      console.warn('[ConsultationVoice] Empty text, skipping');
      return;
    }

    // Déterminer le formulaire et le champ
    let control: any = null;
    let controlPath = '';

    if (fieldName.startsWith('anamnese.')) {
      controlPath = fieldName.replace('anamnese.', '');
      
      // Gérer les chemins imbriqués pour les questions (ex: questionsReponses.0.reponse)
      if (controlPath.includes('.')) {
        const parts = controlPath.split('.');
        if (parts[0] === 'questionsReponses' || parts[0] === 'questionsLibres') {
          const arrayName = parts[0];
          const index = parseInt(parts[1], 10);
          const fieldKey = parts[2];
          const formArray = this.anamneseForm.get(arrayName) as FormArray;
          if (formArray && formArray.at(index)) {
            control = formArray.at(index).get(fieldKey);
          }
        }
      } else {
        control = this.anamneseForm.get(controlPath);
      }
    } else if (fieldName.startsWith('diagnostic.')) {
      controlPath = fieldName.replace('diagnostic.', '');
      control = this.diagnosticForm.get(controlPath);
    } else if (fieldName.startsWith('prescriptions.')) {
      controlPath = fieldName.replace('prescriptions.', '');
      control = this.prescriptionsForm.get(controlPath);
    }

    console.log('[ConsultationVoice] Form control lookup:', { controlPath, found: !!control });

    if (control) {
      const currentValue = control.value || '';
      const newValue = currentValue ? currentValue.trim() + ' ' + trimmedText : trimmedText;
      console.log('[ConsultationVoice] Setting value:', { currentValue, newValue });
      control.setValue(newValue);
      control.markAsDirty();
    } else {
      console.error('[ConsultationVoice] Control not found for:', fieldName);
    }
  }

  isVoiceActive(fieldName: string): boolean {
    return this.isRecording && this.activeVoiceField === fieldName;
  }

  private initForms(): void {
    this.anamneseForm = this.fb.group({
      motifConsultation: [''],
      histoireMaladie: [''],
      antecedentsPersonnels: [''],
      antecedentsFamiliaux: [''],
      allergiesConnues: [''],
      traitementsEnCours: [''],
      questionsReponses: this.fb.array([]),
      questionsLibres: this.fb.array([]),
      poids: [null],
      taille: [null],
      temperature: [null],
      tensionArterielle: [''],
      frequenceCardiaque: [null],
      frequenceRespiratoire: [null],
      saturationOxygene: [null],
      glycemie: [null]
    });

    this.diagnosticForm = this.fb.group({
      examenClinique: [''],
      diagnosticPrincipal: ['', Validators.required],
      diagnosticsSecondaires: [''],
      notesCliniques: ['']
    });

    this.prescriptionsForm = this.fb.group({
      ordonnanceNotes: [''],
      dureeTraitement: [''],
      medicaments: this.fb.array([]),
      examens: this.fb.array([]),
      recommandations: this.fb.array([])
    });
  }

  loadConsultation(): void {
    this.isLoading = true;
    this.consultationService.getConsultation(this.consultationId).subscribe({
      next: (data) => {
        this.consultation = data;
        this.loadQuestions();
        this.populateForms();
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur:', err);
        this.error = 'Impossible de charger la consultation';
        this.isLoading = false;
      }
    });
  }

  loadQuestions(): void {
    if (!this.consultation) return;
    
    const typeVisite = this.consultation.isPremiereConsultation ? 'premiere' : 'suivante';
    this.questionsPredefiniesService.getQuestionsParSpecialite(
      this.consultation.specialiteId, 
      typeVisite
    ).subscribe({
      next: (questions) => {
        this.questionsPredefinies = questions;
        this.initQuestionsFormArray();
      },
      error: (err) => console.error('Erreur chargement questions:', err)
    });
  }

  initQuestionsFormArray(): void {
    const questionsArray = this.anamneseForm.get('questionsReponses') as FormArray;
    questionsArray.clear();
    
    this.questionsPredefinies.forEach(q => {
      questionsArray.push(this.fb.group({
        questionId: [q.id],
        question: [q.texte],
        type: [q.type],
        options: [q.options || []],
        reponse: ['', q.obligatoire ? Validators.required : null]
      }));
    });
  }

  populateForms(): void {
    if (!this.consultation) return;

    // Anamnèse
    if (this.consultation.anamnese) {
      const a = this.consultation.anamnese;
      this.anamneseForm.patchValue({
        motifConsultation: a.motifConsultation,
        histoireMaladie: a.histoireMaladie,
        antecedentsPersonnels: a.antecedentsPersonnels,
        antecedentsFamiliaux: a.antecedentsFamiliaux,
        allergiesConnues: a.allergiesConnues,
        traitementsEnCours: a.traitementsEnCours
      });
      if (a.parametresVitaux) {
        this.anamneseForm.patchValue({
          poids: a.parametresVitaux.poids,
          taille: a.parametresVitaux.taille,
          temperature: a.parametresVitaux.temperature,
          tensionArterielle: a.parametresVitaux.tensionArterielle,
          frequenceCardiaque: a.parametresVitaux.frequenceCardiaque,
          frequenceRespiratoire: a.parametresVitaux.frequenceRespiratoire,
          saturationOxygene: a.parametresVitaux.saturationOxygene,
          glycemie: a.parametresVitaux.glycemie
        });
      }
    }

    // Diagnostic
    if (this.consultation.diagnostic) {
      this.diagnosticForm.patchValue(this.consultation.diagnostic);
    }

    // Prescriptions
    if (this.consultation.prescriptions) {
      const p = this.consultation.prescriptions;
      if (p.ordonnance) {
        this.prescriptionsForm.patchValue({
          ordonnanceNotes: p.ordonnance.notes,
          dureeTraitement: p.ordonnance.dureeTraitement
        });
        p.ordonnance.medicaments.forEach(m => this.addMedicament(m));
      }
      p.examens.forEach(e => this.addExamen(e));
      p.recommandations.forEach(r => this.addRecommandation(r));
    }
  }

  // Getters pour FormArrays
  get questionsArray(): FormArray { return this.anamneseForm.get('questionsReponses') as FormArray; }
  get questionsLibresArray(): FormArray { return this.anamneseForm.get('questionsLibres') as FormArray; }
  get medicamentsArray(): FormArray { return this.prescriptionsForm.get('medicaments') as FormArray; }
  get examensArray(): FormArray { return this.prescriptionsForm.get('examens') as FormArray; }
  get recommandationsArray(): FormArray { return this.prescriptionsForm.get('recommandations') as FormArray; }

  // Médicaments
  addMedicament(med?: MedicamentDto): void {
    this.medicamentsArray.push(this.fb.group({
      nomMedicament: [med?.nomMedicament || '', Validators.required],
      dosage: [med?.dosage || ''],
      frequence: [med?.frequence || ''],
      duree: [med?.duree || ''],
      instructions: [med?.instructions || ''],
      quantite: [med?.quantite || null]
    }));
  }

  removeMedicament(index: number): void {
    this.medicamentsArray.removeAt(index);
  }

  // Examens
  addExamen(exam?: ExamenPrescritDto): void {
    this.examensArray.push(this.fb.group({
      typeExamen: [exam?.typeExamen || '', Validators.required],
      nomExamen: [exam?.nomExamen || '', Validators.required],
      description: [exam?.description || ''],
      urgence: [exam?.urgence || false],
      notes: [exam?.notes || '']
    }));
  }

  removeExamen(index: number): void {
    this.examensArray.removeAt(index);
  }

  // Recommandations
  addRecommandation(rec?: RecommandationDto): void {
    this.recommandationsArray.push(this.fb.group({
      type: [rec?.type || 'conseil', Validators.required],
      specialiteOrientee: [rec?.specialiteOrientee || ''],
      motif: [rec?.motif || ''],
      description: [rec?.description || ''],
      urgence: [rec?.urgence || false]
    }));
  }

  removeRecommandation(index: number): void {
    this.recommandationsArray.removeAt(index);
  }

  // Hospitalisation
  toggleHospitalisationForm(): void {
    this.showHospitalisationForm = !this.showHospitalisationForm;
    if (this.showHospitalisationForm) {
      this.loadChambresEtLits();
    }
  }

  loadChambresEtLits(): void {
    this.isLoadingLits = true;
    this.chambresDisponibles = [];
    this.litsDisponibles = [];
    
    // Charger les chambres avec leurs lits
    this.hospitalisationService.getChambres().subscribe({
      next: (response) => {
        // Filtrer les chambres qui ont des lits disponibles
        this.chambresDisponibles = response.chambres.filter(c => c.litsDisponibles > 0);
        
        // Extraire tous les lits disponibles
        this.chambresDisponibles.forEach(chambre => {
          if (chambre.lits) {
            const litsLibres = chambre.lits.filter(l => l.estDisponible);
            this.litsDisponibles.push(...litsLibres);
          }
        });
        
        this.isLoadingLits = false;
        console.log('[Hospitalisation] Chambres:', this.chambresDisponibles.length, 'Lits:', this.litsDisponibles.length);
      },
      error: (err) => {
        console.error('Erreur chargement chambres:', err);
        // Fallback: charger les lits directement
        this.hospitalisationService.getLitsDisponibles().subscribe({
          next: (resp) => {
            this.litsDisponibles = resp.lits;
            this.isLoadingLits = false;
          },
          error: () => {
            this.isLoadingLits = false;
          }
        });
      }
    });
  }

  onChambreChange(): void {
    // Filtrer les lits de la chambre sélectionnée
    if (this.selectedChambreId) {
      const chambre = this.chambresDisponibles.find(c => c.idChambre === this.selectedChambreId);
      if (chambre && chambre.lits) {
        this.litsDisponibles = chambre.lits.filter(l => l.estDisponible);
      }
    } else {
      // Recharger tous les lits disponibles
      this.litsDisponibles = [];
      this.chambresDisponibles.forEach(chambre => {
        if (chambre.lits) {
          const litsLibres = chambre.lits.filter(l => l.estDisponible);
          this.litsDisponibles.push(...litsLibres);
        }
      });
    }
    this.selectedLitId = null;
  }

  async demanderHospitalisation(): Promise<void> {
    if (!this.hospitalisationMotif.trim()) {
      return;
    }

    this.isSaving = true;
    const request: DemandeHospitalisationRequest = {
      idConsultation: this.consultationId,
      idPatient: this.patientId,
      motif: this.hospitalisationMotif,
      urgence: this.hospitalisationUrgence,
      notes: this.hospitalisationNotes
    };

    this.hospitalisationService.demanderHospitalisation(request).subscribe({
      next: (response) => {
        if (response.success) {
          this.hospitalisationDemandee = true;
          this.showHospitalisationForm = false;
        } else {
          this.error = response.message;
        }
        this.isSaving = false;
      },
      error: (err) => {
        console.error('Erreur demande hospitalisation:', err);
        this.error = 'Erreur lors de la demande d\'hospitalisation';
        this.isSaving = false;
      }
    });
  }

  annulerHospitalisation(): void {
    this.showHospitalisationForm = false;
    this.hospitalisationMotif = '';
    this.hospitalisationUrgence = 'normale';
    this.hospitalisationNotes = '';
    this.selectedChambreId = null;
    this.selectedLitId = null;
  }

  // Questions libres
  toggleAddQuestion(): void {
    this.showAddQuestion = !this.showAddQuestion;
    if (!this.showAddQuestion) {
      this.newQuestionText = '';
    }
  }

  addQuestionLibre(): void {
    if (!this.newQuestionText.trim()) return;
    
    this.questionsLibresArray.push(this.fb.group({
      question: [this.newQuestionText.trim()],
      reponse: ['']
    }));
    
    this.newQuestionText = '';
    this.showAddQuestion = false;
  }

  removeQuestionLibre(index: number): void {
    this.questionsLibresArray.removeAt(index);
  }

  // Navigation
  getEtapeIndex(etape: EtapeConsultation): number {
    return this.etapes.indexOf(etape);
  }

  isEtapeComplete(etape: EtapeConsultation): boolean {
    return this.getEtapeIndex(etape) < this.getEtapeIndex(this.etapeActuelle);
  }

  canGoNext(): boolean {
    switch (this.etapeActuelle) {
      case 'anamnese': return this.anamneseForm.valid;
      case 'diagnostic': return this.diagnosticForm.valid;
      case 'prescriptions': return true;
      case 'validation': return true;
      default: return false;
    }
  }

  async goToEtape(etape: EtapeConsultation): Promise<void> {
    if (this.getEtapeIndex(etape) > this.getEtapeIndex(this.etapeActuelle)) {
      await this.saveCurrentEtape();
    }
    this.etapeActuelle = etape;
  }

  async nextEtape(): Promise<void> {
    await this.saveCurrentEtape();
    const currentIndex = this.getEtapeIndex(this.etapeActuelle);
    if (currentIndex < this.etapes.length - 1) {
      this.etapeActuelle = this.etapes[currentIndex + 1];
    }
  }

  previousEtape(): void {
    const currentIndex = this.getEtapeIndex(this.etapeActuelle);
    if (currentIndex > 0) {
      this.etapeActuelle = this.etapes[currentIndex - 1];
    }
  }

  async saveCurrentEtape(): Promise<void> {
    this.isSaving = true;
    try {
      switch (this.etapeActuelle) {
        case 'anamnese':
          await this.saveAnamnese();
          break;
        case 'diagnostic':
          await this.saveDiagnostic();
          break;
        case 'prescriptions':
          await this.savePrescriptions();
          break;
      }
    } catch (err) {
      console.error('Erreur sauvegarde:', err);
    }
    this.isSaving = false;
  }

  private async saveAnamnese(): Promise<void> {
    const form = this.anamneseForm.value;
    const anamnese: AnamneseDto = {
      motifConsultation: form.motifConsultation,
      histoireMaladie: form.histoireMaladie,
      antecedentsPersonnels: form.antecedentsPersonnels,
      antecedentsFamiliaux: form.antecedentsFamiliaux,
      allergiesConnues: form.allergiesConnues,
      traitementsEnCours: form.traitementsEnCours,
      questionsReponses: form.questionsReponses.map((q: any) => ({
        questionId: q.questionId,
        question: q.question,
        reponse: q.reponse
      })),
      parametresVitaux: {
        poids: form.poids,
        taille: form.taille,
        temperature: form.temperature,
        tensionArterielle: form.tensionArterielle,
        frequenceCardiaque: form.frequenceCardiaque,
        frequenceRespiratoire: form.frequenceRespiratoire,
        saturationOxygene: form.saturationOxygene,
        glycemie: form.glycemie
      }
    };
    await this.consultationService.saveAnamnese(this.consultationId, anamnese).toPromise();
  }

  private async saveDiagnostic(): Promise<void> {
    const diagnostic: DiagnosticDto = this.diagnosticForm.value;
    await this.consultationService.saveDiagnostic(this.consultationId, diagnostic).toPromise();
  }

  private async savePrescriptions(): Promise<void> {
    const form = this.prescriptionsForm.value;
    const prescriptions: PrescriptionsDto = {
      ordonnance: form.medicaments.length > 0 ? {
        notes: form.ordonnanceNotes,
        dureeTraitement: form.dureeTraitement,
        medicaments: form.medicaments
      } : undefined,
      examens: form.examens,
      recommandations: form.recommandations
    };
    await this.consultationService.savePrescriptions(this.consultationId, prescriptions).toPromise();
  }

  async validerConsultation(imprimer: boolean = false): Promise<void> {
    this.isSaving = true;
    try {
      await this.consultationService.validerConsultation(this.consultationId, {
        conclusion: this.diagnosticForm.value.notesCliniques,
        imprimer
      }).toPromise();
      
      if (imprimer) {
        this.imprimerRecapitulatif();
      }
      
      this.completed.emit();
    } catch (err) {
      console.error('Erreur validation:', err);
      this.error = 'Erreur lors de la validation';
    }
    this.isSaving = false;
  }

  /**
   * Sauvegarder la consultation comme brouillon (sans clôturer)
   */
  async sauvegarderBrouillon(): Promise<void> {
    this.isSaving = true;
    this.error = null;
    try {
      await this.saveAnamnese();
      await this.saveDiagnostic();
      await this.savePrescriptions();
      // Notification de succès (optionnel)
      console.log('[Consultation] Brouillon sauvegardé avec succès');
    } catch (err) {
      console.error('Erreur sauvegarde brouillon:', err);
      this.error = 'Erreur lors de la sauvegarde';
    }
    this.isSaving = false;
  }

  /**
   * Clôturer définitivement la consultation
   */
  async cloturerConsultation(imprimer: boolean = false): Promise<void> {
    this.isSaving = true;
    try {
      // Sauvegarder d'abord toutes les données
      await this.saveAnamnese();
      await this.saveDiagnostic();
      await this.savePrescriptions();
      
      // Puis clôturer
      await this.consultationService.validerConsultation(this.consultationId, {
        conclusion: this.diagnosticForm.value.notesCliniques,
        imprimer
      }).toPromise();
      
      if (imprimer) {
        this.imprimerRecapitulatif();
      }
      
      this.completed.emit();
    } catch (err) {
      console.error('Erreur clôture:', err);
      this.error = 'Erreur lors de la clôture de la consultation';
    }
    this.isSaving = false;
  }

  imprimerRecapitulatif(): void {
    this.consultationService.getRecapitulatif(this.consultationId).subscribe({
      next: (recap) => {
        const printWindow = window.open('', '_blank');
        if (printWindow) {
          printWindow.document.write(this.generatePrintContent(recap));
          printWindow.document.close();
          printWindow.print();
        }
      }
    });
  }

  private generatePrintContent(recap: any): string {
    return `
      <!DOCTYPE html>
      <html>
      <head>
        <title>Consultation - ${recap.patient.prenom} ${recap.patient.nom}</title>
        <style>
          body { font-family: Arial, sans-serif; padding: 20px; }
          h1, h2, h3 { color: #333; }
          .header { border-bottom: 2px solid #667eea; padding-bottom: 10px; margin-bottom: 20px; }
          .section { margin-bottom: 20px; }
          .section-title { background: #f0f0f0; padding: 8px; margin-bottom: 10px; }
          .medicament { padding: 5px 0; border-bottom: 1px solid #eee; }
        </style>
      </head>
      <body>
        <div class="header">
          <h1>Compte-rendu de consultation</h1>
          <p><strong>Patient:</strong> ${recap.patient.prenom} ${recap.patient.nom}</p>
          <p><strong>Date:</strong> ${new Date(recap.consultation.dateHeure).toLocaleDateString('fr-FR')}</p>
        </div>
        <div class="section">
          <h2 class="section-title">Motif</h2>
          <p>${recap.consultation.motif || '-'}</p>
        </div>
        <div class="section">
          <h2 class="section-title">Diagnostic</h2>
          <p>${recap.consultation.diagnostic?.diagnosticPrincipal || '-'}</p>
        </div>
        ${recap.consultation.prescriptions?.ordonnance ? `
        <div class="section">
          <h2 class="section-title">Ordonnance</h2>
          ${recap.consultation.prescriptions.ordonnance.medicaments.map((m: any) => `
            <div class="medicament">
              <strong>${m.nomMedicament}</strong> ${m.dosage || ''}<br>
              ${m.frequence || ''} ${m.duree ? '- ' + m.duree : ''}<br>
              <em>${m.instructions || ''}</em>
            </div>
          `).join('')}
        </div>
        ` : ''}
      </body>
      </html>
    `;
  }

  onCancel(): void {
    this.cancelled.emit();
  }
}
