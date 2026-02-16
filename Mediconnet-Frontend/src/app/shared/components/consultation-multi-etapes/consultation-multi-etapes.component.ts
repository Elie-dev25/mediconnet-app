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
  RecommandationDto,
  CreateRecommandationRequest,
  RecommandationResponseDto,
  CreneauDisponible,
  CreerRdvSuiviRequest,
  CreneauAvecStatut,
  LaboratoireDto,
  SpecialiteDto,
  MedecinSpecialisteDto,
  OrientationSpecialisteDto,
  CreateOrientationRequest,
  CreateOrientationManuelleRequest
} from '../../../services/consultation-complete.service';
import { QuestionsPredefiniesService, QuestionPredefinie } from '../../../services/questions-predefinies.service';
import { HospitalisationService, OrdonnerHospitalisationRequest } from '../../../services/hospitalisation.service';
import { HospitalisationMultiEtapesComponent, HospitalisationPatientInfo } from '../hospitalisation-multi-etapes/hospitalisation-multi-etapes.component';
import { PrescriptionExamensComponent, ExamenPrescription } from '../prescription-examens/prescription-examens.component';
import { SpeechRecognitionService, SupportedLanguage } from '../../../services/speech-recognition.service';
import { PharmacieStockService, MedicamentStock } from '../../../services/pharmacie-stock.service';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';

type EtapeConsultation = 'anamnese' | 'examen_clinique' | 'diagnostic' | 'plan_traitement' | 'conclusion' | 'suivi';

