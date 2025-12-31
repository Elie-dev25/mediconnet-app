import { Injectable, OnDestroy } from '@angular/core';
import { Subject, BehaviorSubject, Observable } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

export interface AppointmentEvent {
  type: 'created' | 'updated' | 'cancelled';
  data: any;
}

export interface SlotEvent {
  type: 'locked' | 'unlocked' | 'updated' | 'refresh';
  medecinId: number;
  dateHeure?: Date;
  action?: string;
}

export interface FactureEvent {
  type: 'created' | 'paid';
  data: any;
}

export interface VitalsEvent {
  type: 'recorded' | 'nurse_refresh';
  data: any;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService implements OnDestroy {
  private hubConnection: signalR.HubConnection | null = null;
  private connectionState = new BehaviorSubject<string>('disconnected');
  
  // Subjects pour les événements
  private appointmentEvents = new Subject<AppointmentEvent>();
  private slotEvents = new Subject<SlotEvent>();
  private factureEvents = new Subject<FactureEvent>();
  private refreshRequested = new Subject<number>();
  private vitalsEvents = new Subject<VitalsEvent>();

  // Observables publics
  public connectionState$ = this.connectionState.asObservable();
  public appointmentEvents$ = this.appointmentEvents.asObservable();
  public slotEvents$ = this.slotEvents.asObservable();
  public factureEvents$ = this.factureEvents.asObservable();
  public refreshRequested$ = this.refreshRequested.asObservable();
  public vitalsEvents$ = this.vitalsEvents.asObservable();

  constructor(private authService: AuthService) {
    // Se connecter automatiquement si authentifié
    this.authService.isAuthenticated$.subscribe(isAuth => {
      if (isAuth) {
        this.connect();
      } else {
        this.disconnect();
      }
    });
  }

  /**
   * Établir la connexion au hub SignalR
   */
  async connect(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    const token = this.authService.getToken();
    if (!token) {
      console.warn('SignalR: Pas de token, connexion impossible');
      return;
    }

    // environment.apiUrl = http://localhost:8080/api
    // Hub backend = http://localhost:8080/hubs/appointments
    const hubUrl = `${environment.apiUrl.replace(/\/?api\/?$/, '')}/hubs/appointments`;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Configurer les handlers d'événements
    this.setupEventHandlers();

    // Configurer les handlers de connexion
    this.hubConnection.onreconnecting(() => {
      this.connectionState.next('reconnecting');
      console.log('SignalR: Reconnexion en cours...');
    });

    this.hubConnection.onreconnected(() => {
      this.connectionState.next('connected');
      console.log('SignalR: Reconnecté');
    });

    this.hubConnection.onclose(() => {
      this.connectionState.next('disconnected');
      console.log('SignalR: Déconnecté');
    });

    try {
      await this.hubConnection.start();
      this.connectionState.next('connected');
      console.log('✅ SignalR: Connecté au hub des rendez-vous');
    } catch (err) {
      console.error('❌ SignalR: Erreur de connexion', err);
      this.connectionState.next('error');
    }
  }

  /**
   * Configurer les handlers d'événements SignalR
   */
  private setupEventHandlers(): void {
    if (!this.hubConnection) return;

    // Rendez-vous créé
    this.hubConnection.on('AppointmentCreated', (data: any) => {
      console.log('📅 SignalR: Nouveau rendez-vous', data);
      this.appointmentEvents.next({ type: 'created', data });
    });

    // Rendez-vous modifié
    this.hubConnection.on('AppointmentUpdated', (data: any) => {
      console.log('📝 SignalR: Rendez-vous modifié', data);
      this.appointmentEvents.next({ type: 'updated', data });
    });

    // Rendez-vous annulé
    this.hubConnection.on('AppointmentCancelled', (data: any) => {
      console.log('❌ SignalR: Rendez-vous annulé', data);
      this.appointmentEvents.next({ type: 'cancelled', data });
    });

    // Créneau verrouillé
    this.hubConnection.on('SlotLocked', (data: { medecinId: number; dateHeure: string }) => {
      console.log('🔒 SignalR: Créneau verrouillé', data);
      this.slotEvents.next({ 
        type: 'locked', 
        medecinId: data.medecinId,
        dateHeure: new Date(data.dateHeure)
      });
    });

    // Créneau libéré
    this.hubConnection.on('SlotUnlocked', (data: { medecinId: number; dateHeure: string }) => {
      console.log('🔓 SignalR: Créneau libéré', data);
      this.slotEvents.next({ 
        type: 'unlocked', 
        medecinId: data.medecinId,
        dateHeure: new Date(data.dateHeure)
      });
    });

    // Créneaux mis à jour
    this.hubConnection.on('SlotsUpdated', (data: { medecinId: number; action: string }) => {
      console.log('🔄 SignalR: Créneaux mis à jour', data);
      this.slotEvents.next({ 
        type: 'updated', 
        medecinId: data.medecinId,
        action: data.action
      });
    });

    // Demande de rafraîchissement
    this.hubConnection.on('SlotsRefreshRequested', (medecinId: number) => {
      console.log('🔄 SignalR: Rafraîchissement demandé pour médecin', medecinId);
      this.refreshRequested.next(medecinId);
    });

    // Facture créée (ex: consultation enregistrée)
    this.hubConnection.on('FactureCreated', (data: any) => {
      console.log('🧾 SignalR: Facture créée', data);
      this.factureEvents.next({ type: 'created', data });
    });

    // Facture payée
    this.hubConnection.on('FacturePaid', (data: any) => {
      console.log('✅ SignalR: Facture payée', data);
      this.factureEvents.next({ type: 'paid', data });
    });

    // Paramètres infirmiers enregistrés
    this.hubConnection.on('VitalsRecorded', (data: any) => {
      console.log('🩺 SignalR: Paramètres enregistrés', data);
      this.vitalsEvents.next({ type: 'recorded', data });
    });

    // Rafraîchissement file infirmier demandé
    this.hubConnection.on('NurseQueueRefresh', (data: any) => {
      console.log('🔄 SignalR: Rafraîchissement file infirmier', data);
      this.vitalsEvents.next({ type: 'nurse_refresh', data });
    });
  }

  /**
   * S'abonner aux mises à jour d'un médecin
   */
  async subscribeToMedecin(medecinId: number): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('SubscribeToMedecin', medecinId);
      console.log(`📡 Abonné aux mises à jour du médecin ${medecinId}`);
    }
  }

  /**
   * Se désabonner des mises à jour d'un médecin
   */
  async unsubscribeFromMedecin(medecinId: number): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('UnsubscribeFromMedecin', medecinId);
    }
  }

  /**
   * Demander un rafraîchissement des créneaux
   */
  async requestSlotRefresh(medecinId: number): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('RequestSlotRefresh', medecinId);
    }
  }

  /**
   * Déconnecter du hub
   */
  async disconnect(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.hubConnection = null;
      this.connectionState.next('disconnected');
    }
  }

  /**
   * Vérifier si connecté
   */
  get isConnected(): boolean {
    return this.hubConnection?.state === signalR.HubConnectionState.Connected;
  }

  ngOnDestroy(): void {
    this.disconnect();
  }
}
