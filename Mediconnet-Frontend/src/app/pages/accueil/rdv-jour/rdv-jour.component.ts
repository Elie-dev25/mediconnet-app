import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { LucideAngularModule, DashboardLayoutComponent, ALL_ICONS_PROVIDER } from '../../../shared';
import { ACCUEIL_MENU_ITEMS, ACCUEIL_SIDEBAR_TITLE } from '../shared';
import { AccueilService, RdvAccueilDto } from '../../../services/accueil.service';

@Component({
  selector: 'app-rdv-jour',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    LucideAngularModule,
    DashboardLayoutComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './rdv-jour.component.html',
  styleUrls: ['./rdv-jour.component.scss']
})
export class RdvJourComponent implements OnInit {
  menuItems = ACCUEIL_MENU_ITEMS;
  sidebarTitle = ACCUEIL_SIDEBAR_TITLE;

  rdvs: RdvAccueilDto[] = [];
  rdvsFiltres: RdvAccueilDto[] = [];
  loading = true;
  error: string | null = null;

  // Filtres
  filtreStatut = 'tous';
  filtreRecherche = '';

  constructor(private accueilService: AccueilService) {}

  ngOnInit(): void {
    this.loadRdvs();
  }

  loadRdvs(): void {
    this.loading = true;
    this.error = null;

    this.accueilService.getRdvAujourdHui().subscribe({
      next: (rdvs) => {
        this.rdvs = rdvs;
        this.appliquerFiltres();
        this.loading = false;
      },
      error: (err) => {
        console.error('Erreur chargement RDV:', err);
        this.error = 'Erreur lors du chargement des rendez-vous';
        this.loading = false;
      }
    });
  }

  appliquerFiltres(): void {
    let resultat = [...this.rdvs];

    // Filtre par statut
    if (this.filtreStatut !== 'tous') {
      if (this.filtreStatut === 'en_attente') {
        resultat = resultat.filter(r => !r.patientArrive && r.statut === 'planifie');
      } else if (this.filtreStatut === 'arrive') {
        resultat = resultat.filter(r => r.patientArrive);
      }
    }

    // Filtre par recherche
    if (this.filtreRecherche.trim()) {
      const terme = this.filtreRecherche.toLowerCase();
      resultat = resultat.filter(r =>
        r.patientNom.toLowerCase().includes(terme) ||
        r.patientPrenom.toLowerCase().includes(terme) ||
        r.medecinNom.toLowerCase().includes(terme) ||
        r.medecinPrenom.toLowerCase().includes(terme)
      );
    }

    this.rdvsFiltres = resultat;
  }

  marquerArrivee(rdv: RdvAccueilDto): void {
    this.accueilService.marquerArriveeRdv(rdv.idRendezVous).subscribe({
      next: () => {
        rdv.patientArrive = true;
        rdv.statut = 'confirme';
        this.appliquerFiltres();
      },
      error: (err) => console.error('Erreur:', err)
    });
  }

  formatHeure(dateHeure: string): string {
    return new Date(dateHeure).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
  }

  getStatutClass(statut: string): string {
    const classes: Record<string, string> = {
      'planifie': 'statut-planifie',
      'confirme': 'statut-confirme',
      'en_cours': 'statut-en-cours',
      'termine': 'statut-termine',
      'annule': 'statut-annule'
    };
    return classes[statut] || '';
  }

  getStatutLabel(statut: string): string {
    const labels: Record<string, string> = {
      'planifie': 'En attente',
      'confirme': 'Arrive',
      'en_cours': 'En consultation',
      'termine': 'Termine',
      'annule': 'Annule'
    };
    return labels[statut] || statut;
  }

  get statsRdvs() {
    return {
      total: this.rdvs.length,
      enAttente: this.rdvs.filter(r => r.statut === 'planifie' && !r.patientArrive).length,
      arrives: this.rdvs.filter(r => r.patientArrive).length,
      termines: this.rdvs.filter(r => r.statut === 'termine').length
    };
  }

  rafraichir(): void {
    this.loadRdvs();
  }
}
