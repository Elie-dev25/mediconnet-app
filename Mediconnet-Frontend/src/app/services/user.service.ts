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
  // Infirmier specific
  matricule?: string;
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
   * Supprimer un utilisateur
   */
  deleteUser(userId: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/users/${userId}`);
  }
}
