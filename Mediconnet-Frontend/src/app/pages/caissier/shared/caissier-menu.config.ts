import { MenuItem } from '../../../shared';

/**
 * Configuration du menu sidebar pour l'espace caissier
 * Utilisé par tous les composants de l'espace caissier pour assurer la cohérence
 */
export const CAISSIER_MENU_ITEMS: MenuItem[] = [
  { icon: 'layout-dashboard', label: 'Tableau de bord', route: '/caissier/dashboard', implemented: true },
  { icon: 'credit-card', label: 'Encaissement', route: '/caissier/encaissement', implemented: false },
  { icon: 'receipt', label: 'Factures', route: '/caissier/factures', implemented: false },
  { icon: 'users', label: 'Patients', route: '/caissier/patients', implemented: false },
  { icon: 'piggy-bank', label: 'Assurances', route: '/caissier/assurances', implemented: false },
  { icon: 'wallet', label: 'Caisse', route: '/caissier/caisse', implemented: false },
  { icon: 'bar-chart-3', label: 'Rapports', route: '/caissier/rapports', implemented: false },
  { icon: 'file-text', label: 'Journal', route: '/caissier/journal', implemented: false }
];

/**
 * Titre du sidebar caissier
 */
export const CAISSIER_SIDEBAR_TITLE = 'Espace Caissier';
