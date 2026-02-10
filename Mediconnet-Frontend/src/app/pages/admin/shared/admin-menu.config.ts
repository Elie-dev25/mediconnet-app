import { MenuItem } from '../../../shared';

/**
 * Configuration du menu sidebar pour l'espace administrateur
 * Utilisé par tous les composants de l'espace admin pour assurer la cohérence
 */
export const ADMIN_MENU_ITEMS: MenuItem[] = [
  { icon: 'layout-dashboard', label: 'Tableau de bord', route: '/admin/dashboard', implemented: true },
  { icon: 'users', label: 'Utilisateurs', route: '/admin/users', implemented: true },
  { icon: 'building-2', label: 'Services', route: '/admin/services', implemented: true },
  { icon: 'shield-check', label: 'Assurances', route: '/admin/assurances', implemented: true },
  { icon: 'activity', label: 'Monitoring', route: '/admin/monitoring', implemented: true },
  { icon: 'scroll-text', label: 'Audit', route: '/admin/audit', implemented: true },
  { icon: 'settings', label: 'Paramètres', route: '/admin/settings', implemented: true }
];

/**
 * Titre du sidebar admin
 */
export const ADMIN_SIDEBAR_TITLE = 'Administration';
