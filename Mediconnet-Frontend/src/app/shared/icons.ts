/**
 * Configuration centralisÃ©e des icÃ´nes Lucide
 * Importer ALL_ICONS_PROVIDER dans les composants qui ont besoin d'icÃ´nes
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
  UserSearch,
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
  MailCheck,
  Phone,
  PhoneCall,
  MapPin,
  AtSign,
  Flag,
  
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
  VolumeX,
  
  // Doors
  DoorOpen,
  DoorClosed,
  
  // Weather & Theme
  Sun,
  Moon,
  
  // Construction
  Construction,
  
  // Lab & Science
  Microscope
} from 'lucide-angular';

// Export du module et des utilitaires Lucide
export { LucideAngularModule, LUCIDE_ICONS, LucideIconProvider };

// Toutes les icÃ´nes de l'application - objet centralisÃ©
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
  UserSearch,
  'user-search': UserSearch,
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
  MailCheck,
  'mail-check': MailCheck,
  Phone,
  PhoneCall,
  'phone-call': PhoneCall,
  MapPin,
  AtSign,
  Flag,
  
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
  'check-circle-2': CheckCircle2,
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
  VolumeX,
  
  // Doors
  DoorOpen,
  DoorClosed,
  
  // Weather & Theme
  Sun,
  Moon,
  
  // Construction
  Construction,
  
  // Lab & Science
  Microscope
};

/**
 * Provider Lucide avec toutes les icÃ´nes de l'application
 * Usage: providers: [ALL_ICONS_PROVIDER]
 */
export const ALL_ICONS_PROVIDER = {
  provide: LUCIDE_ICONS,
  useValue: new LucideIconProvider(ALL_ICONS)
};

// IcÃ´nes spÃ©cifiques pour le sidebar mÃ©decin
export const MEDECIN_SIDEBAR_ICONS = {
  LayoutDashboard,
  UserCog,
  Calendar,
  CalendarCheck,
  Stethoscope,
  Users
};

// IcÃ´nes spÃ©cifiques pour le sidebar patient
export const PATIENT_SIDEBAR_ICONS = {
  LayoutDashboard,
  User,
  Calendar,
  FolderOpen,
  Pill,
  FlaskConical,
  Receipt
};

// Provider pour sidebar mÃ©decin
export const MEDECIN_ICONS_PROVIDER = {
  provide: LUCIDE_ICONS,
  useValue: new LucideIconProvider(MEDECIN_SIDEBAR_ICONS)
};

// Provider pour sidebar patient
export const PATIENT_ICONS_PROVIDER = {
  provide: LUCIDE_ICONS,
  useValue: new LucideIconProvider(PATIENT_SIDEBAR_ICONS)
};
