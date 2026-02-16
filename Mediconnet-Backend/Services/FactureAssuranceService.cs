using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Data;

namespace Mediconnet_Backend.Services;

public interface IFactureAssuranceService
{
    Task<FactureAssuranceResult> EnvoyerFactureAssuranceAsync(int idFacture);
    Task<List<FactureAssuranceDto>> GetFacturesAssuranceAsync(FactureAssuranceFilter? filter = null);
    Task<FactureAssuranceDto?> GetFactureAssuranceAsync(int idFacture);
    Task<bool> UpdateStatutFactureAsync(int idFacture, string nouveauStatut, string? notes = null);
    Task<byte[]?> TelechargerFacturePdfAsync(int idFacture);
    Task<FactureAssuranceStats> GetStatistiquesAsync();
}

public class FactureAssuranceService : IFactureAssuranceService
{
    private readonly ApplicationDbContext _context;
    private readonly IFacturePdfService _pdfService;
    private readonly IFactureEmailService _emailService;
    private readonly ILogger<FactureAssuranceService> _logger;

    public FactureAssuranceService(
        ApplicationDbContext context,
        IFacturePdfService pdfService,
        IFactureEmailService emailService,
        ILogger<FactureAssuranceService> logger)
    {
        _context = context;
        _pdfService = pdfService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<FactureAssuranceResult> EnvoyerFactureAssuranceAsync(int idFacture)
    {
        var facture = await _context.Factures
            .Include(f => f.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Include(f => f.Assurance)
            .Include(f => f.Lignes)
            .FirstOrDefaultAsync(f => f.IdFacture == idFacture);

        if (facture == null)
        {
            return new FactureAssuranceResult { Success = false, Message = "Facture non trouvée" };
        }

        if (facture.Assurance == null)
        {
            return new FactureAssuranceResult { Success = false, Message = "Aucune assurance associée à cette facture" };
        }

        if (string.IsNullOrEmpty(facture.Assurance.EmailFacturation))
        {
            return new FactureAssuranceResult { Success = false, Message = "Email de facturation non configuré pour cette assurance" };
        }

        if ((facture.MontantAssurance ?? 0) <= 0)
        {
            return new FactureAssuranceResult { Success = false, Message = "Aucun montant à facturer à l'assurance" };
        }

        try
        {
            // Générer le PDF
            var pdfContent = _pdfService.GenererFacturePdf(facture);

            // Envoyer l'email
            var emailSent = await _emailService.EnvoyerFactureAssuranceAsync(facture, pdfContent);

            if (emailSent)
            {
                // Mettre à jour le statut de la facture
                facture.Statut = "envoyee_assurance";
                facture.DateEnvoiAssurance = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Facture {NumeroFacture} envoyée à l'assurance {Assurance}", 
                    facture.NumeroFacture, facture.Assurance.Nom);

                return new FactureAssuranceResult 
                { 
                    Success = true, 
                    Message = $"Facture envoyée avec succès à {facture.Assurance.EmailFacturation}",
                    NumeroFacture = facture.NumeroFacture
                };
            }
            else
            {
                return new FactureAssuranceResult 
                { 
                    Success = false, 
                    Message = "Erreur lors de l'envoi de l'email. Vérifiez la configuration SMTP." 
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'envoi de la facture {IdFacture}", idFacture);
            return new FactureAssuranceResult { Success = false, Message = $"Erreur: {ex.Message}" };
        }
    }

    public async Task<List<FactureAssuranceDto>> GetFacturesAssuranceAsync(FactureAssuranceFilter? filter = null)
    {
        var query = _context.Factures
            .Include(f => f.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Include(f => f.Assurance)
            .Where(f => f.IdAssurance != null && (f.MontantAssurance ?? 0) > 0)
            .AsQueryable();

        if (filter != null)
        {
            if (filter.IdAssurance.HasValue)
                query = query.Where(f => f.IdAssurance == filter.IdAssurance);

            if (!string.IsNullOrEmpty(filter.Statut))
                query = query.Where(f => f.Statut == filter.Statut);

            if (!string.IsNullOrEmpty(filter.TypeFacture))
                query = query.Where(f => f.TypeFacture == filter.TypeFacture);

            if (filter.DateDebut.HasValue)
                query = query.Where(f => f.DateFacture >= filter.DateDebut);

            if (filter.DateFin.HasValue)
                query = query.Where(f => f.DateFacture <= filter.DateFin);

            if (!string.IsNullOrEmpty(filter.Recherche))
            {
                var recherche = filter.Recherche.ToLower();
                query = query.Where(f => 
                    f.NumeroFacture.ToLower().Contains(recherche) ||
                    (f.Patient != null && f.Patient.Utilisateur != null && 
                        (f.Patient.Utilisateur.Nom.ToLower().Contains(recherche) || 
                         f.Patient.Utilisateur.Prenom.ToLower().Contains(recherche))) ||
                    (f.Assurance != null && f.Assurance.Nom.ToLower().Contains(recherche)));
            }
        }

        var factures = await query
            .OrderByDescending(f => f.DateFacture)
            .Take(filter?.Limit ?? 100)
            .ToListAsync();

        return factures.Select(f => MapToDto(f)).ToList();
    }

    public async Task<FactureAssuranceDto?> GetFactureAssuranceAsync(int idFacture)
    {
        var facture = await _context.Factures
            .Include(f => f.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Include(f => f.Assurance)
            .Include(f => f.Lignes)
            .FirstOrDefaultAsync(f => f.IdFacture == idFacture);

        return facture != null ? MapToDto(facture) : null;
    }

    public async Task<bool> UpdateStatutFactureAsync(int idFacture, string nouveauStatut, string? notes = null)
    {
        var facture = await _context.Factures.FindAsync(idFacture);
        if (facture == null) return false;

        var ancienStatut = facture.Statut;
        facture.Statut = nouveauStatut;
        
        if (!string.IsNullOrEmpty(notes))
        {
            facture.Notes = string.IsNullOrEmpty(facture.Notes) 
                ? notes 
                : $"{facture.Notes}\n[{DateTime.Now:dd/MM/yyyy HH:mm}] {notes}";
        }

        // Mettre à jour la date de paiement si payée
        if (nouveauStatut == "payee")
        {
            facture.DatePaiement = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Statut facture {NumeroFacture} changé de {AncienStatut} à {NouveauStatut}", 
            facture.NumeroFacture, ancienStatut, nouveauStatut);

        return true;
    }

    public async Task<byte[]?> TelechargerFacturePdfAsync(int idFacture)
    {
        var facture = await _context.Factures
            .Include(f => f.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Include(f => f.Assurance)
            .Include(f => f.Lignes)
            .FirstOrDefaultAsync(f => f.IdFacture == idFacture);

        if (facture == null) return null;

        return _pdfService.GenererFacturePdf(facture);
    }

    public async Task<FactureAssuranceStats> GetStatistiquesAsync()
    {
        var factures = await _context.Factures
            .Where(f => f.IdAssurance != null && (f.MontantAssurance ?? 0) > 0)
            .ToListAsync();

        return new FactureAssuranceStats
        {
            TotalFactures = factures.Count,
            FacturesEnAttente = factures.Count(f => f.Statut == "en_attente"),
            FacturesEnvoyees = factures.Count(f => f.Statut == "envoyee_assurance"),
            FacturesPayees = factures.Count(f => f.Statut == "payee"),
            FacturesRejetees = factures.Count(f => f.Statut == "rejetee"),
            MontantTotalDu = factures.Where(f => f.Statut != "payee" && f.Statut != "annulee").Sum(f => f.MontantAssurance ?? 0),
            MontantTotalPaye = factures.Where(f => f.Statut == "payee").Sum(f => f.MontantAssurance ?? 0),
            MontantEnAttente = factures.Where(f => f.Statut == "en_attente" || f.Statut == "envoyee_assurance").Sum(f => f.MontantAssurance ?? 0)
        };
    }

    private FactureAssuranceDto MapToDto(Facture f) => new()
    {
        IdFacture = f.IdFacture,
        NumeroFacture = f.NumeroFacture,
        DateFacture = f.DateFacture,
        TypeFacture = f.TypeFacture,
        Statut = f.Statut,
        MontantTotal = f.MontantTotal,
        MontantAssurance = f.MontantAssurance ?? 0,
        MontantPatient = f.MontantPatient ?? (f.MontantTotal - (f.MontantAssurance ?? 0)),
        TauxCouverture = f.TauxCouverture ?? 0,
        DateEnvoiAssurance = f.DateEnvoiAssurance,
        DatePaiement = f.DatePaiement,
        Notes = f.Notes,
        PatientNom = f.Patient?.Utilisateur != null ? $"{f.Patient.Utilisateur.Nom} {f.Patient.Utilisateur.Prenom}" : null,
        IdPatient = f.IdPatient,
        NumeroCarteAssurance = f.Patient?.NumeroCarteAssurance,
        AssuranceNom = f.Assurance?.Nom,
        AssuranceId = f.IdAssurance,
        AssuranceEmail = f.Assurance?.EmailFacturation
    };
}

// DTOs
public class FactureAssuranceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? NumeroFacture { get; set; }
}

public class FactureAssuranceDto
{
    public int IdFacture { get; set; }
    public string NumeroFacture { get; set; } = string.Empty;
    public DateTime DateFacture { get; set; }
    public string? TypeFacture { get; set; }
    public string? Statut { get; set; }
    public decimal MontantTotal { get; set; }
    public decimal MontantAssurance { get; set; }
    public decimal MontantPatient { get; set; }
    public decimal TauxCouverture { get; set; }
    public DateTime? DateEnvoiAssurance { get; set; }
    public DateTime? DatePaiement { get; set; }
    public string? Notes { get; set; }
    public string? PatientNom { get; set; }
    public int IdPatient { get; set; }
    public string? NumeroCarteAssurance { get; set; }
    public string? AssuranceNom { get; set; }
    public int? AssuranceId { get; set; }
    public string? AssuranceEmail { get; set; }
}

public class FactureAssuranceFilter
{
    public int? IdAssurance { get; set; }
    public string? Statut { get; set; }
    public string? TypeFacture { get; set; }
    public DateTime? DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public string? Recherche { get; set; }
    public int Limit { get; set; } = 100;
}

public class FactureAssuranceStats
{
    public int TotalFactures { get; set; }
    public int FacturesEnAttente { get; set; }
    public int FacturesEnvoyees { get; set; }
    public int FacturesPayees { get; set; }
    public int FacturesRejetees { get; set; }
    public decimal MontantTotalDu { get; set; }
    public decimal MontantTotalPaye { get; set; }
    public decimal MontantEnAttente { get; set; }
}
