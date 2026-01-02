using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Accueil;
using Mediconnet_Backend.Helpers;
using Mediconnet_Backend.Hubs;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service pour la gestion des consultations
/// </summary>
public class ConsultationService : IConsultationService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<ConsultationService> _logger;
    private readonly IRendezVousService _rendezVousService;
    private readonly IAppointmentNotificationService _notificationService;

    public ConsultationService(
        ApplicationDbContext context,
        IAuditService auditService,
        ILogger<ConsultationService> logger,
        IRendezVousService rendezVousService,
        IAppointmentNotificationService notificationService)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
        _rendezVousService = rendezVousService;
        _notificationService = notificationService;
    }

    public async Task<EnregistrerConsultationResponse> EnregistrerConsultationAsync(
        EnregistrerConsultationRequest request, 
        int createdByUserId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Vérifier que le patient existe avec ses infos assurance
            var patient = await _context.Patients
                .Include(p => p.Utilisateur)
                .Include(p => p.Assurance)
                .FirstOrDefaultAsync(p => p.IdUser == request.IdPatient);
            
            if (patient == null)
            {
                return new EnregistrerConsultationResponse
                {
                    Success = false,
                    Message = "Patient non trouvé"
                };
            }

            // Vérifier que le médecin existe
            var medecin = await _context.Medecins
                .Include(m => m.Utilisateur)
                .FirstOrDefaultAsync(m => m.IdUser == request.IdMedecin);
            
            if (medecin == null)
            {
                return new EnregistrerConsultationResponse
                {
                    Success = false,
                    Message = "Médecin non trouvé"
                };
            }

            var now = DateTime.UtcNow;

            // Règle métier: un paiement de consultation est valable 2 semaines
            // tant que le patient reste dans le même service et voit un médecin de la même spécialité.
            var paymentValidSince = now.AddDays(-14);
            var factureValide = await _context.Factures
                .Where(f => f.IdPatient == request.IdPatient)
                .Where(f => f.TypeFacture == "consultation")
                .Where(f => f.Statut == "payee")
                .Where(f => f.DatePaiement.HasValue && f.DatePaiement.Value >= paymentValidSince)
                .Where(f => f.IdService == medecin.IdService)
                .Where(f => f.IdSpecialite == medecin.IdSpecialite)
                .OrderByDescending(f => f.DatePaiement)
                .FirstOrDefaultAsync();

            // Utiliser l'heure du créneau sélectionné ou maintenant par défaut
            var dateHeureRdv = request.DateHeureCreneau ?? now;

            // Créer un RDV pour cette consultation (requis pour la file d'attente infirmier)
            var rendezVous = new RendezVous
            {
                IdPatient = request.IdPatient,
                IdMedecin = request.IdMedecin,
                IdService = medecin.IdService,
                DateHeure = dateHeureRdv,
                Duree = 30,
                Statut = "confirme",
                Motif = request.Motif,
                TypeRdv = "consultation",
                DateCreation = now
            };

            _context.RendezVous.Add(rendezVous);
            await _context.SaveChangesAsync();

            // Créer la consultation liée au RDV
            var consultation = new Consultation
            {
                IdPatient = request.IdPatient,
                IdMedecin = request.IdMedecin,
                IdRendezVous = rendezVous.IdRendezVous,
                Motif = request.Motif,
                DateHeure = now,
                Statut = "planifie",
                TypeConsultation = "normale"
            };

            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Générer un numéro de facture unique
            var numeroFacture = await GenererNumeroFactureAsync();

            // Calculer la couverture assurance si le patient est assuré
            var estAssure = patient.AssuranceId.HasValue && 
                            patient.Assurance != null &&
                            (!patient.DateFinValidite.HasValue || patient.DateFinValidite.Value >= DateTime.UtcNow);
            
            decimal tauxCouverture = 0;
            decimal montantAssurance = 0;
            decimal montantPatient = request.PrixConsultation;
            
            if (estAssure)
            {
                // Utiliser le taux de couverture du patient
                tauxCouverture = patient.CouvertureAssurance ?? 0;
                montantAssurance = Math.Round(request.PrixConsultation * tauxCouverture / 100, 2);
                montantPatient = request.PrixConsultation - montantAssurance;
            }

            // Créer la facture
            var facture = new Facture
            {
                NumeroFacture = numeroFacture,
                IdPatient = request.IdPatient,
                IdMedecin = request.IdMedecin,
                IdService = medecin.IdService,
                IdSpecialite = medecin.IdSpecialite,
                IdConsultation = consultation.IdConsultation,
                MontantTotal = factureValide != null ? 0 : request.PrixConsultation,
                MontantPaye = 0,
                MontantRestant = factureValide != null ? 0 : montantPatient,
                Statut = factureValide != null ? "payee" : "en_attente",
                TypeFacture = "consultation",
                DateCreation = now,
                DateEcheance = factureValide != null ? null : now.AddDays(1),
                CouvertureAssurance = estAssure,
                IdAssurance = estAssure ? patient.AssuranceId : null,
                TauxCouverture = estAssure ? tauxCouverture : null,
                MontantAssurance = estAssure ? montantAssurance : null,
                DatePaiement = factureValide?.DatePaiement,
                Notes = factureValide != null
                    ? $"Paiement consultation déjà valable jusqu'au {factureValide.DatePaiement!.Value.AddDays(14):yyyy-MM-dd}. Facture de référence: {factureValide.NumeroFacture}"
                    : null
            };

            _context.Factures.Add(facture);
            await _context.SaveChangesAsync();

            // Logger l'action
            await _auditService.LogActionAsync(
                createdByUserId,
                "CONSULTATION_ENREGISTREE",
                "Consultation",
                consultation.IdConsultation,
                $"Consultation enregistrée pour {patient.Utilisateur?.Prenom} {patient.Utilisateur?.Nom} avec Dr. {medecin.Utilisateur?.Nom}"
            );

            await transaction.CommitAsync();

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
                dateCreation = facture.DateCreation
            });

            _logger.LogInformation($"Consultation enregistrée: ID={consultation.IdConsultation}, Patient={request.IdPatient}, Médecin={request.IdMedecin}");

            return new EnregistrerConsultationResponse
            {
                Success = true,
                Message = "Consultation enregistrée avec succès",
                IdConsultation = consultation.IdConsultation,
                IdPaiement = facture.IdFacture,
                NumeroPaiement = numeroFacture,
                Patient = new PatientConsultationDto
                {
                    IdUser = patient.IdUser,
                    Nom = patient.Utilisateur?.Nom ?? "",
                    Prenom = patient.Utilisateur?.Prenom ?? "",
                    NumeroDossier = patient.NumeroDossier ?? "",
                    Email = patient.Utilisateur?.Email ?? ""
                }
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erreur lors de l'enregistrement de la consultation");
            
            return new EnregistrerConsultationResponse
            {
                Success = false,
                Message = "Une erreur est survenue lors de l'enregistrement"
            };
        }
    }

    public async Task<List<MedecinDisponibleDto>> GetMedecinsDisponiblesAsync()
    {
        try
        {
            var medecins = await _context.Medecins
                .Include(m => m.Utilisateur)
                .Include(m => m.Specialite)
                .Include(m => m.Service)
                .Where(m => m.Utilisateur != null)
                .Select(m => new MedecinDisponibleDto
                {
                    IdMedecin = m.IdUser,
                    Nom = m.Utilisateur!.Nom,
                    Prenom = m.Utilisateur.Prenom,
                    Specialite = m.Specialite != null ? m.Specialite.NomSpecialite : "Médecine Générale",
                    Service = m.Service != null ? m.Service.NomService : null,
                    IdService = m.IdService,
                    IdSpecialite = m.IdSpecialite
                })
                .OrderBy(m => m.Nom)
                .ToListAsync();

            return medecins;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des médecins disponibles");
            return new List<MedecinDisponibleDto>();
        }
    }

    public async Task<List<MedecinDisponibleDto>> GetMedecinsFiltresAsync(int? idService, int? idSpecialite)
    {
        try
        {
            var query = _context.Medecins
                .Include(m => m.Utilisateur)
                .Include(m => m.Specialite)
                .Include(m => m.Service)
                .Where(m => m.Utilisateur != null)
                .AsQueryable();

            // Filtre par service
            if (idService.HasValue && idService.Value > 0)
            {
                query = query.Where(m => m.IdService == idService.Value);
            }

            // Filtre par spécialité
            if (idSpecialite.HasValue && idSpecialite.Value > 0)
            {
                query = query.Where(m => m.IdSpecialite == idSpecialite.Value);
            }

            var medecins = await query
                .Select(m => new MedecinDisponibleDto
                {
                    IdMedecin = m.IdUser,
                    Nom = m.Utilisateur!.Nom,
                    Prenom = m.Utilisateur.Prenom,
                    Specialite = m.Specialite != null ? m.Specialite.NomSpecialite : "Médecine Générale",
                    Service = m.Service != null ? m.Service.NomService : null,
                    IdService = m.IdService,
                    IdSpecialite = m.IdSpecialite
                })
                .OrderBy(m => m.Nom)
                .ThenBy(m => m.Prenom)
                .ToListAsync();

            _logger.LogInformation($"Médecins filtrés: {medecins.Count} résultats (Service={idService}, Spécialité={idSpecialite})");
            return medecins;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des médecins filtrés");
            return new List<MedecinDisponibleDto>();
        }
    }

    public async Task<List<ServiceDto>> GetServicesAsync()
    {
        try
        {
            var services = await _context.Services
                .OrderBy(s => s.NomService)
                .Select(s => new ServiceDto
                {
                    IdService = s.IdService,
                    NomService = s.NomService,
                    Description = s.Description
                })
                .ToListAsync();

            _logger.LogInformation($"Services récupérés: {services.Count}");
            return services;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des services");
            return new List<ServiceDto>();
        }
    }

    public async Task<List<SpecialiteDto>> GetSpecialitesAsync()
    {
        try
        {
            var specialites = await _context.Specialites
                .OrderBy(s => s.NomSpecialite)
                .Select(s => new SpecialiteDto
                {
                    IdSpecialite = s.IdSpecialite,
                    NomSpecialite = s.NomSpecialite
                })
                .ToListAsync();

            _logger.LogInformation($"Spécialités récupérées: {specialites.Count}");
            return specialites;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des spécialités");
            return new List<SpecialiteDto>();
        }
    }

    public async Task<MedecinsDisponibiliteResponse> GetMedecinsAvecDisponibiliteAsync(int? idService, int? idSpecialite)
    {
        try
        {
            var aujourdhui = DateTimeHelper.Today;
            var maintenant = DateTimeHelper.Now;
            var debutJour = aujourdhui;
            var finJour = aujourdhui.AddDays(1).AddSeconds(-1);

            // Récupérer tous les médecins avec filtres
            var queryMedecins = _context.Medecins
                .Include(m => m.Utilisateur)
                .Include(m => m.Specialite)
                .Include(m => m.Service)
                .Where(m => m.Utilisateur != null)
                .AsQueryable();

            if (idService.HasValue && idService.Value > 0)
            {
                queryMedecins = queryMedecins.Where(m => m.IdService == idService.Value);
            }

            if (idSpecialite.HasValue && idSpecialite.Value > 0)
            {
                queryMedecins = queryMedecins.Where(m => m.IdSpecialite == idSpecialite.Value);
            }

            var medecins = await queryMedecins.ToListAsync();

            // Récupérer les consultations du jour pour tous les médecins
            var idsMedecins = medecins.Select(m => m.IdUser).ToList();
            
            var consultationsJour = await _context.Consultations
                .Where(c => idsMedecins.Contains(c.IdMedecin) && c.DateHeure.Date == aujourdhui)
                .ToListAsync();

            // Récupérer les rendez-vous du jour
            var rendezVousJour = await _context.RendezVous
                .Where(r => idsMedecins.Contains(r.IdMedecin) && r.DateHeure.Date == aujourdhui)
                .ToListAsync();

            var result = new MedecinsDisponibiliteResponse();

            foreach (var medecin in medecins)
            {
                var consultationsMedecin = consultationsJour.Where(c => c.IdMedecin == medecin.IdUser).ToList();
                var rdvMedecin = rendezVousJour.Where(r => r.IdMedecin == medecin.IdUser).ToList();

                // Calculer la disponibilité réelle sur les créneaux d'aujourd'hui (planning + absences + RDV + verrous)
                var creneauxJour = await _rendezVousService.GetCreneauxDisponiblesAsync(
                    medecin.IdUser,
                    debutJour,
                    finJour);

                var hasSlotDisponibleToday = creneauxJour.Creneaux.Any(c => c.Disponible);

                // Compter les patients en attente (planifié) et en consultation (en_cours)
                var patientsEnAttente = consultationsMedecin.Count(c => c.Statut == "planifie");
                var patientsEnConsultation = consultationsMedecin.Count(c => c.Statut == "en_cours");
                var rdvAujourdhui = rdvMedecin.Count(r => r.Statut == "confirme" || r.Statut == "planifie");

                // Déterminer le statut
                string statut = "disponible";
                bool estDisponible = hasSlotDisponibleToday;
                string? raisonIndisponibilite = null;
                int? tempsAttenteEstime = null;

                if (!hasSlotDisponibleToday)
                {
                    statut = "absent";
                    raisonIndisponibilite = creneauxJour.MessageIndisponibilite ?? "Aucun créneau disponible aujourd'hui";
                }

                if (patientsEnConsultation > 0)
                {
                    statut = "occupe";
                    estDisponible = false;
                    raisonIndisponibilite = "En consultation";
                    // Estimation: 20 minutes par patient en attente + 10 minutes restantes pour le patient actuel
                    tempsAttenteEstime = 10 + (patientsEnAttente * 20);
                }
                else if (patientsEnAttente > 3)
                {
                    statut = "occupe";
                    estDisponible = hasSlotDisponibleToday; // Disponible mais chargé (seulement si un créneau est réellement libre)
                    raisonIndisponibilite = $"{patientsEnAttente} patients en attente";
                    tempsAttenteEstime = patientsEnAttente * 20;
                }
                else if (patientsEnAttente > 0)
                {
                    statut = "disponible";
                    estDisponible = hasSlotDisponibleToday;
                    tempsAttenteEstime = patientsEnAttente * 20;
                }

                var medecinDto = new MedecinAvecDisponibiliteDto
                {
                    IdMedecin = medecin.IdUser,
                    Nom = medecin.Utilisateur!.Nom,
                    Prenom = medecin.Utilisateur.Prenom,
                    Specialite = medecin.Specialite?.NomSpecialite ?? "Médecine Générale",
                    Service = medecin.Service?.NomService,
                    IdService = medecin.IdService,
                    IdSpecialite = medecin.IdSpecialite,
                    Statut = statut,
                    EstDisponible = estDisponible,
                    PatientsEnAttente = patientsEnAttente,
                    PatientsEnConsultation = patientsEnConsultation,
                    RendezVousAujourdhui = rdvAujourdhui,
                    RaisonIndisponibilite = raisonIndisponibilite,
                    TempsAttenteEstime = tempsAttenteEstime
                };

                result.Medecins.Add(medecinDto);
            }

            // Trier: disponibles d'abord, puis par temps d'attente
            result.Medecins = result.Medecins
                .OrderByDescending(m => m.EstDisponible)
                .ThenBy(m => m.TempsAttenteEstime ?? 0)
                .ThenBy(m => m.Nom)
                .ToList();

            result.TotalDisponibles = result.Medecins.Count(m => m.Statut == "disponible");
            result.TotalOccupes = result.Medecins.Count(m => m.Statut == "occupe");
            result.TotalAbsents = result.Medecins.Count(m => m.Statut == "absent");

            _logger.LogInformation($"Médecins avec disponibilité: {result.Medecins.Count} total, {result.TotalDisponibles} disponibles");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de la disponibilité des médecins");
            return new MedecinsDisponibiliteResponse { Success = false };
        }
    }

    /// <summary>
    /// Génère un numéro de facture unique
    /// Format: FAC-YYYYMMDD-XXXXX
    /// </summary>
    private async Task<string> GenererNumeroFactureAsync()
    {
        var prefix = $"FAC-{DateTime.UtcNow:yyyyMMdd}-";
        
        // Trouver le dernier numéro pour aujourd'hui
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

    public async Task<VerifierPaiementResponse> VerifierPaiementValideAsync(int idPatient, int idMedecin)
    {
        try
        {
            // Récupérer le médecin pour connaître son service et sa spécialité
            var medecin = await _context.Medecins
                .FirstOrDefaultAsync(m => m.IdUser == idMedecin);

            if (medecin == null)
            {
                return new VerifierPaiementResponse
                {
                    PaiementValide = false,
                    Message = "Médecin non trouvé"
                };
            }

            var now = DateTime.UtcNow;
            var paymentValidSince = now.AddDays(-14);

            // Vérifier si une facture valide existe
            var factureValide = await _context.Factures
                .Where(f => f.IdPatient == idPatient)
                .Where(f => f.TypeFacture == "consultation")
                .Where(f => f.Statut == "payee")
                .Where(f => f.DatePaiement.HasValue && f.DatePaiement.Value >= paymentValidSince)
                .Where(f => f.IdService == medecin.IdService)
                .Where(f => f.IdSpecialite == medecin.IdSpecialite)
                .OrderByDescending(f => f.DatePaiement)
                .FirstOrDefaultAsync();

            if (factureValide != null)
            {
                var dateExpiration = factureValide.DatePaiement!.Value.AddDays(14);
                return new VerifierPaiementResponse
                {
                    PaiementValide = true,
                    NumeroFacture = factureValide.NumeroFacture,
                    DatePaiement = factureValide.DatePaiement,
                    DateExpiration = dateExpiration,
                    Message = $"Paiement valide jusqu'au {dateExpiration:dd/MM/yyyy}"
                };
            }

            return new VerifierPaiementResponse
            {
                PaiementValide = false,
                Message = "Aucun paiement valide trouvé"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur vérification paiement: {ex.Message}");
            return new VerifierPaiementResponse
            {
                PaiementValide = false,
                Message = "Erreur lors de la vérification"
            };
        }
    }
}
