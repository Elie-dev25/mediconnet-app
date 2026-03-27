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
  CreneauDisponible,
  CreerRdvSuiviRequest,
  CreneauAvecStatut,
  LaboratoireDto,
  SpecialiteDto,
  MedecinSpecialisteDto,
  OrientationPreConsultationDto,
  CreateOrientationRequest,
  UpdateOrientationStatutRequest,
  ExamenGynecologiqueDto,
  TYPES_ORIENTATION,
  STATUTS_ORIENTATION,
  TypeOrientation,
  CreneauMedecinDto,
  CreerRdvOrientationRequest
} from '../../../services/consultation-complete.service';
import { QuestionsPredefiniesService, QuestionPredefinie } from '../../../services/questions-predefinies.service';
import { HospitalisationService, OrdonnerHospitalisationRequest } from '../../../services/hospitalisation.service';
import { HospitalisationMultiEtapesComponent, HospitalisationPatientInfo } from '../hospitalisation-multi-etapes/hospitalisation-multi-etapes.component';
import { PrescriptionExamensComponent, ExamenPrescription } from '../prescription-examens/prescription-examens.component';
import { CreneauxSelectorComponent, CreneauUnifie } from '../creneaux-selector/creneaux-selector.component';
import { SpeechRecognitionService, SupportedLanguage } from '../../../services/speech-recognition.service';
import { PharmacieStockService, MedicamentStock, FormePharmaceutique, VoieAdministration } from '../../../services/pharmacie-stock.service';
import { AuthService } from '../../../services/auth.service';
import { ProgrammationInterventionPanelComponent } from '../programmation-intervention-panel/programmation-intervention-panel.component';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';

const GYNECO_SPECIALITE_ID = 23;
const CHIRURGIE_SPECIALITE_IDS = [5, 6, 12, 21, 26, 31, 39, 41]; // IDs des spécialités chirurgicales

type EtapeConsultation = 'anamnese' | 'examen_clinique' | 'examen_gynecologique' | 'examen_chirurgical' | 'diagnostic' | 'plan_traitement' | 'conclusion' | 'suivi';

