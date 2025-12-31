/**
 * @deprecated Ce composant n'est plus utilisé depuis que le profil est complété lors de l'inscription.
 * La modification du profil se fait maintenant via la page dédiée /patient/profile.
 * Ce fichier est conservé pour référence mais ne doit plus être utilisé.
 */
import { Component, EventEmitter, Input, Output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { 
  LucideAngularModule, 
  LUCIDE_ICONS, 
  LucideIconProvider,
  X, Save, User, Phone, MapPin, Calendar, Heart, Users, Briefcase, Droplet, AlertCircle, CheckCircle
} from 'lucide-angular';
import { trigger, transition, style, animate } from '@angular/animations';
import { PatientProfile, PatientProfileService, UpdateProfileRequest } from '../../../services/patient-profile.service';
import { PhoneInputComponent } from '../phone-input/phone-input.component';

/** @deprecated */
@Component({
  selector: 'app-profile-form-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule, PhoneInputComponent],
  providers: [
    { 
      provide: LUCIDE_ICONS, 
      useValue: new LucideIconProvider({ 
        X, Save, User, Phone, MapPin, Calendar, Heart, Users, Briefcase, Droplet, AlertCircle, CheckCircle 
      })
    }
  ],
  templateUrl: './profile-form-modal.component.html',
  styleUrl: './profile-form-modal.component.scss',
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
        style({ opacity: 0, transform: 'translateY(-20px)' }),
        animate('250ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ]),
      transition(':leave', [
        animate('150ms ease-in', style({ opacity: 0, transform: 'translateY(-20px)' }))
      ])
    ])
  ]
})
export class ProfileFormModalComponent implements OnInit {
  @Input() isOpen = false;
  @Input() profile: PatientProfile | null = null;
  
  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  profileForm!: FormGroup;
  isSubmitting = false;
  successMessage = '';
  errorMessage = '';
  maxDate: string;

  constructor(
    private fb: FormBuilder,
    private profileService: PatientProfileService
  ) {
    this.maxDate = new Date().toISOString().split('T')[0];
  }

  ngOnInit(): void {
    this.initForm();
  }

  initForm(): void {
    this.profileForm = this.fb.group({
      naissance: [this.profile?.naissance?.split('T')[0] || '', Validators.required],
      sexe: [this.profile?.sexe || '', Validators.required],
      telephone: [this.profile?.telephone || '', Validators.required],
      situationMatrimoniale: [this.profile?.situationMatrimoniale || ''],
      adresse: [this.profile?.adresse || '', Validators.required],
      ethnie: [this.profile?.ethnie || ''],
      groupeSanguin: [this.profile?.groupeSanguin || ''],
      nbEnfants: [this.profile?.nbEnfants || 0],
      personneContact: [this.profile?.personneContact || '', Validators.required],
      numeroContact: [this.profile?.numeroContact || '', Validators.required],
      profession: [this.profile?.profession || '']
    });
  }

  onOverlayClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-overlay')) {
      this.onClose();
    }
  }

  onClose(): void {
    this.successMessage = '';
    this.errorMessage = '';
    this.close.emit();
  }

  onSubmit(): void {
    if (this.profileForm.invalid) {
      this.errorMessage = 'Veuillez remplir tous les champs obligatoires (*)';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    const data: UpdateProfileRequest = this.profileForm.value;

    this.profileService.updateProfile(data).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.successMessage = 'Profil mis a jour avec succes !';
        
        setTimeout(() => {
          this.saved.emit();
          this.onClose();
        }, 1500);
      },
      error: (error) => {
        this.isSubmitting = false;
        this.errorMessage = error.error?.message || 'Une erreur est survenue';
      }
    });
  }
}
