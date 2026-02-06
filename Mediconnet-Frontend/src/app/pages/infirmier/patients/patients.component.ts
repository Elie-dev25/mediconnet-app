import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DashboardLayoutComponent, LucideAngularModule, ALL_ICONS_PROVIDER, PatientSearchComponent, WelcomeBannerComponent, AttribuerLitPanelComponent, HospitalisationDetailsPanelComponent } from '../../../shared';
import { INFIRMIER_MENU_ITEMS, INFIRMIER_SIDEBAR_TITLE } from '../shared/infirmier-menu.config';
import { PatientService, PatientBasicInfo } from '../../../services/patient.service';
import { HospitalisationService } from '../../../services/hospitalisation.service';
import { AuthService } from '../../../services/auth.service';

type TabType = 'tous' | 'hospitalises';

interface SoinHospitalisation {
  idSoin: number;
  typeSoin: string;
  description: string;
  frequence?: string;
  priorite: string;
  statut: string;
}

interface HospitalisationEnAttente {
  idAdmission: number;
  statut: string;
  urgence: string;
  dateEntree: string;
  dateSortiePrevue?: string;
  motif: string;
  diagnosticPrincipal?: string;
  soins?: SoinHospitalisation[];
  patient: {
    idPatient: number;
    nom: string;
    prenom: string;
    numeroDossier?: string;
  };
  medecin: {
    idMedecin: number;
    nom: string;
    prenom: string;
  };
  lit?: {
    idLit: number;
    numero: string;
    chambre: string;
    standard?: string;
  };
  service?: string;
}

@Component({
  selector: 'app-infirmier-patients',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LucideAngularModule,
    DashboardLayoutComponent,
    PatientSearchComponent,
    WelcomeBannerComponent,
    AttribuerLitPanelComponent,
    HospitalisationDetailsPanelComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './patients.component.html',
  styleUrls: ['./patients.component.scss']
})
export class InfirmierPatientsComponent implements OnInit {
  menuItems = INFIRMIER_MENU_ITEMS;
  sidebarTitle = INFIRMIER_SIDEBAR_TITLE;
  userName = '';
  
  // Onglets
  activeTab: TabType = 'tous';
  
  // Tous les patients
  patients: PatientBasicInfo[] = [];
  filteredPatients: PatientBasicInfo[] = [];
  isLoading = false;
  errorMessage: string | null = null;
  searchPerformed = false;

  // Patients hospitalisés
  hospitalisations: HospitalisationEnAttente[] = [];
  filteredHospitalisations: HospitalisationEnAttente[] = [];
  isLoadingHospitalisations = false;
  hospitalisationsError: string | null = null;
  isMajor = false;
  enAttenteLitCount = 0;
  enCoursCount = 0;
  hospitalisationSearchTerm = '';
  private searchTimeout: any;

  // Attribution de lit (Major)
  showAttribuerLitModal = false;
  selectedHospitalisation: HospitalisationEnAttente | null = null;

  // Panel de détails d'hospitalisation
  showDetailsPanel = false;
  selectedHospitalisationId: number | null = null;

  // Getter pour transformer l'hospitalisation sélectionnée au format attendu par le panel
  get selectedHospitalisationInfo() {
    if (!this.selectedHospitalisation) return null;
    return {
      idAdmission: this.selectedHospitalisation.idAdmission,
      patientNom: this.selectedHospitalisation.patient.nom,
      patientPrenom: this.selectedHospitalisation.patient.prenom,
      motif: this.selectedHospitalisation.motif,
      urgence: this.selectedHospitalisation.urgence,
      dateEntree: this.selectedHospitalisation.dateEntree
    };
  }

  constructor(
    private router: Router,
    private patientService: PatientService,
    private hospitalisationService: HospitalisationService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.loadRecentPatients();
  }

