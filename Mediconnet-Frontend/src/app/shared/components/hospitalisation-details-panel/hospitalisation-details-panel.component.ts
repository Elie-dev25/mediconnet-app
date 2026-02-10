import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { LucideAngularModule } from 'lucide-angular';
import { environment } from '../../../../environments/environment';

export interface ExecutionSoin {
  idExecution: number;
  dateExecution: string;
  executant?: string;
  observations?: string;
  statutExecution: string;
  numeroExecution: number;
}

export interface SoinDetail {
  idSoin: number;
  typeSoin: string;
  description: string;
  frequence?: string;
  dureeJours?: number;
  moments?: string;
  priorite: string;
  instructions?: string;
  statut: string;
  datePrescription: string;
  dateDebut?: string;
  dateFinPrevue?: string;
  prescripteur?: string;
  nbExecutionsPrevues: number;
  nbExecutionsEffectuees: number;
  nbExecutionsManquees?: number;
  progression?: number;
}

export interface ExamenDetail {
  idExamen: number;
  idBulletinExamen?: number;
  typeExamen: string;
  description?: string;
  datePrescription: string;
  statut: string;
  urgence: boolean;
  laboratoire?: string;
  resultat?: string;
  dateResultat?: string;
  hasResultat: boolean;
}

export interface PrescriptionDetail {
  idPrescription: number;
  medicament: string;
  dosage?: string;
  posologie: string;
  frequence?: string;
  voieAdministration?: string;
  duree?: string;
  dateDebut: string;
  instructions?: string;
}

export interface HospitalisationDetails {
  idAdmission: number;
  statut: string;
  urgence: string;
  dateEntree: string;
  dateSortiePrevue?: string;
  dateSortie?: string;
  motif: string;
  diagnosticPrincipal?: string;
  patient: {
    idPatient: number;
    nom: string;
    prenom: string;
    numeroDossier?: string;
    dateNaissance?: string;
    sexe?: string;
    telephone?: string;
  };
  medecin?: {
    idMedecin: number;
    nom: string;
    prenom: string;
    specialite?: string;
  };
  service?: string;
  lit?: {
    idLit: number;
    numero: string;
    chambre: string;
    standard?: string;
  };
  soins?: SoinDetail[];
  examens?: ExamenDetail[];
  prescriptions?: PrescriptionDetail[];
}

import { SoinExecutionsPopupComponent } from '../soin-executions-popup/soin-executions-popup.component';
import { ResultatExamenSidebarComponent } from '../resultat-examen-sidebar/resultat-examen-sidebar.component';

