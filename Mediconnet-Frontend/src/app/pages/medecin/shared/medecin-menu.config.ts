import { MenuItem } from '../../../shared';

/**
 * Configuration du menu sidebar pour l'espace médecin
 * Utilisé par tous les composants de l'espace médecin pour assurer la cohérence
 */
export const MEDECIN_MENU_ITEMS: MenuItem[] = [
  { icon: 'layout-dashboard', label: 'Tableau de bord', route: '/medecin/dashboard', implemented: true },
  { icon: 'user-cog', label: 'Mon profil', route: '/medecin/profile', implemented: true },
  { icon: 'calendar', label: 'Mon Planning', route: '/medecin/planning', implemented: true },
  { icon: 'calendar-check', label: 'Rendez-vous', route: '/medecin/rendez-vous', implemented: true },
  { icon: 'stethoscope', label: 'Consultations', route: '/medecin/consultations', implemented: true },
  { icon: 'users', label: 'Mes patients', route: '/medecin/patients', implemented: true }
];

/**
 * Icônes Lucide requises pour le sidebar médecin
 * À importer dans chaque composant utilisant le sidebar
 */
export const MEDECIN_SIDEBAR_ICONS = [
  'LayoutDashboard',
  'UserCog', 
  'Calendar',
  'CalendarCheck',
  'Stethoscope',
  'Users'
] as const;

/**
 * Titre du sidebar médecin
 */
export const MEDECIN_SIDEBAR_TITLE = 'Espace Médecin';
