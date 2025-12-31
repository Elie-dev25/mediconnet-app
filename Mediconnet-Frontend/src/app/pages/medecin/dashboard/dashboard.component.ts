import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../../services/auth.service';
import { MedecinService, MedecinDashboardDto, RendezVousMedecinDto } from '../../../services/medecin.service';
import { MedecinPlanningService, RdvPlanningDto } from '../../../services/medecin-planning.service';
import { SignalRService } from '../../../services/signalr.service';
import { 
  DashboardLayoutComponent, 
  WelcomeBannerComponent,
  StatsGridComponent,
  StatItem,
  MiniAgendaComponent,
  ConsultationQuestionnaireFormComponent,
  ModalComponent,
  LucideAngularModule,
  ALL_ICONS_PROVIDER,
  formatTime,
  formatDateWithWeekday
} from '../../../shared';
import { ConsultationCompleteService } from '../../../services/consultation-complete.service';
import { MEDECIN_MENU_ITEMS, MEDECIN_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-medecin-dashboard',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule,
    RouterModule, 
    LucideAngularModule, 
    DashboardLayoutComponent,
    WelcomeBannerComponent,
    StatsGridComponent,
    MiniAgendaComponent,
    ConsultationQuestionnaireFormComponent,
    ModalComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class MedecinDashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // Menu partagé pour toutes les pages médecin
  menuItems = MEDECIN_MENU_ITEMS;
  sidebarTitle = MEDECIN_SIDEBAR_TITLE;

  userName = '';
  userRole = 'medecin';

  // Stats dynamiques
  stats: StatItem[] = [
    { icon: 'users', label: 'Patients', value: 0, colorClass: 'patients' },
    { icon: 'stethoscope', label: 'Consultations', value: 0, colorClass: 'consultations' },
    { icon: 'calendar-check', label: 'RDV Aujourd\'hui', value: 0, colorClass: 'rdv' },
    { icon: 'clock', label: 'RDV à venir', value: 0, colorClass: 'rdv-avenir' }
  ];

  // File d'attente (RDV confirmés du jour)
  isLoadingQueue = false;
  queueError: string | null = null;
  fileAttente: RendezVousMedecinDto[] = [];

  // RDV en attente de validation
  isLoadingPending = false;
  pendingError: string | null = null;
  rdvEnAttente: RdvPlanningDto[] = [];

  // Modal actions
  showAnnulerModal = false;
  showSuggererModal = false;
  selectedRdvForAction: RdvPlanningDto | null = null;
  motifAnnulation = '';
  nouveauCreneau = '';
  messageSuggestion = '';
  isProcessing = false;

  showQuestionnaire = false;
  selectedRdvForQuestionnaire: RendezVousMedecinDto | null = null;


  constructor(
    private authService: AuthService,
    private medecinService: MedecinService,
    private medecinPlanningService: MedecinPlanningService,
    private signalRService: SignalRService,
    private consultationCompleteService: ConsultationCompleteService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.userName = `${user.prenom || ''} ${user.nom || ''}`.trim();
    }
    this.loadStats();
    this.loadFileAttente();
    this.loadRdvEnAttente();
    this.subscribeRealtime();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private subscribeRealtime(): void {
    this.signalRService.appointmentEvents$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.loadStats();
        this.loadFileAttente();
      });

    this.signalRService.slotEvents$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.loadFileAttente();
      });

    this.signalRService.vitalsEvents$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.loadStats();
        this.loadFileAttente();
        this.loadRdvEnAttente();
      });
  }

  private loadRdvEnAttente(): void {
    this.isLoadingPending = true;
    this.pendingError = null;

    this.medecinPlanningService.getRdvEnAttente()
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.isLoadingPending = false)
      )
      .subscribe({
        next: (rdvs) => {
          this.rdvEnAttente = rdvs || [];
        },
        error: (err) => {
          console.error('Erreur chargement RDV en attente:', err);
          this.pendingError = 'Impossible de charger les RDV en attente';
          this.rdvEnAttente = [];
        }
      });
  }

  private loadStats(): void {
    this.medecinService.getDashboard()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data: MedecinDashboardDto) => {
          this.stats = [
            { icon: 'users', label: 'Patients', value: data.totalPatients, colorClass: 'patients' },
            { icon: 'stethoscope', label: 'Consultations', value: data.consultationsMois, colorClass: 'consultations' },
            { icon: 'calendar-check', label: 'RDV Aujourd\'hui', value: data.rdvAujourdHui, colorClass: 'rdv' },
            { icon: 'clock', label: 'RDV à venir', value: data.rdvAVenir, colorClass: 'rdv-avenir' }
          ];
        },
        error: (err) => console.error('Erreur chargement stats:', err)
      });
  }

  private loadFileAttente(): void {
    this.isLoadingQueue = true;
    this.queueError = null;

    this.medecinService.getRdvAujourdHui()
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isLoadingQueue = false;
        })
      )
      .subscribe({
        next: (rdvs) => {
          this.fileAttente = (rdvs || []).filter(r => r.statut === 'confirme');
        },
        error: (err) => {
          console.error('Erreur chargement file attente:', err);
          this.queueError = err?.error?.message || 'Impossible de charger la file d\'attente';
          this.fileAttente = [];
        }
      });
  }

  formatTime(dateStr: string): string {
    return formatTime(dateStr);
  }

  getOrigineLabel(rdv: RendezVousMedecinDto): string {
    if (rdv.origine === 'rdv_confirme') {
      return 'RDV confirmé - En attente d\'arrivée';
    }
    
    if (rdv.heureArrivee) {
      const d = new Date(rdv.heureArrivee);
      const timeLabel = d.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
      return `Arrivé à ${timeLabel}`;
    }

    if (rdv.dateModification) {
      const d = new Date(rdv.dateModification);
      const timeLabel = d.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
      return `Enregistré à ${timeLabel}`;
    }

    return 'En attente';
  }

  isRdvConfirme(rdv: RendezVousMedecinDto): boolean {
    return rdv.origine === 'rdv_confirme';
  }

  // Ancien flux questionnaire (à garder pour compatibilité)
  startConsultationQuestionnaire(rdv: RendezVousMedecinDto): void {
    console.log('startConsultationQuestionnaire called with rdv:', rdv);
    if (!rdv.idConsultation || rdv.idConsultation <= 0) {
      console.error('idConsultation missing or invalid:', rdv.idConsultation);
      alert(`Consultation non initialisée (id_consultation: ${rdv.idConsultation})`);
      return;
    }
    this.selectedRdvForQuestionnaire = rdv;
    this.showQuestionnaire = true;
  }

  closeQuestionnaire(): void {
    this.showQuestionnaire = false;
    this.selectedRdvForQuestionnaire = null;
  }

  onQuestionnaireSaved(): void {
    this.loadFileAttente();
  }

  onSlotClick(slot: any): void {
    console.log('Slot clicked:', slot);
    // TODO: Ouvrir le détail du créneau ou RDV
  }

  onRdvClick(rdv: any): void {
    console.log('RDV clicked:', rdv);
    // TODO: Naviguer vers le détail du RDV
  }

  // ==================== ACTIONS RDV EN ATTENTE ====================

  validerRdv(rdv: RdvPlanningDto): void {
    if (this.isProcessing) return;
    this.isProcessing = true;

    this.medecinPlanningService.validerRdv(rdv.idRendezVous)
      .pipe(finalize(() => this.isProcessing = false))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.loadRdvEnAttente();
            this.loadFileAttente();
            this.loadStats();
          } else if (response.conflitDetecte) {
            alert(response.message);
          } else {
            alert(response.message);
          }
        },
        error: (err) => {
          console.error('Erreur validation RDV:', err);
          alert(err.error?.message || 'Erreur lors de la validation');
        }
      });
  }

  openAnnulerModal(rdv: RdvPlanningDto): void {
    this.selectedRdvForAction = rdv;
    this.motifAnnulation = '';
    this.showAnnulerModal = true;
  }

  closeAnnulerModal(): void {
    this.showAnnulerModal = false;
    this.selectedRdvForAction = null;
    this.motifAnnulation = '';
  }

  confirmAnnuler(): void {
    if (!this.selectedRdvForAction || !this.motifAnnulation.trim()) return;
    if (this.isProcessing) return;
    this.isProcessing = true;

    this.medecinPlanningService.annulerRdvMedecin(this.selectedRdvForAction.idRendezVous, this.motifAnnulation)
      .pipe(finalize(() => this.isProcessing = false))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.closeAnnulerModal();
            this.loadRdvEnAttente();
            this.loadStats();
          } else {
            alert(response.message);
          }
        },
        error: (err) => {
          console.error('Erreur annulation RDV:', err);
          alert(err.error?.message || 'Erreur lors de l\'annulation');
        }
      });
  }

  openSuggererModal(rdv: RdvPlanningDto): void {
    this.selectedRdvForAction = rdv;
    this.nouveauCreneau = '';
    this.messageSuggestion = '';
    this.showSuggererModal = true;
  }

  closeSuggererModal(): void {
    this.showSuggererModal = false;
    this.selectedRdvForAction = null;
    this.nouveauCreneau = '';
    this.messageSuggestion = '';
  }

  confirmSuggerer(): void {
    if (!this.selectedRdvForAction || !this.nouveauCreneau) return;
    if (this.isProcessing) return;
    this.isProcessing = true;

    this.medecinPlanningService.suggererCreneau(
      this.selectedRdvForAction.idRendezVous,
      this.nouveauCreneau,
      this.messageSuggestion
    )
      .pipe(finalize(() => this.isProcessing = false))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.closeSuggererModal();
            this.loadRdvEnAttente();
            alert('Nouveau créneau proposé avec succès. Le patient a été notifié par email.');
          } else if (response.conflitDetecte) {
            alert(response.message);
          } else {
            alert(response.message);
          }
        },
        error: (err) => {
          console.error('Erreur suggestion créneau:', err);
          alert(err.error?.message || 'Erreur lors de la suggestion');
        }
      });
  }

  formatDate(dateStr: string): string {
    return formatDateWithWeekday(dateStr);
  }

  // ==================== NAVIGATION DOSSIER PATIENT ====================

  openFichePatient(patientId: number): void {
    // Navigation vers la page dédiée du dossier patient
    this.router.navigate(['/medecin/patient', patientId]);
  }

  openFichePatientForConsultation(rdv: RendezVousMedecinDto): void {
    // Navigation vers la page dossier patient avec l'ID de consultation
    if (rdv.idConsultation && rdv.idConsultation > 0) {
      this.router.navigate(['/medecin/patient', rdv.patientId], {
        queryParams: { consultationId: rdv.idConsultation }
      });
    } else {
      this.router.navigate(['/medecin/patient', rdv.patientId]);
    }
  }
}
