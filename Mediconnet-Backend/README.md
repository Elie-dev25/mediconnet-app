# ?? MediConnect Backend - Documentation

## ?? Vue d'Ensemble

Backend .NET 10 pour la plateforme hospitalière MediConnect avec système multi-rôles et authentification JWT.

## ?? Démarrage Rapide

### Prérequis
- .NET 10 SDK
- SQL Server (ou LocalDB)
- Visual Studio 2022 ou VS Code

### Installation

```bash
# 1. Cloner le repository
git clone ...

# 2. Restaurer les packages
dotnet restore

# 3. Configurer la chaîne de connexion
# Éditer appsettings.json et modifier DefaultConnection

# 4. Créer et appliquer les migrations
dotnet ef migrations add InitialCreate
dotnet ef database update

# 5. Démarrer l'application
dotnet run
```

### Vérification
```
Swagger UI: https://localhost:5001/swagger
Health Check: https://localhost:5001/health
```

## ??? Architecture

### Structure du Projet
```
Core/               ? Enums, Entities, Interfaces
Data/               ? DbContext, Repositories
DTOs/               ? Data Transfer Objects
Services/           ? Business logic
Controllers/        ? API endpoints
Middleware/         ? Custom middleware
Validators/         ? Input validation
```

### Stack Technologique
- **Framework**: .NET 10
- **Database**: Entity Framework Core + SQL Server
- **Authentication**: JWT Bearer Tokens
- **Logging**: Serilog
- **Validation**: FluentValidation
- **Mapping**: AutoMapper

## ?? Authentification

### Login Flow

```
1. POST /api/auth/login
   ?? Username + Password
   ?? Returns: Token + Role + Permissions

2. Client stores token in localStorage

3. Each request:
   ?? Token in Authorization header
   ?? Server validates JWT

4. Response:
   ?? 200 OK if authorized
   ?? 401/403 if unauthorized
```

### Roles Disponibles
| Role | ID | Permissions |
|------|-----|-----------|
| Patient | 1 | Voir appointments, dossier médical |
| Doctor | 2 | Gérer patients, créer ordonnances |
| Nurse | 3 | Enregistrer vitals, assister patients |
| Cashier | 4 | Gérer factures, paiements |
| Administrator | 5 | Accès complet au système |

### Dashboards Redirection

Après login, les utilisateurs sont redirigés vers leur dashboard :

```csharp
{
  "dashboardRoute": "/dashboard/patient"    // Pour Patient
                    "/dashboard/doctor"     // Pour Doctor
                    "/dashboard/nurse"      // Pour Nurse
                    "/dashboard/cashier"    // Pour Cashier
                    "/dashboard/admin"      // Pour Admin
}
```

## ?? API Endpoints

### Authentification

| Endpoint | Méthode | Description | Auth |
|----------|---------|-------------|------|
| `/auth/login` | POST | Connexion | ? |
| `/auth/register` | POST | Enregistrement | ? |
| `/auth/logout` | POST | Déconnexion | ? |
| `/auth/profile` | GET | Profil utilisateur | ? |
| `/auth/refresh-token` | POST | Renouveler token | ? |
| `/auth/validate` | POST | Valider token | ? |

### Exemple Login

**Request:**
```json
POST /api/auth/login
{
  "username": "doctor1@hospital.com",
  "password": "Password123!"
}
```

**Response:**
```json
{
  "userId": 1,
  "username": "doctor1@hospital.com",
  "email": "doctor1@hospital.com",
  "firstName": "Pierre",
  "lastName": "Doctor",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "primaryRole": "Doctor",
  "roles": ["Doctor"],
  "permissions": [
    "Doctor_Read",
    "Doctor_ViewPatients",
    "Doctor_EditPatientRecords",
    "Doctor_CreatePrescription",
    "Doctor_ViewAppointments",
    "Doctor_ManageConsultations",
    "Doctor_ViewLabs"
  ],
  "dashboardRoute": "/dashboard/doctor",
  "expiresIn": 3600
}
```

## ?? Testing

### Avec Insomnia/Postman

1. Importer `test-api.http`
2. Exécuter les requêtes dans l'ordre
3. Copier le token de la réponse login
4. Utiliser le token pour les requêtes protégées

### Avec curl

```bash
# Login
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"doctor1@hospital.com","password":"Password123!"}'

# Get Profile (remplacer TOKEN)
curl -X GET https://localhost:5001/api/auth/profile \
  -H "Authorization: Bearer TOKEN"
```

## ?? Base de Données

### Entités Principales

- **User** - Utilisateurs du système
- **Role** - Rôles et leurs permissions
- **UserAuditLog** - Logs des actions utilisateurs

### Migrations

```bash
# Créer une migration
dotnet ef migrations add NomMigration

# Appliquer les migrations
dotnet ef database update

# Réinitialiser la DB
dotnet ef database drop --force
```

## ?? Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MediConnect;..."
  },
  "Jwt": {
    "Secret": "your-super-secret-key-32-chars-min",
    "Issuer": "MediConnect",
    "Audience": "MediConnectUsers",
    "ExpirationMinutes": 60
  }
}
```

## ?? Logging

### Configuration Serilog

Les logs sont sauvegardés dans `/logs/` :

```
logs/
??? app-20231128.txt
??? app-20231129.txt
??? ...
```

### Niveaux de Log
- **Debug** - Développement
- **Information** - Action importantes
- **Warning** - Problèmes potentiels
- **Error** - Erreurs
- **Fatal** - Erreurs critiques

## ?? Déploiement

### Production

```bash
# Build
dotnet publish -c Release -o ./publish

# Configuration
# - Editer appsettings.Production.json
# - Configurer les variables d'environnement
# - Configurer SSL/HTTPS

# Docker
docker build -t mediconnect-backend .
docker run -p 5000:5000 mediconnect-backend
```

## ?? Dépannage

### Problème: "Migrations" not found
```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
```

### Problème: Connexion base de données échoue
- Vérifier SQL Server est démarré
- Vérifier ConnectionString dans appsettings.json
- Vérifier credentials DB

### Problème: JWT Token invalide
- Vérifier le Jwt:Secret dans appsettings.json
- Vérifier l'expiration du token
- Vérifier le format Authorization header: `Bearer <token>`

## ?? Ressources

- [.NET Documentation](https://docs.microsoft.com/dotnet)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [JWT.io](https://jwt.io)
- [ASP.NET Core Security](https://docs.microsoft.com/aspnet/core/security)

## ?? Contribution

Pour contribuer au projet:

1. Créer une branche feature
2. Faire les changements
3. Commiter avec messages clairs
4. Pousser et créer une Pull Request

## ?? Licence

MIT

---

**Questions? Consultez la documentation complète dans le dossier projet.**

