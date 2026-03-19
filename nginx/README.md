# Nginx – Reverse Proxy pour Mediconnet

Ce dossier contient la configuration Nginx utilisée comme unique point d’entrée public pour l’application Mediconnet.

## Architecture actuelle

```
Internet
   |
   v
Nginx (port 80)
   ├─> /api/*      → Backend (mediconnet-backend:8080)
   └─> /*          → Frontend Angular (mediconnet-frontend:80)
```

- **Nginx** : Reverse proxy, compression, headers X-Forwarded-*.
- **Backend** : Écoute en HTTP interne sur `8080`, non exposé publiquement.
- **Frontend** : Écoute en HTTP interne sur `80`, non exposé publiquement.
- **MySQL/Adminer/MailHog** : Uniquement accessibles via le réseau Docker interne.

## Lancement

```bash
# Démarrer la stack (Nginx inclus)
docker-compose up -d

# Vérifier que tout est up
docker-compose ps
```

Accès public :
- Frontend : http://localhost
- API : http://localhost/api/...

## Accès aux services internes (MySQL, Adminer, MailHog)

Ces services ne sont plus exposés publiquement. Pour y accéder :

### Option 1 : Tunnel SSH (recommandé)

```bash
# MySQL
ssh -L 3306:mediconnet-db:3306 user@serveur
# puis connecte-toi sur localhost:3306 avec app/app

# Adminer
ssh -L 8888:mediconnet-adminer:8080 user@serveur
# puis ouvre http://localhost:8888

# MailHog
ssh -L 8025:mediconnet-mailhog:8025 user@serveur
# puis ouvre http://localhost:8025
```

### Option 2 : Réactiver temporairement les ports (développement uniquement)

Décommente les sections `ports:` dans `docker-compose.yml` pour le service voulu, puis `docker-compose up -d`.

## Logs et monitoring

- **Logs Nginx** : `docker logs mediconnet-nginx`
- **Logs Backend** : `docker logs mediconnet-backend`
- **Logs Frontend** : `docker logs mediconnet-frontend`

Pour voir les logs en temps réel :
```bash
docker-compose logs -f nginx
```

## Prochaines étapes (HTTPS en production)

1. **Obtenir un certificat** (Let’s Encrypt ou auto-signé pour test).
2. **Mettre à jour `nginx.conf`** :
   - Ajouter `listen 443 ssl;`
   - Configurer `ssl_certificate` et `ssl_certificate_key`.
   - Rediriger HTTP → HTTPS.
3. **Mettre à jour `docker-compose.yml`** pour exposer le port 443.
4. **Mettre à jour les URLs** dans `.env.production` pour qu’elles pointent vers `https://...`.

## Personnalisation

- **Taille max des uploads** : `client_max_body_size 50M;` dans `nginx.conf`.
- **Compression** : activée pour text/css/json/js/xml.
- **Headers** : Nginx ajoute `X-Real-IP`, `X-Forwarded-For`, `X-Forwarded-Proto`.

## Dépannage

- **404 sur /api/** : vérifie que le backend est bien démarré (`docker ps`).
- **Frontend inaccessible** : vérifie que le conteneur frontend est up.
- **Toujours sur l’ancien port** : arrête les anciens conteneurs (`docker-compose down`) avant de relancer.
