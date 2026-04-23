import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { ConsultationCompleteService, ConsultationEnCoursDto } from '../../../services/consultation-complete.service';
import { ResultatExamenSidebarComponent } from '../resultat-examen-sidebar/resultat-examen-sidebar.component';

// Re-export des types depuis le fichier centralisé pour la rétrocompatibilité
export type {
  ViewMode,
  TabType,
  DossierPatientInfo,
  DossierStats,
  ConsultationItem,
  OrdonnanceItem,
  MedicamentItem,
  ExamenItem,
  AntecedentItem,
  AllergieItem,
  HospitalisationItem,
  RecommandationItem,
  DossierMedicalData
} from '../../../models/dossier-medical.models';

// Re-export des fonctions
export {
  getStatutLabel,
  getStatutClass,
  isStatutTermine
} from '../../../models/dossier-medical.models';

import type {
  ViewMode,
  TabType,
  DossierMedicalData,
  ConsultationItem,
  OrdonnanceItem,
  MedicamentItem,
  ExamenItem
} from '../../../models/dossier-medical.models';

@Component({
  selector: 'app-dossier-medical-view',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, ResultatExamenSidebarComponent],
  templateUrl: './dossier-medical-view.component.html',
  styleUrl: './dossier-medical-view.component.scss'
})
export class DossierMedicalViewComponent {
  @Input() mode: ViewMode = 'patient';
  @Input() dossier: DossierMedicalData | null = null;
  @Input() isLoading = false;
  @Input() error: string | null = null;
  @Input() showStartConsultation = false;
  @Input() consultationId: number | null = null;
  @Input() consultationStatut: string | null = null;

  @Output() retry = new EventEmitter<void>();
  @Output() startConsultation = new EventEmitter<void>();
  @Output() viewConsultation = new EventEmitter<number>();

  // Helpers pour le bouton de consultation
  get isConsultationEnPause(): boolean {
    return this.consultationStatut === 'en_pause';
  }

  get isConsultationEnCours(): boolean {
    return this.consultationStatut === 'en_cours';
  }

  get consultationButtonLabel(): string {
    if (this.isConsultationEnPause) return 'Continuer la consultation';
    if (this.isConsultationEnCours) return 'Reprendre la consultation';
    return 'Commencer la consultation';
  }

  get consultationButtonIcon(): string {
    if (this.isConsultationEnPause || this.isConsultationEnCours) return 'play';
    return 'stethoscope';
  }

  activeTab: TabType = 'resume';

  // Modale détails consultation
  showConsultationModal = false;
  selectedConsultation: ConsultationEnCoursDto | null = null;
  isLoadingConsultation = false;
  consultationError: string | null = null;

  // Sidebar résultat examen
  showResultatSidebar = false;
  selectedExamenId: number | null = null;

  constructor(private consultationService: ConsultationCompleteService) {}

  setActiveTab(tab: TabType): void {
    this.activeTab = tab;
  }

  onRetry(): void {
    this.retry.emit();
  }

  onStartConsultation(): void {
    this.startConsultation.emit();
  }

  // Getters pour normaliser les données
  getPatientFullName(): string {
    if (!this.dossier) return '';
    return `${this.dossier.patient.prenom || ''} ${this.dossier.patient.nom || ''}`.trim();
  }

  getConsultationDate(c: ConsultationItem): string {
    return c.dateConsultation || c.dateHeure || '';
  }

  getConsultationDiagnosis(c: ConsultationItem): string | undefined {
    return c.diagnosticPrincipal || c.diagnostic;
  }

  getConsultationDoctor(c: ConsultationItem): string {
    return c.nomMedecin || c.medecinNom || 'Médecin';
  }

  getOrdonnanceDate(o: OrdonnanceItem): string {
    return o.dateOrdonnance || o.dateCreation || '';
  }

  getMedicamentName(m: MedicamentItem): string {
    return m.nom || m.nomMedicament || '';
  }

  getExamenDate(e: ExamenItem): string {
    return e.dateExamen || e.datePrescription || '';
  }

  getExamenResult(e: ExamenItem): string | undefined {
    return e.resultat || e.resultats;
  }

