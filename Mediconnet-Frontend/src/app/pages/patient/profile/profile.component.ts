import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { trigger, transition, style, animate } from '@angular/animations';
import { DashboardLayoutComponent, formatDate, formatDateShort, formatTimeRange } from '../../../shared';
import { PatientService, PatientProfile, DossierMedicalDto, PatientDashboardDto, VisiteDto, TraitementDto } from '../../../services/patient.service';
import { AuthService } from '../../../services/auth.service';
import { PATIENT_MENU_ITEMS, PATIENT_SIDEBAR_TITLE } from '../shared';
import { ChangePasswordComponent } from '../../../shared/components/change-password/change-password.component';
import { PhoneInputComponent } from '../../../shared/components/phone-input/phone-input.component';
import { ALL_ICONS_PROVIDER } from '../../../shared/icons';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { Router } from '@angular/router';

type PanelType = 'actions' | 'editProfile' | 'changePassword' | null;

@Component({
  selector: 'app-patient-profile',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule,
    LucideAngularModule, 
    DashboardLayoutComponent, 
    ChangePasswordComponent,
    PhoneInputComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
  animations: [
    trigger('slidePanel', [
      transition(':enter', [
        style({ transform: 'translateX(100%)', opacity: 0 }),
        animate('250ms ease-out', style({ transform: 'translateX(0)', opacity: 1 }))
      ]),
      transition(':leave', [
        animate('200ms ease-in', style({ transform: 'translateX(100%)', opacity: 0 }))
      ])
    ]),
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('200ms ease-out', style({ opacity: 1 }))
      ]),
      transition(':leave', [
        animate('150ms ease-in', style({ opacity: 0 }))
      ])
    ])
  ]
})
export class PatientProfileComponent implements OnInit {
  menuItems = PATIENT_MENU_ITEMS;
  sidebarTitle = PATIENT_SIDEBAR_TITLE;

  loading = true;
  patient: PatientProfile | null = null;
  dossierMedical: DossierMedicalDto | null = null;
  
  // Visites et traitements
  activeTab: 'future' | 'past' | 'treatments' = 'future';
  futureVisits: VisiteDto[] = [];
  pastVisits: VisiteDto[] = [];
  traitements: TraitementDto[] = [];
  
  // Fichiers
  patientFiles: { nom: string; type: string; taille: string; dateAjout: string }[] = [];
  
  // Panneau latéral
  activePanel: PanelType = null;
  
  // Formulaire de modification du profil
  profileForm!: FormGroup;
  isSubmitting = false;
  profileSuccessMessage = '';
  profileErrorMessage = '';
  maxDate: string;

  // Options pour les selects
  readonly groupesSanguins = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'];
  readonly situationsMatrimoniales = ['Célibataire', 'Marié(e)', 'Divorcé(e)', 'Veuf/Veuve', 'Concubinage'];
  readonly regions = [
    'Adamaoua', 'Centre', 'Est', 'Extrême-Nord', 'Littoral',
    'Nord', 'Nord-Ouest', 'Ouest', 'Sud', 'Sud-Ouest'
  ];

  constructor(
    private patientService: PatientService,
    private authService: AuthService,
    private fb: FormBuilder,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    this.maxDate = new Date().toISOString().split('T')[0];
    this.initProfileForm();
  }

  ngOnInit(): void {
    this.loadPatientData();
  }

  private initProfileForm(): void {
    this.profileForm = this.fb.group({
      naissance: [''],
      sexe: [''],
      telephone: ['', Validators.required],
      situationMatrimoniale: [''],
      adresse: [''],
      nationalite: ['Cameroun'],
      regionOrigine: [''],
      profession: [''],
      groupeSanguin: [''],
      nbEnfants: [0],
      personneContact: [''],
      numeroContact: ['']
    });
  }

