# Guide d'Installation et d'Exécution de Mediconnet

Ce guide explique comment installer et exécuter l'application Mediconnet de deux manières différentes : avec Docker (recommandé) ou manuellement sur Windows.

---

## 📋 Prérequis

### Pour Docker (Recommandé)
- **Docker Desktop** installé et démarré
- **Git** pour cloner le repository

### Pour Windows Manuel
- **Node.js 18+** et **npm**
- **.NET 8.0 SDK**
- **MySQL 8.0+** ou **MySQL Workbench**
- **Git**

---

## 🐳 Méthode 1: Docker (Recommandé)

### Étape 1: Cloner le repository
```bash
git clone https://github.com/Elie-dev25/mediconnet-app.git
cd mediconnet-app
```

### Étape 2: Configurer le fichier d'environnement

1. Copier le fichier exemple:
```bash
cp .env.example .env
```

2. Ouvrir le fichier `.env` et remplir les valeurs:
```env
# Base de données
DB_HOST=mediconnet-db
DB_PORT=3306
DB_NAME=mediconnet
DB_USER=app
DB_PASSWORD=app

# JWT (clé secrète pour l'authentification)
JWT_SECRET=VotreCleSecreteTresLongueEtComplexe123456789!
JWT_ISSUER=mediconnet
JWT_AUDIENCE=mediconnet-users

# Email (MailHog pour le développement)
EMAIL_SMTP_SERVER=mediconnet-mailhog
EMAIL_SMTP_PORT=1025
EMAIL_USE_SSL=false
EMAIL_SENDER_EMAIL=noreply@mediconnet.com
EMAIL_SENDER_NAME=Mediconnet

# URLs
FRONTEND_URL=http://localhost
API_URL=http://localhost/api
```

### Étape 3: Démarrer les conteneurs
```bash
docker-compose up -d --build
```

### Étape 4: Attendre le démarrage
Patientez 2-3 minutes pendant que les conteneurs se construisent et démarrent.

### Étape 5: Vérifier le statut
```bash
docker-compose ps
```

Tous les conteneurs devraient être "Running" ou "Healthy".

### Étape 6: Créer le premier administrateur

**IMPORTANT**: Avant de pouvoir utiliser l'application, vous devez créer un compte administrateur.

```bash
docker exec -it mediconnet-backend dotnet Mediconnet-Backend.dll --seed-admin --email admin@mediconnet.com --password Admin123! --nom Admin --prenom Systeme
```

Vous verrez un message de confirmation:
```
✅ Administrateur créé avec succès
    Email     : admin@mediconnet.com
    Nom       : Admin Systeme
    Téléphone : 000000000
```

### Étape 7: Accéder à l'application

| Service | URL | Description |
|---------|-----|-------------|
| **Application** | http://localhost | Interface principale (via Nginx) |
| **Frontend direct** | http://localhost:4200 | Accès direct au frontend |
| **Backend API** | http://localhost:5000 | API REST |
| **Swagger API** | http://localhost:5000/swagger | Documentation API |
| **Admin MySQL** | http://localhost:8080 | Adminer (gestion base de données) |
| **MailHog** | http://localhost:8025 | Interface emails de test |
| **MySQL** | localhost:3306 | Base de données (user: app, password: app) |

---

## 💻 Méthode 2: Windows Manuel

### Étape 1: Cloner le repository
```bash
git clone https://github.com/Elie-dev25/mediconnet-app.git
cd mediconnet-app
```

### Étape 2: Configurer la base de données MySQL

#### Option A: Avec MySQL Workbench
1. Ouvrir MySQL Workbench
2. Créer une nouvelle connexion avec:
   - Hostname: `localhost`
   - Port: `3306`
   - Username: `root`
   - Password: `root`
3. Créer la base de données `mediconnet`
4. Importer le fichier `database/mediconnet_schema.sql`

#### Option B: En ligne de commande
```sql
mysql -u root -p
CREATE DATABASE mediconnet;
USE mediconnet;
SOURCE database/mediconnet_schema.sql;
```

### Étape 3: Configurer le Backend

1. Naviguer vers le dossier backend:
```bash
cd Mediconnet-Backend
```

2. Restaurer les packages:
```bash
dotnet restore
```

3. Configurer la connexion dans `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=mediconnet;Uid=root;Pwd=root;"
  }
}
```

4. Exécuter les migrations:
```bash
dotnet ef database update
```

5. Démarrer le backend:
```bash
dotnet run
```

Le backend sera disponible sur http://localhost:5000

### Étape 4: Configurer le Frontend

1. Ouvrir un nouveau terminal
2. Naviguer vers le dossier frontend:
```bash
cd Mediconnet-Frontend
```

3. Installer les dépendances:
```bash
npm install
```

4. Démarrer le frontend:
```bash
ng serve
```

Le frontend sera disponible sur http://localhost:4200

### Étape 5: Créer le premier administrateur

**IMPORTANT**: Avant de pouvoir utiliser l'application, vous devez créer un compte administrateur.

```bash
cd Mediconnet-Backend
dotnet run --seed-admin --email admin@mediconnet.com --password Admin123! --nom Admin --prenom Systeme
```

Vous verrez un message de confirmation:
```
✅ Administrateur créé avec succès
    Email     : admin@mediconnet.com
    Nom       : Admin Systeme
    Téléphone : 000000000
```

### Étape 6: Accéder à l'application

