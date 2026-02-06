import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { GlobalLoaderComponent } from './shared/components/global-loader/global-loader.component';
import { IdleWarningModalComponent } from './shared/components/idle-warning-modal/idle-warning-modal.component';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, GlobalLoaderComponent, IdleWarningModalComponent],
  templateUrl: './app.html',
  styleUrls: ['./app.scss']
})
export class App implements OnInit {
  protected readonly title = 'mediconnet-frontend';

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    // Si l'utilisateur est déjà authentifié au chargement, démarrer la surveillance d'inactivité
    if (this.authService.isAuthenticated()) {
      this.authService.startIdleWatching();
    }
  }
}