  loadUserInfo(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.userName = user.prenom || user.nom || '';
    }
  }

  // Gestion des onglets
  setActiveTab(tab: TabType): void {
    this.activeTab = tab;
    if (tab === 'hospitalises' && this.hospitalisations.length === 0) {
      this.loadHospitalisations();
    }
  }

  loadRecentPatients(): void {
    this.isLoading = true;
    this.errorMessage = null;
    this.searchPerformed = false;

    this.patientService.getRecentPatients(6).subscribe({
      next: (response) => {
        if (response.success) {
          this.patients = response.patients;
          this.filteredPatients = response.patients;
        } else {
          this.errorMessage = response.message;
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement patients récents:', err);
        this.errorMessage = 'Impossible de charger les patients récents';
        this.isLoading = false;
      }
    });
  }

  // Charger les patients hospitalisés
  loadHospitalisations(search?: string): void {
    this.isLoadingHospitalisations = true;
    this.hospitalisationsError = null;

    this.hospitalisationService.getPatientsHospitalises(search).subscribe({
      next: (response) => {
        console.log('Hospitalisations response:', response);
        if (response.success) {
          this.hospitalisations = response.data;
          this.filteredHospitalisations = response.data;
          this.isMajor = response.isMajor;
          this.enAttenteLitCount = response.enAttente || 0;
          this.enCoursCount = response.enCours || 0;
          console.log('Hospitalisations loaded:', this.hospitalisations);
        } else {
          this.hospitalisationsError = 'Erreur lors du chargement';
        }
        this.isLoadingHospitalisations = false;
      },
      error: (err) => {
        console.error('Erreur chargement hospitalisations:', err);
        this.hospitalisationsError = 'Impossible de charger les hospitalisations';
        this.isLoadingHospitalisations = false;
      }
    });
  }

  // Recherche réactive avec debounce
  onHospitalisationSearch(): void {
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }
    this.searchTimeout = setTimeout(() => {
      this.loadHospitalisations(this.hospitalisationSearchTerm);
    }, 300);
  }

  clearHospitalisationSearch(): void {
    this.hospitalisationSearchTerm = '';
    this.loadHospitalisations();
  }

  // Ouvrir le panel d'attribution de lit (Major)
  openAttribuerLitModal(hospitalisation: HospitalisationEnAttente): void {
    this.selectedHospitalisation = hospitalisation;
    this.showAttribuerLitModal = true;
  }

  closeAttribuerLitModal(): void {
    this.showAttribuerLitModal = false;
    this.selectedHospitalisation = null;
  }

  // Callback quand un lit est attribué via le panel partagé
  onLitAttributed(): void {
    this.closeAttribuerLitModal();
    this.loadHospitalisations();
  }

  onPatientSelected(patient: PatientBasicInfo): void {
    this.viewPatientDetails(patient);
  }

  viewPatientDetails(patient: PatientBasicInfo): void {
    console.log('Voir détails patient:', patient);
  }

  takeVitals(patient: PatientBasicInfo): void {
    this.router.navigate(['/infirmier/prise-parametres', patient.idUser]);
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { day: '2-digit', month: 'long', year: 'numeric' });
  }

  formatDateTime(dateStr?: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  calculateAge(dateStr?: string): number | null {
    if (!dateStr) return null;
    const birthDate = new Date(dateStr);
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    return age;
  }

  getUrgenceClass(urgence: string): string {
    switch (urgence) {
      case 'critique': return 'urgence-critique';
      case 'urgente': return 'urgence-urgente';
      default: return 'urgence-normale';
    }
  }

  getStatutLabel(statut: string): string {
    switch (statut) {
      case 'EN_ATTENTE': return 'En attente de lit';
      case 'EN_COURS': return 'Hospitalisé';
      case 'TERMINE': return 'Terminé';
      default: return statut;
    }
  }

  // Panel de détails d'hospitalisation
  openDetailsPanel(hospitalisation: HospitalisationEnAttente): void {
    this.selectedHospitalisationId = hospitalisation.idAdmission;
    this.selectedHospitalisation = hospitalisation;
    this.showDetailsPanel = true;
  }

  closeDetailsPanel(): void {
    this.showDetailsPanel = false;
    this.selectedHospitalisationId = null;
  }

  onOpenAttribuerLitFromPanel(hospitalisation: any): void {
    this.closeDetailsPanel();
    this.selectedHospitalisation = this.hospitalisations.find(h => h.idAdmission === hospitalisation.idAdmission) || null;
    if (this.selectedHospitalisation) {
      this.showAttribuerLitModal = true;
    }
  }
}