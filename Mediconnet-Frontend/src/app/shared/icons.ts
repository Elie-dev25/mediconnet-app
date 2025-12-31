/**
 * Configuration centralisée des icônes Lucide
 * Importer ALL_ICONS_PROVIDER dans les composants qui ont besoin d'icônes
 * Usage: providers: [ALL_ICONS_PROVIDER]
 */
import { LucideAngularModule, LUCIDE_ICONS, LucideIconProvider } from 'lucide-angular';
import {
  // Navigation & Layout
  Home,
  Menu,
  X,
  ChevronLeft,
  ChevronRight,
  ChevronDown,
  ChevronUp,
  ArrowLeft,
  ArrowRight,
  ArrowUp,
  ArrowDown,
  ArrowUpRight,
  ArrowDownRight,
  ArrowUpLeft,
  ArrowDownLeft,
  MoreVertical,
  MoreHorizontal,
  ExternalLink,
  
  // User & Auth
  User,
  UserCog,
  UserPlus,
  Users,
  UserCheck,
  UserX,
  LogOut,
  LogIn,
  ShieldCheck,
  Shield,
  Lock,
  Unlock,
  Key,
  Eye,
  EyeOff,
  Mail,
  Phone,
  MapPin,
  AtSign,
  
  // Medical
  HeartPulse,
  Heart,
  Stethoscope,
  Pill,
  FlaskConical,
  Syringe,
  Activity,
  Award,
  Briefcase,
  GraduationCap,
  BedDouble,
  Bed,
  TestTube,
  Thermometer,
  Droplet,
  Scale,
  Ruler,
  Calculator,
  
  // Documents
  FileText,
  File,
  FileX,
  Inbox,
  FolderOpen,
  Folder,
  ClipboardList,
  ClipboardCheck,
  Clipboard,
  Printer,
  Save,
  Copy,
  
  // Calendar & Time
  Calendar,
  CalendarPlus,
  CalendarCheck,
  CalendarX,
  CalendarOff,
  CalendarDays,
  CalendarClock,
  Clock,
  Timer,
  History,
  RefreshCw,
  
  // Finance
  Receipt,
  CreditCard,
  Wallet,
  DollarSign,
  Banknote,
  PiggyBank,
  TrendingUp,
  TrendingDown,
  BarChart3,
  BarChart,
  
  // Actions
  Plus,
  Minus,
  Edit,
  Edit2,
  Edit3,
  Trash2,
  Trash,
  Search,
  Filter,
  SlidersHorizontal,
  Download,
  Upload,
  Play,
  Pause,
  Square,
  ToggleLeft,
  ToggleRight,
  Power,
  Zap,
  
  // Status & Alerts
  Check,
  CheckCircle,
  CheckCircle2,
  XCircle,
  AlertCircle,
  AlertTriangle,
  Info,
  HelpCircle,
  Bell,
  BellOff,
  Loader2,
  CircleDot,
  Circle,
  CalendarX2,
  
  // Layout & UI
  LayoutDashboard,
  LayoutGrid,
  LayoutList,
  Settings,
  Settings2,
  Building2,
  Building,
  Grid,
  List,
  Table,
  Image,
  Camera,
  
  // Communication
  MessageSquare,
  MessageCircle,
  Send,
  
  // Misc
  Star,
  Tag,
  Hash,
  Link,
  Paperclip,
  QrCode,
  Percent,
  Globe,
  Map,
  Wine,
  Cigarette,
  Baby,
  Scissors,
  
  // Pharmacy
  Package,
  PackageX,
  Sparkles,
  Truck,
  Pencil,
  RotateCcw,
  ArrowDownCircle,
  ArrowUpCircle,
  
  // Audio & Voice
  Mic,
  MicOff,
  Volume2,
  VolumeX
} from 'lucide-angular';

// Export du module et des utilitaires Lucide
export { LucideAngularModule, LUCIDE_ICONS, LucideIconProvider };

