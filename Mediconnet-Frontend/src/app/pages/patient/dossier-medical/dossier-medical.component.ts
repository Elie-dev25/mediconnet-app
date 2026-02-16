import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
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

  constructor(
    private patientService: PatientService,
    private router: Router
  ) {}

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
        idUser: dto.patient.idUser,
        nom: dto.patient.nom,
        prenom: dto.patient.prenom,
        numeroDossier: dto.patient.numeroDossier,
        groupeSanguin: dto.patient.groupeSanguin,
        naissance: dto.patient.naissance,
        sexe: dto.patient.sexe,
        // Informations personnelles
        telephone: dto.patient.telephone,
        email: dto.patient.email,
        adresse: dto.patient.adresse,
        nationalite: dto.patient.nationalite,
        regionOrigine: dto.patient.regionOrigine,
        situationMatrimoniale: dto.patient.situationMatrimoniale,
        profession: dto.patient.profession,
        ethnie: dto.patient.ethnie,
        nbEnfants: dto.patient.nbEnfants,
        // Informations médicales
        maladiesChroniques: dto.patient.maladiesChroniques,
        allergiesConnues: dto.patient.allergiesConnues,
        allergiesDetails: dto.patient.allergiesDetails,
        antecedentsFamiliaux: dto.patient.antecedentsFamiliaux,
        antecedentsFamiliauxDetails: dto.patient.antecedentsFamiliauxDetails,
        operationsChirurgicales: dto.patient.operationsChirurgicales,
        operationsDetails: dto.patient.operationsDetails,
        // Habitudes de vie
        consommationAlcool: dto.patient.consommationAlcool,
        frequenceAlcool: dto.patient.frequenceAlcool,
        tabagisme: dto.patient.tabagisme,
        activitePhysique: dto.patient.activitePhysique,
        // Contact d'urgence
        personneContact: dto.patient.personneContact,
        numeroContact: dto.patient.numeroContact,
        // Assurance
        nomAssurance: dto.patient.nomAssurance,
        numeroCarteAssurance: dto.patient.numeroCarteAssurance,
        couvertureAssurance: dto.patient.couvertureAssurance,
        dateDebutValidite: dto.patient.dateDebutValidite,
        dateFinValidite: dto.patient.dateFinValidite
      },
      stats: dto.stats,
      consultations: dto.consultations.map(c => ({
        idConsultation: c.idConsultation,
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
      })),
      hospitalisations: (dto.hospitalisations || []).map(h => ({
        idAdmission: h.idAdmission,
        dateEntree: h.dateEntree,
        dateSortiePrevue: h.dateSortiePrevue,
        dateSortie: h.dateSortie,
        motif: h.motif,
        motifSortie: h.motifSortie,
        resumeMedical: h.resumeMedical,
        diagnosticPrincipal: h.diagnosticPrincipal,
        statut: h.statut,
        urgence: h.urgence,
        medecinNom: h.medecinNom,
        serviceNom: h.serviceNom,
        numeroChambre: h.numeroChambre,
        numeroLit: h.numeroLit,
        dureeJours: h.dureeJours
      })),
      recommandations: (dto.recommandations || []).map(r => ({
        idRecommandation: r.idRecommandation,
        type: r.type,
        nomHopital: r.nomHopital,
        nomMedecinRecommande: r.nomMedecinRecommande,
        specialite: r.specialite,
        motif: r.motif,
        prioritaire: r.prioritaire,
        createdAt: r.createdAt,
        medecinPrescripteur: r.medecinPrescripteur,
        idConsultation: r.idConsultation
      }))
    };
  }

  onViewConsultation(consultationId: number): void {
    this.router.navigate(['/patient/consultation', consultationId]);
  }
}
