using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs;

namespace Mediconnet_Backend.Services
{
    // DTOs pour la vérification de disponibilité avec gestion des conflits
    public class VerificationDisponibiliteResult
    {
        public bool Disponible { get; set; } = true;
        public string? Message { get; set; }
        public bool HasInterventionConflict { get; set; } = false;
        public bool HasRdvConflicts { get; set; } = false;
        public List<RdvConflitInfo> RdvsEnConflit { get; set; } = new();
    }

    public class RdvConflitInfo
    {
        public int IdRendezVous { get; set; }
        public DateTime DateHeure { get; set; }
        public int Duree { get; set; }
        public string PatientNom { get; set; } = "";
        public string PatientPrenom { get; set; } = "";
        public string? PatientEmail { get; set; }
        public string? Motif { get; set; }
    }

    public interface IBlocOperatoireService
    {
        // Gestion des blocs
        Task<List<BlocOperatoireListDto>> GetAllBlocsAsync();
        Task<BlocOperatoireDto?> GetBlocByIdAsync(int idBloc);
        Task<BlocOperatoireDto> CreateBlocAsync(CreateBlocOperatoireRequest request);
        Task<BlocOperatoireDto?> UpdateBlocAsync(int idBloc, UpdateBlocOperatoireRequest request);
        Task<bool> DeleteBlocAsync(int idBloc);

        // Gestion des réservations
        Task<List<ReservationBlocListDto>> GetReservationsByBlocAsync(int idBloc, DateTime? dateDebut = null, DateTime? dateFin = null);
        Task<List<ReservationBlocListDto>> GetReservationsByDateAsync(DateTime date);
        Task<ReservationBlocDto?> GetReservationByIdAsync(int idReservation);
        Task<(bool Success, string Message, ReservationBlocDto? Reservation)> CreateReservationAsync(CreateReservationBlocRequest request, int idMedecin);
        Task<(bool Success, string Message, ReservationBlocDto? Reservation)> UpdateReservationAsync(int idReservation, UpdateReservationBlocRequest request);
        Task<bool> CancelReservationAsync(int idReservation);

        // Disponibilité
        Task<List<DisponibiliteBlocDto>> GetDisponibilitesAsync(DateTime date, string heureDebut, int dureeMinutes);
        Task<bool> VerifierDisponibiliteAsync(int idBloc, DateTime date, string heureDebut, int dureeMinutes, int? excludeReservationId = null);
        Task<VerificationDisponibiliteResult> VerifierDisponibiliteChirurgienAsync(int medecinId, DateTime date, string heureDebut, int dureeMinutes);
        Task<bool> AnnulerRdvsEnConflitAsync(int medecinId, DateTime date, string heureDebut, int dureeMinutes, string patientIntervention, string nomChirurgien);

        // Agenda
        Task<AgendaBlocDto> GetAgendaBlocAsync(int idBloc, DateTime date);
        Task<List<AgendaBlocDto>> GetAgendaTousBlocsAsync(DateTime date);

        // Mise à jour automatique des statuts
        Task UpdateBlocStatusesAsync();
    }

    public class BlocOperatoireService : IBlocOperatoireService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BlocOperatoireService> _logger;
        private readonly IEmailService _emailService;

