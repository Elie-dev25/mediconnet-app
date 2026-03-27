import { MenuItem } from '../../../shared';

/**
 * Configuration du menu sidebar pour l'espace pharmacien
 */
export const PHARMACIEN_MENU_ITEMS: MenuItem[] = [
  { icon: 'layout-dashboard', label: 'Tableau de bord', route: '/pharmacien/dashboard', implemented: true },
  { icon: 'user', label: 'Mon profil', route: '/pharmacien/profile', implemented: true },
  { icon: 'pill', label: 'Ordonnances', route: '/pharmacien/ordonnances', implemented: true },
  { icon: 'shopping-cart', label: 'Ventes directes', route: '/pharmacien/ventes-directes', implemented: true },
  { icon: 'package', label: 'Stock médicaments', route: '/pharmacien/stock', implemented: true },
  { icon: 'file-text', label: 'Commandes', route: '/pharmacien/commandes', implemented: true },
  { icon: 'history', label: 'Historique', route: '/pharmacien/historique', implemented: true },
  { icon: 'truck', label: 'Fournisseurs', route: '/pharmacien/fournisseurs', implemented: true }
];

export const PHARMACIEN_SIDEBAR_TITLE = 'Pharmacie';
