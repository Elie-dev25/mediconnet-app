import { MenuItem } from '../../../shared';

/**
 * Configuration du menu sidebar pour l'espace pharmacien
 */
export const PHARMACIEN_MENU_ITEMS: MenuItem[] = [
  { icon: 'layout-dashboard', label: 'Tableau de bord', route: '/pharmacien/dashboard', implemented: true },
  { icon: 'user', label: 'Mon profil', route: '/pharmacien/profile', implemented: false },
  { icon: 'pill', label: 'Ordonnances', route: '/pharmacien/ordonnances', implemented: true },
  { icon: 'package', label: 'Stock médicaments', route: '/pharmacien/stock', implemented: true },
  { icon: 'history', label: 'Historique', route: '/pharmacien/historique', implemented: true }
];

export const PHARMACIEN_SIDEBAR_TITLE = 'Pharmacie';
