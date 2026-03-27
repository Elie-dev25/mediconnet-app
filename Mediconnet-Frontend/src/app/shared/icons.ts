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
  ChevronsLeft,
  ChevronsRight,
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
  Smartphone,
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
  
  // Planning & Idées
  Route,
  Lightbulb,
  
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
  PauseCircle,
  ToggleLeft,
  ToggleRight,
  Square,
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
  ScrollText,
  ShoppingCart,
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
  Microscope,
  FilePlus,
  UploadCloud,
  FileCheck,
  FileClock,
  Cloud,
  Clock8,
  Wand2,
  
  // Missing icons for consultation
  ScanLine,
  CirclePlus,
  BrainCircuit,
  FolderCheck,
  
  // Icons for history view
  ArrowUpDown,
  Layers,
  
  // Direct sales icons
  ShoppingBag
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
  ChevronsLeft,
  ChevronsRight,
  'chevrons-left': ChevronsLeft,
  'chevrons-right': ChevronsRight,
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
  'log-out': LogOut,
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
  Smartphone,
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
  
  // Planning & Idées
  Route,
  Lightbulb,
  
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
  PauseCircle,
  'pause-circle': PauseCircle,
  Square,
  ToggleLeft,
  'toggle-left': ToggleLeft,
  ToggleRight,
  'toggle-right': ToggleRight,
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
  'layout-grid': LayoutGrid,
  LayoutList,
  ScrollText,
  'scroll-text': ScrollText,
  ShoppingCart,
  'shopping-cart': ShoppingCart,
  ShoppingBag,
  'shopping-bag': ShoppingBag,
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
  Microscope,
  'microscope': Microscope,
  FilePlus,
  'file-plus': FilePlus,
  UploadCloud,
  'upload-cloud': UploadCloud,
  FileCheck,
  'file-check': FileCheck,
  FileClock,
  'file-clock': FileClock,
  Cloud,
  'cloud': Cloud,
  Clock8,
  'clock-8': Clock8,
  Wand2,
  'wand-2': Wand2,
  
  // Consultation icons
  ScanLine,
  'scan-line': ScanLine,
  CirclePlus,
  'circle-plus': CirclePlus,
  BrainCircuit,
  'brain-circuit': BrainCircuit,
  FolderCheck,
  'folder-check': FolderCheck,
  
  // History view icons
  ArrowUpDown,
  'arrow-up-down': ArrowUpDown,
  Layers,
  'layers': Layers
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
