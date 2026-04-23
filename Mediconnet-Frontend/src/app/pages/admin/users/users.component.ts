import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { trigger, transition, style, animate } from '@angular/animations';
import { finalize } from 'rxjs/operators';
import { DashboardLayoutComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { UserService, CreateUserRequest, UserDto, UserDetailsDto, Specialite, ServiceDto, SpecialiteInfirmierDto, HistoriqueAffectationsDto } from '../../../services/user.service';
import { ADMIN_MENU_ITEMS, ADMIN_SIDEBAR_TITLE } from '../shared';
import { PatientAssurancePanelComponent, PatientBasicInfo } from '../../../shared/components/patient-assurance-panel/patient-assurance-panel.component';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, LucideAngularModule, DashboardLayoutComponent, PatientAssurancePanelComponent],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss',
  animations: [
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('200ms ease-out', style({ opacity: 1 }))
      ]),
      transition(':leave', [
        animate('150ms ease-in', style({ opacity: 0 }))
      ])
    ]),
    trigger('slideIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'scale(0.95)' }),
        animate('200ms ease-out', style({ opacity: 1, transform: 'scale(1)' }))
      ])
    ])
  ]
})
export class UsersComponent implements OnInit {
  // Menu partagé pour toutes les pages admin
  menuItems = ADMIN_MENU_ITEMS;
  sidebarTitle = ADMIN_SIDEBAR_TITLE;

  users: UserDto[] = [];
  specialites: Specialite[] = [];
  specialitesInfirmiers: SpecialiteInfirmierDto[] = [];
  services: ServiceDto[] = [];
  laboratoires: any[] = [];
  
  showCreateModal = false;
  showPassword = false;
  isLoading = false;
  isSubmitting = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  userForm!: FormGroup;
  searchQuery = '';
  selectedRole = '';
  
  // Onglet actif: 'personnel' ou 'patients'
  activeTab: 'personnel' | 'patients' = 'personnel';

  // Sidebar fiche utilisateur
  showUserSidebar = false;
  selectedUserDetails: UserDetailsDto | null = null;
  isLoadingDetails = false;

  // Sidebar nomination Major
  showNominationSidebar = false;
  selectedServiceForNomination: number | null = null;
  isNominating = false;

  // Actions en cours
  isUpdatingStatut = false;

  // Sidebar assurance patient
  showAssurancePanel = false;
  selectedPatientForAssurance: PatientBasicInfo | null = null;

  // Sidebar changement de service
  showChangeServiceSidebar = false;
  selectedServiceForChange: number | null = null;
  motifChangementService = '';
  isChangingService = false;
  historiqueAffectations: HistoriqueAffectationsDto | null = null;
  isLoadingHistorique = false;

  // Rôles du personnel (sans patient)
  personnelRoles = [
    { value: 'medecin', label: 'Médecin', icon: 'stethoscope' },
    { value: 'infirmier', label: 'Infirmier', icon: 'syringe' },
    { value: 'administrateur', label: 'Administrateur', icon: 'shield' },
    { value: 'caissier', label: 'Caissier', icon: 'wallet' },
    { value: 'accueil', label: 'Accueil', icon: 'users' },
    { value: 'pharmacien', label: 'Pharmacien', icon: 'pill' },
    { value: 'laborantin', label: 'Laborantin', icon: 'flask-conical' }
  ];

  // Tous les rôles (pour le formulaire de création)
  roles = [
    { value: 'medecin', label: 'Médecin', icon: 'stethoscope' },
    { value: 'infirmier', label: 'Infirmier', icon: 'syringe' },
    { value: 'administrateur', label: 'Administrateur', icon: 'shield' },
    { value: 'caissier', label: 'Caissier', icon: 'wallet' },
    { value: 'accueil', label: 'Accueil', icon: 'users' },
    { value: 'pharmacien', label: 'Pharmacien', icon: 'pill' },
    { value: 'laborantin', label: 'Laborantin', icon: 'flask-conical' }
  ];

  constructor(
    private fb: FormBuilder,
    private userService: UserService
  ) {
    this.initForm();
  }

  ngOnInit(): void {
    this.loadUsers();
    this.loadSpecialites();
    this.loadSpecialitesInfirmiers();
    this.loadServices();
    this.loadLaboratoires();
  }

