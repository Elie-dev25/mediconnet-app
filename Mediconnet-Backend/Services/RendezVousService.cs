using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.RendezVous;
using AccueilDtos = Mediconnet_Backend.DTOs.Accueil;
using Mediconnet_Backend.Helpers;
using Mediconnet_Backend.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service pour la gestion des rendez-vous
/// Gère le verrouillage atomique et la gestion de concurrence
/// </summary>
public class RendezVousService : IRendezVousService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RendezVousService> _logger;
    private readonly ISlotLockService _slotLockService;
    private readonly IAppointmentNotificationService _notificationService;
    private readonly NotificationIntegrationService _notificationIntegration;
    private readonly IAssuranceCouvertureService _assuranceCouvertureService;

    public RendezVousService(
        ApplicationDbContext context, 
        ILogger<RendezVousService> logger,
        ISlotLockService slotLockService,
        IAppointmentNotificationService notificationService,
        NotificationIntegrationService notificationIntegration,
        IAssuranceCouvertureService assuranceCouvertureService)
    {
        _context = context;
        _logger = logger;
        _slotLockService = slotLockService;
        _notificationService = notificationService;
        _notificationIntegration = notificationIntegration;
        _assuranceCouvertureService = assuranceCouvertureService;
    }

    // ==================== PATIENT ====================

    public async Task<RendezVousStatsDto> GetPatientStatsAsync(int patientId)
    {
        var now = DateTimeHelper.Now;

        var rdvs = await _context.RendezVous
            .Where(r => r.IdPatient == patientId)
            .ToListAsync();

        var prochainRdv = await _context.RendezVous
            .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(r => r.Service)
            .Where(r => r.IdPatient == patientId && r.DateHeure > now && r.Statut != "annule")
            .OrderBy(r => r.DateHeure)
            .FirstOrDefaultAsync();

        return new RendezVousStatsDto
        {
            TotalRendezVous = rdvs.Count,
            RendezVousAVenir = rdvs.Count(r => r.DateHeure > now && r.Statut != "annule"),
            RendezVousPasses = rdvs.Count(r => r.DateHeure <= now && r.Statut != "annule"),
            RendezVousAnnules = rdvs.Count(r => r.Statut == "annule"),
            ProchainRendezVous = prochainRdv != null ? MapToDto(prochainRdv) : null
        };
    }

    public async Task<List<RendezVousListDto>> GetPatientUpcomingAsync(int patientId)
    {
        var now = DateTimeHelper.Now;

        var rdvs = await _context.RendezVous
            .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(r => r.Service)
            .Where(r => r.IdPatient == patientId && r.DateHeure > now && r.Statut != "annule")
            .OrderBy(r => r.DateHeure)
            .ToListAsync();

        // Récupérer les consultations liées
        var rdvIds = rdvs.Select(r => r.IdRendezVous).ToList();
        var consultations = await _context.Consultations
            .Where(c => c.IdRendezVous.HasValue && rdvIds.Contains(c.IdRendezVous.Value))
            .Select(c => new { c.IdRendezVous, c.IdConsultation, c.Anamnese })
            .ToListAsync();

        return rdvs.Select(r => {
            var consultation = consultations.FirstOrDefault(c => c.IdRendezVous == r.IdRendezVous);
            return MapToListDtoWithConsultation(r, consultation?.IdConsultation, !string.IsNullOrEmpty(consultation?.Anamnese));
        }).ToList();
    }

    public async Task<List<RendezVousListDto>> GetPatientHistoryAsync(int patientId, int limite = 20)
    {
        var now = DateTimeHelper.Now;

        var rdvs = await _context.RendezVous
            .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(r => r.Service)
            .Where(r => r.IdPatient == patientId && (r.DateHeure <= now || r.Statut == "annule"))
            .OrderByDescending(r => r.DateHeure)
            .Take(limite)
            .ToListAsync();

        // Récupérer les consultations liées
        var rdvIds = rdvs.Select(r => r.IdRendezVous).ToList();
        var consultations = await _context.Consultations
            .Where(c => c.IdRendezVous.HasValue && rdvIds.Contains(c.IdRendezVous.Value))
            .Select(c => new { c.IdRendezVous, c.IdConsultation, c.Anamnese })
            .ToListAsync();

        return rdvs.Select(r => {
            var consultation = consultations.FirstOrDefault(c => c.IdRendezVous == r.IdRendezVous);
            return MapToListDtoWithConsultation(r, consultation?.IdConsultation, !string.IsNullOrEmpty(consultation?.Anamnese));
        }).ToList();
    }

    public async Task<RendezVousDto?> GetRendezVousAsync(int rdvId, int patientId)
    {
        var rdv = await _context.RendezVous
            .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(r => r.Service)
            .FirstOrDefaultAsync(r => r.IdRendezVous == rdvId && r.IdPatient == patientId);

        return rdv != null ? MapToDto(rdv) : null;
    }

    public async Task<(bool Success, string Message, RendezVousDto? RendezVous)> CreateRendezVousAsync(
        CreateRendezVousRequest request, int patientId)
    {
        // Vérifier que le médecin existe
        var medecin = await _context.Medecins
            .Include(m => m.Utilisateur)
            .FirstOrDefaultAsync(m => m.IdUser == request.IdMedecin);

        if (medecin == null)
            return (false, "Médecin introuvable", null);

        // Vérifier que la date est dans le futur (permet les créneaux du jour même non encore passés)
        if (DateTimeHelper.IsSlotPassed(request.DateHeure))
            return (false, "Ce créneau est déjà passé", null);

        // Vérifier les indisponibilités du médecin
        var estIndisponible = await _context.IndisponibilitesMedecin
            .AnyAsync(i => i.IdMedecin == request.IdMedecin &&
                          request.DateHeure.Date >= i.DateDebut.Date &&
                          request.DateHeure.Date <= i.DateFin.Date);

        if (estIndisponible)
            return (false, "Le médecin n'est pas disponible à cette date", null);

        // Acquérir un verrou temporaire sur le créneau (atomique)
        var lockResult = await _slotLockService.AcquireLockAsync(
            request.IdMedecin, 
            request.DateHeure, 
            request.Duree, 
            patientId);

        if (!lockResult.Success)
            return (false, lockResult.Message, null);

        try
        {
            // Double vérification après acquisition du verrou
            var conflit = await _context.RendezVous
                .AnyAsync(r => r.IdMedecin == request.IdMedecin &&
                              r.Statut != "annule" &&
                              r.DateHeure < request.DateHeure.AddMinutes(request.Duree) &&
                              r.DateHeure.AddMinutes(r.Duree) > request.DateHeure);

            if (conflit)
            {
                await _slotLockService.ReleaseLockAsync(lockResult.LockToken!, patientId);
                return (false, "Ce créneau n'est plus disponible", null);
            }

            // Vérifier que le patient n'a pas déjà un RDV au même moment
            var conflitPatient = await _context.RendezVous
                .AnyAsync(r => r.IdPatient == patientId &&
                              r.Statut != "annule" &&
                              r.DateHeure < request.DateHeure.AddMinutes(request.Duree) &&
                              r.DateHeure.AddMinutes(r.Duree) > request.DateHeure);

            if (conflitPatient)
            {
                await _slotLockService.ReleaseLockAsync(lockResult.LockToken!, patientId);
                return (false, "Vous avez déjà un rendez-vous à ce créneau", null);
            }

            // Créer le rendez-vous dans une transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Récupérer les infos patient pour l'assurance
                var patient = await _context.Patients
                    .Include(p => p.Utilisateur)
                    .Include(p => p.Assurance)
                    .FirstOrDefaultAsync(p => p.IdUser == patientId);

                if (patient == null)
                {
                    await _slotLockService.ReleaseLockAsync(lockResult.LockToken!, patientId);
                    return (false, "Patient introuvable", null);
                }

                var now = DateTime.UtcNow;

                // Vérifier si un paiement valide existe déjà (14 jours, même service/spécialité)
                var paymentValidSince = now.AddDays(-14);
                var factureValide = await _context.Factures
                    .Where(f => f.IdPatient == patientId)
                    .Where(f => f.TypeFacture == "consultation")
                    .Where(f => f.Statut == "payee")
                    .Where(f => f.DatePaiement.HasValue && f.DatePaiement.Value >= paymentValidSince)
                    .Where(f => f.IdService == medecin.IdService)
                    .Where(f => f.IdSpecialite == medecin.IdSpecialite)
                    .OrderByDescending(f => f.DatePaiement)
                    .FirstOrDefaultAsync();

                // Statut RDV: "planifie" (en attente validation médecin)
                // Après validation médecin: "confirme" si paiement valide, sinon reste "planifie" jusqu'au paiement
                var rdv = new RendezVous
                {
                    IdPatient = patientId,
                    IdMedecin = request.IdMedecin,
                    IdService = request.IdService ?? medecin.IdService,
                    DateHeure = request.DateHeure,
                    Duree = request.Duree,
                    Motif = request.Motif,
                    Notes = request.Notes,
                    TypeRdv = request.TypeRdv,
                    Statut = "planifie",
                    DateCreation = now
                };

                _context.RendezVous.Add(rdv);
                await _context.SaveChangesAsync();

                // Créer la consultation liée au RDV (pour le workflow patient en ligne)
                var consultation = new Consultation
                {
                    IdPatient = patientId,
                    IdMedecin = request.IdMedecin,
                    IdRendezVous = rdv.IdRendezVous,
                    Motif = request.Motif,
                    DateHeure = request.DateHeure,
                    Statut = "planifie",
                    TypeConsultation = "normale"
                };

                _context.Consultations.Add(consultation);
                await _context.SaveChangesAsync();

                // Générer un numéro de facture unique
                var numeroFacture = await GenererNumeroFactureAsync();

                // Prix consultation par défaut (peut être configuré)
                decimal prixConsultation = 5000; // FCFA - à récupérer depuis config ou spécialité
                
                // Calculer la couverture assurance via le service centralisé
                var couverture = await _assuranceCouvertureService.CalculerCouvertureAsync(patient, "consultation", prixConsultation);
                var estAssure = couverture.EstAssure;
                var tauxCouverture = couverture.TauxCouverture;
                var montantAssurance = couverture.MontantAssurance;
                var montantPatient = couverture.MontantPatient;

                // Créer la facture (en attente de paiement, sauf si paiement valide existe)
                var facture = new Facture
                {
                    NumeroFacture = numeroFacture,
                    IdPatient = patientId,
                    IdMedecin = request.IdMedecin,
                    IdService = medecin.IdService,
                    IdSpecialite = medecin.IdSpecialite,
                    IdConsultation = consultation.IdConsultation,
                    MontantTotal = factureValide != null ? 0 : prixConsultation,
                    MontantPaye = 0,
                    MontantRestant = factureValide != null ? 0 : montantPatient,
                    Statut = factureValide != null ? "payee" : "en_attente",
                    TypeFacture = "consultation",
                    DateCreation = now,
                    DateEcheance = factureValide != null ? null : request.DateHeure.AddDays(-1), // Payer avant le RDV
                    CouvertureAssurance = estAssure,
                    IdAssurance = estAssure ? patient.AssuranceId : null,
                    TauxCouverture = estAssure ? tauxCouverture : null,
                    MontantAssurance = estAssure ? montantAssurance : null,
                    DatePaiement = factureValide?.DatePaiement,
                    Notes = factureValide != null
                        ? $"Paiement consultation déjà valable jusqu'au {factureValide.DatePaiement!.Value.AddDays(14):yyyy-MM-dd}. Facture de référence: {factureValide.NumeroFacture}"
                        : "Facture pour RDV pris en ligne - Paiement requis avant la consultation"
                };

                _context.Factures.Add(facture);
                await _context.SaveChangesAsync();

                // Ajouter la ligne de facture détaillée
                var ligneFacture = new LigneFacture
                {
                    IdFacture = facture.IdFacture,
                    Description = $"Consultation - Dr. {medecin.Utilisateur?.Nom ?? "N/A"}",
                    Quantite = 1,
                    PrixUnitaire = prixConsultation,
                    Categorie = "consultation"
                };

                _context.LignesFacture.Add(ligneFacture);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Libérer le verrou après création réussie
                await _slotLockService.ReleaseLockAsync(lockResult.LockToken!, patientId);

                // Recharger avec les relations
                var rdvComplet = await _context.RendezVous
                    .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
                    .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
                    .Include(r => r.Service)
                    .FirstAsync(r => r.IdRendezVous == rdv.IdRendezVous);

                _logger.LogInformation($"RDV en ligne créé: RDV={rdv.IdRendezVous}, Consultation={consultation.IdConsultation}, Facture={facture.IdFacture} pour patient {patientId}");

                // Notification temps réel (SignalR)
                var rdvDto = MapToDto(rdvComplet);
                await _notificationService.NotifyAppointmentCreatedAsync(
                    rdv.IdMedecin, patientId, rdvDto);

                // Notifier la création de facture (pour la caisse)
                await _notificationService.NotifyFactureCreatedAsync(new
                {
                    idFacture = facture.IdFacture,
                    numeroFacture = facture.NumeroFacture,
                    typeFacture = facture.TypeFacture,
                    statut = facture.Statut,
                    idPatient = facture.IdPatient,
                    idMedecin = facture.IdMedecin,
                    idService = facture.IdService,
                    idSpecialite = facture.IdSpecialite,
                    montantRestant = facture.MontantRestant,
                    dateCreation = facture.DateCreation,
                    sourceRdvEnLigne = true
                });

                // Notification persistante (cloche) - notifier le médecin du nouveau RDV
                var patientNomComplet = patient.Utilisateur != null 
                    ? $"{patient.Utilisateur.Prenom} {patient.Utilisateur.Nom}"
                    : "Patient";
                await _notificationIntegration.NotifyRendezVousCreatedAsync(
                    rdv.IdMedecin, patientId, patientNomComplet, rdv.DateHeure);

                return (true, "Rendez-vous créé avec succès. Une facture a été générée pour le paiement.", rdvDto);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                await _slotLockService.ReleaseLockAsync(lockResult.LockToken!, patientId);
                _logger.LogWarning($"Conflit de concurrence: {ex.Message}");
                return (false, "Une modification simultanée a été détectée. Veuillez réessayer.", null);
            }
        }
        catch (Exception ex)
        {
            // S'assurer de libérer le verrou en cas d'erreur
            if (lockResult.LockToken != null)
                await _slotLockService.ReleaseLockAsync(lockResult.LockToken, patientId);
            
            _logger.LogError($"Erreur lors de la création du RDV: {ex.Message}");
            throw;
        }
    }

    public async Task<(bool Success, string Message)> UpdateRendezVousAsync(
        int rdvId, UpdateRendezVousRequest request, int patientId)
    {
        var rdv = await _context.RendezVous
            .FirstOrDefaultAsync(r => r.IdRendezVous == rdvId && r.IdPatient == patientId);

        if (rdv == null)
            return (false, "Rendez-vous introuvable");

        if (rdv.Statut == "annule")
            return (false, "Ce rendez-vous a été annulé");

        if (rdv.Statut == "termine")
            return (false, "Ce rendez-vous est déjà terminé");

        // Mise à jour de la date si fournie
        if (request.DateHeure.HasValue)
        {
            if (request.DateHeure <= DateTime.UtcNow)
                return (false, "La date du rendez-vous doit être dans le futur");

            var duree = request.Duree ?? rdv.Duree;
            var conflit = await _context.RendezVous
                .AnyAsync(r => r.IdMedecin == rdv.IdMedecin &&
                              r.IdRendezVous != rdvId &&
                              r.Statut != "annule" &&
                              r.DateHeure < request.DateHeure.Value.AddMinutes(duree) &&
                              r.DateHeure.AddMinutes(r.Duree) > request.DateHeure.Value);

            if (conflit)
                return (false, "Ce créneau n'est pas disponible");

            rdv.DateHeure = request.DateHeure.Value;
        }

        if (request.Duree.HasValue) rdv.Duree = request.Duree.Value;
        if (request.Motif != null) rdv.Motif = request.Motif;
        if (request.Notes != null) rdv.Notes = request.Notes;
        if (request.TypeRdv != null) rdv.TypeRdv = request.TypeRdv;

        rdv.DateModification = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Rendez-vous modifié: {rdvId}");

        return (true, "Rendez-vous modifié avec succès");
    }

    public async Task<(bool Success, string Message)> AnnulerRendezVousAsync(
        AnnulerRendezVousRequest request, int patientId)
    {
        var rdv = await _context.RendezVous
            .FirstOrDefaultAsync(r => r.IdRendezVous == request.IdRendezVous && r.IdPatient == patientId);

        if (rdv == null)
            return (false, "Rendez-vous introuvable");

        if (rdv.Statut == "annule")
            return (false, "Ce rendez-vous est déjà annulé");

        if (rdv.Statut == "termine")
            return (false, "Ce rendez-vous est déjà terminé");

        // Vérifier qu'on annule au moins 2h avant
        if (rdv.DateHeure < DateTime.UtcNow.AddHours(2))
            return (false, "Impossible d'annuler un rendez-vous moins de 2 heures avant");

        var medecinId = rdv.IdMedecin;
        
        rdv.Statut = "annule";
        rdv.MotifAnnulation = request.Motif;
        rdv.DateAnnulation = DateTime.UtcNow;
        rdv.AnnulePar = patientId;

        await _context.SaveChangesAsync();

        // Notification temps réel
        await _notificationService.NotifyAppointmentCancelledAsync(
            medecinId, patientId, request.IdRendezVous);

        _logger.LogInformation($"Rendez-vous annulé: {request.IdRendezVous}");

        return (true, "Rendez-vous annulé avec succès");
    }

    // ==================== SERVICES ====================

    public async Task<List<AccueilDtos.ServiceDto>> GetServicesAsync()
    {
        var services = await _context.Services
            .OrderBy(s => s.NomService)
            .Select(s => new AccueilDtos.ServiceDto
            {
                IdService = s.IdService,
                NomService = s.NomService,
                Description = s.Description
            })
            .ToListAsync();

        return services;
    }

    // ==================== CRÉNEAUX ====================

    public async Task<List<MedecinDisponibleDto>> GetMedecinsDisponiblesAsync(int? serviceId = null)
    {
        var query = _context.Medecins
            .Include(m => m.Utilisateur)
            .Include(m => m.Service)
            .AsQueryable();

        if (serviceId.HasValue)
            query = query.Where(m => m.IdService == serviceId.Value);

        var medecins = await query.ToListAsync();

        // Charger les spécialités
        var specialites = await _context.Specialites.ToListAsync();

        return medecins.Select(m => new MedecinDisponibleDto
        {
            IdMedecin = m.IdUser,
            Nom = m.Utilisateur?.Nom ?? "",
            Prenom = m.Utilisateur?.Prenom ?? "",
            Specialite = m.IdSpecialite.HasValue 
                ? specialites.FirstOrDefault(s => s.IdSpecialite == m.IdSpecialite)?.NomSpecialite 
                : null,
            ServiceNom = m.Service?.NomService,
            IdService = m.IdService,
            ProchainCreneauDansJours = 0 // À calculer si nécessaire
        }).ToList();
    }

    public async Task<CreneauxDisponiblesResponse> GetCreneauxDisponiblesAsync(
        int medecinId, DateTime dateDebut, DateTime dateFin)
    {
        var response = new CreneauxDisponiblesResponse();

        _logger.LogInformation($"GetCreneauxDisponiblesAsync - MedecinId: {medecinId}, DateDebut: {dateDebut}, DateFin: {dateFin}");

        // Récupérer les créneaux configurés du médecin
        var creneauxConfigures = await _context.CreneauxDisponibles
            .Where(c => c.IdMedecin == medecinId && c.Actif)
            .ToListAsync();

        _logger.LogInformation($"Créneaux configurés trouvés: {creneauxConfigures.Count}");
        foreach (var c in creneauxConfigures)
        {
            _logger.LogInformation($"  - Jour {c.JourSemaine}: {c.HeureDebut} - {c.HeureFin}");
        }

        // Si aucun créneau configuré, le médecin est indisponible
        if (!creneauxConfigures.Any())
        {
            response.MedecinDisponible = false;
            response.MessageIndisponibilite = "Ce médecin n'a pas encore défini ses créneaux de disponibilité";
            return response;
        }

        response.MedecinDisponible = true;

        // Récupérer les RDV CONFIRMÉS du médecin (seuls les RDV confirmés occupent les créneaux)
        // Les RDV "planifie" (en attente de validation) ne bloquent pas les créneaux
        var rdvExistants = await _context.RendezVous
            .Where(r => r.IdMedecin == medecinId &&
                       r.Statut == "confirme" &&
                       r.DateHeure >= dateDebut &&
                       r.DateHeure <= dateFin)
            .ToListAsync();

        // Récupérer les consultations EN COURS du médecin (statut "en_cours" = médecin a cliqué sur "Commencer")
        // Ces consultations occupent le créneau actuel
        var consultationsEnCours = await _context.Consultations
            .Include(c => c.RendezVous)
            .Where(c => c.IdMedecin == medecinId &&
                       c.Statut == "en_cours" &&
                       c.RendezVous != null &&
                       c.RendezVous.DateHeure >= dateDebut &&
                       c.RendezVous.DateHeure <= dateFin)
            .Select(c => c.RendezVous!)
            .ToListAsync();

        // Récupérer les indisponibilités du médecin
        var indisponibilites = await _context.IndisponibilitesMedecin
            .Where(i => i.IdMedecin == medecinId &&
                       i.DateDebut <= dateFin &&
                       i.DateFin >= dateDebut)
            .ToListAsync();

        // Récupérer les verrous actifs (utiliser UTC pour la comparaison avec ExpiresAt qui est en UTC)
        var nowUtc = DateTime.UtcNow;
        var verrous = await _context.SlotLocks
            .Where(l => l.IdMedecin == medecinId &&
                       l.ExpiresAt > nowUtc &&
                       l.DateHeure >= dateDebut &&
                       l.DateHeure <= dateFin)
            .ToListAsync();

        for (var date = dateDebut.Date; date <= dateFin.Date; date = date.AddDays(1))
        {
            // Ignorer les dimanches
            if (date.DayOfWeek == DayOfWeek.Sunday) continue;

            // Vérifier si le médecin est indisponible ce jour
            var indispoJour = indisponibilites.FirstOrDefault(i =>
                date >= i.DateDebut.Date && date <= i.DateFin.Date);

            // Convertir DayOfWeek (0=Dim) vers notre format (1=Lun)
            var jourSemaine = (int)date.DayOfWeek;
            if (jourSemaine == 0) jourSemaine = 7; // Dimanche = 7

            // Récupérer les plages horaires du médecin pour ce jour
            var plagesJour = creneauxConfigures
                .Where(c => c.JourSemaine == jourSemaine)
                .ToList();

            // Si pas de créneaux ce jour-là, continuer
            if (!plagesJour.Any()) continue;

            foreach (var plage in plagesJour)
            {
                var heureActuelle = plage.HeureDebut;
                var duree = plage.DureeParDefaut > 0 ? plage.DureeParDefaut : 30;

                while (heureActuelle.Add(TimeSpan.FromMinutes(duree)) <= plage.HeureFin)
                {
                    var dateHeure = date.Add(heureActuelle);

                    // Déterminer le statut du créneau
                    string statut;
                    string? raison = null;
                    int? idRdv = null;
                    bool disponible;

                    // Créneau passé (vérification à la minute près avec heure Cameroun UTC+1)
                    if (DateTimeHelper.IsSlotPassed(dateHeure))
                    {
                        statut = "passe";
                        raison = "Créneau passé";
                        disponible = false;
                    }
                    // Indisponibilité du médecin
                    else if (indispoJour != null)
                    {
                        statut = "indisponible";
                        raison = indispoJour.Motif ?? $"Médecin en {indispoJour.Type}";
                        disponible = false;
                    }
                    // Vérifier si le créneau est pris par une consultation EN COURS (médecin a cliqué "Commencer")
                    else
                    {
                        var consultationEnCours = consultationsEnCours.FirstOrDefault(r =>
                            dateHeure < r.DateHeure.AddMinutes(r.Duree) &&
                            dateHeure.AddMinutes(duree) > r.DateHeure);

                        if (consultationEnCours != null)
                        {
                            statut = "occupe";
                            raison = "Consultation en cours";
                            idRdv = consultationEnCours.IdRendezVous;
                            disponible = false;
                        }
                        // Vérifier si le créneau est pris par un RDV confirmé
                        else
                        {
                            var rdvPris = rdvExistants.FirstOrDefault(r =>
                                dateHeure < r.DateHeure.AddMinutes(r.Duree) &&
                                dateHeure.AddMinutes(duree) > r.DateHeure);

                            if (rdvPris != null)
                            {
                                statut = "occupe";
                                raison = "Rendez-vous existant";
                                idRdv = rdvPris.IdRendezVous;
                                disponible = false;
                            }
                            // Vérifier si le créneau est verrouillé
                            else
                            {
                                var verrouActif = verrous.FirstOrDefault(l =>
                                    dateHeure < l.DateHeure.AddMinutes(l.Duree) &&
                                    dateHeure.AddMinutes(duree) > l.DateHeure);

                                if (verrouActif != null)
                                {
                                    statut = "verrouille";
                                    raison = "Réservation en cours par un autre utilisateur";
                                    disponible = false;
                                }
                                else
                                {
                                    statut = "disponible";
                                    disponible = true;
                                }
                            }
                        }
                    }

                    response.Creneaux.Add(new CreneauDisponibleDto
                    {
                        DateHeure = dateHeure,
                        Duree = duree,
                        Disponible = disponible,
                        Statut = statut,
                        Raison = raison,
                        IdRendezVous = idRdv
                    });

                    heureActuelle = heureActuelle.Add(TimeSpan.FromMinutes(duree));
                }
            }
        }

        response.Creneaux = response.Creneaux.OrderBy(c => c.DateHeure).ToList();
        return response;
    }

    // ==================== HELPERS ====================

    private RendezVousDto MapToDto(RendezVous rdv)
    {
        return new RendezVousDto
        {
            IdRendezVous = rdv.IdRendezVous,
            IdPatient = rdv.IdPatient,
            PatientNom = rdv.Patient?.Utilisateur?.Nom ?? "",
            PatientPrenom = rdv.Patient?.Utilisateur?.Prenom ?? "",
            NumeroDossier = rdv.Patient?.NumeroDossier,
            IdMedecin = rdv.IdMedecin,
            MedecinNom = rdv.Medecin?.Utilisateur?.Nom ?? "",
            MedecinPrenom = rdv.Medecin?.Utilisateur?.Prenom ?? "",
            IdService = rdv.IdService,
            ServiceNom = rdv.Service?.NomService,
            DateHeure = rdv.DateHeure,
            Duree = rdv.Duree,
            Statut = rdv.Statut,
            Motif = rdv.Motif,
            Notes = rdv.Notes,
            TypeRdv = rdv.TypeRdv,
            DateCreation = rdv.DateCreation
        };
    }

    private RendezVousListDto MapToListDto(RendezVous rdv)
    {
        return MapToListDtoWithConsultation(rdv, null, false);
    }

    private RendezVousListDto MapToListDtoWithConsultation(RendezVous rdv, int? idConsultation, bool anamneseRemplie)
    {
        return new RendezVousListDto
        {
            IdRendezVous = rdv.IdRendezVous,
            IdConsultation = idConsultation,
            DateHeure = rdv.DateHeure,
            Duree = rdv.Duree,
            Statut = rdv.Statut,
            TypeRdv = rdv.TypeRdv,
            Motif = rdv.Motif,
            MedecinNom = rdv.Medecin?.Utilisateur != null
                ? $"Dr. {rdv.Medecin.Utilisateur.Prenom} {rdv.Medecin.Utilisateur.Nom}"
                : "",
            ServiceNom = rdv.Service?.NomService,
            AnamneseRemplie = anamneseRemplie
        };
    }

    // ==================== MÉDECIN ====================

    public async Task<List<RendezVousDto>> GetMedecinRendezVousAsync(
        int medecinId, DateTime? dateDebut = null, DateTime? dateFin = null, string? statut = null)
    {
        var query = _context.RendezVous
            .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(r => r.Service)
            .Where(r => r.IdMedecin == medecinId);

        if (dateDebut.HasValue)
            query = query.Where(r => r.DateHeure >= dateDebut.Value);

        if (dateFin.HasValue)
            query = query.Where(r => r.DateHeure <= dateFin.Value);

        if (!string.IsNullOrEmpty(statut))
            query = query.Where(r => r.Statut == statut);

        var rdvs = await query.OrderBy(r => r.DateHeure).ToListAsync();
        return rdvs.Select(MapToDto).ToList();
    }

    public async Task<List<RendezVousDto>> GetMedecinRdvJourAsync(int medecinId, DateTime date)
    {
        var debutJour = date.Date;
        var finJour = date.Date.AddDays(1).AddSeconds(-1);

        return await GetMedecinRendezVousAsync(medecinId, debutJour, finJour);
    }

    public async Task<(bool Success, string Message)> UpdateStatutRdvAsync(
        int medecinId, int rdvId, string nouveauStatut)
    {
        var rdv = await _context.RendezVous
            .FirstOrDefaultAsync(r => r.IdRendezVous == rdvId && r.IdMedecin == medecinId);

        if (rdv == null)
            return (false, "Rendez-vous introuvable");

        var statutsValides = new[] { "planifie", "confirme", "en_cours", "termine", "annule", "absent" };
        if (!statutsValides.Contains(nouveauStatut))
            return (false, "Statut invalide");

        rdv.Statut = nouveauStatut;
        rdv.DateModification = DateTime.UtcNow;

        if (nouveauStatut == "annule")
        {
            rdv.DateAnnulation = DateTime.UtcNow;
            rdv.AnnulePar = medecinId;
        }

        await _context.SaveChangesAsync();
        return (true, "Statut mis à jour avec succès");
    }
    // ==================== VALIDATION RDV ====================

    public async Task<List<RendezVousDto>> GetRdvEnAttenteAsync(int medecinId)
    {
        var rdvs = await _context.RendezVous
            .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(r => r.Service)
            .Where(r => r.IdMedecin == medecinId && 
                       r.Statut == "planifie" &&
                       r.DateHeure > DateTime.UtcNow)
            .OrderBy(r => r.DateHeure)
            .ToListAsync();

        return rdvs.Select(MapToDto).ToList();
    }

    public async Task<ActionRdvResponse> ValiderRdvAsync(int medecinId, int rdvId)
    {
        var response = new ActionRdvResponse();

        var rdv = await _context.RendezVous
            .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(r => r.Service)
            .FirstOrDefaultAsync(r => r.IdRendezVous == rdvId && r.IdMedecin == medecinId);

        if (rdv == null)
        {
            response.Message = "Rendez-vous introuvable";
            return response;
        }

        if (rdv.Statut != "planifie")
        {
            response.Message = "Ce rendez-vous n'est pas en attente de validation";
            return response;
        }

        // Vérifier les conflits avec d'autres RDV déjà confirmés
        var conflit = await _context.RendezVous
            .AnyAsync(r => r.IdMedecin == medecinId &&
                          r.IdRendezVous != rdvId &&
                          r.Statut == "confirme" &&
                          r.DateHeure < rdv.DateHeure.AddMinutes(rdv.Duree) &&
                          r.DateHeure.AddMinutes(r.Duree) > rdv.DateHeure);

        if (conflit)
        {
            response.ConflitDetecte = true;
            response.Message = "Ce créneau est déjà occupé par un autre rendez-vous confirmé. Veuillez suggérer un autre créneau au patient.";
            return response;
        }

        // Valider le RDV
        rdv.Statut = "confirme";
        rdv.DateModification = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"RDV {rdvId} validé par médecin {medecinId}");

        // Envoyer notification email au patient
        await SendEmailNotificationAsync(rdv, "validation");

        response.Success = true;
        response.Message = "Rendez-vous confirmé avec succès";
        response.RendezVous = MapToDto(rdv);

        // Notification temps réel (SignalR)
        await _notificationService.NotifyAppointmentUpdatedAsync(
            medecinId, rdv.IdPatient, response.RendezVous);

        // Notification persistante (cloche) - notifier le patient de la confirmation
        var medecinNom = rdv.Medecin?.Utilisateur != null 
            ? $"{rdv.Medecin.Utilisateur.Prenom} {rdv.Medecin.Utilisateur.Nom}"
            : "Médecin";
        await _notificationIntegration.NotifyRendezVousConfirmedAsync(
            rdv.IdPatient, medecinNom, rdv.DateHeure);

        return response;
    }

    public async Task<ActionRdvResponse> AnnulerRdvMedecinAsync(int medecinId, AnnulerRdvMedecinRequest request)
    {
        var response = new ActionRdvResponse();

        var rdv = await _context.RendezVous
            .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(r => r.Service)
            .FirstOrDefaultAsync(r => r.IdRendezVous == request.IdRendezVous && r.IdMedecin == medecinId);

        if (rdv == null)
        {
            response.Message = "Rendez-vous introuvable";
            return response;
        }

        if (rdv.Statut == "annule")
        {
            response.Message = "Ce rendez-vous est déjà annulé";
            return response;
        }

        if (rdv.Statut == "termine")
        {
            response.Message = "Ce rendez-vous est déjà terminé";
            return response;
        }

        rdv.Statut = "annule";
        rdv.MotifAnnulation = request.Motif;
        rdv.DateAnnulation = DateTime.UtcNow;
        rdv.AnnulePar = medecinId;
        rdv.DateModification = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"RDV {request.IdRendezVous} annulé par médecin {medecinId}");

        // Envoyer notification email au patient
        await SendEmailNotificationAsync(rdv, "annulation", request.Motif);

        response.Success = true;
        response.Message = "Rendez-vous annulé avec succès";
        response.RendezVous = MapToDto(rdv);

        // Notification temps réel (SignalR)
        await _notificationService.NotifyAppointmentCancelledAsync(
            medecinId, rdv.IdPatient, request.IdRendezVous);

        // Notification persistante (cloche) - notifier le patient de l'annulation
        var medecinNom = rdv.Medecin?.Utilisateur != null 
            ? $"{rdv.Medecin.Utilisateur.Prenom} {rdv.Medecin.Utilisateur.Nom}"
            : "Médecin";
        await _notificationIntegration.NotifyRendezVousCancelledAsync(
            rdv.IdPatient, medecinNom, request.Motif ?? "Non spécifiée", rdv.DateHeure);

        return response;
    }

    public async Task<ActionRdvResponse> SuggererCreneauAsync(int medecinId, SuggererCreneauRequest request)
    {
        var response = new ActionRdvResponse();

        var rdv = await _context.RendezVous
            .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(r => r.Service)
            .FirstOrDefaultAsync(r => r.IdRendezVous == request.IdRendezVous && r.IdMedecin == medecinId);

        if (rdv == null)
        {
            response.Message = "Rendez-vous introuvable";
            return response;
        }

        if (rdv.Statut != "planifie")
        {
            response.Message = "Ce rendez-vous n'est pas en attente de validation";
            return response;
        }

        if (request.NouveauCreneau <= DateTime.UtcNow)
        {
            response.Message = "Le nouveau créneau doit être dans le futur";
            return response;
        }

        // Vérifier que le nouveau créneau n'est pas en conflit
        var conflit = await _context.RendezVous
            .AnyAsync(r => r.IdMedecin == medecinId &&
                          r.IdRendezVous != request.IdRendezVous &&
                          r.Statut == "confirme" &&
                          r.DateHeure < request.NouveauCreneau.AddMinutes(rdv.Duree) &&
                          r.DateHeure.AddMinutes(r.Duree) > request.NouveauCreneau);

        if (conflit)
        {
            response.ConflitDetecte = true;
            response.Message = "Le nouveau créneau proposé est également occupé";
            return response;
        }

        var ancienCreneau = rdv.DateHeure;
        rdv.DateHeure = request.NouveauCreneau;
        rdv.Statut = "proposition"; // Statut spécial pour les créneaux suggérés
        rdv.Notes = (rdv.Notes ?? "") + $"\n[Créneau suggéré par le médecin le {DateTime.Now:dd/MM/yyyy HH:mm}]";
        rdv.DateModification = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"RDV {request.IdRendezVous} - nouveau créneau suggéré par médecin {medecinId}");

        // Envoyer notification email au patient
        await SendEmailNotificationAsync(rdv, "suggestion", request.Message, ancienCreneau);

        response.Success = true;
        response.Message = "Nouveau créneau proposé avec succès. Le patient a été notifié.";
        response.RendezVous = MapToDto(rdv);

        return response;
    }

    // ==================== EMAIL NOTIFICATIONS ====================

    private async Task SendEmailNotificationAsync(RendezVous rdv, string type, string? motif = null, DateTime? ancienCreneau = null)
    {
        try
        {
            var patientEmail = rdv.Patient?.Utilisateur?.Email;
            if (string.IsNullOrEmpty(patientEmail))
            {
                _logger.LogWarning($"Pas d'email pour le patient {rdv.IdPatient}");
                return;
            }

            var patientNom = $"{rdv.Patient?.Utilisateur?.Prenom} {rdv.Patient?.Utilisateur?.Nom}";
            var medecinNom = $"Dr. {rdv.Medecin?.Utilisateur?.Prenom} {rdv.Medecin?.Utilisateur?.Nom}";
            var dateRdv = rdv.DateHeure.ToString("dddd dd MMMM yyyy à HH:mm", new System.Globalization.CultureInfo("fr-FR"));

            string subject;
            string body;

            switch (type)
            {
                case "validation":
                    subject = "✅ Votre rendez-vous a été confirmé - MediConnet";
                    body = $@"
                        <h2>Bonjour {patientNom},</h2>
                        <p>Votre rendez-vous a été <strong>confirmé</strong> par {medecinNom}.</p>
                        <div style='background: #e8f5e9; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                            <p><strong>📅 Date :</strong> {dateRdv}</p>
                            <p><strong>👨‍⚕️ Médecin :</strong> {medecinNom}</p>
                            <p><strong>📋 Motif :</strong> {rdv.Motif ?? "Non spécifié"}</p>
                        </div>
                        <p>Nous vous attendons à l'heure prévue.</p>
                        <p>Cordialement,<br/>L'équipe MediConnet</p>";
                    break;

                case "annulation":
                    subject = "❌ Votre rendez-vous a été annulé - MediConnet";
                    body = $@"
                        <h2>Bonjour {patientNom},</h2>
                        <p>Nous sommes désolés de vous informer que votre rendez-vous a été <strong>annulé</strong> par {medecinNom}.</p>
                        <div style='background: #ffebee; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                            <p><strong>📅 Date prévue :</strong> {dateRdv}</p>
                            <p><strong>👨‍⚕️ Médecin :</strong> {medecinNom}</p>
                            <p><strong>📝 Raison :</strong> {motif ?? "Non spécifiée"}</p>
                        </div>
                        <p>Veuillez prendre un nouveau rendez-vous via notre plateforme.</p>
                        <p>Cordialement,<br/>L'équipe MediConnet</p>";
                    break;

                case "suggestion":
                    var ancienneDateStr = ancienCreneau?.ToString("dddd dd MMMM yyyy à HH:mm", new System.Globalization.CultureInfo("fr-FR")) ?? "";
                    subject = "📅 Nouveau créneau proposé pour votre rendez-vous - MediConnet";
                    body = $@"
                        <h2>Bonjour {patientNom},</h2>
                        <p>{medecinNom} vous propose un <strong>nouveau créneau</strong> pour votre rendez-vous.</p>
                        <div style='background: #fff3e0; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                            <p><strong>❌ Ancien créneau :</strong> {ancienneDateStr}</p>
                            <p><strong>✅ Nouveau créneau :</strong> {dateRdv}</p>
                            <p><strong>👨‍⚕️ Médecin :</strong> {medecinNom}</p>
                            {(string.IsNullOrEmpty(motif) ? "" : $"<p><strong>💬 Message :</strong> {motif}</p>")}
                        </div>
                        <p>Veuillez vous connecter à votre espace patient pour confirmer ce nouveau créneau.</p>
                        <p>Cordialement,<br/>L'équipe MediConnet</p>";
                    break;

                default:
                    return;
            }

            // Utiliser le service d'email existant
            await SendEmailAsync(patientEmail, subject, body);

            rdv.Notifie = true;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur envoi email notification: {ex.Message}");
        }
    }

    private async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            // Utiliser MailKit pour envoyer l'email via le serveur SMTP configuré
            using var client = new MailKit.Net.Smtp.SmtpClient();
            
            var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "mailhog";
            var smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "1025");

            await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.None);

            var message = new MimeKit.MimeMessage();
            message.From.Add(new MimeKit.MailboxAddress("MediConnet", "noreply@mediconnet.com"));
            message.To.Add(new MimeKit.MailboxAddress("", to));
            message.Subject = subject;

            var bodyBuilder = new MimeKit.BodyBuilder
            {
                HtmlBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                        {htmlBody}
                    </body>
                    </html>"
            };
            message.Body = bodyBuilder.ToMessageBody();

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation($"Email envoyé à {to}: {subject}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur envoi email: {ex.Message}");
        }
    }

    // ==================== GESTION PROPOSITIONS PATIENT ====================

    public async Task<List<RendezVousDto>> GetPropositionsPatientAsync(int patientId)
    {
        var rdvs = await _context.RendezVous
            .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(r => r.Service)
            .Where(r => r.IdPatient == patientId && r.Statut == "proposition")
            .OrderBy(r => r.DateHeure)
            .ToListAsync();

        return rdvs.Select(MapToDto).ToList();
    }

    public async Task<ActionRdvResponse> AccepterPropositionAsync(int patientId, int rdvId)
    {
        var response = new ActionRdvResponse();

        var rdv = await _context.RendezVous
            .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(r => r.Service)
            .FirstOrDefaultAsync(r => r.IdRendezVous == rdvId && r.IdPatient == patientId);

        if (rdv == null)
        {
            response.Message = "Rendez-vous introuvable";
            return response;
        }

        if (rdv.Statut != "proposition")
        {
            response.Message = "Ce rendez-vous n'a pas de proposition en attente";
            return response;
        }

        // Vérifier les conflits avec d'autres RDV déjà confirmés
        var conflit = await _context.RendezVous
            .AnyAsync(r => r.IdMedecin == rdv.IdMedecin &&
                          r.IdRendezVous != rdvId &&
                          r.Statut == "confirme" &&
                          r.DateHeure < rdv.DateHeure.AddMinutes(rdv.Duree) &&
                          r.DateHeure.AddMinutes(r.Duree) > rdv.DateHeure);

        if (conflit)
        {
            response.ConflitDetecte = true;
            response.Message = "Ce créneau n'est plus disponible. Veuillez contacter le cabinet.";
            return response;
        }

        // Confirmer le RDV
        rdv.Statut = "confirme";
        rdv.DateModification = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Proposition RDV {rdvId} acceptée par patient {patientId}");

        // Envoyer notification email au médecin
        if (rdv.Medecin?.Utilisateur?.Email != null)
        {
            var patientNom = $"{rdv.Patient?.Utilisateur?.Prenom} {rdv.Patient?.Utilisateur?.Nom}";
            var dateStr = rdv.DateHeure.ToString("dddd d MMMM yyyy à HH:mm", new System.Globalization.CultureInfo("fr-FR"));
            
            await SendEmailAsync(
                rdv.Medecin.Utilisateur.Email,
                "Proposition de créneau acceptée",
                $@"<h2>Proposition acceptée</h2>
                   <p>Le patient <strong>{patientNom}</strong> a accepté votre proposition de rendez-vous.</p>
                   <p><strong>Date :</strong> {dateStr}</p>
                   <p>Le rendez-vous est maintenant confirmé.</p>"
            );
        }

        response.Success = true;
        response.Message = "Rendez-vous confirmé avec succès";
        response.RendezVous = MapToDto(rdv);

        // Notification temps réel
        await _notificationService.NotifyAppointmentUpdatedAsync(
            rdv.IdMedecin, patientId, response.RendezVous);

        return response;
    }

    public async Task<ActionRdvResponse> RefuserPropositionAsync(int patientId, RefuserPropositionRequest request)
    {
        var response = new ActionRdvResponse();

        var rdv = await _context.RendezVous
            .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(r => r.Service)
            .FirstOrDefaultAsync(r => r.IdRendezVous == request.IdRendezVous && r.IdPatient == patientId);

        if (rdv == null)
        {
            response.Message = "Rendez-vous introuvable";
            return response;
        }

        if (rdv.Statut != "proposition")
        {
            response.Message = "Ce rendez-vous n'a pas de proposition en attente";
            return response;
        }

        // Annuler le RDV
        rdv.Statut = "annule";
        rdv.MotifAnnulation = request.Motif ?? "Proposition refusée par le patient";
        rdv.DateModification = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Proposition RDV {request.IdRendezVous} refusée par patient {patientId}");

        // Envoyer notification email au médecin
        if (rdv.Medecin?.Utilisateur?.Email != null)
        {
            var patientNom = $"{rdv.Patient?.Utilisateur?.Prenom} {rdv.Patient?.Utilisateur?.Nom}";
            var dateStr = rdv.DateHeure.ToString("dddd d MMMM yyyy à HH:mm", new System.Globalization.CultureInfo("fr-FR"));
            
            await SendEmailAsync(
                rdv.Medecin.Utilisateur.Email,
                "Proposition de créneau refusée",
                $@"<h2>Proposition refusée</h2>
                   <p>Le patient <strong>{patientNom}</strong> a refusé votre proposition de rendez-vous.</p>
                   <p><strong>Date proposée :</strong> {dateStr}</p>
                   {(!string.IsNullOrEmpty(request.Motif) ? $"<p><strong>Motif :</strong> {request.Motif}</p>" : "")}
                   <p>Veuillez proposer un autre créneau ou contacter le patient.</p>"
            );
        }

        response.Success = true;
        response.Message = "Proposition refusée";
        response.RendezVous = MapToDto(rdv);

        // Notification temps réel
        await _notificationService.NotifyAppointmentUpdatedAsync(
            rdv.IdMedecin, patientId, response.RendezVous);

        return response;
    }

    /// <summary>
    /// Génère un numéro de facture unique
    /// Format: FAC-YYYYMMDD-XXXXX
    /// </summary>
    private async Task<string> GenererNumeroFactureAsync()
    {
        var prefix = $"FAC-{DateTime.UtcNow:yyyyMMdd}-";
        
        var derniereFacture = await _context.Factures
            .Where(f => f.NumeroFacture != null && f.NumeroFacture.StartsWith(prefix))
            .OrderByDescending(f => f.NumeroFacture)
            .Select(f => f.NumeroFacture)
            .FirstOrDefaultAsync();
        
        int nextNumber = 1;
        if (!string.IsNullOrEmpty(derniereFacture))
        {
            var lastNumberStr = derniereFacture.Substring(prefix.Length);
            if (int.TryParse(lastNumberStr, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }
        
        return $"{prefix}{nextNumber:D5}";
    }

    // ==================== PAIEMENT EN LIGNE ====================

    public async Task<List<FacturePatientDto>> GetFacturesPatientEnAttenteAsync(int patientId)
    {
        var factures = await _context.Factures
            .Include(f => f.Consultation)
                .ThenInclude(c => c!.RendezVous)
            .Include(f => f.Medecin)
                .ThenInclude(m => m!.Utilisateur)
            .Include(f => f.Service)
            .Where(f => f.IdPatient == patientId)
            .Where(f => f.Statut == "en_attente" || f.Statut == "partiel")
            .Where(f => f.TypeFacture == "consultation")
            .OrderByDescending(f => f.DateCreation)
            .ToListAsync();

        return factures.Select(f => new FacturePatientDto
        {
            IdFacture = f.IdFacture,
            NumeroFacture = f.NumeroFacture,
            IdRendezVous = f.Consultation?.IdRendezVous,
            DateRendezVous = f.Consultation?.RendezVous?.DateHeure,
            MedecinNom = f.Medecin?.Utilisateur != null 
                ? $"Dr. {f.Medecin.Utilisateur.Prenom} {f.Medecin.Utilisateur.Nom}" 
                : null,
            ServiceNom = f.Service?.NomService,
            MontantTotal = f.MontantTotal,
            MontantRestant = f.MontantRestant,
            Statut = f.Statut ?? "en_attente",
            DateCreation = f.DateCreation,
            DateEcheance = f.DateEcheance,
            CouvertureAssurance = f.CouvertureAssurance,
            TauxCouverture = f.TauxCouverture,
            MontantAssurance = f.MontantAssurance
        }).ToList();
    }

    public async Task<PayerFactureEnLigneResponse> PayerFactureEnLigneAsync(int patientId, PayerFactureEnLigneRequest request)
    {
        var response = new PayerFactureEnLigneResponse();

        // Vérifier la facture
        var facture = await _context.Factures
            .Include(f => f.Consultation)
            .FirstOrDefaultAsync(f => f.IdFacture == request.IdFacture && f.IdPatient == patientId);

        if (facture == null)
        {
            response.Message = "Facture introuvable";
            return response;
        }

        if (facture.Statut == "payee")
        {
            response.Message = "Cette facture est déjà payée";
            return response;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;

            // Générer numéro transaction
            var numeroTransaction = $"TXN-ONLINE-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

            // Créer la transaction (paiement en ligne)
            var newTransaction = new Transaction
            {
                NumeroTransaction = numeroTransaction,
                TransactionUuid = Guid.NewGuid().ToString(),
                IdFacture = request.IdFacture,
                IdPatient = patientId,
                IdCaissier = null, // Paiement en ligne, pas de caissier
                IdSessionCaisse = null,
                Montant = facture.MontantRestant,
                ModePaiement = request.ModePaiement,
                Statut = "complete",
                Reference = request.Reference,
                Notes = request.Notes ?? "Paiement en ligne par le patient",
                MontantRecu = facture.MontantRestant,
                RenduMonnaie = 0,
                EstPaiementPartiel = false
            };

            _context.Transactions.Add(newTransaction);

            // Mettre à jour la facture
            facture.MontantPaye = facture.MontantTotal;
            facture.MontantRestant = 0;
            facture.Statut = "payee";
            facture.DatePaiement = now;

            // Confirmer le RDV associé si existe
            int? idRdv = null;
            string? statutRdv = null;

            if (facture.Consultation?.IdRendezVous.HasValue == true)
            {
                var rdv = await _context.RendezVous
                    .FirstOrDefaultAsync(r => r.IdRendezVous == facture.Consultation.IdRendezVous.Value);

                if (rdv != null && (rdv.Statut == "planifie" || rdv.Statut == "en_attente"))
                {
                    rdv.Statut = "confirme";
                    rdv.DateModification = now;
                    idRdv = rdv.IdRendezVous;
                    statutRdv = "confirme";
                    _logger.LogInformation($"RDV {rdv.IdRendezVous} confirmé après paiement en ligne");
                }
                else if (rdv != null)
                {
                    idRdv = rdv.IdRendezVous;
                    statutRdv = rdv.Statut;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Notification de paiement
            await _notificationService.NotifyFacturePaidAsync(new
            {
                idFacture = facture.IdFacture,
                numeroFacture = facture.NumeroFacture,
                typeFacture = facture.TypeFacture,
                statut = facture.Statut,
                idPatient = facture.IdPatient,
                idMedecin = facture.IdMedecin,
                idService = facture.IdService,
                idSpecialite = facture.IdSpecialite,
                datePaiement = facture.DatePaiement,
                paiementEnLigne = true
            });

            _logger.LogInformation($"Paiement en ligne effectué: Transaction={numeroTransaction}, Facture={facture.NumeroFacture}, Patient={patientId}");

            response.Success = true;
            response.Message = "Paiement effectué avec succès. Votre rendez-vous est confirmé.";
            response.NumeroTransaction = numeroTransaction;
            response.IdRendezVous = idRdv;
            response.StatutRdv = statutRdv;

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError($"Erreur paiement en ligne: {ex.Message}");
            response.Message = "Erreur lors du paiement. Veuillez réessayer.";
            return response;
        }
    }
}
