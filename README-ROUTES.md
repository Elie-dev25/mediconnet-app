# 📡 Documentation des Routes API - MediConnect

## 📋 Table des matières

- [Authentification](#authentification)
- [Patients](#patients)
- [Médecins](#médecins)
- [Hospitalisations](#hospitalisations)
- [Rendez-vous](#rendez-vous)
- [Consultations](#consultations)
- [Examens](#examens)
- [Pharmacie](#pharmacie)
- [Facturation](#facturation)
- [Documents](#documents)
- [Administrateur](#administrateur)
- [🎯 Routes Critiques pour Tests de Charge](#-routes-critiques-pour-tests-de-charge)
- [📊 Métriques de Performance Attendues](#-métriques-de-performance-attendues)
- [🧪 Stratégie de Test de Charge](#-stratégie-de-test-de-charge)

## 🔐 Authentification

### `POST /api/auth/register`
**Inscription d'un nouvel patient**
```json
{
  "email": "patient@example.com",
  "password": "Password123!",
  "firstName": "Jean",
  "lastName": "Dupont",
  "telephone": "0612345678",
  "dateNaissance": "1990-01-01"
}
```
**Réponse :**
```json
{
  "success": true,
  "message": "Inscription réussie. Veuillez confirmer votre email.",
  "data": {
    "userId": "uuid-here",
    "email": "patient@example.com"
  }
}
```

### `POST /api/auth/login`
**Connexion utilisateur**
```json
{
  "email": "patient@example.com",
  "password": "Password123!"
}
```
**Réponse :**
```json
{
  "success": true,
  "data": {
    "token": "jwt-token-here",
    "refreshToken": "refresh-token-here",
    "user": {
      "id": "uuid",
      "email": "patient@example.com",
      "role": "patient",
      "firstName": "Jean",
      "lastName": "Dupont"
    }
  }
}
```

### `POST /api/auth/refresh`
**Rafraîchir le token JWT**
```json
{
  "refreshToken": "refresh-token-here"
}
```

### `GET /api/auth/confirm-email`
**Confirmation d'email**
```
GET /api/auth/confirm-email?token=email-confirmation-token
```

### `POST /api/auth/forgot-password`
**Demande de réinitialisation mot de passe**
```json
{
  "email": "patient@example.com"
}
```

### `POST /api/auth/reset-password`
**Réinitialisation mot de passe**
```json
{
  "token": "reset-token",
  "newPassword": "NewPassword123!"
}
```

---

## 👥 Patients

### `GET /api/patient/profile`
**Obtenir le profil du patient authentifié**
**Headers :** `Authorization: Bearer {token}`

**Réponse :**
```json
{
  "id": "uuid",
  "email": "patient@example.com",
  "firstName": "Jean",
  "lastName": "Dupont",
  "telephone": "0612345678",
  "dateNaissance": "1990-01-01",
  "adresse": "123 rue de la Santé",
  "profession": "Ingénieur",
  "groupeSanguin": "A+",
  "assurance": {
    "idAssurance": 1,
    "nom": "Assurance Maladie",
    "numero": "123456789",
    "couverture": 80
  }
}
```

### `PUT /api/patient/profile`
**Mettre à jour le profil patient**
```json
{
  "firstName": "Jean",
  "lastName": "Dupont",
  "telephone": "0612345678",
  "adresse": "123 rue de la Santé",
  "profession": "Ingénieur",
  "groupeSanguin": "A+"
}
```

### `GET /api/patient/dossier-medical`
**Obtenir le dossier médical complet**
**Réponse :**
```json
{
  "patient": {...},
  "consultations": [...],
  "hospitalisations": [...],
  "examens": [...],
  "ordonnances": [...],
  "factures": [...]
}
```

### `POST /api/patient/ordonnances`
**Créer une nouvelle ordonnance**
```json
{
  "idMedecin": "uuid-medecin",
  "medicaments": [
    {
      "idMedicament": 1,
      "nom": "Paracétamol",
      "dosage": "500mg",
      "duree": "7 jours",
      "instructions": "1 comprimé 3 fois par jour"
    }
  ]
}
```

---

## 👨‍⚕️ Médecins

### `GET /api/medecin/profile`
**Profil du médecin authentifié**

### `GET /api/medecin/patients`
**Liste des patients du médecin**
**Query params :** `page=1&limit=10&search=jean`

### `GET /api/medecin/patients/{id}`
**Détails d'un patient**

### `POST /api/medecin/consultations`
**Créer une consultation**
```json
{
  "idPatient": "uuid-patient",
  "motif": "Consultation générale",
  "diagnostic": "État de santé satisfaisant",
  "traitement": "Repos et hydratation",
  "prix": 5000
}
```

### `GET /api/medecin/consultations`
**Liste des consultations**

### `GET /api/medecin/consultations/{id}`
**Détails d'une consultation**

---

## 🏥 Hospitalisations

### `POST /api/medecin/hospitalisations`
**Admettre un patient**
```json
{
  "idPatient": "uuid-patient",
  "idService": 1,
  "motif": "Chirurgie programmée",
  "diagnosticPresomptif": "Appendicite"
}
```

### `GET /api/medecin/hospitalisations`
**Liste des hospitalisations**

### `GET /api/medecin/hospitalisations/{id}`
**Détails d'une hospitalisation**

### `PUT /api/medecin/hospitalisations/{id}/attribuer-lit`
**Attribuer un lit**
```json
{
  "idLit": 123
}
```

### `PUT /api/medecin/hospitalisations/{id}/terminer`
**Terminer une hospitalisation**
```json
{
  "motifSortie": "Guérison",
  "resumeMedical": "Patient amélioré, peut sortir"
}
```

---

## 📅 Rendez-vous

### `GET /api/rendez-vous/disponibilites`
**Voir les créneaux disponibles**
**Query params :** `date=2024-03-10&idMedecin=uuid`

### `POST /api/rendez-vous`
**Prendre un rendez-vous**
```json
{
  "idMedecin": "uuid-medecin",
  "idCreneau": 123,
  "type": "consultation",
  "motif": "Consultation de suivi"
}
```

### `GET /api/patient/rendez-vous`
**Rendez-vous du patient**

### `PUT /api/rendez-vous/{id}/annuler`
**Annuler un rendez-vous**

---

## 🔬 Examens

### `GET /api/laborantin/examens-catalogue`
**Catalogue des examens disponibles**

### `POST /api/laborantin/examens`
**Créer un examen**
```json
{
  "idPatient": "uuid-patient",
  "idExamenCatalogue": 1,
  "idMedecin": "uuid-medecin",
  "notes": "Examen de routine"
}
```

### `PUT /api/laborantin/examens/{id}/resultats`
**Ajouter les résultats**
```json
{
  "resultats": "Valeurs dans la normale",
  "fichierResultat": "base64-encoded-file"
}
```

---

## 💊 Pharmacie

### `GET /api/pharmacien/medicaments`
**Catalogue des médicaments**
**Query params :** `search=paracetamol`

### `GET /api/pharmacien/medicaments/{id}/stock`
**Stock d'un médicament**

### `POST /api/pharmacien/dispenser`
**Dispenser une ordonnance**
```json
{
  "idOrdonnance": "uuid-ordonnance",
  "medicaments": [
    {
      "idMedicament": 1,
      "quantite: 10,
      "instructions": "Comme prescrit"
    }
  ]
}
```

---

## 💰 Facturation

### `GET /api/factures/patient/{id}`
**Factures d'un patient**

### `GET /api/factures/{id}`
**Détails d'une facture**

### `POST /api/factures/{id}/paiement`
**Enregistrer un paiement**
```json
{
  "montant": 5000,
  "methode": "carte",
  "reference": "123456789"
}
```

### `GET /api/factures/{id}/pdf`
**Télécharger la facture en PDF**

---

## 📄 Documents

### `POST /api/documents/upload`
**Uploader un document**
**Content-Type :** `multipart/form-data`
```
file: [fichier]
type: "examen" | "consultation" | "hospitalisation"
idEntite: "uuid-entite"
```

### `GET /api/documents/{id}`
**Télécharger un document**

### `GET /api/patient/documents`
**Documents du patient**

---

## 👑 Administrateur

### `POST /api/admin/assurances`
**Créer une assurance**
```json
{
  "nom": "Nouvelle Assurance",
  "adresse": "123 rue de l'Assurance",
  "telephone": "0123456789",
  "email": "contact@assurance.com"
}
```

### `GET /api/admin/utilisateurs`
**Liste des utilisateurs**

### `PUT /api/admin/utilisateurs/{id}/role`
**Modifier le rôle d'un utilisateur**
```json
{
  "role": "medecin"
}
```

### `GET /api/admin/statistiques`
**Statistiques du système**
```json
{
  "totalPatients": 1500,
  "totalMedecins": 25,
  "totalConsultations": 5000,
  "totalFactures": 4800,
  "chiffreAffaires": 24000000
}
```

---

## 🔍 Codes d'erreur

| Code | Description |
|------|-------------|
| 200 | Succès |
| 201 | Créé |
| 400 | Requête invalide |
| 401 | Non authentifié |
| 403 | Permission refusée |
| 404 | Ressource non trouvée |
| 409 | Conflit (doublon) |
| 422 | Validation échouée |
| 500 | Erreur serveur |

---

## 📝 Notes importantes

### Authentication
- Toutes les routes protégées nécessitent : `Authorization: Bearer {token}`
- Les tokens JWT expirent après 24h
- Utilisez `/api/auth/refresh` pour obtenir un nouveau token

### Pagination
- Les routes de liste supportent : `?page=1&limit=10`
- Réponse par défaut : 20 résultats par page

### Rate limiting
- Limite : 100 requêtes/minute par IP
- Limite : 1000 requêtes/minute par utilisateur authentifié

### Validation
- Emails : format email valide
- Téléphones : format international (+225...)
- Dates : format ISO 8601 (YYYY-MM-DD)

### Uploads
- Taille max : 50MB
- Formats : PDF, JPG, PNG, DOC, DOCX
- Stockage : chiffré sur le serveur

---

## 🧪 Tests d'API

### Exemple cURL
```bash
# Connexion
curl -X POST http://localhost/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"patient@example.com","password":"Password123!"}'

# Profil patient
curl -X GET http://localhost/api/patient/profile \
  -H "Authorization: Bearer {token}"
```

### Postman Collection
Une collection Postman est disponible dans `/tests/postman/mediconnect-api.json`

---

## 🎯 Routes Critiques pour Tests de Charge

> **⚠️ Note** : Section ajoutée pour identifier les routes les plus sollicitées lors des tests de performance.

---

## 📊 Métriques de Performance Attendues

| Route Critique | P95 Response Time | P99 Response Time | Error Rate | CPU Impact |
|----------------|------------------|------------------|------------|------------|
| `POST /api/auth/login` | < 300ms | < 500ms | < 1% | Medium |
| `GET /api/patient/profile` | < 200ms | < 400ms | < 0.5% | Low |
| `GET /api/rendez-vous/creneaux/{id}` | < 250ms | < 450ms | < 0.5% | Medium |
| `POST /api/rendez-vous` | < 400ms | < 600ms | < 2% | High |
| `GET /api/pharmacie/medicaments` | < 200ms | < 350ms | < 0.5% | Low |
| `POST /api/pharmacie/dispensations` | < 500ms | < 800ms | < 2% | High |
| `POST /api/medecin/consultations` | < 400ms | < 700ms | < 2% | High |

---

## 🧪 Stratégie de Test de Charge

### 🎯 Objectifs
- **Utilisateurs simultanés** : 200 utilisateurs
- **Durée** : 10 minutes sustained
- **Montée** : 50 → 200 users en 2 minutes
- **Taux d'erreur** : < 2%
- **P95 Response Time** : < 500ms

### 📈 Scénarios Critiques

#### **Scénario 1 : Pic de connexion (9h)**
```javascript
// 50 users se connectent simultanément
POST /api/auth/login
GET /api/patient/profile
```

#### **Scénario 2 : Prise massive de RDV**
```javascript
// 100 users consultent créneaux et prennent RDV
GET /api/rendez-vous/creneaux/{medecinId}
POST /api/rendez-vous
```

#### **Scénario 3 : Activité pharmacie**
```javascript
// 50 pharmaciens consultent stock et dispensent
GET /api/pharmacie/medicaments
POST /api/pharmacie/dispensations
```

#### **Scénario 4 : Consultations médecins**
```javascript
// 30 médecins avec activité normale
GET /api/medecin/patients
POST /api/medecin/consultations
```

### 🔍 Points de surveillance

#### **CPU & Mémoire**
- **Backend** : < 75% CPU sustained
- **Database** : < 80% CPU
- **Nginx** : < 50% CPU

#### **Database**
- **Connections** : < 100 simultanées
- **Query time** : < 200ms P95
- **Deadlocks** : 0

#### **Network**
- **Bandwidth** : < 100 Mbps
- **Latency** : < 50ms intra-container

---

## 🚨 Routes à éviter pendant tests de charge

### ⚠️ **Opérations lourdes**
- `GET /api/patient/dossier-medical` - Requêtes complexes
- `POST /api/medecin/hospitalisations` - Transactions multi-tables
- `POST /api/documents/upload` - Upload fichiers

### ⚠️ **Routes admin**
- `GET /api/admin/statistiques` - Agrégations lourdes
- `POST /api/admin/assurances` - Création entités

---

## 📝 Notes d'optimisation

### **Cache recommandé**
- `GET /api/rendez-vous/creneaux/{id}` - 30 secondes
- `GET /api/pharmacie/medicaments` - 60 secondes
- `GET /api/medecin/patients` - 15 secondes

### **Database indexing**
- `email` sur `users` (login)
- `id_medecin, date` sur `rendez_vous`
- `id_medicament` sur `stock_medicaments`

### **Rate limiting**
- Auth : 10 req/s par IP
- Consultation : 5 req/s par user
- Upload : 2 req/s par user

---

**Dernière mise à jour :** 11/03/2026  
**Version API :** v1.0.0
