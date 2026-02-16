import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { 
  DashboardLayoutComponent, 
  FichePatientPanelComponent, 
  LucideAngularModule, 
  ALL_ICONS_PROVIDER, 
  HospitalisationMultiEtapesComponent, 
  HospitalisationPatientInfo, 
  AttribuerLitPanelComponent, 
  HospitalisationEnAttenteInfo, 
  HospitalisationDetailsPanelComponent,
  OrdonnanceHospitalisationPanelComponent,
  ExamenHospitalisationPanelComponent,
  SoinHospitalisationPanelComponent,
  FinHospitalisationPanelComponent
} from '../../../shared';
import { AuthService } from '../../../services/auth.service';
import { MEDECIN_MENU_ITEMS, MEDECIN_SIDEBAR_TITLE } from '../shared';
import { 
  MedecinDataService, 
  MedecinPatientDto, 
  MedecinPatientStatsDto,
  PatientHospitaliseDto,
  HospitalisationDetailDto,
  ConsultationHospitalisationDto
} from '../../../services/medecin-data.service';
import { DossierAccessService } from '../../../services/dossier-access.service';

type TabType = 'tous' | 'hospitalises';

@Component({
  selector: 'app-medecin-patients',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LucideAngularModule,
    DashboardLayoutComponent,
    FichePatientPanelComponent,
    HospitalisationMultiEtapesComponent,
    AttribuerLitPanelComponent,
    HospitalisationDetailsPanelComponent,
    OrdonnanceHospitalisationPanelComponent,
    ExamenHospitalisationPanelComponent,
    SoinHospitalisationPanelComponent,
    FinHospitalisationPanelComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './patients.component.html',
  styleUrl: './patients.component.scss'
})
export class MedecinPatientsComponent implements OnInit {
  // Menu partagé
  menuItems = MEDECIN_MENU_ITEMS;
  sidebarTitle = MEDECIN_SIDEBAR_TITLE;

  // Onglets
  activeTab: TabType = 'tous';

  // État
  isLoading = true;
  isLoadingHospitalises = false;
  searchTerm = '';

  // Données - Tous les patients
  patients: MedecinPatientDto[] = [];
  filteredPatients: MedecinPatientDto[] = [];
  stats: MedecinPatientStatsDto | null = null;

  // Données - Patients hospitalisés
  patientsHospitalises: PatientHospitaliseDto[] = [];
  filteredPatientsHospitalises: PatientHospitaliseDto[] = [];

  // Détail patient (onglet tous)
  selectedPatientId: number | null = null;
  isDetailOpen = false;

  // Détail hospitalisation (onglet hospitalisés)
  selectedHospitalisation: HospitalisationDetailDto | null = null;
  selectedHospitalisationConsultations: ConsultationHospitalisationDto[] = [];
  isHospitalisationPanelOpen = false;
  isLoadingHospitalisationDetail = false;

  // Validation code email pour accès dossier
  showCodeModal = false;
  pendingPatient: MedecinPatientDto | null = null;
  codeSent = false;
  validationCode = '';
  codeError: string | null = null;
  isSendingCode = false;
  isVerifyingCode = false;

  // Panneau latéral Hospitalisation (nouveau composant multi-étapes)
  showHospitalisationPanel = false;
  hospitalisationPatientInfo: HospitalisationPatientInfo | null = null;

  // Panneau latéral Examen Hospitalisation
  showExamenHospPanel = false;
  examenHospitalisationId: number | null = null;
  examenPatientNom = '';
  examenPatientPrenom = '';

  // Panneau latéral Ordonnance Hospitalisation
  showOrdonnanceHospPanel = false;
  ordonnanceHospitalisationId: number | null = null;
  ordonnancePatientNom = '';
  ordonnancePatientPrenom = '';

  // Panneau latéral Soin Hospitalisation
  showSoinHospPanel = false;
  soinHospitalisationId: number | null = null;
  soinPatientNom = '';
  soinPatientPrenom = '';

