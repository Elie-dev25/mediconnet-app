import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { 
  DashboardLayoutComponent, 
  ConsultationDetailsViewComponent,
  LucideAngularModule, 
  ALL_ICONS_PROVIDER 
} from '../../../shared';
import { MEDECIN_MENU_ITEMS, MEDECIN_SIDEBAR_TITLE } from '../shared';

@Component({
  selector: 'app-consultation-details',
  standalone: true,
  imports: [
    CommonModule,
    LucideAngularModule,
    DashboardLayoutComponent,
    ConsultationDetailsViewComponent
  ],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './consultation-details.component.html',
  styleUrl: './consultation-details.component.scss'
})
export class ConsultationDetailsComponent implements OnInit {
  menuItems = MEDECIN_MENU_ITEMS;
  sidebarTitle = MEDECIN_SIDEBAR_TITLE;
  consultationId: number | null = null;

  constructor(private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      const id = params['id'];
      if (id) {
        this.consultationId = +id;
      }
    });
  }
}