// Toutes les icônes de l'application - objet centralisé
export const ALL_ICONS = {
  // Navigation & Layout
  Home,
  Menu,
  X,
  ChevronLeft,
  ChevronRight,
  ChevronDown,
  ChevronUp,
  ArrowLeft,
  ArrowRight,
  ArrowUp,
  ArrowDown,
  ArrowUpRight,
  ArrowDownRight,
  ArrowUpLeft,
  ArrowDownLeft,
  MoreVertical,
  MoreHorizontal,
  ExternalLink,
  
  // User & Auth
  User,
  UserCog,
  UserPlus,
  Users,
  UserCheck,
  UserX,
  LogOut,
  LogIn,
  ShieldCheck,
  Shield,
  Lock,
  Unlock,
  Key,
  Eye,
  EyeOff,
  Mail,
  Phone,
  MapPin,
  AtSign,
  
  // Medical
  HeartPulse,
  Heart,
  Stethoscope,
  Pill,
  FlaskConical,
  Syringe,
  Activity,
  Award,
  Briefcase,
  GraduationCap,
  BedDouble,
  Bed,
  TestTube,
  Thermometer,
  Droplet,
  Scale,
  Ruler,
  Calculator,
  
  // Documents
  FileText,
  File,
  FileX,
  Inbox,
  FolderOpen,
  Folder,
  ClipboardList,
  ClipboardCheck,
  Clipboard,
  Printer,
  Save,
  Copy,
  
  // Calendar & Time
  Calendar,
  CalendarPlus,
  CalendarCheck,
  CalendarX,
  CalendarOff,
  CalendarDays,
  CalendarClock,
  Clock,
  Timer,
  History,
  RefreshCw,
  
  // Finance
  Receipt,
  CreditCard,
  Wallet,
  DollarSign,
  Banknote,
  PiggyBank,
  TrendingUp,
  TrendingDown,
  BarChart3,
  BarChart,
  
  // Actions
  Plus,
  Minus,
  Edit,
  Edit2,
  Edit3,
  Trash2,
  Trash,
  Search,
  Filter,
  SlidersHorizontal,
  Download,
  Upload,
  Play,
  Pause,
  Square,
  ToggleLeft,
  ToggleRight,
  Power,
  Zap,
  
  // Status & Alerts
  Check,
  CheckCircle,
  CheckCircle2,
  XCircle,
  AlertCircle,
  AlertTriangle,
  Info,
  HelpCircle,
  Bell,
  BellOff,
  Loader2,
  CircleDot,
  Circle,
  CalendarX2,
  
  // Layout & UI
  LayoutDashboard,
  LayoutGrid,
  LayoutList,
  Settings,
  Settings2,
  Building2,
  Building,
  Grid,
  List,
  Table,
  Image,
  Camera,
  
  // Communication
  MessageSquare,
  MessageCircle,
  Send,
  
  // Misc
  Star,
  Tag,
  Hash,
  Link,
  Paperclip,
  QrCode,
  Percent,
  Globe,
  Map,
  Wine,
  Cigarette,
  Baby,
  Scissors,
  
  // Pharmacy
  Package,
  PackageX,
  Sparkles,
  Truck,
  Pencil,
  RotateCcw,
  ArrowDownCircle,
  ArrowUpCircle,
  
  // Audio & Voice
  Mic,
  MicOff,
  Volume2,
  VolumeX
};

/**
 * Provider Lucide avec toutes les icônes de l'application
 * Usage: providers: [ALL_ICONS_PROVIDER]
 */
export const ALL_ICONS_PROVIDER = {
  provide: LUCIDE_ICONS,
  useValue: new LucideIconProvider(ALL_ICONS)
};

// Icônes spécifiques pour le sidebar médecin
export const MEDECIN_SIDEBAR_ICONS = {
  LayoutDashboard,
  UserCog,
  Calendar,
  CalendarCheck,
  Stethoscope,
  Users
};

// Icônes spécifiques pour le sidebar patient
export const PATIENT_SIDEBAR_ICONS = {
  LayoutDashboard,
  User,
  Calendar,
  FolderOpen,
  Pill,
  FlaskConical,
  Receipt
};

// Provider pour sidebar médecin
export const MEDECIN_ICONS_PROVIDER = {
  provide: LUCIDE_ICONS,
  useValue: new LucideIconProvider(MEDECIN_SIDEBAR_ICONS)
};

// Provider pour sidebar patient
export const PATIENT_ICONS_PROVIDER = {
  provide: LUCIDE_ICONS,
  useValue: new LucideIconProvider(PATIENT_SIDEBAR_ICONS)
};