  // Panneau attribution lit (Major)
  isMajor = false;
  showAttribuerLitPanel = false;
  selectedHospitalisationForLit: HospitalisationEnAttenteInfo | null = null;

  // Panel de détails d'hospitalisation (nouveau composant unifié)
  showDetailsPanel = false;
  selectedHospitalisationId: number | null = null;

  // Panneau fin d'hospitalisation
  showFinHospPanel = false;
  finHospitalisationId: number | null = null;
  finHospPatientNom = '';
  finHospPatientPrenom = '';

  // Confirmation avant fin d'hospitalisation
  showFinHospConfirm = false;
  finHospConfirmExamens = 0;
  finHospConfirmSoins = 0;
  pendingFinHospData: any = null;

  // Modal ajout de soins
  showSoinsModal = false;
  soinForm = {
    typeSoin: 'surveillance',
    description: '',
    frequence: '',
    duree: '',
    priorite: 'normale',
    instructions: ''
  };
  isAddingSoin = false;
  soinError: string | null = null;
  soinSuccess = false;

  constructor(
    private medecinDataService: MedecinDataService,
    private dossierAccessService: DossierAccessService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadStats();
    this.loadPatients();
  }

  // ==================== ONGLETS ====================

  switchTab(tab: TabType): void {
    this.activeTab = tab;
    this.searchTerm = '';
    if (tab === 'hospitalises' && this.patientsHospitalises.length === 0) {
      this.loadPatientsHospitalises();
    }
  }

  // ==================== TOUS LES PATIENTS ====================

  loadStats(): void {
    this.medecinDataService.getPatientStats().subscribe({
      next: (stats) => this.stats = stats,
      error: (err) => console.error('Erreur stats:', err)
    });
  }

  loadPatients(): void {
    this.isLoading = true;
    this.medecinDataService.getPatients(this.searchTerm || undefined).subscribe({
      next: (response) => {
        this.patients = response.data;
        this.filteredPatients = response.data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur:', err);
        this.isLoading = false;
      }
    });
  }

  onSearch(): void {
    if (this.activeTab === 'tous') {
      if (this.searchTerm.length >= 2 || this.searchTerm.length === 0) {
        this.loadPatients();
      }
    } else {
      this.filterPatientsHospitalises();
    }
  }

  // ==================== PATIENTS HOSPITALISÉS ====================

  loadPatientsHospitalises(): void {
    this.isLoadingHospitalises = true;
    this.medecinDataService.getPatientsHospitalises().subscribe({
      next: (response) => {
        this.patientsHospitalises = response.data;
        this.filteredPatientsHospitalises = response.data;
        this.isLoadingHospitalises = false;
      },
      error: (err) => {
        console.error('Erreur patients hospitalisés:', err);
        this.isLoadingHospitalises = false;
      }
    });
  }

  filterPatientsHospitalises(): void {
    if (!this.searchTerm) {
      this.filteredPatientsHospitalises = this.patientsHospitalises;
      return;
    }
    const term = this.searchTerm.toLowerCase();
    this.filteredPatientsHospitalises = this.patientsHospitalises.filter(p =>
      p.patientNom?.toLowerCase().includes(term) ||
      p.patientPrenom?.toLowerCase().includes(term) ||
      p.numeroDossier?.toLowerCase().includes(term) ||
      p.numeroChambre?.toLowerCase().includes(term) ||
      p.numeroLit?.toLowerCase().includes(term)
    );
  }

  openHospitalisationDetail(patient: PatientHospitaliseDto): void {
    // Utiliser le nouveau composant unifié
    this.selectedHospitalisationId = patient.idAdmission;
    this.showDetailsPanel = true;
  }

  closeHospitalisationDetailPanel(): void {
    this.isHospitalisationPanelOpen = false;
    this.selectedHospitalisation = null;
    this.selectedHospitalisationConsultations = [];
  }

  // Méthodes pour le nouveau panel de détails
  closeDetailsPanel(): void {
    this.showDetailsPanel = false;
    this.selectedHospitalisationId = null;
  }

