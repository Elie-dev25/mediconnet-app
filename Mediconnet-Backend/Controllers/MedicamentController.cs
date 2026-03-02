using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Core.Entities;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Contrôleur pour la gestion des médicaments et leurs formes/voies d'administration
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicamentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedicamentController> _logger;

    public MedicamentController(ApplicationDbContext context, ILogger<MedicamentController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Récupère toutes les formes pharmaceutiques actives
    /// </summary>
    [HttpGet("formes")]
    public async Task<IActionResult> GetFormesPharmaceutiques()
    {
        try
        {
            var formes = await _context.FormesPharmaceutiques
                .Where(f => f.Actif)
                .OrderBy(f => f.Ordre)
                .Select(f => new FormePharmaceutiqueDto
                {
                    IdForme = f.IdForme,
                    Code = f.Code,
                    Libelle = f.Libelle,
                    Description = f.Description
                })
                .ToListAsync();

            return Ok(formes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des formes pharmaceutiques");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupère toutes les voies d'administration actives
    /// </summary>
    [HttpGet("voies")]
    public async Task<IActionResult> GetVoiesAdministration()
    {
        try
        {
            var voies = await _context.VoiesAdministration
                .Where(v => v.Actif)
                .OrderBy(v => v.Ordre)
                .Select(v => new VoieAdministrationDto
                {
                    IdVoie = v.IdVoie,
                    Code = v.Code,
                    Libelle = v.Libelle,
                    Description = v.Description
                })
                .ToListAsync();

            return Ok(voies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des voies d'administration");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupère les formes pharmaceutiques associées à un médicament spécifique
    /// </summary>
    [HttpGet("{idMedicament}/formes")]
    public async Task<IActionResult> GetFormesByMedicament(int idMedicament)
    {
        try
        {
            // Vérifier si le médicament existe
            var medicamentExists = await _context.Medicaments.AnyAsync(m => m.IdMedicament == idMedicament);
            if (!medicamentExists)
            {
                return NotFound(new { message = "Médicament non trouvé" });
            }

            // Récupérer les formes associées au médicament
            var formes = await _context.MedicamentFormes
                .Where(mf => mf.IdMedicament == idMedicament)
                .Include(mf => mf.FormePharmaceutique)
                .Where(mf => mf.FormePharmaceutique.Actif)
                .OrderByDescending(mf => mf.EstDefaut)
                .ThenBy(mf => mf.FormePharmaceutique.Ordre)
                .Select(mf => new FormePharmaceutiqueDto
                {
                    IdForme = mf.FormePharmaceutique.IdForme,
                    Code = mf.FormePharmaceutique.Code,
                    Libelle = mf.FormePharmaceutique.Libelle,
                    Description = mf.FormePharmaceutique.Description,
                    EstDefaut = mf.EstDefaut
                })
                .ToListAsync();

            // Si aucune forme associée, retourner toutes les formes actives
            if (!formes.Any())
            {
                formes = await _context.FormesPharmaceutiques
                    .Where(f => f.Actif)
                    .OrderBy(f => f.Ordre)
                    .Select(f => new FormePharmaceutiqueDto
                    {
                        IdForme = f.IdForme,
                        Code = f.Code,
                        Libelle = f.Libelle,
                        Description = f.Description,
                        EstDefaut = false
                    })
                    .ToListAsync();
            }

            return Ok(formes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des formes pour le médicament {IdMedicament}", idMedicament);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupère les voies d'administration associées à un médicament spécifique
    /// </summary>
    [HttpGet("{idMedicament}/voies")]
    public async Task<IActionResult> GetVoiesByMedicament(int idMedicament)
    {
        try
        {
            // Vérifier si le médicament existe
            var medicamentExists = await _context.Medicaments.AnyAsync(m => m.IdMedicament == idMedicament);
            if (!medicamentExists)
            {
                return NotFound(new { message = "Médicament non trouvé" });
            }

            // Récupérer les voies associées au médicament
            var voies = await _context.MedicamentVoies
                .Where(mv => mv.IdMedicament == idMedicament)
                .Include(mv => mv.VoieAdministration)
                .Where(mv => mv.VoieAdministration.Actif)
                .OrderByDescending(mv => mv.EstDefaut)
                .ThenBy(mv => mv.VoieAdministration.Ordre)
                .Select(mv => new VoieAdministrationDto
                {
                    IdVoie = mv.VoieAdministration.IdVoie,
                    Code = mv.VoieAdministration.Code,
                    Libelle = mv.VoieAdministration.Libelle,
                    Description = mv.VoieAdministration.Description,
                    EstDefaut = mv.EstDefaut
                })
                .ToListAsync();

            // Si aucune voie associée, retourner toutes les voies actives
            if (!voies.Any())
            {
                voies = await _context.VoiesAdministration
                    .Where(v => v.Actif)
                    .OrderBy(v => v.Ordre)
                    .Select(v => new VoieAdministrationDto
                    {
                        IdVoie = v.IdVoie,
                        Code = v.Code,
                        Libelle = v.Libelle,
                        Description = v.Description,
                        EstDefaut = false
                    })
                    .ToListAsync();
            }

            return Ok(voies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des voies pour le médicament {IdMedicament}", idMedicament);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupère les formes et voies d'administration pour un médicament (endpoint combiné)
    /// </summary>
    [HttpGet("{idMedicament}/formes-voies")]
    public async Task<IActionResult> GetFormesVoiesByMedicament(int idMedicament)
    {
        try
        {
            // Vérifier si le médicament existe
            var medicament = await _context.Medicaments
                .Where(m => m.IdMedicament == idMedicament)
                .Select(m => new { m.IdMedicament, m.Nom, m.Dosage })
                .FirstOrDefaultAsync();

            if (medicament == null)
            {
                return NotFound(new { message = "Médicament non trouvé" });
            }

            // Récupérer les formes associées
            var formes = await _context.MedicamentFormes
                .Where(mf => mf.IdMedicament == idMedicament)
                .Include(mf => mf.FormePharmaceutique)
                .Where(mf => mf.FormePharmaceutique.Actif)
                .OrderByDescending(mf => mf.EstDefaut)
                .ThenBy(mf => mf.FormePharmaceutique.Ordre)
                .Select(mf => new FormePharmaceutiqueDto
                {
                    IdForme = mf.FormePharmaceutique.IdForme,
                    Code = mf.FormePharmaceutique.Code,
                    Libelle = mf.FormePharmaceutique.Libelle,
                    Description = mf.FormePharmaceutique.Description,
                    EstDefaut = mf.EstDefaut
                })
                .ToListAsync();

            // Récupérer les voies associées
            var voies = await _context.MedicamentVoies
                .Where(mv => mv.IdMedicament == idMedicament)
                .Include(mv => mv.VoieAdministration)
                .Where(mv => mv.VoieAdministration.Actif)
                .OrderByDescending(mv => mv.EstDefaut)
                .ThenBy(mv => mv.VoieAdministration.Ordre)
                .Select(mv => new VoieAdministrationDto
                {
                    IdVoie = mv.VoieAdministration.IdVoie,
                    Code = mv.VoieAdministration.Code,
                    Libelle = mv.VoieAdministration.Libelle,
                    Description = mv.VoieAdministration.Description,
                    EstDefaut = mv.EstDefaut
                })
                .ToListAsync();

            // Si aucune forme/voie associée, retourner toutes les options
            var useAllFormes = !formes.Any();
            var useAllVoies = !voies.Any();

            if (useAllFormes)
            {
                formes = await _context.FormesPharmaceutiques
                    .Where(f => f.Actif)
                    .OrderBy(f => f.Ordre)
                    .Select(f => new FormePharmaceutiqueDto
                    {
                        IdForme = f.IdForme,
                        Code = f.Code,
                        Libelle = f.Libelle,
                        Description = f.Description,
                        EstDefaut = false
                    })
                    .ToListAsync();
            }

            if (useAllVoies)
            {
                voies = await _context.VoiesAdministration
                    .Where(v => v.Actif)
                    .OrderBy(v => v.Ordre)
                    .Select(v => new VoieAdministrationDto
                    {
                        IdVoie = v.IdVoie,
                        Code = v.Code,
                        Libelle = v.Libelle,
                        Description = v.Description,
                        EstDefaut = false
                    })
                    .ToListAsync();
            }

            return Ok(new MedicamentFormesVoiesDto
            {
                IdMedicament = medicament.IdMedicament,
                NomMedicament = medicament.Nom,
                Dosage = medicament.Dosage,
                Formes = formes,
                Voies = voies,
                HasSpecificFormes = !useAllFormes,
                HasSpecificVoies = !useAllVoies
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des formes/voies pour le médicament {IdMedicament}", idMedicament);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }
}

// DTOs
public class FormePharmaceutiqueDto
{
    public int IdForme { get; set; }
    public string Code { get; set; } = "";
    public string Libelle { get; set; } = "";
    public string? Description { get; set; }
    public bool EstDefaut { get; set; }
}

public class VoieAdministrationDto
{
    public int IdVoie { get; set; }
    public string Code { get; set; } = "";
    public string Libelle { get; set; } = "";
    public string? Description { get; set; }
    public bool EstDefaut { get; set; }
}

public class MedicamentFormesVoiesDto
{
    public int IdMedicament { get; set; }
    public string NomMedicament { get; set; } = "";
    public string? Dosage { get; set; }
    public List<FormePharmaceutiqueDto> Formes { get; set; } = new();
    public List<VoieAdministrationDto> Voies { get; set; } = new();
    public bool HasSpecificFormes { get; set; }
    public bool HasSpecificVoies { get; set; }
}
