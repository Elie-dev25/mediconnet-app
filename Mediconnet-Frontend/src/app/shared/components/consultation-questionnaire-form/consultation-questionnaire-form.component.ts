import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { ALL_ICONS_PROVIDER } from '../../icons';
import {
  ConsultationQuestionnaireService
} from '../../../services/consultation-questionnaire.service';
import { QuestionsPredefiniesService } from '../../../services/questions-predefinies.service';

export interface QuestionDisplay {
  id: string;
  texte: string;
  type: 'texte' | 'choix';
  options?: string[];
  obligatoire: boolean;
  reponse?: string;
  isLibre?: boolean;
  questionIdDb?: number;
}

@Component({
  selector: 'app-consultation-questionnaire-form',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LucideAngularModule],
  providers: [ALL_ICONS_PROVIDER],
  templateUrl: './consultation-questionnaire-form.component.html',
  styleUrls: ['./consultation-questionnaire-form.component.scss']
})
export class ConsultationQuestionnaireFormComponent implements OnInit {
  @Input() consultationId!: number;
  @Input() patientNom: string = '';
  @Input() mode: 'patient' | 'medecin' = 'patient';
  @Input() compact: boolean = false;
  @Input() specialiteId: number = 1; // Médecine générale par défaut
  @Input() typeVisite: 'premiere' | 'suivante' = 'premiere';

  @Output() saved = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  isLoading = false;
  isSaving = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  questions: QuestionDisplay[] = [];
  questionsLibres: QuestionDisplay[] = [];
  form!: FormGroup;

  addQuestionOpen = false;
  newQuestionText = '';
  newQuestionType = 'texte';

  constructor(
    private fb: FormBuilder,
    private questionnaireService: ConsultationQuestionnaireService,
    private questionsPredefiniesService: QuestionsPredefiniesService
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({});
    this.load();
  }

