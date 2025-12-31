import { MenuItem } from '../../../shared';

/**
 * Configuration du menu sidebar pour l'espace patient
 * Utilisé par tous les composants de l'espace patient pour assurer la cohérence
 */
export const PATIENT_MENU_ITEMS: MenuItem[] = [
  { icon: 'layout-dashboard', label: 'Tableau de bord', route: '/patient/dashboard', implemented: true },
  { icon: 'user', label: 'Mon Profil', route: '/patient/profile', implemented: true },
  { icon: 'calendar', label: 'Rendez-vous', route: '/patient/rendez-vous', implemented: true },
  { icon: 'folder-open', label: 'Dossier médical', route: '/patient/dossier', implemented: true },
  { icon: 'pill', label: 'Ordonnances', route: '/patient/ordonnances', implemented: false },
  { icon: 'flask-conical', label: 'Examens', route: '/patient/examens', implemented: false },
  { icon: 'receipt', label: 'Factures', route: '/patient/factures', implemented: false }
];

/**
 * Icônes Lucide requises pour le sidebar patient
 * À importer dans chaque composant utilisant le sidebar
 */
export const PATIENT_SIDEBAR_ICONS = [
  'LayoutDashboard',
  'User',
  'Calendar',
  'FolderOpen',
  'Pill',
  'FlaskConical',
  'Receipt'
] as const;

/**
 * Titre du sidebar patient
 */
export const PATIENT_SIDEBAR_TITLE = 'Espace Patient';
