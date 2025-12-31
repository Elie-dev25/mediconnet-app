using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Consultation;
using Mediconnet_Backend.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service pour la gestion des paramètres vitaux
/// Gère les permissions et la logique métier
/// </summary>
public class ParametreService : IParametreService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ParametreService> _logger;
    private readonly IAppointmentNotificationService _notificationService;

    // Rôles autorisés à modifier les paramètres
    private static readonly string[] RolesModification = { "infirmier", "accueil", "administrateur" };
    
    // Rôles autorisés à voir les paramètres
    private static readonly string[] RolesLecture = { "infirmier", "accueil", "administrateur", "medecin" };

    public ParametreService(
        ApplicationDbContext context,
        ILogger<ParametreService> logger,
        IAppointmentNotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Récupère les paramètres d'une consultation
    /// </summary>
    public async Task<ParametreDto?> GetByConsultationIdAsync(int consultationId)
    {
        var parametre = await _context.Parametres
            .Include(p => p.UtilisateurEnregistrant)
            .FirstOrDefaultAsync(p => p.IdConsultation == consultationId);

        if (parametre == null) return null;

        return MapToDto(parametre);
    }

    /// <summary>
    /// Récupère l'historique des paramètres d'un patient
    /// </summary>
    public async Task<List<ParametreDto>> GetHistoriquePatientAsync(int patientId)
    {
        var parametres = await _context.Parametres
            .Include(p => p.Consultation)
            .Include(p => p.UtilisateurEnregistrant)
            .Where(p => p.Consultation != null && p.Consultation.IdPatient == patientId)
            .OrderByDescending(p => p.DateEnregistrement)
            .ToListAsync();

        return parametres.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Crée ou met à jour les paramètres d'une consultation
    /// </summary>
    public async Task<ParametreDto> CreateOrUpdateAsync(CreateParametreRequest request, int userId)
    {
        var consultation = await _context.Consultations
            .FirstOrDefaultAsync(c => c.IdConsultation == request.IdConsultation);

        if (consultation == null)
            throw new InvalidOperationException("Consultation introuvable");

        // Vérifier si des paramètres existent déjà pour cette consultation
        var existing = await _context.Parametres
            .FirstOrDefaultAsync(p => p.IdConsultation == request.IdConsultation);

        if (existing != null)
        {
            // Mise à jour
            existing.Poids = request.Poids;
            existing.Temperature = request.Temperature;
            existing.TensionSystolique = request.TensionSystolique;
            existing.TensionDiastolique = request.TensionDiastolique;
            existing.Taille = request.Taille;
            existing.DateEnregistrement = DateTime.UtcNow;
            existing.EnregistrePar = userId;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Paramètres mis à jour pour consultation {request.IdConsultation} par user {userId}");

            consultation.Statut = "pret_consultation";
            consultation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _notificationService.NotifyVitalsRecordedAsync(
                consultation.IdMedecin,
                consultation.IdConsultation,
                consultation.IdRendezVous);
            await _notificationService.NotifyNurseQueueRefreshAsync();

            return MapToDto(existing);
        }

        // Création
        var parametre = new Parametre
        {
            IdConsultation = request.IdConsultation,
            Poids = request.Poids,
            Temperature = request.Temperature,
            TensionSystolique = request.TensionSystolique,
            TensionDiastolique = request.TensionDiastolique,
            Taille = request.Taille,
            DateEnregistrement = DateTime.UtcNow,
            EnregistrePar = userId
        };

        _context.Parametres.Add(parametre);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Paramètres créés pour consultation {request.IdConsultation} par user {userId}");

        // Recharger avec les relations
        await _context.Entry(parametre).Reference(p => p.UtilisateurEnregistrant).LoadAsync();

        consultation.Statut = "pret_consultation";
        consultation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _notificationService.NotifyVitalsRecordedAsync(
            consultation.IdMedecin,
            consultation.IdConsultation,
            consultation.IdRendezVous);
        await _notificationService.NotifyNurseQueueRefreshAsync();

        return MapToDto(parametre);
    }

    /// <summary>
    /// Crée les paramètres pour un patient en utilisant sa consultation planifiée existante
    /// ou en créant une consultation à partir d'un rendez-vous confirmé
    /// Le patient doit avoir une consultation ou un RDV prévu avec un médecin
    /// Utilisé par l'infirmière depuis la liste des patients
    /// </summary>
    public async Task<ParametreDto> CreateByPatientAsync(CreateParametreByPatientRequest request, int userId)
    {
        // Vérifier que le patient existe
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.IdUser == request.IdPatient);
        if (patient == null)
            throw new InvalidOperationException("Patient introuvable");

        // Chercher une consultation planifiée existante pour ce patient (du jour)
        var aujourdhui = DateTime.UtcNow.Date;
        var consultation = await _context.Consultations
            .Where(c => c.IdPatient == request.IdPatient 
                && c.DateHeure.Date == aujourdhui
                && (c.Statut == "planifie" || c.Statut == "en_attente" || c.Statut == "arrive" || c.Statut == "pret_consultation"))
            .OrderBy(c => c.DateHeure)
            .FirstOrDefaultAsync();

        // Si pas de consultation, chercher un rendez-vous confirmé/planifié du jour
        if (consultation == null)
        {
            var rdv = await _context.RendezVous
                .Where(r => r.IdPatient == request.IdPatient 
                    && r.DateHeure.Date == aujourdhui
                    && (r.Statut == "planifie" || r.Statut == "confirme"))
                .OrderBy(r => r.DateHeure)
                .FirstOrDefaultAsync();

            if (rdv == null)
            {
                throw new InvalidOperationException("Ce patient n'a pas de consultation ni de rendez-vous prévu avec un médecin aujourd'hui. Veuillez d'abord créer un rendez-vous.");
            }

            // Créer la consultation à partir du RDV
            consultation = new Consultation
            {
                IdPatient = rdv.IdPatient,
                IdMedecin = rdv.IdMedecin,
                IdRendezVous = rdv.IdRendezVous,
                DateHeure = rdv.DateHeure,
                TypeConsultation = rdv.TypeRdv ?? "consultation",
                Motif = rdv.Motif,
                Statut = "en_attente"
            };

            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Consultation créée (ID: {consultation.IdConsultation}) à partir du RDV {rdv.IdRendezVous} pour patient {request.IdPatient}");
        }

        // Vérifier si des paramètres existent déjà pour cette consultation
        var existingParametre = await _context.Parametres
            .FirstOrDefaultAsync(p => p.IdConsultation == consultation.IdConsultation);

        if (existingParametre != null)
        {
            // Mise à jour des paramètres existants
            existingParametre.Poids = request.Poids;
            existingParametre.Temperature = request.Temperature;
            existingParametre.TensionSystolique = request.TensionSystolique;
            existingParametre.TensionDiastolique = request.TensionDiastolique;
            existingParametre.Taille = request.Taille;
            existingParametre.DateEnregistrement = DateTime.UtcNow;
            existingParametre.EnregistrePar = userId;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Paramètres mis à jour pour consultation {consultation.IdConsultation} par user {userId}");

            // Mettre à jour le statut de la consultation
            consultation.Statut = "pret_consultation";
            consultation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Notifier le médecin
            await _notificationService.NotifyVitalsRecordedAsync(
                consultation.IdMedecin,
                consultation.IdConsultation,
                consultation.IdRendezVous);
            await _notificationService.NotifyNurseQueueRefreshAsync();

            await _context.Entry(existingParametre).Reference(p => p.UtilisateurEnregistrant).LoadAsync();
            return MapToDto(existingParametre);
        }

        // Créer les paramètres
        var parametre = new Parametre
        {
            IdConsultation = consultation.IdConsultation,
            Poids = request.Poids,
            Temperature = request.Temperature,
            TensionSystolique = request.TensionSystolique,
            TensionDiastolique = request.TensionDiastolique,
            Taille = request.Taille,
            DateEnregistrement = DateTime.UtcNow,
            EnregistrePar = userId
        };

        _context.Parametres.Add(parametre);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Paramètres créés pour consultation {consultation.IdConsultation} (patient {request.IdPatient}) par user {userId}");

        // Mettre à jour le statut de la consultation
        consultation.Statut = "pret_consultation";
        consultation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Notifier le médecin
        await _notificationService.NotifyVitalsRecordedAsync(
            consultation.IdMedecin,
            consultation.IdConsultation,
            consultation.IdRendezVous);
        await _notificationService.NotifyNurseQueueRefreshAsync();

        // Recharger avec les relations
        await _context.Entry(parametre).Reference(p => p.UtilisateurEnregistrant).LoadAsync();

        return MapToDto(parametre);
    }

    /// <summary>
    /// Met à jour les paramètres existants
    /// </summary>
    public async Task<ParametreDto?> UpdateAsync(int parametreId, UpdateParametreRequest request, int userId)
    {
        var parametre = await _context.Parametres
            .Include(p => p.UtilisateurEnregistrant)
            .FirstOrDefaultAsync(p => p.IdParametre == parametreId);

        if (parametre == null) return null;

        parametre.Poids = request.Poids ?? parametre.Poids;
        parametre.Temperature = request.Temperature ?? parametre.Temperature;
        parametre.TensionSystolique = request.TensionSystolique ?? parametre.TensionSystolique;
        parametre.TensionDiastolique = request.TensionDiastolique ?? parametre.TensionDiastolique;
        parametre.Taille = request.Taille ?? parametre.Taille;
        parametre.DateEnregistrement = DateTime.UtcNow;
        parametre.EnregistrePar = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Paramètres {parametreId} mis à jour par user {userId}");

        return MapToDto(parametre);
    }

    /// <summary>
    /// Supprime les paramètres (admin uniquement)
    /// </summary>
    public async Task<bool> DeleteAsync(int parametreId)
    {
        var parametre = await _context.Parametres.FindAsync(parametreId);
        if (parametre == null) return false;

        _context.Parametres.Remove(parametre);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Paramètres {parametreId} supprimés");

        return true;
    }

    /// <summary>
    /// Vérifie si l'utilisateur peut modifier les paramètres
    /// Accueil, Infirmier, Admin peuvent modifier
    /// </summary>
    public bool CanModifyParametres(string role)
    {
        return RolesModification.Contains(role.ToLower());
    }

    /// <summary>
    /// Vérifie si l'utilisateur peut voir les paramètres
    /// Tous les rôles médicaux peuvent voir
    /// </summary>
    public bool CanViewParametres(string role)
    {
        return RolesLecture.Contains(role.ToLower());
    }

    /// <summary>
    /// Mappe l'entité vers le DTO
    /// </summary>
    private ParametreDto MapToDto(Parametre parametre)
    {
        return new ParametreDto
        {
            IdParametre = parametre.IdParametre,
            IdConsultation = parametre.IdConsultation,
            Poids = parametre.Poids,
            Temperature = parametre.Temperature,
            TensionSystolique = parametre.TensionSystolique,
            TensionDiastolique = parametre.TensionDiastolique,
            Taille = parametre.Taille,
            DateEnregistrement = parametre.DateEnregistrement,
            EnregistrePar = parametre.EnregistrePar,
            NomEnregistrant = parametre.UtilisateurEnregistrant != null 
                ? $"{parametre.UtilisateurEnregistrant.Prenom} {parametre.UtilisateurEnregistrant.Nom}"
                : null
        };
    }
}
