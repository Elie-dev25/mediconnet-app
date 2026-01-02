using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Core.Entities.DMP;
using Mediconnet_Backend.Data;
using System.Text.Json;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service Dossier Médical Partagé (DMP) - Interopérabilité
/// </summary>
public class DMPService : IDMPService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DMPService> _logger;

    public DMPService(ApplicationDbContext context, ILogger<DMPService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DMPPatientDto?> GetDMPPatientAsync(int idPatient)
    {
        var dmp = await _context.DossiersMP
            .Include(d => d.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(d => d.Documents)
            .FirstOrDefaultAsync(d => d.IdPatient == idPatient && d.Statut != "ferme");

        if (dmp == null) return null;

        return new DMPPatientDto
        {
            IdDMP = dmp.IdDMP,
            IdPatient = dmp.IdPatient,
            NomPatient = dmp.Patient?.Utilisateur != null 
                ? $"{dmp.Patient.Utilisateur.Prenom} {dmp.Patient.Utilisateur.Nom}" : "",
            IdentifiantNational = dmp.IdentifiantNational ?? "",
            DateCreation = dmp.DateCreation,
            DateDerniereSync = dmp.DateDerniereSync,
            Statut = dmp.Statut,
            SyncAvecNational = dmp.SyncAvecNational,
            Stats = new DMPStatsDto
            {
                NombreDocuments = dmp.Documents.Count(d => !d.Supprime),
                NombreConsultations = dmp.Documents.Count(d => d.TypeDocument == "consultation" && !d.Supprime),
                NombreOrdonnances = dmp.Documents.Count(d => d.TypeDocument == "ordonnance" && !d.Supprime),
                NombreResultatsLabo = dmp.Documents.Count(d => d.TypeDocument == "resultat_labo" && !d.Supprime),
                NombreImagerie = dmp.Documents.Count(d => d.TypeDocument == "imagerie" && !d.Supprime),
                DerniereModification = dmp.Documents.Where(d => !d.Supprime).Max(d => (DateTime?)d.DateAjout)
            }
        };
    }

    public async Task<DMPCreationResult> CreerDMPAsync(int idPatient, CreateDMPRequest request)
    {
        var existant = await _context.DossiersMP.AnyAsync(d => d.IdPatient == idPatient && d.Statut != "ferme");
        if (existant)
            return new DMPCreationResult { Success = false, Message = "Un DMP existe déjà pour ce patient" };

        var patient = await _context.Patients.FindAsync(idPatient);
        if (patient == null)
            return new DMPCreationResult { Success = false, Message = "Patient non trouvé" };

        var identifiantNational = request.IdentifiantNational ?? GenerateIdentifiantNational();

        var dmp = new DossierMedicalPartage
        {
            IdPatient = idPatient,
            IdentifiantNational = identifiantNational,
            ConsentementPatient = request.ConsentementPatient,
            DateConsentement = request.DateConsentement,
            SyncAvecNational = request.SyncAvecNational,
            Statut = "actif"
        };

        _context.DossiersMP.Add(dmp);
        await _context.SaveChangesAsync();

        _logger.LogInformation("DMP créé pour patient {IdPatient}: {IdentifiantNational}", idPatient, identifiantNational);

        return new DMPCreationResult
        {
            Success = true,
            Message = "DMP créé avec succès",
            IdDMP = dmp.IdDMP,
            IdentifiantNational = identifiantNational
        };
    }

    public async Task<bool> ActivateDMPAsync(int idPatient)
    {
        var dmp = await _context.DossiersMP.FirstOrDefaultAsync(d => d.IdPatient == idPatient);
        if (dmp == null) return false;

        dmp.Statut = "actif";
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DesactiverDMPAsync(int idPatient, string motif)
    {
        var dmp = await _context.DossiersMP.FirstOrDefaultAsync(d => d.IdPatient == idPatient && d.Statut == "actif");
        if (dmp == null) return false;

        dmp.Statut = "ferme";
        dmp.DateFermeture = DateTime.UtcNow;
        dmp.MotifFermeture = motif;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<SyncResult> SynchroniserAvecDMPNationalAsync(int idPatient)
    {
        var dmp = await _context.DossiersMP.FirstOrDefaultAsync(d => d.IdPatient == idPatient && d.Statut == "actif");
        if (dmp == null)
            return new SyncResult { Success = false, Message = "DMP non trouvé ou inactif" };

        // Simulation de synchronisation avec le système national
        // Dans une vraie implémentation, appel API vers le système national de santé
        
        dmp.DateDerniereSync = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("DMP synchronisé pour patient {IdPatient}", idPatient);

        return new SyncResult
        {
            Success = true,
            Message = "Synchronisation réussie",
            DocumentsSynchronises = 0,
            DateSync = DateTime.UtcNow
        };
    }

    public async Task<SyncResult> ExporterVersDMPNationalAsync(int idPatient, List<string> documentsAExporter)
    {
        var dmp = await _context.DossiersMP
            .Include(d => d.Documents)
            .FirstOrDefaultAsync(d => d.IdPatient == idPatient && d.Statut == "actif");

        if (dmp == null)
            return new SyncResult { Success = false, Message = "DMP non trouvé" };

        var documents = dmp.Documents.Where(d => documentsAExporter.Contains(d.IdDocument.ToString())).ToList();

        // Simulation d'export
        dmp.DateDerniereSync = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new SyncResult
        {
            Success = true,
            Message = "Export réussi",
            DocumentsSynchronises = documents.Count,
            DateSync = DateTime.UtcNow
        };
    }

    public async Task<ImportResult> ImporterDepuisDMPNationalAsync(int idPatient)
    {
        // Simulation d'import depuis le système national
        return new ImportResult
        {
            Success = true,
            Message = "Import simulé - aucun document disponible",
            DocumentsImportes = 0,
            DocumentsIgnores = 0
        };
    }

    public async Task<List<DocumentDMPDto>> GetDocumentsPatientAsync(int idPatient, string? typeDocument = null)
    {
        var query = _context.DocumentsDMP
            .Where(d => d.IdPatient == idPatient && !d.Supprime);

        if (!string.IsNullOrEmpty(typeDocument))
            query = query.Where(d => d.TypeDocument == typeDocument);

        return await query
            .OrderByDescending(d => d.DateDocument)
            .Select(d => new DocumentDMPDto
            {
                IdDocument = d.IdDocument,
                IdPatient = d.IdPatient,
                TypeDocument = d.TypeDocument,
                Titre = d.Titre,
                Description = d.Description,
                DateDocument = d.DateDocument,
                DateAjout = d.DateAjout,
                Auteur = d.Auteur,
                Etablissement = d.Etablissement,
                Format = d.Format,
                TailleFichier = d.TailleFichier,
                Confidentiel = d.Confidentiel,
                ReferenceExterne = d.ReferenceExterne
            })
            .ToListAsync();
    }

    public async Task<DocumentDMPDto> AjouterDocumentAsync(int idPatient, AjoutDocumentDMPRequest request)
    {
        var dmp = await _context.DossiersMP.FirstOrDefaultAsync(d => d.IdPatient == idPatient && d.Statut == "actif");
        if (dmp == null) throw new Exception("DMP non trouvé ou inactif");

        var document = new DocumentDMP
        {
            IdDMP = dmp.IdDMP,
            IdPatient = idPatient,
            TypeDocument = request.TypeDocument,
            Titre = request.Titre,
            Description = request.Description,
            DateDocument = request.DateDocument,
            Format = request.Format,
            TailleFichier = request.Contenu.Length,
            Confidentiel = request.Confidentiel,
            ContenuFichier = request.Contenu,
            IdConsultation = request.IdConsultation,
            IdOrdonnance = request.IdOrdonnance
        };

        _context.DocumentsDMP.Add(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document ajouté au DMP patient {IdPatient}: {Titre}", idPatient, request.Titre);

        return new DocumentDMPDto
        {
            IdDocument = document.IdDocument,
            IdPatient = document.IdPatient,
            TypeDocument = document.TypeDocument,
            Titre = document.Titre,
            Description = document.Description,
            DateDocument = document.DateDocument,
            DateAjout = document.DateAjout,
            Format = document.Format,
            TailleFichier = document.TailleFichier,
            Confidentiel = document.Confidentiel
        };
    }

    public async Task<byte[]> TelechargerDocumentAsync(int idDocument)
    {
        var document = await _context.DocumentsDMP.FindAsync(idDocument);
        if (document == null || document.Supprime) throw new Exception("Document non trouvé");

        return document.ContenuFichier ?? Array.Empty<byte>();
    }

    public async Task<bool> SupprimerDocumentAsync(int idDocument, string motif)
    {
        var document = await _context.DocumentsDMP.FindAsync(idDocument);
        if (document == null) return false;

        document.Supprime = true;
        document.DateSuppression = DateTime.UtcNow;
        document.MotifSuppression = motif;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<AccesDMPDto>> GetHistoriqueAccesAsync(int idPatient)
    {
        var dmp = await _context.DossiersMP.FirstOrDefaultAsync(d => d.IdPatient == idPatient);
        if (dmp == null) return new List<AccesDMPDto>();

        return await _context.AccesDMP
            .Include(a => a.Professionnel)
            .Include(a => a.DocumentConsulte)
            .Where(a => a.IdDMP == dmp.IdDMP)
            .OrderByDescending(a => a.DateAcces)
            .Take(100)
            .Select(a => new AccesDMPDto
            {
                IdAcces = a.IdAcces,
                IdProfessionnel = a.IdProfessionnel,
                NomProfessionnel = a.Professionnel != null ? $"{a.Professionnel.Prenom} {a.Professionnel.Nom}" : "",
                TypeProfessionnel = a.Professionnel != null ? a.Professionnel.Role : "",
                DateAcces = a.DateAcces,
                TypeAcces = a.TypeAcces,
                DocumentConsulte = a.DocumentConsulte != null ? a.DocumentConsulte.Titre : null
            })
            .ToListAsync();
    }

    public async Task<bool> AccorderAccesAsync(int idPatient, AccorderAccesRequest request)
    {
        var dmp = await _context.DossiersMP.FirstOrDefaultAsync(d => d.IdPatient == idPatient && d.Statut == "actif");
        if (dmp == null) return false;

        var autorisation = new AutorisationDMP
        {
            IdDMP = dmp.IdDMP,
            IdProfessionnel = request.IdProfessionnel,
            TypeAcces = request.TypeAcces,
            DateExpiration = request.DateExpiration,
            Motif = request.Motif,
            Actif = true
        };

        _context.AutorisationsDMP.Add(autorisation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Accès DMP accordé: Patient {IdPatient}, Professionnel {IdPro}", idPatient, request.IdProfessionnel);
        return true;
    }

    public async Task<bool> RevoquerAccesAsync(int idPatient, int idProfessionnel)
    {
        var dmp = await _context.DossiersMP.FirstOrDefaultAsync(d => d.IdPatient == idPatient);
        if (dmp == null) return false;

        var autorisation = await _context.AutorisationsDMP
            .FirstOrDefaultAsync(a => a.IdDMP == dmp.IdDMP && a.IdProfessionnel == idProfessionnel && a.Actif);

        if (autorisation == null) return false;

        autorisation.Actif = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<AutorisationDMPDto>> GetAutorisationsAsync(int idPatient)
    {
        var dmp = await _context.DossiersMP.FirstOrDefaultAsync(d => d.IdPatient == idPatient);
        if (dmp == null) return new List<AutorisationDMPDto>();

        return await _context.AutorisationsDMP
            .Include(a => a.Professionnel)
            .Where(a => a.IdDMP == dmp.IdDMP)
            .Select(a => new AutorisationDMPDto
            {
                IdAutorisation = a.IdAutorisation,
                IdProfessionnel = a.IdProfessionnel,
                NomProfessionnel = a.Professionnel != null ? $"{a.Professionnel.Prenom} {a.Professionnel.Nom}" : "",
                TypeProfessionnel = a.Professionnel != null ? a.Professionnel.Role : "",
                TypeAcces = a.TypeAcces,
                DateAutorisation = a.DateAutorisation,
                DateExpiration = a.DateExpiration,
                Actif = a.Actif
            })
            .ToListAsync();
    }

    public async Task<string> ExportFHIRPatientAsync(int idPatient)
    {
        var patient = await _context.Patients
            .Include(p => p.Utilisateur)
            .FirstOrDefaultAsync(p => p.IdUser == idPatient);

        if (patient?.Utilisateur == null) throw new Exception("Patient non trouvé");

        // Export au format FHIR R4
        var fhirPatient = new
        {
            resourceType = "Patient",
            id = patient.IdUser.ToString(),
            identifier = new[]
            {
                new { system = "urn:mediconnet:patient", value = patient.NumeroDossier }
            },
            name = new[]
            {
                new { family = patient.Utilisateur.Nom, given = new[] { patient.Utilisateur.Prenom } }
            },
            gender = patient.Utilisateur.Sexe?.ToLower() == "masculin" ? "male" : "female",
            birthDate = patient.Utilisateur.Naissance?.ToString("yyyy-MM-dd"),
            telecom = new[]
            {
                new { system = "phone", value = patient.Utilisateur.Telephone },
                new { system = "email", value = patient.Utilisateur.Email }
            },
            address = new[]
            {
                new { text = patient.Utilisateur.Adresse }
            }
        };

        return JsonSerializer.Serialize(fhirPatient, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<string> ExportFHIRDocumentAsync(int idDocument)
    {
        var document = await _context.DocumentsDMP.FindAsync(idDocument);
        if (document == null) throw new Exception("Document non trouvé");

        var fhirDocument = new
        {
            resourceType = "DocumentReference",
            id = document.IdDocument.ToString(),
            status = "current",
            type = new { text = document.TypeDocument },
            subject = new { reference = $"Patient/{document.IdPatient}" },
            date = document.DateDocument.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            description = document.Titre,
            content = new[]
            {
                new
                {
                    attachment = new
                    {
                        contentType = GetMimeType(document.Format),
                        title = document.Titre,
                        size = document.TailleFichier
                    }
                }
            }
        };

        return JsonSerializer.Serialize(fhirDocument, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<ImportResult> ImportFHIRBundleAsync(string fhirBundle, int idPatient)
    {
        // Simulation d'import FHIR
        _logger.LogInformation("Import FHIR pour patient {IdPatient}", idPatient);

        return new ImportResult
        {
            Success = true,
            Message = "Import FHIR traité",
            DocumentsImportes = 0,
            DocumentsIgnores = 0
        };
    }

    private static string GenerateIdentifiantNational()
    {
        return $"DMP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..12].ToUpper()}";
    }

    private static string GetMimeType(string format) => format.ToLower() switch
    {
        "pdf" => "application/pdf",
        "image" => "image/jpeg",
        "hl7" => "application/hl7-v2",
        "fhir" => "application/fhir+json",
        _ => "application/octet-stream"
    };
}
