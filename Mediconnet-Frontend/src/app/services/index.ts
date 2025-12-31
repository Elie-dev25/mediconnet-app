/**
 * Barrel exports pour les services de l'application
 * 
 * NOTE: En raison de conflits de noms entre certains DTOs exportés par différents services,
 * il est recommandé d'importer les services directement depuis leur fichier source
 * plutôt que depuis ce barrel export.
 * 
 * Exemple: import { PatientService } from '../services/patient.service';
 * 
 * Ce fichier exporte uniquement les services (classes), pas les DTOs/interfaces.
 */

// Services d'authentification
export { AuthService } from './auth.service';
export { AuthNavigationService } from './auth-navigation.service';
export { FirstLoginService } from './first-login.service';

// Services métier - Patient
export { PatientService } from './patient.service';
export { ReceptionPatientService } from './reception-patient.service';

// Services métier - Médecin
export { MedecinService } from './medecin.service';
export { MedecinDataService } from './medecin-data.service';
export { MedecinPlanningService } from './medecin-planning.service';

// Services métier - Consultation
export { ConsultationService } from './consultation.service';
export { ConsultationCompleteService } from './consultation-complete.service';
export { ConsultationQuestionnaireService } from './consultation-questionnaire.service';
export { QuestionsPredefiniesService } from './questions-predefinies.service';
export { ParametreService } from './parametre.service';

// Services métier - Rendez-vous
export { RendezVousService } from './rendez-vous.service';

// Services métier - Administration
export { AdminService } from './admin.service';
export { AdminSettingsService } from './admin-settings.service';
export { UserService } from './user.service';

// Services métier - Autres
export { AccueilService } from './accueil.service';
export { AssuranceService } from './assurance.service';
export { CaisseService } from './caisse.service';
export { DossierAccessService } from './dossier-access.service';
export { HospitalisationService } from './hospitalisation.service';
export { InfirmierService } from './infirmier.service';
export { PharmacieStockService } from './pharmacie-stock.service';

// Services utilitaires
export { LoadingService } from './loading.service';
export { PhoneValidationService } from './phone-validation.service';
export { SignalRService } from './signalr.service';
export { SpeechRecognitionService } from './speech-recognition.service';

// NOTE: Services obsolètes - ne pas utiliser
// ProfileService           // @deprecated - Remplacé par patient.service
// PatientProfileService    // @deprecated - Remplacé par patient.service
