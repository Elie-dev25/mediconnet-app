# Mediconnet-Backend.Tests

Projet de tests unitaires pour le backend `Mediconnet-Backend`.

## Stack

- **xUnit** — runner de tests
- **FluentAssertions** — assertions lisibles
- **Moq** — mocking
- **Microsoft.EntityFrameworkCore.InMemory** — DbContext en mémoire pour les services qui en dépendent
- **coverlet** — collecte de couverture (format OpenCover pour SonarQube)

## Exécution locale

```powershell
# Tests seuls
dotnet test Mediconnet-Backend.Tests/Mediconnet-Backend.Tests.csproj

# Tests + couverture (fichier OpenCover utilisable par Sonar)
dotnet test Mediconnet-Backend.Tests/Mediconnet-Backend.Tests.csproj `
    --settings Mediconnet-Backend.Tests/coverlet.runsettings `
    --results-directory Mediconnet-Backend.Tests/TestResults
```

Le rapport de couverture est produit dans `Mediconnet-Backend.Tests/TestResults/<guid>/coverage.opencover.xml`.

## Exécution via Docker (si `dotnet` n'est pas installé localement)

```powershell
docker run --rm -v "${PWD}:/work" -w /work mcr.microsoft.com/dotnet/sdk:8.0 `
    dotnet test Mediconnet-Backend.Tests/Mediconnet-Backend.Tests.csproj `
    --settings Mediconnet-Backend.Tests/coverlet.runsettings `
    --results-directory Mediconnet-Backend.Tests/TestResults
```

## Analyse Sonar complète

Un script prêt à l'emploi est disponible :

```powershell
.\scripts\run-sonar-scan.ps1 -SonarToken "squ_xxxxxx"
```

Il enchaîne `sonarscanner begin` → `dotnet build` → `dotnet test` (avec couverture) → `sonarscanner end`. Les exclusions de couverture (migrations, DTOs, entités, `Program.cs`, etc.) sont déjà configurées.

## Organisation

| Dossier | Classes testées |
|---|---|
| `Services/` | `PasswordValidationService`, `JwtTokenService`, `DataProtectionService`, `MedecinHelperService` |
| `Validators/` | `LoginRequestValidator`, `RegisterRequestValidator`, `ChangePasswordRequestValidator`, `UpdatePatientProfileRequestValidator`, `PatientSearchRequestValidator` |
| `Helpers/` | `DateTimeHelper` |

## Ajouter un test

1. Créer un fichier `<Cible>Tests.cs` dans le dossier approprié
2. Utiliser les `using` globaux `Xunit` et `FluentAssertions` (déjà configurés dans le csproj)
3. Préférer l'`InMemoryDatabase` au `Mock<DbContext>` pour les services EF Core

## Exclusions de couverture

Voir `coverlet.runsettings`. Sont ignorés :

- `**/Migrations/**`
- `**/Program.cs`
- `**/DTOs/**`
- `Mediconnet_Backend.Data.*` (DbContext / configuration EF)
- `Mediconnet_Backend.Core.Entities.*` (POCO)
- `Mediconnet_Backend.Core.Enums.*`
- `Mediconnet_Backend.Hubs.*`
- Classes marquées `[ExcludeFromCodeCoverage]`, `[GeneratedCode]`, `[Obsolete]`
