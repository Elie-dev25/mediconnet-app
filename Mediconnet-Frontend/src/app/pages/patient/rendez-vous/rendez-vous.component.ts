import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { DashboardLayoutComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { PATIENT_MENU_ITEMS, PATIENT_SIDEBAR_TITLE } from '../shared';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { 
  RendezVousService, 
  RendezVousListDto, 
  RendezVousStatsDto, 
  RendezVousDto,
  MedecinDisponibleDto,
  CreneauDisponibleDto,
  CreateRendezVousRequest,
  ServiceDto,
  ActionRdvResponse
} from '../../../services/rendez-vous.service';
import { 
  PatientAnamneseService, 
  AnamneseQuestionDto, 
  AnamneseQuestionsResponse 
} from '../../../services/patient-anamnese.service';
import { QuestionsPredefiniesService, QuestionPredefinie } from '../../../services/questions-predefinies.service';

@Component({
  selector: 'app-patient-rendez-vous',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    ReactiveFormsModule,
    LucideAngularModule,
    DashboardLayoutComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './rendez-vous.component.html',
  styleUrl: './rendez-vous.component.scss'
})
export class PatientRendezVousComponent implements OnInit {
  // Menu partagé pour toutes les pages patient
  menuItems = PATIENT_MENU_ITEMS;
  sidebarTitle = PATIENT_SIDEBAR_TITLE;

  // État
  activeTab: 'upcoming' | 'history' | 'missed' = 'upcoming';
  isLoading = true;
  
  // Données
  stats: RendezVousStatsDto | null = null;
  upcomingRdvs: RendezVousListDto[] = [];
  historyRdvs: RendezVousListDto[] = [];
  missedRdvs: RendezVousListDto[] = [];
  propositions: RendezVousDto[] = [];
  
  // Drawer questionnaire
  showQuestionnaireDrawer = false;
  selectedRdvForQuestionnaire: RendezVousListDto | null = null;
  questionnaireLoading = false;
  questionnaireError = '';
  questionnaireSuccess = '';
  questionnaireSaving = false;
  questionnaireData: AnamneseQuestionsResponse | null = null;
  questionnaireQuestions: QuestionPredefinie[] = [];
  questionnaireReponses: { [questionId: string]: string } = {};
  questionnaireConsultationId: number | null = null;
  questionnaireIsFirstConsult = true;
  questionnaireSpecialiteId = 0;

  // Modal nouveau RDV
  showNewRdvModal = false;
  currentStep = 1;
  services: ServiceDto[] = [];
  selectedServiceId: number | null = null;
  medecins: MedecinDisponibleDto[] = [];
  filteredMedecins: MedecinDisponibleDto[] = [];
  selectedMedecin: MedecinDisponibleDto | null = null;
  creneaux: CreneauDisponibleDto[] = [];
  selectedCreneau: CreneauDisponibleDto | null = null;
  rdvForm!: FormGroup;
  isSubmitting = false;
  error = '';
  success = '';

  // Calendrier
  currentMonth = new Date();
  selectedDate: Date | null = null;
  calendarDays: { date: Date; isCurrentMonth: boolean; hasSlots: boolean }[] = [];

  // Disponibilité médecin
  medecinDisponible = true;
  messageIndisponibilite = '';

  // Popup confirmation
  showConfirmationPopup = false;
  confirmedRdv: RendezVousDto | null = null;

  // Modal annulation
  showCancelModal = false;
  rdvToCancel: RendezVousListDto | null = null;
  cancelReason = '';

  // Modal refus proposition
  showRefuserModal = false;
  propositionToRefuse: RendezVousDto | null = null;
  refuserMotif = '';
  isProcessingProposition = false;

  constructor(
    private rdvService: RendezVousService,
    private fb: FormBuilder,
    private anamneseService: PatientAnamneseService,
    private questionsPredefiniesService: QuestionsPredefiniesService,
    private route: ActivatedRoute
  ) {
    this.initForm();
  }

  ngOnInit(): void {
    this.loadDataAndCheckQueryParams();
    this.generateCalendarDays();
  }

  private loadDataAndCheckQueryParams(): void {
    this.isLoading = true;

    forkJoin({
      stats: this.rdvService.getStats(),
      upcoming: this.rdvService.getUpcoming(),
      history: this.rdvService.getHistory(),
      propositions: this.rdvService.getPropositions()
    }).pipe(
      finalize(() => {
        this.isLoading = false;
      })
    ).subscribe({
      next: ({ stats, upcoming, history, propositions }) => {
        this.stats = stats;
        this.upcomingRdvs = upcoming;
        this.missedRdvs = history.filter(rdv => rdv.statut === 'absent');
        this.historyRdvs = history.filter(rdv => rdv.statut !== 'absent');
        this.propositions = propositions;

        // Vérifier si on doit ouvrir le questionnaire (redirect depuis nouveau-rdv)
        this.route.queryParams.subscribe(params => {
          if (params['questionnaire']) {
            const rdvId = parseInt(params['questionnaire']);
            if (rdvId) {
              setTimeout(() => this.openQuestionnaireByRdvId(rdvId), 300);
            }
          }
        });
      },
      error: (err) => {
        console.error('Erreur chargement RDV:', err);
      }
    });
  }

  private initForm(): void {
    this.rdvForm = this.fb.group({
      motif: ['', Validators.maxLength(100)],
      notes: ['', Validators.maxLength(500)],
      typeRdv: ['consultation']
    });
  }

  loadData(): void {
    this.isLoading = true;

    forkJoin({
      stats: this.rdvService.getStats(),
      upcoming: this.rdvService.getUpcoming(),
      history: this.rdvService.getHistory(),
      propositions: this.rdvService.getPropositions()
    }).pipe(
      finalize(() => {
        this.isLoading = false;
      })
    ).subscribe({
      next: ({ stats, upcoming, history, propositions }) => {
        this.stats = stats;
        this.upcomingRdvs = upcoming;
        // Séparer les RDV manqués (absent) de l'historique
        this.missedRdvs = history.filter(rdv => rdv.statut === 'absent');
        this.historyRdvs = history.filter(rdv => rdv.statut !== 'absent');
        this.propositions = propositions;
      },
      error: (err) => {
        console.error('Erreur chargement RDV:', err);
      }
    });
  }

  setActiveTab(tab: 'upcoming' | 'history' | 'missed'): void {
    this.activeTab = tab;
  }

  get currentRdvs(): RendezVousListDto[] {
    switch (this.activeTab) {
      case 'upcoming': return this.upcomingRdvs;
      case 'history': return this.historyRdvs;
      case 'missed': return this.missedRdvs;
      default: return this.upcomingRdvs;
    }
  }

  // Méthode pour obtenir la classe du badge de statut
  getStatusBadgeClass(statut: string): string {
    const classes: { [key: string]: string } = {
      'planifie': 'list-page-status-badge--pending',
      'confirme': 'list-page-status-badge--confirmed',
      'en_cours': 'list-page-status-badge--progress',
      'termine': 'list-page-status-badge--completed',
      'annule': 'list-page-status-badge--cancelled',
      'absent': 'list-page-status-badge--absent'
    };
    return classes[statut] || 'list-page-status-badge--pending';
  }

  // Ouvrir le drawer de questionnaire
  openQuestionnaireDrawer(rdv: RendezVousListDto): void {
    this.selectedRdvForQuestionnaire = rdv;
    this.showQuestionnaireDrawer = true;
    this.questionnaireLoading = true;
    this.questionnaireError = '';
    this.questionnaireSuccess = '';
    this.questionnaireData = null;
    this.questionnaireQuestions = [];
    this.questionnaireReponses = {};
    this.questionnaireConsultationId = null;

    this.anamneseService.getQuestionsByRdv(rdv.idRendezVous).subscribe({
      next: (data) => {
        this.questionnaireData = data;
        this.questionnaireConsultationId = data.consultationId ?? null;
        this.questionnaireIsFirstConsult = data.isPremiereConsultation;
        this.questionnaireSpecialiteId = data.specialiteId;

        // Charger les questions JSON adaptées au type de consultation
        const typeVisite = data.isPremiereConsultation ? 'premiere' : 'suivante';
        this.questionsPredefiniesService.getQuestionsParSpecialite(data.specialiteId, typeVisite).subscribe({
          next: (questions) => {
            this.questionnaireQuestions = questions;
            this.questionnaireLoading = false;
            // Pré-remplir les réponses existantes (par texte de question)
            if (data.existingReponses) {
              // Les réponses existantes sont indexées par questionId DB
              // On les mappe vers les questions prédéfinies par texte
              const dbQuestions = data.questions || [];
              for (const q of questions) {
                const dbQ = dbQuestions.find(dq => dq.texte === q.texte);
                if (dbQ && data.existingReponses[dbQ.id]) {
                  this.questionnaireReponses[q.id] = data.existingReponses[dbQ.id];
                }
              }
            }
          },
          error: () => {
            this.questionnaireQuestions = [];
            this.questionnaireLoading = false;
          }
        });
      },
      error: (err) => {
        this.questionnaireLoading = false;
        this.questionnaireError = err.error?.message || 'Impossible de charger le questionnaire';
      }
    });
  }

  openQuestionnaireByRdvId(rdvId: number): void {
    // Trouver le RDV dans la liste des upcoming
    const rdv = this.upcomingRdvs.find(r => r.idRendezVous === rdvId);
    if (rdv) {
      this.openQuestionnaireDrawer(rdv);
    }
  }

  closeQuestionnaireDrawer(): void {
    this.showQuestionnaireDrawer = false;
    this.selectedRdvForQuestionnaire = null;
    this.questionnaireData = null;
    this.questionnaireQuestions = [];
    this.questionnaireReponses = {};
    this.questionnaireError = '';
    this.questionnaireSuccess = '';
    this.questionnaireConsultationId = null;
  }

  onQuestionnaireReponseChange(questionId: string, value: string): void {
    this.questionnaireReponses[questionId] = value;
  }

  submitQuestionnaire(): void {
    if (!this.questionnaireConsultationId || this.questionnaireSaving) return;

    // Vérifier les champs obligatoires
    const missingRequired = this.questionnaireQuestions
      .filter(q => q.obligatoire && !this.questionnaireReponses[q.id]?.trim());
    
    if (missingRequired.length > 0) {
      this.questionnaireError = 'Veuillez remplir tous les champs obligatoires';
      return;
    }

    this.questionnaireSaving = true;
    this.questionnaireError = '';
    this.questionnaireSuccess = '';

    // Construire les réponses avec le texte de la question pour le backend
    const reponses = this.questionnaireQuestions
      .filter(q => this.questionnaireReponses[q.id]?.trim())
      .map(q => ({
        texteQuestion: q.texte,
        typeQuestion: q.type,
        valeurReponse: this.questionnaireReponses[q.id]
      }));

    this.anamneseService.saveReponsesAvecQuestions({
      consultationId: this.questionnaireConsultationId,
      reponses
    }).subscribe({
      next: (res) => {
        this.questionnaireSaving = false;
        if (res.success) {
          this.questionnaireSuccess = 'Vos réponses ont été enregistrées avec succès !';
          if (this.selectedRdvForQuestionnaire) {
            this.selectedRdvForQuestionnaire.anamneseRemplie = true;
          }
          setTimeout(() => this.closeQuestionnaireDrawer(), 2000);
        } else {
          this.questionnaireError = res.message || 'Erreur lors de l\'enregistrement';
        }
      },
      error: (err) => {
        this.questionnaireSaving = false;
        this.questionnaireError = err.error?.message || 'Erreur serveur';
      }
    });
  }

  getQuestionnaireProgress(): number {
    if (!this.questionnaireQuestions?.length) return 0;
    const answered = this.questionnaireQuestions.filter(q => this.questionnaireReponses[q.id]?.trim()).length;
    return Math.round((answered / this.questionnaireQuestions.length) * 100);
  }

  getConsultationTypeLabel(): string {
    return this.questionnaireIsFirstConsult ? 'Première consultation' : 'Consultation de suivi';
  }

  // Reprogrammer un RDV manqué
  reprogrammerRdv(rdv: RendezVousListDto): void {
    // Rediriger vers la page de nouveau RDV avec le médecin pré-sélectionné
    // Pour l'instant, on redirige simplement vers la page nouveau-rdv
    window.location.href = '/patient/nouveau-rdv';
  }

  // ==================== MODAL NOUVEAU RDV ====================

  openNewRdvModal(): void {
    this.showNewRdvModal = true;
    this.currentStep = 1;
    this.selectedServiceId = null;
    this.selectedMedecin = null;
    this.selectedCreneau = null;
    this.selectedDate = null;
    this.creneaux = [];
    this.filteredMedecins = [];
    this.error = '';
    this.rdvForm.reset({ typeRdv: 'consultation' });
    this.loadServices();
    this.loadMedecins();
  }

  closeNewRdvModal(): void {
    this.showNewRdvModal = false;
  }

  loadServices(): void {
    this.rdvService.getServices().subscribe({
      next: (services) => this.services = services,
      error: (err) => console.error('Erreur services:', err)
    });
  }

  loadMedecins(): void {
    this.rdvService.getMedecins().subscribe({
      next: (medecins) => {
        this.medecins = medecins;
        this.filterMedecinsByService();
      },
      error: (err) => console.error('Erreur médecins:', err)
    });
  }

  onServiceChange(serviceId: number | null): void {
    this.selectedServiceId = serviceId;
    this.selectedMedecin = null;
    this.filterMedecinsByService();
  }

  filterMedecinsByService(): void {
    if (!this.selectedServiceId) {
      this.filteredMedecins = this.medecins;
    } else {
      this.filteredMedecins = this.medecins.filter(m => m.idService === this.selectedServiceId);
    }
  }

  selectMedecin(medecin: MedecinDisponibleDto): void {
    this.selectedMedecin = medecin;
    this.currentStep = 2;
    this.selectedDate = null;
    this.selectedCreneau = null;
    this.creneaux = [];
    this.medecinDisponible = true;
    this.messageIndisponibilite = '';
    this.loadCreneauxMedecin();
  }

  // ==================== CALENDRIER ====================

  generateCalendarDays(): void {
    const year = this.currentMonth.getFullYear();
    const month = this.currentMonth.getMonth();
    
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    
    // Début: lundi de la semaine du 1er
    const startDate = new Date(firstDay);
    startDate.setDate(startDate.getDate() - (startDate.getDay() === 0 ? 6 : startDate.getDay() - 1));
    
    // Fin: dimanche de la semaine du dernier jour
    const endDate = new Date(lastDay);
    endDate.setDate(endDate.getDate() + (7 - (endDate.getDay() === 0 ? 7 : endDate.getDay())));

    this.calendarDays = [];
    const current = new Date(startDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0); // Comparer uniquement les dates, pas les heures
    
    while (current <= endDate) {
      const currentDate = new Date(current);
      currentDate.setHours(0, 0, 0, 0);
      
      this.calendarDays.push({
        date: new Date(current),
        isCurrentMonth: current.getMonth() === month,
        // Permettre le jour même (>=) et exclure les dimanches
        // Le backend gère les créneaux passés à la minute près
        hasSlots: currentDate >= today && current.getDay() !== 0
      });
      current.setDate(current.getDate() + 1);
    }
  }

  prevMonth(): void {
    this.currentMonth = new Date(this.currentMonth.getFullYear(), this.currentMonth.getMonth() - 1, 1);
    this.generateCalendarDays();
  }

  nextMonth(): void {
    this.currentMonth = new Date(this.currentMonth.getFullYear(), this.currentMonth.getMonth() + 1, 1);
    this.generateCalendarDays();
  }

  selectDate(day: { date: Date; hasSlots: boolean }): void {
    if (!day.hasSlots || !this.selectedMedecin) return;
    
    this.selectedDate = day.date;
    this.selectedCreneau = null;
    this.loadCreneaux();
  }

  isDateSelected(date: Date): boolean {
    if (!this.selectedDate) return false;
    return date.toDateString() === this.selectedDate.toDateString();
  }

  isToday(date: Date): boolean {
    return date.toDateString() === new Date().toDateString();
  }

  loadCreneaux(): void {
    if (!this.selectedMedecin || !this.selectedDate) return;

    // Format date sans conversion UTC pour éviter le décalage de jour
    const formatLocalDate = (d: Date): string => {
      const year = d.getFullYear();
      const month = String(d.getMonth() + 1).padStart(2, '0');
      const day = String(d.getDate()).padStart(2, '0');
      const hours = String(d.getHours()).padStart(2, '0');
      const minutes = String(d.getMinutes()).padStart(2, '0');
      const seconds = String(d.getSeconds()).padStart(2, '0');
      return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
    };

    const dateDebut = new Date(this.selectedDate);
    dateDebut.setHours(0, 0, 0, 0);
    
    const dateFin = new Date(this.selectedDate);
    dateFin.setHours(23, 59, 59, 999);

    this.rdvService.getCreneaux(
      this.selectedMedecin.idMedecin,
      formatLocalDate(dateDebut),
      formatLocalDate(dateFin)
    ).subscribe({
      next: (response) => {
        this.medecinDisponible = response.medecinDisponible;
        this.messageIndisponibilite = response.messageIndisponibilite || '';
        this.creneaux = response.creneaux;
      },
      error: (err) => console.error('Erreur créneaux:', err)
    });
  }

  // Charger tous les créneaux d'un médecin pour la semaine
  loadCreneauxMedecin(): void {
    if (!this.selectedMedecin) return;

    const formatLocalDate = (d: Date): string => {
      const year = d.getFullYear();
      const month = String(d.getMonth() + 1).padStart(2, '0');
      const day = String(d.getDate()).padStart(2, '0');
      const hours = String(d.getHours()).padStart(2, '0');
      const minutes = String(d.getMinutes()).padStart(2, '0');
      const seconds = String(d.getSeconds()).padStart(2, '0');
      return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
    };

    const dateDebut = new Date();
    dateDebut.setHours(0, 0, 0, 0);
    const dateFin = new Date();
    dateFin.setDate(dateFin.getDate() + 14); // 2 semaines
    dateFin.setHours(23, 59, 59, 999);

    this.rdvService.getCreneaux(
      this.selectedMedecin.idMedecin,
      formatLocalDate(dateDebut),
      formatLocalDate(dateFin)
    ).subscribe({
      next: (response) => {
        this.medecinDisponible = response.medecinDisponible;
        this.messageIndisponibilite = response.messageIndisponibilite || '';
      },
      error: (err) => console.error('Erreur créneaux:', err)
    });
  }

  selectCreneau(creneau: CreneauDisponibleDto): void {
    if (!creneau.disponible) return;
    this.selectedCreneau = creneau;
    this.currentStep = 3;
  }

  // ==================== CONFIRMATION ====================

  goBack(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
      if (this.currentStep === 1) {
        this.selectedMedecin = null;
      }
    }
  }

  submitRdv(): void {
    if (!this.selectedMedecin || !this.selectedCreneau) return;

    this.isSubmitting = true;
    this.error = '';

    const request: CreateRendezVousRequest = {
      idMedecin: this.selectedMedecin.idMedecin,
      dateHeure: this.selectedCreneau.dateHeure,
      duree: this.selectedCreneau.duree,
      motif: this.rdvForm.value.motif || undefined,
      notes: this.rdvForm.value.notes || undefined,
      typeRdv: this.rdvForm.value.typeRdv
    };

    this.rdvService.create(request).subscribe({
      next: (res) => {
        this.isSubmitting = false;
        this.confirmedRdv = res.rendezVous;
        this.closeNewRdvModal();
        this.showConfirmationPopup = true;
        this.loadData();
      },
      error: (err) => {
        this.error = err.error?.message || 'Erreur lors de la création du rendez-vous';
        this.isSubmitting = false;
      }
    });
  }

  closeConfirmationPopup(): void {
    this.showConfirmationPopup = false;
    this.confirmedRdv = null;
  }

  // ==================== ANNULATION ====================

  openCancelModal(rdv: RendezVousListDto): void {
    this.rdvToCancel = rdv;
    this.cancelReason = '';
    this.showCancelModal = true;
  }

  closeCancelModal(): void {
    this.showCancelModal = false;
    this.rdvToCancel = null;
  }

  confirmCancel(): void {
    if (!this.rdvToCancel || !this.cancelReason.trim()) return;

    this.rdvService.annuler({
      idRendezVous: this.rdvToCancel.idRendezVous,
      motif: this.cancelReason
    }).subscribe({
      next: () => {
        this.closeCancelModal();
        this.loadData();
      },
      error: (err) => {
        this.error = err.error?.message || 'Erreur lors de l\'annulation';
      }
    });
  }

  // ==================== HELPERS ====================

  formatDate(dateStr: string): string {
    return this.rdvService.formatDate(dateStr);
  }

  formatTime(dateStr: string): string {
    return this.rdvService.formatTime(dateStr);
  }

  formatDateTime(dateStr: string): string {
    return this.rdvService.formatDateTime(dateStr);
  }

  getStatutLabel(statut: string): string {
    return this.rdvService.getStatutLabel(statut);
  }

  getStatutClass(statut: string): string {
    const classes: { [key: string]: string } = {
      'planifie': 'status-planned',
      'confirme': 'status-confirmed',
      'termine': 'status-completed',
      'annule': 'status-cancelled'
    };
    return classes[statut] || '';
  }

  getTypeRdvLabel(type: string): string {
    return this.rdvService.getTypeRdvLabel(type);
  }

  get monthName(): string {
    return this.currentMonth.toLocaleDateString('fr-FR', { month: 'long', year: 'numeric' });
  }

  refresh(): void {
    this.loadData();
  }

  // ==================== GESTION PROPOSITIONS ====================

  accepterProposition(proposition: RendezVousDto): void {
    if (this.isProcessingProposition) return;
    this.isProcessingProposition = true;

    this.rdvService.accepterProposition(proposition.idRendezVous)
      .pipe(finalize(() => this.isProcessingProposition = false))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.loadData();
          } else if (response.conflitDetecte) {
            alert(response.message);
          } else {
            alert(response.message);
          }
        },
        error: (err) => {
          console.error('Erreur acceptation proposition:', err);
          alert(err.error?.message || 'Erreur lors de l\'acceptation');
        }
      });
  }

  openRefuserModal(proposition: RendezVousDto): void {
    this.propositionToRefuse = proposition;
    this.refuserMotif = '';
    this.showRefuserModal = true;
  }

  closeRefuserModal(): void {
    this.showRefuserModal = false;
    this.propositionToRefuse = null;
    this.refuserMotif = '';
  }

  confirmRefuser(): void {
    if (!this.propositionToRefuse) return;
    if (this.isProcessingProposition) return;
    this.isProcessingProposition = true;

    this.rdvService.refuserProposition(this.propositionToRefuse.idRendezVous, this.refuserMotif)
      .pipe(finalize(() => this.isProcessingProposition = false))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.closeRefuserModal();
            this.loadData();
          } else {
            alert(response.message);
          }
        },
        error: (err) => {
          console.error('Erreur refus proposition:', err);
          alert(err.error?.message || 'Erreur lors du refus');
        }
      });
  }
}
