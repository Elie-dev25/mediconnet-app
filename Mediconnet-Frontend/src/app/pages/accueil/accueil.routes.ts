import { Routes } from '@angular/router';

export const ACCUEIL_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/dashboard.component').then(m => m.AccueilDashboardComponent)
  },
  {
    path: 'enregistrement',
    loadComponent: () => import('./enregistrement/enregistrement.component').then(m => m.EnregistrementComponent)
  },
  {
    path: 'rdv-jour',
    loadComponent: () => import('./rdv-jour/rdv-jour.component').then(m => m.RdvJourComponent)
  },
  {
    path: 'ajouter-patient',
    loadComponent: () => import('./ajouter-patient/ajouter-patient.component').then(m => m.AjouterPatientComponent)
  }
];
