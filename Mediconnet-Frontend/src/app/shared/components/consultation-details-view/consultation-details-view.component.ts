import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { ConsultationCompleteService, ConsultationDetailDto, ConsultationEnCoursDto, ConclusionDto, ExamenCliniqueDto, ExamenGynecologiqueDto, ExamenChirurgicalDto, ParametresVitauxDto, PlanTraitementDto, OrdonnanceDto, ExamenPrescritDetailDto } from '../../../services/consultation-complete.service';
import { PrintService, PrintableConsultation } from '../../../services/print.service';

export type ConsultationViewMode = 'patient' | 'medecin';

interface ConsultationStep {
  id: string;
  label: string;
  icon: string;
  hasData: boolean;
}

@Component({
  selector: 'app-consultation-details-view',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './consultation-details-view.component.html',
  styleUrl: './consultation-details-view.component.scss'
})
export class ConsultationDetailsViewComponent implements OnInit {
  @Input() consultationId: number | null = null;
  @Input() mode: ConsultationViewMode = 'patient';

  consultation: ConsultationDetailDto | null = null;
  isLoading = true;
  error: string | null = null;
  steps: ConsultationStep[] = [];
  activeStepId: string | null = null;

  constructor(
    private router: Router,
    private consultationService: ConsultationCompleteService,
    private printService: PrintService
  ) {}

  ngOnInit(): void {
    if (this.consultationId) {
      this.loadConsultation();
    } else {
      this.error = 'ID de consultation manquant';
      this.isLoading = false;
    }
  }

  get isReadonly(): boolean {
    return this.consultation?.statut === 'terminee';
  }

  selectStep(stepId: string): void {
    this.activeStepId = stepId;
  }

  trackByStepId(_: number, step: ConsultationStep): string {
    return step.id;
  }

  hasAnamneseData(): boolean {
    const c = this.consultation;
    return !!(c?.anamnese || (c?.questionnaire && c.questionnaire.length > 0) || c?.motif);
  }

  hasExamenData(): boolean {
    const examen = this.getExamenClinique();
    return !!(
      this.consultation?.notesCliniques ||
      this.hasParametresVitaux() ||
      examen?.inspection ||
      examen?.palpation ||
      examen?.auscultation ||
      examen?.percussion ||
      examen?.autresObservations
    );
  }

  hasExamenGynecologiqueData(): boolean {
    const examen = this.getExamenGynecologique();
    if (!examen) {
      return false;
    }
    return !!(
      examen.inspectionExterne?.trim() ||
      examen.examenSpeculum?.trim() ||
      examen.toucherVaginal?.trim() ||
      examen.autresObservations?.trim()
    );
  }

  hasExamenChirurgicalData(): boolean {
    const examen = this.getExamenChirurgical();
    if (!examen) {
      return false;
    }
    return !!(
      examen.zoneExaminee?.trim() ||
      examen.inspectionLocale?.trim() ||
      examen.palpationLocale?.trim() ||
      examen.signesInflammatoires?.trim() ||
      examen.conclusionChirurgicale?.trim() ||
      examen.decision?.trim()
    );
  }

  getExamenChirurgical(): ExamenChirurgicalDto | undefined {
    return this.consultation?.examenChirurgical;
  }

  getDecisionChirurgicaleLabel(decision?: string): string {
    const labels: { [key: string]: string } = {
      'surveillance': 'Surveillance',
      'traitement_medical': 'Traitement médical',
      'indication_operatoire': 'Indication opératoire'
    };
    return decision ? (labels[decision] || decision) : 'Non renseigné';
  }

  hasDiagnosticData(): boolean {
    const c = this.consultation;
    return !!(c?.diagnostic || c?.conclusion || this.hasConclusionDetailleeData());
  }

  hasPrescriptionData(): boolean {
    const ordonnance = this.consultation?.ordonnance || this.consultation?.planTraitement?.ordonnance;
    return !!(ordonnance?.medicaments && ordonnance.medicaments.length > 0);
  }

  hasExamensData(): boolean {
    const examens = this.consultation?.examensPrescrits || this.consultation?.planTraitement?.examensPrescrits;
    return !!(examens && examens.length > 0);
  }