@Component({
  selector: 'app-consultation-multi-etapes',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LucideAngularModule, HospitalisationMultiEtapesComponent, PrescriptionExamensComponent, CreneauxSelectorComponent, ProgrammationInterventionPanelComponent],
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

  // Programmation d'intervention chirurgicale
  showProgrammationIntervention = false;
  programmationCreee = false;

  // Panneau latéral Hospitalisation (composant multi-étapes réutilisable)
  showHospitalisationPanel = false;
  hospitalisationPatientInfo: HospitalisationPatientInfo | null = null;

  // Orientations unifiées (remplace recommandations et orientations séparées)
  orientationsSauvegardees: OrientationPreConsultationDto[] = [];
  showOrientationForm = false;
  
  // Examens collapse state
  examensCollapsed = true;
  orientationType: TypeOrientation = TYPES_ORIENTATION.MEDECIN_INTERNE;
  orientationNomDestinataire = '';
  orientationIdMedecin: number | null = null;
  orientationSpecialiteTexte = '';
  orientationMotif = '';
  orientationPrioritaire = false;
  orientationUrgence = false;
  orientationNotes = '';
  orientationAdresse = '';
  orientationTelephone = '';
  orientationIsSubmitting = false;
  orientationError: string | null = null;
  specialitesListe: SpecialiteDto[] = [];
  medecinsParSpecialite: MedecinSpecialisteDto[] = [];
  selectedSpecialiteId: number | null = null;

  // Créneaux pour orientation médecin interne
  showOrientationCreneaux = false;
  orientationCreneaux: CreneauMedecinDto[] = [];
  orientationDateRdv: string = '';
  orientationCreneauSelectionne: CreneauMedecinDto | null = null;
  isLoadingOrientationCreneaux = false;
  orientationMedecinDisponible = true;
  orientationMessageIndispo = '';
  orientationRdvCree = false;
  orientationRdvInfo: { idRendezVous: number; dateHeure: string } | null = null;

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

  // Formes et voies par défaut (fallback)
  formesPharmaceutiquesDefaut = [
    'Comprimé', 'Gélule', 'Sirop', 'Solution buvable', 'Ampoule injectable',
    'Pommade', 'Crème', 'Gel', 'Suppositoire', 'Collyre', 'Spray nasal',
    'Inhalateur', 'Patch', 'Sachet', 'Autre'
  ];

  // Formes et voies dynamiques par médicament (indexé par position dans le FormArray)
  formesParMedicament: Map<number, FormePharmaceutique[]> = new Map();
  voiesParMedicament: Map<number, VoieAdministration[]> = new Map();
  loadingFormesVoies: Map<number, boolean> = new Map();

  voiesAdministrationDefaut = [
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

  // Orientation spécialiste (Plan de traitement - supprimé, centralisé dans Conclusion)
  specialites: SpecialiteDto[] = [];
  medecinsSpecialite: MedecinSpecialisteDto[] = [];
  orientations: OrientationPreConsultationDto[] = [];

  // Questions libres
  showAddQuestion = false;
  newQuestionText = '';

  // Réponses patient (pour affichage indicateur et préremplissage)
  reponsesPatientMap: Map<string, string> = new Map();
  hasReponsesPatient = false;

  // Formulaires
  anamneseForm!: FormGroup;
  examenCliniqueForm!: FormGroup;
  examenGynecologiqueForm!: FormGroup;
  examenChirurgicalForm!: FormGroup;
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

  // Titre affiché de l'utilisateur (pour filtrer les examens par spécialité)
  userTitreAffiche: string = '';

  constructor(
    private fb: FormBuilder,
    private consultationService: ConsultationCompleteService,
    private questionsPredefiniesService: QuestionsPredefiniesService,
    private hospitalisationService: HospitalisationService,
    private speechService: SpeechRecognitionService,
    private pharmacieService: PharmacieStockService,
    private authService: AuthService
  ) {
    this.initForms();
    this.isVoiceSupported = this.speechService.isSupported;
  }

  ngOnInit(): void {
    this.loadUserTitreAffiche();
    this.loadConsultation();
    this.loadLaboratoires();
    this.loadSpecialites();
    this.loadOrientations();
    this.setupVoiceRecognition();
    this.setupMedicamentAutocomplete();
    this.initMinDate();
  }

  private loadUserTitreAffiche(): void {
    const user = this.authService.getCurrentUser();
    this.userTitreAffiche = user?.titreAffiche || '';
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

  // ==================== ORIENTATIONS UNIFIÉES (Conclusion) ====================

  loadOrientations(): void {
    if (!this.consultationId) return;
    this.consultationService.getOrientations(this.consultationId).subscribe({
      next: (orientations: OrientationPreConsultationDto[]) => {
        this.orientations = orientations;
        this.orientationsSauvegardees = orientations;
      },
      error: (err) => console.error('Erreur chargement orientations:', err)
    });
  }

  loadMedecinsSpecialite(idSpecialite: number): void {
    if (!idSpecialite) {
      this.medecinsSpecialite = [];
      this.medecinsParSpecialite = [];
      return;
    }
    this.consultationService.getMedecinsParSpecialite(idSpecialite).subscribe({
      next: (medecins) => {
        this.medecinsSpecialite = medecins;
        this.medecinsParSpecialite = medecins;
      },
      error: (err) => console.error('Erreur chargement médecins:', err)
    });
  }

  onSpecialiteChange(): void {
    const idSpecialite = this.selectedSpecialiteId;
    if (idSpecialite) {
      this.loadMedecinsSpecialite(idSpecialite);
      // Mettre à jour le texte de spécialité
      const spec = this.specialitesListe.find(s => s.idSpecialite === idSpecialite);
      if (spec) this.orientationSpecialiteTexte = spec.nomSpecialite;
    } else {
      this.medecinsParSpecialite = [];
      this.orientationSpecialiteTexte = '';
    }
  }

  toggleOrientationForm(): void {
    this.showOrientationForm = !this.showOrientationForm;
    if (this.showOrientationForm) {
      this.resetOrientationForm();
    }
  }

  resetOrientationForm(): void {
    this.orientationType = TYPES_ORIENTATION.MEDECIN_INTERNE;
    this.orientationNomDestinataire = '';
    this.orientationIdMedecin = null;
    this.orientationSpecialiteTexte = '';
    this.orientationMotif = '';
    this.orientationPrioritaire = false;
    this.orientationUrgence = false;
    this.orientationNotes = '';
    this.orientationAdresse = '';
    this.orientationTelephone = '';
    this.orientationError = null;
    this.selectedSpecialiteId = null;
    this.medecinsParSpecialite = [];
    // Reset créneaux orientation
    this.showOrientationCreneaux = false;
    this.orientationCreneaux = [];
    this.orientationDateRdv = '';
    this.orientationCreneauSelectionne = null;
    this.orientationRdvCree = false;
    this.orientationRdvInfo = null;
  }

  // Méthode appelée quand un médecin est sélectionné pour l'orientation
  onOrientationMedecinChange(): void {
    this.showOrientationCreneaux = !!this.orientationIdMedecin;
    this.orientationCreneaux = [];
    this.orientationCreneauSelectionne = null;
    this.orientationDateRdv = this.minDate;
    if (this.orientationIdMedecin && this.orientationDateRdv) {
      this.loadOrientationCreneaux();
    }
  }

  // Charger les créneaux disponibles du médecin sélectionné
  loadOrientationCreneaux(): void {
    if (!this.orientationIdMedecin || !this.orientationDateRdv) return;

    this.isLoadingOrientationCreneaux = true;
    this.orientationCreneaux = [];
    this.orientationCreneauSelectionne = null;

    this.consultationService.getCreneauxMedecinDisponibles(this.orientationIdMedecin, this.orientationDateRdv).subscribe({
      next: (response) => {
        this.orientationCreneaux = response.creneaux;
        this.orientationMedecinDisponible = response.medecinDisponible;
        this.orientationMessageIndispo = response.messageIndisponibilite || '';
        this.isLoadingOrientationCreneaux = false;
      },
      error: (err) => {
        console.error('Erreur chargement créneaux orientation:', err);
        this.orientationCreneaux = [];
        this.isLoadingOrientationCreneaux = false;
      }
    });
  }

  // Changement de date pour les créneaux d'orientation
  onOrientationDateChange(): void {
    this.orientationCreneauSelectionne = null;
    if (this.orientationIdMedecin && this.orientationDateRdv) {
      this.loadOrientationCreneaux();
    }
  }

  // Sélectionner un créneau pour l'orientation
  selectOrientationCreneau(creneau: CreneauMedecinDto): void {
    if (!creneau.selectionnable) return;
    this.orientationCreneauSelectionne = creneau;
  }

  // Vérifier si un créneau est sélectionné
  isOrientationCreneauSelected(creneau: CreneauMedecinDto): boolean {
    return this.orientationCreneauSelectionne?.dateHeure === creneau.dateHeure;
  }

  // Obtenir la classe CSS pour un créneau d'orientation
  getOrientationCreneauClass(creneau: CreneauMedecinDto): string {
    switch (creneau.statut) {
      case 'disponible': return 'creneau-disponible';
      case 'occupe': return 'creneau-occupe';
      case 'passe': return 'creneau-passe';
      case 'indisponible': return 'creneau-indisponible';
      default: return '';
    }
  }

  get canSubmitOrientation(): boolean {
    if (!this.orientationMotif.trim()) return false;
    
    switch (this.orientationType) {
      case TYPES_ORIENTATION.MEDECIN_INTERNE:
        return !!this.selectedSpecialiteId;
      case TYPES_ORIENTATION.MEDECIN_EXTERNE:
        return !!this.orientationNomDestinataire.trim() && !!this.orientationSpecialiteTexte.trim();
      case TYPES_ORIENTATION.HOPITAL:
        return !!this.orientationNomDestinataire.trim();
      case TYPES_ORIENTATION.SERVICE_INTERNE:
      case TYPES_ORIENTATION.LABORATOIRE:
        return !!this.orientationSpecialiteTexte.trim();
      default:
        return false;
    }
  }

  submitOrientation(): void {
    if (!this.canSubmitOrientation || this.orientationIsSubmitting || !this.consultationId) return;

    this.orientationIsSubmitting = true;
    this.orientationError = null;

    const request: CreateOrientationRequest = {
      typeOrientation: this.orientationType,
      idSpecialite: this.orientationType === TYPES_ORIENTATION.MEDECIN_INTERNE ? this.selectedSpecialiteId ?? undefined : undefined,
      idMedecinOriente: this.orientationType === TYPES_ORIENTATION.MEDECIN_INTERNE ? this.orientationIdMedecin ?? undefined : undefined,
      nomDestinataire: (this.orientationType === TYPES_ORIENTATION.MEDECIN_EXTERNE || this.orientationType === TYPES_ORIENTATION.HOPITAL) 
        ? this.orientationNomDestinataire : undefined,
      specialiteTexte: this.orientationSpecialiteTexte || undefined,
      adresseDestinataire: this.orientationAdresse || undefined,
      telephoneDestinataire: this.orientationTelephone || undefined,
      motif: this.orientationMotif,
      notes: this.orientationNotes || undefined,
      urgence: this.orientationUrgence,
      prioritaire: this.orientationPrioritaire
    };

    this.consultationService.createOrientation(this.consultationId, request).subscribe({
      next: (orientation: OrientationPreConsultationDto) => {
        // Si un créneau est sélectionné pour un médecin interne, créer le RDV
        if (this.orientationType === TYPES_ORIENTATION.MEDECIN_INTERNE && 
            this.orientationIdMedecin && 
            this.orientationCreneauSelectionne) {
          this.creerRdvPourOrientation(orientation);
        } else {
          this.finaliserOrientation(orientation);
        }
      },
      error: (err: any) => {
        this.orientationError = err.error?.message || 'Erreur lors de la sauvegarde';
        this.orientationIsSubmitting = false;
      }
    });
  }

  // Créer un RDV lié à l'orientation
  private creerRdvPourOrientation(orientation: OrientationPreConsultationDto): void {
    if (!this.orientationCreneauSelectionne) {
      this.finaliserOrientation(orientation);
      return;
    }

    const rdvRequest: CreerRdvOrientationRequest = {
      dateHeure: this.orientationCreneauSelectionne.dateHeure,
      duree: this.orientationCreneauSelectionne.duree,
      motif: this.orientationMotif,
      notes: this.orientationNotes
    };

    this.consultationService.creerRdvOrientation(orientation.idOrientation, rdvRequest).subscribe({
      next: (response) => {
        if (response.success && response.idRendezVous) {
          // Mettre à jour l'orientation avec les infos du RDV
          orientation.idRdvCree = response.idRendezVous;
          // dateRdvPropose est de type Date dans le DTO
          if (response.dateHeure) {
            (orientation as any).dateRdvPropose = new Date(response.dateHeure);
          }
          orientation.statut = 'rdv_pris';
          this.orientationRdvCree = true;
          this.orientationRdvInfo = {
            idRendezVous: response.idRendezVous,
            dateHeure: response.dateHeure ?? this.orientationCreneauSelectionne?.dateHeure ?? ''
          };
        }
        this.finaliserOrientation(orientation);
      },
      error: (err) => {
        console.error('Erreur création RDV orientation:', err);
        // L'orientation est créée mais pas le RDV - on continue quand même
        this.finaliserOrientation(orientation);
      }
    });
  }

  // Finaliser l'ajout de l'orientation à la liste
  private finaliserOrientation(orientation: OrientationPreConsultationDto): void {
    this.orientationsSauvegardees = [orientation, ...this.orientationsSauvegardees];
    this.orientations = this.orientationsSauvegardees;
    this.resetOrientationForm();
    this.showOrientationForm = false;
    this.orientationIsSubmitting = false;
  }

  deleteOrientation(id: number): void {
    if (!confirm('Supprimer cette orientation ?')) return;
    
    this.consultationService.deleteOrientation(id).subscribe({
      next: () => {
        this.orientationsSauvegardees = this.orientationsSauvegardees.filter(o => o.idOrientation !== id);
        this.orientations = this.orientations.filter(o => o.idOrientation !== id);
      },
      error: () => console.error('Erreur suppression orientation')
    });
  }

  getStatutLabel(statut: string): string {
    const labels: { [key: string]: string } = {
      'en_attente': 'En attente',
      'acceptee': 'Acceptée',
      'refusee': 'Refusée',
      'rdv_pris': 'RDV pris',
      'terminee': 'Terminée',
      'annulee': 'Annulée'
    };
    return labels[statut] || statut;
  }

  getTypeOrientationLabel(type: string): string {
    const labels: { [key: string]: string } = {
      'medecin_interne': 'Médecin interne',
      'medecin_externe': 'Médecin externe',
      'hopital': 'Hôpital',
      'service_interne': 'Service interne',
      'laboratoire': 'Laboratoire'
    };
    return labels[type] || type;
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
        dosage: medicament.dosage || '',
        idMedicament: medicament.idMedicament
      });
      // Charger les formes et voies spécifiques au médicament
      this.loadFormesVoiesMedicament(index, medicament.idMedicament);
    }
    this.medicamentSuggestions = [];
    this.activeMedicamentIndex = null;
  }

  /**
   * Charge les formes pharmaceutiques et voies d'administration pour un médicament
   */
  loadFormesVoiesMedicament(index: number, idMedicament: number): void {
    this.loadingFormesVoies.set(index, true);
    
    this.pharmacieService.getFormesVoiesMedicament(idMedicament).subscribe({
      next: (data) => {
        this.formesParMedicament.set(index, data.formes);
        this.voiesParMedicament.set(index, data.voies);
        this.loadingFormesVoies.set(index, false);

        // Si une forme/voie par défaut existe, la pré-sélectionner
        const control = this.medicamentsArray.at(index);
        if (control) {
          const formeDefaut = data.formes.find(f => f.estDefaut);
          const voieDefaut = data.voies.find(v => v.estDefaut);
          
          if (formeDefaut && !control.get('formePharmaceutique')?.value) {
            control.patchValue({ formePharmaceutique: formeDefaut.libelle });
          }
          if (voieDefaut && !control.get('voieAdministration')?.value) {
            control.patchValue({ voieAdministration: voieDefaut.libelle });
          }
        }
      },
      error: (err) => {
        console.error('Erreur chargement formes/voies:', err);
        this.loadingFormesVoies.set(index, false);
        // En cas d'erreur, utiliser les valeurs par défaut
        this.formesParMedicament.delete(index);
        this.voiesParMedicament.delete(index);
      }
    });
  }

  /**
   * Retourne les formes pharmaceutiques pour un médicament à l'index donné
   */
  getFormesForMedicament(index: number): string[] {
    const formes = this.formesParMedicament.get(index);
    if (formes && formes.length > 0) {
      return formes.map(f => f.libelle);
    }
    return this.formesPharmaceutiquesDefaut;
  }

  /**
   * Retourne les voies d'administration pour un médicament à l'index donné
   */
  getVoiesForMedicament(index: number): string[] {
    const voies = this.voiesParMedicament.get(index);
    if (voies && voies.length > 0) {
      return voies.map(v => v.libelle);
    }
    return this.voiesAdministrationDefaut;
  }

  /**
   * Vérifie si les formes/voies sont en cours de chargement pour un médicament
   */
  isLoadingFormesVoies(index: number): boolean {
    return this.loadingFormesVoies.get(index) || false;
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
    } else if (fieldName.startsWith('examenGynecologique.')) {
      controlPath = fieldName.replace('examenGynecologique.', '');
      control = this.examenGynecologiqueForm.get(controlPath);
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
      const newValue = currentValue ? `${currentValue} ${trimmedText}`.trim() : trimmedText;
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

    this.examenGynecologiqueForm = this.fb.group({
      inspectionExterne: [''],
      examenSpeculum: [''],
      toucherVaginal: [''],
      autresObservations: ['']
    });

    this.examenChirurgicalForm = this.fb.group({
      zoneExaminee: [''],
      inspectionLocale: [''],
      palpationLocale: [''],
      signesInflammatoires: [''],
      cicatricesExistantes: [''],
      mobiliteFonction: [''],
      conclusionChirurgicale: [''],
      notesComplementaires: ['']
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
      motifOrientation: [''],
      // Décision chirurgicale (uniquement pour les chirurgiens)
      decisionChirurgicale: ['surveillance']
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

        this.updateEtapes();
        // D'abord peupler les formulaires pour stocker les réponses patient dans reponsesPatientMap
        this.populateForms();
        // Ensuite charger les questions prédéfinies (qui utilisera reponsesPatientMap pour préremplir)
        this.loadQuestions();
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
    
    let reponsesPreRemplies = 0;
    this.questionsPredefinies.forEach(q => {
      // Chercher si le patient/médecin a déjà répondu à cette question
      // Chercher par texte de question ou par ID
      const reponsePatient = this.reponsesPatientMap.get(q.texte) || 
                             this.reponsesPatientMap.get(q.id.toString()) || '';
      
      if (reponsePatient) {
        reponsesPreRemplies++;
      }
      
      questionsArray.push(this.fb.group({
        questionId: [q.id],
        question: [q.texte],
        type: [q.type],
        options: [q.options || []],
        reponse: [reponsePatient, q.obligatoire ? Validators.required : null],
        reponsePatientOriginale: [reponsePatient] // Pour traçabilité
      }));
    });
    
    console.log('[Anamnèse] FormArray initialisé:', this.questionsPredefinies.length, 'questions,', reponsesPreRemplies, 'préremplies');
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

      // Stocker les réponses du patient pour préremplissage du formulaire
      // Note: initQuestionsFormArray() sera appelé par loadQuestions() après le chargement des questions prédéfinies
      if (a.questionsReponses && a.questionsReponses.length > 0) {
        this.reponsesPatientMap.clear();
        a.questionsReponses
          .filter(qr => qr.reponse && qr.reponse.trim() !== '')
          .forEach(qr => {
            this.reponsesPatientMap.set(qr.question, qr.reponse);
            if (qr.questionId) {
              this.reponsesPatientMap.set(qr.questionId, qr.reponse);
            }
          });
        this.hasReponsesPatient = this.reponsesPatientMap.size > 0;
        console.log('[Anamnèse] Réponses patient chargées:', this.reponsesPatientMap.size, 'réponses');
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

    // Étape 2bis: Examen Gynécologique (si gynécologue)
    if (this.isGynecoConsultation() && this.consultation.examenGynecologique) {
      this.patchGynecologiqueForm(this.consultation.examenGynecologique);
    }

    // Étape 2ter: Examen Chirurgical (si chirurgien)
    if (this.isChirurgieConsultation() && this.consultation.examenChirurgical) {
      this.patchChirurgicalForm(this.consultation.examenChirurgical);
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
      // Les orientations sont chargées séparément via loadOrientations()
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

  // NOTE: Les recommandations sont maintenant gérées via le système d'orientations unifiées
  // Voir les méthodes loadOrientations(), submitOrientation(), deleteOrientation() ci-dessus

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

  // Restaurer la réponse originale du patient
  restorePatientResponse(index: number): void {
    const questionsArray = this.anamneseForm.get('questionsReponses') as FormArray;
    const control = questionsArray.at(index);
    if (control) {
      const originalResponse = control.get('reponsePatientOriginale')?.value;
      control.get('reponse')?.setValue(originalResponse);
    }
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
        if (this.hasReponsesPatient) {
          return true;
        }
        return this.anamneseForm.valid;
      case 'examen_clinique': return true; // Examen clinique optionnel
      case 'examen_gynecologique':
        return !this.isGynecoConsultation() || this.examenGynecologiqueForm.valid;
      case 'examen_chirurgical':
        return !this.isChirurgieConsultation() || this.examenChirurgicalForm.valid;
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
        case 'examen_gynecologique':
          await this.saveExamenGynecologique();
          break;
        case 'examen_chirurgical':
          await this.saveExamenChirurgical();
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
    // Filtrer les questions sans réponse pour éviter d'envoyer des données vides
    const allQuestions = [
      ...form.questionsReponses
        .filter((q: any) => q.reponse && q.reponse.trim() !== '')
        .map((q: any) => ({
          questionId: String(q.questionId),
          question: q.question,
          reponse: q.reponse
        })),
      ...form.questionsLibres
        .filter((q: any) => q.question && q.reponse && q.question.trim() !== '' && q.reponse.trim() !== '')
        .map((q: any) => ({
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
    
    console.log('[Anamnèse] Sauvegarde:', allQuestions.length, 'questions avec réponses');
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

  private async saveExamenGynecologique(): Promise<void> {
    if (!this.isGynecoConsultation()) {
      return;
    }
    const form = this.examenGynecologiqueForm.value;
    await this.consultationService.saveExamenGynecologique(this.consultationId, form).toPromise();
  }

  private updateEtapes(): void {
    const newEtapes: EtapeConsultation[] = ['anamnese', 'examen_clinique'];
    if (this.isGynecoConsultation()) {
      newEtapes.push('examen_gynecologique');
    }
    if (this.isChirurgieConsultation()) {
      newEtapes.push('examen_chirurgical');
    }
    newEtapes.push('diagnostic', 'plan_traitement', 'conclusion', 'suivi');

    const current = this.etapeActuelle;
    this.etapes = newEtapes;
    if (!this.etapes.includes(current)) {
      this.etapeActuelle = this.etapes[0];
    }
  }

  public isGynecoConsultation(): boolean {
    return (this.consultation?.specialiteId ?? 0) === GYNECO_SPECIALITE_ID;
  }

  public isChirurgieConsultation(): boolean {
    return CHIRURGIE_SPECIALITE_IDS.includes(this.consultation?.specialiteId ?? 0);
  }

  private patchGynecologiqueForm(gyneco?: ExamenGynecologiqueDto): void {
    if (!gyneco) {
      this.examenGynecologiqueForm.reset();
      return;
    }
    this.examenGynecologiqueForm.patchValue({
      inspectionExterne: gyneco.inspectionExterne ?? '',
      examenSpeculum: gyneco.examenSpeculum ?? '',
      toucherVaginal: gyneco.toucherVaginal ?? '',
      autresObservations: gyneco.autresObservations ?? ''
    });
  }

  hasEtape(etape: EtapeConsultation): boolean {
    return this.etapes.includes(etape);
  }

  hasExamenGynecoData(): boolean {
    if (!this.isGynecoConsultation()) {
      return false;
    }
    const form = this.examenGynecologiqueForm.value || {};
    return !!(
      form.inspectionExterne?.trim() ||
      form.examenSpeculum?.trim() ||
      form.toucherVaginal?.trim() ||
      form.autresObservations?.trim()
    );
  }

  hasExamenChirurgicalData(): boolean {
    if (!this.isChirurgieConsultation()) {
      return false;
    }
    const form = this.examenChirurgicalForm.value || {};
    return !!(
      form.zoneExaminee?.trim() ||
      form.inspectionLocale?.trim() ||
      form.palpationLocale?.trim() ||
      form.signesInflammatoires?.trim() ||
      form.cicatricesExistantes?.trim() ||
      form.mobiliteFonction?.trim() ||
      form.conclusionChirurgicale?.trim()
    );
  }

  private async saveExamenChirurgical(): Promise<void> {
    if (!this.isChirurgieConsultation()) {
      return;
    }
    const form = this.examenChirurgicalForm.value;
    await this.consultationService.saveExamenChirurgical(this.consultationId, form).toPromise();
  }

  private patchChirurgicalForm(chirurgical?: any): void {
    if (!chirurgical) {
      this.examenChirurgicalForm.reset();
      return;
    }
    this.examenChirurgicalForm.patchValue({
      zoneExaminee: chirurgical.zoneExaminee ?? '',
      inspectionLocale: chirurgical.inspectionLocale ?? '',
      palpationLocale: chirurgical.palpationLocale ?? '',
      signesInflammatoires: chirurgical.signesInflammatoires ?? '',
      cicatricesExistantes: chirurgical.cicatricesExistantes ?? '',
      mobiliteFonction: chirurgical.mobiliteFonction ?? '',
      conclusionChirurgicale: chirurgical.conclusionChirurgicale ?? '',
      notesComplementaires: chirurgical.notesComplementaires ?? ''
    });
    // La décision chirurgicale est maintenant dans planTraitementForm
    if (chirurgical.decision) {
      this.planTraitementForm.patchValue({
        decisionChirurgicale: chirurgical.decision
      });
    }
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
      motifOrientation: form.motifOrientation,
      // Décision chirurgicale (uniquement pour les chirurgiens)
      decisionChirurgicale: this.isChirurgieConsultation() ? form.decisionChirurgicale : undefined
    };
    await this.consultationService.savePlanTraitement(this.consultationId, planTraitement as any).toPromise();
    
    // Si indication opératoire, ouvrir le panneau de programmation d'intervention
    if (this.isChirurgieConsultation() && form.decisionChirurgicale === 'indication_operatoire') {
      this.showProgrammationIntervention = true;
    }
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
      orientations: this.orientationsSauvegardees
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
    this.error = null;
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
    } catch (err: any) {
      console.error('Erreur clôture:', err);
      // Extraire le message d'erreur détaillé du backend
      if (err?.error?.erreurs && Array.isArray(err.error.erreurs)) {
        this.error = err.error.erreurs.join('. ');
      } else if (err?.error?.message) {
        this.error = err.error.message;
      } else {
        this.error = 'Erreur lors de la clôture de la consultation';
      }
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
        if (response.success && response.idRendezVous) {
          this.rdvSuiviCree = true;
          this.rdvSuiviInfo = {
            dateHeure: response.dateHeure ?? this.selectedCreneau?.dateHeure ?? '',
            idRendezVous: response.idRendezVous
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

  // ==================== CONVERSION CRÉNEAUX UNIFIÉS ====================

  /**
   * Convertir les créneaux de suivi vers le format unifié
   */
  get creneauxSuiviUnifies(): CreneauUnifie[] {
    return this.creneauxAvecStatut.map(c => ({
      dateHeure: c.dateHeure,
      heureDebut: c.heureDebut,
      heureFin: c.heureFin,
      duree: c.duree,
      statut: c.statut as 'disponible' | 'occupe' | 'passe' | 'indisponible',
      selectionnable: c.statut === 'disponible'
    }));
  }

  /**
   * Créneau de suivi sélectionné au format unifié
   */
  get selectedCreneauUnifie(): CreneauUnifie | null {
    if (!this.selectedCreneau) return null;
    return {
      dateHeure: this.selectedCreneau.dateHeure,
      heureDebut: this.selectedCreneau.heureDebut,
      heureFin: this.selectedCreneau.heureFin,
      duree: this.selectedCreneau.duree,
      statut: 'disponible',
      selectionnable: true
    };
  }

  /**
   * Gérer la sélection d'un créneau unifié (suivi)
   */
  onCreneauSuiviSelected(creneau: CreneauUnifie): void {
    this.selectedCreneau = {
      heureDebut: creneau.heureDebut,
      heureFin: creneau.heureFin,
      dateHeure: creneau.dateHeure,
      duree: creneau.duree
    };
  }

  /**
   * Convertir les créneaux d'orientation vers le format unifié
   */
  get creneauxOrientationUnifies(): CreneauUnifie[] {
    return this.orientationCreneaux.map(c => ({
      dateHeure: c.dateHeure,
      heureDebut: c.heureDebut,
      heureFin: c.heureFin,
      duree: c.duree,
      statut: c.statut,
      selectionnable: c.selectionnable
    }));
  }

  /**
   * Créneau d'orientation sélectionné au format unifié
   */
  get selectedOrientationCreneauUnifie(): CreneauUnifie | null {
    if (!this.orientationCreneauSelectionne) return null;
    return {
      dateHeure: this.orientationCreneauSelectionne.dateHeure,
      heureDebut: this.orientationCreneauSelectionne.heureDebut,
      heureFin: this.orientationCreneauSelectionne.heureFin,
      duree: this.orientationCreneauSelectionne.duree,
      statut: 'disponible',
      selectionnable: true
    };
  }

  /**
   * Gérer la sélection d'un créneau unifié (orientation)
   */
  onCreneauOrientationSelected(creneau: CreneauUnifie): void {
    this.orientationCreneauSelectionne = {
      dateHeure: creneau.dateHeure,
      heureDebut: creneau.heureDebut,
      heureFin: creneau.heureFin,
      duree: creneau.duree,
      statut: creneau.statut as 'disponible' | 'occupe' | 'passe' | 'indisponible',
      selectionnable: creneau.selectionnable
    };
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

  /**
   * Ouvrir le panneau de programmation d'intervention
   */
  openProgrammationIntervention(): void {
    this.showProgrammationIntervention = true;
  }

  /**
   * Fermer le panneau de programmation d'intervention
   */
  closeProgrammationIntervention(): void {
    this.showProgrammationIntervention = false;
  }

  /**
   * Callback quand la programmation est sauvegardée
   */
  onProgrammationSaved(idProgrammation: number): void {
    this.programmationCreee = true;
    this.showProgrammationIntervention = false;
  }

  /**
   * Vérifie si une indication opératoire a été décidée (maintenant dans planTraitementForm)
   */
  hasIndicationOperatoire(): boolean {
    return this.planTraitementForm?.get('decisionChirurgicale')?.value === 'indication_operatoire';
  }
}
