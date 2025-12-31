import { ApplicationConfig, provideBrowserGlobalErrorListeners, LOCALE_ID } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { authInterceptor, loadingInterceptor } from './core';
import { registerLocaleData } from '@angular/common';
import localeFr from '@angular/common/locales/fr';

registerLocaleData(localeFr, 'fr-FR');
import { 
  ArrowLeft, ArrowRight, Mail, Lock, Eye, EyeOff, User, UserPlus, 
  LogIn, LogOut, AlertCircle, Loader2, ShieldCheck, Menu, X, 
  Home, Calendar, CalendarPlus, Pill, FlaskConical, Receipt, 
  FolderOpen, UserCog, LayoutDashboard, HeartPulse, Settings
} from 'lucide-angular';

import { routes } from './app.routes';

// Icônes Lucide disponibles globalement
const icons = {
  ArrowLeft, ArrowRight, Mail, Lock, Eye, EyeOff, User, UserPlus,
  LogIn, LogOut, AlertCircle, Loader2, ShieldCheck, Menu, X,
  Home, Calendar, CalendarPlus, Pill, FlaskConical, Receipt,
  FolderOpen, UserCog, LayoutDashboard, HeartPulse, Settings
};

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, loadingInterceptor])),
    provideAnimations(),
    { provide: LOCALE_ID, useValue: 'fr-FR' }
  ]
};

// Export des icônes pour utilisation dans les composants
export { icons };
