using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Admin;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Services;

public interface IUserDetailsService
{
    Task<UserDetailsDto?> GetUserDetailsAsync(int userId);
}

public class UserDetailsService : IUserDetailsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserDetailsService> _logger;

    public UserDetailsService(ApplicationDbContext context, ILogger<UserDetailsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserDetailsDto?> GetUserDetailsAsync(int userId)
    {
        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.IdUser == userId);

        if (utilisateur == null)
            return null;

        var dto = new UserDetailsDto
        {
            IdUser = utilisateur.IdUser,
            Nom = utilisateur.Nom,
            Prenom = utilisateur.Prenom,
            Email = utilisateur.Email,
            Telephone = utilisateur.Telephone,
            Role = utilisateur.Role,
            Naissance = utilisateur.Naissance,
            Sexe = utilisateur.Sexe,
            Adresse = utilisateur.Adresse,
            SituationMatrimoniale = utilisateur.SituationMatrimoniale,
            Nationalite = utilisateur.Nationalite,
            RegionOrigine = utilisateur.RegionOrigine,
            Photo = utilisateur.Photo,
            EmailConfirmed = utilisateur.EmailConfirmed,
            EmailConfirmedAt = utilisateur.EmailConfirmedAt,
            ProfileCompleted = utilisateur.ProfileCompleted,
            ProfileCompletedAt = utilisateur.ProfileCompletedAt,
            CreatedAt = utilisateur.CreatedAt,
            UpdatedAt = utilisateur.UpdatedAt
        };

        // Charger les données spécifiques au rôle
        switch (utilisateur.Role.ToLower())
        {
            case "infirmier":
                dto.Infirmier = await GetInfirmierDetailsAsync(userId);
                break;
            case "medecin":
                dto.Medecin = await GetMedecinDetailsAsync(userId);
                break;
            case "patient":
                dto.Patient = await GetPatientDetailsAsync(userId);
                break;
        }

        return dto;
    }

    private async Task<InfirmierDetailsDto?> GetInfirmierDetailsAsync(int userId)
    {
        var infirmier = await _context.Infirmiers
            .Include(i => i.Service)
            .FirstOrDefaultAsync(i => i.IdUser == userId);

        if (infirmier == null)
            return null;

        // Vérifier si l'infirmier est Major d'un service (via Service.IdMajor)
        var serviceMajor = await _context.Services
            .FirstOrDefaultAsync(s => s.IdMajor == userId);

        bool isMajor = serviceMajor != null;

        return new InfirmierDetailsDto
        {
            Matricule = infirmier.Matricule,
            Statut = infirmier.Statut,
            IdService = infirmier.IdService,
            NomService = infirmier.Service?.NomService,
            IsMajor = isMajor,
            IdServiceMajor = serviceMajor?.IdService,
            NomServiceMajor = serviceMajor?.NomService,
            DateNominationMajor = infirmier.DateNominationMajor,
            Accreditations = infirmier.Accreditations,
            TitreAffiche = isMajor && serviceMajor != null
                ? $"Major {serviceMajor.NomService}"
                : "Infirmier"
        };
    }

    private async Task<MedecinDetailsDto?> GetMedecinDetailsAsync(int userId)
    {
        var medecin = await _context.Medecins
            .Include(m => m.Specialite)
            .Include(m => m.Service)
            .FirstOrDefaultAsync(m => m.IdUser == userId);

        if (medecin == null)
            return null;

        return new MedecinDetailsDto
        {
            NumeroOrdre = medecin.NumeroOrdre,
            IdSpecialite = medecin.IdSpecialite,
            NomSpecialite = medecin.Specialite?.NomSpecialite,
            IdService = medecin.IdService,
            NomService = medecin.Service?.NomService
        };
    }

    private async Task<PatientDetailsDto?> GetPatientDetailsAsync(int userId)
    {
        var patient = await _context.Patients
            .Include(p => p.Assurance)
            .FirstOrDefaultAsync(p => p.IdUser == userId);

        if (patient == null)
            return null;

        return new PatientDetailsDto
        {
            NumeroPatient = patient.NumeroDossier,
            GroupeSanguin = patient.GroupeSanguin,
            Allergies = patient.AllergiesDetails,
            AntecedentsMedicaux = patient.AntecedentsFamiliauxDetails,
            ContactUrgenceNom = patient.PersonneContact,
            ContactUrgenceTelephone = patient.NumeroContact,
            DeclarationHonneurAcceptee = patient.DeclarationHonneurAcceptee,
            DateDeclarationHonneur = patient.DeclarationHonneurAt,
            IdAssurance = patient.AssuranceId,
            NomAssurance = patient.Assurance?.Nom,
            NumeroAssurance = patient.NumeroCarteAssurance
        };
    }
}
