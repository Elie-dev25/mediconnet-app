import { MenuItem } from '../../../shared';

/**
 * Configuration du menu sidebar pour l'espace accueil
 * Utilisé par tous les composants de l'espace accueil pour assurer la cohérence
 */
export const ACCUEIL_MENU_ITEMS: MenuItem[] = [
  { icon: 'layout-dashboard', label: 'Tableau de bord', route: '/accueil/dashboard', implemented: true },
  { icon: 'user-plus', label: 'Ajouter patient', route: '/accueil/ajouter-patient', implemented: true },
  { icon: 'clipboard-list', label: 'Enregistrement', route: '/accueil/enregistrement', implemented: true },
  { icon: 'calendar-check', label: 'RDV du jour', route: '/accueil/rdv-jour', implemented: true },
  { icon: 'users', label: 'Patients', route: '/accueil/patients', implemented: false },
  { icon: 'stethoscope', label: 'Médecins', route: '/accueil/medecins', implemented: false },
  { icon: 'settings', label: 'Paramètres', route: '/accueil/parametres', implemented: false }
];

/**
 * Titre du sidebar accueil
 */
export const ACCUEIL_SIDEBAR_TITLE = 'Espace Accueil';
