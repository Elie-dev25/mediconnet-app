using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Data;

namespace Mediconnet_Backend.Core.Services;

/// <summary>
/// Service pour enregistrer les audits des modifications de consultations
/// </summary>
public interface IConsultationAuditService
{
    /// <summary>Enregistrer une action d'audit</summary>
    Task LogAsync(ConsultationAuditEntry entry);
    
    /// <summary>Enregistrer un changement de statut</summary>
    Task LogStatutChangeAsync(int idConsultation, int idUtilisateur, string? ancienStatut, string nouveauStatut, string? description = null);
    
    /// <summary>Enregistrer une modification de champ</summary>
    Task LogFieldChangeAsync(int idConsultation, int idUtilisateur, string champ, string? ancienneValeur, string? nouvelleValeur);
    
    /// <summary>Récupérer l'historique d'audit d'une consultation</summary>
    Task<List<ConsultationAuditDto>> GetAuditHistoryAsync(int idConsultation);
}

public class ConsultationAuditService : IConsultationAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ConsultationAuditService> _logger;

    public ConsultationAuditService(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ConsultationAuditService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(ConsultationAuditEntry entry)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            var audit = new ConsultationAudit
            {
                IdConsultation = entry.IdConsultation,
                IdUtilisateur = entry.IdUtilisateur,
                TypeAction = entry.TypeAction,
                ChampModifie = entry.ChampModifie,
                AncienneValeur = entry.AncienneValeur,
                NouvelleValeur = entry.NouvelleValeur,
                Description = entry.Description,
                AdresseIp = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString(),
                DateModification = DateTime.UtcNow
            };

            _context.Set<ConsultationAudit>().Add(audit);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'enregistrement de l'audit pour consultation {IdConsultation}", entry.IdConsultation);
        }
    }

    public async Task LogStatutChangeAsync(int idConsultation, int idUtilisateur, string? ancienStatut, string nouveauStatut, string? description = null)
    {
        var desc = description ?? GetStatutChangeDescription(ancienStatut, nouveauStatut);
        
        await LogAsync(new ConsultationAuditEntry
        {
            IdConsultation = idConsultation,
            IdUtilisateur = idUtilisateur,
            TypeAction = ConsultationAuditActions.StatutChange,
            ChampModifie = "statut",
            AncienneValeur = ancienStatut,
            NouvelleValeur = nouveauStatut,
            Description = desc
        });
    }

    public async Task LogFieldChangeAsync(int idConsultation, int idUtilisateur, string champ, string? ancienneValeur, string? nouvelleValeur)
    {
        await LogAsync(new ConsultationAuditEntry
        {
            IdConsultation = idConsultation,
            IdUtilisateur = idUtilisateur,
            TypeAction = ConsultationAuditActions.Modification,
            ChampModifie = champ,
            AncienneValeur = ancienneValeur,
            NouvelleValeur = nouvelleValeur,
            Description = $"Modification du champ '{champ}'"
        });
    }

    public async Task<List<ConsultationAuditDto>> GetAuditHistoryAsync(int idConsultation)
    {
        return await _context.Set<ConsultationAudit>()
            .Where(a => a.IdConsultation == idConsultation)
            .Include(a => a.Utilisateur)
            .OrderByDescending(a => a.DateModification)
            .Select(a => new ConsultationAuditDto
            {
                IdAudit = a.IdAudit,
                TypeAction = a.TypeAction,
                ChampModifie = a.ChampModifie,
                AncienneValeur = a.AncienneValeur,
                NouvelleValeur = a.NouvelleValeur,
                Description = a.Description,
                DateModification = a.DateModification,
                UtilisateurNom = a.Utilisateur != null 
                    ? $"{a.Utilisateur.Prenom} {a.Utilisateur.Nom}" 
                    : "Inconnu"
            })
            .ToListAsync();
    }

    private static string GetStatutChangeDescription(string? ancien, string nouveau) => nouveau switch
    {
        "en_cours" => "Consultation démarrée",
        "en_pause" => "Consultation mise en pause",
        "terminee" => "Consultation validée et terminée",
        "annulee" => "Consultation annulée",
        _ => $"Statut changé de '{ancien ?? "inconnu"}' vers '{nouveau}'"
    };
}

/// <summary>
/// Entrée d'audit à enregistrer
/// </summary>
public class ConsultationAuditEntry
{
    public int IdConsultation { get; set; }
    public int IdUtilisateur { get; set; }
    public string TypeAction { get; set; } = string.Empty;
    public string? ChampModifie { get; set; }
    public string? AncienneValeur { get; set; }
    public string? NouvelleValeur { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// DTO pour l'affichage de l'historique d'audit
/// </summary>
public class ConsultationAuditDto
{
    public int IdAudit { get; set; }
    public string TypeAction { get; set; } = string.Empty;
    public string? ChampModifie { get; set; }
    public string? AncienneValeur { get; set; }
    public string? NouvelleValeur { get; set; }
    public string? Description { get; set; }
    public DateTime DateModification { get; set; }
    public string UtilisateurNom { get; set; } = string.Empty;
}
