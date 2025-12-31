import { Component, EventEmitter, Output, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService, ChangePasswordRequest } from '../../../services/auth.service';
import { PasswordStrengthIndicatorComponent } from '../password-strength-indicator/password-strength-indicator.component';
import { ALL_ICONS_PROVIDER } from '../../icons';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule, PasswordStrengthIndicatorComponent],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChangePasswordComponent {
  @Output() passwordChanged = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  changePasswordForm: FormGroup;
  isLoading = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;
  
  showCurrentPassword = false;
  showNewPassword = false;
  showConfirmPassword = false;
  
  newPasswordValue = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {
    this.changePasswordForm = this.fb.group({
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmNewPassword: ['', [Validators.required]]
    });
  }

  get currentPassword() {
    return this.changePasswordForm.get('currentPassword');
  }

  get newPassword() {
    return this.changePasswordForm.get('newPassword');
  }

  get confirmNewPassword() {
    return this.changePasswordForm.get('confirmNewPassword');
  }

  onNewPasswordInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.newPasswordValue = input.value;
  }

  toggleCurrentPasswordVisibility(): void {
    this.showCurrentPassword = !this.showCurrentPassword;
  }

  toggleNewPasswordVisibility(): void {
    this.showNewPassword = !this.showNewPassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  passwordMismatch(): boolean {
    const newPass = this.newPassword?.value;
    const confirmPass = this.confirmNewPassword?.value;
    return newPass && confirmPass && newPass !== confirmPass && this.confirmNewPassword?.touched;
  }

  onSubmit(): void {
    if (this.changePasswordForm.invalid || this.isLoading) {
      return;
    }

    if (this.passwordMismatch()) {
      this.errorMessage = 'Les nouveaux mots de passe ne correspondent pas';
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;
    this.successMessage = null;

    const request: ChangePasswordRequest = {
      currentPassword: this.currentPassword?.value,
      newPassword: this.newPassword?.value,
      confirmNewPassword: this.confirmNewPassword?.value
    };

    this.authService.changePassword(request).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          this.successMessage = response.message;
          this.changePasswordForm.reset();
          this.newPasswordValue = '';
          this.passwordChanged.emit();
        } else {
          this.errorMessage = response.message;
        }
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.message || 'Erreur lors du changement de mot de passe';
        this.cdr.markForCheck();
      }
    });
  }

  onCancel(): void {
    this.changePasswordForm.reset();
    this.newPasswordValue = '';
    this.errorMessage = null;
    this.successMessage = null;
    this.cancelled.emit();
  }
}
