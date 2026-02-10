# Documentation du Stockage des Documents Médicaux

## 📋 Vue d'ensemble

Le système MediConnect utilise une architecture de stockage de documents médicaux basée sur **UUID** avec :
- Contrôle d'intégrité (hash SHA-256)
- Audit complet des accès
- Structure de fichiers sécurisée et organisée

## 🗂️ Structure de Stockage

### Format du chemin
```
/storage/mediConnect/{annee}/{mois}/patient_{id_patient}/{uuid_part1}/{uuid_part2}/{uuid}.{ext}
```

### Exemple
Pour un document avec :
- UUID: `a1b2c3d4-e5f6-7890-abcd-ef1234567890`
- Patient ID: `123`
- Date: Février 2026
- Type: PDF

Le chemin sera :
```
/storage/mediConnect/2026/02/patient_123/a1/b2/a1b2c3d4-e5f6-7890-abcd-ef1234567890.pdf
```

### Décomposition
- `{annee}` : Année courante (ex: 2026)
- `{mois}` : Mois sur 2 chiffres (ex: 02)
- `patient_{id_patient}` : Dossier du patient
- `{uuid_part1}` : 2 premiers caractères de l'UUID (sans tirets)
- `{uuid_part2}` : 2 caractères suivants
- `{uuid}.{ext}` : Fichier nommé par son UUID

## 🔧 Utilisation du Service

### Injection de dépendance
```csharp
public class MonController : ControllerBase
{
    private readonly IDocumentStorageService _storageService;
    
    public MonController(IDocumentStorageService storageService)
    {
        _storageService = storageService;
    }
}
```

### Upload d'un fichier (IFormFile)
```csharp
var uuid = Guid.NewGuid().ToString();
var result = await _storageService.SaveFileAsync(file, uuid, idPatient);

if (result.Success)
{
    // Enregistrer en base de données
    var document = new DocumentMedical
    {
        Uuid = uuid,
        CheminRelatif = result.RelativePath,
        HashSha256 = result.HashSha256,
        TailleOctets = (ulong)result.FileSize,
        MimeType = result.MimeType,
        // ... autres propriétés
    };
}
```

### Upload d'un fichier (byte[])
```csharp
var uuid = Guid.NewGuid().ToString();
var result = await _storageService.SaveFileAsync(
    content: fileBytes,
    uuid: uuid,
    idPatient: 123,
    originalFileName: "rapport.pdf",
    mimeType: "application/pdf"
);
```

### Téléchargement d'un fichier
```csharp
// Par stream (recommandé pour gros fichiers)
var stream = await _storageService.OpenReadStreamAsync(document.CheminRelatif);
return File(stream, document.MimeType, document.NomFichierOriginal);

// Par byte[] (petits fichiers)
var content = await _storageService.ReadFileAsync(document.CheminRelatif);
```

### Vérification d'intégrité
```csharp
var result = await _storageService.VerifyIntegrityAsync(
    document.CheminRelatif,
    document.HashSha256,
    document.TailleOctets
);

if (!result.IsValid)
{
    // Gérer le problème d'intégrité
    Console.WriteLine($"Problème: {result.Status} - {result.ErrorMessage}");
}
```

### Validation de fichier
```csharp
var validation = _storageService.ValidateFile(file);
if (!validation.IsValid)
{
    return BadRequest(new { errors = validation.Errors });
}
```

## 📡 API Endpoints

### Upload
```http
POST /api/documents/upload
Content-Type: multipart/form-data

file: [fichier binaire]
IdPatient: 123
TypeDocument: resultat_examen
Description: Résultat IRM cérébrale
```

### Téléchargement
```http
GET /api/documents/{uuid}/download
Authorization: Bearer {token}
```

### Métadonnées
```http
GET /api/documents/{uuid}
Authorization: Bearer {token}
```

### Liste des documents d'un patient
```http
GET /api/documents/patient/{idPatient}?typeDocument=resultat_examen&page=1&pageSize=20
Authorization: Bearer {token}
```

### Vérification d'intégrité
```http
POST /api/documents/{uuid}/verify-integrity
Authorization: Bearer {token}
```

### Suppression (soft delete)
```http
DELETE /api/documents/{uuid}?motif=Document obsolète
Authorization: Bearer {token}
```

### Statistiques
```http
GET /api/documents/stats
Authorization: Bearer {token}
Roles: administrateur, medecin
```

### Initialisation du stockage
```http
POST /api/documents/initialize-storage
Authorization: Bearer {token}
Roles: administrateur
```

## 🔐 Sécurité

### Permissions des fichiers
- **Dossiers** : 750 (rwxr-x---)
- **Fichiers** : 640 (rw-r-----)
- **Propriétaire** : www-data (ou utilisateur du container)

### Contrôle d'accès
- **Administrateur** : Accès complet
- **Patient** : Accès à ses propres documents (si `acces_patient = true`)
- **Médecin/Infirmier/Laborantin/Pharmacien** : Accès aux documents de leurs patients

### Audit
Chaque accès est enregistré dans `audit_acces_documents` :
- Type d'action (consultation, téléchargement, modification, etc.)
- Utilisateur et rôle
- IP et User-Agent
- Résultat (autorisé/refusé)

## 📊 Tables de Base de Données

### documents_medicaux
Table principale stockant les métadonnées des documents.

| Colonne | Type | Description |
|---------|------|-------------|
| uuid | CHAR(36) | Clé primaire UUID |
| chemin_relatif | VARCHAR(500) | Chemin de stockage |
| hash_sha256 | CHAR(64) | Hash d'intégrité |
| type_document | ENUM | Type de document |
| id_patient | INT | Patient propriétaire |
| statut | ENUM | actif, archive, supprime, quarantaine |

### audit_acces_documents
Journal d'audit des accès.

### verification_integrite
Historique des vérifications d'intégrité.

## ⚙️ Configuration

### appsettings.json
```json
{
  "DocumentStorage": {
    "RootPath": "/storage/mediConnect",
    "MaxFileSizeBytes": 52428800,
    "AllowedMimeTypes": ["application/pdf", "image/jpeg", ...],
    "AllowedExtensions": [".pdf", ".jpg", ...],
    "DirectoryPermissions": "750",
    "FilePermissions": "640"
  }
}
```

### Variables d'environnement (Docker)
```yaml
environment:
  - DocumentStorage__RootPath=/storage/mediConnect
  - DocumentStorage__MaxFileSizeBytes=52428800
```

## 🐳 Docker

### Volume
```yaml
volumes:
  - document_storage:/storage/mediConnect

volumes:
  document_storage:
```

## ⚠️ Règles Importantes

1. **Ne JAMAIS créer de chemins manuellement** - Utiliser uniquement `IDocumentStorageService.GenerateStoragePath()`

2. **Ne JAMAIS stocker de chemins absolus en base** - Stocker uniquement le chemin relatif

3. **Toujours valider les fichiers avant upload** - Utiliser `ValidateFile()`

4. **Toujours enregistrer l'audit** - Chaque accès doit être tracé

5. **Soft delete uniquement** - Les fichiers supprimés vont en quarantaine

## 🧪 Tests

Pour tester l'upload :
```bash
curl -X POST http://localhost:8080/api/documents/upload \
  -H "Authorization: Bearer {token}" \
  -F "file=@test.pdf" \
  -F "IdPatient=1" \
  -F "TypeDocument=resultat_examen"
```
