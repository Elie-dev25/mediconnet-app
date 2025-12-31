import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DashboardLayoutComponent, FichePatientPanelComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { MEDECIN_MENU_ITEMS, MEDECIN_SIDEBAR_TITLE } from '../shared';
import { 
  MedecinDataService, 
  MedecinPatientDto, 
  MedecinPatientStatsDto 
} from '../../../services/medecin-data.service';
import { DossierAccessService } from '../../../services/dossier-access.service';

@Component({
  selector: 'app-medecin-patients',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LucideAngularModule,
    DashboardLayoutComponent,
    FichePatientPanelComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './patients.component.html',
  styleUrl: './patients.component.scss'
})
export class MedecinPatientsComponent implements OnInit {
  // Menu partagé
  menuItems = MEDECIN_MENU_ITEMS;
  sidebarTitle = MEDECIN_SIDEBAR_TITLE;

  // État
  isLoading = true;
  searchTerm = '';

  // Données
  patients: MedecinPatientDto[] = [];
  filteredPatients: MedecinPatientDto[] = [];
  stats: MedecinPatientStatsDto | null = null;

  // Détail patient
  selectedPatientId: number | null = null;
  isDetailOpen = false;

  // Validation code email pour accès dossier
  showCodeModal = false;
  pendingPatient: MedecinPatientDto | null = null;
  codeSent = false;
  validationCode = '';
  codeError: string | null = null;
  isSendingCode = false;
  isVerifyingCode = false;

  constructor(
    private medecinDataService: MedecinDataService,
    private dossierAccessService: DossierAccessService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadStats();
    this.loadPatients();
  }

  loadStats(): void {
    this.medecinDataService.getPatientStats().subscribe({
      next: (stats) => this.stats = stats,
      error: (err) => console.error('Erreur stats:', err)
    });
  }

  loadPatients(): void {
    this.isLoading = true;
    this.medecinDataService.getPatients(this.searchTerm || undefined).subscribe({
      next: (patients) => {
        this.patients = patients;
        this.filteredPatients = patients;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur:', err);
        this.isLoading = false;
      }
    });
  }

  onSearch(): void {
    if (this.searchTerm.length >= 2 || this.searchTerm.length === 0) {
      this.loadPatients();
    }
  }

  openPatientDetail(patient: MedecinPatientDto): void {
    this.selectedPatientId = patient.idPatient;
    this.isDetailOpen = true;
  }

  closeDetail(): void {
    this.isDetailOpen = false;
    this.selectedPatientId = null;
  }

  // Helpers
  formatDate(dateStr: string | undefined): string {
    if (!dateStr) return '-';
    return this.medecinDataService.formatDate(dateStr);
  }

  formatDateTime(dateStr: string): string {
    return this.medecinDataService.formatDateTime(dateStr);
  }

  getAge(naissance: string | undefined): string {
    if (!naissance) return '-';
    const birthDate = new Date(naissance);
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const m = today.getMonth() - birthDate.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    return `${age} ans`;
  }

  getSexeLabel(sexe: string | undefined): string {
    if (!sexe) return '-';
    return sexe === 'M' ? 'Homme' : sexe === 'F' ? 'Femme' : sexe;
  }

  getRelativeTime(dateStr: string | undefined): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    const now = new Date();
    const diffDays = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24));
    
    if (diffDays === 0) return "Aujourd'hui";
    if (diffDays === 1) return 'Hier';
    if (diffDays < 7) return `Il y a ${diffDays} jours`;
    if (diffDays < 30) return `Il y a ${Math.floor(diffDays / 7)} semaine(s)`;
    if (diffDays < 365) return `Il y a ${Math.floor(diffDays / 30)} mois`;
    return `Il y a ${Math.floor(diffDays / 365)} an(s)`;
  }

  getFutureTime(dateStr: string | undefined): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    const now = new Date();
    const diffDays = Math.floor((date.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
    
    if (diffDays === 0) return "Aujourd'hui";
    if (diffDays === 1) return 'Demain';
    if (diffDays < 7) return `Dans ${diffDays} jours`;
    return this.formatDate(dateStr);
  }

  refresh(): void {
    this.loadStats();
    this.loadPatients();
  }

  // ==================== ACCÈS DOSSIER PATIENT ====================

  requestDossierAccess(patient: MedecinPatientDto, event: Event): void {
    event.stopPropagation();
    this.pendingPatient = patient;
    this.showCodeModal = true;
    this.codeSent = false;
    this.validationCode = '';
    this.codeError = null;
  }

  closeCodeModal(): void {
    this.showCodeModal = false;
    this.pendingPatient = null;
    this.codeSent = false;
    this.validationCode = '';
    this.codeError = null;
  }

  sendValidationCode(): void {
    if (!this.pendingPatient) return;

    this.isSendingCode = true;
    this.codeError = null;

    this.dossierAccessService.sendValidationCode(this.pendingPatient.idPatient).subscribe({
      next: (response: { success: boolean; message: string }) => {
        if (response.success) {
          this.codeSent = true;
        } else {
          this.codeError = response.message;
        }
        this.isSendingCode = false;
      },
      error: (err: any) => {
        console.error('Erreur envoi code:', err);
        this.codeError = err.error?.message || 'Erreur lors de l\'envoi du code';
        this.isSendingCode = false;
      }
    });
  }

  verifyCode(): void {
    if (!this.pendingPatient || this.validationCode.length !== 5) return;

    this.isVerifyingCode = true;
    this.codeError = null;
    const patientId = this.pendingPatient.idPatient; // Sauvegarder l'ID avant de fermer

    this.dossierAccessService.verifyCode(patientId, this.validationCode).subscribe({
      next: (response: { success: boolean; message: string }) => {
        if (response.success) {
          // Accès autorisé - fermer la modale et naviguer vers le dossier patient
          this.closeCodeModal();
          this.router.navigate(['/medecin/patient', patientId]);
        } else {
          this.codeError = response.message || 'Code invalide';
        }
        this.isVerifyingCode = false;
      },
      error: (err: any) => {
        console.error('Erreur vérification code:', err);
        this.codeError = err.error?.message || 'Code invalide ou expiré';
        this.isVerifyingCode = false;
      }
    });
  }
}
