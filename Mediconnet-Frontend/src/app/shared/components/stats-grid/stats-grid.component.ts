import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { 
  LucideAngularModule, 
  LUCIDE_ICONS, 
  LucideIconProvider,
  Users, Calendar, Stethoscope, Pill, ClipboardList, Receipt,
  UserCog, Building2, Shield, Activity, TrendingUp, Heart,
  Syringe, FileText, CreditCard, Wallet, BadgeDollarSign,
  FlaskConical, BedDouble, UserPlus, CalendarCheck, Clock
} from 'lucide-angular';

export interface StatItem {
  icon: string;
  label: string;
  value: number | string;
  colorClass: string; // 'patients', 'consultations', 'ordonnances', 'examens', etc.
}

@Component({
  selector: 'app-stats-grid',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  providers: [
    { 
      provide: LUCIDE_ICONS, 
      useValue: new LucideIconProvider({ 
        Users, Calendar, Stethoscope, Pill, ClipboardList, Receipt,
        UserCog, Building2, Shield, Activity, TrendingUp, Heart,
        Syringe, FileText, CreditCard, Wallet, BadgeDollarSign,
        FlaskConical, BedDouble, UserPlus, CalendarCheck, Clock
      })
    }
  ],
  templateUrl: './stats-grid.component.html',
  styleUrl: './stats-grid.component.scss'
})
export class StatsGridComponent {
  @Input() stats: StatItem[] = [];
  @Input() title = 'Rapport';
  @Input() showPeriod = true;

  formatValue(value: number | string): string {
    if (typeof value === 'number') {
      return value === 0 ? '00' : value.toString();
    }
    return value || '00';
  }
}
