import { MenuItem } from '../../../shared';

/**
 * Configuration du menu sidebar pour l'espace biologiste
 */
export const BIOLOGISTE_MENU_ITEMS: MenuItem[] = [
  { icon: 'layout-dashboard', label: 'Tableau de bord', route: '/biologiste/dashboard', implemented: true },
  { icon: 'flask-conical', label: 'Analyses en cours', route: '/biologiste/analyses', implemented: false },
  { icon: 'file-check', label: 'Résultats', route: '/biologiste/resultats', implemented: false },
  { icon: 'microscope', label: 'Examens', route: '/biologiste/examens', implemented: false },
  { icon: 'clipboard-list', label: 'Historique', route: '/biologiste/historique', implemented: false },
  { icon: 'user', label: 'Mon profil', route: '/biologiste/profile', implemented: false }
];

export const BIOLOGISTE_SIDEBAR_TITLE = 'Laboratoire';