  private loadPatientData(): void {
    this.loading = true;

    forkJoin({
      profile: this.patientService.getProfile(),
      dossier: this.patientService.getDossierMedical(),
      dashboard: this.patientService.getDashboard()
    }).pipe(
      finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      })
    ).subscribe({
      next: ({ profile, dossier, dashboard }) => {
        this.patient = profile;
        this.dossierMedical = dossier;
        this.futureVisits = dashboard.visitesAVenir || [];
        this.pastVisits = dashboard.visitesPassees || [];
        this.traitements = dashboard.traitementsPrevus || [];
        this.populateProfileForm();
      },
      error: (err) => {
        console.error('Erreur lors du chargement du profil patient:', err);
      }
    });
  }

  private populateProfileForm(): void {
    if (!this.patient) return;
    
    this.profileForm.patchValue({
      naissance: this.patient.naissance?.split('T')[0] || '',
      sexe: this.patient.sexe || '',
      telephone: this.patient.telephone || '',
      situationMatrimoniale: this.patient.situationMatrimoniale || '',
      adresse: this.patient.adresse || '',
      profession: this.patient.profession || '',
      groupeSanguin: this.patient.groupeSanguin || '',
      nbEnfants: this.patient.nbEnfants || 0,
      personneContact: this.patient.personneContact || '',
      numeroContact: this.patient.numeroContact || ''
    });
  }

  // === Gestion des panneaux latéraux ===
  openPanel(panel: PanelType): void {
    this.activePanel = panel;
    if (panel === 'editProfile') {
      this.populateProfileForm();
      this.profileSuccessMessage = '';
      this.profileErrorMessage = '';
    }
  }

  closePanel(): void {
    this.activePanel = null;
  }

  openActionsPanel(): void {
    this.openPanel('actions');
  }

  openEditProfile(): void {
    this.openPanel('editProfile');
  }

  openChangePassword(): void {
    this.openPanel('changePassword');
  }

  // === Soumission du formulaire de profil ===
  onSubmitProfile(): void {
    if (this.profileForm.invalid) {
      this.profileErrorMessage = 'Veuillez corriger les erreurs du formulaire';
      return;
    }

    this.isSubmitting = true;
    this.profileErrorMessage = '';
    this.profileSuccessMessage = '';

    const data = this.profileForm.value;

    this.patientService.updateProfile(data).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.profileSuccessMessage = 'Profil mis à jour avec succès !';
        this.loadPatientData();
        
        setTimeout(() => {
          this.closePanel();
        }, 1500);
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.isSubmitting = false;
        this.profileErrorMessage = error.error?.message || 'Une erreur est survenue';
        this.cdr.markForCheck();
      }
    });
  }

  // === Gestion mot de passe ===
  onPasswordChanged(): void {
    setTimeout(() => {
      this.closePanel();
    }, 1500);
  }

  onPasswordChangeCancelled(): void {
    this.closePanel();
  }

  // === Impression ===
  printDossier(): void {
    this.closePanel();
    setTimeout(() => {
      window.print();
    }, 300);
  }

  // === Déconnexion ===
  logout(): void {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }

  // === Formatage ===
  formatValue(value: any, fallback = 'Non renseigné'): string {
    if (value === null || value === undefined || value === '') return fallback;
    return String(value);
  }

  formatDate(dateStr?: string): string {
    return formatDate(dateStr);
  }

  formatDateShort(dateStr?: string): string {
    return formatDateShort(dateStr);
  }

  formatSexe(sexe?: string): string {
    if (!sexe) return 'Non renseigné';
    return sexe === 'M' ? 'Masculin' : sexe === 'F' ? 'Féminin' : sexe;
  }

  calculateAge(dateStr?: string): string {
    if (!dateStr) return 'Non renseigné';
    try {
      const birth = new Date(dateStr);
      const today = new Date();
      let age = today.getFullYear() - birth.getFullYear();
      const m = today.getMonth() - birth.getMonth();
      if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) {
        age--;
      }
      return `${age} ans`;
    } catch {
      return 'Non renseigné';
    }
  }

  // === Gestion des onglets visites ===
  setActiveTab(tab: 'future' | 'past' | 'treatments'): void {
    this.activeTab = tab;
  }

  get upcomingCount(): number {
    return this.futureVisits.length;
  }

  get pastCount(): number {
    return this.pastVisits.length;
  }

  get traitementsCount(): number {
    return this.traitements.length;
  }

  formatVisiteDate(dateStr: string): string {
    return formatDateShort(dateStr);
  }

  formatVisiteTime(dateStr: string, duree: number): string {
    return formatTimeRange(dateStr, duree);
  }

  // === Getters pour les statistiques ===
  get consultationsCount(): number {
    return this.dossierMedical?.consultations?.length || 0;
  }

  get ordonnancesCount(): number {
    return this.dossierMedical?.ordonnances?.length || 0;
  }

  get examensCount(): number {
    return this.dossierMedical?.examens?.length || 0;
  }

  get allergiesCount(): number {
    return this.dossierMedical?.allergies?.length || 0;
  }

  get antecedentsCount(): number {
    return this.dossierMedical?.antecedents?.length || 0;
  }

  get filesCount(): number {
    return this.patientFiles.length;
  }

  getFileIcon(type: string): string {
    const iconMap: { [key: string]: string } = {
      'pdf': 'file-text',
      'image': 'image',
      'doc': 'file-text',
      'xls': 'file-spreadsheet',
      'zip': 'file-archive',
      'default': 'file'
    };
    return iconMap[type] || iconMap['default'];
  }
}
