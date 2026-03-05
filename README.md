# Mediconnet

[![Frontend CI](https://github.com/Elie-dev25/mediconnet-app/actions/workflows/frontend-ci.yml/badge.svg)](https://github.com/Elie-dev25/mediconnet-app/actions/workflows/frontend-ci.yml)
[![Backend CI](https://github.com/Elie-dev25/mediconnet-app/actions/workflows/backend-ci.yml/badge.svg)](https://github.com/Elie-dev25/mediconnet-app/actions/workflows/backend-ci.yml)
[![Docker Build](https://github.com/Elie-dev25/mediconnet-app/actions/workflows/docker-build.yml/badge.svg)](https://github.com/Elie-dev25/mediconnet-app/actions/workflows/docker-build.yml)
[![Security Scan](https://github.com/Elie-dev25/mediconnet-app/actions/workflows/security.yml/badge.svg)](https://github.com/Elie-dev25/mediconnet-app/actions/workflows/security.yml)

Mediconnet est une application de gestion médicale développée par **[[Elie NJINE]]**  pour **[Saaje Engineering & Consulting]** pour les établissements de santé au Cameroun.

Le constat de départ était simple : des dossiers patients éparpillés, des médecins qui cherchent des ordonnances perdues, des caissiers qui refont des calculs à la main. Des problèmes du quotidien, mais qui coûtent du temps — et parfois plus. Mediconnet est la réponse concrète à ça : une application pensée pour le contexte local, pas adaptée d'un template générique.

## Ce que ça fait concrètement

**Côté soins :**
- Les dossiers patients sont centralisés et accessibles en quelques secondes
- Les médecins ont un vrai workflow de consultation, pas juste un formulaire
- Les ordonnances sont digitalisées et liées directement à la pharmacie
- Le laboratoire et l'imagerie remontent leurs résultats dans le même système

**Côté gestion :**
- La pharmacie suit ses stocks en temps réel, avec les mouvements tracés
- La caisse génère les factures automatiquement, gère les échéanciers et les remboursements assurance
- Les lits, chambres et services d'hospitalisation sont visualisés et mis à jour en direct
- Les notifications passent par SignalR — quand quelque chose change, les bonnes personnes sont alertées immédiatement

**Côté admin :**
- Tableau de bord global pour les responsables d'établissement
- Gestion des utilisateurs avec rôles distincts (voir plus bas)
- Tous les documents (prescriptions, résultats, factures) sont stockés et liés aux bons dossiers

## Stack technique

**Frontend** — Angular 19 en standalone components, TailwindCSS, SCSS modulaire, Lucide Icons

**Backend** — .NET 8 Web API, MySQL 8, Entity Framework Core, JWT avec refresh tokens

**DevOps** — Docker + Docker Compose, GitHub Actions, GitHub Container Registry, Nginx en reverse proxy

## Lancer le projet

### Avec Docker (recommandé)

```bash
git clone https://github.com/Elie-dev25/mediconnet-app.git
cd mediconnet-app
docker-compose up -d --build
```

Une fois lancé :
- Frontend → http://localhost:4200
- API → http://localhost:8080
- Adminer (base de données) → http://localhost:8081
- MailHog → http://localhost:8025

### En local pour développer

**Frontend :**
```bash
cd Mediconnet-Frontend
npm install
npm start
```

**Backend :**
```bash
cd Mediconnet-Backend
dotnet restore
dotnet run
```

## CI/CD

| Workflow | Déclencheur | Ce qu'il fait |
|----------|-------------|---------------|
| **Frontend CI** | Push `main`/`develop`, PR | Build, lint, tests unitaires avec couverture |
| **Backend CI** | Push `main`/`develop`, PR | Build, tests .NET avec couverture |
| **Docker Build** | Push `main`, Release | Build et push vers GHCR |
| **Security** | Push, PR, chaque lundi 9h | CodeQL, Trivy, npm audit |
| **Deploy** | Push `main`/`develop` | Déploiement staging/production |
| **Release** | Tag `v*.*.*` ou manuel | Création de release GitHub |

```
Commit → Build & Tests → Docker Build → Deploy Staging
              │                │               │
         Coverage          Security        Deploy Prod
          Report             Scan
```

**Images Docker :**
```bash
docker pull ghcr.io/elie-dev25/mediconnet-app-frontend:latest
docker pull ghcr.io/elie-dev25/mediconnet-app-backend:latest
```

**Secrets à configurer pour le déploiement automatique :**

| Secret | Rôle |
|--------|------|
| `STAGING_SSH_KEY` | Clé SSH serveur staging |
| `STAGING_HOST` | Adresse serveur staging |
| `STAGING_USER` | Utilisateur SSH staging |
| `PRODUCTION_SSH_KEY` | Clé SSH serveur production |
| `PRODUCTION_HOST` | Adresse serveur production |
| `PRODUCTION_USER` | Utilisateur SSH production |

## Structure du projet

```
mediconnet-app/
├── .github/
│   └── workflows/
│       ├── frontend-ci.yml
│       ├── backend-ci.yml
│       ├── docker-build.yml
│       ├── security.yml
│       ├── deploy.yml
│       └── release.yml
├── Mediconnet-Frontend/
│   ├── src/
│   │   ├── app/
│   │   │   ├── pages/       # Pages par rôle
│   │   │   ├── shared/      # Composants partagés
│   │   │   └── services/    # Services Angular
│   │   └── styles/
│   └── Dockerfile
├── Mediconnet-Backend/
│   ├── Controllers/
│   ├── Services/
│   ├── DTOs/
│   └── Dockerfile
├── database/
│   └── migrations/
├── docker-compose.yml
└── README.md
```

## Rôles utilisateurs

| Rôle | Accès |
|------|-------|
| **Patient** | Prise de RDV, consultation de son dossier |
| **Médecin** | Consultations, prescriptions, planning |
| **Infirmier** | Paramètres vitaux |
| **Caissier** | Facturation, encaissements |
| **Pharmacien** | Stock, délivrance médicaments |
| **Accueil** | Enregistrement patients, gestion RDV |
| **Admin** | Configuration système, gestion utilisateurs |

## Base de données

MySQL 8 avec une cinquantaine de tables, migrations versionnées dans `database/migrations/`. Volume Docker persistant pour les documents, scripts de sauvegarde automatique.

**Tables principales :**
- `users`, `patients`, `personnel` — Utilisateurs et profils
- `consultations`, `ordonnance`, `ordonnance_medicament` — Workflow médical
- `hospitalisation`, `lit`, `chambre` — Gestion hospitalière
- `facture`, `ligne_facture`, `echeancier` — Facturation
- `examen`, `bulletin_examen`, `resultat_examen` — Laboratoire / Imagerie
- `mouvement_stock`, `medicament` — Pharmacie

## Documents et stockage

Les fichiers sont dans `./storage/` (volume Docker). Prescriptions, résultats d'examens, factures et documents patients sont générés en PDF et rattachés automatiquement aux bons dossiers.

## Sécurité

JWT avec refresh tokens, bcrypt pour les mots de passe, validation forte à l'inscription (8+ caractères, maj/min/chiffre), CORS configuré. Les pipelines CI/CD font tourner CodeQL, Trivy et les audits npm/NuGet à chaque push.

## Licence

Propriété de **Saaje Engineering & Consulting**. Tous droits réservés.

## Créer une release

```bash
# Via GitHub Actions : Actions > Release > Run workflow
# Renseigner le numéro de version (ex: 1.0.0) et le type (patch/minor/major)

# Ou directement via un tag Git
git tag v1.0.0
git push origin v1.0.0
```

---

Mediconnet est un produit de **Saaje Engineering & Consulting**.

Développé par **Elie Njine** — pour toute question technique ou collaboration, n'hésitez pas à nous contacter.

🔗 [LinkedIn](https://www.linkedin.com/in/elie-njine-736b04274) · [GitHub](https://github.com/Elie-dev25)
