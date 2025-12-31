import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs/operators';
import { LucideAngularModule } from 'lucide-angular';
import { PatientService, PatientBasicInfo } from '../../../services/patient.service';

@Component({
  selector: 'app-patient-search',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule],
  templateUrl: './patient-search.component.html',
  styleUrls: ['./patient-search.component.scss']
})
export class PatientSearchComponent implements OnInit {
  @Input() placeholder = 'Numéro de dossier, nom, prénom ou email...';
  @Input() showRecentOnLoad = true;
  @Input() limit = 50;
  @Output() patientSelected = new EventEmitter<PatientBasicInfo>();
  
  searchForm!: FormGroup;
  patients: PatientBasicInfo[] = [];
  isLoading = false;
  errorMessage: string | null = null;
  showResults = false;
  selectedPatient: PatientBasicInfo | null = null;

  constructor(
    private fb: FormBuilder,
    private patientService: PatientService
  ) {}

  ngOnInit(): void {
    this.initForm();
    if (this.showRecentOnLoad) {
      this.loadRecentPatients();
    }
  }

  private initForm(): void {
    this.searchForm = this.fb.group({
      searchTerm: ['']
    });

    this.searchForm.get('searchTerm')?.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(term => {
        if (term && term.trim().length > 0) {
          this.searchPatients(term);
        } else if (this.showRecentOnLoad) {
          this.loadRecentPatients();
        } else {
          this.patients = [];
          this.showResults = false;
        }
      });
  }

  loadRecentPatients(): void {
    this.isLoading = true;
    this.errorMessage = null;

    this.patientService.getRecentPatients(6).pipe(
      finalize(() => {
        this.isLoading = false;
      })
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.patients = response.patients;
          this.showResults = true;
        } else {
          this.errorMessage = response.message;
        }
      },
      error: (err) => {
        console.error('Erreur chargement patients récents:', err);
        this.errorMessage = err?.error?.message || 'Impossible de charger les patients récents';
      }
    });
  }

  searchPatients(searchTerm: string): void {
    // Ne pas chercher si un patient est déjà sélectionné et le terme correspond
    if (this.selectedPatient) {
      const selectedName = `${this.selectedPatient.prenom} ${this.selectedPatient.nom}`.toLowerCase();
      if (searchTerm.toLowerCase() === selectedName) {
        return;
      }
      // L'utilisateur modifie la recherche, on désélectionne
      this.selectedPatient = null;
    }

    this.isLoading = true;
    this.errorMessage = null;
    this.showResults = true;

    this.patientService.searchPatients({ searchTerm, limit: this.limit }).pipe(
      finalize(() => {
        this.isLoading = false;
      })
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.patients = response.patients;
        } else {
          this.errorMessage = response.message;
          this.patients = [];
        }
        // S'assurer que les résultats restent visibles
        if (!this.selectedPatient) {
          this.showResults = true;
        }
      },
      error: (err) => {
        console.error('Erreur recherche patients:', err);
        this.errorMessage = err?.error?.message || 'Erreur lors de la recherche';
        this.patients = [];
      }
    });
  }

  selectPatient(patient: PatientBasicInfo): void {
    this.selectedPatient = patient;
    this.searchForm.patchValue({ searchTerm: `${patient.prenom} ${patient.nom}` });
    this.showResults = false;
    this.patientSelected.emit(patient);
  }

  clearSearch(): void {
    this.searchForm.patchValue({ searchTerm: '' });
    this.selectedPatient = null;
    this.patients = [];
    this.showResults = false;
    if (this.showRecentOnLoad) {
      this.loadRecentPatients();
    }
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  calculateAge(dateStr?: string): number | null {
    if (!dateStr) return null;
    const birthDate = new Date(dateStr);
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    return age;
  }

  onFocus(): void {
    // Ne pas afficher les résultats si un patient est déjà sélectionné
    if (this.selectedPatient) {
      return;
    }
    
    const currentTerm = this.searchForm.get('searchTerm')?.value?.trim();
    
    if (currentTerm && currentTerm.length > 0) {
      // Si il y a un terme de recherche, afficher les résultats existants ou relancer la recherche
      if (this.patients.length > 0) {
        this.showResults = true;
      } else {
        this.searchPatients(currentTerm);
      }
    } else if (this.showRecentOnLoad) {
      this.loadRecentPatients();
    }
  }

  onBlur(): void {
    // Delay plus long pour permettre le clic sur un résultat
    setTimeout(() => {
      if (!this.selectedPatient) {
        this.showResults = false;
      }
    }, 250);
  }

  // Méthode pour forcer l'affichage des résultats (utile pour le débogage)
  keepResultsOpen(): void {
    this.showResults = true;
  }
}