@Component({
  selector: 'app-consultation-multi-etapes',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LucideAngularModule, HospitalisationMultiEtapesComponent, PrescriptionExamensComponent],
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
  etapes: EtapeConsultation[] = ['anamnese', 'examen_clinique', 'diagnostic', 'plan_traitement', 'conclusion', 'suivi'];
  
  consultation: ConsultationEnCoursDto | null = null;
  questionsPredefinies: QuestionPredefinie[] = [];
  
  isLoading = true;
  isSaving = false;
  isPausing = false;
  isPaused = false;  // État de pause (consultation en pause mais toujours affichée)
  isResuming = false;  // En cours de reprise
  error: string | null = null;

  // Hospitalisation (nouveau workflow: médecin ne choisit pas de lit)
  showHospitalisationForm = false;
  hospitalisationMotif = '';
  hospitalisationUrgence = 'normale';
  hospitalisationDiagnostic = '';
  hospitalisationSoins = '';
  hospitalisationNotes = '';
  hospitalisationDateSortie: string | null = null;
  hospitalisationDemandee = false;

  // Planification RDV de suivi - Étape obligatoire
  suiviChoix: 'rdv' | 'cloture' | null = null; // Choix obligatoire
  showRdvSuiviForm = false;
  rdvSuiviDate: string = '';
  rdvSuiviMotif: string = '';
  rdvSuiviNotes: string = '';
  creneauxDisponibles: CreneauDisponible[] = [];
  creneauxAvecStatut: CreneauAvecStatut[] = [];
  selectedCreneau: CreneauDisponible | null = null;
  isLoadingCreneaux = false;
  rdvSuiviCree = false;
  rdvSuiviInfo: { dateHeure: string; idRendezVous: number } | null = null;
  minDate: string = '';
  
  // Clôture de dossier
  dossierCloture = false;
  clotureConfirmee = false;

  // Panneau latéral Hospitalisation (composant multi-étapes réutilisable)
  showHospitalisationPanel = false;
  hospitalisationPatientInfo: HospitalisationPatientInfo | null = null;

  // Recommandations structurées
  recommandationsSauvegardees: RecommandationResponseDto[] = [];
  showRecommandationForm = false;
  recommandationType: 'hopital' | 'medecin' = 'medecin';
  recommandationNomHopital = '';
  recommandationNomMedecin = '';
  recommandationIdMedecin: number | null = null;
  recommandationSpecialite = '';
  recommandationMotif = '';
  recommandationPrioritaire = false;
  recommandationIsSubmitting = false;
  recommandationError: string | null = null;
  specialitesListe: SpecialiteDto[] = [];
  medecinsParSpecialite: MedecinSpecialisteDto[] = [];
  selectedSpecialiteId: number | null = null;
  recommandationMedecinMode: 'interne' | 'externe' = 'interne';

  // Autocomplete médicaments
  medicamentSuggestions: MedicamentStock[] = [];
  activeMedicamentIndex: number | null = null;
  private searchMedicament$ = new Subject<{index: number, term: string}>();

  // Examens prescrits (utilisé par le composant PrescriptionExamensComponent)
  examensPrescriptions: ExamenPrescription[] = [];

  // Options prédéfinies pour les prescriptions (identiques au composant hospitalisation)
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

  // Options prédéfinies pour les examens (identiques au composant hospitalisation)
  typesExamen = [
    { value: 'biologie', label: 'Biologie / Analyses', icon: 'test-tube' },
    { value: 'imagerie', label: 'Imagerie médicale', icon: 'scan-line' },
    { value: 'cardiologie', label: 'Cardiologie', icon: 'heart-pulse' },
    { value: 'neurologie', label: 'Neurologie', icon: 'brain-circuit' },
    { value: 'autre', label: 'Autre', icon: 'file-plus' }
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
      'Sérologies',
      'Autre'
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
      'Doppler artériel',
      'Autre'
    ],
    cardiologie: [
      'ECG (Électrocardiogramme)',
      'Holter ECG 24h',
      'Holter tensionnel (MAPA)',
      'Échocardiographie',
      'Épreuve d\'effort',
      'Coronarographie',
      'Autre'
    ],
    neurologie: [
      'EEG (Électroencéphalogramme)',
      'EMG (Électromyogramme)',
      'Potentiels évoqués',
      'Ponction lombaire',
      'Autre'
    ],
    autre: []
  };

  // Laboratoires disponibles
  laboratoires: LaboratoireDto[] = [];

  // Orientation spécialiste
  specialites: SpecialiteDto[] = [];
  medecinsSpecialite: MedecinSpecialisteDto[] = [];
  orientations: OrientationSpecialisteDto[] = [];
  showOrientationForm = false;
  orientationForm!: FormGroup;
  orientationMode: 'liste' | 'manuel' = 'liste';

  // Questions libres
  showAddQuestion = false;
  newQuestionText = '';

  // Réponses patient déjà remplies (lecture seule)
  questionsDejaRepondues: { question: string; reponse: string }[] = [];

  // Formulaires
  anamneseForm!: FormGroup;
  examenCliniqueForm!: FormGroup;
  diagnosticForm!: FormGroup;
  planTraitementForm!: FormGroup;
  conclusionForm!: FormGroup;
  prescriptionsForm!: FormGroup; // Conservé pour compatibilité
  
  // Récapitulatif patient (pour étape diagnostic)
  recapitulatifPatient: any = null;

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
    this.loadLaboratoires();
    this.loadSpecialites();
    this.loadRecommandations();
    this.initOrientationForm();
    this.setupVoiceRecognition();
    this.setupMedicamentAutocomplete();
    this.initMinDate();
  }

  private loadLaboratoires(): void {
    this.consultationService.getLaboratoires().subscribe({
      next: (labs) => this.laboratoires = labs,
      error: (err) => console.error('Erreur chargement laboratoires:', err)
    });
  }

  private loadSpecialites(): void {
    this.consultationService.getSpecialites().subscribe({
      next: (specs) => { this.specialites = specs; this.specialitesListe = specs; },
      error: (err) => console.error('Erreur chargement spécialités:', err)
    });
  }

  private initOrientationForm(): void {
    this.orientationForm = this.fb.group({
      idSpecialite: [null],
      idMedecinOriente: [null],
      specialiteManuelle: [''],
      medecinManuel: [''],
      motif: ['', Validators.required],
      urgence: [false],
      notes: ['']
    });
  }

  onOrientationModeChange(): void {
    this.orientationForm.patchValue({
      idSpecialite: null,
      idMedecinOriente: null,
      specialiteManuelle: '',
      medecinManuel: ''
    });
    this.medecinsSpecialite = [];
  }

  isOrientationFormValid(): boolean {
    if (!this.orientationForm.get('motif')?.value) return false;
    
    if (this.orientationMode === 'liste') {
      return !!this.orientationForm.get('idSpecialite')?.value;
    } else {
      return !!this.orientationForm.get('specialiteManuelle')?.value?.trim();
    }
  }

  loadMedecinsSpecialite(idSpecialite: number): void {
    if (!idSpecialite) {
      this.medecinsSpecialite = [];
      return;
    }
    this.consultationService.getMedecinsParSpecialite(idSpecialite).subscribe({
      next: (medecins) => this.medecinsSpecialite = medecins,
      error: (err) => console.error('Erreur chargement médecins:', err)
    });
  }

  loadOrientations(): void {
    if (!this.consultationId) return;
    this.consultationService.getOrientations(this.consultationId).subscribe({
      next: (orientations) => this.orientations = orientations,
      error: (err) => console.error('Erreur chargement orientations:', err)
    });
  }

  toggleOrientationForm(): void {
    this.showOrientationForm = !this.showOrientationForm;
    if (!this.showOrientationForm) {
      this.orientationForm.reset();
      this.medecinsSpecialite = [];
      this.orientationMode = 'liste';
    }
  }

  onSpecialiteChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const idSpecialite = parseInt(select.value, 10);
    this.loadMedecinsSpecialite(idSpecialite);
  }

  addOrientation(): void {
    if (!this.isOrientationFormValid() || !this.consultationId) return;

    if (this.orientationMode === 'liste') {
      const request: CreateOrientationRequest = {
        idConsultation: this.consultationId,
        idSpecialite: this.orientationForm.value.idSpecialite,
        idMedecinOriente: this.orientationForm.value.idMedecinOriente || undefined,
        motif: this.orientationForm.value.motif,
        urgence: this.orientationForm.value.urgence || false,
        notes: this.orientationForm.value.notes || undefined
      };

      this.consultationService.createOrientation(request).subscribe({
        next: (orientation) => {
          this.orientations.unshift(orientation);
          this.toggleOrientationForm();
        },
        error: (err) => console.error('Erreur création orientation:', err)
      });
    } else {
      // Mode manuel - créer une orientation locale sans ID spécialité BD
      const specialiteManuelle = this.orientationForm.value.specialiteManuelle?.trim();
      const medecinManuel = this.orientationForm.value.medecinManuel?.trim();
      
      const orientationManuelle: OrientationSpecialisteDto = {
        idConsultation: this.consultationId,
        idSpecialite: 0,
        nomSpecialite: specialiteManuelle,
        nomMedecinOriente: medecinManuel || undefined,
        motif: this.orientationForm.value.motif,
        urgence: this.orientationForm.value.urgence || false,
        statut: 'en_attente',
        dateOrientation: new Date(),
        notes: this.orientationForm.value.notes || undefined
      };

      // Envoyer au backend avec spécialité manuelle
      this.consultationService.createOrientationManuelle({
        idConsultation: this.consultationId,
        specialiteManuelle: specialiteManuelle,
        medecinManuel: medecinManuel || undefined,
        motif: this.orientationForm.value.motif,
        urgence: this.orientationForm.value.urgence || false,
        notes: this.orientationForm.value.notes || undefined
      }).subscribe({
        next: (orientation) => {
          this.orientations.unshift(orientation);
          this.toggleOrientationForm();
        },
        error: (err) => {
          console.error('Erreur création orientation manuelle:', err);
          // Fallback: ajouter localement si erreur
          this.orientations.unshift(orientationManuelle);
          this.toggleOrientationForm();
        }
      });
    }
  }

  removeOrientation(idOrientation: number): void {
    if (!confirm('Supprimer cette orientation ?')) return;
    
    this.consultationService.deleteOrientation(idOrientation).subscribe({
      next: () => {
        this.orientations = this.orientations.filter(o => o.idOrientation !== idOrientation);
      },
      error: (err) => console.error('Erreur suppression orientation:', err)
    });
  }

  getStatutLabel(statut: string): string {
    const labels: { [key: string]: string } = {
      'en_attente': 'En attente',
      'acceptee': 'Acceptée',
      'refusee': 'Refusée',
      'terminee': 'Terminée'
    };
    return labels[statut] || statut;
  }

  private initMinDate(): void {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    this.minDate = tomorrow.toISOString().split('T')[0];
    this.rdvSuiviDate = this.minDate;
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
    // Étape 1: Anamnèse
    this.anamneseForm = this.fb.group({
      motifConsultation: [''],
      histoireMaladie: [''],
      traitementsEnCours: [''],
      questionsReponses: this.fb.array([]),
      questionsLibres: this.fb.array([])
    });

    // Étape 2: Examen Clinique
    this.examenCliniqueForm = this.fb.group({
      // Constantes vitales
      poids: [null],
      taille: [null],
      temperature: [null],
      tensionArterielle: [''],
      frequenceCardiaque: [null],
      frequenceRespiratoire: [null],
      saturationOxygene: [null],
      glycemie: [null],
      // Examen physique
      inspection: [''],
      palpation: [''],
      auscultation: [''],
      percussion: [''],
      autresObservations: ['']
    });

    // Étape 3: Diagnostic
    this.diagnosticForm = this.fb.group({
      diagnosticPrincipal: ['', Validators.required],
      diagnosticsSecondaires: [''],
      hypothesesDiagnostiques: [''],
      notesCliniques: ['']
    });

    // Étape 4: Plan de Traitement
    this.planTraitementForm = this.fb.group({
      explicationDiagnostic: [''],
      optionsTraitement: [''],
      ordonnanceNotes: [''],
      dureeTraitement: [''],
      medicaments: this.fb.array([]),
      examens: this.fb.array([]),
      orientationSpecialiste: [''],
      motifOrientation: ['']
    });

    // Étape 5: Conclusion
    this.conclusionForm = this.fb.group({
      resumeConsultation: [''],
      questionsPatient: [''],
      consignesPatient: [''],
      recommandations: ['']
    });

    // Conservé pour compatibilité
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
        
        // Restaurer l'étape actuelle si sauvegardée (reprise après pause)
        if (data.etapeActuelle && this.etapes.includes(data.etapeActuelle as EtapeConsultation)) {
          this.etapeActuelle = data.etapeActuelle as EtapeConsultation;
          console.log('[Consultation] Reprise à l\'étape:', this.etapeActuelle);
        }
        
        // Vérifier si la consultation est en pause
        if (data.statut === 'en_pause') {
          this.isPaused = true;
        }
        
        this.loadQuestions();
        this.populateForms();
        this.loadOrientations();
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

    // Étape 1: Anamnèse
    if (this.consultation.anamnese) {
      const a = this.consultation.anamnese;
      this.anamneseForm.patchValue({
        motifConsultation: a.motifConsultation,
        histoireMaladie: a.histoireMaladie,
        traitementsEnCours: a.traitementsEnCours
      });

      // Stocker les réponses déjà fournies par le patient (lecture seule)
      if (a.questionsReponses && a.questionsReponses.length > 0) {
        this.questionsDejaRepondues = a.questionsReponses
          .filter(qr => qr.reponse && qr.reponse.trim() !== '')
          .map(qr => ({
            question: qr.question,
            reponse: qr.reponse
          }));
      }
    }

    // Étape 2: Examen Clinique
    if (this.consultation.examenClinique) {
      const ec = this.consultation.examenClinique;
      if (ec.parametresVitaux) {
        this.examenCliniqueForm.patchValue({
          poids: ec.parametresVitaux.poids,
          taille: ec.parametresVitaux.taille,
          temperature: ec.parametresVitaux.temperature,
          tensionArterielle: ec.parametresVitaux.tensionArterielle,
          frequenceCardiaque: ec.parametresVitaux.frequenceCardiaque,
          frequenceRespiratoire: ec.parametresVitaux.frequenceRespiratoire,
          saturationOxygene: ec.parametresVitaux.saturationOxygene,
          glycemie: ec.parametresVitaux.glycemie
        });
      }
      this.examenCliniqueForm.patchValue({
        inspection: ec.inspection,
        palpation: ec.palpation,
        auscultation: ec.auscultation,
        percussion: ec.percussion,
        autresObservations: ec.autresObservations
      });
    }

    // Étape 3: Diagnostic
    if (this.consultation.diagnostic) {
      this.diagnosticForm.patchValue({
        diagnosticPrincipal: this.consultation.diagnostic.diagnosticPrincipal,
        diagnosticsSecondaires: this.consultation.diagnostic.diagnosticsSecondaires,
        hypothesesDiagnostiques: this.consultation.diagnostic.hypothesesDiagnostiques,
        notesCliniques: this.consultation.diagnostic.notesCliniques
      });
      // Stocker le récapitulatif patient
      this.recapitulatifPatient = this.consultation.diagnostic.recapitulatifPatient;
    }

    // Étape 4: Plan de Traitement
    if (this.consultation.planTraitement) {
      const pt = this.consultation.planTraitement;
      this.planTraitementForm.patchValue({
        explicationDiagnostic: pt.explicationDiagnostic,
        optionsTraitement: pt.optionsTraitement,
        orientationSpecialiste: pt.orientationSpecialiste,
        motifOrientation: pt.motifOrientation
      });
      if (pt.ordonnance) {
        this.planTraitementForm.patchValue({
          ordonnanceNotes: pt.ordonnance.notes,
          dureeTraitement: pt.ordonnance.dureeTraitement
        });
        pt.ordonnance.medicaments.forEach(m => this.addMedicament(m));
      }
      // Initialiser les examens pour le composant PrescriptionExamensComponent
      if (pt.examensPrescrits && pt.examensPrescrits.length > 0) {
        this.examensPrescriptions = pt.examensPrescrits.map(e => ({
          typeExamen: e.typeExamen || '',
          nomExamen: e.nomExamen || '',
          description: e.description,
          urgence: e.urgence || false,
          notes: e.notes,
          idLaboratoire: e.idLaboratoire
        } as ExamenPrescription));
      }
    }

    // Étape 5: Conclusion
    if (this.consultation.conclusion) {
      this.conclusionForm.patchValue(this.consultation.conclusion);
    }

    // Compatibilité: Prescriptions
    if (this.consultation.prescriptions) {
      const p = this.consultation.prescriptions;
      if (p.ordonnance) {
        this.prescriptionsForm.patchValue({
          ordonnanceNotes: p.ordonnance.notes,
          dureeTraitement: p.ordonnance.dureeTraitement
        });
      }
      p.recommandations?.forEach(r => this.addRecommandation(r));
    }
  }

  // Getters pour FormArrays
  get questionsArray(): FormArray { return this.anamneseForm.get('questionsReponses') as FormArray; }
  get questionsLibresArray(): FormArray { return this.anamneseForm.get('questionsLibres') as FormArray; }
  get medicamentsArray(): FormArray { return this.planTraitementForm.get('medicaments') as FormArray; }
  get examensArray(): FormArray { return this.planTraitementForm.get('examens') as FormArray; }
  get recommandationsArray(): FormArray { return this.prescriptionsForm.get('recommandations') as FormArray; }
  
  // Vérifier si les paramètres ont été pris par l'infirmier
  get parametresPrisParInfirmier(): boolean {
    return this.consultation?.examenClinique?.parametresPrisParInfirmier ?? false;
  }
  
  get infirmierNom(): string | null {
    return this.consultation?.examenClinique?.infirmierNom ?? null;
  }

  // Médicaments
  addMedicament(med?: MedicamentDto): void {
    this.medicamentsArray.push(this.fb.group({
      idMedicament: [med?.idMedicament || null],
      nomMedicament: [med?.nomMedicament || '', Validators.required],
      dosage: [med?.dosage || ''],
      posologie: [med?.posologie || ''],
      frequence: [med?.frequence || ''],
      duree: [med?.duree || ''],
      voieAdministration: [med?.voieAdministration || ''],
      formePharmaceutique: [med?.formePharmaceutique || ''],
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
      notes: [exam?.notes || ''],
      idLaboratoire: [exam?.idLaboratoire || null]
    }));
  }

  removeExamen(index: number): void {
    this.examensArray.removeAt(index);
  }

  getExamensSuggestions(index: number): string[] {
    const typeExamen = this.examensArray.at(index)?.get('typeExamen')?.value;
    return this.examensParType[typeExamen] || [];
  }

  onExamenTypeChange(index: number): void {
    const control = this.examensArray.at(index);
    if (control) {
      control.get('nomExamen')?.setValue('');
    }
  }

  getExamenTypeIcon(type: string): string {
    return this.typesExamen.find(t => t.value === type)?.icon || 'file-plus';
  }

  getLaboratoiresByType(type: string): LaboratoireDto[] {
    return this.laboratoires.filter(lab => lab.type === type);
  }

  // Gestion des examens via le composant PrescriptionExamensComponent
  onExamensChange(examens: ExamenPrescription[]): void {
    this.examensPrescriptions = examens;
  }

  // Recommandations structurées
  loadRecommandations(): void {
    this.consultationService.getRecommandations(this.consultationId).subscribe({
      next: (data) => this.recommandationsSauvegardees = data,
      error: () => console.error('Erreur chargement recommandations')
    });
  }

  onRecommandationSpecialiteChange(): void {
    this.medecinsParSpecialite = [];
    this.recommandationIdMedecin = null;
    if (this.selectedSpecialiteId) {
      this.consultationService.getMedecinsParSpecialite(this.selectedSpecialiteId).subscribe({
        next: (data) => this.medecinsParSpecialite = data,
        error: () => console.error('Erreur chargement médecins')
      });
      const spec = this.specialitesListe.find(s => s.idSpecialite === this.selectedSpecialiteId);
      if (spec) this.recommandationSpecialite = spec.nomSpecialite;
    }
  }

  toggleRecommandationForm(): void {
    this.showRecommandationForm = !this.showRecommandationForm;
    if (this.showRecommandationForm) {
      this.resetRecommandationForm();
    }
  }

  resetRecommandationForm(): void {
    this.recommandationType = 'medecin';
    this.recommandationNomHopital = '';
    this.recommandationNomMedecin = '';
    this.recommandationIdMedecin = null;
    this.recommandationSpecialite = '';
    this.recommandationMotif = '';
    this.recommandationPrioritaire = false;
    this.recommandationError = null;
    this.selectedSpecialiteId = null;
    this.medecinsParSpecialite = [];
    this.recommandationMedecinMode = 'interne';
  }

  get canSubmitRecommandation(): boolean {
    if (!this.recommandationMotif.trim()) return false;
    if (this.recommandationType === 'hopital' && !this.recommandationNomHopital.trim()) return false;
    if (this.recommandationType === 'medecin') {
      if (this.recommandationMedecinMode === 'interne' && !this.recommandationIdMedecin) return false;
      if (this.recommandationMedecinMode === 'externe' && !this.recommandationNomMedecin.trim()) return false;
    }
    return true;
  }

  submitRecommandation(): void {
    if (!this.canSubmitRecommandation || this.recommandationIsSubmitting) return;

    this.recommandationIsSubmitting = true;
    this.recommandationError = null;

    const request: CreateRecommandationRequest = {
      type: this.recommandationType,
      nomHopital: this.recommandationType === 'hopital' ? this.recommandationNomHopital : undefined,
      nomMedecinRecommande: this.recommandationType === 'medecin' && this.recommandationMedecinMode === 'externe'
        ? this.recommandationNomMedecin : undefined,
      idMedecinRecommande: this.recommandationType === 'medecin' && this.recommandationMedecinMode === 'interne'
        ? (this.recommandationIdMedecin ?? undefined) : undefined,
      specialite: this.recommandationSpecialite || undefined,
      motif: this.recommandationMotif,
      prioritaire: this.recommandationPrioritaire
    };

    this.consultationService.createRecommandation(this.consultationId, request).subscribe({
      next: (rec) => {
        this.recommandationsSauvegardees.unshift(rec);
        this.resetRecommandationForm();
        this.showRecommandationForm = false;
        this.recommandationIsSubmitting = false;
      },
      error: (err) => {
        this.recommandationError = err.error?.message || 'Erreur lors de la sauvegarde';
        this.recommandationIsSubmitting = false;
      }
    });
  }

  deleteRecommandation(id: number): void {
    this.consultationService.deleteRecommandation(id).subscribe({
      next: () => {
        this.recommandationsSauvegardees = this.recommandationsSauvegardees.filter(r => r.idRecommandation !== id);
      },
      error: () => console.error('Erreur suppression recommandation')
    });
  }

  // Legacy compatibility
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

  // Hospitalisation (nouveau workflow: médecin ne choisit pas de lit)
  toggleHospitalisationForm(): void {
    this.showHospitalisationForm = !this.showHospitalisationForm;
  }

  /**
   * Ordonner une hospitalisation (nouveau workflow)
   * Le médecin renseigne uniquement les détails médicaux
   * Le Major du service attribuera le lit ultérieurement
   */
  async ordonnerHospitalisation(): Promise<void> {
    if (!this.hospitalisationMotif.trim()) {
      this.error = 'Veuillez saisir un motif d\'hospitalisation';
      return;
    }

    this.isSaving = true;
    this.error = '';
    
    // Convertir la chaîne de soins en tableau de SoinComplementaireDto
    const soinsArray = this.hospitalisationSoins ? [{
      typeSoin: 'soins_infirmiers',
      description: this.hospitalisationSoins,
      priorite: 'normale'
    }] : undefined;

    const request: OrdonnerHospitalisationRequest = {
      idConsultation: this.consultationId,
      idPatient: this.patientId,
      motif: this.hospitalisationMotif,
      urgence: this.hospitalisationUrgence,
      diagnosticPrincipal: this.hospitalisationDiagnostic || undefined,
      soins: soinsArray,
      dateSortiePrevue: this.hospitalisationDateSortie || undefined
    };

    this.hospitalisationService.ordonnerHospitalisation(request).subscribe({
      next: (response) => {
        if (response.success) {
          this.hospitalisationDemandee = true;
          this.showHospitalisationForm = false;
          console.log('[Hospitalisation] Ordonnée avec succès. En attente d\'attribution de lit par le Major.');
        } else {
          this.error = response.message;
        }
        this.isSaving = false;
      },
      error: (err) => {
        console.error('Erreur ordonnance hospitalisation:', err);
        this.error = err.error?.message || 'Erreur lors de l\'ordonnance d\'hospitalisation';
        this.isSaving = false;
      }
    });
  }

  annulerHospitalisation(): void {
    this.showHospitalisationForm = false;
    this.hospitalisationMotif = '';
    this.hospitalisationUrgence = 'normale';
    this.hospitalisationDiagnostic = '';
    this.hospitalisationSoins = '';
    this.hospitalisationNotes = '';
    this.hospitalisationDateSortie = null;
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
      case 'anamnese': 
        // Si le patient a déjà répondu aux questions, le formulaire est valide
        if (this.questionsDejaRepondues.length > 0) {
          return true;
        }
        return this.anamneseForm.valid;
      case 'examen_clinique': return true; // Examen clinique optionnel
      case 'diagnostic': return this.diagnosticForm.valid;
      case 'plan_traitement': return true; // Plan de traitement optionnel
      case 'conclusion': return true;
      case 'suivi': return this.isSuiviComplete();
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
        case 'examen_clinique':
          await this.saveExamenClinique();
          break;
        case 'diagnostic':
          await this.saveDiagnostic();
          break;
        case 'plan_traitement':
          await this.savePlanTraitement();
          break;
        case 'conclusion':
          await this.saveConclusion();
          break;
      }
    } catch (err) {
      console.error('Erreur sauvegarde:', err);
    }
    this.isSaving = false;
  }

  private async saveAnamnese(): Promise<void> {
    const form = this.anamneseForm.value;
    
    // Combiner questions prédéfinies et questions libres
    const allQuestions = [
      ...form.questionsReponses.map((q: any) => ({
        questionId: q.questionId,
        question: q.question,
        reponse: q.reponse
      })),
      ...form.questionsLibres.map((q: any) => ({
        questionId: 'libre-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9),
        question: q.question,
        reponse: q.reponse
      }))
    ];
    
    const anamnese: AnamneseDto = {
      motifConsultation: form.motifConsultation,
      histoireMaladie: form.histoireMaladie,
      traitementsEnCours: form.traitementsEnCours,
      questionsReponses: allQuestions
    };
    await this.consultationService.saveAnamnese(this.consultationId, anamnese).toPromise();
  }

  private async saveExamenClinique(): Promise<void> {
    const form = this.examenCliniqueForm.value;
    const examenClinique = {
      parametresVitaux: {
        poids: form.poids,
        taille: form.taille,
        temperature: form.temperature,
        tensionArterielle: form.tensionArterielle,
        frequenceCardiaque: form.frequenceCardiaque,
        frequenceRespiratoire: form.frequenceRespiratoire,
        saturationOxygene: form.saturationOxygene,
        glycemie: form.glycemie
      },
      inspection: form.inspection,
      palpation: form.palpation,
      auscultation: form.auscultation,
      percussion: form.percussion,
      autresObservations: form.autresObservations
    };
    await this.consultationService.saveExamenClinique(this.consultationId, examenClinique as any).toPromise();
  }

  private async saveDiagnostic(): Promise<void> {
    const diagnostic: DiagnosticDto = this.diagnosticForm.value;
    await this.consultationService.saveDiagnostic(this.consultationId, diagnostic).toPromise();
  }

  private async savePlanTraitement(): Promise<void> {
    const form = this.planTraitementForm.value;
    const planTraitement = {
      explicationDiagnostic: form.explicationDiagnostic,
      optionsTraitement: form.optionsTraitement,
      ordonnance: form.medicaments.length > 0 ? {
        notes: form.ordonnanceNotes,
        dureeTraitement: form.dureeTraitement,
        medicaments: form.medicaments
      } : undefined,
      examensPrescrits: this.examensPrescriptions,
      orientationSpecialiste: form.orientationSpecialiste,
      motifOrientation: form.motifOrientation
    };
    await this.consultationService.savePlanTraitement(this.consultationId, planTraitement as any).toPromise();
  }

  private async saveConclusion(): Promise<void> {
    const conclusion = this.conclusionForm.value;
    await this.consultationService.saveConclusion(this.consultationId, conclusion).toPromise();
  }

  private async savePrescriptions(): Promise<void> {
    const form = this.prescriptionsForm.value;
    const prescriptions: PrescriptionsDto = {
      ordonnance: form.medicaments.length > 0 ? {
        notes: form.ordonnanceNotes,
        dureeTraitement: form.dureeTraitement,
        medicaments: form.medicaments
      } : undefined,
      examens: this.examensPrescriptions,
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
   * Mettre la consultation en pause (sauvegarde + changement de statut)
   * Ne redirige PAS - reste sur la page avec un état "en pause"
   */
  async pauserConsultation(): Promise<void> {
    this.isPausing = true;
    this.error = null;
    try {
      // Sauvegarder d'abord toutes les données
      await this.saveAnamnese();
      await this.saveDiagnostic();
      await this.savePrescriptions();
      
      // Mettre en pause via l'API avec l'étape actuelle
      await this.consultationService.pauseConsultation(this.consultationId, this.etapeActuelle).toPromise();
      
      console.log('[Consultation] Mise en pause réussie à l\'étape:', this.etapeActuelle);
      
      // Passer en état "en pause" sans redirection
      this.isPaused = true;
    } catch (err) {
      console.error('Erreur mise en pause:', err);
      this.error = 'Erreur lors de la mise en pause';
    }
    this.isPausing = false;
  }

  /**
   * Reprendre la consultation après une pause
   */
  async reprendreConsultation(): Promise<void> {
    this.isResuming = true;
    this.error = null;
    try {
      await this.consultationService.reprendreConsultation(this.consultationId).toPromise();
      
      console.log('[Consultation] Reprise réussie');
      
      // Sortir de l'état "en pause"
      this.isPaused = false;
    } catch (err) {
      console.error('Erreur reprise:', err);
      this.error = 'Erreur lors de la reprise de la consultation';
    }
    this.isResuming = false;
  }

  /**
   * Quitter la consultation (retour au dashboard)
   */
  quitterConsultation(): void {
    this.cancelled.emit();
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

  // ==================== ÉTAPE SUIVI - CHOIX OBLIGATOIRE ====================

  /**
   * Sélectionner le choix de suivi (RDV ou Clôture)
   */
  selectSuiviChoix(choix: 'rdv' | 'cloture'): void {
    this.suiviChoix = choix;
    this.error = null;
    
    if (choix === 'rdv') {
      this.showRdvSuiviForm = true;
      this.clotureConfirmee = false;
      if (this.rdvSuiviDate) {
        this.loadCreneauxAvecStatut();
      }
    } else {
      this.showRdvSuiviForm = false;
      this.selectedCreneau = null;
    }
  }

  /**
   * Vérifier si l'étape suivi est complète
   */
  isSuiviComplete(): boolean {
    // Si une hospitalisation a été demandée, le suivi est considéré comme complet
    if (this.hospitalisationDemandee) {
      return true;
    }
    if (this.suiviChoix === 'rdv') {
      return this.rdvSuiviCree;
    } else if (this.suiviChoix === 'cloture') {
      return this.clotureConfirmee;
    }
    return false;
  }

  /**
   * Charger les créneaux avec leur statut (disponible/occupé/passé)
   */
  loadCreneauxAvecStatut(): void {
    if (!this.rdvSuiviDate) return;
    
    this.isLoadingCreneaux = true;
    this.creneauxAvecStatut = [];
    this.selectedCreneau = null;
    
    this.consultationService.getCreneauxAvecStatut(this.rdvSuiviDate).subscribe({
      next: (response) => {
        this.creneauxAvecStatut = response.creneaux;
        this.isLoadingCreneaux = false;
      },
      error: (err) => {
        console.error('Erreur chargement créneaux:', err);
        this.creneauxAvecStatut = [];
        this.isLoadingCreneaux = false;
      }
    });
  }

  onDateChange(): void {
    this.selectedCreneau = null;
    this.creneauxAvecStatut = [];
    if (this.rdvSuiviDate) {
      this.loadCreneauxAvecStatut();
    }
  }

  /**
   * Sélectionner un créneau disponible
   */
  selectCreneauAvecStatut(creneau: CreneauAvecStatut): void {
    if (creneau.statut !== 'disponible') return;
    
    this.selectedCreneau = {
      heureDebut: creneau.heureDebut,
      heureFin: creneau.heureFin,
      dateHeure: creneau.dateHeure,
      duree: creneau.duree
    };
  }

  /**
   * Créer le RDV de suivi
   */
  async creerRdvSuivi(): Promise<void> {
    if (!this.selectedCreneau) {
      this.error = 'Veuillez sélectionner un créneau disponible';
      return;
    }

    this.isSaving = true;
    this.error = null;

    const request: CreerRdvSuiviRequest = {
      dateHeure: this.selectedCreneau.dateHeure,
      duree: this.selectedCreneau.duree,
      motif: this.rdvSuiviMotif || 'Consultation de suivi',
      notes: this.rdvSuiviNotes
    };

    this.consultationService.creerRdvSuivi(this.consultationId, request).subscribe({
      next: (response) => {
        if (response.success) {
          this.rdvSuiviCree = true;
          this.rdvSuiviInfo = {
            dateHeure: response.dateHeure || this.selectedCreneau!.dateHeure,
            idRendezVous: response.idRendezVous || 0
          };
        } else {
          this.error = response.message || 'Erreur lors de la création du RDV';
        }
        this.isSaving = false;
      },
      error: (err) => {
        console.error('Erreur création RDV suivi:', err);
        this.error = err.error?.message || 'Erreur lors de la création du rendez-vous';
        this.isSaving = false;
      }
    });
  }

  /**
   * Confirmer la clôture du dossier
   */
  async confirmerClotureDossier(): Promise<void> {
    this.isSaving = true;
    this.error = null;

    this.consultationService.cloturerDossier(this.consultationId).subscribe({
      next: (response) => {
        if (response.success) {
          this.clotureConfirmee = true;
          this.dossierCloture = true;
        } else {
          this.error = response.message || 'Erreur lors de la clôture du dossier';
        }
        this.isSaving = false;
      },
      error: (err) => {
        console.error('Erreur clôture dossier:', err);
        this.error = err.error?.message || 'Erreur lors de la clôture du dossier';
        this.isSaving = false;
      }
    });
  }

  formatCreneauDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    });
  }

  formatCreneauTime(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleTimeString('fr-FR', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  /**
   * Obtenir la classe CSS pour un créneau selon son statut
   */
  getCreneauClass(creneau: CreneauAvecStatut): string {
    switch (creneau.statut) {
      case 'disponible': return 'creneau-disponible';
      case 'occupe': return 'creneau-occupe';
      case 'passe': return 'creneau-passe';
      default: return '';
    }
  }

  /**
   * Vérifier si un créneau est sélectionné
   */
  isCreneauSelected(creneau: CreneauAvecStatut): boolean {
    return this.selectedCreneau?.dateHeure === creneau.dateHeure;
  }

  // ==================== PANNEAU HOSPITALISATION ====================

  /**
   * Ouvrir le panneau d'hospitalisation multi-étapes
   */
  openHospitalisationPanel(): void {
    // Extraire nom et prénom du patientNom (format "Prénom Nom")
    const parts = this.patientNom.split(' ');
    const prenom = parts[0] || '';
    const nom = parts.slice(1).join(' ') || '';

    this.hospitalisationPatientInfo = {
      idPatient: this.patientId,
      nom: nom,
      prenom: prenom
    };
    this.showHospitalisationPanel = true;
  }

  /**
   * Fermer le panneau d'hospitalisation
   */
  closeHospitalisationPanel(): void {
    this.showHospitalisationPanel = false;
    this.hospitalisationPatientInfo = null;
  }

  /**
   * Callback quand l'hospitalisation est complétée
   */
  onHospitalisationCompleted(): void {
    this.closeHospitalisationPanel();
    this.hospitalisationDemandee = true;
  }

  /**
   * Callback quand l'hospitalisation est annulée
   */
  onHospitalisationCancelled(): void {
    this.closeHospitalisationPanel();
  }
}
