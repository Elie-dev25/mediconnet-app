import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface UserDto {
  idUser: number;
  nom: string;
  prenom: string;
  email: string;
  telephone?: string;
  role: string;
  createdAt?: string;
  specialite?: string;
  service?: string;
}

export interface UserDetailsDto {
  idUser: number;
  nom: string;
  prenom: string;
  email: string;
  telephone?: string;
  role: string;
  naissance?: string;
  sexe?: string;
  adresse?: string;
  situationMatrimoniale?: string;
  nationalite?: string;
  regionOrigine?: string;
  photo?: string;
  emailConfirmed: boolean;
  emailConfirmedAt?: string;
  profileCompleted: boolean;
  profileCompletedAt?: string;
  createdAt?: string;
  updatedAt?: string;
  infirmier?: InfirmierDetailsDto;
  medecin?: MedecinDetailsDto;
  patient?: PatientDetailsDto;
}

export interface InfirmierDetailsDto {
  matricule?: string;
  statut: string;
  isMajor: boolean;
  idServiceMajor?: number;
  nomServiceMajor?: string;
  dateNominationMajor?: string;
  accreditations?: string;
  titreAffiche: string;
}

export interface MedecinDetailsDto {
  numeroOrdre?: string;
  idSpecialite?: number;
  nomSpecialite?: string;
  idService?: number;
  nomService?: string;
}

export interface PatientDetailsDto {
  numeroPatient?: string;
  groupeSanguin?: string;
  allergies?: string;
  antecedentsMedicaux?: string;
  contactUrgenceNom?: string;
  contactUrgenceTelephone?: string;
  declarationHonneurAcceptee: boolean;
  dateDeclarationHonneur?: string;
  idAssurance?: number;
  nomAssurance?: string;
  numeroAssurance?: string;
}

export interface CreateUserRequest {
  nom: string;
  prenom: string;
  email: string;
  telephone: string;
  password: string;
  role: string;
  // Medecin specific
  idSpecialite?: number;
  idService?: number;
  numeroOrdre?: string;
  // Infirmier specific (idService aussi utilisé)
  matricule?: string;
  // Laborantin specific
  idLabo?: number;
  specialisation?: string;
}

export interface Specialite {
  idSpecialite: number;
  nomSpecialite: string;
}

export interface ServiceDto {
  idService: number;
  nomService: string;
  description?: string;
}

export interface LaboratoireDto {
  idLabo: number;
  nomLabo: string;
  contact?: string;
  telephone?: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = `${environment.apiUrl}/admin`;

  constructor(private http: HttpClient) {}

  /**
   * Recuperer la liste des utilisateurs (sauf patients)
   */
  getUsers(): Observable<UserDto[]> {
    return this.http.get<UserDto[]>(`${this.apiUrl}/users`);
  }

  /**
   * Creer un nouvel utilisateur
   */
  createUser(request: CreateUserRequest): Observable<{ message: string; userId: number }> {
    return this.http.post<{ message: string; userId: number }>(`${this.apiUrl}/users`, request);
  }

  /**
   * Recuperer la liste des specialites
   */
  getSpecialites(): Observable<Specialite[]> {
    return this.http.get<Specialite[]>(`${this.apiUrl}/specialites`);
  }

  /**
   * Recuperer la liste des services
   */
  getServices(): Observable<ServiceDto[]> {
    return this.http.get<ServiceDto[]>(`${this.apiUrl}/services`);
  }

  /**
   * Récupérer la liste des laboratoires internes
   */
  getLaboratoires(): Observable<LaboratoireDto[]> {
    return this.http.get<LaboratoireDto[]>(`${this.apiUrl}/laboratoires`);
  }

  /**
   * Supprimer un utilisateur
   */
  deleteUser(userId: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/users/${userId}`);
  }

  /**
   * Récupérer les détails complets d'un utilisateur
   */
  getUserDetails(userId: number): Observable<{ success: boolean; data: UserDetailsDto }> {
    return this.http.get<{ success: boolean; data: UserDetailsDto }>(`${this.apiUrl}/users/${userId}/details`);
  }

  /**
   * Mettre à jour le statut d'un infirmier
   */
  updateInfirmierStatut(userId: number, statut: string): Observable<{ success: boolean; message: string }> {
    return this.http.put<{ success: boolean; message: string }>(`${this.apiUrl}/infirmiers/${userId}/statut`, { statut });
  }

  /**
   * Nommer un infirmier Major d'un service
   */
  nommerInfirmierMajor(userId: number, idService: number): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/infirmiers/${userId}/nommer-major`, { idService });
  }

  /**
   * Révoquer la nomination Major d'un infirmier
   */
  revoquerInfirmierMajor(userId: number, motif?: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/infirmiers/${userId}/revoquer-major`, { motif });
  }

  /**
   * Mettre à jour les accréditations d'un infirmier
   */
  updateInfirmierAccreditations(userId: number, accreditations: string): Observable<{ success: boolean; message: string }> {
    return this.http.put<{ success: boolean; message: string }>(`${this.apiUrl}/infirmiers/${userId}/accreditations`, { accreditations });
  }
}
