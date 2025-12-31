import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { 
  LucideAngularModule, 
  LUCIDE_ICONS, 
  LucideIconProvider,
  Home, Calendar, CalendarCheck, Pill, FlaskConical, Receipt, Settings, X, 
  LayoutDashboard, FolderOpen, UserCog, User, Users, Building2, BarChart3, Shield,
  Stethoscope, HeartPulse, ClipboardList, FileText, UserPlus, CheckCircle, Clock,
  CreditCard, Wallet, PiggyBank, Banknote, TrendingUp, TrendingDown, DollarSign,
  ArrowDownRight, ArrowUpRight,
  Package, History, Search, Plus, Pencil, Trash2, SlidersHorizontal, Filter,
  AlertCircle, AlertTriangle, Info, ChevronLeft, ChevronRight, Loader2,
  PackageX, Sparkles, Truck, ClipboardCheck, RotateCcw, ArrowDownCircle, ArrowUpCircle,
  Inbox, Eye, EyeOff, XCircle, Save, RefreshCw, Download, Upload,
  Syringe, BedDouble, Activity
} from 'lucide-angular';

export interface MenuItem {
  icon: string;
  label: string;
  route: string;
  badge?: number;
  implemented?: boolean; // true par defaut si non specifie
  action?: string; // 'profile' pour ouvrir le formulaire de profil
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule],
  providers: [
    { 
      provide: LUCIDE_ICONS, 
      useValue: new LucideIconProvider({ 
        Home, Calendar, CalendarCheck, Pill, FlaskConical, Receipt, Settings, X, 
        LayoutDashboard, FolderOpen, UserCog, User, Users, Building2, BarChart3, Shield,
        Stethoscope, HeartPulse, ClipboardList, FileText, UserPlus, CheckCircle, Clock,
        CreditCard, Wallet, PiggyBank, Banknote, TrendingUp, TrendingDown, DollarSign,
        ArrowDownRight, ArrowUpRight,
        Package, History, Search, Plus, Pencil, Trash2, SlidersHorizontal, Filter,
        AlertCircle, AlertTriangle, Info, ChevronLeft, ChevronRight, Loader2,
        PackageX, Sparkles, Truck, ClipboardCheck, RotateCcw, ArrowDownCircle, ArrowUpCircle,
        Inbox, Eye, EyeOff, XCircle, Save, RefreshCw, Download, Upload,
        Syringe, BedDouble, Activity
      })
    }
  ],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  @Input() title = 'Menu';
  @Input() menuItems: MenuItem[] = [];
  @Input() collapsed = false;
  @Input() mobileOpen = false;
  @Output() closeMobile = new EventEmitter<void>();
  @Output() featureUnavailable = new EventEmitter<string>();
  @Output() profileClick = new EventEmitter<void>();

  onMenuItemClick(event: Event, item: MenuItem): void {
    // Si c'est une action speciale (profil)
    if (item.action === 'profile') {
      event.preventDefault();
      this.profileClick.emit();
      return;
    }
    
    // Si la fonctionnalite n'est pas implementee, empecher la navigation
    if (item.implemented === false) {
      event.preventDefault();
      this.featureUnavailable.emit(item.label);
    }
  }

  isImplemented(item: MenuItem): boolean {
    // Les items avec action sont consideres comme implementes
    if (item.action) return true;
    return item.implemented !== false;
  }
}