  getOrdonnance(): OrdonnanceDto | undefined {
    return this.consultation?.ordonnance || this.consultation?.planTraitement?.ordonnance;
  }

  getExamensPrescrits(): ExamenPrescritDetailDto[] {
    return this.consultation?.examensPrescrits || 
           this.consultation?.planTraitement?.examensPrescrits?.map(e => ({
             nomExamen: e.nomExamen,
             instructions: e.notes,
             statut: 'prescrit'
           })) || [];
  }

  hasRecommandationsData(): boolean {
    return !!this.consultation?.recommandations;
  }

  hasPlanTraitementData(): boolean {
    const plan = this.consultation?.planTraitement;
    return !!(
      plan && (
        plan.explicationDiagnostic ||
        plan.optionsTraitement ||
        plan.orientationSpecialiste ||
        plan.motifOrientation ||
        plan.motifOrientation ||
        (plan.examensPrescrits && plan.examensPrescrits.length > 0) ||
        plan.ordonnance
      )
    );
  }

  hasParametresVitaux(): boolean {
    return this.hasAnyParametreValue(this.rawParametresVitaux());
  }

  getParametresVitaux(): ParametresVitauxDto | undefined {
    const vitaux = this.rawParametresVitaux();
    return this.hasAnyParametreValue(vitaux) ? vitaux : undefined;
  }

  getExamenClinique(): ExamenCliniqueDto | undefined {
    return this.consultation?.examenClinique;
  }

  getPlanTraitement(): PlanTraitementDto | undefined {
    return this.consultation?.planTraitement;
  }

  getExamenGynecologique(): ExamenGynecologiqueDto | undefined {
    return this.consultation?.examenGynecologique;
  }

  getConclusionDetaillee(): ConclusionDto | undefined {
    return this.consultation?.conclusionDetaillee;
  }

  hasConclusionDetailleeData(): boolean {
    const detail = this.consultation?.conclusionDetaillee;
    return !!(
      detail && (
        detail.resumeConsultation ||
        detail.questionsPatient ||
        detail.consignesPatient ||
        detail.recommandations ||
        detail.typeSuivi ||
        detail.dateSuiviPrevue ||
        detail.notesSuivi
      )
    );
  }

  displayValue(value?: string | number | null, suffix?: string): string {
    if (value === null || value === undefined || value === '') {
      return 'Non renseigné';
    }
    return suffix ? `${value} ${suffix}`.trim() : `${value}`;
  }

  displayDuration(minutes?: number | null): string {
    if (minutes === null || minutes === undefined) {
      return 'Non renseigné';
    }
    return `${minutes} minutes`;
  }

  getParametresPrisParLabel(): string {
    const examen = this.getExamenClinique();
    if (!examen) {
      return 'Non renseigné';
    }
    if (examen.parametresPrisParInfirmier) {
      return examen.infirmierNom || 'Infirmier(ère)';
    }
    return 'Médecin';
  }

  formatDateOrPlaceholder(value?: string | Date): string {
    if (!value) {
      return 'Non renseigné';
    }
    const str = value instanceof Date ? value.toISOString() : value;
    return this.formatDate(str);
  }

  formatTimeOrPlaceholder(value?: string | Date): string {
    if (!value) {
      return 'Non renseigné';
    }
    const str = value instanceof Date ? value.toISOString() : value;
    return this.formatTime(str);
  }

  private rawParametresVitaux(): ParametresVitauxDto | undefined {
    return this.consultation?.examenClinique?.parametresVitaux || this.consultation?.parametresVitaux || undefined;
  }

  private hasAnyParametreValue(parametres?: ParametresVitauxDto | null): parametres is ParametresVitauxDto {
    if (!parametres) {
      return false;
    }

    const keys: (keyof ParametresVitauxDto)[] = [
      'poids',
      'taille',
      'temperature',
      'tensionArterielle',
      'frequenceCardiaque',
      'frequenceRespiratoire',
      'saturationOxygene',
      'glycemie'
    ];

    return keys.some((key) => {
      const value = parametres[key];
      return value !== null && value !== undefined && value !== '';
    });
  }