  onOpenAttribuerLitFromPanel(hospitalisation: any): void {
    this.closeDetailsPanel();
    // Convertir au format attendu par le panel d'attribution de lit
    const hospitalisationForLit = {
      idAdmission: hospitalisation.idAdmission,
      patientNom: hospitalisation.patient?.nom || '',
      patientPrenom: hospitalisation.patient?.prenom || '',
      motif: hospitalisation.motif || '',
      urgence: hospitalisation.urgence || 'normale',
      dateEntree: hospitalisation.dateEntree
    };
    this.selectedHospitalisationForLit = hospitalisationForLit;
    this.showAttribuerLitPanel = true;
  }

  openExamenPanelFromDetails(hospitalisation: any): void {
    this.closeDetailsPanel();
    this.openExamenHospPanel(hospitalisation);
  }

  openOrdonnancePanelFromDetails(hospitalisation: any): void {
    this.closeDetailsPanel();
    this.openOrdonnanceHospPanel(hospitalisation);
  }

  terminerHospitalisation(hospitalisation: any): void {
    this.closeDetailsPanel();

    // Compter les examens et soins non terminés/annulés
    const examens = hospitalisation.examens || [];
    const soins = hospitalisation.soins || [];
    const examensRestants = examens.filter(
      (e: any) => e.statut !== 'termine' && e.statut !== 'annule'
    ).length;
    const soinsRestants = soins.filter(
      (s: any) => s.statut !== 'termine' && s.statut !== 'annule'
    ).length;

    if (examensRestants > 0 || soinsRestants > 0) {
      // Stocker les données et afficher la confirmation
      this.pendingFinHospData = hospitalisation;
      this.finHospConfirmExamens = examensRestants;
      this.finHospConfirmSoins = soinsRestants;
      this.showFinHospConfirm = true;
    } else {
      // Pas de soins/examens en attente, ouvrir directement le panneau
      this.openFinHospPanel(hospitalisation);
    }
  }

  confirmFinHospitalisation(): void {
    this.showFinHospConfirm = false;
    if (this.pendingFinHospData) {
      this.openFinHospPanel(this.pendingFinHospData);
      this.pendingFinHospData = null;
    }
  }

  cancelFinHospConfirm(): void {
    this.showFinHospConfirm = false;
    this.pendingFinHospData = null;
    this.finHospConfirmExamens = 0;
    this.finHospConfirmSoins = 0;
  }

  private openFinHospPanel(hospitalisation: any): void {
    this.finHospitalisationId = hospitalisation.idAdmission;
    this.finHospPatientNom = hospitalisation.patient?.nom || '';
    this.finHospPatientPrenom = hospitalisation.patient?.prenom || '';
    this.showFinHospPanel = true;
  }

  closeFinHospPanel(): void {
    this.showFinHospPanel = false;
    this.finHospitalisationId = null;
    this.finHospPatientNom = '';
    this.finHospPatientPrenom = '';
  }

  onFinHospCompleted(): void {
    this.closeFinHospPanel();
    this.loadPatientsHospitalises();
  }

  programmerSoins(): void {
    if (!this.selectedHospitalisation) return;
    this.showSoinsModal = true;
    this.soinError = null;
    this.soinSuccess = false;
    this.resetSoinForm();
  }

  resetSoinForm(): void {
    this.soinForm = {
      typeSoin: 'surveillance',
      description: '',
      frequence: '',
      duree: '',
      priorite: 'normale',
      instructions: ''
    };
  }

  closeSoinsModal(): void {
    this.showSoinsModal = false;
    this.soinError = null;
    this.soinSuccess = false;
    this.resetSoinForm();
  }

