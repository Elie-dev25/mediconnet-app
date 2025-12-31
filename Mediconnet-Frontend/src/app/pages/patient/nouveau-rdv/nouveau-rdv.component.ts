import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { DashboardLayoutComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { PATIENT_MENU_ITEMS, PATIENT_SIDEBAR_TITLE } from '../shared';
import { 
  RendezVousService, 
  MedecinDisponibleDto,
  CreneauDisponibleDto,
  CreateRendezVousRequest,
  ServiceDto,
  RendezVousDto
} from '../../../services/rendez-vous.service';

@Component({
  selector: 'app-patient-nouveau-rdv',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    LucideAngularModule,
    DashboardLayoutComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './nouveau-rdv.component.html',
  styleUrl: './nouveau-rdv.component.scss'
})
export class PatientNouveauRdvComponent implements OnInit {
  menuItems = PATIENT_MENU_ITEMS;
  sidebarTitle = PATIENT_SIDEBAR_TITLE;

  // Stepper
  currentStep = 1;
  
  // Services et médecins
  services: ServiceDto[] = [];
  selectedServiceId: number | null = null;
  medecins: MedecinDisponibleDto[] = [];
  filteredMedecins: MedecinDisponibleDto[] = [];
  selectedMedecin: MedecinDisponibleDto | null = null;
  
  // Créneaux
  creneaux: CreneauDisponibleDto[] = [];
  selectedCreneau: CreneauDisponibleDto | null = null;
  
  // Calendrier
  currentMonth = new Date();
  selectedDate: Date | null = null;
  calendarDays: { date: Date; isCurrentMonth: boolean; hasSlots: boolean }[] = [];
  
  // Disponibilité médecin
  medecinDisponible = true;
  messageIndisponibilite = '';
  
  // Formulaire
  rdvForm!: FormGroup;
  isSubmitting = false;
  error = '';
  
  // Confirmation
  showConfirmationPopup = false;
  confirmedRdv: RendezVousDto | null = null;

  constructor(
    private rdvService: RendezVousService,
    private fb: FormBuilder,
    private router: Router
  ) {
    this.initForm();
  }

  ngOnInit(): void {
    this.loadServices();
    this.loadMedecins();
    this.generateCalendarDays();
  }

  private initForm(): void {
    this.rdvForm = this.fb.group({
      motif: ['', Validators.maxLength(100)],
      notes: ['', Validators.maxLength(500)],
      typeRdv: ['consultation']
    });
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
    
    const startDate = new Date(firstDay);
    startDate.setDate(startDate.getDate() - (startDate.getDay() === 0 ? 6 : startDate.getDay() - 1));
    
    const endDate = new Date(lastDay);
    endDate.setDate(endDate.getDate() + (7 - (endDate.getDay() === 0 ? 7 : endDate.getDay())));

    this.calendarDays = [];
    const current = new Date(startDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    while (current <= endDate) {
      const currentDate = new Date(current);
      currentDate.setHours(0, 0, 0, 0);
      
      this.calendarDays.push({
        date: new Date(current),
        isCurrentMonth: current.getMonth() === month,
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
    dateFin.setDate(dateFin.getDate() + 14);
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

  // ==================== NAVIGATION ====================

  goBack(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
      if (this.currentStep === 1) {
        this.selectedMedecin = null;
      }
    } else {
      this.router.navigate(['/patient/rendez-vous']);
    }
  }

  cancel(): void {
    this.router.navigate(['/patient/rendez-vous']);
  }

  // ==================== SOUMISSION ====================

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
        this.showConfirmationPopup = true;
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
    this.router.navigate(['/patient/rendez-vous']);
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

  get monthName(): string {
    return this.currentMonth.toLocaleDateString('fr-FR', { month: 'long', year: 'numeric' });
  }
}
