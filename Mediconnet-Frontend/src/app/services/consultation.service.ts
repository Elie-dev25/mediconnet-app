import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface EnregistrerConsultationRequest {
  idPatient: number;
  motif: string;
  idMedecin: number;
  prixConsultation: number;
  dateHeureCreneau?: string;
}

export interface EnregistrerConsultationResponse {
  success: boolean;
  message: string;
  idConsultation: number;
  idPaiement: number;
  numeroPaiement: string;
  patient?: {
    idUser: number;
    nom: string;
    prenom: string;
    numeroDossier: string;
    email: string;
  };
}

export interface MedecinDisponible {
  idMedecin: number;
  nom: string;
  prenom: string;
  specialite: string;
  service?: string;
  idService?: number;
  idSpecialite?: number;
}

export interface ServiceHospitalier {
  idService: number;
  nomService: string;
  description?: string;
}

export interface Specialite {
  idSpecialite: number;
  nomSpecialite: string;
}

export interface MedecinAvecDisponibilite {
  idMedecin: number;
  nom: string;
  prenom: string;
  specialite: string;
  service?: string;
  idService?: number;
  idSpecialite?: number;
  
  // Statut de disponibilité
  statut: 'disponible' | 'occupe' | 'absent';
  estDisponible: boolean;
  
  // Détails de charge
  patientsEnAttente: number;
  patientsEnConsultation: number;
  rendezVousAujourdhui: number;
  
  // Prochaine disponibilité
  raisonIndisponibilite?: string;
  prochaineDisponibilite?: string;
  
  // Temps d'attente estimé (en minutes)
  tempsAttenteEstime?: number;
}

export interface MedecinsDisponibiliteResponse {
  success: boolean;
  medecins: MedecinAvecDisponibilite[];
  totalDisponibles: number;
  totalOccupes: number;
  totalAbsents: number;
}

export interface VerifierPaiementResponse {
  paiementValide: boolean;
  numeroFacture?: string;
  datePaiement?: string;
  dateExpiration?: string;
  message?: string;
}

export interface CreneauJourDto {
  heureDebut: string;
  heureFin: string;
  dateHeure: string;
  statut: 'disponible' | 'occupe' | 'passe';
  selectionnable: boolean;
}

export interface CreneauxMedecinJourResponse {
  idMedecin: number;
  date: string;
  coutConsultation: number;
  creneaux: CreneauJourDto[];
}

@Injectable({
  providedIn: 'root'
})
export class ConsultationService {
  private apiUrl = `${environment.apiUrl}/accueil`;

  constructor(private http: HttpClient) {}

  /**
   * Enregistre une nouvelle consultation
   */
  enregistrerConsultation(request: EnregistrerConsultationRequest): Observable<EnregistrerConsultationResponse> {
    return this.http.post<EnregistrerConsultationResponse>(`${this.apiUrl}/consultations/enregistrer`, request);
  }

  /**
   * Récupère la liste des médecins disponibles
   */
  getMedecinsDisponibles(): Observable<MedecinDisponible[]> {
    return this.http.get<MedecinDisponible[]>(`${this.apiUrl}/medecins/disponibles`);
  }

  /**
   * Récupère la liste des médecins filtrés par service et/ou spécialité
   */
  getMedecinsFiltres(idService?: number, idSpecialite?: number): Observable<MedecinDisponible[]> {
    let params: any = {};
    if (idService) params.idService = idService;
    if (idSpecialite) params.idSpecialite = idSpecialite;
    return this.http.get<MedecinDisponible[]>(`${this.apiUrl}/medecins/filtrer`, { params });
  }

  /**
   * Récupère la liste des services hospitaliers
   */
  getServices(): Observable<ServiceHospitalier[]> {
    return this.http.get<ServiceHospitalier[]>(`${this.apiUrl}/services`);
  }

  /**
   * Récupère la liste des spécialités médicales
   */
  getSpecialites(): Observable<Specialite[]> {
    return this.http.get<Specialite[]>(`${this.apiUrl}/specialites`);
  }

  /**
   * Récupère la liste des médecins avec leur statut de disponibilité en temps réel
   */
  getMedecinsAvecDisponibilite(idService?: number, idSpecialite?: number): Observable<MedecinsDisponibiliteResponse> {
    let params: any = {};
    if (idService) params.idService = idService;
    if (idSpecialite) params.idSpecialite = idSpecialite;
    return this.http.get<MedecinsDisponibiliteResponse>(`${this.apiUrl}/medecins/disponibilite`, { params });
  }

  /**
   * Vérifie si un patient a un paiement de consultation encore valide (règle des 14 jours)
   */
  verifierPaiementValide(idPatient: number, idMedecin: number): Observable<VerifierPaiementResponse> {
    return this.http.get<VerifierPaiementResponse>(`${this.apiUrl}/verifier-paiement/${idPatient}/${idMedecin}`);
  }

  /**
   * Récupère les créneaux du jour d'un médecin avec leur statut
   */
  getCreneauxMedecinJour(idMedecin: number): Observable<CreneauxMedecinJourResponse> {
    return this.http.get<CreneauxMedecinJourResponse>(`${this.apiUrl}/medecins/${idMedecin}/creneaux-jour`);
  }
}