  load(): void {
    if (!this.consultationId) return;
    this.isLoading = true;
    this.errorMessage = null;
    this.successMessage = null;

    this.questionsPredefiniesService.getQuestionsParSpecialite(this.specialiteId, this.typeVisite).subscribe({
      next: (questionsPredefinies) => this.handleQuestionsPredefinies(questionsPredefinies),
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Impossible de charger les questions';
      }
    });
  }

  private handleQuestionsPredefinies(questionsPredefinies: { id: string; texte: string; type: string; options?: string[]; obligatoire: boolean }[]): void {
    this.questions = questionsPredefinies.map(q => ({
      id: q.id,
      texte: q.texte,
      type: q.type as 'texte' | 'choix',
      options: q.options,
      obligatoire: q.obligatoire,
      reponse: '',
      isLibre: false as const
    }));

    this.questionsPredefiniesService.getToutesQuestionsSpecialite(this.specialiteId).subscribe({
      next: (toutesQuestionsPredefinies) => this.loadDbQuestions(new Set(toutesQuestionsPredefinies.map(q => q.texte))),
      error: () => this.loadDbQuestionsFallback()
    });
  }

  private loadDbQuestions(tousTextesPredefinis: Set<string>): void {
    this.questionnaireService.getQuestions(this.consultationId).subscribe({
      next: (res) => {
        this.isLoading = false;
        const dbData = res.data || [];
        this.mapDbResponsesToQuestions(dbData);
        this.questionsLibres = this.extractQuestionsLibres(dbData, tousTextesPredefinis);
        this.rebuildControls();
      },
      error: () => {
        this.isLoading = false;
        this.rebuildControls();
      }
    });
  }

  private loadDbQuestionsFallback(): void {
    this.questionnaireService.getQuestions(this.consultationId).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.mapDbResponsesToQuestions(res.data || []);
        this.questionsLibres = [];
        this.rebuildControls();
      },
      error: () => {
        this.isLoading = false;
        this.rebuildControls();
      }
    });
  }

  private mapDbResponsesToQuestions(dbData: { texteQuestion: string; valeurReponse?: string | null; questionId: number }[]): void {
    for (const q of this.questions) {
      const dbQuestion = dbData.find(d => d.texteQuestion === q.texte);
      if (dbQuestion) {
        q.reponse = dbQuestion.valeurReponse || '';
        q.questionIdDb = dbQuestion.questionId;
      }
    }
  }

  private extractQuestionsLibres(dbData: { texteQuestion: string; valeurReponse?: string | null; questionId: number }[], tousTextesPredefinis: Set<string>): QuestionDisplay[] {
    return dbData
      .filter(d => !tousTextesPredefinis.has(d.texteQuestion))
      .map(d => ({
        id: `libre_${d.questionId}`,
        texte: d.texteQuestion,
        type: 'texte' as const,
        obligatoire: false,
        reponse: d.valeurReponse || '',
        isLibre: true,
        questionIdDb: d.questionId
      }));
  }

  private rebuildControls(): void {
    const group: Record<string, FormControl> = {};
    for (const q of this.questions) {
      const validators = q.obligatoire ? [Validators.required] : [];
      group[this.controlName(q.id)] = new FormControl(q.reponse ?? '', validators);
    }
    for (const q of this.questionsLibres) {
      group[this.controlName(q.id)] = new FormControl(q.reponse ?? '');
    }
    this.form = this.fb.group(group);
  }

  controlName(questionId: string | number): string {
    return `q_${questionId}`;
  }

  isMedecin(): boolean {
    return this.mode === 'medecin';
  }

  getInputKind(typeQuestion: string): 'textarea' | 'text' | 'number' | 'date' | 'select_yes_no' {
    const t = (typeQuestion || '').toLowerCase();
    if (t === 'nombre' || t === 'number') return 'number';
    if (t === 'date') return 'date';
    if (t === 'oui_non' || t === 'boolean') return 'select_yes_no';
    if (t === 'textarea') return 'textarea';
    return 'textarea';
  }

  formatDate(dateStr?: string | null): string {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    return d.toLocaleString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  onSubmit(): void {
    if (this.isSaving || !this.form) return;
    if (this.form.invalid) {
      this.errorMessage = 'Veuillez remplir tous les champs obligatoires';
      return;
    }

    this.isSaving = true;
    this.errorMessage = null;
    this.successMessage = null;

    // Collecter les réponses des questions prédéfinies et libres
    const allQuestions = [...this.questions, ...this.questionsLibres];
    const reponsesData = allQuestions.map(q => ({
      texteQuestion: q.texte,
      typeQuestion: q.type,
      valeurReponse: this.form.get(this.controlName(q.id))?.value ?? '',
      questionIdDb: q.questionIdDb
    }));

    this.questionnaireService.saveReponsesAvecQuestions(this.consultationId, reponsesData).subscribe({
      next: (res) => {
        this.isSaving = false;
        if (res.success) {
          this.successMessage = 'Réponses enregistrées';
          this.saved.emit();
        } else {
          this.errorMessage = res.message || 'Erreur lors de l\'enregistrement';
        }
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMessage = err?.error?.message || 'Erreur serveur';
      }
    });
  }

  toggleAddQuestion(): void {
    if (!this.isMedecin()) return;
    this.addQuestionOpen = !this.addQuestionOpen;
    this.newQuestionText = '';
    this.newQuestionType = 'texte';
  }

  addQuestionLibre(): void {
    if (!this.isMedecin() || this.isSaving) return;
    const texte = (this.newQuestionText || '').trim();
    if (!texte) {
      this.errorMessage = 'Texte de question obligatoire';
      return;
    }

    // Ajouter localement la question libre
    const newId = `libre_new_${Date.now()}`;
    const newQuestion: QuestionDisplay = {
      id: newId,
      texte: texte,
      type: 'texte',
      obligatoire: false,
      reponse: '',
      isLibre: true
    };

    this.questionsLibres = [...this.questionsLibres, newQuestion];
    this.form.addControl(this.controlName(newId), new FormControl(''));
    this.addQuestionOpen = false;
    this.newQuestionText = '';
    this.successMessage = 'Question ajoutée';
  }

  onCancel(): void {
    this.cancelled.emit();
  }
}
