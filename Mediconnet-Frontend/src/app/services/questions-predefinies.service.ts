import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, forkJoin } from 'rxjs';
import { map, catchError, shareReplay, switchMap } from 'rxjs/operators';

export interface QuestionPredefinie {
  id: string;
  texte: string;
  type: 'texte' | 'choix';
  options?: string[];
  obligatoire: boolean;
}

export interface SpecialiteIndex {
  id: number;
  key: string;
  nom: string;
}

export interface QuestionsFile {
  specialiteId: number;
  specialiteNom: string;
  typeVisite: string;
  questions: QuestionPredefinie[];
}

@Injectable({
  providedIn: 'root'
})
export class QuestionsPredefiniesService {
  private readonly baseUrl = '/assets/data/questions';
  // Version pour éviter le cache navigateur sur les fichiers JSON
  private readonly cacheVersion = 'v=20260213';
  private indexCache$: Observable<SpecialiteIndex[]> | null = null;
  private questionsCache: Map<string, Observable<QuestionPredefinie[]>> = new Map();

  constructor(private http: HttpClient) {}

  /**
   * Charge l'index des spécialités disponibles
   */
  private loadIndex(): Observable<SpecialiteIndex[]> {
    if (!this.indexCache$) {
      this.indexCache$ = this.http.get<{ specialites: SpecialiteIndex[] }>(`${this.baseUrl}/index.json?${this.cacheVersion}`).pipe(
        map(data => data.specialites),
        shareReplay(1),
        catchError(err => {
          console.error('Erreur chargement index spécialités:', err);
          return of([]);
        })
      );
    }
    return this.indexCache$;
  }

  /**
   * Récupère les questions pour une spécialité donnée
   */
  getQuestionsParSpecialite(specialiteId: number, typeVisite: 'premiere' | 'suivante'): Observable<QuestionPredefinie[]> {
    console.log(`[Questions] Recherche questions pour spécialité ${specialiteId}, type: ${typeVisite}`);
    
    if (!specialiteId || specialiteId === 0) {
      console.log('[Questions] ID spécialité invalide');
      return of([]);
    }

    return this.loadIndex().pipe(
      map(specialites => {
        console.log('[Questions] Spécialités disponibles:', specialites);
        const specialite = specialites.find(s => s.id === specialiteId);
        console.log(`[Questions] Spécialité trouvée pour ID ${specialiteId}:`, specialite);
        return specialite;
      }),
      switchMap(specialite => {
        if (!specialite) {
          console.log(`[Questions] Aucune spécialité trouvée pour ID ${specialiteId}`);
          return of([]);
        }
        console.log(`[Questions] Chargement fichier: ${specialite.key}/${typeVisite}`);
        return this.loadQuestionsFile(specialite.key, typeVisite);
      }),
      catchError((err) => {
        console.error('[Questions] Erreur:', err);
        return of([]);
      })
    );
  }

  /**
   * Charge un fichier de questions spécifique
   */
  private loadQuestionsFile(specialiteKey: string, typeVisite: 'premiere' | 'suivante'): Observable<QuestionPredefinie[]> {
    const fileName = typeVisite === 'premiere' ? 'firstconsult.json' : 'secondconsult.json';
    const cacheKey = `${specialiteKey}-${typeVisite}`;

    if (!this.questionsCache.has(cacheKey)) {
      const url = `${this.baseUrl}/${specialiteKey}/${fileName}?${this.cacheVersion}`;
      console.log(`[Questions] URL du fichier: ${url}`);
      const obs$ = this.http.get<QuestionsFile>(url).pipe(
        map(data => {
          console.log(`[Questions] Données reçues pour ${specialiteKey}/${fileName}:`, data);
          return data.questions || [];
        }),
        shareReplay(1),
        catchError(err => {
          console.error(`Erreur chargement questions ${specialiteKey}/${fileName}:`, err);
          return of([]);
        })
      );
      this.questionsCache.set(cacheKey, obs$);
    }

    return this.questionsCache.get(cacheKey)!;
  }

  /**
   * Récupère les questions par clé de spécialité directement
   */
  getQuestionsByKey(specialiteKey: string, typeVisite: 'premiere' | 'suivante'): Observable<QuestionPredefinie[]> {
    if (!specialiteKey) {
      return of([]);
    }
    return this.loadQuestionsFile(specialiteKey, typeVisite);
  }

  /**
   * Liste toutes les spécialités disponibles avec questions
   */
  getSpecialitesDisponibles(): Observable<SpecialiteIndex[]> {
    return this.loadIndex();
  }

  /**
   * Récupère TOUTES les questions prédéfinies d'une spécialité (première + suivante)
   */
  getToutesQuestionsSpecialite(specialiteId: number): Observable<QuestionPredefinie[]> {
    if (!specialiteId || specialiteId === 0) {
      return of([]);
    }

    return this.loadIndex().pipe(
      map(specialites => specialites.find(s => s.id === specialiteId)),
      switchMap(specialite => {
        if (!specialite) {
          return of([]);
        }
        return forkJoin([
          this.loadQuestionsFile(specialite.key, 'premiere'),
          this.loadQuestionsFile(specialite.key, 'suivante')
        ]).pipe(
          map(([first, second]) => [...first, ...second])
        );
      }),
      catchError(() => of([]))
    );
  }
}