  submitSoin(): void {
    if (!this.selectedHospitalisation || !this.soinForm.description.trim()) {
      this.soinError = 'Veuillez remplir la description du soin';
      return;
    }

    this.isAddingSoin = true;
    this.soinError = null;

    this.medecinDataService.ajouterSoin(this.selectedHospitalisation.idAdmission, {
      typeSoin: this.soinForm.typeSoin,
      description: this.soinForm.description,
      frequence: this.soinForm.frequence || undefined,
      duree: this.soinForm.duree || undefined,
      priorite: this.soinForm.priorite,
      instructions: this.soinForm.instructions || undefined
    }).subscribe({
      next: (response) => {
        if (response.success) {
          this.soinSuccess = true;
          setTimeout(() => {
            this.closeSoinsModal();
            // Rafraîchir les détails de l'hospitalisation en forçant un rechargement
            if (this.selectedHospitalisationId) {
              const id = this.selectedHospitalisationId;
              this.selectedHospitalisationId = null;
              setTimeout(() => this.selectedHospitalisationId = id, 50);
            }
          }, 1500);
        } else {
          this.soinError = response.message || 'Erreur lors de l\'ajout du soin';
        }
        this.isAddingSoin = false;
      },
      error: (err) => {
        console.error('Erreur ajout soin:', err);
        this.soinError = err.error?.message || 'Erreur lors de l\'ajout du soin';
        this.isAddingSoin = false;
      }
    });
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
    if (this.activeTab === 'tous') {
      this.loadPatients();
    } else {
      this.loadPatientsHospitalises();
    }
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

  // ==================== OUVRIR FICHE PATIENT ====================

  openPatientFiche(patient: MedecinPatientDto, event: Event): void {
    event.stopPropagation();
    this.selectedPatientId = patient.idPatient;
    this.isDetailOpen = true;
  }

  // ==================== PANNEAU HOSPITALISATION (multi-étapes) ====================

  openHospitalisationPanel(patientId: number): void {
    const patient = this.patients.find(p => p.idPatient === patientId);
    if (patient) {
      this.hospitalisationPatientInfo = {
        idPatient: patient.idPatient,
        nom: patient.nom,
        prenom: patient.prenom,
        numeroDossier: patient.numeroDossier
      };
      this.showHospitalisationPanel = true;
      this.isDetailOpen = false;
    }
  }

  closeHospitalisationPanel(): void {
    this.showHospitalisationPanel = false;
    this.hospitalisationPatientInfo = null;
  }

  onHospitalisationCompleted(): void {
    this.closeHospitalisationPanel();
    this.loadPatientsHospitalises();
  }

  onHospitalisationCancelled(): void {
    this.closeHospitalisationPanel();
  }

  // ==================== PANNEAU EXAMEN HOSPITALISATION ====================

  openExamenHospPanel(hospitalisation: any): void {
    this.examenHospitalisationId = hospitalisation.idAdmission;
    this.examenPatientNom = hospitalisation.patient?.nom || '';
    this.examenPatientPrenom = hospitalisation.patient?.prenom || '';
    this.showExamenHospPanel = true;
    this.showDetailsPanel = false;
  }

  closeExamenHospPanel(): void {
    this.showExamenHospPanel = false;
    this.examenHospitalisationId = null;
    this.examenPatientNom = '';
    this.examenPatientPrenom = '';
  }

  onExamenHospSaved(): void {
    this.closeExamenHospPanel();
    // Rafraîchir les détails si le panneau était ouvert
    if (this.selectedHospitalisationId) {
      const id = this.selectedHospitalisationId;
      this.selectedHospitalisationId = null;
      setTimeout(() => {
        this.selectedHospitalisationId = id;
        this.showDetailsPanel = true;
      }, 50);
    }
  }

  // ==================== PANNEAU ORDONNANCE HOSPITALISATION ====================

  openOrdonnanceHospPanel(hospitalisation: any): void {
    this.ordonnanceHospitalisationId = hospitalisation.idAdmission;
    this.ordonnancePatientNom = hospitalisation.patient?.nom || '';
    this.ordonnancePatientPrenom = hospitalisation.patient?.prenom || '';
    this.showOrdonnanceHospPanel = true;
    this.showDetailsPanel = false;
  }

  closeOrdonnanceHospPanel(): void {
    this.showOrdonnanceHospPanel = false;
    this.ordonnanceHospitalisationId = null;
    this.ordonnancePatientNom = '';
    this.ordonnancePatientPrenom = '';
  }

  onOrdonnanceHospSaved(): void {
    this.closeOrdonnanceHospPanel();
    // Rafraîchir les détails si le panneau était ouvert
    if (this.selectedHospitalisationId) {
      const id = this.selectedHospitalisationId;
      this.selectedHospitalisationId = null;
      setTimeout(() => {
        this.selectedHospitalisationId = id;
        this.showDetailsPanel = true;
      }, 50);
    }
  }

  // ==================== PANNEAU SOIN HOSPITALISATION ====================

  openSoinHospPanel(hospitalisation: any): void {
    this.soinHospitalisationId = hospitalisation.idAdmission;
    this.soinPatientNom = hospitalisation.patient?.nom || '';
    this.soinPatientPrenom = hospitalisation.patient?.prenom || '';
    this.showSoinHospPanel = true;
    this.showDetailsPanel = false;
  }

  closeSoinHospPanel(): void {
    this.showSoinHospPanel = false;
    this.soinHospitalisationId = null;
    this.soinPatientNom = '';
    this.soinPatientPrenom = '';
  }

  onSoinHospSaved(): void {
    this.closeSoinHospPanel();
    // Rafraîchir les détails si le panneau était ouvert
    if (this.selectedHospitalisationId) {
      const id = this.selectedHospitalisationId;
      this.selectedHospitalisationId = null;
      setTimeout(() => {
        this.selectedHospitalisationId = id;
        this.showDetailsPanel = true;
      }, 50);
    }
  }

  openSoinPanelFromDetails(hospitalisation: any): void {
    this.closeDetailsPanel();
    this.openSoinHospPanel(hospitalisation);
  }

  // ==================== PANNEAU EXAMEN/ORDONNANCE (FICHE PATIENT) ====================
  // Ces méthodes sont appelées depuis app-fiche-patient-panel (contexte consultation, pas hospitalisation)

  openExamenPanel(patientId: number): void {
    // Pour l'instant, afficher un message - fonctionnalité à implémenter via consultation
    console.log('Prescrire examen pour patient:', patientId);
    // TODO: Ouvrir le panneau de prescription d'examen dans le contexte consultation
  }

  openOrdonnancePanel(patientId: number): void {
    // Pour l'instant, afficher un message - fonctionnalité à implémenter via consultation
    console.log('Faire ordonnance pour patient:', patientId);
    // TODO: Ouvrir le panneau d'ordonnance dans le contexte consultation
  }

  // ==================== PANNEAU ATTRIBUTION LIT (MAJOR) ====================

  openAttribuerLitPanel(patient: PatientHospitaliseDto): void {
    this.selectedHospitalisationForLit = {
      idAdmission: patient.idAdmission,
      patientNom: patient.patientNom,
      patientPrenom: patient.patientPrenom,
      motif: patient.motif,
      dateEntree: patient.dateEntree
    };
    this.showAttribuerLitPanel = true;
  }

  closeAttribuerLitPanel(): void {
    this.showAttribuerLitPanel = false;
    this.selectedHospitalisationForLit = null;
  }

  onLitAttribue(): void {
    this.closeAttribuerLitPanel();
    this.loadPatientsHospitalises();
  }

  getStatutLabel(statut: string): string {
    const s = statut?.toLowerCase();
    switch (s) {
      case 'en_attente':
      case 'en_attente_lit': return 'En attente';
      case 'en_cours': return 'En cours';
      case 'termine': return 'Terminé';
      case 'annule': return 'Annulé';
      default: return statut;
    }
  }

  getStatutClass(statut: string): string {
    const s = statut?.toLowerCase();
    switch (s) {
      case 'en_attente':
      case 'en_attente_lit': return 'statut-attente';
      case 'en_cours': return 'statut-en-cours';
      case 'termine': return 'statut-termine';
      case 'annule': return 'statut-annule';
      default: return '';
    }
  }
}