  private initForm(): void {
    this.userForm = this.fb.group({
      nom: ['', [Validators.required, Validators.minLength(2)]],
      prenom: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      telephone: ['', [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      role: ['', [Validators.required]],
      // Champs specifiques medecin
      idSpecialite: [null],
      idService: [null],
      numeroOrdre: [''],
      // Champs specifiques infirmier (idService aussi utilisé)
      matricule: [''],
      idSpecialiteInfirmier: [null],
      // Champs specifiques laborantin
      idLabo: [null],
      specialisation: ['']
    });

    // Ecouter les changements de role
    this.userForm.get('role')?.valueChanges.subscribe(role => {
      this.updateValidators(role);
    });
  }

  private updateValidators(role: string): void {
    const idSpecialite = this.userForm.get('idSpecialite');
    const idService = this.userForm.get('idService');
    const idLabo = this.userForm.get('idLabo');
    const numeroOrdre = this.userForm.get('numeroOrdre');
    const idSpecialiteInfirmier = this.userForm.get('idSpecialiteInfirmier');
    const matricule = this.userForm.get('matricule');
    const specialisation = this.userForm.get('specialisation');
    
    // Reset all validators
    idSpecialite?.clearValidators();
    idService?.clearValidators();
    idLabo?.clearValidators();
    numeroOrdre?.clearValidators();
    idSpecialiteInfirmier?.clearValidators();
    matricule?.clearValidators();
    specialisation?.clearValidators();
    
    if (role === 'medecin') {
      idSpecialite?.setValidators([Validators.required]);
      idService?.setValidators([Validators.required]);
      numeroOrdre?.setValidators([Validators.required]);
    } else if (role === 'infirmier') {
      idService?.setValidators([Validators.required]);
      idSpecialiteInfirmier?.setValidators([Validators.required]);
      matricule?.setValidators([Validators.required]);
    } else if (role === 'laborantin') {
      idLabo?.setValidators([Validators.required]);
      specialisation?.setValidators([Validators.required]);
      matricule?.setValidators([Validators.required]);
    }
    
    idSpecialite?.updateValueAndValidity();
    idService?.updateValueAndValidity();
    idLabo?.updateValueAndValidity();
    numeroOrdre?.updateValueAndValidity();
    idSpecialiteInfirmier?.updateValueAndValidity();
    matricule?.updateValueAndValidity();
    specialisation?.updateValueAndValidity();
  }

  loadUsers(): void {
    this.isLoading = true;
    console.log('[UsersComponent] Loading users...');
    this.userService.getUsers().pipe(
      finalize(() => {
        this.isLoading = false;
      })
    ).subscribe({
      next: (users) => {
        console.log('[UsersComponent] Users loaded:', users?.length, users);
        this.users = users;
      },
      error: (err) => {
        console.error('[UsersComponent] Error loading users:', err);
      }
    });
  }

  loadSpecialites(): void {
    this.userService.getSpecialites().subscribe({
      next: (specialites) => this.specialites = specialites,
      error: (err) => console.error('Error loading specialites:', err)
    });
  }

  loadSpecialitesInfirmiers(): void {
    this.userService.getSpecialitesInfirmiers().subscribe({
      next: (specialites) => this.specialitesInfirmiers = specialites,
      error: (err) => console.error('Error loading specialites infirmiers:', err)
    });
  }

  loadServices(): void {
    this.userService.getServices().subscribe({
      next: (services) => this.services = services,
      error: (err) => console.error('Error loading services:', err)
    });
  }

  loadLaboratoires(): void {
    this.userService.getLaboratoires().subscribe({
      next: (laboratoires) => this.laboratoires = laboratoires,
      error: (err) => console.error('Error loading laboratoires:', err)
    });
  }

  openCreateModal(): void {
    this.showCreateModal = true;
    this.userForm.reset();
    this.errorMessage = null;
    this.successMessage = null;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
    this.userForm.reset();
    this.errorMessage = null;
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    if (this.userForm.invalid || this.isSubmitting) {
      // Marquer tous les champs comme touched pour afficher les erreurs
      Object.keys(this.userForm.controls).forEach(key => {
        this.userForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = null;

    const formValue = this.userForm.value;
    const request: CreateUserRequest = {
      nom: formValue.nom,
      prenom: formValue.prenom,
      email: formValue.email,
      telephone: formValue.telephone,
      password: formValue.password,
      role: formValue.role,
      idSpecialite: formValue.role === 'medecin' ? formValue.idSpecialite : undefined,
      idService: formValue.role === 'medecin' ? formValue.idService : undefined,
      numeroOrdre: formValue.role === 'medecin' ? formValue.numeroOrdre : undefined,
      matricule: formValue.role === 'infirmier' ? formValue.matricule : undefined
    };

    this.userService.createUser(request).pipe(
      finalize(() => {
        this.isSubmitting = false;
      })
    ).subscribe({
      next: () => {
        this.successMessage = 'Utilisateur cree avec succes';
        this.loadUsers();
        setTimeout(() => {
          this.closeCreateModal();
          this.successMessage = null;
        }, 1500);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Erreur lors de la creation';
      }
    });
  }

  getRoleLabel(role: string): string {
    const labels: Record<string, string> = {
      'patient': 'Patient',
      'medecin': 'Medecin',
      'infirmier': 'Infirmier',
      'administrateur': 'Administrateur',
      'caissier': 'Caissier'
    };
    return labels[role] || role;
  }

  getRoleIcon(role: string): string {
    const icons: Record<string, string> = {
      'patient': 'user',
      'medecin': 'stethoscope',
      'infirmier': 'syringe',
      'administrateur': 'shield',
      'caissier': 'wallet'
    };
    return icons[role] || 'user';
  }

  // Getter pour les rôles de filtre selon l'onglet actif
  get filterRoles() {
    return this.activeTab === 'personnel' ? this.personnelRoles : [];
  }

  // Getter pour le placeholder du filtre
  get filterPlaceholder(): string {
    return this.activeTab === 'personnel' ? 'Tout le personnel' : 'Tous les patients';
  }

  // Changer d'onglet
  setActiveTab(tab: 'personnel' | 'patients'): void {
    this.activeTab = tab;
    this.selectedRole = ''; // Reset le filtre de rôle
    this.searchQuery = ''; // Reset la recherche
  }

  // Compter les utilisateurs par catégorie
  get personnelCount(): number {
    return this.users.filter(u => u.role !== 'patient').length;
  }

  get patientsCount(): number {
    return this.users.filter(u => u.role === 'patient').length;
  }

  get filteredUsers(): UserDto[] {
    return this.users.filter(user => {
      // Filtrer par onglet actif
      const matchesTab = this.activeTab === 'personnel' 
        ? user.role !== 'patient' 
        : user.role === 'patient';
      
      const matchesSearch = !this.searchQuery || 
        user.nom.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        user.prenom.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        user.email.toLowerCase().includes(this.searchQuery.toLowerCase());
      
      const matchesRole = !this.selectedRole || user.role === this.selectedRole;
      
      return matchesTab && matchesSearch && matchesRole;
    });
  }

  hasError(fieldName: string): boolean {
    const field = this.userForm.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  // ==================== SIDEBAR FICHE UTILISATEUR ====================

  openUserSidebar(user: UserDto): void {
    this.showUserSidebar = true;
    this.isLoadingDetails = true;
    this.selectedUserDetails = null;

    this.userService.getUserDetails(user.idUser).pipe(
      finalize(() => this.isLoadingDetails = false)
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.selectedUserDetails = response.data;
        }
      },
      error: (err) => {
        console.error('Error loading user details:', err);
        this.selectedUserDetails = null;
      }
    });
  }

  closeUserSidebar(): void {
    this.showUserSidebar = false;
    this.selectedUserDetails = null;
    this.showNominationSidebar = false;
  }

  // ==================== GESTION STATUT INFIRMIER ====================

  updateInfirmierStatut(statut: string): void {
    if (!this.selectedUserDetails || this.isUpdatingStatut) return;

    this.isUpdatingStatut = true;
    this.userService.updateInfirmierStatut(this.selectedUserDetails.idUser, statut).pipe(
      finalize(() => this.isUpdatingStatut = false)
    ).subscribe({
      next: (response) => {
        if (response.success) {
          // Recharger les détails
          this.refreshUserDetails();
          this.successMessage = response.message;
          setTimeout(() => this.successMessage = null, 3000);
        }
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Erreur lors de la mise à jour du statut';
        setTimeout(() => this.errorMessage = null, 3000);
      }
    });
  }

  // ==================== NOMINATION MAJOR ====================

  openNominationSidebar(): void {
    this.showNominationSidebar = true;
    this.selectedServiceForNomination = null;
  }

  closeNominationSidebar(): void {
    this.showNominationSidebar = false;
    this.selectedServiceForNomination = null;
  }

  confirmNomination(): void {
    if (!this.selectedUserDetails || !this.selectedServiceForNomination || this.isNominating) return;

    this.isNominating = true;
    this.userService.nommerInfirmierMajor(this.selectedUserDetails.idUser, this.selectedServiceForNomination).pipe(
      finalize(() => this.isNominating = false)
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.closeNominationSidebar();
          this.refreshUserDetails();
          this.loadUsers(); // Rafraîchir la liste
          this.successMessage = response.message;
          setTimeout(() => this.successMessage = null, 3000);
        }
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Erreur lors de la nomination';
        setTimeout(() => this.errorMessage = null, 3000);
      }
    });
  }