        public BlocOperatoireService(
            ApplicationDbContext context, 
            ILogger<BlocOperatoireService> logger,
            IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        #region Gestion des Blocs

        public async Task<List<BlocOperatoireListDto>> GetAllBlocsAsync()
        {
            var today = DateTime.Today;

            var blocs = await _context.BlocsOperatoires
                .Select(b => new BlocOperatoireListDto
                {
                    IdBloc = b.IdBloc,
                    Nom = b.Nom,
                    Description = b.Description,
                    Statut = b.Statut,
                    Actif = b.Actif,
                    Localisation = b.Localisation,
                    NombreReservationsAujourdhui = b.Reservations.Count(r => 
                        r.DateReservation.Date == today && 
                        r.Statut != "annulee")
                })
                .OrderBy(b => b.Nom)
                .ToListAsync();

            return blocs;
        }

        public async Task<BlocOperatoireDto?> GetBlocByIdAsync(int idBloc)
        {
            var today = DateTime.Today;
            var now = DateTime.Now;
            var currentTime = now.ToString("HH:mm");

            var bloc = await _context.BlocsOperatoires
                .Include(b => b.Reservations.Where(r => r.DateReservation.Date == today && r.Statut != "annulee"))
                    .ThenInclude(r => r.Medecin)
                        .ThenInclude(m => m!.Utilisateur)
                .Include(b => b.Reservations.Where(r => r.DateReservation.Date == today && r.Statut != "annulee"))
                    .ThenInclude(r => r.Programmation)
                        .ThenInclude(p => p!.Patient)
                            .ThenInclude(pat => pat!.Utilisateur)
                .FirstOrDefaultAsync(b => b.IdBloc == idBloc);

            if (bloc == null) return null;

            // Trouver la réservation en cours
            var reservationEnCours = bloc.Reservations
                .FirstOrDefault(r => 
                    r.DateReservation.Date == today &&
                    string.Compare(r.HeureDebut, currentTime) <= 0 &&
                    string.Compare(r.HeureFin, currentTime) > 0 &&
                    (r.Statut == "confirmee" || r.Statut == "en_cours"));

            return new BlocOperatoireDto
            {
                IdBloc = bloc.IdBloc,
                Nom = bloc.Nom,
                Description = bloc.Description,
                Statut = bloc.Statut,
                Actif = bloc.Actif,
                Localisation = bloc.Localisation,
                Capacite = bloc.Capacite,
                Equipements = bloc.Equipements,
                CreatedAt = bloc.CreatedAt,
                UpdatedAt = bloc.UpdatedAt,
                NombreReservationsAujourdhui = bloc.Reservations.Count,
                ReservationEnCours = reservationEnCours != null ? MapToReservationDto(reservationEnCours) : null
            };
        }

        public async Task<BlocOperatoireDto> CreateBlocAsync(CreateBlocOperatoireRequest request)
        {
            var bloc = new BlocOperatoire
            {
                Nom = request.Nom,
                Description = request.Description,
                Localisation = request.Localisation,
                Capacite = request.Capacite,
                Equipements = request.Equipements,
                Statut = "libre",
                Actif = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.BlocsOperatoires.Add(bloc);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bloc opératoire créé: {Nom} (ID: {Id})", bloc.Nom, bloc.IdBloc);

            return new BlocOperatoireDto
            {
                IdBloc = bloc.IdBloc,
                Nom = bloc.Nom,
                Description = bloc.Description,
                Statut = bloc.Statut,
                Actif = bloc.Actif,
                Localisation = bloc.Localisation,
                Capacite = bloc.Capacite,
                Equipements = bloc.Equipements,
                CreatedAt = bloc.CreatedAt,
                NombreReservationsAujourdhui = 0
            };
        }

        public async Task<BlocOperatoireDto?> UpdateBlocAsync(int idBloc, UpdateBlocOperatoireRequest request)
        {
            var bloc = await _context.BlocsOperatoires.FindAsync(idBloc);
            if (bloc == null) return null;

            if (!string.IsNullOrEmpty(request.Nom)) bloc.Nom = request.Nom;
            if (request.Description != null) bloc.Description = request.Description;
            if (!string.IsNullOrEmpty(request.Statut)) bloc.Statut = request.Statut;
            if (request.Actif.HasValue) bloc.Actif = request.Actif.Value;
            if (request.Localisation != null) bloc.Localisation = request.Localisation;
            if (request.Capacite.HasValue) bloc.Capacite = request.Capacite.Value;
            if (request.Equipements != null) bloc.Equipements = request.Equipements;

            bloc.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Bloc opératoire mis à jour: {Nom} (ID: {Id})", bloc.Nom, bloc.IdBloc);

            return await GetBlocByIdAsync(idBloc);
        }

        public async Task<bool> DeleteBlocAsync(int idBloc)
        {
            var bloc = await _context.BlocsOperatoires
                .Include(b => b.Reservations)
                .FirstOrDefaultAsync(b => b.IdBloc == idBloc);

            if (bloc == null) return false;

            // Vérifier s'il y a des réservations futures
            var hasActiveReservations = bloc.Reservations.Any(r => 
                r.DateReservation >= DateTime.Today && 
                r.Statut != "annulee" && 
                r.Statut != "terminee");

            if (hasActiveReservations)
            {
                _logger.LogWarning("Impossible de supprimer le bloc {Nom}: réservations actives", bloc.Nom);
                return false;
            }

            _context.BlocsOperatoires.Remove(bloc);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bloc opératoire supprimé: {Nom} (ID: {Id})", bloc.Nom, bloc.IdBloc);

            return true;
        }

        #endregion

        #region Gestion des Réservations

        public async Task<List<ReservationBlocListDto>> GetReservationsByBlocAsync(int idBloc, DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            var query = _context.ReservationsBlocs
                .Include(r => r.Bloc)
                .Include(r => r.Medecin)
                    .ThenInclude(m => m!.Utilisateur)
                .Include(r => r.Programmation)
                    .ThenInclude(p => p!.Patient)
                        .ThenInclude(pat => pat!.Utilisateur)
                .Where(r => r.IdBloc == idBloc);

            if (dateDebut.HasValue)
                query = query.Where(r => r.DateReservation >= dateDebut.Value.Date);

            if (dateFin.HasValue)
                query = query.Where(r => r.DateReservation <= dateFin.Value.Date);

            var reservations = await query
                .OrderBy(r => r.DateReservation)
                .ThenBy(r => r.HeureDebut)
                .Select(r => new ReservationBlocListDto
                {
                    IdReservation = r.IdReservation,
                    IdBloc = r.IdBloc,
                    NomBloc = r.Bloc!.Nom,
                    MedecinNom = r.Medecin!.Utilisateur!.Nom + " " + r.Medecin.Utilisateur.Prenom,
                    PatientNom = r.Programmation!.Patient!.Utilisateur!.Nom + " " + r.Programmation.Patient.Utilisateur.Prenom,
                    TypeIntervention = r.Programmation.TypeIntervention,
                    DateReservation = r.DateReservation,
                    HeureDebut = r.HeureDebut,
                    HeureFin = r.HeureFin,
                    DureeMinutes = r.DureeMinutes,
                    Statut = r.Statut
                })
                .ToListAsync();

            return reservations;
        }

        public async Task<List<ReservationBlocListDto>> GetReservationsByDateAsync(DateTime date)
        {
            var reservations = await _context.ReservationsBlocs
                .Include(r => r.Bloc)
                .Include(r => r.Medecin)
                    .ThenInclude(m => m!.Utilisateur)
                .Include(r => r.Programmation)
                    .ThenInclude(p => p!.Patient)
                        .ThenInclude(pat => pat!.Utilisateur)
                .Where(r => r.DateReservation.Date == date.Date && r.Statut != "annulee")
                .OrderBy(r => r.Bloc!.Nom)
                .ThenBy(r => r.HeureDebut)
                .Select(r => new ReservationBlocListDto
                {
                    IdReservation = r.IdReservation,
                    IdBloc = r.IdBloc,
                    NomBloc = r.Bloc!.Nom,
                    MedecinNom = r.Medecin!.Utilisateur!.Nom + " " + r.Medecin.Utilisateur.Prenom,
                    PatientNom = r.Programmation!.Patient!.Utilisateur!.Nom + " " + r.Programmation.Patient.Utilisateur.Prenom,
                    TypeIntervention = r.Programmation.TypeIntervention,
                    DateReservation = r.DateReservation,
                    HeureDebut = r.HeureDebut,
                    HeureFin = r.HeureFin,
                    DureeMinutes = r.DureeMinutes,
                    Statut = r.Statut
                })
                .ToListAsync();

            return reservations;
        }

        public async Task<ReservationBlocDto?> GetReservationByIdAsync(int idReservation)
        {
            var reservation = await _context.ReservationsBlocs
                .Include(r => r.Bloc)
                .Include(r => r.Medecin)
                    .ThenInclude(m => m!.Utilisateur)
                .Include(r => r.Programmation)
                    .ThenInclude(p => p!.Patient)
                        .ThenInclude(pat => pat!.Utilisateur)
                .FirstOrDefaultAsync(r => r.IdReservation == idReservation);

            if (reservation == null) return null;

            return MapToReservationDto(reservation);
        }

        public async Task<(bool Success, string Message, ReservationBlocDto? Reservation)> CreateReservationAsync(
            CreateReservationBlocRequest request, int idMedecin)
        {
            // Vérifier que le bloc existe et est actif
            var bloc = await _context.BlocsOperatoires.FindAsync(request.IdBloc);
            if (bloc == null)
                return (false, "Bloc opératoire non trouvé", null);

            if (!bloc.Actif)
                return (false, "Ce bloc opératoire n'est pas actif", null);

            if (bloc.Statut == "maintenance")
                return (false, "Ce bloc opératoire est en maintenance", null);

            // Vérifier que la programmation existe
            var programmation = await _context.ProgrammationsInterventions
                .Include(p => p.Patient)
                    .ThenInclude(pat => pat!.Utilisateur)
                .FirstOrDefaultAsync(p => p.IdProgrammation == request.IdProgrammation);

            if (programmation == null)
                return (false, "Programmation d'intervention non trouvée", null);

            // Calculer l'heure de fin
            var heureFin = CalculerHeureFin(request.HeureDebut, request.DureeMinutes);

            // Vérifier la disponibilité
            var estDisponible = await VerifierDisponibiliteAsync(
                request.IdBloc, 
                request.DateReservation, 
                request.HeureDebut, 
                request.DureeMinutes);

            if (!estDisponible)
                return (false, "Le bloc n'est pas disponible sur ce créneau", null);

            // Créer la réservation
            var reservation = new ReservationBloc
            {
                IdBloc = request.IdBloc,
                IdProgrammation = request.IdProgrammation,
                IdMedecin = idMedecin,
                DateReservation = request.DateReservation.Date,
                HeureDebut = request.HeureDebut,
                HeureFin = heureFin,
                DureeMinutes = request.DureeMinutes,
                Statut = "confirmee",
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.ReservationsBlocs.Add(reservation);

            // Mettre à jour le statut du bloc si c'est aujourd'hui et dans le créneau actuel
            await UpdateBlocStatusIfNeeded(bloc, reservation);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Réservation bloc créée: Bloc {BlocNom}, Date {Date}, {HeureDebut}-{HeureFin}",
                bloc.Nom, request.DateReservation.ToString("yyyy-MM-dd"), request.HeureDebut, heureFin);

            // Recharger avec les relations
            var reservationDto = await GetReservationByIdAsync(reservation.IdReservation);
            return (true, "Réservation créée avec succès", reservationDto);
        }

        public async Task<(bool Success, string Message, ReservationBlocDto? Reservation)> UpdateReservationAsync(
            int idReservation, UpdateReservationBlocRequest request)
        {
            var reservation = await _context.ReservationsBlocs
                .Include(r => r.Bloc)
                .FirstOrDefaultAsync(r => r.IdReservation == idReservation);

            if (reservation == null)
                return (false, "Réservation non trouvée", null);

            if (reservation.Statut == "terminee" || reservation.Statut == "annulee")
                return (false, "Impossible de modifier une réservation terminée ou annulée", null);

            var newIdBloc = request.IdBloc ?? reservation.IdBloc;
            var newDate = request.DateReservation ?? reservation.DateReservation;
            var newHeureDebut = request.HeureDebut ?? reservation.HeureDebut;
            var newDuree = request.DureeMinutes ?? reservation.DureeMinutes;

            // Vérifier la disponibilité si changement de créneau
            if (request.IdBloc.HasValue || request.DateReservation.HasValue || 
                request.HeureDebut != null || request.DureeMinutes.HasValue)
            {
                var estDisponible = await VerifierDisponibiliteAsync(
                    newIdBloc, newDate, newHeureDebut, newDuree, idReservation);

                if (!estDisponible)
                    return (false, "Le nouveau créneau n'est pas disponible", null);
            }

            // Appliquer les modifications
            if (request.IdBloc.HasValue) reservation.IdBloc = request.IdBloc.Value;
            if (request.DateReservation.HasValue) reservation.DateReservation = request.DateReservation.Value.Date;
            if (!string.IsNullOrEmpty(request.HeureDebut)) reservation.HeureDebut = request.HeureDebut;
            if (request.DureeMinutes.HasValue)
            {
                reservation.DureeMinutes = request.DureeMinutes.Value;
                reservation.HeureFin = CalculerHeureFin(reservation.HeureDebut, reservation.DureeMinutes);
            }
            if (!string.IsNullOrEmpty(request.Statut)) reservation.Statut = request.Statut;
            if (request.Notes != null) reservation.Notes = request.Notes;

            reservation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await UpdateBlocStatusesAsync();

            var reservationDto = await GetReservationByIdAsync(idReservation);
            return (true, "Réservation mise à jour avec succès", reservationDto);
        }

        public async Task<bool> CancelReservationAsync(int idReservation)
        {
            var reservation = await _context.ReservationsBlocs
                .Include(r => r.Bloc)
                .FirstOrDefaultAsync(r => r.IdReservation == idReservation);

            if (reservation == null) return false;

            reservation.Statut = "annulee";
            reservation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await UpdateBlocStatusesAsync();

            _logger.LogInformation("Réservation bloc annulée: ID {Id}", idReservation);

            return true;
        }

        #endregion

        #region Disponibilité

        public async Task<List<DisponibiliteBlocDto>> GetDisponibilitesAsync(DateTime date, string heureDebut, int dureeMinutes)
        {
            var blocs = await _context.BlocsOperatoires
                .Where(b => b.Actif && b.Statut != "maintenance")
                .Include(b => b.Reservations.Where(r => r.DateReservation.Date == date.Date && r.Statut != "annulee"))
                .ToListAsync();

            var heureFin = CalculerHeureFin(heureDebut, dureeMinutes);
            var result = new List<DisponibiliteBlocDto>();

            foreach (var bloc in blocs)
            {
                var estDisponible = !HasConflict(bloc.Reservations.ToList(), heureDebut, heureFin);

                var dto = new DisponibiliteBlocDto
                {
                    IdBloc = bloc.IdBloc,
                    NomBloc = bloc.Nom,
                    EstDisponible = estDisponible,
                    CreneauxOccupes = bloc.Reservations
                        .OrderBy(r => r.HeureDebut)
                        .Select(r => $"{r.HeureDebut}-{r.HeureFin}")
                        .ToList()
                };

                result.Add(dto);
            }

            return result.OrderByDescending(d => d.EstDisponible).ThenBy(d => d.NomBloc).ToList();
        }

        public async Task<bool> VerifierDisponibiliteAsync(int idBloc, DateTime date, string heureDebut, int dureeMinutes, int? excludeReservationId = null)
        {
            var heureFin = CalculerHeureFin(heureDebut, dureeMinutes);

            var reservations = await _context.ReservationsBlocs
                .Where(r => r.IdBloc == idBloc && 
                           r.DateReservation.Date == date.Date && 
                           r.Statut != "annulee" &&
                           (excludeReservationId == null || r.IdReservation != excludeReservationId))
                .ToListAsync();

            return !HasConflict(reservations, heureDebut, heureFin);
        }

        /// <summary>
        /// Vérifie si le chirurgien est disponible sur le créneau pour une intervention
        /// Retourne les conflits détaillés : interventions (bloquantes) vs RDV (annulables)
        /// </summary>
        public async Task<VerificationDisponibiliteResult> VerifierDisponibiliteChirurgienAsync(
            int medecinId, DateTime date, string heureDebut, int dureeMinutes)
        {
            var result = new VerificationDisponibiliteResult { Disponible = true };
            
            var heureFin = CalculerHeureFin(heureDebut, dureeMinutes);
            
            // Parser les heures
            if (!TimeSpan.TryParse(heureDebut, out var heureDebutTs) || 
                !TimeSpan.TryParse(heureFin, out var heureFinTs))
            {
                result.Disponible = false;
                result.Message = "Format d'heure invalide";
                return result;
            }

            var dateDebut = date.Date.Add(heureDebutTs);
            var dateFin = date.Date.Add(heureFinTs);

            // 1. Vérifier les indisponibilités de type "intervention" (BLOQUANT)
            var indispoIntervention = await _context.IndisponibilitesMedecin
                .Where(i => i.IdMedecin == medecinId &&
                           i.Type == "intervention" &&
                           i.DateDebut < dateFin &&
                           i.DateFin > dateDebut)
                .FirstOrDefaultAsync();

            if (indispoIntervention != null)
            {
                result.Disponible = false;
                result.HasInterventionConflict = true;
                result.Message = "Une intervention est déjà programmée sur ce créneau. Veuillez choisir un autre horaire.";
                return result;
            }

            // 2. Vérifier les autres indisponibilités (congés, absences, etc.) - BLOQUANT
            var autreIndispo = await _context.IndisponibilitesMedecin
                .Where(i => i.IdMedecin == medecinId &&
                           i.Type != "intervention" &&
                           i.DateDebut < dateFin &&
                           i.DateFin > dateDebut)
                .FirstOrDefaultAsync();

            if (autreIndispo != null)
            {
                result.Disponible = false;
                result.Message = $"Une indisponibilité existe sur ce créneau ({autreIndispo.Motif ?? autreIndispo.Type ?? "Absence"})";
                return result;
            }

            // 3. Vérifier les RDV existants (NON BLOQUANT - peuvent être annulés)
            var rdvsEnConflit = await _context.RendezVous
                .Include(r => r.Patient)
                    .ThenInclude(p => p.Utilisateur)
                .Where(r => r.IdMedecin == medecinId &&
                           r.Statut != "annule" &&
                           r.DateHeure.Date == date.Date &&
                           r.DateHeure < dateFin &&
                           r.DateHeure.AddMinutes(r.Duree) > dateDebut)
                .ToListAsync();

            if (rdvsEnConflit.Any())
            {
                result.HasRdvConflicts = true;
                result.RdvsEnConflit = rdvsEnConflit.Select(r => new RdvConflitInfo
                {
                    IdRendezVous = r.IdRendezVous,
                    DateHeure = r.DateHeure,
                    Duree = r.Duree,
                    PatientNom = r.Patient?.Utilisateur?.Nom ?? "Inconnu",
                    PatientPrenom = r.Patient?.Utilisateur?.Prenom ?? "",
                    PatientEmail = r.Patient?.Utilisateur?.Email,
                    Motif = r.Motif
                }).ToList();
                
                var heuresRdv = string.Join(", ", rdvsEnConflit.Select(r => r.DateHeure.ToString("HH:mm")));
                result.Message = $"Un ou plusieurs rendez-vous existent sur ce créneau ({heuresRdv}). En poursuivant, ces rendez-vous seront annulés et devront être reprogrammés.";
            }

            return result;
        }

        /// <summary>
        /// Annule tous les RDV en conflit avec l'intervention et envoie les notifications
        /// </summary>
        public async Task<bool> AnnulerRdvsEnConflitAsync(
            int medecinId, DateTime date, string heureDebut, int dureeMinutes, 
            string patientIntervention, string nomChirurgien)
        {
            var heureFin = CalculerHeureFin(heureDebut, dureeMinutes);
            
            if (!TimeSpan.TryParse(heureDebut, out var heureDebutTs) || 
                !TimeSpan.TryParse(heureFin, out var heureFinTs))
                return false;

            var dateDebut = date.Date.Add(heureDebutTs);
            var dateFin = date.Date.Add(heureFinTs);

            // Récupérer tous les RDV en conflit
            var rdvsEnConflit = await _context.RendezVous
                .Include(r => r.Patient)
                    .ThenInclude(p => p.Utilisateur)
                .Include(r => r.Medecin)
                    .ThenInclude(m => m.Utilisateur)
                .Where(r => r.IdMedecin == medecinId &&
                           r.Statut != "annule" &&
                           r.DateHeure.Date == date.Date &&
                           r.DateHeure < dateFin &&
                           r.DateHeure.AddMinutes(r.Duree) > dateDebut)
                .ToListAsync();

            if (!rdvsEnConflit.Any())
                return true;

            var patientsAnnules = new List<string>();
            var emailMedecin = rdvsEnConflit.FirstOrDefault()?.Medecin?.Utilisateur?.Email;

            foreach (var rdv in rdvsEnConflit)
            {
                // Marquer le RDV comme annulé (ne pas supprimer pour garder l'historique)
                rdv.Statut = "annule";
                rdv.Notes = (rdv.Notes ?? "") + $"\n[Annulé automatiquement le {DateTime.Now:dd/MM/yyyy HH:mm}] - Intervention prioritaire programmée";

                var patientNom = $"{rdv.Patient?.Utilisateur?.Prenom} {rdv.Patient?.Utilisateur?.Nom}";
                patientsAnnules.Add(patientNom);

                // Envoyer email au patient
                var patientEmail = rdv.Patient?.Utilisateur?.Email;
                if (!string.IsNullOrEmpty(patientEmail))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var subject = "Annulation de votre rendez-vous - MediConnect";
                            var body = $@"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6;'>
<p>Bonjour {rdv.Patient?.Utilisateur?.Prenom} {rdv.Patient?.Utilisateur?.Nom},</p>

<p>Votre rendez-vous avec le <strong>Dr. {nomChirurgien}</strong> prévu pour le <strong>{rdv.DateHeure:dd/MM/yyyy}</strong> à <strong>{rdv.DateHeure:HH:mm}</strong> a été annulé en raison d'une intervention médicale prioritaire.</p>

<p>Nous vous invitons à reprendre rendez-vous via notre plateforme ou à attendre une reprogrammation de la part du médecin.</p>

<p>Nous nous excusons pour ce désagrément et vous remercions de votre compréhension.</p>

<p>Cordialement,<br/>
L'équipe MediConnect</p>
</body>
</html>";
                            await _emailService.SendEmailAsync(patientEmail, subject, body);
                            _logger.LogInformation("Email d'annulation envoyé au patient: {Email}", patientEmail);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erreur envoi email annulation au patient: {Email}", patientEmail);
                        }
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Envoyer email récapitulatif au médecin
            if (!string.IsNullOrEmpty(emailMedecin) && patientsAnnules.Any())
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var subject = "Rendez-vous annulés suite à intervention - MediConnect";
                        var listePatients = string.Join("<br/>- ", patientsAnnules);
                        var body = $@"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6;'>
<p>Bonjour Dr. {nomChirurgien},</p>

<p>Vos rendez-vous avec le(s) patient(s) suivant(s) ont été annulés suite à la programmation de l'intervention du patient <strong>{patientIntervention}</strong> :</p>

<p>- {listePatients}</p>

<p>Veuillez procéder à leur reprogrammation dans les meilleurs délais.</p>

<p>Cordialement,<br/>
L'équipe MediConnect</p>
</body>
</html>";
                        await _emailService.SendEmailAsync(emailMedecin, subject, body);
                        _logger.LogInformation("Email récapitulatif envoyé au médecin: {Email}", emailMedecin);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur envoi email récapitulatif au médecin: {Email}", emailMedecin);
                    }
                });
            }

            _logger.LogInformation("Annulation de {Count} RDV en conflit pour intervention du patient {Patient}", 
                rdvsEnConflit.Count, patientIntervention);

            return true;
        }

        #endregion

        #region Agenda

        public async Task<AgendaBlocDto> GetAgendaBlocAsync(int idBloc, DateTime date)
        {
            var bloc = await _context.BlocsOperatoires
                .Include(b => b.Reservations.Where(r => r.DateReservation.Date == date.Date && r.Statut != "annulee"))
                    .ThenInclude(r => r.Medecin)
                        .ThenInclude(m => m!.Utilisateur)
                .Include(b => b.Reservations.Where(r => r.DateReservation.Date == date.Date && r.Statut != "annulee"))
                    .ThenInclude(r => r.Programmation)
                        .ThenInclude(p => p!.Patient)
                            .ThenInclude(pat => pat!.Utilisateur)
                .FirstOrDefaultAsync(b => b.IdBloc == idBloc);

            if (bloc == null)
                return new AgendaBlocDto { IdBloc = idBloc, Date = date };

            // Générer les créneaux de 7h à 20h par tranches de 30 min
            var creneaux = new List<CreneauBlocDto>();
            for (int hour = 7; hour < 20; hour++)
            {
                for (int min = 0; min < 60; min += 30)
                {
                    var heureDebut = $"{hour:D2}:{min:D2}";
                    var heureFin = min == 30 ? $"{hour + 1:D2}:00" : $"{hour:D2}:30";

                    var reservation = bloc.Reservations.FirstOrDefault(r =>
                        string.Compare(r.HeureDebut, heureDebut) <= 0 &&
                        string.Compare(r.HeureFin, heureDebut) > 0);

                    creneaux.Add(new CreneauBlocDto
                    {
                        HeureDebut = heureDebut,
                        HeureFin = heureFin,
                        EstReserve = reservation != null,
                        Reservation = reservation != null ? MapToReservationDto(reservation) : null
                    });
                }
            }

            return new AgendaBlocDto
            {
                IdBloc = bloc.IdBloc,
                NomBloc = bloc.Nom,
                Date = date,
                Creneaux = creneaux
            };
        }

        public async Task<List<AgendaBlocDto>> GetAgendaTousBlocsAsync(DateTime date)
        {
            var blocs = await _context.BlocsOperatoires
                .Where(b => b.Actif)
                .Select(b => b.IdBloc)
                .ToListAsync();

            var agendas = new List<AgendaBlocDto>();
            foreach (var idBloc in blocs)
            {
                agendas.Add(await GetAgendaBlocAsync(idBloc, date));
            }

            return agendas;
        }

        #endregion

        #region Mise à jour des statuts

        public async Task UpdateBlocStatusesAsync()
        {
            var now = DateTime.Now;
            var today = now.Date;
            var currentTime = now.ToString("HH:mm");

            var blocs = await _context.BlocsOperatoires
                .Where(b => b.Actif && b.Statut != "maintenance")
                .Include(b => b.Reservations.Where(r => r.DateReservation.Date == today && r.Statut != "annulee"))
                .ToListAsync();

            foreach (var bloc in blocs)
            {
                var reservationEnCours = bloc.Reservations.FirstOrDefault(r =>
                    string.Compare(r.HeureDebut, currentTime) <= 0 &&
                    string.Compare(r.HeureFin, currentTime) > 0);

                var newStatut = reservationEnCours != null ? "occupe" : "libre";

                if (bloc.Statut != newStatut && bloc.Statut != "maintenance")
                {
                    bloc.Statut = newStatut;
                    bloc.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
        }

        #endregion

        #region Helpers

        private string CalculerHeureFin(string heureDebut, int dureeMinutes)
        {
            var parts = heureDebut.Split(':');
            var hours = int.Parse(parts[0]);
            var minutes = int.Parse(parts[1]);

            var totalMinutes = hours * 60 + minutes + dureeMinutes;
            var endHours = totalMinutes / 60;
            var endMinutes = totalMinutes % 60;

            return $"{endHours:D2}:{endMinutes:D2}";
        }

        private bool HasConflict(List<ReservationBloc> reservations, string heureDebut, string heureFin)
        {
            foreach (var r in reservations)
            {
                // Vérifier le chevauchement
                if (string.Compare(heureDebut, r.HeureFin) < 0 && string.Compare(heureFin, r.HeureDebut) > 0)
                {
                    return true;
                }
            }
            return false;
        }

        private async Task UpdateBlocStatusIfNeeded(BlocOperatoire bloc, ReservationBloc reservation)
        {
            var now = DateTime.Now;
            var currentTime = now.ToString("HH:mm");

            if (reservation.DateReservation.Date == now.Date &&
                string.Compare(reservation.HeureDebut, currentTime) <= 0 &&
                string.Compare(reservation.HeureFin, currentTime) > 0)
            {
                bloc.Statut = "occupe";
                bloc.UpdatedAt = DateTime.UtcNow;
            }
        }

        private ReservationBlocDto MapToReservationDto(ReservationBloc r)
        {
            return new ReservationBlocDto
            {
                IdReservation = r.IdReservation,
                IdBloc = r.IdBloc,
                NomBloc = r.Bloc?.Nom ?? "",
                IdProgrammation = r.IdProgrammation,
                IdMedecin = r.IdMedecin,
                MedecinNom = r.Medecin?.Utilisateur?.Nom ?? "",
                MedecinPrenom = r.Medecin?.Utilisateur?.Prenom ?? "",
                IdPatient = r.Programmation?.IdPatient ?? 0,
                PatientNom = r.Programmation?.Patient?.Utilisateur?.Nom ?? "",
                PatientPrenom = r.Programmation?.Patient?.Utilisateur?.Prenom ?? "",
                TypeIntervention = r.Programmation?.TypeIntervention ?? "",
                IndicationOperatoire = r.Programmation?.IndicationOperatoire,
                DateReservation = r.DateReservation,
                HeureDebut = r.HeureDebut,
                HeureFin = r.HeureFin,
                DureeMinutes = r.DureeMinutes,
                Statut = r.Statut,
                Notes = r.Notes,
                CreatedAt = r.CreatedAt
            };
        }

        #endregion
    }
}
