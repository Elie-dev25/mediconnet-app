using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Controllers;

[Route("api/infirmier")]
[Authorize(Roles = "infirmier,administrateur")]
public class InfirmierController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InfirmierController> _logger;

    public InfirmierController(ApplicationDbContext context, ILogger<InfirmierController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("file-attente")]
    public async Task<IActionResult> GetFileAttente()
    {
        try
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Patients à voir par l'infirmier:
            // - facture consultation payée
            // - consultation liée à la facture
            // - RDV du jour lié à la consultation
            // - paramètres NON saisis (parametre absent)
            // - consultation PAS encore prêt consultation
            var items = await _context.Consultations
                .Include(c => c.Patient)
                    .ThenInclude(p => p!.Utilisateur)
                .Include(c => c.Medecin)
                    .ThenInclude(m => m!.Utilisateur)
                .Where(c => c.IdRendezVous.HasValue)
                .Where(c => !_context.Parametres.Any(p => p.IdConsultation == c.IdConsultation))
                .Where(c => c.Statut == null || c.Statut != "pret_consultation")
                .Where(c => _context.Factures.Any(f =>
                    f.IdConsultation == c.IdConsultation &&
                    f.TypeFacture == "consultation" &&
                    f.Statut == "payee"))
                .Where(c => _context.RendezVous.Any(r =>
                    r.IdRendezVous == c.IdRendezVous &&
                    r.DateHeure >= today &&
                    r.DateHeure < tomorrow &&
                    r.Statut != "annule"))
                .OrderBy(c => c.DateHeure)
                .Select(c => new
                {
                    idConsultation = c.IdConsultation,
                    idRendezVous = c.IdRendezVous,
                    idPatient = c.IdPatient,
                    patientNom = c.Patient != null && c.Patient.Utilisateur != null ? c.Patient.Utilisateur.Nom : "",
                    patientPrenom = c.Patient != null && c.Patient.Utilisateur != null ? c.Patient.Utilisateur.Prenom : "",
                    numeroDossier = c.Patient != null ? c.Patient.NumeroDossier : null,
                    idMedecin = c.IdMedecin,
                    medecinNom = c.Medecin != null && c.Medecin.Utilisateur != null ? c.Medecin.Utilisateur.Nom : "",
                    medecinPrenom = c.Medecin != null && c.Medecin.Utilisateur != null ? c.Medecin.Utilisateur.Prenom : "",
                    dateHeure = c.DateHeure,
                    statutConsultation = c.Statut
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, count = items.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetFileAttente infirmier: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }
}
