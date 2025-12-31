# ?? ACCÈS RAPIDES - MEDICONNECT

## ?? URLs IMPORTANTES

| Service | URL | Description |
|---------|-----|-------------|
| **Frontend** | http://localhost:4200 | Interface utilisateur |
| **Swagger API** | http://localhost:8080/swagger/index.html | Documentation API |
| **Backend API** | http://localhost:8080 | Base API |
| **MySQL** | localhost:3306 | Base de données |

---

## ?? IDENTIFIANTS BD

```
Host: localhost
Port: 3306
Database: mediconnect
User: app
Password: app
```

---

## ?? CRÉER UN COMPTE DE TEST

### Formulaire (Frontend)
```
Prénom: John
Nom: Doe
Email: john@example.com
Mot de passe: Test123!
Confirmer: Test123!
```

### API (cURL)
```bash
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName":"John",
    "lastName":"Doe",
    "email":"john@example.com",
    "password":"Test123!",
    "confirmPassword":"Test123!"
  }'
```

---

## ?? SE CONNECTER

### Formulaire (Frontend)
```
Email: john@example.com
Mot de passe: Test123!
```

### API (cURL)
```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email":"john@example.com",
    "password":"Test123!"
  }'
```

---

## ?? COMMANDES DOCKER

```bash
# Voir l'état
docker-compose ps

# Logs en temps réel
docker-compose logs -f

# Redémarrer tout
docker-compose restart

# Arrêter
docker-compose down

# Redémarrer complètement
docker-compose down -v && docker-compose up -d

# Logs d'un service
docker-compose logs -f mediconnet-backend
```

---

## ??? COMMANDES MYSQL

```bash
# Se connecter
docker exec -it mediconnet-mysql mysql -u app -papp mediconnect

# Voir les utilisateurs
SELECT * FROM utilisateurs;

# Voir les patients
SELECT * FROM patient;

# Voir les tables
SHOW TABLES;
```

---

## ?? ENDPOINTS API

### Auth
- `POST /api/auth/register` - Créer un compte
- `POST /api/auth/login` - Se connecter
- `GET /api/auth/profile` - Récupérer le profil (JWT Required)
- `POST /api/auth/logout` - Se déconnecter
- `POST /api/auth/validate` - Valider token

---

## ?? EXEMPLE COMPLET

### 1. Créer un compte
```bash
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName":"Alice",
    "lastName":"Smith",
    "email":"alice@hospital.com",
    "password":"Alice123!",
    "confirmPassword":"Alice123!"
  }'
```

**Réponse:**
```json
{
  "message": "Utilisateur enregistré avec succès",
  "user": {
    "idUser": "USER_abc123",
    "nom": "Smith",
    "prenom": "Alice",
    "email": "alice@hospital.com",
    "role": "patient",
    "createdAt": "2025-11-28T15:20:00Z"
  }
}
```

### 2. Se connecter
```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email":"alice@hospital.com",
    "password":"Alice123!"
  }'
```

**Réponse:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "idUser": "USER_abc123",
  "nom": "Smith",
  "prenom": "Alice",
  "email": "alice@hospital.com",
  "role": "patient",
  "message": "Connexion réussie",
  "expiresIn": 3600
}
```

### 3. Utiliser le token
```bash
curl -X GET http://localhost:8080/api/auth/profile \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

---

## ? VÉRIFIER QUE TOUT FONCTIONNE

```bash
# 1. Frontend accessible
curl http://localhost:4200

# 2. Backend accessible
curl http://localhost:8080/swagger/index.html

# 3. MySQL accessible
docker exec mediconnet-mysql mysql -u app -papp mediconnect -e "SELECT 1"

# 4. Utilisateurs en BD
docker exec mediconnet-mysql mysql -u app -papp mediconnect -e "SELECT COUNT(*) FROM utilisateurs;"
```

---

## ?? PORTS

| Service | Port | Container Port |
|---------|------|------------------|
| Frontend | 4200 | 80 |
| Backend | 8080 | 8080 |
| MySQL | 3306 | 3306 |

---

## ?? PROBLÈMES COURANTS

### "Connection refused"
? Attendez 30-60 secondes que les services démarrent
? Vérifiez avec `docker-compose ps`

### "Email déjà utilisé"
? Utilisez un email différent ou vérifiez la BD

### "Mot de passe incorrect"
? Vérifiez la casse et les espaces

### "Cannot POST /api/auth/register"
? Le frontend proxy n'est pas configuré
? Vérifiez que nginx.conf est utilisé

---

## ?? FICHIERS IMPORTANTS

- `FINAL_BUILD_SUMMARY.md` - Résumé complet
- `docker-compose.yml` - Configuration Docker
- `Dockerfile` (Backend et Frontend)
- `nginx.conf` - Configuration Nginx
- `Program.cs` - Configuration .NET
- `auth.service.ts` - Service authentication Angular

---

**Bonne utilisation! ??**
