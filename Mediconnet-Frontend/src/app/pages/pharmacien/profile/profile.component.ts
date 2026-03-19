import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DashboardLayoutComponent, LucideAngularModule, ALL_ICONS_PROVIDER } from '../../../shared';
import { PharmacieStockService, PharmacieProfileDto, PharmacieDashboardDto, UpdatePharmacieProfileRequest } from '../../../services/pharmacie-stock.service';
import { PHARMACIEN_MENU_ITEMS, PHARMACIEN_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-pharmacien-profile',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, LucideAngularModule, DashboardLayoutComponent],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class PharmacienProfileComponent implements OnInit {
  // Menu partagé pour toutes les pages pharmacien
  menuItems = PHARMACIEN_MENU_ITEMS;
  sidebarTitle = PHARMACIEN_SIDEBAR_TITLE;

  profile: PharmacieProfileDto | null = null;
  isLoading = true;
  isEditing = false;
  editedProfile: Partial<PharmacieProfileDto> = {};

  // Stats du pharmacien (données réelles)
  stats = {
    totalMedicaments: 0,
    commandesMonth: 0,
    ordonnancesToday: 0,
    fournisseursActifs: 0
  };

  constructor(private pharmacieService: PharmacieStockService) {}

  ngOnInit(): void {
    this.loadProfile();
    this.loadStats();
  }

  loadProfile(): void {
    this.isLoading = true;
    this.pharmacieService.getProfile().subscribe({
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
    this.pharmacieService.getDashboard().subscribe({
      next: (data: PharmacieDashboardDto) => {
        this.stats = {
          totalMedicaments: data.totalMedicaments,
          commandesMonth: data.commandesMois,
          ordonnancesToday: data.ordonnancesAujourdHui,
          fournisseursActifs: data.fournisseursActifs
        };
      },
      error: (err) => console.error('Erreur chargement stats:', err)
    });
  }

  getFullName(): string {
    if (!this.profile) return '';
    return `Ph. ${this.profile.prenom} ${this.profile.nom}`;
  }

  getAvatarUrl(): string {
    if (this.profile?.photo) return this.profile.photo;
    const name = this.profile ? `${this.profile.prenom}+${this.profile.nom}` : 'Pharmacien';
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
    if (this.profile && this.editedProfile) {
      const updateRequest: UpdatePharmacieProfileRequest = {
        telephone: this.editedProfile.telephone,
        photo: this.editedProfile.photo,
        specialite: this.editedProfile.specialite,
        numeroLicence: this.editedProfile.numeroLicence,
        pharmacieNom: this.editedProfile.pharmacieNom
      };

      this.pharmacieService.updateProfile(updateRequest).subscribe({
        next: (updatedProfile) => {
          this.profile = updatedProfile;
          this.isEditing = false;
          this.editedProfile = {};
          // Optionnel: afficher un message de succès
          console.log('Profil mis à jour avec succès');
        },
        error: (err) => {
          console.error('Erreur mise à jour profil:', err);
          // Optionnel: afficher un message d'erreur
        }
      });
    }
  }
}
