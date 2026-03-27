using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Admin;
using Mediconnet_Backend.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Contrôleur pour la gestion des spécialités infirmiers
/// </summary>
[ApiController]
[Route("api/specialites-infirmiers")]
public class SpecialitesInfirmiersController : BaseAdminController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SpecialitesInfirmiersController> _logger;

    public SpecialitesInfirmiersController(
        ApplicationDbContext context,
        ILogger<SpecialitesInfirmiersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Liste toutes les spécialités infirmiers
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SpecialiteInfirmierDto>>> GetAll()
    {
        try
        {
            var specialites = await _context.SpecialitesInfirmiers
                .Select(s => new SpecialiteInfirmierDto
                {
                    IdSpecialite = s.IdSpecialite,
                    Code = s.Code,
                    Nom = s.Nom,
                    Description = s.Description,
                    Actif = s.Actif,
                    CreatedAt = s.CreatedAt,
                    NombreInfirmiers = s.Infirmiers.Count
                })
                .OrderBy(s => s.Nom)
                .ToListAsync();

            return Ok(specialites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des spécialités infirmiers");
            return StatusCode(500, new { message = "Erreur lors de la récupération des spécialités" });
        }
    }

    /// <summary>
    /// Liste les spécialités actives (pour les dropdowns)
    /// </summary>
    [HttpGet("actives")]
    public async Task<ActionResult<List<SpecialiteInfirmierDto>>> GetActives()
    {
        try
        {
            var specialites = await _context.SpecialitesInfirmiers
                .Where(s => s.Actif)
                .Select(s => new SpecialiteInfirmierDto
                {
                    IdSpecialite = s.IdSpecialite,
                    Code = s.Code,
                    Nom = s.Nom,
                    Description = s.Description,
                    Actif = s.Actif,
                    CreatedAt = s.CreatedAt,
                    NombreInfirmiers = s.Infirmiers.Count
                })
                .OrderBy(s => s.Nom)
                .ToListAsync();

            return Ok(specialites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des spécialités actives");
            return StatusCode(500, new { message = "Erreur lors de la récupération des spécialités" });
        }
    }

    /// <summary>
    /// Récupère une spécialité par son ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<SpecialiteInfirmierDto>> GetById(int id)
    {
        try
        {
            var specialite = await _context.SpecialitesInfirmiers
                .Where(s => s.IdSpecialite == id)
                .Select(s => new SpecialiteInfirmierDto
                {
                    IdSpecialite = s.IdSpecialite,
                    Code = s.Code,
                    Nom = s.Nom,
                    Description = s.Description,
                    Actif = s.Actif,
                    CreatedAt = s.CreatedAt,
                    NombreInfirmiers = s.Infirmiers.Count
                })
                .FirstOrDefaultAsync();

            if (specialite == null)
                return NotFound(new { message = "Spécialité non trouvée" });

            return Ok(specialite);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de la spécialité {Id}", id);
            return StatusCode(500, new { message = "Erreur lors de la récupération de la spécialité" });
        }
    }

    /// <summary>
    /// Crée une nouvelle spécialité infirmier (Admin uniquement)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSpecialiteInfirmierRequest request)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Vérifier si le code existe déjà
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var existingCode = await _context.SpecialitesInfirmiers
                    .AnyAsync(s => s.Code == request.Code);
                if (existingCode)
                    return BadRequest(new { message = "Ce code de spécialité existe déjà" });
            }

            // Vérifier si le nom existe déjà
            var existingNom = await _context.SpecialitesInfirmiers
                .AnyAsync(s => s.Nom == request.Nom);
            if (existingNom)
                return BadRequest(new { message = "Ce nom de spécialité existe déjà" });

            var specialite = new SpecialiteInfirmier
            {
                Code = request.Code,
                Nom = request.Nom,
                Description = request.Description,
                Actif = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.SpecialitesInfirmiers.Add(specialite);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Spécialité infirmier créée: {Nom} (ID: {Id})", specialite.Nom, specialite.IdSpecialite);

            return CreatedAtAction(nameof(GetById), new { id = specialite.IdSpecialite }, new SpecialiteInfirmierDto
            {
                IdSpecialite = specialite.IdSpecialite,
                Code = specialite.Code,
                Nom = specialite.Nom,
                Description = specialite.Description,
                Actif = specialite.Actif,
                CreatedAt = specialite.CreatedAt,
                NombreInfirmiers = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de la spécialité infirmier");
            return StatusCode(500, new { message = "Erreur lors de la création de la spécialité" });
        }
    }

    /// <summary>
    /// Met à jour une spécialité infirmier (Admin uniquement)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSpecialiteInfirmierRequest request)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var specialite = await _context.SpecialitesInfirmiers
                .Include(s => s.Infirmiers)
                .FirstOrDefaultAsync(s => s.IdSpecialite == id);

            if (specialite == null)
                return NotFound(new { message = "Spécialité non trouvée" });

            // Vérifier si le code existe déjà (pour une autre spécialité)
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var existingCode = await _context.SpecialitesInfirmiers
                    .AnyAsync(s => s.Code == request.Code && s.IdSpecialite != id);
                if (existingCode)
                    return BadRequest(new { message = "Ce code de spécialité existe déjà" });
            }

            // Vérifier si le nom existe déjà (pour une autre spécialité)
            var existingNom = await _context.SpecialitesInfirmiers
                .AnyAsync(s => s.Nom == request.Nom && s.IdSpecialite != id);
            if (existingNom)
                return BadRequest(new { message = "Ce nom de spécialité existe déjà" });

            specialite.Code = request.Code;
            specialite.Nom = request.Nom;
            specialite.Description = request.Description;
            specialite.Actif = request.Actif;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Spécialité infirmier mise à jour: {Nom} (ID: {Id})", specialite.Nom, specialite.IdSpecialite);

            return Ok(new SpecialiteInfirmierDto
            {
                IdSpecialite = specialite.IdSpecialite,
                Code = specialite.Code,
                Nom = specialite.Nom,
                Description = specialite.Description,
                Actif = specialite.Actif,
                CreatedAt = specialite.CreatedAt,
                NombreInfirmiers = specialite.Infirmiers.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour de la spécialité {Id}", id);
            return StatusCode(500, new { message = "Erreur lors de la mise à jour de la spécialité" });
        }
    }

    /// <summary>
    /// Supprime une spécialité infirmier (Admin uniquement)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var specialite = await _context.SpecialitesInfirmiers
                .Include(s => s.Infirmiers)
                .FirstOrDefaultAsync(s => s.IdSpecialite == id);

            if (specialite == null)
                return NotFound(new { message = "Spécialité non trouvée" });

            // Vérifier si des infirmiers utilisent cette spécialité
            if (specialite.Infirmiers.Any())
            {
                return BadRequest(new { 
                    message = $"Impossible de supprimer cette spécialité car {specialite.Infirmiers.Count} infirmier(s) l'utilisent. Désactivez-la plutôt." 
                });
            }

            _context.SpecialitesInfirmiers.Remove(specialite);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Spécialité infirmier supprimée: {Nom} (ID: {Id})", specialite.Nom, id);

            return Ok(new { message = "Spécialité supprimée avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression de la spécialité {Id}", id);
            return StatusCode(500, new { message = "Erreur lors de la suppression de la spécialité" });
        }
    }
}
