import { MenuItem } from '../../../shared';

/**
 * Configuration du menu sidebar pour l'espace laborantin
 */
export const LABORANTIN_MENU_ITEMS: MenuItem[] = [
  { icon: 'layout-dashboard', label: 'Tableau de bord', route: '/laborantin/dashboard', implemented: true },
  { icon: 'microscope', label: 'Examens', route: '/laborantin/examens', implemented: true },
  { icon: 'clipboard-list', label: 'Historique', route: '/laborantin/examens', queryParams: { statut: 'termine' }, implemented: true },
  { icon: 'user', label: 'Mon profil', route: '/laborantin/profile', implemented: false }
];

export const LABORANTIN_SIDEBAR_TITLE = 'Laboratoire';
