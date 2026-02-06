import { MenuItem } from '../../../shared';

/**
 * Configuration du menu sidebar pour l'espace laborantin
 */
export const LABORANTIN_MENU_ITEMS: MenuItem[] = [
  { icon: 'layout-dashboard', label: 'Tableau de bord', route: '/laborantin/dashboard', implemented: true },
  { icon: 'flask-conical', label: 'Analyses en cours', route: '/laborantin/analyses', implemented: false },
  { icon: 'file-check', label: 'Résultats', route: '/laborantin/resultats', implemented: false },
  { icon: 'microscope', label: 'Examens', route: '/laborantin/examens', implemented: false },
  { icon: 'clipboard-list', label: 'Historique', route: '/laborantin/historique', implemented: false },
  { icon: 'user', label: 'Mon profil', route: '/laborantin/profile', implemented: false }
];

export const LABORANTIN_SIDEBAR_TITLE = 'Laboratoire';