  goBack(): void {
    const backRoute = this.mode === 'medecin' ? '/medecin/consultations' : '/patient/dossier';
    this.router.navigate([backRoute]);
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    });
  }

  formatTime(dateStr: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleTimeString('fr-FR', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getStatutLabel(statut: string): string {
    const labels: { [key: string]: string } = {
      'a_faire': 'À faire',
      'en_cours': 'En cours',
      'terminee': 'Terminée'
    };
    return labels[statut] || statut;
  }

  getStatutClass(statut: string): string {
    const classes: { [key: string]: string } = {
      'a_faire': 'status-pending',
      'en_cours': 'status-progress',
      'terminee': 'status-completed'
    };
    return classes[statut] || '';
  }

  /**
   * Imprime la fiche patient de manière professionnelle
   */
  printConsultation(): void {
    if (!this.consultation) return;
    
    const printData = this.buildPrintableData();
    this.printService.printConsultation(printData);
  }

  /**
   * Télécharge la fiche patient en PDF
   */
  downloadPDF(): void {
    if (!this.consultation) return;
    
    const printData = this.buildPrintableData();
    this.printService.downloadConsultationPDF(printData);
  }

  /**
   * Construit les données pour l'impression
   */
  private buildPrintableData(): PrintableConsultation {
    const c = this.consultation!;
    
    // Extraire les infos médecin du RDV de suivi si disponible
    const medecinNom = (c as any).medecinNom || c.rdvSuivi?.medecinNom || 'Médecin traitant';
    const serviceNom = (c as any).serviceNom || c.rdvSuivi?.serviceNom;
    
    return {
      etablissement: {
        nom: 'MédiConnect',
        adresse: 'Centre Hospitalier Universitaire',
        telephone: '+237 6XX XXX XXX',
        email: 'contact@mediconnect.cm'
      },
      patient: {
        nom: c.patientNom || 'Non renseigné',
        prenom: c.patientPrenom || '',
        numeroDossier: c.numeroDossier,
        age: (c as any).patientAge,
        sexe: (c as any).patientSexe
      },
      medecin: {
        nom: medecinNom,
        prenom: (c as any).medecinPrenom,
        specialite: (c as any).medecinSpecialite,
        service: serviceNom
      },
      consultation: c
    };
  }

  private loadConsultation(): void {
    if (!this.consultationId) {
      return;
    }

    this.consultationService.getConsultationDetails(this.consultationId).subscribe({
      next: (consultation) => {
        this.consultation = consultation;
        this.isLoading = false;
        this.buildSteps();
      },
      error: () => {
        this.consultationService.getConsultation(this.consultationId!).subscribe({
          next: (data) => {
            this.consultation = this.mapToDetailDto(data);
            this.isLoading = false;
            this.buildSteps();
          },
          error: (err) => {
            console.error('Erreur chargement consultation:', err);
            this.error = 'Impossible de charger les détails de la consultation';
            this.isLoading = false;
          }
        });
      }
    });
  }

  /**
   * Construit les étapes de consultation dans l'ordre chronologique exact:
   * 1. Anamnèse
   * 2. Examen clinique
   * 3. Examen gynécologique (si gynécologue)
   * 4. Diagnostic
   * 5. Traitement (prescriptions + examens)
   * 6. Conclusion
   * 7. Suivi
   */
  private buildSteps(): void {
    const c = this.consultation;
    if (!c) {
      this.steps = [];
      this.activeStepId = null;
      return;
    }

    const steps: ConsultationStep[] = [
      // Étape 1: Anamnèse
      { id: 'anamnese', label: 'Anamnèse', icon: 'clipboard-list', hasData: this.hasAnamneseData() },
      // Étape 2: Examen clinique
      { id: 'examen', label: 'Examen clinique', icon: 'stethoscope', hasData: this.hasExamenData() }
    ];

    // Étape 3: Examen gynécologique (uniquement si données présentes = consultation gynéco)
    if (this.hasExamenGynecologiqueData()) {
      steps.push({ id: 'examen_gynecologique', label: 'Examen gynécologique', icon: 'sparkles', hasData: true });
    }

    // Étape 3 bis: Examen chirurgical (uniquement si données présentes = consultation chirurgicale)
    if (this.hasExamenChirurgicalData()) {
      steps.push({ id: 'examen_chirurgical', label: 'Examen chirurgical', icon: 'scissors', hasData: true });
    }

    // Étape 4: Diagnostic
    steps.push({ id: 'diagnostic', label: 'Diagnostic', icon: 'activity', hasData: this.hasDiagnosticData() });

    // Étape 5: Traitement (regroupe prescriptions médicaments + examens prescrits)
    steps.push({ id: 'traitement', label: 'Traitement', icon: 'pill', hasData: this.hasTraitementData() });

    // Étape 6: Conclusion
    steps.push({ id: 'conclusion', label: 'Conclusion', icon: 'check-circle-2', hasData: this.hasConclusionData() });

    // Étape 7: Suivi
    steps.push({ id: 'suivi', label: 'Suivi', icon: 'calendar-check', hasData: this.hasSuiviData() });

    this.steps = steps;
    const firstWithData = steps.find(step => step.hasData);
    this.activeStepId = (firstWithData || steps[0] || { id: null }).id;
  }

  hasTraitementData(): boolean {
    return this.hasPrescriptionData() || this.hasExamensData() || this.hasPlanTraitementData();
  }

  hasConclusionData(): boolean {
    const detail = this.consultation?.conclusionDetaillee;
    return !!(
      this.consultation?.conclusion ||
      detail?.resumeConsultation ||
      detail?.questionsPatient ||
      detail?.consignesPatient
    );
  }

  hasSuiviData(): boolean {
    const detail = this.consultation?.conclusionDetaillee;
    return !!(
      detail?.typeSuivi ||
      detail?.dateSuiviPrevue ||
      detail?.notesSuivi ||
      this.consultation?.recommandations ||
      this.consultation?.rdvSuivi
    );
  }

  hasRdvSuiviData(): boolean {
    return !!this.consultation?.rdvSuivi;
  }

  getRdvSuiviStatutLabel(statut: string): string {
    const labels: { [key: string]: string } = {
      'planifie': 'Planifié',
      'confirme': 'Confirmé',
      'en_cours': 'En cours',
      'termine': 'Terminé',
      'annule': 'Annulé',
      'absent': 'Absent'
    };
    return labels[statut] || statut;
  }

  getRdvSuiviStatutClass(statut: string): string {
    const classes: { [key: string]: string } = {
      'planifie': 'status-pending',
      'confirme': 'status-confirmed',
      'en_cours': 'status-progress',
      'termine': 'status-completed',
      'annule': 'status-cancelled',
      'absent': 'status-absent'
    };
    return classes[statut] || '';
  }

  private mapToDetailDto(data: ConsultationEnCoursDto): ConsultationDetailDto {
    return {
      idConsultation: data.idConsultation,
      idPatient: data.idPatient,
      patientNom: data.patientNom,
      patientPrenom: data.patientPrenom,
      dateConsultation: data.dateHeure?.toString() || new Date().toISOString(),
      motif: data.motif,
      statut: data.statut || 'a_faire',
      anamnese: data.anamnese?.histoireMaladie,
      notesCliniques: data.diagnostic?.notesCliniques,
      diagnostic: data.diagnostic?.diagnosticPrincipal,
      conclusion: data.conclusion?.resumeConsultation,
      recommandations: data.conclusion?.recommandations,
      ordonnance: data.prescriptions?.ordonnance,
      examensPrescrits: data.prescriptions?.examens?.map(e => ({
        nomExamen: e.nomExamen,
        instructions: e.notes
      })),
      questionnaire: data.anamnese?.questionsReponses,
      parametresVitaux: data.examenClinique?.parametresVitaux || data.anamnese?.parametresVitaux,
      examenClinique: data.examenClinique,
      examenGynecologique: data.examenGynecologique,
      examenChirurgical: data.examenChirurgical,
      planTraitement: data.planTraitement,
      conclusionDetaillee: data.conclusion
    };
  }
}
