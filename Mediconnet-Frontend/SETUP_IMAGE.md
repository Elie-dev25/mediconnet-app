# 📝 Guide - Remplacer l'Image d'Accueil

## 🎯 Situation Actuelle

- Une image SVG **placeholder** est affichée à gauche
- Elle indique où placer votre vrai image

## ✅ Comment Remplacer l'Image

### Option 1 : Utiliser votre JPEG (Recommandé)

1. **Copiez votre image** `accueil.jpeg` dans :
   ```
   Mediconnet-Frontend/src/assets/images/
   ```

2. **Mettez à jour le chemin** dans `landing.component.html` :
   ```html
   [backgroundImage]="'assets/images/accueil.jpeg'"
   ```

3. **Attendez le hot reload** ou relancez Docker

### Option 2 : Utiliser un SVG personnalisé

1. **Remplacez** `accueil.svg` par votre propre SVG dans :
   ```
   Mediconnet-Frontend/src/assets/images/accueil.svg
   ```

2. **Gardez le reste inchangé** - ça fonctionne automatiquement

### Option 3 : Utiliser une URL externe

1. **Dans** `landing.component.html` :
   ```html
   [backgroundImage]="'https://votre-domaine.com/image.jpg'"
   ```

## 📋 Checklist

- [ ] Image placée dans `src/assets/images/`
- [ ] Chemin correct dans le composant
- [ ] Format de l'image : JPG, PNG, SVG, WebP
- [ ] Dimensions recommandées : 800x600px minimum
- [ ] Taille fichier : < 500KB pour performance

## 🎨 Recommandations Design

- **Ratio** : 4:3 ou 16:9
- **Couleur** : Compatible avec le gradient bleu (thème actuel)
- **Contenu** : Peut être une illustration médicale ou un design moderne
- **Texte** : Optionnel, laissez espace pour la lisibilité

## 🔄 Pour Recompiler

```powershell
cd D:\mediconnet_app
.\rebuild.ps1
```

Ensuite, videz le cache du navigateur (`Ctrl+Shift+Delete`) avant de recharger la page.

