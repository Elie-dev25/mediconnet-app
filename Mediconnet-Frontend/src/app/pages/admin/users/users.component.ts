import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { trigger, transition, style, animate } from '@angular/animations';
import { finalize } from 'rxjs/operators';
import { DashboardLayoutComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { UserService, CreateUserRequest, UserDto, Specialite, ServiceDto } from '../../../services/user.service';
import { ADMIN_MENU_ITEMS, ADMIN_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, LucideAngularModule, DashboardLayoutComponent],
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
  services: ServiceDto[] = [];
  
  showCreateModal = false;
  showPassword = false;
  isLoading = false;
  isSubmitting = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  userForm!: FormGroup;
  searchQuery = '';
  selectedRole = '';

  roles = [
    { value: 'patient', label: 'Patient', icon: 'user' },
    { value: 'medecin', label: 'Médecin', icon: 'stethoscope' },
    { value: 'infirmier', label: 'Infirmier', icon: 'syringe' },
    { value: 'administrateur', label: 'Administrateur', icon: 'shield' },
    { value: 'caissier', label: 'Caissier', icon: 'wallet' },
    { value: 'accueil', label: 'Accueil', icon: 'users' },
    { value: 'pharmacien', label: 'Pharmacien', icon: 'pill' },
    { value: 'biologiste', label: 'Biologiste', icon: 'flask-conical' }
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
    this.loadServices();
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
      // Champs specifiques infirmier
      matricule: ['']
    });

    // Ecouter les changements de role
    this.userForm.get('role')?.valueChanges.subscribe(role => {
      this.updateValidators(role);
    });
  }

  private updateValidators(role: string): void {
    const idSpecialite = this.userForm.get('idSpecialite');
    const idService = this.userForm.get('idService');
    
    if (role === 'medecin') {
      idSpecialite?.setValidators([Validators.required]);
      idService?.setValidators([Validators.required]);
    } else {
      idSpecialite?.clearValidators();
      idService?.clearValidators();
    }
    
    idSpecialite?.updateValueAndValidity();
    idService?.updateValueAndValidity();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.userService.getUsers().pipe(
      finalize(() => {
        this.isLoading = false;
      })
    ).subscribe({
      next: (users) => {
        this.users = users;
      },
      error: (err) => {
        console.error('Error loading users:', err);
      }
    });
  }

  loadSpecialites(): void {
    this.userService.getSpecialites().subscribe({
      next: (specialites) => this.specialites = specialites,
      error: (err) => console.error('Error loading specialites:', err)
    });
  }

  loadServices(): void {
    this.userService.getServices().subscribe({
      next: (services) => this.services = services,
      error: (err) => console.error('Error loading services:', err)
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

  get filteredUsers(): UserDto[] {
    return this.users.filter(user => {
      const matchesSearch = !this.searchQuery || 
        user.nom.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        user.prenom.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        user.email.toLowerCase().includes(this.searchQuery.toLowerCase());
      
      const matchesRole = !this.selectedRole || user.role === this.selectedRole;
      
      return matchesSearch && matchesRole;
    });
  }

  hasError(fieldName: string): boolean {
    const field = this.userForm.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }
}
