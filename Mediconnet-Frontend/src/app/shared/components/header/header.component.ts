import { Component, Input, Output, EventEmitter, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { 
  LucideAngularModule, 
  LUCIDE_ICONS, 
  LucideIconProvider,
  Menu, HeartPulse, LogOut, User, ChevronDown, Settings, Bell
} from 'lucide-angular';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  providers: [
    { 
      provide: LUCIDE_ICONS, 
      useValue: new LucideIconProvider({ Menu, HeartPulse, LogOut, User, ChevronDown, Settings, Bell })
    }
  ],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  @Input() userName = '';
  @Input() userRole = '';
  @Output() toggleSidebar = new EventEmitter<void>();
  @Output() logoutClick = new EventEmitter<void>();
  @Output() profileClick = new EventEmitter<void>();

  isDropdownOpen = false;

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.user-menu')) {
      this.isDropdownOpen = false;
    }
  }

  toggleDropdown(): void {
    this.isDropdownOpen = !this.isDropdownOpen;
  }

  onProfileClick(): void {
    this.isDropdownOpen = false;
    this.profileClick.emit();
  }

  onLogout(): void {
    this.isDropdownOpen = false;
    this.logoutClick.emit();
  }

  get userInitials(): string {
    return this.userName.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2) || '??';
  }

  get userRoleLabel(): string {
    const labels: Record<string, string> = {
      'patient': 'Patient',
      'medecin': 'Médecin',
      'infirmier': 'Infirmier',
      'administrateur': 'Administrateur',
      'caissier': 'Caissier'
    };
    return labels[this.userRole] || this.userRole;
  }
}