@Component({
  selector: 'app-hospitalisation-details-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, SoinExecutionsPopupComponent, ResultatExamenSidebarComponent],
  templateUrl: './hospitalisation-details-panel.component.html',
  styleUrls: ['./hospitalisation-details-panel.component.scss']
})
export class HospitalisationDetailsPanelComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() hospitalisationId: number | null = null;
  @Input() context: 'medecin' | 'infirmier' = 'infirmier';
  
  @Output() close = new EventEmitter<void>();
  @Output() openAttribuerLit = new EventEmitter<HospitalisationDetails>();
  @Output() addSoins = new EventEmitter<HospitalisationDetails>();
  @Output() addExamens = new EventEmitter<HospitalisationDetails>();
  @Output() addPrescriptions = new EventEmitter<HospitalisationDetails>();
  @Output() finHospitalisation = new EventEmitter<HospitalisationDetails>();

  hospitalisation: HospitalisationDetails | null = null;
  isLoading = false;
  error: string | null = null;
  activeTab: 'general' | 'soins' | 'examens' | 'prescriptions' = 'general';
  expandedSoinId: number | null = null;
  
  // Popup exécutions
  showExecutionsPopup = false;
  selectedSoinId: number | null = null;

  // Modal enregistrement soin
  showEnregistrerModal = false;
  soinAEnregistrer: SoinDetail | null = null;
  observationsEnregistrement = '';
  isEnregistrementEnCours = false;
  enregistrementSuccess = false;
  enregistrementError: string | null = null;
  enregistrementResult: { moment?: string; heureExecution?: string; executant?: string; nbExecutionsRestantes?: number } | null = null;

  // Sidebar résultat examen
  showResultatSidebar = false;
  selectedExamenId: number | null = null;

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen && this.hospitalisationId) {
      this.loadHospitalisationDetails();
    }
    if (changes['isOpen'] && !this.isOpen) {
      this.resetState();
    }
  }

  private resetState(): void {
    this.hospitalisation = null;
    this.error = null;
    this.activeTab = 'general';
  }

  loadHospitalisationDetails(): void {
    if (!this.hospitalisationId) return;

    this.isLoading = true;
    this.error = null;

    const baseUrl = this.context === 'medecin' 
      ? `${this.apiUrl}/medecin` 
      : `${this.apiUrl}/infirmier`;

    this.http.get<{ success: boolean; data: HospitalisationDetails }>(
      `${baseUrl}/hospitalisation/${this.hospitalisationId}/details`
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.hospitalisation = response.data;
        } else {
          this.error = 'Impossible de charger les détails';
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement détails hospitalisation:', err);
        this.error = 'Erreur de connexion au serveur';
        this.isLoading = false;
      }
    });
  }

  onClose(): void {
    this.close.emit();
  }

  onOverlayClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('panel-overlay')) {
      this.onClose();
    }
  }

  setActiveTab(tab: 'general' | 'soins' | 'examens' | 'prescriptions'): void {
    this.activeTab = tab;
  }

  onAttribuerLit(): void {
    if (this.hospitalisation) {
      this.openAttribuerLit.emit(this.hospitalisation);
    }
  }

  onAddSoins(): void {
    if (this.hospitalisation) {
      this.addSoins.emit(this.hospitalisation);
    }
  }

  onAddExamens(): void {
    if (this.hospitalisation) {
      this.addExamens.emit(this.hospitalisation);
    }
  }

  onAddPrescriptions(): void {
    if (this.hospitalisation) {
      this.addPrescriptions.emit(this.hospitalisation);
    }
  }

  onFinHospitalisation(): void {
    if (this.hospitalisation) {
      this.finHospitalisation.emit(this.hospitalisation);
    }
  }

  get isMajor(): boolean {
    return this.context === 'infirmier';
  }

  get isMedecin(): boolean {
    return this.context === 'medecin';
  }

  get canAttribuerLit(): boolean {
    const statut = this.hospitalisation?.statut?.toLowerCase();
    return this.isMajor && (statut === 'en_attente' || this.hospitalisation?.statut === 'EN_ATTENTE');
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { day: '2-digit', month: 'long', year: 'numeric' });
  }

  formatDateTime(dateStr?: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { 
      day: '2-digit', 
      month: 'short', 
      year: 'numeric', 
      hour: '2-digit', 
      minute: '2-digit' 
    });
  }

  getStatutLabel(statut: string): string {
    const s = statut?.toLowerCase();
    switch (s) {
      case 'en_attente': return 'En attente de lit';
      case 'en_cours': return 'Hospitalisé';
      case 'termine': return 'Terminé';
      default: return statut;
    }
  }

  getStatutClass(statut: string): string {
    const s = statut?.toLowerCase();
    switch (s) {
      case 'en_attente': return 'status-warning';
      case 'en_cours': return 'status-success';
      case 'termine': return 'status-neutral';
      default: return '';
    }
  }

  getUrgenceClass(urgence: string): string {
    switch (urgence) {
      case 'critique': return 'urgence-critique';
      case 'urgente': return 'urgence-urgente';
      default: return 'urgence-normale';
    }
  }

  getSoinStatutClass(statut: string): string {
    switch (statut?.toLowerCase()) {
      case 'effectue': case 'termine': return 'status-success';
      case 'en_cours': return 'status-info';
      case 'planifie': return 'status-warning';
      default: return 'status-neutral';
    }
  }

  getExamenStatutClass(statut: string): string {
    switch (statut?.toLowerCase()) {
      case 'termine': case 'resultat_disponible': return 'status-success';
      case 'en_cours': return 'status-info';
      case 'prescrit': return 'status-warning';
      default: return 'status-neutral';
    }
  }

  getPrioriteClass(priorite: string): string {
    switch (priorite?.toLowerCase()) {
      case 'urgente': return 'priorite-urgente';
      case 'haute': return 'priorite-haute';
      case 'normale': return 'priorite-normale';
      case 'basse': return 'priorite-basse';
      default: return 'priorite-normale';
    }
  }

  toggleSoinDetails(idSoin: number): void {
    this.expandedSoinId = this.expandedSoinId === idSoin ? null : idSoin;
  }

  getProgressPercent(soin: SoinDetail): number {
    if (soin.nbExecutionsPrevues <= 0) return 0;
    return Math.min(100, (soin.nbExecutionsEffectuees / soin.nbExecutionsPrevues) * 100);
  }

  getExecutionStatutLabel(statut: string): string {
    switch (statut) {
      case 'effectue': return 'Effectué';
      case 'fait': return 'Fait';
      case 'partiel': return 'Partiel';
      case 'refuse_patient': return 'Refusé';
      case 'reporte': return 'Reporté';
      case 'annule': return 'Annulé';
      case 'manque': return 'Manqué';
      case 'prevu': return 'Prévu';
      default: return statut;
    }
  }

  getMomentsArray(moments: string): string[] {
    if (!moments) return [];
    return moments.split(',').map(m => m.trim());
  }

  getMomentIcon(moment: string): string {
    switch (moment.toLowerCase()) {
      case 'matin': return 'sun';
      case 'midi': return 'sun';
      case 'soir': return 'cloud-sun';
      case 'nuit': return 'moon';
      default: return 'clock';
    }
  }

  getMomentLabel(moment: string): string {
    switch (moment.toLowerCase()) {
      case 'matin': return 'Matin';
      case 'midi': return 'Midi';
      case 'soir': return 'Soir';
      case 'nuit': return 'Nuit';
      default: return moment;
    }
  }

  openSoinDetailsPopup(idSoin: number): void {
    this.selectedSoinId = idSoin;
    this.showExecutionsPopup = true;
  }

  closeExecutionsPopup(): void {
    this.showExecutionsPopup = false;
    this.selectedSoinId = null;
  }

  // ==================== Enregistrement de soin ====================

  openEnregistrerSoinModal(soin: SoinDetail): void {
    this.soinAEnregistrer = soin;
    this.observationsEnregistrement = '';
    this.isEnregistrementEnCours = false;
    this.enregistrementSuccess = false;
    this.enregistrementError = null;
    this.enregistrementResult = null;
    this.showEnregistrerModal = true;
  }

  closeEnregistrerModal(): void {
    this.showEnregistrerModal = false;
    this.soinAEnregistrer = null;
    // Si succès, recharger les détails
    if (this.enregistrementSuccess) {
      this.loadHospitalisationDetails();
    }
  }

  enregistrerSoin(): void {
    if (!this.soinAEnregistrer || this.isEnregistrementEnCours) return;

    this.isEnregistrementEnCours = true;
    this.enregistrementError = null;

    // Utiliser le bon endpoint selon le contexte
    const endpoint = this.context === 'medecin'
      ? `${this.apiUrl}/medecin/soins/${this.soinAEnregistrer.idSoin}/executer`
      : `${this.apiUrl}/infirmier/soins/${this.soinAEnregistrer.idSoin}/executer`;

    this.http.post<{ 
      success: boolean; 
      message: string; 
      data?: { 
        idExecution: number;
        moment: string;
        datePrevue: string;
        heureExecution: string;
        executant: string;
        nbExecutionsRestantes: number;
      } 
    }>(
      endpoint,
      { observations: this.observationsEnregistrement || null }
    ).subscribe({
      next: (response) => {
        this.isEnregistrementEnCours = false;
        if (response.success && response.data) {
          this.enregistrementSuccess = true;
          this.enregistrementResult = response.data;
          // Mettre à jour le soin localement
          if (this.soinAEnregistrer) {
            this.soinAEnregistrer.nbExecutionsEffectuees++;
            if (this.soinAEnregistrer.nbExecutionsEffectuees >= this.soinAEnregistrer.nbExecutionsPrevues) {
              this.soinAEnregistrer.statut = 'termine';
            } else if (this.soinAEnregistrer.statut === 'prescrit') {
              this.soinAEnregistrer.statut = 'en_cours';
            }
          }
        } else {
          this.enregistrementError = response.message || 'Erreur lors de l\'enregistrement';
        }
      },
      error: (err) => {
        this.isEnregistrementEnCours = false;
        this.enregistrementError = err.error?.message || 'Erreur de connexion au serveur';
      }
    });
  }

  getMomentLabelForResult(moment?: string): string {
    if (!moment) return '';
    switch (moment.toLowerCase()) {
      case 'matin': return 'du matin';
      case 'midi': return 'de midi';
      case 'soir': return 'du soir';
      case 'nuit': return 'de nuit';
      default: return moment;
    }
  }

  // Méthodes pour la sidebar résultat examen
  openResultatExamen(examen: ExamenDetail): void {
    const id = examen.idBulletinExamen || examen.idExamen;
    if (id) {
      this.selectedExamenId = id;
      this.showResultatSidebar = true;
    }
  }

  closeResultatSidebar(): void {
    this.showResultatSidebar = false;
    this.selectedExamenId = null;
  }
}
