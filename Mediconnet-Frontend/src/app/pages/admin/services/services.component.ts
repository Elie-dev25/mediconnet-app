import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { trigger, transition, style, animate } from '@angular/animations';
import { finalize } from 'rxjs/operators';
import { DashboardLayoutComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { AdminService, ServiceDto, Responsable } from '../../../services/admin.service';
import { ADMIN_MENU_ITEMS, ADMIN_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-admin-services',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule,
    ReactiveFormsModule,
    LucideAngularModule, 
    DashboardLayoutComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './services.component.html',
  styleUrl: './services.component.scss',
  animations: [
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('200ms ease-out', style({ opacity: 1 }))
      ])
    ]),
    trigger('slideIn', [
      transition(':enter', [
        style({ transform: 'translateY(-20px)', opacity: 0 }),
        animate('300ms ease-out', style({ transform: 'translateY(0)', opacity: 1 }))
      ])
    ])
  ]
})
export class AdminServicesComponent implements OnInit {
  // Menu partagé pour toutes les pages admin
  menuItems = ADMIN_MENU_ITEMS;
  sidebarTitle = ADMIN_SIDEBAR_TITLE;

  services: ServiceDto[] = [];
  responsables: Responsable[] = [];
  isLoading = true;
  searchQuery = '';

  // Modal
  showModal = false;
  isEditMode = false;
  serviceForm!: FormGroup;
  isSubmitting = false;
  errorMessage = '';
  successMessage = '';
  currentServiceId: number | null = null;

  // Confirmation suppression
  showDeleteConfirm = false;
  serviceToDelete: ServiceDto | null = null;

  constructor(
    private adminService: AdminService,
    private fb: FormBuilder
  ) {
    this.initForm();
  }

  ngOnInit(): void {
    this.loadServices();
    this.loadResponsables();
  }

  initForm(): void {
    this.serviceForm = this.fb.group({
      nomService: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(150)]],
      description: ['', [Validators.maxLength(500)]],
      responsableId: [null]
    });
  }

  loadServices(): void {
    this.isLoading = true;
    this.adminService.getServices().pipe(
      finalize(() => {
        this.isLoading = false;
      })
    ).subscribe({
      next: (services) => {
        this.services = services;
      },
      error: (err) => {
        console.error('Erreur chargement services:', err);
      }
    });
  }

  loadResponsables(): void {
    this.adminService.getResponsables().subscribe({
      next: (responsables) => {
        this.responsables = responsables;
      },
      error: (err) => {
        console.error('Erreur chargement responsables:', err);
      }
    });
  }

  get filteredServices(): ServiceDto[] {
    if (!this.searchQuery.trim()) return this.services;
    const query = this.searchQuery.toLowerCase();
    return this.services.filter(s => 
      s.nomService.toLowerCase().includes(query) ||
      (s.description && s.description.toLowerCase().includes(query)) ||
      (s.responsableNom && s.responsableNom.toLowerCase().includes(query))
    );
  }

  // Modal actions
  openCreateModal(): void {
    this.isEditMode = false;
    this.currentServiceId = null;
    this.serviceForm.reset();
    this.errorMessage = '';
    this.successMessage = '';
    this.showModal = true;
  }

  openEditModal(service: ServiceDto): void {
    this.isEditMode = true;
    this.currentServiceId = service.idService;
    this.serviceForm.patchValue({
      nomService: service.nomService,
      description: service.description || '',
      responsableId: service.responsableId || null
    });
    this.errorMessage = '';
    this.successMessage = '';
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.serviceForm.reset();
    this.errorMessage = '';
    this.successMessage = '';
  }

  onSubmit(): void {
    if (this.isSubmitting) {
      return;
    }

    if (this.serviceForm.invalid) {
      this.serviceForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    const formData = this.serviceForm.value;

    if (this.isEditMode && this.currentServiceId) {
      this.adminService.updateService(this.currentServiceId, formData).pipe(
        finalize(() => {
          this.isSubmitting = false;
        })
      ).subscribe({
        next: (res) => {
          this.successMessage = res.message;
          this.loadServices();
          setTimeout(() => this.closeModal(), 1500);
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Erreur lors de la modification';
        }
      });
    } else {
      this.adminService.createService(formData).pipe(
        finalize(() => {
          this.isSubmitting = false;
        })
      ).subscribe({
        next: (res) => {
          this.successMessage = res.message;
          this.loadServices();
          setTimeout(() => this.closeModal(), 1500);
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Erreur lors de la creation';
        }
      });
    }
  }

  // Suppression
  confirmDelete(service: ServiceDto): void {
    this.serviceToDelete = service;
    this.showDeleteConfirm = true;
  }

  cancelDelete(): void {
    this.serviceToDelete = null;
    this.showDeleteConfirm = false;
  }

  deleteService(): void {
    if (!this.serviceToDelete) return;

    this.adminService.deleteService(this.serviceToDelete.idService).subscribe({
      next: () => {
        this.loadServices();
        this.cancelDelete();
      },
      error: (err) => {
        alert(err.error?.message || 'Erreur lors de la suppression');
        this.cancelDelete();
      }
    });
  }

  hasError(field: string): boolean {
    const control = this.serviceForm.get(field);
    return control ? control.invalid && control.touched : false;
  }
}
