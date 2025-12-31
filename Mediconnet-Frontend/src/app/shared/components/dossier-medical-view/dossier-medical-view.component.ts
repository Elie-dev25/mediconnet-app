import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { ConsultationCompleteService, ConsultationEnCoursDto } from '../../../services/consultation-complete.service';

export type ViewMode = 'patient' | 'medecin';
export type TabType = 'resume' | 'consultations' | 'ordonnances' | 'examens' | 'antecedents';

export interface DossierPatientInfo {
  idUser?: number;
  nom: string;
  prenom: string;
  numeroDossier?: string;
  groupeSanguin?: string;
  naissance?: string;
  sexe?: string;
  age?: number;
}

export interface DossierStats {
  totalConsultations: number;
  totalOrdonnances: number;
  totalExamens: number;
  derniereVisite?: string;
}

export interface ConsultationItem {
  idConsultation?: number;
  dateConsultation?: string;
  dateHeure?: string;
  motif: string;
  diagnosticPrincipal?: string;
  diagnostic?: string;
  nomMedecin?: string;
  medecinNom?: string;
  specialite?: string;
  statut: string;
}

export interface OrdonnanceItem {
  idOrdonnance: number;
  dateOrdonnance?: string;
  dateCreation?: string;
  nomMedecin?: string;
  statut?: string;
  medicaments: MedicamentItem[];
}

export interface MedicamentItem {
  nom?: string;
  nomMedicament?: string;
  dosage?: string;
  frequence?: string;
  duree?: string;
  instructions?: string;
}

export interface ExamenItem {
  idExamen: number;
  dateExamen?: string;
  datePrescription?: string;
  typeExamen: string;
  nomExamen: string;
  resultat?: string;
  resultats?: string;
  nomMedecin?: string;
  statut: string;
  urgent?: boolean;
}

export interface AntecedentItem {
  type: string;
  description: string;
  dateDebut?: string;
  actif: boolean;
}

export interface AllergieItem {
  type: string;
  allergene: string;
  severite: string;
  reaction?: string;
}

export interface DossierMedicalData {
  patient: DossierPatientInfo;
  stats?: DossierStats;
  consultations: ConsultationItem[];
  ordonnances: OrdonnanceItem[];
  examens: ExamenItem[];
  antecedents: AntecedentItem[];
  allergies: AllergieItem[];
}

@Component({
  selector: 'app-dossier-medical-view',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
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

  @Output() retry = new EventEmitter<void>();
  @Output() startConsultation = new EventEmitter<void>();

  activeTab: TabType = 'resume';

  // Modale détails consultation
  showConsultationModal = false;
  selectedConsultation: ConsultationEnCoursDto | null = null;
  isLoadingConsultation = false;
  consultationError: string | null = null;

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

  getExamenTypeIcon(type: string): string {
    switch (type) {
      case 'biologie':
        return 'flask-conical';
      case 'imagerie':
        return 'scan';
      case 'cardiologie':
        return 'heart-pulse';
      default:
        return 'file-text';
    }
  }

  // Afficher les détails d'une consultation
  viewConsultationDetails(consultation: ConsultationItem): void {
    if (!consultation.idConsultation) return;

    this.showConsultationModal = true;
    this.isLoadingConsultation = true;
    this.consultationError = null;
    this.selectedConsultation = null;

    this.consultationService.getConsultation(consultation.idConsultation).subscribe({
      next: (data: ConsultationEnCoursDto) => {
        this.selectedConsultation = data;
        this.isLoadingConsultation = false;
      },
      error: (err: any) => {
        console.error('Erreur chargement consultation:', err);
        this.consultationError = 'Impossible de charger les détails';
        this.isLoadingConsultation = false;
      }
    });
  }

  closeConsultationModal(): void {
    this.showConsultationModal = false;
    this.selectedConsultation = null;
    this.consultationError = null;
  }
}