  revoquerMajor(): void {
    if (!this.selectedUserDetails || this.isNominating) return;

    this.isNominating = true;
    this.userService.revoquerInfirmierMajor(this.selectedUserDetails.idUser).pipe(
      finalize(() => this.isNominating = false)
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.refreshUserDetails();
          this.loadUsers();
          this.successMessage = response.message;
          setTimeout(() => this.successMessage = null, 3000);
        }
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Erreur lors de la révocation';
        setTimeout(() => this.errorMessage = null, 3000);
      }
    });
  }

  private refreshUserDetails(): void {
    if (!this.selectedUserDetails) return;
    
    this.userService.getUserDetails(this.selectedUserDetails.idUser).subscribe({
      next: (response) => {
        if (response.success) {
          this.selectedUserDetails = response.data;
        }
      }
    });
  }

  getStatutClass(statut: string): string {
    switch (statut) {
      case 'actif': return 'statut-actif';
      case 'bloque': return 'statut-bloque';
      case 'suspendu': return 'statut-suspendu';
      default: return '';
    }
  }

  getStatutLabel(statut: string): string {
    switch (statut) {
      case 'actif': return 'Actif';
      case 'bloque': return 'Bloqué';
      case 'suspendu': return 'Suspendu';
      default: return statut;
    }
  }

  // ==================== ASSURANCE PATIENT ====================

  openAssurancePanel(user: UserDto): void {
    this.selectedPatientForAssurance = {
      idPatient: user.idUser,
      nomComplet: `${user.prenom} ${user.nom}`,
      telephone: user.telephone,
      email: user.email
    };
    this.showAssurancePanel = true;
  }

  closeAssurancePanel(): void {
    this.showAssurancePanel = false;
    this.selectedPatientForAssurance = null;
  }

  onAssuranceSaved(): void {
    this.loadUsers();
  }

  // ==================== CHANGEMENT DE SERVICE ====================

  openChangeServiceSidebar(): void {
    if (!this.selectedUserDetails) return;
    
    // Initialiser avec le service actuel
    if (this.selectedUserDetails.role === 'medecin' && this.selectedUserDetails.medecin) {
      this.selectedServiceForChange = this.selectedUserDetails.medecin.idService || null;
    } else if (this.selectedUserDetails.role === 'infirmier' && this.selectedUserDetails.infirmier) {
      this.selectedServiceForChange = this.selectedUserDetails.infirmier.idService || null;
    }
    
    this.motifChangementService = '';
    this.showChangeServiceSidebar = true;
    this.loadHistoriqueAffectations();
  }

  closeChangeServiceSidebar(): void {
    this.showChangeServiceSidebar = false;
    this.selectedServiceForChange = null;
    this.motifChangementService = '';
    this.historiqueAffectations = null;
  }

  loadHistoriqueAffectations(): void {
    if (!this.selectedUserDetails) return;

    this.isLoadingHistorique = true;
    const userId = this.selectedUserDetails.idUser;
    const role = this.selectedUserDetails.role;

    const request$ = role === 'medecin' 
      ? this.userService.getHistoriqueAffectationsMedecin(userId)
      : this.userService.getHistoriqueAffectationsInfirmier(userId);

    request$.pipe(
      finalize(() => this.isLoadingHistorique = false)
    ).subscribe({
      next: (historique) => {
        this.historiqueAffectations = historique;
      },
      error: (err) => {
        console.error('Erreur chargement historique:', err);
        // L'historique peut être vide si aucune affectation n'existe encore
        this.historiqueAffectations = null;
      }
    });
  }

  changerService(): void {
    if (!this.selectedUserDetails || !this.selectedServiceForChange || this.isChangingService) return;

    // Vérifier que le service a changé
    const serviceActuel = this.selectedUserDetails.role === 'medecin' 
      ? this.selectedUserDetails.medecin?.idService 
      : this.selectedUserDetails.infirmier?.idService;

    if (this.selectedServiceForChange === serviceActuel) {
      this.errorMessage = 'Veuillez sélectionner un service différent';
      setTimeout(() => this.errorMessage = null, 3000);
      return;
    }

    this.isChangingService = true;
    const userId = this.selectedUserDetails.idUser;
    const role = this.selectedUserDetails.role;

    const request$ = role === 'medecin'
      ? this.userService.changerServiceMedecin(userId, {
          idNouveauService: this.selectedServiceForChange,
          motif: this.motifChangementService || undefined
        })
      : this.userService.changerServiceInfirmier(userId, {
          idNouveauService: this.selectedServiceForChange,
          motif: this.motifChangementService || undefined
        });

    request$.pipe(
      finalize(() => this.isChangingService = false)
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.successMessage = response.message;
          setTimeout(() => this.successMessage = null, 3000);
          this.refreshUserDetails();
          this.loadHistoriqueAffectations();
          this.loadUsers();
        } else {
          this.errorMessage = response.message;
          setTimeout(() => this.errorMessage = null, 3000);
        }
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Erreur lors du changement de service';
        setTimeout(() => this.errorMessage = null, 3000);
      }
    });
  }

  getServiceName(idService: number): string {
    const service = this.services.find(s => s.idService === idService);
    return service?.nomService || 'Service inconnu';
  }
}
