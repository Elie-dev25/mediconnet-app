using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Core.Entities;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// API pour les tables de référence (données de configuration)
/// </summary>
[ApiController]
[Route("api/reference")]
public class ReferenceDataController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReferenceDataController> _logger;

    public ReferenceDataController(ApplicationDbContext context, ILogger<ReferenceDataController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==================== TYPES DE PRESTATION ====================

    /// <summary>
    /// Liste des types de prestation (consultation, hospitalisation, examen, pharmacie)
    /// </summary>
    [HttpGet("types-prestation")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<TypePrestationDto>>> GetTypesPrestations()
    {
        var types = await _context.TypesPrestations
            .Where(t => t.Actif)
            .OrderBy(t => t.Ordre)
            .Select(t => new TypePrestationDto
            {
                Code = t.Code,
                Libelle = t.Libelle,
                Description = t.Description,
                Icone = t.Icone
            })
            .ToListAsync();

        return Ok(types);
    }

    // ==================== CATÉGORIES DE BÉNÉFICIAIRES ====================

    /// <summary>
    /// Liste des catégories de bénéficiaires
    /// </summary>
    [HttpGet("categories-beneficiaires")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ReferenceItemDto>>> GetCategoriesBeneficiaires()
    {
        var categories = await _context.CategoriesBeneficiaires
            .Where(c => c.Actif)
            .OrderBy(c => c.Libelle)
            .Select(c => new ReferenceItemDto
            {
                Id = c.IdCategorie,
                Code = c.Code,
                Libelle = c.Libelle,
                Description = c.Description
            })
            .ToListAsync();

        return Ok(categories);
    }

    // ==================== MODES DE PAIEMENT ====================

    /// <summary>
    /// Liste des modes de paiement
    /// </summary>
    [HttpGet("modes-paiement")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ReferenceItemDto>>> GetModesPaiement()
    {
        var modes = await _context.ModesPaiement
            .Where(m => m.Actif)
            .OrderBy(m => m.Libelle)
            .Select(m => new ReferenceItemDto
            {
                Id = m.IdMode,
                Code = m.Code,
                Libelle = m.Libelle,
                Description = m.Description
            })
            .ToListAsync();

        return Ok(modes);
    }

    // ==================== ZONES DE COUVERTURE ====================

    /// <summary>
    /// Liste des zones de couverture géographique
    /// </summary>
    [HttpGet("zones-couverture")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ReferenceItemDto>>> GetZonesCouverture()
    {
        var zones = await _context.ZonesCouverture
            .Where(z => z.Actif)
            .OrderBy(z => z.Libelle)
            .Select(z => new ReferenceItemDto
            {
                Id = z.IdZone,
                Code = z.Code,
                Libelle = z.Libelle,
                Description = z.Description
            })
            .ToListAsync();

        return Ok(zones);
    }

    // ==================== TYPES DE COUVERTURE SANTÉ ====================

    /// <summary>
    /// Liste des types de couverture santé (hospitalisation, maternité, dentaire, etc.)
    /// </summary>
    [HttpGet("types-couverture-sante")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ReferenceItemDto>>> GetTypesCouvertureSante()
    {
        var types = await _context.TypesCouvertureSante
            .Where(t => t.Actif)
            .OrderBy(t => t.Libelle)
            .Select(t => new ReferenceItemDto
            {
                Id = t.IdTypeCouverture,
                Code = t.Code,
                Libelle = t.Libelle,
                Description = t.Description
            })
            .ToListAsync();

        return Ok(types);
    }

    // ==================== TOUTES LES RÉFÉRENCES (pour chargement initial) ====================

    /// <summary>
    /// Récupère toutes les données de référence en une seule requête
    /// </summary>
    [HttpGet("all")]
    [AllowAnonymous]
    public async Task<ActionResult<AllReferenceDataDto>> GetAllReferenceData()
    {
        var result = new AllReferenceDataDto
        {
            TypesPrestations = await _context.TypesPrestations
                .Where(t => t.Actif)
                .OrderBy(t => t.Ordre)
                .Select(t => new TypePrestationDto
                {
                    Code = t.Code,
                    Libelle = t.Libelle,
                    Description = t.Description,
                    Icone = t.Icone
                })
                .ToListAsync(),

            CategoriesBeneficiaires = await _context.CategoriesBeneficiaires
                .Where(c => c.Actif)
                .OrderBy(c => c.Libelle)
                .Select(c => new ReferenceItemDto
                {
                    Id = c.IdCategorie,
                    Code = c.Code,
                    Libelle = c.Libelle,
                    Description = c.Description
                })
                .ToListAsync(),

            ModesPaiement = await _context.ModesPaiement
                .Where(m => m.Actif)
                .OrderBy(m => m.Libelle)
                .Select(m => new ReferenceItemDto
                {
                    Id = m.IdMode,
                    Code = m.Code,
                    Libelle = m.Libelle,
                    Description = m.Description
                })
                .ToListAsync(),

            ZonesCouverture = await _context.ZonesCouverture
                .Where(z => z.Actif)
                .OrderBy(z => z.Libelle)
                .Select(z => new ReferenceItemDto
                {
                    Id = z.IdZone,
                    Code = z.Code,
                    Libelle = z.Libelle,
                    Description = z.Description
                })
                .ToListAsync(),

            TypesCouvertureSante = await _context.TypesCouvertureSante
                .Where(t => t.Actif)
                .OrderBy(t => t.Libelle)
                .Select(t => new ReferenceItemDto
                {
                    Id = t.IdTypeCouverture,
                    Code = t.Code,
                    Libelle = t.Libelle,
                    Description = t.Description
                })
                .ToListAsync()
        };

        return Ok(result);
    }
}

// ==================== DTOs ====================

public class TypePrestationDto
{
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icone { get; set; }
}

public class ReferenceItemDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AllReferenceDataDto
{
    public List<TypePrestationDto> TypesPrestations { get; set; } = new();
    public List<ReferenceItemDto> CategoriesBeneficiaires { get; set; } = new();
    public List<ReferenceItemDto> ModesPaiement { get; set; } = new();
    public List<ReferenceItemDto> ZonesCouverture { get; set; } = new();
    public List<ReferenceItemDto> TypesCouvertureSante { get; set; } = new();
}
