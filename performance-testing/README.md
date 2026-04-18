# 🧪 Performance Testing Suite - Mediconnet

Suite complète de tests pour mesurer les vraies performances et générer des métriques réelles pour le portfolio.

## 📁 Structure

```
performance-testing/
├── frontend/              # Tests de performance Angular
├── backend/              # Tests de performance .NET API
├── load-testing/         # Tests de charge (k6)
├── results/              # Résultats des tests (auto-généré)
├── run-all-tests.ps1     # Orchestrateur principal
└── README.md
```

## 🚀 Démarrage Rapide

### Prérequis

```powershell
# 1. Node.js et npm (pour frontend)
node --version
npm --version

# 2. .NET SDK (pour backend)
dotnet --version

# 3. k6 pour tests de charge
# Télécharger depuis https://k6.io/docs/getting-started/installation/
# Ou installer via chocolatey :
choco install k6
```

### Exécuter Tous les Tests

```powershell
cd performance-testing
.\run-all-tests.ps1
```

## 📊 Tests Disponibles

### 1️⃣ Frontend Performance
- Bundle size analysis
- Lighthouse score
- Build time
- Tree-shaking effectiveness

**Exécuter** : `.\frontend\run-performance-tests.ps1`

### 2️⃣ Backend API Performance
- API response times (CRUD operations)
- Database query performance
- Memory usage
- Concurrent request handling

**Exécuter** : `.\backend\run-api-tests.ps1`

### 3️⃣ Load Testing
- 100, 500, 1000 concurrent users
- Spike testing
- Soak testing (endurance)
- Error rate under load

**Exécuter** : `.\load-testing\run-load-tests.ps1`

## 📈 Résultats

Tous les résultats sont générés dans `./results/` avec timestamps :
- `metrics-{timestamp}.json` - Métriques brutes
- `performance-report-{timestamp}.html` - Rapport HTML
- `summary-{timestamp}.md` - Résumé texte

## 🎯 Utilisation pour Portfolio

Après exécution des tests, les chiffres réels peuvent être intégrés dans :
- `readme-result.md` - Mises à jour automatiques
- Présentations LinkedIn
- Entretiens techniques

---

**Créé pour générer des métriques vérifiées et impactantes !**