| Service | URL | Description |
|---------|-----|-------------|
| **Frontend** | http://localhost:4200 | Interface utilisateur |
| **Backend API** | http://localhost:5000 | API REST |
| **Swagger API** | http://localhost:5000/swagger | Documentation API |
| **MySQL** | localhost:3306 | Base de données |

---

## 👤 Création des Utilisateurs

### Créer le premier administrateur

L'administrateur est le seul utilisateur qui peut être créé via la ligne de commande. Une fois connecté, l'administrateur peut créer tous les autres utilisateurs via l'interface d'administration.

#### Avec Docker:
```bash
docker exec -it mediconnet-backend dotnet Mediconnet-Backend.dll --seed-admin --email admin@mediconnet.com --password Admin123! --nom Admin --prenom Systeme
```

#### Sans Docker (Windows Manuel):
```bash
cd Mediconnet-Backend
dotnet run --seed-admin --email admin@mediconnet.com --password Admin123! --nom Admin --prenom Systeme
```

### Options disponibles:

| Option | Description | Obligatoire |
|--------|-------------|-------------|
| `--email` | Email de l'administrateur | ✅ Oui |
| `--password` | Mot de passe (généré automatiquement si non fourni) | ❌ Non |
| `--nom` | Nom de famille (défaut: "Admin") | ❌ Non |
| `--prenom` | Prénom (défaut: "Systeme") | ❌ Non |
| `--telephone` | Numéro de téléphone (défaut: "000000000") | ❌ Non |

### Exemple avec mot de passe généré automatiquement:
```bash
docker exec -it mediconnet-backend dotnet Mediconnet-Backend.dll --seed-admin --email admin@mediconnet.com
```

Le système générera un mot de passe sécurisé et l'affichera dans la console.

### Créer les autres utilisateurs

Une fois connecté en tant qu'administrateur:

1. Accédez à http://localhost (ou http://localhost:4200)
2. Connectez-vous avec le compte administrateur
3. Allez dans **Administration** → **Gestion des utilisateurs**
4. Cliquez sur **Ajouter un utilisateur**
5. Remplissez le formulaire avec les informations du nouvel utilisateur
6. Sélectionnez le rôle approprié (médecin, pharmacien, infirmier, etc.)

---

## 📁 Structure des Dossiers

```
mediconnet-app/
├── Mediconnet-Backend/          # API .NET 8.0
├── Mediconnet-Frontend/         # Application Angular
├── database/                    # Scripts SQL et migrations
├── docker-compose.yml           # Configuration Docker
├── docker-compose.override.yml  # Configuration locale (optionnel)
└── .env.example                 # Variables d'environnement
```

---

## 🚀 Commandes Utiles

### Docker
```bash
# Démarrer tous les services
docker-compose up -d

# Arrêter tous les services
docker-compose down

# Voir les logs
docker-compose logs -f

# Reconstruire les images
docker-compose up -d --build

# Nettoyer Docker
docker system prune -f
```

### Backend (.NET)
```bash
# Restaurer les packages
dotnet restore

# Compiler le projet
dotnet build

# Exécuter l'application
dotnet run

# Exécuter les migrations
dotnet ef database update

# Créer une nouvelle migration
dotnet ef migrations add NomMigration
```

### Frontend (Angular)
```bash
# Installer les dépendances
npm install

# Démarrer le serveur de développement
ng serve

# Compiler pour la production
ng build --configuration=production

# Exécuter les tests
ng test
```

---

## 🔍 Dépannage

### Problèmes Communs

#### Docker: "Port déjà utilisé"
```bash
# Vérifier qui utilise le port
netstat -ano | findstr :80
# Arrêter le processus ou changer le port dans docker-compose.yml
```

#### Backend: Erreur de connexion MySQL
- Vérifiez que MySQL est démarré
- Vérifiez les identifiants dans `appsettings.json`
- Assurez-vous que la base de données `mediconnet` existe

#### Frontend: Erreur de dépendances
```bash
# Supprimer node_modules et réinstaller
rm -rf node_modules package-lock.json
npm install
```

#### Problèmes de permissions (Linux/Mac)
```bash
# Donner les permissions nécessaires
sudo chown -R $USER:$USER ./
```

### Vérifier que tout fonctionne

1. **Backend**: http://localhost:5000/health (devrait retourner "Healthy")
2. **Swagger API**: http://localhost:5000/swagger (documentation interactive)
3. **Frontend**: http://localhost:4200 (devrait afficher la page de login)
4. **Adminer**: http://localhost:8080 (interface MySQL, serveur: mediconnet-db, user: app, password: app)
5. **MailHog**: http://localhost:8025 (interface emails de test)

---

## 📞 Support

En cas de problème:

1. Vérifiez les logs avec `docker-compose logs -f`
2. Consultez la documentation dans `README.md`
3. Ouvrez une issue sur GitHub: https://github.com/Elie-dev25/mediconnet-app/issues

---

## 🔄 Mise à jour

Pour mettre à jour l'application:

```bash
# Récupérer les dernières modifications
git pull origin main

# Avec Docker
docker-compose up -d --build

# Manuellement
# Mettre à jour le backend
cd Mediconnet-Backend
dotnet restore
dotnet build

# Mettre à jour le frontend
cd ../Mediconnet-Frontend
npm install
```

---

## 🎯 Conseil

**Utilisez toujours la méthode Docker** si possible. C'est plus simple, plus rapide et garantit que toutes les dépendances sont correctement configurées.
