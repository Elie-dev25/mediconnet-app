import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DashboardLayoutComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { MedecinService, MedecinProfileDto, MedecinDashboardDto } from '../../../services/medecin.service';
import { MEDECIN_MENU_ITEMS, MEDECIN_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-medecin-profile',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, LucideAngularModule, DashboardLayoutComponent],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class MedecinProfileComponent implements OnInit {
  // Menu partagé pour toutes les pages médecin
  menuItems = MEDECIN_MENU_ITEMS;
  sidebarTitle = MEDECIN_SIDEBAR_TITLE;

  profile: MedecinProfileDto | null = null;
  isLoading = true;
  isEditing = false;
  editedProfile: Partial<MedecinProfileDto> = {};

  // Stats du médecin (données réelles)
  stats = {
    totalPatients: 0,
    consultationsMonth: 0,
    rdvAujourdHui: 0,
    rdvAVenir: 0
  };

  constructor(private medecinService: MedecinService) {}

  ngOnInit(): void {
    this.loadProfile();
    this.loadStats();
  }

  loadProfile(): void {
    this.isLoading = true;
    this.medecinService.getProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement profil:', err);
        this.isLoading = false;
      }
    });
  }

  loadStats(): void {
    this.medecinService.getDashboard().subscribe({
      next: (data: MedecinDashboardDto) => {
        this.stats = {
          totalPatients: data.totalPatients,
          consultationsMonth: data.consultationsMois,
          rdvAujourdHui: data.rdvAujourdHui,
          rdvAVenir: data.rdvAVenir
        };
      },
      error: (err) => console.error('Erreur chargement stats:', err)
    });
  }

  getFullName(): string {
    if (!this.profile) return '';
    return `Dr. ${this.profile.prenom} ${this.profile.nom}`;
  }

  getAvatarUrl(): string {
    if (this.profile?.photo) return this.profile.photo;
    const name = this.profile ? `${this.profile.prenom}+${this.profile.nom}` : 'Médecin';
    return `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&background=5c6bc0&color=fff&size=200`;
  }

  getMemberSince(): string {
    if (!this.profile?.createdAt) return '';
    const date = new Date(this.profile.createdAt);
    return date.toLocaleDateString('fr-FR', { month: 'long', year: 'numeric' });
  }

  startEditing(): void {
    if (this.profile) {
      this.editedProfile = { ...this.profile };
      this.isEditing = true;
    }
  }

  cancelEditing(): void {
    this.isEditing = false;
    this.editedProfile = {};
  }

  saveProfile(): void {
    // À implémenter : appel API pour sauvegarder
    if (this.profile && this.editedProfile) {
      this.profile = { ...this.profile, ...this.editedProfile };
    }
    this.isEditing = false;
    this.editedProfile = {};
  }
}
