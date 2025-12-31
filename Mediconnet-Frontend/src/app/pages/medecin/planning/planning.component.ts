import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { DashboardLayoutComponent, ModalComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { MEDECIN_MENU_ITEMS, MEDECIN_SIDEBAR_TITLE } from '../shared';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import {
  MedecinPlanningService,
  SemaineTypeDto,
  SemainePlanningDto,
  IndisponibiliteDto,
  PlanningDashboardDto,
  JourneeCalendrierDto,
  CreateCreneauRequest,
  CreateIndisponibiliteRequest
} from '../../../services/medecin-planning.service';

@Component({
  selector: 'app-medecin-planning',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    LucideAngularModule,
    DashboardLayoutComponent,
    ModalComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './planning.component.html',
  styleUrl: './planning.component.scss'
})
export class MedecinPlanningComponent implements OnInit {
  // Menu partagé pour toutes les pages médecin
  menuItems = MEDECIN_MENU_ITEMS;
  sidebarTitle = MEDECIN_SIDEBAR_TITLE;

  // État
  activeTab: 'horaires' | 'conges' | 'calendrier' = 'horaires';
  isLoading = true;

  // Données
  dashboard: PlanningDashboardDto | null = null;
  semaineType: SemaineTypeDto | null = null;
  semainePlanning: SemainePlanningDto | null = null;
  indisponibilites: IndisponibiliteDto[] = [];

  calendrierSemaine: JourneeCalendrierDto[] = [];
  isLoadingCalendrier = false;

  // Navigation semaine pour tab Horaires
  horairesWeekStart: Date = this.getWeekStartStatic(new Date());
  isLoadingHoraires = false;

  // Méthode statique pour initialiser horairesWeekStart
  private getWeekStartStatic(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    d.setDate(diff);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  // Modal Créneau
  showCreneauModal = false;
  editingCreneau: any = null;
  creneauForm!: FormGroup;

  // Modal Indisponibilité
  showIndispoModal = false;
  indispoForm!: FormGroup;

  // Messages
  error = '';
  success = '';

  // Calendrier - utilise getWeekStartStatic défini plus haut
  currentWeekStart: Date = this.getWeekStartStatic(new Date());

  constructor(
    private planningService: MedecinPlanningService,
    private fb: FormBuilder
  ) {
    this.initForms();
  }

  ngOnInit(): void {
    this.loadData();
    this.loadSemainePlanning();
    this.loadCalendrierSemaine();
  }

  private initForms(): void {
    this.creneauForm = this.fb.group({
      jourSemaine: [1, Validators.required],
      heureDebut: ['09:00', [Validators.required, Validators.pattern(/^\d{2}:\d{2}$/)]],
      heureFin: ['12:00', [Validators.required, Validators.pattern(/^\d{2}:\d{2}$/)]],
      dureeParDefaut: [30, [Validators.min(10), Validators.max(120)]]
    });

    this.indispoForm = this.fb.group({
      dateDebut: ['', Validators.required],
      dateFin: ['', Validators.required],
      type: ['conge', Validators.required],
      motif: [''],
      journeeComplete: [true]
    });
  }

  loadData(): void {
    this.isLoading = true;

    forkJoin({
      dashboard: this.planningService.getDashboard(),
      semaineType: this.planningService.getSemaineType()
    }).pipe(
      finalize(() => {
        this.isLoading = false;
      })
    ).subscribe({
      next: ({ dashboard, semaineType }) => {
        this.dashboard = dashboard;
        this.semaineType = semaineType;
      },
      error: (err) => {
        console.error('Erreur chargement planning:', err);
      }
    });

    this.loadIndisponibilites();
  }

  loadIndisponibilites(): void {
    this.planningService.getIndisponibilites().subscribe({
      next: (data) => this.indisponibilites = data,
      error: (err) => console.error('Erreur indisponibilités:', err)
    });
  }

  loadSemainePlanning(date?: Date): void {
    this.isLoadingHoraires = true;
    const d = date || this.horairesWeekStart;
    const isoDate = this.toLocalDateTimeString(d);

    this.planningService.getSemainePlanning(isoDate).pipe(
      finalize(() => this.isLoadingHoraires = false)
    ).subscribe({
      next: (data) => {
        this.semainePlanning = data;
      },
      error: (err) => {
        console.error('Erreur chargement semaine planning:', err);
      }
    });
  }

  previousHorairesWeek(): void {
    const d = new Date(this.horairesWeekStart);
    d.setDate(d.getDate() - 7);
    this.horairesWeekStart = this.getWeekStart(d);
    this.loadSemainePlanning(this.horairesWeekStart);
  }

  nextHorairesWeek(): void {
    const d = new Date(this.horairesWeekStart);
    d.setDate(d.getDate() + 7);
    this.horairesWeekStart = this.getWeekStart(d);
    this.loadSemainePlanning(this.horairesWeekStart);
  }

  goToCurrentWeek(): void {
    this.horairesWeekStart = this.getWeekStart(new Date());
    this.loadSemainePlanning(this.horairesWeekStart);
  }

  getHorairesWeekLabel(): string {
    // Utiliser horairesWeekStart comme source de vérité pour le label
    const start = this.horairesWeekStart;
    const end = new Date(start);
    end.setDate(start.getDate() + 6);
    const startStr = start.toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' });
    const endStr = end.toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' });
    return `Du ${startStr} au ${endStr}`;
  }

  setActiveTab(tab: 'horaires' | 'conges' | 'calendrier'): void {
    this.activeTab = tab;

    if (tab === 'calendrier' && this.calendrierSemaine.length === 0) {
      this.loadCalendrierSemaine();
    }
  }

  loadCalendrierSemaine(date?: Date): void {
    this.isLoadingCalendrier = true;

    const d = date || this.currentWeekStart;
    const isoDate = this.toLocalDateTimeString(d);

    this.planningService.getCalendrierSemaine(isoDate).pipe(
      finalize(() => {
        this.isLoadingCalendrier = false;
      })
    ).subscribe({
      next: (data) => {
        this.calendrierSemaine = data;
      },
      error: (err) => {
        console.error('Erreur calendrier semaine:', err);
      }
    });
  }

  previousWeek(): void {
    const d = new Date(this.currentWeekStart);
    d.setDate(d.getDate() - 7);
    this.currentWeekStart = this.getWeekStart(d);
    this.loadCalendrierSemaine(this.currentWeekStart);
  }

  nextWeek(): void {
    const d = new Date(this.currentWeekStart);
    d.setDate(d.getDate() + 7);
    this.currentWeekStart = this.getWeekStart(d);
    this.loadCalendrierSemaine(this.currentWeekStart);
  }

  getWeekLabel(): string {
    const start = this.getWeekStart(this.currentWeekStart);
    const end = new Date(start);
    end.setDate(start.getDate() + 6);

    const startStr = start.toLocaleDateString('fr-FR', { day: '2-digit', month: 'long' });
    const endStr = end.toLocaleDateString('fr-FR', { day: '2-digit', month: 'long', year: 'numeric' });
    return `du ${startStr} au ${endStr}`;
  }

  private getWeekStart(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    d.setDate(diff);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  private toLocalDateTimeString(date: Date): string {
    const d = new Date(date);
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}T00:00:00`;
  }

  // ==================== CRÉNEAUX ====================

  openCreneauModal(jourSemaine?: number, creneau?: any): void {
    this.showCreneauModal = true;
    this.editingCreneau = creneau || null;
    this.error = '';

    if (creneau) {
      this.creneauForm.patchValue({
        jourSemaine: creneau.jourSemaine,
        heureDebut: creneau.heureDebut,
        heureFin: creneau.heureFin,
        dureeParDefaut: creneau.dureeParDefaut
      });
    } else {
      this.creneauForm.reset({
        jourSemaine: jourSemaine || 1,
        heureDebut: '09:00',
        heureFin: '12:00',
        dureeParDefaut: 30
      });
    }
  }

  closeCreneauModal(): void {
    this.showCreneauModal = false;
    this.editingCreneau = null;
  }

  saveCreneau(): void {
    if (this.creneauForm.invalid) return;

    const request: CreateCreneauRequest = this.creneauForm.value;

    if (this.editingCreneau) {
      this.planningService.updateCreneau(this.editingCreneau.idCreneau, request).subscribe({
        next: () => {
          this.closeCreneauModal();
          this.loadData();
        },
        error: (err) => this.error = err.error?.message || 'Erreur lors de la modification'
      });
    } else {
      this.planningService.createCreneau(request).subscribe({
        next: () => {
          this.closeCreneauModal();
          this.loadData();
        },
        error: (err) => this.error = err.error?.message || 'Erreur lors de la création'
      });
    }
  }

  deleteCreneau(id: number): void {
    if (!confirm('Supprimer ce créneau ?')) return;

    this.planningService.deleteCreneau(id).subscribe({
      next: () => this.loadData(),
      error: (err) => alert(err.error?.message || 'Erreur')
    });
  }

  toggleCreneau(id: number): void {
    this.planningService.toggleCreneau(id).subscribe({
      next: () => this.loadData(),
      error: (err) => alert(err.error?.message || 'Erreur')
    });
  }

  // ==================== INDISPONIBILITÉS ====================

  openIndispoModal(): void {
    this.showIndispoModal = true;
    this.error = '';
    
    const today = new Date().toISOString().split('T')[0];
    this.indispoForm.reset({
      dateDebut: today,
      dateFin: today,
      type: 'conge',
      motif: '',
      journeeComplete: true
    });
  }

  closeIndispoModal(): void {
    this.showIndispoModal = false;
  }

  saveIndisponibilite(): void {
    if (this.indispoForm.invalid) return;

    const formValue = this.indispoForm.value;
    const request: CreateIndisponibiliteRequest = {
      dateDebut: `${formValue.dateDebut}T00:00:00`,
      dateFin: `${formValue.dateFin}T00:00:00`,
      type: formValue.type,
      motif: formValue.motif || undefined,
      journeeComplete: formValue.journeeComplete
    };

    this.planningService.createIndisponibilite(request).subscribe({
      next: () => {
        this.closeIndispoModal();
        this.loadIndisponibilites();
      },
      error: (err) => this.error = err.error?.message || 'Erreur lors de la création'
    });
  }

  deleteIndisponibilite(id: number): void {
    if (!confirm('Supprimer cette indisponibilité ?')) return;

    this.planningService.deleteIndisponibilite(id).subscribe({
      next: () => this.loadIndisponibilites(),
      error: (err) => alert(err.error?.message || 'Erreur')
    });
  }

  // ==================== HELPERS ====================

  getJourNom(numero: number): string {
    return this.planningService.getJourNom(numero);
  }

  getTypeIndispoOptions() {
    return this.planningService.getTypeIndispoOptions();
  }

  formatDate(dateStr: string): string {
    return this.planningService.formatDate(dateStr);
  }

  formatTime(dateStr: string): string {
    return this.planningService.formatTime(dateStr);
  }

  refresh(): void {
    this.loadData();
  }
}
