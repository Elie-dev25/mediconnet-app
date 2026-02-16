using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Core.Enums;
using Mediconnet_Backend.Core.Constants;
using Mediconnet_Backend.DTOs.Hospitalisation;

namespace Mediconnet_Backend.Services;

public class HospitalisationService : IHospitalisationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HospitalisationService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly IAssuranceCouvertureService _assuranceCouvertureService;

    public HospitalisationService(
        ApplicationDbContext context, 
        ILogger<HospitalisationService> logger,
        INotificationService notificationService,
        IEmailService emailService,
        IAssuranceCouvertureService assuranceCouvertureService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
        _emailService = emailService;
        _assuranceCouvertureService = assuranceCouvertureService;
    }

    public async Task<ChambresResponse> GetChambresAsync()
    {
        var chambres = await _context.Chambres
            .Include(c => c.Lits)
            .ToListAsync();

        var chambresDto = chambres.Select(c => new ChambreDto
        {
            IdChambre = c.IdChambre,
            Numero = c.Numero,
            Capacite = c.Capacite,
            Etat = c.Etat,
            Statut = c.Statut,
            LitsDisponibles = c.Lits?.Count(l => l.Statut == "libre") ?? 0,
            LitsOccupes = c.Lits?.Count(l => l.Statut == "occupe") ?? 0,
            Lits = c.Lits?.Select(l => new LitDto
            {
                IdLit = l.IdLit,
                Numero = l.Numero,
                Statut = l.Statut,
                IdChambre = l.IdChambre,
                NumeroChambre = c.Numero,
                EstDisponible = l.Statut == "libre"
            }).ToList()
        }).ToList();

        return new ChambresResponse
        {
            Chambres = chambresDto,
            TotalChambres = chambresDto.Count,
            TotalLits = chambresDto.Sum(c => c.Lits?.Count ?? 0),
            LitsDisponibles = chambresDto.Sum(c => c.LitsDisponibles)
        };
    }

    public async Task<LitsDisponiblesResponse> GetLitsDisponiblesAsync()
    {
        var lits = await _context.Lits
            .Include(l => l.Chambre)
            .Where(l => l.Statut == "libre")
            .ToListAsync();

        var litsDto = lits.Select(l => new LitDto
        {
            IdLit = l.IdLit,
            Numero = l.Numero,
            Statut = l.Statut,
            IdChambre = l.IdChambre,
            NumeroChambre = l.Chambre?.Numero,
            EstDisponible = true
        }).ToList();

        return new LitsDisponiblesResponse
        {
            Lits = litsDto,
            TotalDisponibles = litsDto.Count
        };
    }

    public async Task<List<HospitalisationDto>> GetHospitalisationsAsync(FiltreHospitalisationRequest? filtre = null)
    {
        var query = _context.Hospitalisations
            .Include(h => h.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(h => h.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(h => h.Service)
            .Include(h => h.Lit).ThenInclude(l => l!.Chambre)
            .AsQueryable();

        if (filtre != null)
        {
            if (!string.IsNullOrEmpty(filtre.Statut))
                query = query.Where(h => h.Statut == filtre.Statut);

            if (filtre.IdPatient.HasValue)
                query = query.Where(h => h.IdPatient == filtre.IdPatient.Value);

            if (filtre.IdMedecin.HasValue)
                query = query.Where(h => h.IdMedecin == filtre.IdMedecin.Value);

            if (filtre.IdService.HasValue)
                query = query.Where(h => h.IdService == filtre.IdService.Value);

            if (filtre.DateDebut.HasValue)
                query = query.Where(h => h.DateEntree >= filtre.DateDebut.Value);

            if (filtre.DateFin.HasValue)
                query = query.Where(h => h.DateEntree <= filtre.DateFin.Value);
        }

        var hospitalisations = await query
            .OrderByDescending(h => h.DateEntree)
            .ToListAsync();

        return hospitalisations.Select(MapToDto).ToList();
    }

    public async Task<HospitalisationDto?> GetHospitalisationByIdAsync(int idAdmission)
    {
        var hospitalisation = await _context.Hospitalisations
            .Include(h => h.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(h => h.Lit).ThenInclude(l => l!.Chambre)
            .FirstOrDefaultAsync(h => h.IdAdmission == idAdmission);

        return hospitalisation != null ? MapToDto(hospitalisation) : null;
    }

    public async Task<HospitalisationResponse> CreerHospitalisationAsync(CreerHospitalisationRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Vérifier que le lit existe et est disponible avec Standard
            var lit = await _context.Lits
                .Include(l => l.Chambre)
                .ThenInclude(c => c!.Standard)
                .FirstOrDefaultAsync(l => l.IdLit == request.IdLit);

            if (lit == null)
            {
                return new HospitalisationResponse
                {
                    Success = false,
                    Message = "Lit non trouvé"
                };
            }

            if (lit.Statut != "libre")
            {
                return new HospitalisationResponse
                {
                    Success = false,
                    Message = "Ce lit n'est plus disponible"
                };
            }

            // Vérifier que le patient existe
            var patient = await _context.Patients
                .Include(p => p.Utilisateur)
                .Include(p => p.Assurance)
                .FirstOrDefaultAsync(p => p.IdUser == request.IdPatient);

            if (patient == null)
            {
                return new HospitalisationResponse
                {
                    Success = false,
                    Message = "Patient non trouvé"
                };
            }

            // Vérifier que le médecin est fourni
            if (!request.IdMedecin.HasValue)
            {
                return new HospitalisationResponse
                {
                    Success = false,
                    Message = "Un médecin doit être assigné à l'hospitalisation"
                };
            }

            // Récupérer les infos du médecin
            var medecin = await _context.Medecins
                .Include(m => m.Utilisateur)
                .FirstOrDefaultAsync(m => m.IdUser == request.IdMedecin.Value);

            if (medecin == null)
            {
                return new HospitalisationResponse
                {
                    Success = false,
                    Message = "Médecin non trouvé"
                };
            }

            // Créer l'hospitalisation
            var dateEntree = request.DateEntreePrevue ?? DateTime.Now;
            var hospitalisation = new Hospitalisation
            {
                IdPatient = request.IdPatient,
                IdLit = request.IdLit,
                IdMedecin = request.IdMedecin.Value,
                DateEntree = dateEntree,
                DateSortie = request.DateSortiePrevue,
                Motif = request.Motif,
                Statut = HospitalisationStatut.EnCours.ToDbString()
            };

            _context.Hospitalisations.Add(hospitalisation);

            // Marquer le lit comme occupé
            lit.Statut = LitStatuts.Occupe;

            await _context.SaveChangesAsync();

            // ==================== FACTURATION ====================
            var prixJournalier = lit.Chambre?.Standard?.PrixJournalier ?? 0;
            var dureeEstimee = request.DateSortiePrevue.HasValue 
                ? (int)(request.DateSortiePrevue.Value - dateEntree).TotalDays 
                : 1;
            if (dureeEstimee < 1) dureeEstimee = 1;
            var montantEstime = prixJournalier * dureeEstimee;

            var numeroFacture = $"HOSP-{DateTime.Now:yyyyMMdd}-{hospitalisation.IdAdmission:D4}";
            var couverture = await _assuranceCouvertureService.CalculerCouvertureAsync(patient, "hospitalisation", montantEstime);
            var facture = new Facture
            {
                NumeroFacture = numeroFacture,
                IdPatient = request.IdPatient,
                IdMedecin = request.IdMedecin.Value,
                MontantTotal = montantEstime,
                MontantPaye = 0,
                MontantRestant = couverture.MontantPatient,
                Statut = FactureStatuts.EnAttente,
                TypeFacture = FactureTypes.Hospitalisation,
                DateCreation = DateTime.UtcNow,
                DateEcheance = request.DateSortiePrevue ?? DateTime.Now.AddDays(BusinessRules.FactureHospitalisationEcheanceDays),
                CouvertureAssurance = couverture.EstAssure,
                IdAssurance = couverture.IdAssurance,
                TauxCouverture = couverture.EstAssure ? couverture.TauxCouverture : (decimal?)null,
                MontantAssurance = couverture.EstAssure ? couverture.MontantAssurance : (decimal?)null,
                Notes = $"Hospitalisation - Chambre {lit.Chambre?.Numero}, Lit {lit.Numero}"
            };

            _context.Factures.Add(facture);
            await _context.SaveChangesAsync();

            // Ajouter la ligne de facture
            var ligneFacture = new LigneFacture
            {
                IdFacture = facture.IdFacture,
                Description = $"Hospitalisation {lit.Chambre?.Standard?.Nom ?? "Standard"} - Chambre {lit.Chambre?.Numero}",
                Quantite = dureeEstimee,
                PrixUnitaire = prixJournalier,
                Categorie = "hospitalisation"
            };

            _context.LignesFacture.Add(ligneFacture);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // ==================== NOTIFICATIONS ====================
            var patientNom = $"{patient.Utilisateur?.Prenom} {patient.Utilisateur?.Nom}";
            var medecinNom = $"Dr. {medecin.Utilisateur?.Prenom} {medecin.Utilisateur?.Nom}";
            var standardNom = lit.Chambre?.Standard?.Nom ?? "Standard";
            var numeroChambre = lit.Chambre?.Numero ?? "N/A";
            var numeroLit = lit.Numero ?? "N/A";

            // Notification cloche au patient
            await _notificationService.CreateAsync(new CreateNotificationRequest
            {
                IdUser = request.IdPatient,
                Type = NotificationType.Alerte,
                Titre = "🏥 Hospitalisation ordonnée",
                Message = $"Vous êtes hospitalisé(e) en chambre {numeroChambre}, lit {numeroLit} ({standardNom}). " +
                          $"Prix: {prixJournalier:N0} FCFA/jour. Durée estimée: {dureeEstimee} jour(s). " +
                          $"Montant estimé: {montantEstime:N0} FCFA. Médecin: {medecinNom}",
                Lien = "/patient/factures",
                Priorite = NotificationPriority.Haute,
                SendRealTime = true
            });

            // Email au patient
            if (!string.IsNullOrEmpty(patient.Utilisateur?.Email))
            {
                var emailHtml = GetHospitalisationEmailTemplate(
                    patientNom,
                    medecinNom,
                    standardNom,
                    numeroChambre,
                    numeroLit,
                    dateEntree,
                    request.DateSortiePrevue,
                    request.Motif ?? "Non spécifié",
                    prixJournalier,
                    dureeEstimee,
                    montantEstime,
                    numeroFacture
                );

                await _emailService.SendEmailAsync(
                    patient.Utilisateur.Email,
                    "🏥 Confirmation d'hospitalisation - MediConnect",
                    emailHtml
                );
            }

            _logger.LogInformation("Hospitalisation créée: Patient {PatientId}, Lit {LitId}, Médecin {MedecinId}, Facture {NumeroFacture}", 
                request.IdPatient, request.IdLit, request.IdMedecin, numeroFacture);

            // Construire la réponse avec toutes les données
            var responseData = new HospitalisationCreatedData
            {
                IdAdmission = hospitalisation.IdAdmission,
                IdPatient = hospitalisation.IdPatient,
                IdLit = hospitalisation.IdLit ?? 0,
                NumeroChambre = numeroChambre,
                NumeroLit = numeroLit,
                StandardNom = standardNom,
                PrixJournalier = prixJournalier,
                DateEntree = hospitalisation.DateEntree,
                DateSortiePrevue = hospitalisation.DateSortie,
                Motif = hospitalisation.Motif,
                Statut = hospitalisation.Statut,
                IdFacture = facture.IdFacture,
                NumeroFacture = facture.NumeroFacture,
                MontantEstime = montantEstime,
                DureeEstimeeJours = dureeEstimee
            };

            return new HospitalisationResponse
            {
                Success = true,
                Message = "Hospitalisation créée avec succès",
                IdAdmission = hospitalisation.IdAdmission,
                Data = responseData
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erreur lors de la création de l'hospitalisation");
            return new HospitalisationResponse
            {
                Success = false,
                Message = "Erreur lors de la création de l'hospitalisation"
            };
        }
    }

    public async Task<HospitalisationResponse> DemanderHospitalisationAsync(DemandeHospitalisationRequest request, int medecinId)
    {
        // Vérifier que le lit est fourni (sélection manuelle obligatoire)
        if (request.IdLit <= 0)
        {
            return new HospitalisationResponse
            {
                Success = false,
                Message = "Veuillez sélectionner un lit pour l'hospitalisation"
            };
        }

        // Construire le motif complet avec urgence et notes si présentes
        var motifComplet = request.Motif;
        if (!string.IsNullOrEmpty(request.Urgence))
        {
            motifComplet = $"[{request.Urgence.ToUpper()}] {motifComplet}";
        }
        if (!string.IsNullOrEmpty(request.Notes))
        {
            motifComplet = $"{motifComplet}\nNotes: {request.Notes}";
        }

        // Créer la demande d'hospitalisation via la méthode centralisée
        var creerRequest = new CreerHospitalisationRequest
        {
            IdPatient = request.IdPatient,
            IdLit = request.IdLit,
            IdMedecin = medecinId,
            Motif = motifComplet,
            DateSortiePrevue = request.DateSortiePrevue,
            IdConsultation = request.IdConsultation
        };

        return await CreerHospitalisationAsync(creerRequest);
    }

    public async Task<HospitalisationResponse> TerminerHospitalisationAsync(TerminerHospitalisationRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var hospitalisation = await _context.Hospitalisations
                .Include(h => h.Lit).ThenInclude(l => l!.Chambre).ThenInclude(c => c!.Standard)
                .Include(h => h.Patient).ThenInclude(p => p!.Utilisateur)
                .Include(h => h.Patient).ThenInclude(p => p!.Assurance)
                .Include(h => h.Medecin).ThenInclude(m => m!.Utilisateur)
                .FirstOrDefaultAsync(h => h.IdAdmission == request.IdAdmission);

            if (hospitalisation == null)
            {
                return new HospitalisationResponse
                {
                    Success = false,
                    Message = "Hospitalisation non trouvée"
                };
            }

            // Vérifier que l'hospitalisation est en cours
            if (!string.Equals(hospitalisation.Statut, HospitalisationStatut.EnCours.ToDbString(), StringComparison.OrdinalIgnoreCase))
            {
                return new HospitalisationResponse
                {
                    Success = false,
                    Message = $"Impossible de terminer: l'hospitalisation n'est pas en cours (statut actuel: {hospitalisation.Statut})"
                };
            }

            // Vérifier que le résumé médical est fourni
            if (string.IsNullOrWhiteSpace(request.ResumeMedical))
            {
                return new HospitalisationResponse
                {
                    Success = false,
                    Message = "Le résumé médical de sortie est obligatoire"
                };
            }

            // Annuler automatiquement les examens en cours/attente
            var examensEnCours = await _context.Set<BulletinExamen>()
                .Where(b => b.IdHospitalisation == request.IdAdmission 
                    && b.Statut != "termine" && b.Statut != "annule" && b.Statut != null)
                .ToListAsync();

            foreach (var examen in examensEnCours)
            {
                examen.Statut = "annule";
            }

            // Mettre à jour l'hospitalisation
            hospitalisation.DateSortie = request.DateSortie ?? DateTime.UtcNow;
            hospitalisation.Statut = HospitalisationStatut.Termine.ToDbString();
            hospitalisation.MotifSortie = request.MotifSortie;
            hospitalisation.ResumeMedical = request.ResumeMedical;

            // Libérer le lit
            if (hospitalisation.Lit != null)
            {
                hospitalisation.Lit.Statut = LitStatuts.Libre;
            }

            // Annuler les soins encore en cours/prescrits
            var soinsActifs = await _context.SoinsHospitalisation
                .Where(s => s.IdHospitalisation == request.IdAdmission 
                    && (s.Statut == "prescrit" || s.Statut == "en_cours"))
                .ToListAsync();

            foreach (var soin in soinsActifs)
            {
                soin.Statut = "termine";
            }

            // Annuler les exécutions de soins prévues non effectuées
            var executionsPrevu = await _context.ExecutionsSoins
                .Where(e => e.Soin!.IdHospitalisation == request.IdAdmission && e.Statut == "prevu")
                .ToListAsync();

            foreach (var exec in executionsPrevu)
            {
                exec.Statut = "annule";
                exec.UpdatedAt = DateTime.UtcNow;
            }

            // ==================== RECALCUL FACTURE ====================
            // Recalculer la facture avec la durée réelle du séjour
            var factureHospit = await _context.Factures
                .Include(f => f.Lignes)
                .FirstOrDefaultAsync(f => f.IdPatient == hospitalisation.IdPatient
                    && f.TypeFacture == FactureTypes.Hospitalisation
                    && f.NumeroFacture.StartsWith("HOSP-")
                    && f.NumeroFacture.EndsWith($"-{hospitalisation.IdAdmission:D4}")
                    && f.Statut != "annulee");

            if (factureHospit != null)
            {
                var dateSortieReelle = hospitalisation.DateSortie ?? DateTime.UtcNow;
                var dureeReelle = (int)(dateSortieReelle - hospitalisation.DateEntree).TotalDays;
                if (dureeReelle < 1) dureeReelle = 1;

                var prixJournalier = hospitalisation.Lit?.Chambre?.Standard?.PrixJournalier ?? 0;
                var montantReel = prixJournalier * dureeReelle;

                // Recalculer la couverture assurance avec le montant réel
                var couvertureSortie = await _assuranceCouvertureService.CalculerCouvertureAsync(hospitalisation.Patient!, "hospitalisation", montantReel);

                factureHospit.MontantTotal = montantReel;
                factureHospit.CouvertureAssurance = couvertureSortie.EstAssure;
                factureHospit.IdAssurance = couvertureSortie.IdAssurance;
                factureHospit.TauxCouverture = couvertureSortie.EstAssure ? couvertureSortie.TauxCouverture : (decimal?)null;
                factureHospit.MontantAssurance = couvertureSortie.EstAssure ? couvertureSortie.MontantAssurance : (decimal?)null;
                factureHospit.MontantRestant = couvertureSortie.MontantPatient - factureHospit.MontantPaye;
                if (factureHospit.MontantRestant < 0) factureHospit.MontantRestant = 0;
                factureHospit.Notes = $"Hospitalisation terminée - Durée réelle: {dureeReelle} jour(s) - Chambre {hospitalisation.Lit?.Chambre?.Numero}";

                // Mettre à jour la ligne de facture
                var ligneHospit = factureHospit.Lignes.FirstOrDefault(l => l.Categorie == "hospitalisation");
                if (ligneHospit != null)
                {
                    ligneHospit.Quantite = dureeReelle;
                    ligneHospit.PrixUnitaire = prixJournalier;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Notification au patient
            var patientNom = $"{hospitalisation.Patient?.Utilisateur?.Prenom} {hospitalisation.Patient?.Utilisateur?.Nom}";
            var medecinNom = $"Dr. {hospitalisation.Medecin?.Utilisateur?.Prenom} {hospitalisation.Medecin?.Utilisateur?.Nom}";

            await _notificationService.CreateAsync(new CreateNotificationRequest
            {
                IdUser = hospitalisation.IdPatient,
                Type = "alerte",
                Titre = "Hospitalisation terminée",
                Message = $"Votre hospitalisation a été clôturée par {medecinNom}. " +
                          $"Motif de sortie: {request.MotifSortie ?? "Non spécifié"}.",
                Lien = "/patient/dossier-medical",
                Priorite = "haute",
                SendRealTime = true
            });

            _logger.LogInformation("Hospitalisation terminée: {IdAdmission}, Motif: {MotifSortie}", 
                request.IdAdmission, request.MotifSortie);

            return new HospitalisationResponse
            {
                Success = true,
                Message = "Hospitalisation terminée avec succès",
                IdAdmission = hospitalisation.IdAdmission
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erreur lors de la terminaison de l'hospitalisation");
            return new HospitalisationResponse
            {
                Success = false,
                Message = "Erreur lors de la terminaison de l'hospitalisation"
            };
        }
    }

    public async Task<List<HospitalisationDto>> GetHospitalisationsPatientAsync(int idPatient)
    {
        var hospitalisations = await _context.Hospitalisations
            .Include(h => h.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(h => h.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(h => h.Service)
            .Include(h => h.Lit).ThenInclude(l => l!.Chambre)
            .Where(h => h.IdPatient == idPatient)
            .OrderByDescending(h => h.DateEntree)
            .ToListAsync();

        return hospitalisations.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Médecin ordonne une hospitalisation SANS choisir de lit
    /// Le patient et le Major du service sont notifiés
    /// </summary>
    public async Task<HospitalisationResponse> OrdonnerHospitalisationAsync(OrdonnerHospitalisationRequest request, int medecinId)
    {
        try
        {
            // Récupérer le médecin et son service
            var medecin = await _context.Medecins
                .Include(m => m.Utilisateur)
                .Include(m => m.Service)
                .FirstOrDefaultAsync(m => m.IdUser == medecinId);

            if (medecin == null)
            {
                return new HospitalisationResponse { Success = false, Message = "Médecin non trouvé" };
            }

            // Récupérer le patient
            var patient = await _context.Patients
                .Include(p => p.Utilisateur)
                .FirstOrDefaultAsync(p => p.IdUser == request.IdPatient);

            if (patient == null)
            {
                return new HospitalisationResponse { Success = false, Message = "Patient non trouvé" };
            }

            // Déterminer le service cible (priorité: IdServiceCible > service du médecin)
            var idServiceCible = request.IdServiceCible ?? medecin.IdService;

            // Créer l'hospitalisation en attente de lit
            var hospitalisation = new Hospitalisation
            {
                IdPatient = request.IdPatient,
                IdMedecin = medecinId,
                IdLit = null, // Pas de lit assigné
                IdService = idServiceCible,
                IdConsultation = request.IdConsultation,
                DateEntree = DateTime.UtcNow,
                DateSortie = request.DateSortiePrevue,
                Motif = request.Motif,
                Urgence = request.Urgence ?? NiveauUrgence.Normale.ToDbString(),
                DiagnosticPrincipal = request.DiagnosticPrincipal,
                Statut = HospitalisationStatut.EnAttente.ToDbString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Hospitalisations.Add(hospitalisation);
            await _context.SaveChangesAsync();

            // Créer les soins dans la table soin_hospitalisation
            if (request.Soins?.Any() == true)
            {
                foreach (var soin in request.Soins)
                {
                    var soinEntity = new SoinHospitalisation
                    {
                        IdHospitalisation = hospitalisation.IdAdmission,
                        TypeSoin = soin.TypeSoin,
                        Description = soin.Description,
                        Frequence = soin.Frequence,
                        DureeJours = 7,
                        Moments = "matin",
                        Priorite = soin.Priorite,
                        Instructions = soin.Instructions,
                        Statut = SoinStatut.Prescrit.ToDbString(),
                        DatePrescription = DateTime.UtcNow,
                        DateDebut = DateTime.UtcNow.Date,
                        IdPrescripteur = medecinId
                    };
                    _context.SoinsHospitalisation.Add(soinEntity);
                }
                await _context.SaveChangesAsync();
            }

            var patientNom = $"{patient.Utilisateur?.Prenom} {patient.Utilisateur?.Nom}";
            var medecinNom = $"Dr. {medecin.Utilisateur?.Prenom} {medecin.Utilisateur?.Nom}";
            var serviceNom = medecin.Service?.NomService ?? "Service";

            // Notification cloche au patient
            await _notificationService.CreateAsync(new CreateNotificationRequest
            {
                IdUser = request.IdPatient,
                Type = NotificationType.Alerte,
                Titre = "🏥 Hospitalisation ordonnée",
                Message = $"Une hospitalisation a été ordonnée par {medecinNom}. " +
                          $"Motif: {request.Motif}. " +
                          $"Un lit vous sera attribué prochainement par le service {serviceNom}.",
                Lien = "/patient/hospitalisations",
                Priorite = NotificationPriority.Haute,
                SendRealTime = true
            });

            // Email au patient
            if (!string.IsNullOrEmpty(patient.Utilisateur?.Email))
            {
                var emailHtml = GetHospitalisationOrdonneEmailTemplate(
                    patientNom, medecinNom, serviceNom, request.Motif, 
                    request.Urgence ?? "normale", hospitalisation.DateEntree);

                await _emailService.SendEmailAsync(
                    patient.Utilisateur.Email,
                    "🏥 Hospitalisation ordonnée - MediConnect",
                    emailHtml
                );
            }

            // Trouver le Major du service CIBLE pour le notifier (via Service.IdMajor)
            var serviceCible = await _context.Services
                .FirstOrDefaultAsync(s => s.IdService == idServiceCible);

            if (serviceCible?.IdMajor != null)
            {
                await _notificationService.CreateAsync(new CreateNotificationRequest
                {
                    IdUser = serviceCible.IdMajor.Value,
                    Type = NotificationType.Alerte,
                    Titre = "🛏️ Nouvelle hospitalisation à attribuer",
                    Message = $"Patient: {patientNom}. Médecin: {medecinNom}. " +
                              $"Urgence: {request.Urgence ?? "normale"}. " +
                              $"Veuillez attribuer un lit au patient.",
                    Lien = "/infirmier/patients?tab=hospitalises",
                    Priorite = request.Urgence == "critique" ? NotificationPriority.Urgente : NotificationPriority.Haute,
                    SendRealTime = true
                });
            }

            _logger.LogInformation("Hospitalisation ordonnée: Patient {PatientId}, Médecin {MedecinId}, Service {ServiceId}", 
                request.IdPatient, medecinId, idServiceCible);

            return new HospitalisationResponse
            {
                Success = true,
                Message = "Hospitalisation ordonnée avec succès. En attente d'attribution de lit par le Major.",
                IdAdmission = hospitalisation.IdAdmission
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'ordonnance d'hospitalisation");
            return new HospitalisationResponse
            {
                Success = false,
                Message = "Erreur lors de l'ordonnance de l'hospitalisation"
            };
        }
    }

    /// <summary>
    /// Ordonne une hospitalisation complète avec prescriptions (examens, médicaments, soins)
    /// </summary>
    public async Task<HospitalisationResponse> OrdonnerHospitalisationCompleteAsync(OrdonnerHospitalisationCompleteRequest request, int medecinId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Récupérer le médecin et son service
            var medecin = await _context.Medecins
                .Include(m => m.Utilisateur)
                .Include(m => m.Service)
                .FirstOrDefaultAsync(m => m.IdUser == medecinId);

            if (medecin == null)
            {
                return new HospitalisationResponse { Success = false, Message = "Médecin non trouvé" };
            }

            // Récupérer le patient
            var patient = await _context.Patients
                .Include(p => p.Utilisateur)
                .FirstOrDefaultAsync(p => p.IdUser == request.IdPatient);

            if (patient == null)
            {
                return new HospitalisationResponse { Success = false, Message = "Patient non trouvé" };
            }

            // Déterminer le service cible (priorité: IdServiceCible > service du médecin)
            var idServiceCible = request.IdServiceCible ?? medecin.IdService;

            // Créer l'hospitalisation en attente de lit
            var hospitalisation = new Hospitalisation
            {
                IdPatient = request.IdPatient,
                IdMedecin = medecinId,
                IdLit = null,
                IdService = idServiceCible,
                IdConsultation = request.IdConsultation,
                DateEntree = DateTime.UtcNow,
                DateSortie = request.DateSortiePrevue,
                Motif = request.Motif,
                Urgence = request.Urgence ?? NiveauUrgence.Normale.ToDbString(),
                DiagnosticPrincipal = request.DiagnosticPrincipal,
                Statut = HospitalisationStatut.EnAttente.ToDbString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Hospitalisations.Add(hospitalisation);
            await _context.SaveChangesAsync();

            // Créer les soins dans la table soin_hospitalisation
            if (request.Soins?.Any() == true)
            {
                foreach (var soin in request.Soins)
                {
                    var soinEntity = new SoinHospitalisation
                    {
                        IdHospitalisation = hospitalisation.IdAdmission,
                        TypeSoin = soin.TypeSoin,
                        Description = soin.Description,
                        Frequence = soin.Frequence,
                        DureeJours = 7,
                        Moments = "matin",
                        Priorite = soin.Priorite,
                        Instructions = soin.Instructions,
                        Statut = SoinStatut.Prescrit.ToDbString(),
                        DatePrescription = DateTime.UtcNow,
                        DateDebut = DateTime.UtcNow.Date,
                        IdPrescripteur = medecinId
                    };
                    _context.SoinsHospitalisation.Add(soinEntity);
                }
                await _context.SaveChangesAsync();
            }

            // Enregistrer les examens dans la table bulletin_examen
            if (request.Examens?.Any() == true)
            {
                foreach (var examen in request.Examens)
                {
                    // Rechercher l'examen dans le catalogue
                    var examenCatalogue = await _context.ExamensCatalogue
                        .FirstOrDefaultAsync(e => e.NomExamen.Contains(examen.NomExamen) || examen.NomExamen.Contains(e.NomExamen));

                    var bulletinExamen = new BulletinExamen
                    {
                        DateDemande = DateTime.UtcNow,
                        IdHospitalisation = hospitalisation.IdAdmission,
                        IdConsultation = request.IdConsultation,
                        IdExamen = examenCatalogue?.IdExamen,
                        Instructions = examen.Notes,
                        Urgence = examen.Urgence
                    };
                    _context.BulletinsExamen.Add(bulletinExamen);
                }
                await _context.SaveChangesAsync();
            }

            // Enregistrer les médicaments dans prescription/prescription_medicament
            if (request.Medicaments?.Any() == true && request.IdConsultation.HasValue)
            {
                // Créer une ordonnance liée à la consultation
                var ordonnance = new Ordonnance
                {
                    Date = DateTime.UtcNow,
                    IdConsultation = request.IdConsultation.Value,
                    Commentaire = $"Prescription hospitalisation #{hospitalisation.IdAdmission}"
                };
                _context.Ordonnances.Add(ordonnance);
                await _context.SaveChangesAsync();

                foreach (var med in request.Medicaments)
                {
                    // Rechercher le médicament dans le catalogue
                    var medicament = await _context.Medicaments
                        .FirstOrDefaultAsync(m => m.Nom.Contains(med.NomMedicament) || med.NomMedicament.Contains(m.Nom));

                    if (medicament != null)
                    {
                        var prescriptionMed = new PrescriptionMedicament
                        {
                            IdOrdonnance = ordonnance.IdOrdonnance,
                            IdMedicament = medicament.IdMedicament,
                            Quantite = med.Quantite ?? 1,
                            Posologie = med.Posologie,
                            DureeTraitement = med.DureeTraitement,
                            VoieAdministration = med.VoieAdministration,
                            FormePharmaceutique = med.FormePharmaceutique,
                            Instructions = med.Instructions
                        };
                        _context.PrescriptionMedicaments.Add(prescriptionMed);
                    }
                }
                await _context.SaveChangesAsync();
            }
            await transaction.CommitAsync();

            var patientNom = $"{patient.Utilisateur?.Prenom} {patient.Utilisateur?.Nom}";
            var medecinNom = $"Dr. {medecin.Utilisateur?.Prenom} {medecin.Utilisateur?.Nom}";
            var serviceNom = medecin.Service?.NomService ?? "Service";

            // Notifications
            await _notificationService.CreateAsync(new CreateNotificationRequest
            {
                IdUser = request.IdPatient,
                Type = NotificationType.Alerte,
                Titre = "🏥 Hospitalisation ordonnée",
                Message = $"Une hospitalisation complète a été ordonnée par {medecinNom}. " +
                          $"Motif: {request.Motif}. " +
                          (request.Examens?.Any() == true ? $"Examens: {request.Examens.Count}. " : "") +
                          (request.Medicaments?.Any() == true ? $"Médicaments: {request.Medicaments.Count}. " : "") +
                          $"Un lit vous sera attribué par le service {serviceNom}.",
                Lien = "/patient/hospitalisations",
                Priorite = NotificationPriority.Haute,
                SendRealTime = true
            });

            // Notifier le Major du service CIBLE (via Service.IdMajor)
            var serviceNotif = await _context.Services
                .FirstOrDefaultAsync(s => s.IdService == idServiceCible);

            if (serviceNotif?.IdMajor != null)
            {
                await _notificationService.CreateAsync(new CreateNotificationRequest
                {
                    IdUser = serviceNotif.IdMajor.Value,
                    Type = NotificationType.Alerte,
                    Titre = "🛏️ Nouvelle hospitalisation complète",
                    Message = $"Patient: {patientNom}. Médecin: {medecinNom}. " +
                              $"Urgence: {request.Urgence}. " +
                              (request.Examens?.Any() == true ? $"Avec {request.Examens.Count} examen(s). " : "") +
                              (request.Medicaments?.Any() == true ? $"Et {request.Medicaments.Count} médicament(s). " : "") +
                              "Veuillez attribuer un lit.",
                    Lien = "/infirmier/patients?tab=hospitalises",
                    Priorite = request.Urgence == "critique" ? NotificationPriority.Urgente : NotificationPriority.Haute,
                    SendRealTime = true
                });
            }

            _logger.LogInformation("Hospitalisation complète ordonnée: Patient {PatientId}, Examens: {NbExamens}, Médicaments: {NbMedicaments}", 
                request.IdPatient, request.Examens?.Count ?? 0, request.Medicaments?.Count ?? 0);

            return new HospitalisationResponse
            {
                Success = true,
                Message = "Hospitalisation complète ordonnée avec succès.",
                IdAdmission = hospitalisation.IdAdmission
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erreur lors de l'ordonnance d'hospitalisation complète");
            return new HospitalisationResponse
            {
                Success = false,
                Message = "Erreur lors de l'ordonnance de l'hospitalisation complète"
            };
        }
    }

    /// <summary>
    /// Major attribue un lit à une hospitalisation en attente
    /// Le Major est identifié via Service.IdMajor
    /// </summary>
    public async Task<HospitalisationResponse> AttribuerLitAsync(AttribuerLitRequest request, int majorId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Vérifier que l'utilisateur est Major d'un service (via Service.IdMajor)
            var serviceMajor = await _context.Services
                .FirstOrDefaultAsync(s => s.IdMajor == majorId);

            if (serviceMajor == null)
            {
                return new HospitalisationResponse { Success = false, Message = "Vous n'êtes pas autorisé à attribuer des lits (non Major)" };
            }

            // Récupérer l'hospitalisation
            var hospitalisation = await _context.Hospitalisations
                .Include(h => h.Patient).ThenInclude(p => p!.Utilisateur)
                .Include(h => h.Patient).ThenInclude(p => p!.Assurance)
                .Include(h => h.Medecin).ThenInclude(m => m!.Utilisateur)
                .FirstOrDefaultAsync(h => h.IdAdmission == request.IdAdmission);

            if (hospitalisation == null)
            {
                return new HospitalisationResponse { Success = false, Message = "Hospitalisation non trouvée" };
            }

            // Vérifier le statut EN_ATTENTE
            if (!string.Equals(hospitalisation.Statut, HospitalisationStatut.EnAttente.ToDbString(), StringComparison.OrdinalIgnoreCase))
            {
                return new HospitalisationResponse { Success = false, Message = $"Cette hospitalisation n'est pas en attente (statut actuel: {hospitalisation.Statut})" };
            }

            // Vérifier que le Major est bien du même service que l'hospitalisation
            if (serviceMajor.IdService != hospitalisation.IdService)
            {
                return new HospitalisationResponse { Success = false, Message = "Vous ne pouvez attribuer des lits que pour votre service" };
            }

            // Vérifier le lit
            var lit = await _context.Lits
                .Include(l => l.Chambre)
                .ThenInclude(c => c!.Standard)
                .FirstOrDefaultAsync(l => l.IdLit == request.IdLit);

            if (lit == null)
            {
                return new HospitalisationResponse { Success = false, Message = "Lit non trouvé" };
            }

            if (lit.Statut != LitStatuts.Libre)
            {
                return new HospitalisationResponse { Success = false, Message = "Ce lit n'est plus disponible" };
            }

            // Attribuer le lit
            hospitalisation.IdLit = request.IdLit;
            hospitalisation.Statut = HospitalisationStatut.EnCours.ToDbString();

            // Marquer le lit comme occupé
            lit.Statut = LitStatuts.Occupe;

            await _context.SaveChangesAsync();

            // Créer la facture (seulement si aucune facture d'hospitalisation n'existe déjà pour cette admission)
            var prixJournalier = lit.Chambre?.Standard?.PrixJournalier ?? 0;
            var dureeEstimee = hospitalisation.DateSortie.HasValue 
                ? (int)(hospitalisation.DateSortie.Value - hospitalisation.DateEntree).TotalDays 
                : 1;
            if (dureeEstimee < 1) dureeEstimee = 1;
            var montantEstime = prixJournalier * dureeEstimee;

            var factureExistante = await _context.Factures
                .Include(f => f.Lignes)
                .FirstOrDefaultAsync(f => f.IdPatient == hospitalisation.IdPatient 
                    && f.TypeFacture == FactureTypes.Hospitalisation
                    && f.NumeroFacture.StartsWith($"HOSP-")
                    && f.NumeroFacture.EndsWith($"-{hospitalisation.IdAdmission:D4}")
                    && f.Statut != "annulee");

            var numeroFacture = $"HOSP-{DateTime.Now:yyyyMMdd}-{hospitalisation.IdAdmission:D4}";
            Facture facture;

            // Calculer la couverture assurance pour l'hospitalisation
            var couvertureHosp = await _assuranceCouvertureService.CalculerCouvertureAsync(hospitalisation.Patient!, "hospitalisation", montantEstime);

            if (factureExistante != null)
            {
                // Mettre à jour la facture existante avec les infos du nouveau lit
                facture = factureExistante;
                facture.MontantTotal = montantEstime;
                facture.CouvertureAssurance = couvertureHosp.EstAssure;
                facture.IdAssurance = couvertureHosp.IdAssurance;
                facture.TauxCouverture = couvertureHosp.EstAssure ? couvertureHosp.TauxCouverture : (decimal?)null;
                facture.MontantAssurance = couvertureHosp.EstAssure ? couvertureHosp.MontantAssurance : (decimal?)null;
                facture.MontantRestant = couvertureHosp.MontantPatient - facture.MontantPaye;
                facture.Notes = $"Hospitalisation - Chambre {lit.Chambre?.Numero}, Lit {lit.Numero}";
                numeroFacture = facture.NumeroFacture;

                // Mettre à jour la ligne de facture existante
                var ligneExistante = facture.Lignes.FirstOrDefault(l => l.Categorie == "hospitalisation");
                if (ligneExistante != null)
                {
                    ligneExistante.Description = $"Hospitalisation {lit.Chambre?.Standard?.Nom ?? "Standard"} - Chambre {lit.Chambre?.Numero}";
                    ligneExistante.Quantite = dureeEstimee;
                    ligneExistante.PrixUnitaire = prixJournalier;
                }
            }
            else
            {
                facture = new Facture
                {
                    NumeroFacture = numeroFacture,
                    IdPatient = hospitalisation.IdPatient,
                    IdMedecin = hospitalisation.IdMedecin,
                    IdService = hospitalisation.IdService,
                    MontantTotal = montantEstime,
                    MontantPaye = 0,
                    MontantRestant = couvertureHosp.MontantPatient,
                    Statut = FactureStatuts.EnAttente,
                    TypeFacture = FactureTypes.Hospitalisation,
                    DateCreation = DateTime.UtcNow,
                    DateEcheance = hospitalisation.DateSortie ?? DateTime.Now.AddDays(BusinessRules.FactureHospitalisationEcheanceDays),
                    CouvertureAssurance = couvertureHosp.EstAssure,
                    IdAssurance = couvertureHosp.IdAssurance,
                    TauxCouverture = couvertureHosp.EstAssure ? couvertureHosp.TauxCouverture : (decimal?)null,
                    MontantAssurance = couvertureHosp.EstAssure ? couvertureHosp.MontantAssurance : (decimal?)null,
                    Notes = $"Hospitalisation - Chambre {lit.Chambre?.Numero}, Lit {lit.Numero}"
                };

                _context.Factures.Add(facture);
                await _context.SaveChangesAsync();

                var ligneFacture = new LigneFacture
                {
                    IdFacture = facture.IdFacture,
                    Description = $"Hospitalisation {lit.Chambre?.Standard?.Nom ?? "Standard"} - Chambre {lit.Chambre?.Numero}",
                    Quantite = dureeEstimee,
                    PrixUnitaire = prixJournalier,
                    Categorie = "hospitalisation"
                };

                _context.LignesFacture.Add(ligneFacture);
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            var patientNom = $"{hospitalisation.Patient?.Utilisateur?.Prenom} {hospitalisation.Patient?.Utilisateur?.Nom}";
            var numeroChambre = lit.Chambre?.Numero ?? "N/A";
            var numeroLit = lit.Numero ?? "N/A";
            var standardNom = lit.Chambre?.Standard?.Nom ?? "Standard";

            // Notifier le patient que le lit est attribué
            await _notificationService.CreateAsync(new CreateNotificationRequest
            {
                IdUser = hospitalisation.IdPatient,
                Type = NotificationType.Alerte,
                Titre = "🛏️ Lit attribué",
                Message = $"Votre lit a été attribué: Chambre {numeroChambre}, Lit {numeroLit} ({standardNom}). " +
                          $"Prix: {prixJournalier:N0} FCFA/jour.",
                Lien = "/patient/factures",
                Priorite = NotificationPriority.Haute,
                SendRealTime = true
            });

            // Email au patient avec les détails complets
            if (!string.IsNullOrEmpty(hospitalisation.Patient?.Utilisateur?.Email))
            {
                var medecinNom = $"Dr. {hospitalisation.Medecin?.Utilisateur?.Prenom} {hospitalisation.Medecin?.Utilisateur?.Nom}";
                var emailHtml = GetHospitalisationEmailTemplate(
                    patientNom, medecinNom, standardNom, numeroChambre, numeroLit,
                    hospitalisation.DateEntree, hospitalisation.DateSortie,
                    hospitalisation.Motif ?? "Non spécifié",
                    prixJournalier, dureeEstimee, montantEstime, numeroFacture
                );

                await _emailService.SendEmailAsync(
                    hospitalisation.Patient.Utilisateur.Email,
                    "🛏️ Lit attribué - Confirmation d'hospitalisation - MediConnect",
                    emailHtml
                );
            }

            _logger.LogInformation("Lit attribué: Hospitalisation {IdAdmission}, Lit {IdLit}, Major {MajorId}", 
                request.IdAdmission, request.IdLit, majorId);

            return new HospitalisationResponse
            {
                Success = true,
                Message = $"Lit attribué avec succès. Chambre {numeroChambre}, Lit {numeroLit}.",
                IdAdmission = hospitalisation.IdAdmission,
                Data = new HospitalisationCreatedData
                {
                    IdAdmission = hospitalisation.IdAdmission,
                    IdPatient = hospitalisation.IdPatient,
                    IdLit = request.IdLit,
                    NumeroChambre = numeroChambre,
                    NumeroLit = numeroLit,
                    StandardNom = standardNom,
                    PrixJournalier = prixJournalier,
                    DateEntree = hospitalisation.DateEntree,
                    DateSortiePrevue = hospitalisation.DateSortie,
                    Motif = hospitalisation.Motif,
                    Statut = hospitalisation.Statut,
                    IdFacture = facture.IdFacture,
                    NumeroFacture = facture.NumeroFacture,
                    MontantEstime = montantEstime,
                    DureeEstimeeJours = dureeEstimee
                }
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erreur lors de l'attribution du lit");
            return new HospitalisationResponse
            {
                Success = false,
                Message = "Erreur lors de l'attribution du lit"
            };
        }
    }

    /// <summary>
    /// Récupérer les hospitalisations EN_ATTENTE (pour le Major)
    /// Filtre obligatoire par service
    /// </summary>
    public async Task<List<HospitalisationDto>> GetHospitalisationsEnAttenteAsync(int? idService = null)
    {
        _logger.LogInformation("GetHospitalisationsEnAttenteAsync - Service: {ServiceId}", idService);

        var query = _context.Hospitalisations
            .Include(h => h.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(h => h.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(h => h.Service)
            .Where(h => h.Statut == HospitalisationStatut.EnAttente.ToDbString());

        if (idService.HasValue)
        {
            query = query.Where(h => h.IdService == idService.Value);
        }
        else
        {
            _logger.LogWarning("GetHospitalisationsEnAttenteAsync appelé sans idService - retourne liste vide");
            return new List<HospitalisationDto>();
        }

        var hospitalisations = await query
            .OrderByDescending(h => h.Urgence == NiveauUrgence.Critique.ToDbString())
            .ThenByDescending(h => h.Urgence == NiveauUrgence.Urgente.ToDbString())
            .ThenBy(h => h.DateEntree)
            .ToListAsync();

        _logger.LogInformation("GetHospitalisationsEnAttenteAsync - Trouvé {Count} hospitalisations EN_ATTENTE", hospitalisations.Count);

        return hospitalisations.Select(MapToDto).ToList();
    }

    private static HospitalisationDto MapToDto(Hospitalisation h)
    {
        return new HospitalisationDto
        {
            IdAdmission = h.IdAdmission,
            DateEntree = h.DateEntree,
            DateSortiePrevue = h.DateSortiePrevue ?? h.DateSortie,
            DateSortie = h.Statut == HospitalisationStatut.Termine.ToDbString() ? h.DateSortie : null,
            Motif = h.Motif,
            MotifSortie = h.MotifSortie,
            ResumeMedical = h.ResumeMedical,
            DiagnosticPrincipal = h.DiagnosticPrincipal,
            Statut = h.Statut,
            Urgence = h.Urgence,
            IdPatient = h.IdPatient,
            PatientNom = h.Patient?.Utilisateur?.Nom,
            PatientPrenom = h.Patient?.Utilisateur?.Prenom,
            PatientNumeroDossier = h.Patient?.NumeroDossier,
            IdLit = h.IdLit ?? 0,
            NumeroLit = h.Lit?.Numero,
            NumeroChambre = h.Lit?.Chambre?.Numero,
            IdService = h.IdService,
            ServiceNom = h.Service?.NomService,
            IdMedecin = h.IdMedecin,
            MedecinNom = h.Medecin?.Utilisateur != null 
                ? $"Dr. {h.Medecin.Utilisateur.Prenom} {h.Medecin.Utilisateur.Nom}" 
                : null,
            IdConsultation = h.IdConsultation,
            DureeJours = h.DateSortie.HasValue 
                ? (int)(h.DateSortie.Value - h.DateEntree).TotalDays 
                : (int)(DateTime.UtcNow - h.DateEntree).TotalDays
        };
    }

    #region Email Templates

    private static string GetHospitalisationOrdonneEmailTemplate(
        string patientNom,
        string medecinNom,
        string serviceNom,
        string motif,
        string urgence,
        DateTime dateOrdonnance)
    {
        var urgenceColor = urgence switch
        {
            "critique" => "#dc2626",
            "urgente" => "#f59e0b",
            _ => "#10b981"
        };

        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Hospitalisation ordonnée</title>
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #0e7490 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: 600;"">🏥 MediConnect</h1>
                            <p style=""color: #e0f2fe; margin: 10px 0 0; font-size: 16px;"">Hospitalisation ordonnée</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937; margin: 0 0 20px; font-size: 24px;"">Bonjour {patientNom} 👋</h2>
                            
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6; margin: 0 0 25px;"">
                                Une hospitalisation a été ordonnée par <strong>{medecinNom}</strong>.
                            </p>

                            <div style=""background-color: #fef3c7; border-radius: 12px; padding: 25px; margin-bottom: 25px; border-left: 4px solid #d97706;"">
                                <h3 style=""color: #92400e; margin: 0 0 15px; font-size: 18px;"">⏳ En attente d'attribution de lit</h3>
                                <p style=""color: #78716c; margin: 0;"">
                                    Un lit vous sera attribué prochainement par le service <strong>{serviceNom}</strong>.
                                    Vous recevrez une notification dès que votre chambre sera prête.
                                </p>
                            </div>

                            <div style=""background-color: #f3f4f6; border-radius: 12px; padding: 25px; margin-bottom: 25px;"">
                                <h3 style=""color: #374151; margin: 0 0 15px; font-size: 18px;"">📋 Détails</h3>
                                <table style=""width: 100%; border-collapse: collapse;"">
                                    <tr>
                                        <td style=""padding: 8px 0; color: #6b7280;"">Motif:</td>
                                        <td style=""padding: 8px 0; color: #1f2937; font-weight: 600; text-align: right;"">{motif}</td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 8px 0; color: #6b7280;"">Urgence:</td>
                                        <td style=""padding: 8px 0; font-weight: 600; text-align: right;"">
                                            <span style=""background-color: {urgenceColor}; color: white; padding: 4px 12px; border-radius: 12px; font-size: 12px;"">{urgence.ToUpper()}</span>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 8px 0; color: #6b7280;"">Date:</td>
                                        <td style=""padding: 8px 0; color: #1f2937; font-weight: 600; text-align: right;"">{dateOrdonnance:dd/MM/yyyy à HH:mm}</td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 8px 0; color: #6b7280;"">Service:</td>
                                        <td style=""padding: 8px 0; color: #1f2937; font-weight: 600; text-align: right;"">{serviceNom}</td>
                                    </tr>
                                </table>
                            </div>

                            <div style=""background-color: #ede9fe; border-radius: 12px; padding: 20px; text-align: center;"">
                                <p style=""color: #5b21b6; margin: 0; font-size: 14px;"">
                                    👨‍⚕️ Médecin: <strong>{medecinNom}</strong>
                                </p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px 30px; text-align: center; border-radius: 0 0 12px 12px; border-top: 1px solid #e5e7eb;"">
                            <p style=""color: #6b7280; font-size: 14px; margin: 0 0 10px;"">
                                Vous serez notifié dès qu'un lit vous sera attribué.
                            </p>
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">
                                © {DateTime.Now.Year} MediConnect. Tous droits réservés.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private static string GetHospitalisationEmailTemplate(
        string patientNom,
        string medecinNom,
        string standardNom,
        string numeroChambre,
        string numeroLit,
        DateTime dateEntree,
        DateTime? dateSortiePrevue,
        string motif,
        decimal prixJournalier,
        int dureeEstimee,
        decimal montantEstime,
        string numeroFacture)
    {
        var dateSortieStr = dateSortiePrevue.HasValue 
            ? dateSortiePrevue.Value.ToString("dd/MM/yyyy") 
            : "À déterminer";

        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Confirmation d'hospitalisation</title>
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <!-- Header -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #0e7490 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: 600;"">
                                🏥 MediConnect
                            </h1>
                            <p style=""color: #e0f2fe; margin: 10px 0 0; font-size: 16px;"">
                                Confirmation d'hospitalisation
                            </p>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937; margin: 0 0 20px; font-size: 24px;"">
                                Bonjour {patientNom} 👋
                            </h2>
                            
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6; margin: 0 0 25px;"">
                                Votre hospitalisation a été ordonnée par <strong>{medecinNom}</strong>. 
                                Veuillez trouver ci-dessous tous les détails de votre séjour.
                            </p>

                            <!-- Détails de la chambre -->
                            <div style=""background-color: #f0f9ff; border-radius: 12px; padding: 25px; margin-bottom: 25px; border-left: 4px solid #0891b2;"">
                                <h3 style=""color: #0e7490; margin: 0 0 15px; font-size: 18px;"">🛏️ Votre chambre</h3>
                                <table style=""width: 100%; border-collapse: collapse;"">
                                    <tr>
                                        <td style=""padding: 8px 0; color: #6b7280;"">Standard:</td>
                                        <td style=""padding: 8px 0; color: #1f2937; font-weight: 600; text-align: right;"">{standardNom}</td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 8px 0; color: #6b7280;"">Chambre:</td>
                                        <td style=""padding: 8px 0; color: #1f2937; font-weight: 600; text-align: right;"">{numeroChambre}</td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 8px 0; color: #6b7280;"">Lit:</td>
                                        <td style=""padding: 8px 0; color: #1f2937; font-weight: 600; text-align: right;"">{numeroLit}</td>
                                    </tr>
                                </table>
                            </div>

                            <!-- Dates -->
                            <div style=""background-color: #fef3c7; border-radius: 12px; padding: 25px; margin-bottom: 25px; border-left: 4px solid #d97706;"">
                                <h3 style=""color: #92400e; margin: 0 0 15px; font-size: 18px;"">📅 Dates du séjour</h3>
                                <table style=""width: 100%; border-collapse: collapse;"">
                                    <tr>
                                        <td style=""padding: 8px 0; color: #78716c;"">Date d'entrée:</td>
                                        <td style=""padding: 8px 0; color: #1f2937; font-weight: 600; text-align: right;"">{dateEntree:dd/MM/yyyy à HH:mm}</td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 8px 0; color: #78716c;"">Date de sortie prévue:</td>
                                        <td style=""padding: 8px 0; color: #1f2937; font-weight: 600; text-align: right;"">{dateSortieStr}</td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 8px 0; color: #78716c;"">Durée estimée:</td>
                                        <td style=""padding: 8px 0; color: #1f2937; font-weight: 600; text-align: right;"">{dureeEstimee} jour(s)</td>
                                    </tr>
                                </table>
                            </div>

                            <!-- Motif -->
                            <div style=""background-color: #f3f4f6; border-radius: 12px; padding: 25px; margin-bottom: 25px;"">
                                <h3 style=""color: #374151; margin: 0 0 10px; font-size: 18px;"">📋 Motif d'hospitalisation</h3>
                                <p style=""color: #4b5563; margin: 0; line-height: 1.6;"">{motif}</p>
                            </div>

                            <!-- Facturation -->
                            <div style=""background-color: #dcfce7; border-radius: 12px; padding: 25px; margin-bottom: 25px; border-left: 4px solid #16a34a;"">
                                <h3 style=""color: #166534; margin: 0 0 15px; font-size: 18px;"">💰 Détails de facturation</h3>
                                <table style=""width: 100%; border-collapse: collapse;"">
                                    <tr>
                                        <td style=""padding: 8px 0; color: #4d7c0f;"">N° Facture:</td>
                                        <td style=""padding: 8px 0; color: #1f2937; font-weight: 600; text-align: right;"">{numeroFacture}</td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 8px 0; color: #4d7c0f;"">Prix journalier:</td>
                                        <td style=""padding: 8px 0; color: #1f2937; font-weight: 600; text-align: right;"">{prixJournalier:N0} FCFA</td>
                                    </tr>
                                    <tr style=""border-top: 2px solid #86efac;"">
                                        <td style=""padding: 12px 0 8px; color: #166534; font-weight: 600; font-size: 16px;"">Montant estimé:</td>
                                        <td style=""padding: 12px 0 8px; color: #166534; font-weight: 700; text-align: right; font-size: 20px;"">{montantEstime:N0} FCFA</td>
                                    </tr>
                                </table>
                                <p style=""color: #4d7c0f; font-size: 13px; margin: 15px 0 0; font-style: italic;"">
                                    * Le montant final sera calculé à la sortie selon la durée réelle du séjour.
                                </p>
                            </div>

                            <!-- Médecin -->
                            <div style=""background-color: #ede9fe; border-radius: 12px; padding: 20px; text-align: center;"">
                                <p style=""color: #5b21b6; margin: 0; font-size: 14px;"">
                                    👨‍⚕️ Médecin responsable: <strong>{medecinNom}</strong>
                                </p>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px 30px; text-align: center; border-radius: 0 0 12px 12px; border-top: 1px solid #e5e7eb;"">
                            <p style=""color: #6b7280; font-size: 14px; margin: 0 0 10px;"">
                                Pour toute question, contactez notre service d'accueil.
                            </p>
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">
                                © {DateTime.Now.Year} MediConnect. Tous droits réservés.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    #endregion
}
