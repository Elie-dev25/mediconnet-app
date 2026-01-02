using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Medecin;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service pour la gestion des médecins
/// Contient la logique métier liée aux médecins
/// </summary>
public class MedecinService : IMedecinService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedecinService> _logger;

    public MedecinService(ApplicationDbContext context, ILogger<MedecinService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MedecinProfileDto?> GetProfileAsync(int userId)
    {
        var medecin = await _context.Medecins
            .Include(m => m.Utilisateur)
            .Include(m => m.Service)
            .FirstOrDefaultAsync(m => m.IdUser == userId);

        if (medecin?.Utilisateur == null)
            return null;

        // Récupérer la spécialité
        string? specialiteNom = null;
        if (medecin.IdSpecialite.HasValue)
        {
            var specialite = await _context.Specialites
                .FirstOrDefaultAsync(s => s.IdSpecialite == medecin.IdSpecialite.Value);
            specialiteNom = specialite?.NomSpecialite;
        }

        return new MedecinProfileDto
        {
            IdUser = medecin.IdUser,
            Nom = medecin.Utilisateur.Nom,
            Prenom = medecin.Utilisateur.Prenom,
            Email = medecin.Utilisateur.Email,
            Telephone = medecin.Utilisateur.Telephone,
            Adresse = medecin.Utilisateur.Adresse,
            Photo = medecin.Utilisateur.Photo,
            Sexe = medecin.Utilisateur.Sexe,
            Naissance = medecin.Utilisateur.Naissance,
            NumeroOrdre = medecin.NumeroOrdre,
            Specialite = specialiteNom,
            IdSpecialite = medecin.IdSpecialite,
            Service = medecin.Service?.NomService,
            IdService = medecin.IdService,
            CreatedAt = medecin.Utilisateur.CreatedAt
        };
    }

    /// <inheritdoc />
    public async Task<MedecinDashboardDto> GetDashboardAsync(int userId)
    {
        var now = DateTime.Now;
        var debutMois = new DateTime(now.Year, now.Month, 1);
        var finMois = debutMois.AddMonths(1);

        // Nombre de patients distincts
        var totalPatients = await _context.RendezVous
            .Where(r => r.IdMedecin == userId && r.Statut != "annule")
            .Select(r => r.IdPatient)
            .Distinct()
            .CountAsync();

        // Consultations ce mois
        var consultationsMois = await _context.RendezVous
            .CountAsync(r => r.IdMedecin == userId && 
                       r.DateHeure >= debutMois && 
                       r.DateHeure < finMois &&
                       r.Statut == "termine");

        // RDV aujourd'hui
        var rdvAujourdHui = await _context.RendezVous
            .CountAsync(r => r.IdMedecin == userId && 
                       r.DateHeure.Date == now.Date &&
                       r.Statut != "annule");

        // RDV à venir
        var rdvAVenir = await _context.RendezVous
            .CountAsync(r => r.IdMedecin == userId && 
                       r.DateHeure > now &&
                       (r.Statut == "planifie" || r.Statut == "confirme"));

        return new MedecinDashboardDto
        {
            TotalPatients = totalPatients,
            ConsultationsMois = consultationsMois,
            RdvAujourdHui = rdvAujourdHui,
            RdvAVenir = rdvAVenir,
            OrdonnancesMois = 0,
            ExamensMois = 0
        };
    }

    /// <inheritdoc />
    public async Task<bool> UpdateProfileAsync(int userId, UpdateMedecinProfileRequest request)
    {
        var medecin = await _context.Medecins
            .Include(m => m.Utilisateur)
            .FirstOrDefaultAsync(m => m.IdUser == userId);

        if (medecin?.Utilisateur == null)
            return false;

        if (!string.IsNullOrEmpty(request.Telephone))
            medecin.Utilisateur.Telephone = request.Telephone;
        if (!string.IsNullOrEmpty(request.Adresse))
            medecin.Utilisateur.Adresse = request.Adresse;

        medecin.Utilisateur.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Profile updated for medecin {UserId}", userId);
        return true;
    }

    /// <inheritdoc />
    public async Task<MedecinAgendaDto> GetAgendaAsync(int userId, DateTime dateDebut, DateTime dateFin)
    {
        // Récupérer les créneaux configurés
        var creneaux = await _context.CreneauxDisponibles
            .Where(c => c.IdMedecin == userId && c.Actif)
            .Select(c => new CreneauAgendaDto
            {
                Id = c.IdCreneau,
                JourSemaine = c.JourSemaine.ToString(),
                HeureDebut = c.HeureDebut,
                HeureFin = c.HeureFin,
                DureeMinutes = c.DureeParDefaut,
                Actif = c.Actif
            })
            .ToListAsync();

        // Récupérer les RDV existants
        var rdvs = await _context.RendezVous
            .Include(r => r.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Where(r => r.IdMedecin == userId && 
                       r.DateHeure >= dateDebut && 
                       r.DateHeure <= dateFin &&
                       r.Statut != "annule")
            .Select(r => new RdvAgendaDto
            {
                Id = r.IdRendezVous,
                DateHeure = r.DateHeure,
                Statut = r.Statut,
                PatientNom = r.Patient != null && r.Patient.Utilisateur != null 
                    ? $"{r.Patient.Utilisateur.Prenom} {r.Patient.Utilisateur.Nom}" 
                    : "Patient inconnu",
                Motif = r.Motif
            })
            .ToListAsync();

        return new MedecinAgendaDto
        {
            Creneaux = creneaux,
            RendezVous = rdvs
        };
    }
}
