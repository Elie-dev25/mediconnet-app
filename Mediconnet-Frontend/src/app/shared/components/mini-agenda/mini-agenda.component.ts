import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { Subject, takeUntil } from 'rxjs';
import { MedecinService, AgendaJourDto, AgendaSlotDto, RendezVousMedecinDto } from '../../../services/medecin.service';
import { SignalRService } from '../../../services/signalr.service';

@Component({
  selector: 'app-mini-agenda',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './mini-agenda.component.html',
  styleUrl: './mini-agenda.component.scss'
})
export class MiniAgendaComponent implements OnInit, OnDestroy {
  @Input() nombreJours = 7;
  @Input() showLegend = true;
  @Output() slotClick = new EventEmitter<AgendaSlotDto>();
  @Output() rdvClick = new EventEmitter<RendezVousMedecinDto>();

  private destroy$ = new Subject<void>();

  jours: AgendaJourDto[] = [];
  rdvPeriode: RendezVousMedecinDto[] = [];
  prochainRdv: RendezVousMedecinDto | null = null;
  
  isLoading = true;
  error: string | null = null;
  
  selectedDate: Date = new Date();
  currentWeekStart: Date = this.getWeekStartStatic(new Date());

  // Méthode statique pour initialiser currentWeekStart
  private getWeekStartStatic(date: Date): Date {
    const d = new Date(date);
    const dayOfWeek = d.getDay();
    const diff = dayOfWeek === 0 ? -6 : 1 - dayOfWeek;
    d.setDate(d.getDate() + diff);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  constructor(
    private medecinService: MedecinService,
    private signalRService: SignalRService
  ) {}

  ngOnInit(): void {
    this.loadAgenda();
    this.subscribeToUpdates();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private getWeekStart(date: Date): Date {
    const d = new Date(date);
    const dayOfWeek = d.getDay();
    const diff = dayOfWeek === 0 ? -6 : 1 - dayOfWeek;
    d.setDate(d.getDate() + diff);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  private subscribeToUpdates(): void {
    // S'abonner aux mises à jour temps réel
    this.signalRService.appointmentEvents$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.loadAgenda();
      });

    this.signalRService.slotEvents$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.loadAgenda();
      });
  }

  loadAgenda(): void {
    this.isLoading = true;
    this.error = null;

    const dateDebut = this.formatDate(this.currentWeekStart);
    const dateFin = this.formatDate(this.addDays(this.currentWeekStart, this.nombreJours - 1));

    this.medecinService.getAgenda(dateDebut, dateFin)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.jours = response.jours;
          this.extractRdvFromAgenda();
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Erreur chargement agenda:', err);
          this.error = 'Impossible de charger l\'agenda';
          this.isLoading = false;
        }
      });
  }

  private extractRdvFromAgenda(): void {
    // Extraire les RDV de la période à partir des slots occupés
    const rdvMap = new Map<number, RendezVousMedecinDto>(); // Éviter les doublons par idRendezVous
    const now = new Date();
    
    for (const jour of this.jours) {
      for (const slot of jour.slots) {
        if (slot.statut === 'occupe' && slot.idRendezVous && !rdvMap.has(slot.idRendezVous)) {
          rdvMap.set(slot.idRendezVous, {
            idConsultation: 0,
            idRendezVous: slot.idRendezVous,
            dateHeure: slot.dateHeure,
            duree: slot.duree,
            statut: 'confirme',
            motif: slot.motif,
            typeRdv: '',
            patientNom: slot.patientNom || '',
            patientPrenom: slot.patientPrenom || '',
            patientId: 0
          });
        }
      }
    }
    
    this.rdvPeriode = Array.from(rdvMap.values()).sort((a, b) => 
      new Date(a.dateHeure).getTime() - new Date(b.dateHeure).getTime()
    );
    
    // Trouver le prochain RDV (premier non passé)
    this.prochainRdv = this.rdvPeriode.find(r => new Date(r.dateHeure) > now) || null;
  }

  previousWeek(): void {
    const newDate = new Date(this.currentWeekStart);
    newDate.setDate(newDate.getDate() - 7);
    this.currentWeekStart = this.getWeekStart(newDate);
    this.loadAgenda();
  }

  nextWeek(): void {
    const newDate = new Date(this.currentWeekStart);
    newDate.setDate(newDate.getDate() + 7);
    this.currentWeekStart = this.getWeekStart(newDate);
    this.loadAgenda();
  }

  goToToday(): void {
    this.currentWeekStart = this.getWeekStart(new Date());
    this.loadAgenda();
  }

  isCurrentWeek(): boolean {
    const today = new Date();
    const todayWeekStart = this.getWeekStart(today);
    return this.currentWeekStart.getTime() === todayWeekStart.getTime();
  }

  onSlotClick(slot: AgendaSlotDto): void {
    if (slot.statut !== 'passe') {
      this.slotClick.emit(slot);
    }
  }

  onRdvClick(rdv: RendezVousMedecinDto): void {
    this.rdvClick.emit(rdv);
  }

  getSlotClass(statut: string): string {
    switch (statut) {
      case 'disponible': return 'slot-disponible';
      case 'occupe': return 'slot-occupe';
      case 'indisponible': return 'slot-indisponible';
      case 'passe': return 'slot-passe';
      default: return '';
    }
  }

  getSlotIcon(statut: string): string {
    switch (statut) {
      case 'disponible': return 'check-circle';
      case 'occupe': return 'user';
      case 'indisponible': return 'x-circle';
      case 'passe': return 'clock';
      default: return 'circle';
    }
  }

  formatTime(dateHeure: string): string {
    return new Date(dateHeure).toLocaleTimeString('fr-FR', { 
      hour: '2-digit', 
      minute: '2-digit' 
    });
  }

  formatDayName(date: string): string {
    return new Date(date).toLocaleDateString('fr-FR', { weekday: 'short' });
  }

  formatDayNumber(date: string): string {
    return new Date(date).getDate().toString();
  }

  isToday(date: string): boolean {
    const d = new Date(date);
    const today = new Date();
    return d.toDateString() === today.toDateString();
  }

  getWeekRange(): string {
    const start = this.currentWeekStart;
    const end = this.addDays(start, this.nombreJours - 1);
    return `${start.toLocaleDateString('fr-FR', { day: 'numeric', month: 'short' })} - ${end.toLocaleDateString('fr-FR', { day: 'numeric', month: 'short', year: 'numeric' })}`;
  }

  private formatDate(date: Date): string {
    // Utiliser le format local pour éviter les décalages UTC
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private addDays(date: Date, days: number): Date {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
  }

  trackByDate(index: number, jour: AgendaJourDto): string {
    return jour.date;
  }

  trackBySlot(index: number, slot: AgendaSlotDto): string {
    return slot.dateHeure;
  }
}
