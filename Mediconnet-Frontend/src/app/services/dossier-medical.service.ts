import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import type {
  DossierMedicalData,
  DossierPatientInfo,
  ConsultationItem,
  OrdonnanceItem,
  ExamenItem
} from '../models/dossier-medical.models';

/**
 * Service unifié pour la gestion du dossier médical
 * Centralise les appels API pour patient et médecin
 */
@Injectable({
  providedIn: 'root'
})
export class DossierMedicalService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  /**
   * Récupère le dossier médical du patient connecté
   */
  getMonDossierMedical(): Observable<DossierMedicalData> {
    return this.http.get<any>(`${this.apiUrl}/patient/dossier-medical`).pipe(
      map(response => this.transformPatientResponse(response))
    );
  }

  /**
   * Récupère le dossier médical d'un patient (pour médecin)
   */
  getDossierPatient(patientId: number): Observable<DossierMedicalData> {
    return this.http.get<any>(`${this.apiUrl}/consultation/dossier-patient/${patientId}`).pipe(
      map(response => this.transformMedecinResponse(response))
    );
  }

  /**
   * Transforme la réponse de l'API patient vers DossierMedicalData
   */
  private transformPatientResponse(response: any): DossierMedicalData {
    return {
      patient: this.extractPatientInfo(response),
      stats: {
        totalConsultations: response.consultations?.length || 0,
        totalOrdonnances: response.ordonnances?.length || 0,
        totalExamens: response.examens?.length || 0,
        derniereVisite: response.consultations?.[0]?.dateHeure
      },
      consultations: this.transformConsultations(response.consultations || []),
      ordonnances: this.transformOrdonnances(response.ordonnances || []),
      examens: this.transformExamens(response.examens || []),
      antecedents: this.extractAntecedents(response),
      allergies: this.extractAllergies(response)
    };
  }

  /**
   * Transforme la réponse de l'API médecin vers DossierMedicalData
   */
  private transformMedecinResponse(response: any): DossierMedicalData {
    return {
      patient: {
        idUser: response.idPatient,
        nom: response.nom,
        prenom: response.prenom,
        numeroDossier: response.numeroDossier,
        groupeSanguin: response.groupeSanguin,
        naissance: response.naissance,
        sexe: response.sexe,
        age: response.age,
        telephone: response.telephone,
        email: response.email,
        adresse: response.adresse,
        nationalite: response.nationalite,
        regionOrigine: response.regionOrigine,
        situationMatrimoniale: response.situationMatrimoniale,
        profession: response.profession,
        ethnie: response.ethnie,
        nbEnfants: response.nbEnfants,
        maladiesChroniques: response.maladiesChroniques,
        allergiesConnues: response.allergiesConnues,
        allergiesDetails: response.allergiesDetails,
        antecedentsFamiliaux: response.antecedentsFamiliaux,
        antecedentsFamiliauxDetails: response.antecedentsFamiliauxDetails,
        operationsChirurgicales: response.operationsChirurgicales,
        operationsDetails: response.operationsDetails,
        consommationAlcool: response.consommationAlcool,
        frequenceAlcool: response.frequenceAlcool,
        tabagisme: response.tabagisme,
        activitePhysique: response.activitePhysique,
        personneContact: response.personneContact,
        numeroContact: response.numeroContact,
        nomAssurance: response.nomAssurance,
        numeroCarteAssurance: response.numeroCarteAssurance,
        couvertureAssurance: response.couvertureAssurance,
        dateDebutValidite: response.dateDebutValidite,
        dateFinValidite: response.dateFinValidite
      },
      stats: {
        totalConsultations: response.consultations?.length || 0,
        totalOrdonnances: response.ordonnances?.length || 0,
        totalExamens: response.examens?.length || 0
      },
      consultations: this.transformConsultations(response.consultations || []),
      ordonnances: this.transformOrdonnances(response.ordonnances || []),
      examens: this.transformExamens(response.examens || []),
      antecedents: this.extractAntecedents(response),
      allergies: this.extractAllergies(response)
    };
  }

  /**
   * Extrait les informations patient de la réponse
   */
  private extractPatientInfo(response: any): DossierPatientInfo {
    return {
      idUser: response.idUser,
      nom: response.nom,
      prenom: response.prenom,
      numeroDossier: response.numeroDossier,
      groupeSanguin: response.groupeSanguin,
      naissance: response.naissance,
      sexe: response.sexe,
      age: response.age,
      telephone: response.telephone,
      email: response.email,
      adresse: response.adresse,
      nationalite: response.nationalite,
      regionOrigine: response.regionOrigine,
      situationMatrimoniale: response.situationMatrimoniale,
      profession: response.profession,
      ethnie: response.ethnie,
      nbEnfants: response.nbEnfants,
      maladiesChroniques: response.maladiesChroniques,
      allergiesConnues: response.allergiesConnues,
      allergiesDetails: response.allergiesDetails,
      antecedentsFamiliaux: response.antecedentsFamiliaux,
      antecedentsFamiliauxDetails: response.antecedentsFamiliauxDetails,
      operationsChirurgicales: response.operationsChirurgicales,
      operationsDetails: response.operationsDetails,
      consommationAlcool: response.consommationAlcool,
      frequenceAlcool: response.frequenceAlcool,
      tabagisme: response.tabagisme,
      activitePhysique: response.activitePhysique,
      personneContact: response.personneContact,
      numeroContact: response.numeroContact,
      nomAssurance: response.nomAssurance,
      numeroCarteAssurance: response.numeroCarteAssurance,
      couvertureAssurance: response.couvertureAssurance,
      dateDebutValidite: response.dateDebutValidite,
      dateFinValidite: response.dateFinValidite
    };
  }

  /**
   * Transforme les consultations vers le format unifié
   */
  private transformConsultations(consultations: any[]): ConsultationItem[] {
    return consultations.map(c => ({
      idConsultation: c.idConsultation,
      dateConsultation: c.dateConsultation,
      dateHeure: c.dateHeure,
      motif: c.motif || '',
      diagnosticPrincipal: c.diagnosticPrincipal,
      diagnostic: c.diagnostic,
      nomMedecin: c.nomMedecin || c.medecinNom,
      medecinNom: c.medecinNom,
      specialite: c.specialite,
      statut: c.statut || 'terminee'
    }));
  }

  /**
   * Transforme les ordonnances vers le format unifié
   */
  private transformOrdonnances(ordonnances: any[]): OrdonnanceItem[] {
    return ordonnances.map(o => ({
      idOrdonnance: o.idOrdonnance,
      dateOrdonnance: o.dateOrdonnance,
      dateCreation: o.dateCreation,
      nomMedecin: o.nomMedecin,
      statut: o.statut,
      medicaments: (o.medicaments || []).map((m: any) => ({
        nom: m.nom,
        nomMedicament: m.nomMedicament,
        dosage: m.dosage || m.quantite?.toString(),
        frequence: m.frequence,
        duree: m.duree,
        instructions: m.instructions
      }))
    }));
  }

  /**
   * Transforme les examens vers le format unifié
   */
  private transformExamens(examens: any[]): ExamenItem[] {
    return examens.map(e => ({
      idExamen: e.idExamen || e.idBulletinExamen,
      dateExamen: e.dateExamen,
      datePrescription: e.datePrescription || e.dateCreation,
      typeExamen: e.typeExamen || e.categorie || 'autre',
      nomExamen: e.nomExamen || e.nom,
      resultat: e.resultat,
      resultats: e.resultats,
      nomMedecin: e.nomMedecin,
      statut: e.statut || 'prescrit',
      urgent: e.urgent || e.urgence === 'urgente' || e.urgence === 'critique'
    }));
  }

  /**
   * Extrait les antécédents médicaux de la réponse
   */
  private extractAntecedents(response: any): any[] {
    const antecedents = [];
    
    if (response.maladiesChroniques) {
      antecedents.push({
        type: 'Maladies chroniques',
        description: response.maladiesChroniques,
        actif: true
      });
    }
    
    if (response.antecedentsFamiliaux && response.antecedentsFamiliauxDetails) {
      antecedents.push({
        type: 'Antécédents familiaux',
        description: response.antecedentsFamiliauxDetails,
        actif: true
      });
    }
    
    if (response.operationsChirurgicales && response.operationsDetails) {
      antecedents.push({
        type: 'Opérations chirurgicales',
        description: response.operationsDetails,
        actif: false
      });
    }
    
    return antecedents;
  }

  /**
   * Extrait les allergies de la réponse
   */
  private extractAllergies(response: any): any[] {
    if (!response.allergiesConnues || !response.allergiesDetails) {
      return [];
    }
    
    return [{
      type: 'Allergie',
      allergene: response.allergiesDetails,
      severite: 'Non spécifiée'
    }];
  }
}
