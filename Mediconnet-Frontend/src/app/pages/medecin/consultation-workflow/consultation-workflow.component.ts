import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { 
  DashboardLayoutComponent, 
  ConsultationMultiEtapesComponent,
  DossierPatientComponent,
  LucideAngularModule, 
  ALL_ICONS_PROVIDER,
  PageHeaderComponent,
  PatientCardComponent,
  FormSectionComponent,
  AlertMessageComponent,
  LoadingStateComponent,
  PatientInfo
} from '../../../shared';
import { MEDECIN_MENU_ITEMS, MEDECIN_SIDEBAR_TITLE } from '../shared';
import { ConsultationCompleteService } from '../../../services/consultation-complete.service';

@Component({
  selector: 'app-consultation-workflow',
  standalone: true,
  imports: [
    CommonModule,
    LucideAngularModule,
    DashboardLayoutComponent,
    ConsultationMultiEtapesComponent,
    DossierPatientComponent,
    PageHeaderComponent,
    PatientCardComponent,
    FormSectionComponent,
    AlertMessageComponent,
    LoadingStateComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './consultation-workflow.component.html',
  styleUrl: './consultation-workflow.component.scss'
})
export class ConsultationWorkflowComponent implements OnInit {
  menuItems = MEDECIN_MENU_ITEMS;
  sidebarTitle = MEDECIN_SIDEBAR_TITLE;

  consultationId: number | null = null;
  patientId: number | null = null;
  patientNom = '';
  patientData: PatientInfo | null = null;
  
  isLoading = true;
  error: string | null = null;
  showDossierPanel = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private consultationService: ConsultationCompleteService
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      const id = params['id'];
      if (id) {
        this.consultationId = +id;
        this.loadConsultationInfo();
      } else {
        this.error = 'ID de consultation manquant';
        this.isLoading = false;
      }
    });
  }

  loadConsultationInfo(): void {
    if (!this.consultationId) return;
    
    this.consultationService.getConsultation(this.consultationId).subscribe({
      next: (consultation) => {
        this.patientId = consultation.idPatient;
        this.patientNom = `${consultation.patientPrenom} ${consultation.patientNom}`;
        this.patientData = {
          nom: consultation.patientNom,
          prenom: consultation.patientPrenom
        };
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Impossible de charger la consultation';
        this.isLoading = false;
      }
    });
  }

  toggleDossierPanel(): void {
    this.showDossierPanel = !this.showDossierPanel;
  }

  onConsultationCompleted(): void {
    this.router.navigate(['/medecin/consultations']);
  }

  onConsultationCancelled(): void {
    this.router.navigate(['/medecin/dashboard']);
  }

  goBack(): void {
    this.router.navigate(['/medecin/dashboard']);
  }
}
