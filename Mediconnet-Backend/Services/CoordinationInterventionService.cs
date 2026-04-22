using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Chirurgie;
using Mediconnet_Backend.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Mediconnet_Backend.Services;

public interface ICoordinationInterventionService
{
    Task<List<AnesthesisteDisponibiliteDto>> GetAnesthesistesDisponiblesAsync(DateTime dateDebut, DateTime dateFin, int dureeMinutes);
    Task<List<CreneauDisponibleDto>> GetCreneauxAnesthesisteAsync(int idAnesthesiste, DateTime dateDebut, DateTime dateFin);
    Task<CoordinationActionResponse> ProposerCoordinationAsync(ProposerCoordinationRequest request, int idChirurgien);
    Task<CoordinationActionResponse> ValiderCoordinationAsync(ValiderCoordinationRequest request, int idAnesthesiste);
    Task<CoordinationActionResponse> ModifierCoordinationAsync(ModifierCoordinationRequest request, int idAnesthesiste);
    Task<CoordinationActionResponse> RefuserCoordinationAsync(RefuserCoordinationRequest request, int idAnesthesiste);
    Task<CoordinationActionResponse> AccepterContrePropositionAsync(AccepterContrePropositionRequest request, int idChirurgien);
    Task<CoordinationActionResponse> RefuserContrePropositionAsync(RefuserContrePropositionRequest request, int idChirurgien);
    Task<CoordinationActionResponse> AnnulerCoordinationAsync(AnnulerCoordinationRequest request, int idUser);
    Task<CoordinationInterventionDto?> GetCoordinationAsync(int idCoordination);
    Task<List<CoordinationInterventionDto>> GetCoordinationsChirurgienAsync(int idChirurgien, CoordinationFilterDto? filter = null);
    Task<List<CoordinationInterventionDto>> GetCoordinationsAnesthesisteAsync(int idAnesthesiste, CoordinationFilterDto? filter = null);
    Task<List<CoordinationHistoriqueDto>> GetHistoriqueCoordinationAsync(int idCoordination);
    Task<CoordinationStatsDto> GetStatsAnesthesisteAsync(int idAnesthesiste);
}

