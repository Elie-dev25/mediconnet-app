using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Core.Enums;
using Mediconnet_Backend.Core.Constants;
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
    private readonly IAssuranceCouvertureService _assuranceCouvertureService;

    public ConsultationService(
        ApplicationDbContext context,
        IAuditService auditService,
        ILogger<ConsultationService> logger,
        IRendezVousService rendezVousService,
        IAppointmentNotificationService notificationService,
        IAssuranceCouvertureService assuranceCouvertureService)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
        _rendezVousService = rendezVousService;
        _notificationService = notificationService;
        _assuranceCouvertureService = assuranceCouvertureService;
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

            // Règle métier: un paiement de consultation est valable pendant BusinessRules.PaymentValidityDays jours
            // tant que le patient reste dans le même service et voit un médecin de la même spécialité.
            var paymentValidSince = now.AddDays(-BusinessRules.PaymentValidityDays);
            var factureValide = await _context.Factures
                .Where(f => f.IdPatient == request.IdPatient)
                .Where(f => f.TypeFacture == FactureTypes.Consultation)
                .Where(f => f.Statut == FactureStatuts.Payee)
                .Where(f => f.DatePaiement.HasValue && f.DatePaiement.Value >= paymentValidSince)
                .Where(f => f.IdService == medecin.IdService)
                .Where(f => f.IdSpecialite == medecin.IdSpecialite)
                .OrderByDescending(f => f.DatePaiement)
                .FirstOrDefaultAsync();

            // Utiliser l'heure du créneau sélectionné ou maintenant par défaut
            var dateHeureRdv = request.DateHeureCreneau ?? now;

            // Déterminer le statut du RDV:
            // - "confirme" si paiement valide existe déjà (factureValide != null)
            // - "en_attente" sinon (sera confirmé après paiement à la caisse)
            var statutRdv = factureValide != null 
                ? RendezVousStatut.Confirme.ToDbString() 
                : RendezVousStatut.EnAttente.ToDbString();

            // Créer un RDV pour cette consultation (requis pour la file d'attente infirmier)
            var rendezVous = new RendezVous
            {
                IdPatient = request.IdPatient,
                IdMedecin = request.IdMedecin,
                IdService = medecin.IdService,
                DateHeure = dateHeureRdv,
                Duree = BusinessRules.DefaultConsultationDurationMinutes,
                Statut = statutRdv,
                Motif = request.Motif,
                TypeRdv = RendezVousTypes.Consultation,
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
                Statut = ConsultationStatut.Planifie.ToDbString(),
                TypeConsultation = ConsultationTypes.Normale
            };

            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Générer un numéro de facture unique
            var numeroFacture = await GenererNumeroFactureAsync();

            // Calculer la couverture assurance via le service centralisé
            var couverture = await _assuranceCouvertureService.CalculerCouvertureAsync(patient, "consultation", request.PrixConsultation);
            var estAssure = couverture.EstAssure;
            var tauxCouverture = couverture.TauxCouverture;
            var montantAssurance = couverture.MontantAssurance;
            var montantPatient = couverture.MontantPatient;

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
                Statut = factureValide != null ? FactureStatuts.Payee : FactureStatuts.EnAttente,
                TypeFacture = FactureTypes.Consultation,
                DateCreation = now,
                DateEcheance = factureValide != null ? null : now.AddDays(BusinessRules.FactureConsultationEcheanceDays),
                CouvertureAssurance = estAssure,
                IdAssurance = estAssure ? patient.AssuranceId : null,
                TauxCouverture = estAssure ? tauxCouverture : null,
                MontantAssurance = estAssure ? montantAssurance : null,
                DatePaiement = factureValide?.DatePaiement,
                Notes = factureValide != null
                    ? $"Paiement consultation déjà valable jusqu'au {factureValide.DatePaiement!.Value.AddDays(BusinessRules.PaymentValidityDays):yyyy-MM-dd}. Facture de référence: {factureValide.NumeroFacture}"
                    : null
            };

            _context.Factures.Add(facture);
            await _context.SaveChangesAsync();

            // Ajouter la ligne de facture détaillée
            var ligneFacture = new LigneFacture
            {
                IdFacture = facture.IdFacture,
                Description = $"Consultation - Dr. {medecin.Utilisateur?.Nom ?? "N/A"}",
                Quantite = 1,
                PrixUnitaire = request.PrixConsultation,
                Categorie = "consultation"
            };

            _context.LignesFacture.Add(ligneFacture);
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

            _logger.LogInformation("Consultation enregistrée: ID={IdConsultation}, Patient={IdPatient}, Médecin={IdMedecin}", consultation.IdConsultation, request.IdPatient, request.IdMedecin);

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

            _logger.LogInformation("Médecins filtrés: {Count} résultats (Service={IdService}, Spécialité={IdSpecialite})", medecins.Count, idService, idSpecialite);
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

            _logger.LogInformation("Services récupérés: {Count}", services.Count);
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

            _logger.LogInformation("Spécialités récupérées: {Count}", specialites.Count);
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
                // Un médecin est "occupé" UNIQUEMENT s'il a une consultation en_cours (bouton "Commencer" cliqué)
                // Mais il reste TOUJOURS sélectionnable pour les RDV (seul le créneau actuel est indisponible)
                string statut = "disponible";
                bool estDisponible = hasSlotDisponibleToday;
                string? raisonIndisponibilite = null;
                int? tempsAttenteEstime = null;

                if (!hasSlotDisponibleToday)
                {
                    statut = "absent";
                    estDisponible = false;
                    raisonIndisponibilite = creneauxJour.MessageIndisponibilite ?? "Aucun créneau disponible aujourd'hui";
                }
                else if (patientsEnConsultation > 0)
                {
                    // Médecin en consultation active (a cliqué sur "Commencer la consultation")
                    // Il est marqué "occupé" mais RESTE SÉLECTIONNABLE pour les créneaux suivants
                    statut = "occupe";
                    estDisponible = true; // Reste sélectionnable pour les RDV sur d'autres créneaux
                    raisonIndisponibilite = "En consultation";
                    // Estimation: 10 minutes restantes pour le patient actuel + 20 minutes par patient en attente
                    tempsAttenteEstime = 10 + (patientsEnAttente * 20);
                }
                else if (patientsEnAttente > 0)
                {
                    // Médecin disponible avec des patients en attente
                    statut = "disponible";
                    estDisponible = true;
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

            _logger.LogInformation("Médecins avec disponibilité: {Count} total, {TotalDisponibles} disponibles", result.Medecins.Count, result.TotalDisponibles);
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
            _logger.LogError(ex, "Erreur vérification paiement");
            return new VerifierPaiementResponse
            {
                PaiementValide = false,
                Message = "Erreur lors de la vérification"
            };
        }
    }
}
