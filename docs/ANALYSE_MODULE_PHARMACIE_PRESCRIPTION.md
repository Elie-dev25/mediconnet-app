# Rapport d'Analyse Complète : Module Pharmacie & Prescription

**Date :** 25 février 2026  
**Auteur :** Analyse technique Cascade  
**Version :** 1.0

---

## Table des matières

1. [Résumé exécutif](#1-résumé-exécutif)
2. [Architecture actuelle](#2-architecture-actuelle)
3. [Problèmes détectés](#3-problèmes-détectés)
4. [Analyse des points d'entrée de prescription](#4-analyse-des-points-dentrée-de-prescription)
5. [Fonctionnalités manquantes](#5-fonctionnalités-manquantes)
6. [Recommandations techniques](#6-recommandations-techniques)
7. [Architecture cible proposée](#7-architecture-cible-proposée)
8. [Plan d'action](#8-plan-daction)

---

## 1. Résumé exécutif

### Constat global

Le module Pharmacie et Prescription présente une **architecture fragmentée** avec plusieurs problèmes majeurs :

| Catégorie | Sévérité | Description |
|-----------|----------|-------------|
| **Duplication de code** | 🔴 Haute | Logique de prescription dupliquée dans 3+ contrôleurs |
| **Deux systèmes d'ordonnance** | 🔴 Haute | `Ordonnance` (table `prescription`) et `OrdonnanceElectronique` coexistent |
| **Incohérence des données** | 🟠 Moyenne | Les ordonnances ne sont pas toujours liées au bon contexte |
| **Fonctionnalités manquantes** | 🟠 Moyenne | Pas de gestion des interactions, alertes stock limitées |
| **Absence de service centralisé** | 🔴 Haute | Pas de `PrescriptionService` unifié côté backend |

### Points positifs

- ✅ Gestion du stock fonctionnelle (entrées, sorties, alertes)
- ✅ Dispensation avec facturation intégrée
- ✅ Mouvements de stock tracés
- ✅ Composants frontend réutilisables (`prescription-medicaments`, `prescription-examens`)

---

## 2. Architecture actuelle

### 2.1 Backend - Entités de prescription

```
┌─────────────────────────────────────────────────────────────────────┐
│                    SYSTÈME 1 : Ordonnance classique                 │
├─────────────────────────────────────────────────────────────────────┤
│  Table: prescription                                                │
│  ├── Ordonnance (id_ord, date, id_consultation, commentaire)       │
│  └── PrescriptionMedicament (id_prescription_med, id_ord,          │
│       id_medicament, quantite, posologie, frequence, etc.)         │
│                                                                     │
│  Utilisé par: ConsultationCompleteController, MedecinController    │
│  Dispensé par: PharmacieStockService                               │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                SYSTÈME 2 : Ordonnance électronique                  │
├─────────────────────────────────────────────────────────────────────┤
│  Table: OrdonnancesElectroniques                                    │
│  ├── OrdonnanceElectronique (CodeUnique, QRCode, Statut,           │
│  │    DateExpiration, Renouvelable, IdPharmacieExterne, etc.)      │
│  └── LignePrescription (Dosage, Substitutable, Dispense, etc.)     │
│                                                                     │
│  Utilisé par: PrescriptionElectroniqueController                   │
│  NON dispensé par PharmacieStockService (circuit séparé)           │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 Backend - Contrôleurs impliqués

| Contrôleur | Responsabilité | Problème |
|------------|----------------|----------|
| `ConsultationCompleteController` | Prescription lors consultation | Logique inline dans `SavePlanTraitement` |
| `MedecinController` | Prescription hospitalisation | Logique inline dans `AjouterOrdonnanceHospitalisation` |
| `PrescriptionElectroniqueController` | Ordonnances électroniques | Système parallèle non intégré |
| `PharmacieController` | Dispensation | Ne gère que les `Ordonnance` classiques |

### 2.3 Frontend - Composants

| Composant | Utilisation | Réutilisable |
|-----------|-------------|--------------|
| `prescription-medicaments` | Consultation multi-étapes, Hospitalisation | ✅ Oui |
| `prescription-examens` | Consultation multi-étapes | ✅ Oui |
| `ordonnance-hospitalisation-panel` | Hospitalisation | ✅ Oui |
| `fiche-patient-panel` | Actions rapides (Prescrire) | ⚠️ Événements émis mais non implémentés |

---

## 3. Problèmes détectés

### 3.1 🔴 P1 - Duplication de la logique de création d'ordonnance

**Localisation :**
- `ConsultationCompleteController.cs` lignes 920-978
- `MedecinController.cs` lignes 1720-1772

**Code dupliqué :**
```csharp
// Pattern répété dans les deux contrôleurs
var ordonnance = new Ordonnance
{
    IdConsultation = idConsultation,
    Date = DateTime.UtcNow,
    Commentaire = notes
};
_context.Ordonnances.Add(ordonnance);
await _context.SaveChangesAsync();

foreach (var med in medicaments)
{
    // Recherche médicament par nom (logique identique)
    var medicament = await _context.Medicaments
        .FirstOrDefaultAsync(m => m.Nom.Contains(med.NomMedicament));
    
    // Création si inexistant (logique identique)
    if (medicament == null) { ... }
    
    var prescriptionMed = new PrescriptionMedicament { ... };
    _context.PrescriptionMedicaments.Add(prescriptionMed);
}
```

**Impact :**
- Maintenance difficile (corrections à faire en plusieurs endroits)
- Risque d'incohérence entre les différents flux
- Pas de validation centralisée

### 3.2 🔴 P2 - Deux systèmes d'ordonnance non intégrés

**Problème :**
- `Ordonnance` (table `prescription`) : utilisé pour consultations et hospitalisations
- `OrdonnanceElectronique` : système parallèle avec QR code, transmission pharmacie externe

**Conséquences :**
- La pharmacie interne (`PharmacieStockService`) ne voit que les `Ordonnance`
- Les `OrdonnanceElectronique` ne sont pas dispensables en interne
- Confusion sur quel système utiliser

### 3.3 🟠 P3 - Prescription depuis fiche patient non implémentée

**Localisation :** `fiche-patient-panel.component.ts`

```typescript
@Output() faireOrdonnance = new EventEmitter<number>();

onFaireOrdonnance(): void {
  if (this.patientId) {
    this.faireOrdonnance.emit(this.patientId);
  }
}
```

**Problème :** L'événement est émis mais **aucun handler n'est implémenté** dans les composants parents (`patients.component.ts`, `dashboard.component.ts`).

### 3.4 🟠 P4 - Ordonnances hospitalisation sans consultation

**Localisation :** `MedecinController.cs` ligne 1700-1720

```csharp
// Si pas de consultation, on en crée une "fantôme"
var consultation = new Consultation
{
    IdPatient = hospitalisation.IdPatient,
    IdMedecin = userId.Value,
    DateHeure = DateTime.UtcNow,
    Motif = "Ordonnance hospitalisation",
    Statut = "terminee"
};
```

**Impact :**
- Création de consultations "fantômes" pour rattacher les ordonnances
- Pollution des données de consultation
- Statistiques faussées

### 3.5 🟡 P5 - Recherche médicament par nom approximative

**Localisation :** `ConsultationCompleteController.cs` ligne 948

```csharp
var medicament = await _context.Medicaments
    .FirstOrDefaultAsync(m => m.Nom.Contains(med.NomMedicament) 
                           || med.NomMedicament.Contains(m.Nom));
```

**Problème :**
- Recherche approximative peut matcher le mauvais médicament
- Pas de validation de l'ID médicament depuis l'autocomplete
- Création automatique de médicaments si non trouvé

---

## 4. Analyse des points d'entrée de prescription

### 4.1 Consultation classique

| Aspect | État | Détail |
|--------|------|--------|
| Point d'entrée | `POST /api/consultation/{id}/plan-traitement` | ✅ Fonctionnel |
| Composant frontend | `consultation-multi-etapes` | ✅ Réutilise `prescription-medicaments` |
| Lien ordonnance-consultation | `Ordonnance.IdConsultation` | ✅ Correct |
| Visibilité pharmacie | Via `PharmacieController.GetOrdonnances` | ✅ Fonctionnel |

### 4.2 Hospitalisation

| Aspect | État | Détail |
|--------|------|--------|
| Point d'entrée | `POST /api/medecin/hospitalisation/{id}/ordonnance` | ✅ Fonctionnel |
| Composant frontend | `ordonnance-hospitalisation-panel` | ✅ Réutilise `prescription-medicaments` |
| Lien ordonnance-hospitalisation | Via consultation "fantôme" | ⚠️ Indirect |
| Visibilité pharmacie | Via `PharmacieController.GetOrdonnances` | ✅ Fonctionnel |

### 4.3 Fiche patient (Prescrire directement)

| Aspect | État | Détail |
|--------|------|--------|
| Point d'entrée | **NON IMPLÉMENTÉ** | 🔴 Manquant |
| Composant frontend | Bouton existe dans `fiche-patient-panel` | ⚠️ Événement non géré |
| Lien ordonnance-patient | N/A | 🔴 Pas de contexte |
| Visibilité pharmacie | N/A | 🔴 Non applicable |

---

## 5. Fonctionnalités manquantes

### 5.1 Module Pharmacie

| Fonctionnalité | Priorité | Description |
|----------------|----------|-------------|
| **Alertes interactions médicamenteuses** | 🔴 Haute | Aucune vérification des interactions lors de la prescription |
| **Historique patient côté pharmacie** | 🟠 Moyenne | Le pharmacien ne voit pas l'historique des dispensations du patient |
| **Substitution médicament** | 🟠 Moyenne | Pas de workflow pour proposer un générique |
| **Ordonnances renouvelables** | 🟡 Basse | Existe dans `OrdonnanceElectronique` mais pas intégré |
| **Scan QR code** | 🟡 Basse | Existe dans `PrescriptionElectroniqueController` mais non utilisé |
| **Gestion des lots** | 🟡 Basse | `NumeroLot` existe mais pas de traçabilité complète |
| **Alertes péremption avancées** | 🟡 Basse | Alertes basiques, pas de workflow de retrait |

### 5.2 Module Prescription

| Fonctionnalité | Priorité | Description |
|----------------|----------|-------------|
| **Prescription hors consultation** | 🔴 Haute | Impossible de prescrire sans consultation |
| **Modèles d'ordonnance** | 🟠 Moyenne | Pas de templates réutilisables |
| **Protocoles thérapeutiques** | 🟠 Moyenne | Pas de protocoles prédéfinis par pathologie |
| **Impression ordonnance** | 🟠 Moyenne | Pas de génération PDF |
| **Signature électronique** | 🟡 Basse | Non implémenté |

---

## 6. Recommandations techniques

### 6.1 R1 - Créer un service centralisé `IPrescriptionService`

```csharp
public interface IPrescriptionService
{
    // Création d'ordonnance unifiée
    Task<OrdonnanceResult> CreerOrdonnanceAsync(CreateOrdonnanceRequest request);
    
    // Contextes supportés
    Task<OrdonnanceResult> CreerOrdonnanceConsultationAsync(int idConsultation, List<MedicamentPrescritDto> medicaments, string? notes);
    Task<OrdonnanceResult> CreerOrdonnanceHospitalisationAsync(int idAdmission, List<MedicamentPrescritDto> medicaments, string? notes);
    Task<OrdonnanceResult> CreerOrdonnanceDirecteAsync(int idPatient, int idMedecin, List<MedicamentPrescritDto> medicaments, string? notes);
    
    // Gestion
    Task<OrdonnanceDto?> GetOrdonnanceAsync(int idOrdonnance);
    Task<List<OrdonnanceDto>> GetOrdonnancesPatientAsync(int idPatient);
    Task<bool> AnnulerOrdonnanceAsync(int idOrdonnance, string motif);
    
    // Validation
    Task<ValidationResult> ValiderPrescriptionAsync(int idPatient, List<MedicamentPrescritDto> medicaments);
}
```

### 6.2 R2 - Unifier les entités d'ordonnance

**Option A : Enrichir `Ordonnance` existante**
```csharp
public class Ordonnance
{
    // Existant
    public int IdOrdonnance { get; set; }
    public DateTime Date { get; set; }
    public int? IdConsultation { get; set; }
    
    // Nouveaux champs
    public int IdPatient { get; set; }  // Lien direct au patient
    public int IdMedecin { get; set; }  // Prescripteur
    public int? IdHospitalisation { get; set; }  // Contexte hospitalisation
    public string? CodeUnique { get; set; }  // Pour QR code
    public string Statut { get; set; }  // active, dispensee, annulee
    public DateTime? DateExpiration { get; set; }
    public bool Renouvelable { get; set; }
    public string TypeContexte { get; set; }  // consultation, hospitalisation, directe
}
```

**Option B : Migrer vers `OrdonnanceElectronique`**
- Plus complet mais nécessite migration des données existantes

**Recommandation : Option A** (moins de risque, évolution progressive)

### 6.3 R3 - Implémenter la prescription directe

```typescript
// patients.component.ts
onFaireOrdonnance(patientId: number): void {
  this.showPrescriptionModal = true;
  this.selectedPatientForPrescription = patientId;
}

// Nouveau composant: prescription-directe-modal
@Component({
  selector: 'app-prescription-directe-modal',
  template: `
    <app-modal [isOpen]="isOpen" title="Nouvelle ordonnance" (closed)="close()">
      <app-prescription-medicaments 
        [(medicaments)]="medicaments">
      </app-prescription-medicaments>
      <button (click)="valider()">Créer l'ordonnance</button>
    </app-modal>
  `
})
```

### 6.4 R4 - Ajouter la validation des interactions

```csharp
public class InteractionMedicamenteuseService : IInteractionMedicamenteuseService
{
    public async Task<List<InteractionAlert>> VerifierInteractionsAsync(
        int idPatient, 
        List<int> idMedicaments)
    {
        // 1. Récupérer les médicaments en cours du patient
        var medicamentsEnCours = await GetMedicamentsEnCoursPatientAsync(idPatient);
        
        // 2. Vérifier les interactions connues
        var interactions = await _context.InteractionsMedicamenteuses
            .Where(i => idMedicaments.Contains(i.IdMedicament1) 
                     && medicamentsEnCours.Contains(i.IdMedicament2))
            .ToListAsync();
        
        return interactions.Select(MapToAlert).ToList();
    }
}
```

---

## 7. Architecture cible proposée

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              FRONTEND                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────┐  ┌─────────────────────┐  ┌─────────────────────┐ │
│  │ consultation-multi- │  │ hospitalisation-    │  │ prescription-       │ │
│  │ etapes              │  │ details-panel       │  │ directe-modal       │ │
│  └─────────┬───────────┘  └─────────┬───────────┘  └─────────┬───────────┘ │
│            │                        │                        │             │
│            └────────────────────────┼────────────────────────┘             │
│                                     ▼                                       │
│                    ┌────────────────────────────────┐                       │
│                    │   prescription-medicaments     │  (composant partagé)  │
│                    │   prescription-examens         │                       │
│                    └────────────────┬───────────────┘                       │
│                                     │                                       │
│                                     ▼                                       │
│                    ┌────────────────────────────────┐                       │
│                    │   prescription.service.ts      │  (service unifié)     │
│                    └────────────────┬───────────────┘                       │
└─────────────────────────────────────┼───────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              BACKEND                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    PrescriptionController                            │   │
│  │  POST /api/prescription                                              │   │
│  │  POST /api/prescription/consultation/{id}                            │   │
│  │  POST /api/prescription/hospitalisation/{id}                         │   │
│  │  POST /api/prescription/directe                                      │   │
│  │  GET  /api/prescription/patient/{id}                                 │   │
│  │  POST /api/prescription/{id}/annuler                                 │   │
│  └─────────────────────────────┬───────────────────────────────────────┘   │
│                                │                                           │
│                                ▼                                           │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    IPrescriptionService                              │   │
│  │  - CreerOrdonnanceAsync()                                            │   │
│  │  - ValiderPrescriptionAsync()                                        │   │
│  │  - GetOrdonnancesPatientAsync()                                      │   │
│  └─────────────────────────────┬───────────────────────────────────────┘   │
│                                │                                           │
│              ┌─────────────────┼─────────────────┐                         │
│              ▼                 ▼                 ▼                         │
│  ┌───────────────────┐ ┌───────────────┐ ┌───────────────────┐            │
│  │ IInteractionService│ │ IStockService │ │ INotificationService│           │
│  └───────────────────┘ └───────────────┘ └───────────────────┘            │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    Ordonnance (entité unifiée)                       │   │
│  │  - IdPatient, IdMedecin (obligatoires)                               │   │
│  │  - IdConsultation, IdHospitalisation (optionnels)                    │   │
│  │  - TypeContexte: consultation | hospitalisation | directe           │   │
│  │  - Statut: active | dispensee | annulee | expiree                   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           MODULE PHARMACIE                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    PharmacieController                               │   │
│  │  GET  /api/pharmacie/ordonnances (toutes les ordonnances actives)   │   │
│  │  POST /api/pharmacie/dispensations                                   │   │
│  │  GET  /api/pharmacie/patient/{id}/historique                        │   │
│  └─────────────────────────────┬───────────────────────────────────────┘   │
│                                │                                           │
│                                ▼                                           │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    IPharmacieStockService                            │   │
│  │  - DispenserOrdonnanceAsync() (existant, à enrichir)                │   │
│  │  - GetHistoriquePatientAsync() (nouveau)                            │   │
│  │  - VerifierStockDisponibleAsync() (existant)                        │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 8. Plan d'action

### Phase 1 : Centralisation (2-3 jours)

| Tâche | Priorité | Effort |
|-------|----------|--------|
| Créer `IPrescriptionService` et `PrescriptionService` | 🔴 Haute | 4h |
| Créer `PrescriptionController` unifié | 🔴 Haute | 2h |
| Migrer logique de `ConsultationCompleteController.SavePlanTraitement` | 🔴 Haute | 2h |
| Migrer logique de `MedecinController.AjouterOrdonnanceHospitalisation` | 🔴 Haute | 2h |
| Ajouter champs `IdPatient`, `IdMedecin`, `TypeContexte` à `Ordonnance` | 🔴 Haute | 1h |
| Migration SQL pour les nouveaux champs | 🔴 Haute | 1h |

### Phase 2 : Prescription directe (1-2 jours)

| Tâche | Priorité | Effort |
|-------|----------|--------|
| Créer endpoint `POST /api/prescription/directe` | 🟠 Moyenne | 2h |
| Créer `prescription.service.ts` frontend unifié | 🟠 Moyenne | 2h |
| Créer `prescription-directe-modal` component | 🟠 Moyenne | 3h |
| Implémenter handler dans `patients.component.ts` | 🟠 Moyenne | 1h |

### Phase 3 : Enrichissement pharmacie (2-3 jours)

| Tâche | Priorité | Effort |
|-------|----------|--------|
| Ajouter historique patient côté pharmacie | 🟠 Moyenne | 3h |
| Créer table `interactions_medicamenteuses` | 🟠 Moyenne | 2h |
| Implémenter `IInteractionMedicamenteuseService` | 🟠 Moyenne | 4h |
| Intégrer alertes interactions dans prescription | 🟠 Moyenne | 2h |
| Améliorer alertes péremption | 🟡 Basse | 2h |

### Phase 4 : Nettoyage (1 jour)

| Tâche | Priorité | Effort |
|-------|----------|--------|
| Supprimer code dupliqué des anciens contrôleurs | 🟡 Basse | 2h |
| Décider du sort de `OrdonnanceElectronique` | 🟡 Basse | 1h |
| Tests d'intégration | 🟠 Moyenne | 3h |
| Documentation API | 🟡 Basse | 1h |

---

## Conclusion

Le module Pharmacie & Prescription nécessite une **refactorisation structurelle** pour :

1. **Centraliser** la logique de prescription dans un service unique
2. **Unifier** les entités d'ordonnance
3. **Implémenter** la prescription directe depuis la fiche patient
4. **Enrichir** les fonctionnalités pharmacie (interactions, historique)

L'effort total estimé est de **6-9 jours de développement**, avec un gain significatif en maintenabilité et cohérence fonctionnelle.

---

*Fin du rapport*
