import { Routes } from '@angular/router';
import { LandingComponent } from './pages/auth/landing/landing.component';
import { LoginComponent } from './pages/auth/login/login.component';
import { RegisterComponent } from './pages/auth/register/register.component';
import { EmailVerifiedComponent } from './pages/auth/email-verified/email-verified.component';
import { PatientDashboardComponent } from './pages/patient/dashboard/dashboard.component';
import { AdminDashboardComponent } from './pages/admin/dashboard/dashboard.component';
import { MedecinDashboardComponent } from './pages/medecin/dashboard/dashboard.component';
import { InfirmierDashboardComponent } from './pages/infirmier/dashboard/dashboard.component';
import { CaissierDashboardComponent } from './pages/caissier/dashboard/dashboard.component';
import { authGuard, roleGuard } from './core';

export const routes: Routes = [
  // Page d'accueil
  { path: '', component: LandingComponent },
  
  // Routes d'authentification (publiques)
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'auth/email-verified', component: EmailVerifiedComponent },
  { path: 'auth/login', redirectTo: 'login', pathMatch: 'full' },
  { path: 'auth/register', redirectTo: 'register', pathMatch: 'full' },
  { 
    path: 'auth/change-password', 
    loadComponent: () => import('./pages/auth/change-password/change-password.component').then(m => m.ChangePasswordComponent),
    canActivate: [authGuard]
  },
  
  // Première connexion (pour patients créés par l'accueil)
  { 
    path: 'auth/first-login', 
    loadComponent: () => import('./pages/auth/first-login/first-login.component').then(m => m.FirstLoginComponent),
    canActivate: [authGuard]
  },
  
  // Page de notifications (accessible à tous les utilisateurs connectés)
  { 
    path: 'notifications', 
    loadComponent: () => import('./pages/notifications/notifications.component').then(m => m.NotificationsComponent),
    canActivate: [authGuard]
  },
  
  // @deprecated - Complétion de profil (redirige vers inscription car le profil est maintenant complété lors de l'inscription)
  { 
    path: 'complete-profile', 
    redirectTo: 'register',
    pathMatch: 'full'
  },
  
  // Routes Patient (protegees)
  { 
    path: 'patient', 
    canActivate: [authGuard, roleGuard(['patient'])],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: PatientDashboardComponent },
      { 
        path: 'profile', 
        loadComponent: () => import('./pages/patient/profile/profile.component').then(m => m.PatientProfileComponent)
      },
      { 
        path: 'rendez-vous', 
        loadComponent: () => import('./pages/patient/rendez-vous/rendez-vous.component').then(m => m.PatientRendezVousComponent)
      },
      { 
        path: 'nouveau-rdv', 
        loadComponent: () => import('./pages/patient/nouveau-rdv/nouveau-rdv.component').then(m => m.PatientNouveauRdvComponent)
      },
      { 
        path: 'dossier', 
        loadComponent: () => import('./pages/patient/dossier-medical/dossier-medical.component').then(m => m.DossierMedicalComponent)
      },
      { 
        path: 'consultation/:id', 
        loadComponent: () => import('./pages/patient/consultation-details/consultation-details.component').then(m => m.PatientConsultationDetailsComponent)
      }
    ]
  },
  
  // Routes Medecin (protegees)
  { 
    path: 'medecin', 
    canActivate: [authGuard, roleGuard(['medecin'])],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: MedecinDashboardComponent },
      { 
        path: 'profile', 
        loadComponent: () => import('./pages/medecin/profile/profile.component').then(m => m.MedecinProfileComponent)
      },
      { 
        path: 'planning', 
        loadComponent: () => import('./pages/medecin/planning/planning.component').then(m => m.MedecinPlanningComponent)
      },
      { 
        path: 'rendez-vous', 
        loadComponent: () => import('./pages/medecin/rendez-vous/rendez-vous.component').then(m => m.MedecinRendezVousComponent)
      },
      { 
        path: 'consultations', 
        loadComponent: () => import('./pages/medecin/consultations/consultations.component').then(m => m.MedecinConsultationsComponent)
      },
      { 
        path: 'consultation/:id', 
        loadComponent: () => import('./pages/medecin/consultation-workflow/consultation-workflow.component').then(m => m.ConsultationWorkflowComponent)
      },
      { 
        path: 'consultation-details/:id', 
        loadComponent: () => import('./pages/medecin/consultation-details/consultation-details.component').then(m => m.ConsultationDetailsComponent)
      },
      { 
        path: 'patients', 
        loadComponent: () => import('./pages/medecin/patients/patients.component').then(m => m.MedecinPatientsComponent)
      },
      { 
        path: 'patient/:patientId', 
        loadComponent: () => import('./pages/medecin/dossier-patient/dossier-patient.component').then(m => m.MedecinDossierPatientComponent)
      }
    ]
  },
  
  // Routes Infirmier (protegees)
  { 
    path: 'infirmier', 
    canActivate: [authGuard, roleGuard(['infirmier'])],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: InfirmierDashboardComponent },
      { 
        path: 'patients', 
        loadComponent: () => import('./pages/infirmier/patients/patients.component').then(m => m.InfirmierPatientsComponent)
      },
      {
        path: 'prise-parametres/:patientId',
        loadComponent: () => import('./pages/infirmier/prise-parametres/prise-parametres.component').then(m => m.PriseParametresComponent)
      },
      { 
        path: 'parametres', 
        redirectTo: 'dashboard',
        pathMatch: 'full'
      }
    ]
  },
  
  // Routes Admin (protegees)
  { 
    path: 'admin', 
    canActivate: [authGuard, roleGuard(['administrateur'])],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: AdminDashboardComponent },
      { 
        path: 'users', 
        loadComponent: () => import('./pages/admin/users/users.component').then(m => m.UsersComponent)
      },
      { 
        path: 'services', 
        loadComponent: () => import('./pages/admin/services/services.component').then(m => m.AdminServicesComponent)
      },
      { 
        path: 'assurances', 
        loadComponent: () => import('./pages/admin/assurances/assurances.component').then(m => m.AdminAssurancesComponent)
      },
      { 
        path: 'blocs-operatoires', 
        loadComponent: () => import('./pages/admin/blocs-operatoires/blocs-operatoires.component').then(m => m.BlocsOperatoiresComponent)
      },
      { 
        path: 'settings', 
        loadComponent: () => import('./pages/admin/settings/settings.component').then(m => m.AdminSettingsComponent)
      },
      { 
        path: 'monitoring', 
        loadComponent: () => import('./pages/admin/monitoring/monitoring.component').then(m => m.MonitoringComponent)
      },
      { 
        path: 'audit', 
        loadComponent: () => import('./pages/admin/audit/audit.component').then(m => m.AuditComponent)
      },
      { 
        path: 'finance', 
        loadComponent: () => import('./pages/admin/finance/finance').then(m => m.Finance)
      }
    ]
  },
  
  // Routes Caissier (protegees)
  { 
    path: 'caissier', 
    canActivate: [authGuard, roleGuard(['caissier'])],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: CaissierDashboardComponent }
    ]
  },
  
  // Routes Accueil (protegees)
  { 
    path: 'accueil', 
    canActivate: [authGuard, roleGuard(['accueil'])],
    loadChildren: () => import('./pages/accueil/accueil.routes').then(m => m.ACCUEIL_ROUTES)
  },
  
  // Routes Pharmacien (protegees)
  { 
    path: 'pharmacien', 
    canActivate: [authGuard, roleGuard(['pharmacien'])],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { 
        path: 'dashboard', 
        loadComponent: () => import('./pages/pharmacien/dashboard/dashboard.component').then(m => m.PharmacienDashboardComponent)
      },
      { 
        path: 'stock', 
        loadComponent: () => import('./pages/pharmacien/stock/stock.component').then(m => m.PharmacienStockComponent)
      },
      { 
        path: 'ordonnances', 
        loadComponent: () => import('./pages/pharmacien/ordonnances/ordonnances.component').then(m => m.PharmacienOrdonnancesComponent)
      },
      { 
        path: 'profile', 
        loadComponent: () => import('./pages/pharmacien/profile/profile.component').then(m => m.PharmacienProfileComponent)
      },
      { 
        path: 'commandes', 
        loadComponent: () => import('./pages/pharmacien/commandes/commandes.component').then(m => m.PharmacienCommandesComponent)
      },
      { 
        path: 'historique', 
        loadComponent: () => import('./pages/pharmacien/historique/historique.component').then(m => m.PharmacienHistoriqueComponent)
      },
      { 
        path: 'fournisseurs', 
        loadComponent: () => import('./pages/pharmacien/fournisseurs/fournisseurs.component').then(m => m.PharmacienFournisseursComponent)
      },
      { 
        path: 'ventes-directes', 
        loadComponent: () => import('./pages/pharmacien/ventes-directes/ventes-directes.component').then(m => m.PharmacienVentesDirectesComponent)
      }
    ]
  },
  
  // Routes Laborantin (protegees)
  { 
    path: 'laborantin', 
    canActivate: [authGuard, roleGuard(['laborantin'])],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { 
        path: 'dashboard', 
        loadComponent: () => import('./pages/laborantin/dashboard/dashboard.component').then(m => m.LaborantinDashboardComponent)
      },
      { 
        path: 'examens', 
        loadComponent: () => import('./pages/laborantin/examens/examens.component').then(m => m.LaborantinExamensComponent)
      }
    ]
  },
  
  // Redirection par defaut
  { path: '**', redirectTo: '' }
];
