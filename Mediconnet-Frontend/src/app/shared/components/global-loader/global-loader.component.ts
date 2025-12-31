import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { LoadingService } from '../../../services/loading.service';
import { ALL_ICONS_PROVIDER } from '../../icons';

@Component({
  selector: 'app-global-loader',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './global-loader.component.html',
  styleUrl: './global-loader.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GlobalLoaderComponent {
  private readonly loadingService = inject(LoadingService);
  readonly isLoading$ = this.loadingService.isLoading$;
}
