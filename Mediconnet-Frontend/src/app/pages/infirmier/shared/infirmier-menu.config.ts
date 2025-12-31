import { MenuItem } from '../../../shared';

/**
 * Configuration du menu sidebar pour l'espace infirmier
 * Utilisé par tous les composants de l'espace infirmier pour assurer la cohérence
 */
export const INFIRMIER_MENU_ITEMS: MenuItem[] = [
  { icon: 'layout-dashboard', label: 'Tableau de bord', route: '/infirmier/dashboard', implemented: true },
  { icon: 'user-cog', label: 'Mon profil', route: '/infirmier/profile', implemented: false },
  { icon: 'users', label: 'Patients', route: '/infirmier/patients', implemented: true },
  { icon: 'calendar', label: 'Planning', route: '/infirmier/planning', implemented: false },
  { icon: 'syringe', label: 'Soins', route: '/infirmier/soins', implemented: false },
  { icon: 'bed-double', label: 'Hospitalisations', route: '/infirmier/hospitalisations', implemented: false }
];

/**
 * Titre du sidebar infirmier
 */
export const INFIRMIER_SIDEBAR_TITLE = 'Espace Infirmier';
