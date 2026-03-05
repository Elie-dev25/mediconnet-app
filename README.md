# Mediconnet App

[![Frontend CI](https://github.com/Elie-dev25/mediconnet-app/actions/workflows/frontend-ci.yml/badge.svg)](https://github.com/Elie-dev25/mediconnet-app/actions/workflows/frontend-ci.yml)
[![Backend CI](https://github.com/Elie-dev25/mediconnet-app/actions/workflows/backend-ci.yml/badge.svg)](https://github.com/Elie-dev25/mediconnet-app/actions/workflows/backend-ci.yml)
[![Docker Build](https://github.com/Elie-dev25/mediconnet-app/actions/workflows/docker-build.yml/badge.svg)](https://github.com/Elie-dev25/mediconnet-app/actions/workflows/docker-build.yml)
[![Security Scan](https://github.com/Elie-dev25/mediconnet-app/actions/workflows/security.yml/badge.svg)](https://github.com/Elie-dev25/mediconnet-app/actions/workflows/security.yml)

>  **Mon projet phare** - Une application de gestion médicale complète que j'ai développée pour les établissements de santé au Cameroun

Après avoir observé les défis quotidiens des hôpitaux et cliniques camerounais, j'ai créé **Mediconnet** pour digitaliser et simplifier la gestion médicale. C'est ma solution concrète aux problèmes de papier, de perte d'informations et de coordination que j'ai pu constater.

## 🚀 Ce que j'ai intégré

### Le quotidien de l'hôpital ✨
- **👥 Gestion des patients** : Fini les dossiers papier ! Tout est digitalisé, de l'inscription à l'historique complet
- **📅 Rendez-vous** : Prise de RDV en ligne et planning intelligent des médecins 
- **🩺 Consultations** : Un workflow complet qui suit le vrai parcours médical
- **💊 Pharmacie** : Gestion des stocks en temps réel et ordonnances digitalisées
- **💰 Caisse** : Facturation automatique et suivi des paiements
- **⚙️ Administration** : Tableau de bord complet pour gérer tout l'établissement

### Les fonctionnalités puissantes 🔥
- **🏥 Hospitalisation** : Gestion complète des lits, chambres et services médicaux
- **🔬 Examens** : Laboratoire et imagerie avec résultats intégrés
- **📊 Facturation avancée** : Échéanciers, remboursements assurances, tout est géré
- **🔔 Notifications** : Alertes temps réel avec SignalR pour ne rien manquer
- **📁 Documents** : Stockage sécurisé de tous les dossiers médicaux

## Stack Technique

### Frontend
- **Angular 19** avec standalone components
- **TailwindCSS** pour le styling
- **Lucide Icons** pour les icônes
- **SCSS** avec architecture modulaire

### Backend
- **.NET 8** Web API
- **MySQL 8** base de données
- **JWT** authentification
- **Entity Framework Core**

### DevOps
- **Docker & Docker Compose**
- **GitHub Actions** CI/CD complet
- **GitHub Container Registry** pour les images Docker
- **Nginx** reverse proxy

## Prérequis

- Docker & Docker Compose
- Node.js 20+ (pour développement local)
- .NET 8 SDK (pour développement local)

## 🚀 Lancez-le en 2 minutes !

### La méthode simple (Docker) 🐳

```bash
# Clonez mon projet
git clone https://github.com/Elie-dev25/mediconnet-app.git
cd mediconnet-app

# Uniquement cette commande et c'est parti !
docker-compose up -d --build

# Voilà ! L'application est accessible :
# 🌐 Frontend: http://localhost:4200
# ⚙️ Backend API: http://localhost:8080
# 🗄️ Adminer (DB): http://localhost:8081
# 📧 MailHog: http://localhost:8025
```

### Pour les développeurs 👨‍💻

#### Frontend (Angular)
```bash
cd Mediconnet-Frontend
npm install
npm start
# C'est parti sur http://localhost:4200
```

#### Backend (.NET)
```bash
cd Mediconnet-Backend
dotnet restore
dotnet run
# API dispo sur http://localhost:8080
```

## CI/CD

Le projet utilise GitHub Actions pour l'intégration et le déploiement continus avec une pipeline complète.

### Workflows disponibles

| Workflow | Déclencheur | Description |
|----------|-------------|-------------|
| **Frontend CI** | Push sur `main`/`develop`, PR | Build, lint, tests unitaires avec couverture |
| **Backend CI** | Push sur `main`/`develop`, PR | Build, tests .NET avec couverture |
| **Docker Build** | Push sur `main`, Release | Build et push des images vers GHCR |
| **Security** | Push, PR, Planifié (lundi 9h) | Scan de sécurité CodeQL, Trivy, npm audit |
| **Deploy** | Push sur `main`/`develop` | Déploiement staging/production |
| **Release** | Tag `v*.*.*`, Manuel | Création de releases GitHub |

### Pipeline CI/CD

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Commit    │────▶│   Build &   │────▶│   Docker    │────▶│   Deploy    │
│   & Push    │     │   Tests     │     │   Build     │     │   Staging   │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
                           │                   │                   │
                           ▼                   ▼                   ▼
                    ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
                    │  Coverage   │     │  Security   │     │   Deploy    │
                    │   Report    │     │    Scan     │     │ Production  │
                    └─────────────┘     └─────────────┘     └─────────────┘
```

### Images Docker

Les images sont publiées sur GitHub Container Registry :
```bash
# Frontend
docker pull ghcr.io/elie-dev25/mediconnet-app-frontend:latest

# Backend
docker pull ghcr.io/elie-dev25/mediconnet-app-backend:latest
```

### Configuration des secrets (pour le déploiement)

Pour activer le déploiement automatique, configurez ces secrets dans GitHub :

| Secret | Description |
|--------|-------------|
| `STAGING_SSH_KEY` | Clé SSH pour le serveur staging |
| `STAGING_HOST` | Adresse du serveur staging |
| `STAGING_USER` | Utilisateur SSH staging |
| `PRODUCTION_SSH_KEY` | Clé SSH pour le serveur production |
| `PRODUCTION_HOST` | Adresse du serveur production |
| `PRODUCTION_USER` | Utilisateur SSH production |

## Structure du projet

```
mediconnet-app/
├── .github/
│   └── workflows/           # GitHub Actions CI/CD
│       ├── frontend-ci.yml  # Tests et build Angular
│       ├── backend-ci.yml   # Tests et build .NET
│       ├── docker-build.yml # Build images Docker
│       ├── security.yml     # Scans de sécurité
│       ├── deploy.yml       # Déploiement staging/prod
│       └── release.yml      # Gestion des releases
├── Mediconnet-Frontend/     # Application Angular
│   ├── src/
│   │   ├── app/
│   │   │   ├── pages/       # Pages par rôle (patient, medecin, admin...)
│   │   │   ├── shared/      # Composants partagés
│   │   │   └── services/    # Services Angular
│   │   └── styles/          # SCSS globaux
│   └── Dockerfile
├── Mediconnet-Backend/      # API .NET
│   ├── Controllers/
│   ├── Services/
│   ├── DTOs/
│   └── Dockerfile
├── database/
│   └── migrations/          # Scripts SQL
├── docker-compose.yml
└── README.md
```

## Rôles utilisateurs

| Rôle | Description |
|------|-------------|
| **Patient** | Prise de RDV, consultation dossier médical |
| **Médecin** | Consultations, prescriptions, planning |
| **Infirmier** | Prise de paramètres vitaux |
| **Caissier** | Facturation, encaissements |
| **Pharmacien** | Gestion stock, délivrance médicaments |
| **Accueil** | Enregistrement patients, gestion RDV |
| **Admin** | Configuration système, gestion utilisateurs |

## Base de données

### MySQL 8
- **Structure** : Schéma normalisé avec 50+ tables
- **Migrations** : Scripts SQL versionnés dans `database/migrations/`
- **Stockage** : Volume Docker persistant pour les documents
- **Backup** : Scripts de sauvegarde automatique

### Tables principales
- `users`, `patients`, `personnel` - Utilisateurs et profils
- `consultations`, `ordonnance`, `ordonnance_medicament` - Workflow médical
- `hospitalisation`, `lit`, `chambre` - Gestion hospitalière
- `facture`, `ligne_facture`, `echeancier` - Facturation avancée
- `examen`, `bulletin_examen`, `resultat_examen` - Laboratoire/Imagerie
- `mouvement_stock`, `medicament` - Gestion pharmacie

## Stockage des documents

Les documents médicaux sont stockés dans le volume Docker `./storage/` :
- **Prescriptions** : PDF générés automatiquement
- **Résultats examens** : PDF et images médicales
- **Factures** : PDF officiels
- **Documents patients** : Scans et formulaires

## Sécurité

- **Authentification** : JWT avec refresh tokens
- **Mots de passe** : Validation (8+ caractères, majuscule, minuscule, chiffre)
- **CORS** : Protection configurée
- **Hashage** : bcrypt pour les mots de passe
- **CI/CD** : Scans automatiques avec CodeQL et Trivy
- **Dépendances** : Audit npm et NuGet automatique

## Créer une release

```bash
# Via GitHub Actions (recommandé)
# 1. Allez dans Actions > Release > Run workflow
# 2. Entrez le numéro de version (ex: 1.0.0)
# 3. Sélectionnez le type (patch/minor/major)

# Ou via tag Git
git tag v1.0.0
git push origin v1.0.0
```

## Licence

Ce projet est sous licence privée. Tous droits réservés.

## À propos

Je suis **Elie Njine**, développeur passionné par la transformation digitale du secteur médical en Afrique. 

**Mediconnet** n'est pas juste un projet technique, c'est ma contribution concrète pour améliorer les soins de santé au Cameroun. Chaque ligne de code a été pensée pour résoudre de vrais problèmes que j'ai observés sur le terrain.

**Discutons-en** : [elienjiedev@gmail.com](mailto:elienjiedev@gmail.com)  
**Connectons-nous** : [LinkedIn](https://www.linkedin.com/in/elie-njine-736b04274)  
**Suivez mon travail** : [GitHub](https://github.com/Elie-dev25)

---

*Prêt à transformer la gestion médicale ensemble ?*