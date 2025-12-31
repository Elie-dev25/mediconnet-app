import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { PatientService, DossierMedicalDto } from '../../../services/patient.service';
import { 
  DashboardLayoutComponent, 
  DossierMedicalViewComponent,
  DossierMedicalData,
  LucideAngularModule,
  ALL_ICONS_PROVIDER
} from '../../../shared';
import { PATIENT_MENU_ITEMS, PATIENT_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-dossier-medical',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    LucideAngularModule, 
    DashboardLayoutComponent,
    DossierMedicalViewComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './dossier-medical.component.html',
  styleUrl: './dossier-medical.component.scss'
})
export class DossierMedicalComponent implements OnInit {
  menuItems = PATIENT_MENU_ITEMS;
  sidebarTitle = PATIENT_SIDEBAR_TITLE;

  isLoading = true;
  error: string | null = null;

  dossier: DossierMedicalDto | null = null;
  dossierData: DossierMedicalData | null = null;

  constructor(private patientService: PatientService) {}

  ngOnInit(): void {
    this.loadDossierMedical();
  }

  loadDossierMedical(): void {
    this.isLoading = true;
    this.error = null;

    this.patientService.getDossierMedical().pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (data) => {
        this.dossier = data;
        this.dossierData = this.mapToDossierData(data);
      },
      error: (err) => {
        console.error('Erreur chargement dossier:', err);
        this.error = 'Impossible de charger votre dossier médical. Veuillez réessayer.';
      }
    });
  }

  private mapToDossierData(dto: DossierMedicalDto): DossierMedicalData {
    return {
      patient: {
        nom: dto.patient.nom,
        prenom: dto.patient.prenom,
        numeroDossier: dto.patient.numeroDossier,
        groupeSanguin: dto.patient.groupeSanguin,
        naissance: dto.patient.naissance,
        sexe: dto.patient.sexe
      },
      stats: dto.stats,
      consultations: dto.consultations.map(c => ({
        dateConsultation: c.dateConsultation,
        motif: c.motif,
        diagnosticPrincipal: c.diagnosticPrincipal,
        nomMedecin: c.nomMedecin,
        specialite: c.specialite,
        statut: c.statut
      })),
      ordonnances: dto.ordonnances.map(o => ({
        idOrdonnance: o.idOrdonnance,
        dateOrdonnance: o.dateOrdonnance,
        nomMedecin: o.nomMedecin,
        statut: o.statut,
        medicaments: o.medicaments.map(m => ({
          nom: m.nom,
          dosage: m.dosage,
          frequence: m.frequence,
          duree: m.duree,
          instructions: m.instructions
        }))
      })),
      examens: dto.examens.map(e => ({
        idExamen: e.idExamen,
        dateExamen: e.dateExamen,
        typeExamen: e.typeExamen,
        nomExamen: e.nomExamen,
        resultat: e.resultat,
        nomMedecin: e.nomMedecin,
        statut: e.statut,
        urgent: e.urgent
      })),
      antecedents: dto.antecedents.map(a => ({
        type: a.type,
        description: a.description,
        dateDebut: a.dateDebut,
        actif: a.actif
      })),
      allergies: dto.allergies.map(a => ({
        type: a.type,
        allergene: a.allergene,
        severite: a.severite,
        reaction: a.reaction
      }))
    };
  }
}
