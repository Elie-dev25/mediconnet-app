import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { ConsultationCompleteService, DossierPatientDto } from '../../../services/consultation-complete.service';
import { DossierMedicalViewComponent } from '../dossier-medical-view/dossier-medical-view.component';
import { 
  DossierMedicalData,
  ConsultationItem,
  OrdonnanceItem,
  ExamenItem
} from '../../../models/dossier-medical.models';

/**
 * Composant wrapper pour afficher le dossier patient dans un contexte de consultation.
 * Utilise DossierMedicalViewComponent en interne pour éviter la duplication de code.
 */
@Component({
  selector: 'app-dossier-patient',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, DossierMedicalViewComponent],
  template: `
    <div class="dossier-patient-wrapper">
      <div class="wrapper-header">
        <h2>
          <lucide-icon name="folder-open" [size]="24"></lucide-icon>
          Dossier Patient
        </h2>
        <button class="btn-close" (click)="onClose()">
          <lucide-icon name="x" [size]="20"></lucide-icon>
        </button>
      </div>
      
      <div class="wrapper-body">
        <app-dossier-medical-view
          [mode]="'medecin'"
          [dossier]="dossierData"
          [isLoading]="isLoading"
          [error]="error"
          [showStartConsultation]="!!consultationId"
          [consultationId]="consultationId || null"
          (retry)="loadDossier()"
          (startConsultation)="onStartConsultation()"
          (viewConsultation)="onViewConsultation($event)">
        </app-dossier-medical-view>
      </div>
    </div>
  `,
  styles: [`
    .dossier-patient-wrapper {
      display: flex;
      flex-direction: column;
      height: 100%;
      background: #fff;
      border-radius: 12px;
      overflow: hidden;
    }
    
    .wrapper-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem 1.5rem;
      background: linear-gradient(135deg, #0e7490 0%, #0891b2 100%);
      color: #fff;
      
      h2 {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        margin: 0;
        font-size: 1.25rem;
        font-weight: 600;
      }
      
      .btn-close {
        background: rgba(255, 255, 255, 0.2);
        border: none;
        color: #fff;
        width: 36px;
        height: 36px;
        border-radius: 8px;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        transition: background-color 0.2s;
        
        &:hover {
          background: rgba(255, 255, 255, 0.3);
        }
      }
    }
    
    .wrapper-body {
      flex: 1;
      overflow: auto;
    }
  `]
})
export class DossierPatientComponent implements OnInit {
  @Input() patientId!: number;
  @Input() consultationId?: number;
  @Output() startConsultation = new EventEmitter<{ patientId: number; consultationId: number }>();
  @Output() viewConsultation = new EventEmitter<{ consultationId: number }>();
  @Output() close = new EventEmitter<void>();

  dossierData: DossierMedicalData | null = null;
  isLoading = true;
  error: string | null = null;

  constructor(private consultationService: ConsultationCompleteService) {}

  ngOnInit(): void {
    this.loadDossier();
  }

  loadDossier(): void {
    this.isLoading = true;
    this.error = null;

    this.consultationService.getDossierPatient(this.patientId).subscribe({
      next: (dossier) => {
        this.dossierData = this.transformToDossierMedicalData(dossier);
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement dossier:', err);
        this.error = 'Impossible de charger le dossier patient';
        this.isLoading = false;
      }
    });
  }

  /**
   * Transforme les données du service vers le format DossierMedicalData
   */
  private transformToDossierMedicalData(dto: DossierPatientDto): DossierMedicalData {
    return {
      patient: {
        idUser: dto.idPatient,
        nom: dto.nom,
        prenom: dto.prenom,
        numeroDossier: dto.numeroDossier,
        groupeSanguin: dto.groupeSanguin,
        naissance: dto.naissance ? new Date(dto.naissance).toISOString() : undefined,
        sexe: dto.sexe,
        age: dto.age,
        telephone: dto.telephone,
        email: dto.email,
        adresse: dto.adresse,
        nationalite: dto.nationalite,
        regionOrigine: dto.regionOrigine,
        situationMatrimoniale: dto.situationMatrimoniale,
        profession: dto.profession,
        ethnie: dto.ethnie,
        nbEnfants: dto.nbEnfants,
        maladiesChroniques: dto.maladiesChroniques,
        allergiesConnues: dto.allergiesConnues,
        allergiesDetails: dto.allergiesDetails,
        antecedentsFamiliaux: dto.antecedentsFamiliaux,
        antecedentsFamiliauxDetails: dto.antecedentsFamiliauxDetails,
        operationsChirurgicales: dto.operationsChirurgicales,
        operationsDetails: dto.operationsDetails,
        consommationAlcool: dto.consommationAlcool,
        frequenceAlcool: dto.frequenceAlcool,
        tabagisme: dto.tabagisme,
        activitePhysique: dto.activitePhysique,
        personneContact: dto.personneContact,
        numeroContact: dto.numeroContact,
        nomAssurance: dto.nomAssurance,
        numeroCarteAssurance: dto.numeroCarteAssurance,
        couvertureAssurance: dto.couvertureAssurance,
        dateDebutValidite: dto.dateDebutValidite,
        dateFinValidite: dto.dateFinValidite
      },
      stats: {
        totalConsultations: dto.consultations?.length || 0,
        totalOrdonnances: dto.ordonnances?.length || 0,
        totalExamens: dto.examens?.length || 0
      },
      consultations: (dto.consultations || []).map(c => ({
        idConsultation: c.idConsultation,
        dateHeure: c.dateHeure ? new Date(c.dateHeure).toISOString() : undefined,
        motif: c.motif || '',
        diagnostic: c.diagnostic,
        nomMedecin: c.medecinNom,
        specialite: c.specialite,
        statut: c.statut || 'terminee'
      } as ConsultationItem)),
      ordonnances: (dto.ordonnances || []).map(o => ({
        idOrdonnance: o.idOrdonnance,
        dateCreation: o.dateCreation ? new Date(o.dateCreation).toISOString() : undefined,
        medicaments: (o.medicaments || []).map(m => ({
          nomMedicament: m.nomMedicament,
          frequence: m.frequence,
          duree: m.duree,
          dosage: m.quantite?.toString()
        }))
      } as OrdonnanceItem)),
      examens: (dto.examens || []).map(e => ({
        idExamen: e.idExamen,
        datePrescription: e.datePrescription ? new Date(e.datePrescription).toISOString() : undefined,
        typeExamen: e.typeExamen || 'autre',
        nomExamen: e.nomExamen,
        resultats: e.resultats,
        statut: e.statut || 'prescrit'
      } as ExamenItem)),
      antecedents: [],
      allergies: []
    };
  }

  onStartConsultation(): void {
    if (this.consultationId) {
      this.startConsultation.emit({ patientId: this.patientId, consultationId: this.consultationId });
    }
  }

  onViewConsultation(consultationId: number): void {
    this.viewConsultation.emit({ consultationId });
  }

  onClose(): void {
    this.close.emit();
  }
}
