# Module Chirurgie - Plan d'implémentation

## 🏗️ Architecture corrigée

```
Consultation Chirurgicale (avis/diagnostic)
    ↓
    Décision : Intervention nécessaire ? 
    ↓ OUI
Programmation d'Intervention (formulaire dédié)
    ↓
Bloc Opératoire (intervention)
    ↓
Compte-Rendu Opératoire
    ↓
Consultations de Suivi Post-Opératoire
```

---

## ✅ P0 - Urgent (TERMINÉ ✓)

### 1. Examen Chirurgical Spécialisé
Étape "Examen Chirurgical" ajoutée dans le workflow (comme l'examen gynécologique) :
- [x] Entité `ConsultationChirurgicale` (backend) - `ConsultationChirurgicaleEntity.cs`
- [x] Table `consultation_chirurgicale` (MySQL) - `init.sql` + migration
- [x] DTO `ExamenChirurgicalDto` + `SaveExamenChirurgicalRequest`
- [x] Endpoint POST `/api/consultation-complete/{id}/examen-chirurgical`
- [x] Formulaire d'examen chirurgical (frontend) - `consultation-multi-etapes`
- [x] Champs : zone_examinee, inspection_locale, palpation_locale, signes_inflammatoires, cicatrices_existantes, mobilite_fonction, conclusion_chirurgicale, decision (surveillance/traitement_medical/indication_operatoire)
- [x] Spécialités chirurgicales autorisées : IDs 5, 6, 12, 21, 26, 31, 39, 41

### 2. Formulaire de Programmation d'Intervention
Formulaire séparé déclenché si decision = "indication_operatoire" :
- [x] Entité `ProgrammationIntervention` (backend) - `ProgrammationInterventionEntity.cs`
- [x] Table `programmation_intervention` (MySQL) - `init.sql` + migration
- [x] DTOs `ProgrammationInterventionDto`, `CreateProgrammationRequest`, `UpdateProgrammationRequest`
- [x] API CRUD complet - `ProgrammationInterventionController.cs`
- [x] Formulaire de programmation (frontend) - `programmation-intervention-panel`
- [x] Champs : type_intervention, classification_asa, risque_operatoire, consentement_eclaire, indication_operatoire, technique_prevue, date_prevue, notes_anesthesie, bilan_preoperatoire, instructions_patient, duree_estimee
- [x] Déclenchement automatique si decision = "indication_operatoire" dans l'étape examen chirurgical
- [x] Validation consentement éclairé et statuts de programmation

---

## 📋 P1 - Moyen terme

### 3. Suivi Post-Opératoire
- [ ] Formulaire de suivi post-op (état cicatrice, drains, douleur, complications)
- [ ] Checklist pré-opératoire automatisée

### 4. Compte-Rendu Opératoire
- [ ] Entité `CompteRenduOperatoire`
- [ ] Formulaire de saisie post-intervention

---

## 🚀 P2 - Long terme

### 5. Intégrations avancées
- [ ] Intégration bloc opératoire
- [ ] Protocoles de suivi post-opératoire
- [ ] Planification des rééducations
- [ ] Liaison avec service d'anesthésie

La consultation en chirurgie est actuellement traitée comme une consultation générale sans adaptation aux spécificités chirurgicales. Il manque les éléments essentiels pour une prise en charge chirurgicale complète.