public class CoordinationInterventionService : ICoordinationInterventionService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly IEmailService _emailService;
    private readonly ILogger<CoordinationInterventionService> _logger;
    
    // ID de la spécialité Anesthésiologie
    private const int SPECIALITE_ANESTHESIOLOGIE = 3;

    public CoordinationInterventionService(
        ApplicationDbContext context,
        IHubContext<NotificationHub> notificationHub,
        IEmailService emailService,
        ILogger<CoordinationInterventionService> logger)
    {
        _context = context;
        _notificationHub = notificationHub;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Récupère la liste des anesthésistes avec leurs disponibilités
    /// </summary>
    public async Task<List<AnesthesisteDisponibiliteDto>> GetAnesthesistesDisponiblesAsync(
        DateTime dateDebut, DateTime dateFin, int dureeMinutes)
    {
        var anesthesistes = await _context.Medecins
            .Include(m => m.Utilisateur)
            .Include(m => m.Specialite)
            .Where(m => m.IdSpecialite == SPECIALITE_ANESTHESIOLOGIE)
            .ToListAsync();

        var result = new List<AnesthesisteDisponibiliteDto>();

        foreach (var anesth in anesthesistes)
        {
            var creneaux = await GetCreneauxAnesthesisteAsync(anesth.IdUser, dateDebut, dateFin);
            
            // Compter les interventions de la semaine
            var debutSemaine = dateDebut.Date.AddDays(-(int)dateDebut.DayOfWeek + 1);
            var finSemaine = debutSemaine.AddDays(7);
            var nbInterventions = await _context.Set<CoordinationIntervention>()
                .CountAsync(c => c.IdAnesthesiste == anesth.IdUser 
                    && c.Statut == "validee"
                    && c.DateProposee >= debutSemaine 
                    && c.DateProposee < finSemaine);

            result.Add(new AnesthesisteDisponibiliteDto
            {
                IdMedecin = anesth.IdUser,
                Nom = anesth.Utilisateur?.Nom ?? "",
                Prenom = anesth.Utilisateur?.Prenom ?? "",
                Photo = anesth.Utilisateur?.Photo,
                NbInterventionsSemaine = nbInterventions,
                CreneauxDisponibles = creneaux.Where(c => c.DureeMinutes >= dureeMinutes).ToList()
            });
        }

        return result.OrderByDescending(a => a.CreneauxDisponibles.Count).ToList();
    }

    /// <summary>
    /// Récupère les créneaux disponibles d'un anesthésiste
    /// </summary>
    public async Task<List<CreneauDisponibleDto>> GetCreneauxAnesthesisteAsync(
        int idAnesthesiste, DateTime dateDebut, DateTime dateFin)
    {
        var creneaux = new List<CreneauDisponibleDto>();
        
        // Récupérer les indisponibilités
        var indisponibilites = await _context.IndisponibilitesMedecin
            .Where(i => i.IdMedecin == idAnesthesiste
                && i.DateDebut.Date <= dateFin.Date
                && i.DateFin.Date >= dateDebut.Date)
            .ToListAsync();

        // Récupérer les RDV existants
        var rdvs = await _context.RendezVous
            .Where(r => r.IdMedecin == idAnesthesiste
                && r.DateHeure.Date >= dateDebut.Date
                && r.DateHeure.Date <= dateFin.Date
                && r.Statut != "annulé")
            .ToListAsync();

        // Récupérer les coordinations validées
        var coordinations = await _context.Set<CoordinationIntervention>()
            .Where(c => c.IdAnesthesiste == idAnesthesiste
                && c.Statut == "validee"
                && c.DateProposee.Date >= dateDebut.Date
                && c.DateProposee.Date <= dateFin.Date)
            .ToListAsync();

        // Générer les créneaux pour chaque jour
        for (var date = dateDebut.Date; date <= dateFin.Date; date = date.AddDays(1))
        {
            // Ignorer les week-ends
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                continue;

            // Créneaux de 8h à 18h par tranches de 30 min
            for (var heure = 8; heure < 18; heure++)
            {
                foreach (var minute in new[] { 0, 30 })
                {
                    var heureDebut = new TimeSpan(heure, minute, 0);
                    var heureFin = heureDebut.Add(TimeSpan.FromMinutes(30));
                    var dateHeure = date.Add(heureDebut);

                    // Vérifier si le créneau est disponible
                    var estDisponible = true;
                    string? motif = null;

                    // Vérifier les indisponibilités
                    var indispo = indisponibilites.FirstOrDefault(i => 
                        dateHeure >= i.DateDebut && dateHeure < i.DateFin);
                    if (indispo != null)
                    {
                        estDisponible = false;
                        motif = indispo.Motif ?? "Indisponible";
                    }

                    // Vérifier les RDV
                    var rdv = rdvs.FirstOrDefault(r => 
                        r.DateHeure <= dateHeure && r.DateHeure.AddMinutes(30) > dateHeure);
                    if (rdv != null)
                    {
                        estDisponible = false;
                        motif = "Consultation";
                    }

                    // Vérifier les interventions
                    var coord = coordinations.FirstOrDefault(c =>
                    {
                        var debut = c.DateProposee.Date.Add(TimeSpan.Parse(c.HeureProposee, System.Globalization.CultureInfo.InvariantCulture));
                        var fin = debut.AddMinutes(c.DureeEstimee);
                        return dateHeure >= debut && dateHeure < fin;
                    });
                    if (coord != null)
                    {
                        estDisponible = false;
                        motif = "Intervention programmée";
                    }

                    creneaux.Add(new CreneauDisponibleDto
                    {
                        Date = date,
                        HeureDebut = $"{heure:D2}:{minute:D2}",
                        HeureFin = $"{heureFin.Hours:D2}:{heureFin.Minutes:D2}",
                        DureeMinutes = 30,
                        EstDisponible = estDisponible,
                        MotifIndisponibilite = motif
                    });
                }
            }
        }

        // Fusionner les créneaux consécutifs disponibles
        return FusionnerCreneauxConsecutifs(creneaux);
    }

    private List<CreneauDisponibleDto> FusionnerCreneauxConsecutifs(List<CreneauDisponibleDto> creneaux)
    {
        var result = new List<CreneauDisponibleDto>();
        CreneauDisponibleDto? current = null;

        foreach (var creneau in creneaux.OrderBy(c => c.Date).ThenBy(c => c.HeureDebut))
        {
            if (!creneau.EstDisponible)
            {
                if (current != null)
                {
                    result.Add(current);
                    current = null;
                }
                result.Add(creneau);
                continue;
            }

            if (current == null)
            {
                current = new CreneauDisponibleDto
                {
                    Date = creneau.Date,
                    HeureDebut = creneau.HeureDebut,
                    HeureFin = creneau.HeureFin,
                    DureeMinutes = creneau.DureeMinutes,
                    EstDisponible = true
                };
            }
            else if (current.Date == creneau.Date && current.HeureFin == creneau.HeureDebut)
            {
                current.HeureFin = creneau.HeureFin;
                current.DureeMinutes += creneau.DureeMinutes;
            }
            else
            {
                result.Add(current);
                current = new CreneauDisponibleDto
                {
                    Date = creneau.Date,
                    HeureDebut = creneau.HeureDebut,
                    HeureFin = creneau.HeureFin,
                    DureeMinutes = creneau.DureeMinutes,
                    EstDisponible = true
                };
            }
        }

        if (current != null)
            result.Add(current);

        return result;
    }

    /// <summary>
    /// Proposer une coordination (par le chirurgien)
    /// </summary>
    public async Task<CoordinationActionResponse> ProposerCoordinationAsync(
        ProposerCoordinationRequest request, int idChirurgien)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Vérifier la programmation
            var programmation = await _context.ProgrammationsInterventions
                .Include(p => p.Patient).ThenInclude(p => p.Utilisateur)
                .Include(p => p.Chirurgien).ThenInclude(c => c.Utilisateur)
                .FirstOrDefaultAsync(p => p.IdProgrammation == request.IdProgrammation);

            if (programmation == null)
                return new CoordinationActionResponse { Success = false, Message = "Programmation non trouvée" };

            if (programmation.IdChirurgien != idChirurgien)
                return new CoordinationActionResponse { Success = false, Message = "Vous n'êtes pas le chirurgien de cette intervention" };

            // Vérifier que l'anesthésiste existe et est bien anesthésiste
            var anesthesiste = await _context.Medecins
                .Include(m => m.Utilisateur)
                .FirstOrDefaultAsync(m => m.IdUser == request.IdAnesthesiste && m.IdSpecialite == SPECIALITE_ANESTHESIOLOGIE);

            if (anesthesiste == null)
                return new CoordinationActionResponse { Success = false, Message = "Anesthésiste non trouvé ou spécialité incorrecte" };

            // Vérifier la disponibilité du créneau
            var creneauxDispo = await GetCreneauxAnesthesisteAsync(
                request.IdAnesthesiste, 
                request.DateProposee.Date, 
                request.DateProposee.Date);

            var creneauValide = creneauxDispo.Any(c => 
                c.EstDisponible 
                && c.Date == request.DateProposee.Date
                && c.HeureDebut == request.HeureProposee
                && c.DureeMinutes >= request.DureeEstimee);

            // Note: On permet quand même la proposition même si le créneau n'est pas parfaitement disponible
            // L'anesthésiste pourra contre-proposer

            // Vérifier s'il existe déjà une coordination active
            var existingCoord = await _context.Set<CoordinationIntervention>()
                .FirstOrDefaultAsync(c => c.IdProgrammation == request.IdProgrammation 
                    && c.Statut != "annulee" && c.Statut != "refusee");

            if (existingCoord != null)
                return new CoordinationActionResponse { Success = false, Message = "Une coordination est déjà en cours pour cette intervention" };

            // Créer la coordination
            var coordination = new CoordinationIntervention
            {
                IdProgrammation = request.IdProgrammation,
                IdChirurgien = idChirurgien,
                IdAnesthesiste = request.IdAnesthesiste,
                DateProposee = request.DateProposee,
                HeureProposee = request.HeureProposee,
                DureeEstimee = request.DureeEstimee,
                NotesChirurgien = request.NotesChirurgien,
                Statut = "proposee",
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<CoordinationIntervention>().Add(coordination);

            // Mettre à jour la programmation
            programmation.Statut = "coordination_proposee";
            programmation.IdAnesthesiste = request.IdAnesthesiste;
            programmation.DatePrevue = request.DateProposee;
            programmation.HeureDebut = request.HeureProposee;
            programmation.DureeEstimee = request.DureeEstimee;
            programmation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Ajouter à l'historique
            await AddHistoriqueAsync(coordination.IdCoordination, "proposition", idChirurgien, "chirurgien",
                $"Proposition de créneau: {request.DateProposee:dd/MM/yyyy} à {request.HeureProposee}",
                request.DateProposee, request.HeureProposee);

            // Envoyer notification à l'anesthésiste
            await SendNotificationAsync(
                request.IdAnesthesiste,
                "Nouvelle demande de coordination",
                $"Dr. {programmation.Chirurgien.Utilisateur?.Prenom} {programmation.Chirurgien.Utilisateur?.Nom} vous propose une intervention pour {programmation.Patient?.Utilisateur?.Prenom} {programmation.Patient?.Utilisateur?.Nom} le {request.DateProposee:dd/MM/yyyy} à {request.HeureProposee}",
                "coordination_intervention",
                coordination.IdCoordination);

            // Envoyer email à l'anesthésiste (non bloquant)
            _ = Task.Run(async () =>
            {
                try
                {
                    var emailAnesthesiste = anesthesiste.Utilisateur?.Email;
                    if (!string.IsNullOrEmpty(emailAnesthesiste))
                    {
                        await _emailService.SendCoordinationDemandeAsync(
                            emailAnesthesiste,
                            $"{anesthesiste.Utilisateur?.Prenom} {anesthesiste.Utilisateur?.Nom}",
                            $"{programmation.Chirurgien.Utilisateur?.Prenom} {programmation.Chirurgien.Utilisateur?.Nom}",
                            $"{programmation.Patient?.Utilisateur?.Prenom} {programmation.Patient?.Utilisateur?.Nom}",
                            request.DateProposee.ToString("dd/MM/yyyy"),
                            request.HeureProposee,
                            programmation.IndicationOperatoire ?? "Non spécifiée");
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Échec envoi email coordination demande");
                }
            });

            await transaction.CommitAsync();

            return new CoordinationActionResponse
            {
                Success = true,
                Message = "Proposition envoyée à l'anesthésiste",
                IdCoordination = coordination.IdCoordination,
                NouveauStatut = "proposee"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erreur lors de la proposition de coordination");
            return new CoordinationActionResponse { Success = false, Message = "Erreur lors de la proposition" };
        }
    }

    /// <summary>
    /// Valider une coordination (par l'anesthésiste)
    /// </summary>
    public async Task<CoordinationActionResponse> ValiderCoordinationAsync(
        ValiderCoordinationRequest request, int idAnesthesiste)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var coordination = await _context.Set<CoordinationIntervention>()
                .Include(c => c.Programmation).ThenInclude(p => p.Patient).ThenInclude(p => p.Utilisateur)
                .Include(c => c.Chirurgien).ThenInclude(c => c.Utilisateur)
                .Include(c => c.Anesthesiste).ThenInclude(a => a.Utilisateur)
                .FirstOrDefaultAsync(c => c.IdCoordination == request.IdCoordination);

            if (coordination == null)
                return new CoordinationActionResponse { Success = false, Message = "Coordination non trouvée" };

            if (coordination.IdAnesthesiste != idAnesthesiste)
                return new CoordinationActionResponse { Success = false, Message = "Vous n'êtes pas l'anesthésiste de cette coordination" };

            if (coordination.Statut != "proposee" && coordination.Statut != "modifiee")
                return new CoordinationActionResponse { Success = false, Message = $"Impossible de valider une coordination en statut '{coordination.Statut}'" };

            // Déterminer la date finale (originale ou contre-proposée acceptée)
            var datefinale = coordination.DateContreProposee ?? coordination.DateProposee;
            var heureFinale = coordination.HeureContreProposee ?? coordination.HeureProposee;

            // Créer les indisponibilités pour bloquer les agendas
            var dateHeureDebut = datefinale.Date.Add(TimeSpan.Parse(heureFinale, System.Globalization.CultureInfo.InvariantCulture));
            var dateHeureFin = dateHeureDebut.AddMinutes(coordination.DureeEstimee);

            // Indisponibilité chirurgien
            var indispoChirurgien = new IndisponibiliteMedecin
            {
                IdMedecin = coordination.IdChirurgien,
                DateDebut = dateHeureDebut,
                DateFin = dateHeureFin,
                Motif = $"Intervention chirurgicale - Patient: {coordination.Programmation.Patient.Utilisateur.Prenom} {coordination.Programmation.Patient.Utilisateur.Nom}",
                Type = "intervention"
            };
            _context.IndisponibilitesMedecin.Add(indispoChirurgien);

            // Indisponibilité anesthésiste
            var indispoAnesthesiste = new IndisponibiliteMedecin
            {
                IdMedecin = coordination.IdAnesthesiste,
                DateDebut = dateHeureDebut,
                DateFin = dateHeureFin,
                Motif = $"Intervention chirurgicale - Patient: {coordination.Programmation.Patient.Utilisateur.Prenom} {coordination.Programmation.Patient.Utilisateur.Nom}",
                Type = "intervention"
            };
            _context.IndisponibilitesMedecin.Add(indispoAnesthesiste);

            await _context.SaveChangesAsync();

            // Créer le RDV de consultation pré-opératoire UNIQUEMENT si l'anesthésiste l'a planifié
            RendezVous? rdvConsultation = null;
            if (request.DateRdvConsultation.HasValue && !string.IsNullOrEmpty(request.HeureRdvConsultation))
            {
                var heureRdvParts = request.HeureRdvConsultation.Split(':');
                var dateHeureRdv = request.DateRdvConsultation.Value.Date
                    .AddHours(int.Parse(heureRdvParts[0]))
                    .AddMinutes(int.Parse(heureRdvParts[1]));

                // Vérifier que le RDV est avant l'intervention
                if (dateHeureRdv >= datefinale)
                {
                    await transaction.RollbackAsync();
                    return new CoordinationActionResponse 
                    { 
                        Success = false, 
                        Message = "Le RDV de consultation doit être programmé avant la date d'intervention" 
                    };
                }

                rdvConsultation = new RendezVous
                {
                    IdPatient = coordination.Programmation.IdPatient,
                    IdMedecin = idAnesthesiste,
                    DateHeure = dateHeureRdv,
                    Motif = $"Consultation pré-opératoire - Intervention prévue le {datefinale:dd/MM/yyyy}",
                    Statut = "confirmé",
                    TypeRdv = "consultation_preanesthesique"
                };
                _context.RendezVous.Add(rdvConsultation);
                await _context.SaveChangesAsync();

                coordination.IdRdvConsultationAnesthesiste = rdvConsultation.IdRendezVous;
            }

            // Mettre à jour la coordination
            coordination.Statut = "validee";
            coordination.CommentaireAnesthesiste = request.CommentaireAnesthesiste;
            coordination.DateValidation = DateTime.UtcNow;
            coordination.DateReponse = DateTime.UtcNow;
            coordination.IdIndisponibiliteChirurgien = indispoChirurgien.IdIndisponibilite;
            coordination.IdIndisponibiliteAnesthesiste = indispoAnesthesiste.IdIndisponibilite;
            coordination.UpdatedAt = DateTime.UtcNow;

            // Mettre à jour la programmation
            coordination.Programmation.Statut = "coordination_validee";
            coordination.Programmation.DatePrevue = datefinale;
            coordination.Programmation.HeureDebut = heureFinale;
            coordination.Programmation.IdIndisponibilite = indispoChirurgien.IdIndisponibilite;
            coordination.Programmation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Ajouter à l'historique
            await AddHistoriqueAsync(coordination.IdCoordination, "validation", idAnesthesiste, "anesthesiste",
                "Coordination validée", datefinale, heureFinale);

            // Notifier le chirurgien
            await SendNotificationAsync(
                coordination.IdChirurgien,
                "Coordination validée",
                $"Dr. {coordination.Anesthesiste.Utilisateur?.Prenom} {coordination.Anesthesiste.Utilisateur?.Nom} a validé l'intervention du {datefinale:dd/MM/yyyy} à {heureFinale}",
                "coordination_validee",
                coordination.IdCoordination);

            // Notifier le patient pour le RDV de consultation
            if (rdvConsultation != null)
            {
                await SendNotificationAsync(
                    coordination.Programmation.IdPatient,
                    "RDV consultation pré-opératoire",
                    $"Un rendez-vous de consultation pré-opératoire a été programmé le {rdvConsultation.DateHeure:dd/MM/yyyy à HH:mm} avec Dr. {coordination.Anesthesiste.Utilisateur?.Prenom} {coordination.Anesthesiste.Utilisateur?.Nom}",
                    "rdv_preanesthesique",
                    rdvConsultation.IdRendezVous);
            }

            // Envoyer emails (non bloquant)
            var nomChirurgien = $"{coordination.Chirurgien.Utilisateur?.Prenom} {coordination.Chirurgien.Utilisateur?.Nom}";
            var nomAnesthesiste = $"{coordination.Anesthesiste.Utilisateur?.Prenom} {coordination.Anesthesiste.Utilisateur?.Nom}";
            var nomPatient = $"{coordination.Programmation.Patient.Utilisateur?.Prenom} {coordination.Programmation.Patient.Utilisateur?.Nom}";
            var dateInterventionStr = datefinale.ToString("dd/MM/yyyy");
            var rdvConsultationDate = rdvConsultation?.DateHeure.ToString("dd/MM/yyyy à HH:mm");

            _ = Task.Run(async () =>
            {
                try
                {
                    // Email au chirurgien
                    var emailChirurgien = coordination.Chirurgien.Utilisateur?.Email;
                    if (!string.IsNullOrEmpty(emailChirurgien))
                    {
                        await _emailService.SendCoordinationValideeAsync(
                            emailChirurgien, nomChirurgien, nomAnesthesiste,
                            nomPatient, dateInterventionStr, heureFinale);
                    }

                    // Email à l'anesthésiste
                    var emailAnesthesiste = coordination.Anesthesiste.Utilisateur?.Email;
                    if (!string.IsNullOrEmpty(emailAnesthesiste))
                    {
                        await _emailService.SendInterventionConfirmeeAnesthesisteAsync(
                            emailAnesthesiste, nomAnesthesiste, nomChirurgien,
                            nomPatient, dateInterventionStr, heureFinale, rdvConsultationDate);
                    }

                    // Email au patient
                    var emailPatient = coordination.Programmation.Patient.Utilisateur?.Email;
                    if (!string.IsNullOrEmpty(emailPatient))
                    {
                        await _emailService.SendInterventionPlanifieePatientAsync(
                            emailPatient, nomPatient, nomChirurgien, nomAnesthesiste,
                            dateInterventionStr, heureFinale, rdvConsultationDate);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Échec envoi email coordination validée");
                }
            });

            await transaction.CommitAsync();

            return new CoordinationActionResponse
            {
                Success = true,
                Message = "Coordination validée avec succès",
                IdCoordination = coordination.IdCoordination,
                NouveauStatut = "validee",
                IdRdvConsultationAnesthesiste = rdvConsultation?.IdRendezVous,
                DateRdvConsultation = rdvConsultation?.DateHeure
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erreur lors de la validation de coordination");
            return new CoordinationActionResponse { Success = false, Message = "Erreur lors de la validation" };
        }
    }

    /// <summary>
    /// Modifier/contre-proposer une coordination (par l'anesthésiste)
    /// </summary>
    public async Task<CoordinationActionResponse> ModifierCoordinationAsync(
        ModifierCoordinationRequest request, int idAnesthesiste)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var coordination = await _context.Set<CoordinationIntervention>()
                .Include(c => c.Programmation).ThenInclude(p => p.Patient).ThenInclude(p => p.Utilisateur)
                .Include(c => c.Chirurgien).ThenInclude(c => c.Utilisateur)
                .Include(c => c.Anesthesiste).ThenInclude(a => a.Utilisateur)
                .FirstOrDefaultAsync(c => c.IdCoordination == request.IdCoordination);

            if (coordination == null)
                return new CoordinationActionResponse { Success = false, Message = "Coordination non trouvée" };

            if (coordination.IdAnesthesiste != idAnesthesiste)
                return new CoordinationActionResponse { Success = false, Message = "Vous n'êtes pas l'anesthésiste de cette coordination" };

            if (coordination.Statut != "proposee")
                return new CoordinationActionResponse { Success = false, Message = $"Impossible de modifier une coordination en statut '{coordination.Statut}'" };

            // Mettre à jour la coordination
            coordination.Statut = "modifiee";
            coordination.DateContreProposee = request.DateContreProposee;
            coordination.HeureContreProposee = request.HeureContreProposee;
            coordination.CommentaireAnesthesiste = request.CommentaireAnesthesiste;
            coordination.DateReponse = DateTime.UtcNow;
            coordination.NbModifications++;
            coordination.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Ajouter à l'historique
            await AddHistoriqueAsync(coordination.IdCoordination, "modification", idAnesthesiste, "anesthesiste",
                $"Contre-proposition: {request.DateContreProposee:dd/MM/yyyy} à {request.HeureContreProposee}. {request.CommentaireAnesthesiste}",
                request.DateContreProposee, request.HeureContreProposee);

            // Notifier le chirurgien
            await SendNotificationAsync(
                coordination.IdChirurgien,
                "Contre-proposition reçue",
                $"Dr. {coordination.Anesthesiste.Utilisateur?.Prenom} {coordination.Anesthesiste.Utilisateur?.Nom} propose une modification: {request.DateContreProposee:dd/MM/yyyy} à {request.HeureContreProposee}",
                "coordination_modifiee",
                coordination.IdCoordination);

            // Envoyer email au chirurgien (non bloquant)
            var nomChirurgien = $"{coordination.Chirurgien.Utilisateur?.Prenom} {coordination.Chirurgien.Utilisateur?.Nom}";
            var nomAnesthesiste = $"{coordination.Anesthesiste.Utilisateur?.Prenom} {coordination.Anesthesiste.Utilisateur?.Nom}";
            var nomPatient = $"{coordination.Programmation.Patient.Utilisateur?.Prenom} {coordination.Programmation.Patient.Utilisateur?.Nom}";

            _ = Task.Run(async () =>
            {
                try
                {
                    var emailChirurgien = coordination.Chirurgien.Utilisateur?.Email;
                    if (!string.IsNullOrEmpty(emailChirurgien))
                    {
                        await _emailService.SendCoordinationModifieeAsync(
                            emailChirurgien, nomChirurgien, nomAnesthesiste, nomPatient,
                            request.DateContreProposee.ToString("dd/MM/yyyy"),
                            request.HeureContreProposee,
                            request.CommentaireAnesthesiste ?? "");
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Échec envoi email coordination modifiée");
                }
            });

            await transaction.CommitAsync();

            return new CoordinationActionResponse
            {
                Success = true,
                Message = "Contre-proposition envoyée au chirurgien",
                IdCoordination = coordination.IdCoordination,
                NouveauStatut = "modifiee"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erreur lors de la modification de coordination");
            return new CoordinationActionResponse { Success = false, Message = "Erreur lors de la modification" };
        }
    }

    /// <summary>
    /// Refuser une coordination (par l'anesthésiste)
    /// </summary>
    public async Task<CoordinationActionResponse> RefuserCoordinationAsync(
        RefuserCoordinationRequest request, int idAnesthesiste)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var coordination = await _context.Set<CoordinationIntervention>()
                .Include(c => c.Programmation)
                .Include(c => c.Chirurgien).ThenInclude(c => c.Utilisateur)
                .Include(c => c.Anesthesiste).ThenInclude(a => a.Utilisateur)
                .FirstOrDefaultAsync(c => c.IdCoordination == request.IdCoordination);

            if (coordination == null)
                return new CoordinationActionResponse { Success = false, Message = "Coordination non trouvée" };

            if (coordination.IdAnesthesiste != idAnesthesiste)
                return new CoordinationActionResponse { Success = false, Message = "Vous n'êtes pas l'anesthésiste de cette coordination" };

            if (coordination.Statut != "proposee")
                return new CoordinationActionResponse { Success = false, Message = $"Impossible de refuser une coordination en statut '{coordination.Statut}'" };

            // Mettre à jour la coordination
            coordination.Statut = "refusee";
            coordination.MotifRefus = request.MotifRefus;
            coordination.DateReponse = DateTime.UtcNow;
            coordination.UpdatedAt = DateTime.UtcNow;

            // Remettre la programmation en attente de coordination
            coordination.Programmation.Statut = "en_attente_coordination";
            coordination.Programmation.IdAnesthesiste = null;
            coordination.Programmation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Ajouter à l'historique
            await AddHistoriqueAsync(coordination.IdCoordination, "refus", idAnesthesiste, "anesthesiste",
                $"Refus: {request.MotifRefus}", null, null);

            // Notifier le chirurgien
            await SendNotificationAsync(
                coordination.IdChirurgien,
                "Coordination refusée",
                $"Dr. {coordination.Anesthesiste.Utilisateur?.Prenom} {coordination.Anesthesiste.Utilisateur?.Nom} a refusé la coordination. Motif: {request.MotifRefus}",
                "coordination_refusee",
                coordination.IdCoordination);

            // Envoyer email au chirurgien (non bloquant)
            var nomChirurgien = $"{coordination.Chirurgien.Utilisateur?.Prenom} {coordination.Chirurgien.Utilisateur?.Nom}";
            var nomAnesthesiste = $"{coordination.Anesthesiste.Utilisateur?.Prenom} {coordination.Anesthesiste.Utilisateur?.Nom}";
            var nomPatient = coordination.Programmation.Patient?.Utilisateur != null 
                ? $"{coordination.Programmation.Patient.Utilisateur.Prenom} {coordination.Programmation.Patient.Utilisateur.Nom}"
                : "Patient";

            _ = Task.Run(async () =>
            {
                try
                {
                    var emailChirurgien = coordination.Chirurgien.Utilisateur?.Email;
                    if (!string.IsNullOrEmpty(emailChirurgien))
                    {
                        await _emailService.SendCoordinationRefuseeAsync(
                            emailChirurgien, nomChirurgien, nomAnesthesiste,
                            nomPatient, request.MotifRefus);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Échec envoi email coordination refusée");
                }
            });

            await transaction.CommitAsync();

            return new CoordinationActionResponse
            {
                Success = true,
                Message = "Coordination refusée",
                IdCoordination = coordination.IdCoordination,
                NouveauStatut = "refusee"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erreur lors du refus de coordination");
            return new CoordinationActionResponse { Success = false, Message = "Erreur lors du refus" };
        }
    }

    /// <summary>
    /// Accepter une contre-proposition (par le chirurgien)
    /// </summary>
    public async Task<CoordinationActionResponse> AccepterContrePropositionAsync(
        AccepterContrePropositionRequest request, int idChirurgien)
    {
        var coordination = await _context.Set<CoordinationIntervention>()
            .FirstOrDefaultAsync(c => c.IdCoordination == request.IdCoordination);

        if (coordination == null)
            return new CoordinationActionResponse { Success = false, Message = "Coordination non trouvée" };

        if (coordination.IdChirurgien != idChirurgien)
            return new CoordinationActionResponse { Success = false, Message = "Vous n'êtes pas le chirurgien de cette coordination" };

        if (coordination.Statut != "modifiee")
            return new CoordinationActionResponse { Success = false, Message = "Aucune contre-proposition à accepter" };

        // Accepter = valider avec la date contre-proposée
        // On appelle ValiderCoordinationAsync avec l'ID de l'anesthésiste
        // Mais d'abord on met à jour les notes du chirurgien
        coordination.NotesChirurgien = request.NotesChirurgien;
        coordination.DateProposee = coordination.DateContreProposee!.Value;
        coordination.HeureProposee = coordination.HeureContreProposee!;
        coordination.Statut = "proposee"; // Remettre en proposée pour que la validation fonctionne
        await _context.SaveChangesAsync();

        // Ajouter à l'historique
        await AddHistoriqueAsync(coordination.IdCoordination, "acceptation_contre_proposition", idChirurgien, "chirurgien",
            "Contre-proposition acceptée", coordination.DateProposee, coordination.HeureProposee);

        // Valider automatiquement
        return await ValiderCoordinationAsync(
            new ValiderCoordinationRequest { IdCoordination = request.IdCoordination },
            coordination.IdAnesthesiste);
    }

    /// <summary>
    /// Refuser une contre-proposition (par le chirurgien)
    /// Permet de relancer avec un autre anesthésiste si demandé
    /// </summary>
    public async Task<CoordinationActionResponse> RefuserContrePropositionAsync(
        RefuserContrePropositionRequest request, int idChirurgien)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var coordination = await _context.Set<CoordinationIntervention>()
                .Include(c => c.Programmation).ThenInclude(p => p.Patient).ThenInclude(p => p.Utilisateur)
                .Include(c => c.Chirurgien).ThenInclude(c => c.Utilisateur)
                .Include(c => c.Anesthesiste).ThenInclude(a => a.Utilisateur)
                .FirstOrDefaultAsync(c => c.IdCoordination == request.IdCoordination);

            if (coordination == null)
                return new CoordinationActionResponse { Success = false, Message = "Coordination non trouvée" };

            if (coordination.IdChirurgien != idChirurgien)
                return new CoordinationActionResponse { Success = false, Message = "Vous n'êtes pas le chirurgien de cette coordination" };

            if (coordination.Statut != "modifiee")
                return new CoordinationActionResponse { Success = false, Message = "Aucune contre-proposition à refuser" };

            // Mettre à jour la coordination
            coordination.Statut = "contre_proposition_refusee";
            coordination.MotifRefus = request.MotifRefus;
            coordination.DateReponse = DateTime.UtcNow;
            coordination.UpdatedAt = DateTime.UtcNow;

            // Remettre la programmation en attente de coordination si relance demandée
            if (request.RelancerAvecAutre)
            {
                coordination.Programmation.Statut = "en_attente_coordination";
                coordination.Programmation.IdAnesthesiste = null;
                coordination.Programmation.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Ajouter à l'historique
            var details = request.RelancerAvecAutre 
                ? $"Contre-proposition refusée. Motif: {request.MotifRefus}. Relance avec autre anesthésiste."
                : $"Contre-proposition refusée. Motif: {request.MotifRefus}";
            await AddHistoriqueAsync(coordination.IdCoordination, "refus_contre_proposition", idChirurgien, "chirurgien",
                details, null, null);

            // Notifier l'anesthésiste
            await SendNotificationAsync(
                coordination.IdAnesthesiste,
                "Contre-proposition refusée",
                $"Dr. {coordination.Chirurgien.Utilisateur?.Prenom} {coordination.Chirurgien.Utilisateur?.Nom} a refusé votre contre-proposition. Motif: {request.MotifRefus}",
                "contre_proposition_refusee",
                coordination.IdCoordination);

            // Envoyer email à l'anesthésiste (non bloquant)
            var nomChirurgien = $"{coordination.Chirurgien.Utilisateur?.Prenom} {coordination.Chirurgien.Utilisateur?.Nom}";
            var nomAnesthesiste = $"{coordination.Anesthesiste.Utilisateur?.Prenom} {coordination.Anesthesiste.Utilisateur?.Nom}";
            var nomPatient = $"{coordination.Programmation.Patient.Utilisateur?.Prenom} {coordination.Programmation.Patient.Utilisateur?.Nom}";

            _ = Task.Run(async () =>
            {
                try
                {
                    var emailAnesthesiste = coordination.Anesthesiste.Utilisateur?.Email;
                    if (!string.IsNullOrEmpty(emailAnesthesiste))
                    {
                        await _emailService.SendCoordinationRefuseeAsync(
                            emailAnesthesiste, nomAnesthesiste, nomChirurgien,
                            nomPatient, request.MotifRefus);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Échec envoi email contre-proposition refusée");
                }
            });

            await transaction.CommitAsync();

            return new CoordinationActionResponse
            {
                Success = true,
                Message = request.RelancerAvecAutre 
                    ? "Contre-proposition refusée. Vous pouvez maintenant sélectionner un autre anesthésiste."
                    : "Contre-proposition refusée",
                IdCoordination = coordination.IdCoordination,
                NouveauStatut = "contre_proposition_refusee"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erreur lors du refus de contre-proposition");
            return new CoordinationActionResponse { Success = false, Message = "Erreur lors du refus" };
        }
    }

    /// <summary>
    /// Annuler une coordination
    /// </summary>
    public async Task<CoordinationActionResponse> AnnulerCoordinationAsync(
        AnnulerCoordinationRequest request, int idUser)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var coordination = await _context.Set<CoordinationIntervention>()
                .Include(c => c.Programmation)
                .Include(c => c.Chirurgien).ThenInclude(c => c.Utilisateur)
                .Include(c => c.Anesthesiste).ThenInclude(a => a.Utilisateur)
                .FirstOrDefaultAsync(c => c.IdCoordination == request.IdCoordination);

            if (coordination == null)
                return new CoordinationActionResponse { Success = false, Message = "Coordination non trouvée" };

            // Seuls le chirurgien ou l'anesthésiste peuvent annuler
            if (coordination.IdChirurgien != idUser && coordination.IdAnesthesiste != idUser)
                return new CoordinationActionResponse { Success = false, Message = "Vous n'êtes pas autorisé à annuler cette coordination" };

            var role = coordination.IdChirurgien == idUser ? "chirurgien" : "anesthesiste";

            // Supprimer les indisponibilités si elles existent
            if (coordination.IdIndisponibiliteChirurgien.HasValue)
            {
                var indispoChir = await _context.IndisponibilitesMedecin
                    .FindAsync(coordination.IdIndisponibiliteChirurgien.Value);
                if (indispoChir != null)
                    _context.IndisponibilitesMedecin.Remove(indispoChir);
            }

            if (coordination.IdIndisponibiliteAnesthesiste.HasValue)
            {
                var indispoAnesth = await _context.IndisponibilitesMedecin
                    .FindAsync(coordination.IdIndisponibiliteAnesthesiste.Value);
                if (indispoAnesth != null)
                    _context.IndisponibilitesMedecin.Remove(indispoAnesth);
            }

            // Annuler le RDV de consultation si existant
            if (coordination.IdRdvConsultationAnesthesiste.HasValue)
            {
                var rdv = await _context.RendezVous.FindAsync(coordination.IdRdvConsultationAnesthesiste.Value);
                if (rdv != null)
                    rdv.Statut = "annulé";
            }

            // Mettre à jour la coordination
            coordination.Statut = "annulee";
            coordination.UpdatedAt = DateTime.UtcNow;

            // Remettre la programmation en attente
            coordination.Programmation.Statut = "en_attente_coordination";
            coordination.Programmation.IdAnesthesiste = null;
            coordination.Programmation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Ajouter à l'historique
            await AddHistoriqueAsync(coordination.IdCoordination, "annulation", idUser, role,
                $"Annulation: {request.MotifAnnulation}", null, null);

            // Notifier l'autre partie
            var idAutre = role == "chirurgien" ? coordination.IdAnesthesiste : coordination.IdChirurgien;
            var nomAnnuleur = role == "chirurgien" 
                ? $"Dr. {coordination.Chirurgien.Utilisateur?.Prenom} {coordination.Chirurgien.Utilisateur?.Nom}"
                : $"Dr. {coordination.Anesthesiste.Utilisateur?.Prenom} {coordination.Anesthesiste.Utilisateur?.Nom}";

            await SendNotificationAsync(
                idAutre,
                "Coordination annulée",
                $"{nomAnnuleur} a annulé la coordination. Motif: {request.MotifAnnulation}",
                "coordination_annulee",
                coordination.IdCoordination);

            await transaction.CommitAsync();

            return new CoordinationActionResponse
            {
                Success = true,
                Message = "Coordination annulée",
                IdCoordination = coordination.IdCoordination,
                NouveauStatut = "annulee"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erreur lors de l'annulation de coordination");
            return new CoordinationActionResponse { Success = false, Message = "Erreur lors de l'annulation" };
        }
    }

    /// <summary>
    /// Récupérer une coordination par ID
    /// </summary>
    public async Task<CoordinationInterventionDto?> GetCoordinationAsync(int idCoordination)
    {
        var coordination = await _context.Set<CoordinationIntervention>()
            .Include(c => c.Programmation).ThenInclude(p => p.Patient).ThenInclude(p => p.Utilisateur)
            .Include(c => c.Chirurgien).ThenInclude(c => c.Utilisateur)
            .Include(c => c.Chirurgien).ThenInclude(c => c.Specialite)
            .Include(c => c.Anesthesiste).ThenInclude(a => a.Utilisateur)
            .Include(c => c.RdvConsultationAnesthesiste)
            .FirstOrDefaultAsync(c => c.IdCoordination == idCoordination);

        return coordination == null ? null : MapToDto(coordination);
    }

    /// <summary>
    /// Récupérer les coordinations d'un chirurgien
    /// </summary>
    public async Task<List<CoordinationInterventionDto>> GetCoordinationsChirurgienAsync(
        int idChirurgien, CoordinationFilterDto? filter = null)
    {
        var query = _context.Set<CoordinationIntervention>()
            .Include(c => c.Programmation).ThenInclude(p => p.Patient).ThenInclude(p => p.Utilisateur)
            .Include(c => c.Chirurgien).ThenInclude(c => c.Utilisateur)
            .Include(c => c.Chirurgien).ThenInclude(c => c.Specialite)
            .Include(c => c.Anesthesiste).ThenInclude(a => a.Utilisateur)
            .Where(c => c.IdChirurgien == idChirurgien);

        query = ApplyFilters(query, filter);

        var coordinations = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return coordinations.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Récupérer les coordinations d'un anesthésiste
    /// </summary>
    public async Task<List<CoordinationInterventionDto>> GetCoordinationsAnesthesisteAsync(
        int idAnesthesiste, CoordinationFilterDto? filter = null)
    {
        var query = _context.Set<CoordinationIntervention>()
            .Include(c => c.Programmation).ThenInclude(p => p.Patient).ThenInclude(p => p.Utilisateur)
            .Include(c => c.Chirurgien).ThenInclude(c => c.Utilisateur)
            .Include(c => c.Chirurgien).ThenInclude(c => c.Specialite)
            .Include(c => c.Anesthesiste).ThenInclude(a => a.Utilisateur)
            .Where(c => c.IdAnesthesiste == idAnesthesiste);

        query = ApplyFilters(query, filter);

        var coordinations = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return coordinations.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Récupérer l'historique d'une coordination
    /// </summary>
    public async Task<List<CoordinationHistoriqueDto>> GetHistoriqueCoordinationAsync(int idCoordination)
    {
        var historique = await _context.Set<CoordinationInterventionHistorique>()
            .Include(h => h.UserAction)
            .Where(h => h.IdCoordination == idCoordination)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        return historique.Select(h => new CoordinationHistoriqueDto
        {
            IdHistorique = h.IdHistorique,
            TypeAction = h.TypeAction,
            NomUser = $"{h.UserAction.Prenom} {h.UserAction.Nom}",
            RoleUser = h.RoleUser,
            Details = h.Details,
            DateProposee = h.DateProposee,
            HeureProposee = h.HeureProposee,
            CreatedAt = h.CreatedAt
        }).ToList();
    }

    /// <summary>
    /// Statistiques pour un anesthésiste
    /// </summary>
    public async Task<CoordinationStatsDto> GetStatsAnesthesisteAsync(int idAnesthesiste)
    {
        var coordinations = await _context.Set<CoordinationIntervention>()
            .Where(c => c.IdAnesthesiste == idAnesthesiste)
            .ToListAsync();

        return new CoordinationStatsDto
        {
            EnAttente = coordinations.Count(c => c.Statut == "proposee"),
            Validees = coordinations.Count(c => c.Statut == "validee"),
            Modifiees = coordinations.Count(c => c.Statut == "modifiee"),
            Refusees = coordinations.Count(c => c.Statut == "refusee"),
            Total = coordinations.Count
        };
    }

    // ==================== Méthodes privées ====================

    private async Task<DateTime?> TrouverCreneauConsultationAsync(
        int idAnesthesiste, DateTime dateDebut, DateTime dateFin)
    {
        var creneaux = await GetCreneauxAnesthesisteAsync(idAnesthesiste, dateDebut, dateFin);
        
        // Chercher un créneau disponible d'au moins 30 minutes
        var creneauDispo = creneaux
            .Where(c => c.EstDisponible && c.DureeMinutes >= 30)
            .OrderBy(c => c.Date)
            .ThenBy(c => c.HeureDebut)
            .FirstOrDefault();

        if (creneauDispo == null)
            return null;

        return creneauDispo.Date.Add(TimeSpan.Parse(creneauDispo.HeureDebut, System.Globalization.CultureInfo.InvariantCulture));
    }

    private async Task AddHistoriqueAsync(int idCoordination, string typeAction, int idUser, 
        string roleUser, string? details, DateTime? dateProposee, string? heureProposee)
    {
        var historique = new CoordinationInterventionHistorique
        {
            IdCoordination = idCoordination,
            TypeAction = typeAction,
            IdUserAction = idUser,
            RoleUser = roleUser,
            Details = details,
            DateProposee = dateProposee,
            HeureProposee = heureProposee,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<CoordinationInterventionHistorique>().Add(historique);
        await _context.SaveChangesAsync();
    }

    private async Task SendNotificationAsync(int idUser, string titre, string message, 
        string type, int? referenceId)
    {
        var notification = new Notification
        {
            IdUser = idUser,
            Titre = titre,
            Message = message,
            Type = type,
            Metadata = referenceId?.ToString(),
            Lu = false,
            DateCreation = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Envoyer via SignalR
        await _notificationHub.Clients.User(idUser.ToString())
            .SendAsync("ReceiveNotification", new
            {
                notification.IdNotification,
                notification.Titre,
                notification.Message,
                notification.Type,
                ReferenceId = referenceId,
                CreatedAt = notification.DateCreation
            });
    }

    private IQueryable<CoordinationIntervention> ApplyFilters(
        IQueryable<CoordinationIntervention> query, CoordinationFilterDto? filter)
    {
        if (filter == null) return query;

        if (!string.IsNullOrEmpty(filter.Statut))
            query = query.Where(c => c.Statut == filter.Statut);

        if (filter.DateDebut.HasValue)
            query = query.Where(c => c.DateProposee >= filter.DateDebut.Value);

        if (filter.DateFin.HasValue)
            query = query.Where(c => c.DateProposee <= filter.DateFin.Value);

        if (filter.IdPatient.HasValue)
            query = query.Where(c => c.Programmation.IdPatient == filter.IdPatient.Value);

        return query;
    }

    private CoordinationInterventionDto MapToDto(CoordinationIntervention c)
    {
        return new CoordinationInterventionDto
        {
            IdCoordination = c.IdCoordination,
            IdProgrammation = c.IdProgrammation,
            IdChirurgien = c.IdChirurgien,
            NomChirurgien = $"Dr. {c.Chirurgien.Utilisateur?.Prenom} {c.Chirurgien.Utilisateur?.Nom}",
            SpecialiteChirurgien = c.Chirurgien.Specialite?.NomSpecialite ?? "",
            IdAnesthesiste = c.IdAnesthesiste,
            NomAnesthesiste = $"Dr. {c.Anesthesiste.Utilisateur?.Prenom} {c.Anesthesiste.Utilisateur?.Nom}",
            IdPatient = c.Programmation.IdPatient,
            NomPatient = $"{c.Programmation.Patient?.Utilisateur?.Prenom} {c.Programmation.Patient?.Utilisateur?.Nom}",
            IndicationOperatoire = c.Programmation.IndicationOperatoire ?? "",
            TypeIntervention = c.Programmation.TypeIntervention,
            DateProposee = c.DateProposee,
            HeureProposee = c.HeureProposee,
            DureeEstimee = c.DureeEstimee,
            Statut = c.Statut,
            DateContreProposee = c.DateContreProposee,
            HeureContreProposee = c.HeureContreProposee,
            CommentaireAnesthesiste = c.CommentaireAnesthesiste,
            MotifRefus = c.MotifRefus,
            NotesChirurgien = c.NotesChirurgien,
            NotesAnesthesie = c.Programmation.NotesAnesthesie,
            ClassificationAsa = c.Programmation.ClassificationAsa,
            RisqueOperatoire = c.Programmation.RisqueOperatoire,
            IdRdvConsultationAnesthesiste = c.IdRdvConsultationAnesthesiste,
            DateRdvConsultation = c.RdvConsultationAnesthesiste?.DateHeure,
            DateValidation = c.DateValidation,
            DateReponse = c.DateReponse,
            NbModifications = c.NbModifications,
            CreatedAt = c.CreatedAt
        };
    }
}
