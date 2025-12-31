# Étape de construction - Utilisation de Node.js 20 (LTS)
FROM node:20-alpine AS build
WORKDIR /app

# Copier les fichiers de configuration des dépendances
COPY package*.json ./
COPY angular.json ./
COPY tsconfig*.json ./

# Installer les dépendances
RUN npm install

# Copier le code source
COPY . .

# Builder l'application avec la syntaxe Angular 21
RUN npm run build -- --configuration=production --output-hashing=all

# Étape de production
FROM nginx:alpine

# Copier la configuration Nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copier les fichiers buildés vers nginx
COPY --from=build /app/dist/mediconnet-frontend /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
