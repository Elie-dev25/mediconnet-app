import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { 
  DashboardLayoutComponent,
  DossierMedicalViewComponent,
  DossierMedicalData,
  LucideAngularModule,
  ALL_ICONS_PROVIDER
} from '../../../shared';
import { ConsultationCompleteService, DossierPatientDto } from '../../../services/consultation-complete.service';
import { MEDECIN_MENU_ITEMS, MEDECIN_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-medecin-dossier-patient',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule,
    LucideAngularModule, 
    DashboardLayoutComponent,
    DossierMedicalViewComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './dossier-patient.component.html',
  styleUrl: './dossier-patient.component.scss'
})
export class MedecinDossierPatientComponent implements OnInit {
  menuItems = MEDECIN_MENU_ITEMS;
  sidebarTitle = MEDECIN_SIDEBAR_TITLE;

  patientId: number = 0;
  consultationId: number | null = null;
  
  dossier: DossierPatientDto | null = null;
  dossierData: DossierMedicalData | null = null;
  isLoading = true;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private consultationService: ConsultationCompleteService
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.patientId = +params['patientId'];
      this.loadDossier();
    });

    this.route.queryParams.subscribe(params => {
      if (params['consultationId']) {
        this.consultationId = +params['consultationId'];
      }
    });
  }

  loadDossier(): void {
    if (!this.patientId) {
      this.error = 'ID patient invalide';
      this.isLoading = false;
      return;
    }

    this.isLoading = true;
    this.error = null;

    this.consultationService.getDossierPatient(this.patientId).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (dossier) => {
        this.dossier = dossier;
        this.dossierData = this.mapToDossierData(dossier);
      },
      error: (err) => {
        console.error('Erreur chargement dossier:', err);
        this.error = 'Impossible de charger le dossier patient';
      }
    });
  }

  private mapToDossierData(dto: DossierPatientDto): DossierMedicalData {
    const birthDate = dto.naissance ? new Date(dto.naissance) : null;
    const age = birthDate ? Math.floor((Date.now() - birthDate.getTime()) / (365.25 * 24 * 60 * 60 * 1000)) : dto.age;

    // Convertir les champs texte en tableaux pour le composant partagé
    const antecedents = [];
    if (dto.maladiesChroniques) {
      antecedents.push({ type: 'medical', description: dto.maladiesChroniques, actif: true });
    }
    if (dto.antecedentsFamiliauxDetails) {
      antecedents.push({ type: 'familial', description: dto.antecedentsFamiliauxDetails, actif: true });
    }
    if (dto.operationsDetails) {
      antecedents.push({ type: 'chirurgical', description: dto.operationsDetails, actif: false });
    }

    const allergies = [];
    if (dto.allergiesDetails) {
      allergies.push({ type: 'medicament', allergene: dto.allergiesDetails, severite: 'moderate' });
    }

    return {
      patient: {
        idUser: dto.idPatient,
        nom: dto.nom,
        prenom: dto.prenom,
        numeroDossier: dto.numeroDossier,
        groupeSanguin: dto.groupeSanguin,
        naissance: dto.naissance ? String(dto.naissance) : undefined,
        sexe: dto.sexe,
        age,
        // Informations personnelles
        telephone: dto.telephone,
        email: dto.email,
        adresse: dto.adresse,
        nationalite: dto.nationalite,
        regionOrigine: dto.regionOrigine,
        situationMatrimoniale: dto.situationMatrimoniale,
        profession: dto.profession,
        ethnie: dto.ethnie,
        nbEnfants: dto.nbEnfants,
        // Informations médicales
        maladiesChroniques: dto.maladiesChroniques,
        allergiesConnues: dto.allergiesConnues,
        allergiesDetails: dto.allergiesDetails,
        antecedentsFamiliaux: dto.antecedentsFamiliaux,
        antecedentsFamiliauxDetails: dto.antecedentsFamiliauxDetails,
        operationsChirurgicales: dto.operationsChirurgicales,
        operationsDetails: dto.operationsDetails,
        // Habitudes de vie
        consommationAlcool: dto.consommationAlcool,
        frequenceAlcool: dto.frequenceAlcool,
        tabagisme: dto.tabagisme,
        activitePhysique: dto.activitePhysique,
        // Contact d'urgence
        personneContact: dto.personneContact,
        numeroContact: dto.numeroContact,
        // Assurance
        nomAssurance: dto.nomAssurance,
        numeroCarteAssurance: dto.numeroCarteAssurance,
        couvertureAssurance: dto.couvertureAssurance,
        dateDebutValidite: dto.dateDebutValidite,
        dateFinValidite: dto.dateFinValidite
      },
      stats: {
        totalConsultations: dto.consultations?.length || 0,
        totalOrdonnances: dto.ordonnances?.length || 0,
        totalExamens: dto.examens?.length || 0,
        derniereVisite: dto.consultations?.[0]?.dateHeure ? String(dto.consultations[0].dateHeure) : undefined
      },
      consultations: (dto.consultations || []).map(c => ({
        idConsultation: c.idConsultation,
        dateHeure: String(c.dateHeure),
        motif: c.motif || 'Consultation',
        diagnostic: c.diagnostic,
        medecinNom: c.medecinNom,
        specialite: c.specialite,
        statut: c.statut || 'terminee'
      })),
      ordonnances: (dto.ordonnances || []).map(o => ({
        idOrdonnance: o.idOrdonnance,
        dateCreation: String(o.dateCreation),
        statut: 'active',
        medicaments: (o.medicaments || []).map(m => ({
          nomMedicament: m.nomMedicament,
          dosage: m.dosage,
          frequence: m.frequence,
          duree: m.duree
        }))
      })),
      examens: (dto.examens || []).map(e => ({
        idExamen: e.idExamen,
        datePrescription: String(e.datePrescription),
        typeExamen: e.typeExamen || 'autre',
        nomExamen: e.nomExamen,
        resultats: e.resultats,
        statut: e.statut || 'prescrit'
      })),
      antecedents,
      allergies
    };
  }

  goBack(): void {
    this.router.navigate(['/medecin/dashboard']);
  }

  startConsultation(): void {
    if (!this.consultationId) {
      alert('Aucune consultation associée');
      return;
    }

    // Naviguer vers la page de consultation complète
    this.router.navigate(['/medecin/consultation', this.consultationId]);
  }

  getPatientFullName(): string {
    if (!this.dossier) return '';
    return `${this.dossier.prenom || ''} ${this.dossier.nom || ''}`.trim();
  }

  onViewConsultation(consultationId: number): void {
    this.router.navigate(['/medecin/consultation-details', consultationId]);
  }
}