  // Formatage
  formatDate(dateStr: string | undefined): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatDateLong(dateStr: string | undefined): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
  }

  // Classes et labels
  getStatutClass(statut: string): string {
    switch (statut) {
      case 'terminee':
      case 'termine':
      case 'realise':
        return 'success';
      case 'en_cours':
      case 'active':
        return 'info';
      case 'en_attente':
      case 'prescrit':
        return 'warning';
      case 'annulee':
      case 'annule':
        return 'danger';
      default:
        return '';
    }
  }

  getStatutLabel(statut: string): string {
    switch (statut) {
      case 'terminee':
      case 'termine':
        return 'Terminé';
      case 'en_cours':
        return 'En cours';
      case 'active':
        return 'Active';
      case 'en_attente':
        return 'En attente';
      case 'prescrit':
        return 'Prescrit';
      case 'realise':
        return 'Réalisé';
      case 'annulee':
      case 'annule':
        return 'Annulé';
      default:
        return statut;
    }
  }

  getSeveriteClass(severite: string): string {
    switch (severite) {
      case 'severe':
        return 'danger';
      case 'moderate':
        return 'warning';
      case 'mild':
        return 'info';
      default:
        return '';
    }
  }

  getSeveriteLabel(severite: string): string {
    switch (severite) {
      case 'severe':
        return 'Sévère';
      case 'moderate':
        return 'Modérée';
      case 'mild':
        return 'Légère';
      default:
        return severite;
    }
  }

  getAntecedentTypeIcon(type: string): string {
    switch (type) {
      case 'medical':
        return 'heart-pulse';
      case 'chirurgical':
        return 'scissors';
      case 'familial':
        return 'users';
      default:
        return 'file-text';
    }
  }

  getMotifSortieLabel(motif?: string): string {
    if (!motif) return 'Non spécifié';
    switch (motif) {
      case 'guerison': return 'Guérison';
      case 'amelioration': return 'Amélioration';
      case 'transfert': return 'Transfert';
      case 'demande_patient': return 'Demande du patient';
      case 'autre': return 'Autre';
      default: return motif;
    }
  }

  getExamenTypeIcon(type: string): string {
    if (!type) return 'flask-conical';
    const typeLower = type.toLowerCase();
    
    if (typeLower.includes('biologie') || typeLower.includes('analyse')) {
      return 'flask-conical';
    }
    if (typeLower.includes('imagerie') || typeLower.includes('radio') || typeLower.includes('scanner') || typeLower.includes('irm')) {
      return 'scan';
    }
    if (typeLower.includes('cardio')) {
      return 'heart-pulse';
    }
    if (typeLower.includes('neuro')) {
      return 'brain';
    }
    return 'test-tube';
  }

  // ==================== ORIENTATIONS ====================

  getOrientationIcon(rec: any): string {
    const type = rec.typeOrientation || rec.type;
    switch (type) {
      case 'hopital':
        return 'building-2';
      case 'medecin_interne':
        return 'user-check';
      case 'medecin_externe':
        return 'user';
      case 'service_interne':
        return 'building';
      case 'laboratoire':
        return 'flask-conical';
      case 'medecin':
        return 'user-check';
      default:
        return 'send';
    }
  }

  getOrientationTypeLabel(rec: any): string {
    const type = rec.typeOrientation || rec.type;
    switch (type) {
      case 'hopital':
        return 'Hôpital';
      case 'medecin_interne':
        return 'Médecin interne';
      case 'medecin_externe':
        return 'Médecin externe';
      case 'service_interne':
        return 'Service interne';
      case 'laboratoire':
        return 'Laboratoire';
      case 'medecin':
        return 'Médecin';
      default:
        return 'Orientation';
    }
  }

  getOrientationDestinataire(rec: any): string {
    return rec.nomDestinataire || rec.nomMedecinOriente || rec.nomMedecinRecommande || rec.nomHopital || rec.nomSpecialite || 'Non spécifié';
  }

  getOrientationSpecialite(rec: any): string | null {
    return rec.nomSpecialite || rec.specialite || rec.specialiteTexte || null;
  }

  getOrientationStatutLabel(statut: string | undefined): string {
    if (!statut) return '';
    switch (statut) {
      case 'en_attente':
        return 'En attente';
      case 'acceptee':
        return 'Acceptée';
      case 'refusee':
        return 'Refusée';
      case 'rdv_pris':
        return 'RDV pris';
      case 'terminee':
        return 'Terminée';
      case 'annulee':
        return 'Annulée';
      default:
        return statut;
    }
  }

  // Afficher les détails d'une consultation - émet un événement pour navigation
  viewConsultationDetails(consultation: ConsultationItem): void {
    if (!consultation.idConsultation) return;
    this.viewConsultation.emit(consultation.idConsultation);
  }

  closeConsultationModal(): void {
    this.showConsultationModal = false;
    this.selectedConsultation = null;
    this.consultationError = null;
  }

  // Ouvrir la sidebar pour consulter un résultat d'examen
  openResultatExamen(idExamen: number): void {
    this.selectedExamenId = idExamen;
    this.showResultatSidebar = true;
  }

  closeResultatSidebar(): void {
    this.showResultatSidebar = false;
    this.selectedExamenId = null;
  }
}
