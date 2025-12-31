import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { 
  LucideAngularModule, 
  DashboardLayoutComponent,
  ParametresFormComponent,
  ALL_ICONS_PROVIDER 
} from '../../../shared';
import { ParametreService, ParametreDto } from '../../../services/parametre.service';

// Import du menu infirmier
interface MenuItem {
  icon: string;
  label: string;
  route: string;
  implemented?: boolean;
}

@Component({
  selector: 'app-infirmier-parametres',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    LucideAngularModule,
    DashboardLayoutComponent,
    ParametresFormComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './parametres.component.html',
  styleUrls: ['./parametres.component.scss']
})
export class InfirmierParametresComponent implements OnInit {
  menuItems: MenuItem[] = [
    { icon: 'layout-dashboard', label: 'Tableau de bord', route: '/infirmier/dashboard', implemented: true },
    { icon: 'activity', label: 'Paramètres vitaux', route: '/infirmier/parametres', implemented: true },
    { icon: 'users', label: 'Patients', route: '/infirmier/patients', implemented: false },
    { icon: 'clipboard-list', label: 'Soins', route: '/infirmier/soins', implemented: false },
    { icon: 'settings', label: 'Paramètres', route: '/infirmier/settings', implemented: false }
  ];
  sidebarTitle = 'Espace Infirmier';

  // Recherche patient
  searchTerm = '';
  selectedPatientId: number | null = null;
  selectedPatientNom = '';
  selectedConsultationId: number | null = null;

  // Historique
  historique: ParametreDto[] = [];
  loadingHistorique = false;

  // Mode formulaire
  showForm = false;

  constructor(private parametreService: ParametreService) {}

  ngOnInit(): void {}

  /**
   * Recherche un patient (simulation - à connecter au vrai service)
   */
  onSearch(): void {
    // TODO: Implémenter la recherche de patients
    console.log('Recherche:', this.searchTerm);
  }

  /**
   * Sélectionne un patient et charge son historique
   */
  selectPatient(patientId: number, patientNom: string, consultationId: number): void {
    this.selectedPatientId = patientId;
    this.selectedPatientNom = patientNom;
    this.selectedConsultationId = consultationId;
    this.loadHistorique(patientId);
  }

  /**
   * Charge l'historique des paramètres d'un patient
   */
  loadHistorique(patientId: number): void {
    this.loadingHistorique = true;
    this.parametreService.getHistoriquePatient(patientId).subscribe({
      next: (response) => {
        this.historique = response.data;
        this.loadingHistorique = false;
      },
      error: (err) => {
        console.error('Erreur chargement historique:', err);
        this.loadingHistorique = false;
      }
    });
  }

  /**
   * Ouvre le formulaire d'enregistrement
   */
  openForm(): void {
    this.showForm = true;
  }

  /**
   * Callback après enregistrement des paramètres
   */
  onParametresSaved(parametre: ParametreDto): void {
    this.showForm = false;
    // Recharger l'historique
    if (this.selectedPatientId) {
      this.loadHistorique(this.selectedPatientId);
    }
  }

  /**
   * Annule le formulaire
   */
  onFormCancelled(): void {
    this.showForm = false;
  }

  /**
   * Formate la date pour l'affichage
   */
  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  /**
   * Interprétation IMC
   */
  getIMCStatus(imc: number | null): { label: string; color: string } {
    return this.parametreService.interpreterIMC(imc);
  }

  /**
   * Interprétation Tension
   */
  getTensionStatus(sys: number | null, dia: number | null): { label: string; color: string } {
    return this.parametreService.interpreterTension(sys, dia);
  }
}
