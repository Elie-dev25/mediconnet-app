using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.DTOs.Prescription;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service centralisé pour la gestion des prescriptions médicamenteuses
/// Unifie la logique de prescription depuis tous les contextes
/// </summary>
public class PrescriptionService : IPrescriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PrescriptionService> _logger;

    // Constantes pour les types de contexte
    public static class TypeContexte
    {
        public const string Consultation = "consultation";
        public const string Hospitalisation = "hospitalisation";
        public const string Directe = "directe";
    }

    // Constantes pour les statuts d'ordonnance
    public static class StatutOrdonnance
    {
        public const string Active = "active";
        public const string Dispensee = "dispensee";
        public const string PartielleDispensee = "partielle";
        public const string Annulee = "annulee";
        public const string Expiree = "expiree";
    }

    public PrescriptionService(
        ApplicationDbContext context,
        ILogger<PrescriptionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==================== Création d'ordonnances ====================

    public async Task<OrdonnanceResult> CreerOrdonnanceAsync(CreateOrdonnanceRequest request, int medecinId)
    {
        try
        {
            // Validation de base
            if (request.IdPatient <= 0)
            {
                return new OrdonnanceResult
                {
                    Success = false,
                    Message = "ID patient invalide",
                    Erreurs = new List<string> { "L'ID du patient est obligatoire" }
                };
            }

            if (request.Medicaments == null || request.Medicaments.Count == 0)
            {
                return new OrdonnanceResult
                {
                    Success = false,
                    Message = "Aucun médicament prescrit",
                    Erreurs = new List<string> { "Au moins un médicament doit être prescrit" }
                };
            }

            // Vérifier que le patient existe
            var patient = await _context.Patients
                .Include(p => p.Utilisateur)
                .FirstOrDefaultAsync(p => p.IdUser == request.IdPatient);

            if (patient == null)
            {
                return new OrdonnanceResult
                {
                    Success = false,
                    Message = "Patient non trouvé",
                    Erreurs = new List<string> { $"Aucun patient trouvé avec l'ID {request.IdPatient}" }
                };
            }

            // Vérifier que le médecin existe
            var medecin = await _context.Medecins
                .Include(m => m.Utilisateur)
                .FirstOrDefaultAsync(m => m.IdUser == medecinId);

            if (medecin == null)
            {
                return new OrdonnanceResult
                {
                    Success = false,
                    Message = "Médecin non trouvé",
                    Erreurs = new List<string> { $"Aucun médecin trouvé avec l'ID {medecinId}" }
                };
            }

            // Déterminer le type de contexte
            // IMPORTANT: Plus de création de consultations fantômes!
            // Les ordonnances peuvent maintenant être liées directement à une hospitalisation ou être "directes"
            string typeContexte;
            int? idConsultation = request.IdConsultation;

            if (request.IdConsultation.HasValue && request.IdConsultation.Value > 0)
            {
                typeContexte = TypeContexte.Consultation;
            }
            else if (request.IdHospitalisation.HasValue && request.IdHospitalisation.Value > 0)
            {
                typeContexte = TypeContexte.Hospitalisation;
                // Plus de consultation fantôme! L'ordonnance est liée directement à l'hospitalisation
                idConsultation = null;
            }
            else
            {
                typeContexte = TypeContexte.Directe;
                // Prescription directe: pas de consultation, liée uniquement au patient et médecin
                idConsultation = null;
            }

            // Valider les médicaments et collecter les alertes
            var validationResult = await ValiderPrescriptionAsync(request.IdPatient, request.Medicaments);
            var alertes = validationResult.Alertes;

            // Créer l'ordonnance avec tous les champs
            var ordonnance = new Ordonnance
            {
                Date = DateTime.UtcNow,
                IdPatient = request.IdPatient,
                IdMedecin = medecinId,
                IdConsultation = idConsultation,
                IdHospitalisation = request.IdHospitalisation,
                TypeContexte = typeContexte,
                Statut = StatutOrdonnance.Active,
                Commentaire = request.Notes,
                CreatedAt = DateTime.UtcNow,
                // Fonctionnalités avancées
                DateExpiration = DateTime.UtcNow.AddDays(request.DureeValiditeJours),
                Renouvelable = request.Renouvelable,
                NombreRenouvellements = request.NombreRenouvellements,
                RenouvellementRestants = request.NombreRenouvellements
            };

            _context.Ordonnances.Add(ordonnance);
            await _context.SaveChangesAsync();

            // Ajouter les médicaments (catalogue ou saisie libre)
            foreach (var med in request.Medicaments)
            {
                var prescriptionMed = await CreerPrescriptionMedicamentAsync(ordonnance.IdOrdonnance, med);
                _context.PrescriptionMedicaments.Add(prescriptionMed);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Ordonnance {IdOrdonnance} créée pour patient {IdPatient} par médecin {IdMedecin} (contexte: {TypeContexte})",
                ordonnance.IdOrdonnance, request.IdPatient, medecinId, typeContexte);

            // Récupérer l'ordonnance complète pour le retour
            var ordonnanceDto = await GetOrdonnanceAsync(ordonnance.IdOrdonnance);

            return new OrdonnanceResult
            {
                Success = true,
                Message = "Ordonnance créée avec succès",
                IdOrdonnance = ordonnance.IdOrdonnance,
                Ordonnance = ordonnanceDto,
                Alertes = alertes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de l'ordonnance pour patient {IdPatient}", request.IdPatient);
            return new OrdonnanceResult
            {
                Success = false,
                Message = "Erreur lors de la création de l'ordonnance",
                Erreurs = new List<string> { ex.Message }
            };
        }
    }

    public async Task<OrdonnanceResult> CreerOrdonnanceConsultationAsync(
        int idConsultation,
        List<MedicamentPrescriptionRequest> medicaments,
        string? notes,
        int medecinId)
    {
        // Vérifier que la consultation existe et appartient au médecin
        var consultation = await _context.Consultations
            .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId);

        if (consultation == null)
        {
            return new OrdonnanceResult
            {
                Success = false,
                Message = "Consultation non trouvée",
                Erreurs = new List<string> { "La consultation n'existe pas ou n'appartient pas à ce médecin" }
            };
        }

        // Vérifier si une ordonnance existe déjà pour cette consultation
        var ordonnanceExistante = await _context.Ordonnances
            .Include(o => o.Medicaments)
            .FirstOrDefaultAsync(o => o.IdConsultation == idConsultation);

        if (ordonnanceExistante != null)
        {
            // Mettre à jour l'ordonnance existante
            return await MettreAJourOrdonnanceAsync(ordonnanceExistante.IdOrdonnance, medicaments, notes, medecinId);
        }

        // Créer une nouvelle ordonnance
        var request = new CreateOrdonnanceRequest
        {
            IdPatient = consultation.IdPatient,
            IdConsultation = idConsultation,
            Notes = notes,
            Medicaments = medicaments
        };

        return await CreerOrdonnanceAsync(request, medecinId);
    }

    public async Task<OrdonnanceResult> CreerOrdonnanceHospitalisationAsync(
        int idHospitalisation,
        List<MedicamentPrescriptionRequest> medicaments,
        string? notes,
        int medecinId)
    {
        // Vérifier que l'hospitalisation existe
        var hospitalisation = await _context.Hospitalisations
            .FirstOrDefaultAsync(h => h.IdAdmission == idHospitalisation);

        if (hospitalisation == null)
        {
            return new OrdonnanceResult
            {
                Success = false,
                Message = "Hospitalisation non trouvée",
                Erreurs = new List<string> { $"Aucune hospitalisation trouvée avec l'ID {idHospitalisation}" }
            };
        }

        var request = new CreateOrdonnanceRequest
        {
            IdPatient = hospitalisation.IdPatient,
            IdHospitalisation = idHospitalisation,
            Notes = notes,
            Medicaments = medicaments
        };

        return await CreerOrdonnanceAsync(request, medecinId);
    }

    public async Task<OrdonnanceResult> CreerOrdonnanceDirecteAsync(
        int idPatient,
        List<MedicamentPrescriptionRequest> medicaments,
        string? notes,
        int medecinId)
    {
        var request = new CreateOrdonnanceRequest
        {
            IdPatient = idPatient,
            Notes = notes,
            Medicaments = medicaments
        };

        return await CreerOrdonnanceAsync(request, medecinId);
    }

    // ==================== Lecture ====================

    public async Task<OrdonnanceDto?> GetOrdonnanceAsync(int idOrdonnance)
    {
        var ordonnance = await _context.Ordonnances
            // Navigations directes (nouveaux champs)
            .Include(o => o.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Include(o => o.Medecin)
                .ThenInclude(m => m!.Utilisateur)
            // Navigations via consultation (fallback pour compatibilité)
            .Include(o => o.Consultation)
                .ThenInclude(c => c!.Patient)
                    .ThenInclude(p => p!.Utilisateur)
            .Include(o => o.Consultation)
                .ThenInclude(c => c!.Medecin)
                    .ThenInclude(m => m!.Utilisateur)
            .Include(o => o.Medicaments!)
                .ThenInclude(m => m.Medicament)
            .FirstOrDefaultAsync(o => o.IdOrdonnance == idOrdonnance);

        if (ordonnance == null) return null;

        return MapToDto(ordonnance);
    }

    public async Task<OrdonnanceDto?> GetOrdonnanceByConsultationAsync(int idConsultation)
    {
        var ordonnance = await _context.Ordonnances
            .Include(o => o.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Include(o => o.Medecin)
                .ThenInclude(m => m!.Utilisateur)
            .Include(o => o.Consultation)
                .ThenInclude(c => c!.Patient)
                    .ThenInclude(p => p!.Utilisateur)
            .Include(o => o.Consultation)
                .ThenInclude(c => c!.Medecin)
                    .ThenInclude(m => m!.Utilisateur)
            .Include(o => o.Medicaments!)
                .ThenInclude(m => m.Medicament)
            .FirstOrDefaultAsync(o => o.IdConsultation == idConsultation);

        if (ordonnance == null) return null;

        return MapToDto(ordonnance);
    }

    public async Task<List<OrdonnanceDto>> GetOrdonnancesPatientAsync(int idPatient)
    {
        var ordonnances = await _context.Ordonnances
            .Include(o => o.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Include(o => o.Medecin)
                .ThenInclude(m => m!.Utilisateur)
            .Include(o => o.Consultation)
                .ThenInclude(c => c!.Patient)
                    .ThenInclude(p => p!.Utilisateur)
            .Include(o => o.Consultation)
                .ThenInclude(c => c!.Medecin)
                    .ThenInclude(m => m!.Utilisateur)
            .Include(o => o.Medicaments!)
                .ThenInclude(m => m.Medicament)
            .Where(o => o.IdPatient == idPatient || 
                       (o.Consultation != null && o.Consultation.IdPatient == idPatient))
            .OrderByDescending(o => o.Date)
            .ToListAsync();

        return ordonnances.Select(MapToDto).ToList();
    }

    public async Task<List<OrdonnanceDto>> GetOrdonnancesHospitalisationAsync(int idHospitalisation)
    {
        // D'abord chercher les ordonnances liées directement à l'hospitalisation
        var ordonnancesDirectes = await _context.Ordonnances
            .Include(o => o.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Include(o => o.Medecin)
                .ThenInclude(m => m!.Utilisateur)
            .Include(o => o.Consultation)
                .ThenInclude(c => c!.Patient)
                    .ThenInclude(p => p!.Utilisateur)
            .Include(o => o.Consultation)
                .ThenInclude(c => c!.Medecin)
                    .ThenInclude(m => m!.Utilisateur)
            .Include(o => o.Medicaments!)
                .ThenInclude(m => m.Medicament)
            .Where(o => o.IdHospitalisation == idHospitalisation)
            .OrderByDescending(o => o.Date)
            .ToListAsync();

        if (ordonnancesDirectes.Any())
        {
            return ordonnancesDirectes.Select(MapToDto).ToList();
        }

        // Fallback: récupérer l'hospitalisation pour chercher par période
        var hospitalisation = await _context.Hospitalisations
            .FirstOrDefaultAsync(h => h.IdAdmission == idHospitalisation);

        if (hospitalisation == null) return new List<OrdonnanceDto>();

        // Récupérer les ordonnances du patient pendant la période d'hospitalisation
        var ordonnances = await _context.Ordonnances
            .Include(o => o.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Include(o => o.Medecin)
                .ThenInclude(m => m!.Utilisateur)
            .Include(o => o.Consultation)
                .ThenInclude(c => c!.Patient)
                    .ThenInclude(p => p!.Utilisateur)
            .Include(o => o.Consultation)
                .ThenInclude(c => c!.Medecin)
                    .ThenInclude(m => m!.Utilisateur)
            .Include(o => o.Medicaments!)
                .ThenInclude(m => m.Medicament)
            .Where(o => (o.IdPatient == hospitalisation.IdPatient || 
                        (o.Consultation != null && o.Consultation.IdPatient == hospitalisation.IdPatient))
                && o.Date >= hospitalisation.DateEntree.Date
                && (hospitalisation.DateSortie == null || o.Date <= hospitalisation.DateSortie.Value.Date))
            .OrderByDescending(o => o.Date)
            .ToListAsync();

        return ordonnances.Select(MapToDto).ToList();
    }

    public async Task<(List<OrdonnanceDto> Items, int Total)> RechercherOrdonnancesAsync(FiltreOrdonnanceRequest filtre)
    {
        var query = _context.Ordonnances
            .Include(o => o.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Include(o => o.Medecin)
                .ThenInclude(m => m!.Utilisateur)
            .Include(o => o.Consultation)
                .ThenInclude(c => c!.Patient)
                    .ThenInclude(p => p!.Utilisateur)
            .Include(o => o.Consultation)
                .ThenInclude(c => c!.Medecin)
                    .ThenInclude(m => m!.Utilisateur)
            .Include(o => o.Medicaments!)
                .ThenInclude(m => m.Medicament)
            .AsQueryable();

        // Appliquer les filtres (utilise les champs directs avec fallback)
        if (filtre.IdPatient.HasValue)
            query = query.Where(o => o.IdPatient == filtre.IdPatient.Value || 
                                    (o.Consultation != null && o.Consultation.IdPatient == filtre.IdPatient.Value));

        if (filtre.IdMedecin.HasValue)
            query = query.Where(o => o.IdMedecin == filtre.IdMedecin.Value || 
                                    (o.Consultation != null && o.Consultation.IdMedecin == filtre.IdMedecin.Value));

        if (filtre.IdConsultation.HasValue)
            query = query.Where(o => o.IdConsultation == filtre.IdConsultation.Value);

        if (filtre.DateDebut.HasValue)
            query = query.Where(o => o.Date >= filtre.DateDebut.Value);

        if (filtre.DateFin.HasValue)
            query = query.Where(o => o.Date <= filtre.DateFin.Value);

        var total = await query.CountAsync();

        var ordonnances = await query
            .OrderByDescending(o => o.Date)
            .Skip((filtre.Page - 1) * filtre.PageSize)
            .Take(filtre.PageSize)
            .ToListAsync();

        return (ordonnances.Select(MapToDto).ToList(), total);
    }

    // ==================== Modification ====================

    public async Task<OrdonnanceResult> MettreAJourOrdonnanceAsync(
        int idOrdonnance,
        List<MedicamentPrescriptionRequest> medicaments,
        string? notes,
        int medecinId)
    {
        try
        {
            var ordonnance = await _context.Ordonnances
                .Include(o => o.Medicaments)
                .Include(o => o.Consultation)
                .FirstOrDefaultAsync(o => o.IdOrdonnance == idOrdonnance);

            if (ordonnance == null)
            {
                return new OrdonnanceResult
                {
                    Success = false,
                    Message = "Ordonnance non trouvée",
                    Erreurs = new List<string> { $"Aucune ordonnance trouvée avec l'ID {idOrdonnance}" }
                };
            }

            // Vérifier que le médecin a le droit de modifier
            if (ordonnance.Consultation?.IdMedecin != medecinId)
            {
                _logger.LogWarning(
                    "Médecin {MedecinId} tente de modifier l'ordonnance {IdOrdonnance} d'un autre médecin",
                    medecinId, idOrdonnance);
                // On autorise quand même la modification mais on log
            }

            // Supprimer les anciens médicaments
            if (ordonnance.Medicaments != null && ordonnance.Medicaments.Any())
            {
                _context.PrescriptionMedicaments.RemoveRange(ordonnance.Medicaments);
            }

            // Mettre à jour les notes
            ordonnance.Commentaire = notes;

            // Valider et ajouter les nouveaux médicaments
            var validationResult = await ValiderPrescriptionAsync(
                ordonnance.Consultation?.IdPatient ?? 0, 
                medicaments);

            foreach (var med in medicaments)
            {
                var prescriptionMed = await CreerPrescriptionMedicamentAsync(ordonnance.IdOrdonnance, med);
                _context.PrescriptionMedicaments.Add(prescriptionMed);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Ordonnance {IdOrdonnance} mise à jour par médecin {MedecinId}: {NbMedicaments} médicaments",
                idOrdonnance, medecinId, medicaments.Count);

            var ordonnanceDto = await GetOrdonnanceAsync(idOrdonnance);

            return new OrdonnanceResult
            {
                Success = true,
                Message = "Ordonnance mise à jour avec succès",
                IdOrdonnance = idOrdonnance,
                Ordonnance = ordonnanceDto,
                Alertes = validationResult.Alertes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour de l'ordonnance {IdOrdonnance}", idOrdonnance);
            return new OrdonnanceResult
            {
                Success = false,
                Message = "Erreur lors de la mise à jour de l'ordonnance",
                Erreurs = new List<string> { ex.Message }
            };
        }
    }

    public async Task<bool> AnnulerOrdonnanceAsync(int idOrdonnance, string motif, int medecinId)
    {
        try
        {
            var ordonnance = await _context.Ordonnances
                .Include(o => o.Consultation)
                .FirstOrDefaultAsync(o => o.IdOrdonnance == idOrdonnance);

            if (ordonnance == null)
            {
                _logger.LogWarning("Tentative d'annulation d'une ordonnance inexistante: {IdOrdonnance}", idOrdonnance);
                return false;
            }

            // Ajouter le motif d'annulation dans le commentaire
            ordonnance.Commentaire = $"[ANNULÉE - {DateTime.UtcNow:dd/MM/yyyy HH:mm}] Motif: {motif}\n{ordonnance.Commentaire}";

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Ordonnance {IdOrdonnance} annulée par médecin {MedecinId}. Motif: {Motif}",
                idOrdonnance, medecinId, motif);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'annulation de l'ordonnance {IdOrdonnance}", idOrdonnance);
            return false;
        }
    }

    // ==================== Validation ====================

    public async Task<ValidationPrescriptionResult> ValiderPrescriptionAsync(
        int idPatient,
        List<MedicamentPrescriptionRequest> medicaments)
    {
        var result = new ValidationPrescriptionResult
        {
            EstValide = true,
            Alertes = new List<AlertePrescription>()
        };

        foreach (var med in medicaments)
        {
            // Résoudre l'ID du médicament
            int? idMedicament = med.IdMedicament;
            
            if (!idMedicament.HasValue && !string.IsNullOrEmpty(med.NomMedicament))
            {
                var medicament = await _context.Medicaments
                    .FirstOrDefaultAsync(m => m.Nom.ToLower().Contains(med.NomMedicament.ToLower()));
                idMedicament = medicament?.IdMedicament;
            }

            if (idMedicament.HasValue)
            {
                // Vérifier le stock
                var medicamentEntity = await _context.Medicaments
                    .FirstOrDefaultAsync(m => m.IdMedicament == idMedicament.Value);

                if (medicamentEntity != null)
                {
                    var stock = medicamentEntity.Stock ?? 0;
                    var seuilAlerte = medicamentEntity.SeuilStock ?? 10;

                    if (stock == 0)
                    {
                        result.Alertes.Add(new AlertePrescription
                        {
                            Type = "rupture",
                            Severite = "error",
                            Message = $"Rupture de stock pour {medicamentEntity.Nom}",
                            IdMedicament = idMedicament.Value,
                            NomMedicament = medicamentEntity.Nom
                        });
                    }
                    else if (stock < med.Quantite)
                    {
                        result.Alertes.Add(new AlertePrescription
                        {
                            Type = "stock_insuffisant",
                            Severite = "warning",
                            Message = $"Stock insuffisant pour {medicamentEntity.Nom}: {stock} disponibles, {med.Quantite} demandés",
                            IdMedicament = idMedicament.Value,
                            NomMedicament = medicamentEntity.Nom
                        });
                    }
                    else if (stock <= seuilAlerte)
                    {
                        result.Alertes.Add(new AlertePrescription
                        {
                            Type = "stock_faible",
                            Severite = "info",
                            Message = $"Stock faible pour {medicamentEntity.Nom}: {stock} restants",
                            IdMedicament = idMedicament.Value,
                            NomMedicament = medicamentEntity.Nom
                        });
                    }

                    // Vérifier la péremption
                    if (medicamentEntity.DatePeremption.HasValue)
                    {
                        var joursRestants = (medicamentEntity.DatePeremption.Value - DateTime.UtcNow).Days;
                        if (joursRestants <= 0)
                        {
                            result.Alertes.Add(new AlertePrescription
                            {
                                Type = "perime",
                                Severite = "error",
                                Message = $"{medicamentEntity.Nom} est périmé",
                                IdMedicament = idMedicament.Value,
                                NomMedicament = medicamentEntity.Nom
                            });
                        }
                        else if (joursRestants <= 30)
                        {
                            result.Alertes.Add(new AlertePrescription
                            {
                                Type = "peremption_proche",
                                Severite = "warning",
                                Message = $"{medicamentEntity.Nom} expire dans {joursRestants} jours",
                                IdMedicament = idMedicament.Value,
                                NomMedicament = medicamentEntity.Nom
                            });
                        }
                    }
                }
            }
        }

        return result;
    }

    // ==================== Utilitaires ====================

    /// <summary>
    /// Crée une ligne de prescription médicament.
    /// Supporte les médicaments du catalogue ET les médicaments en saisie libre (hors catalogue).
    /// Ne crée JAMAIS de médicament dans la table medicament - les médicaments hors catalogue
    /// sont stockés uniquement dans la prescription.
    /// </summary>
    private async Task<PrescriptionMedicament> CreerPrescriptionMedicamentAsync(int idOrdonnance, MedicamentPrescriptionRequest med)
    {
        int? idMedicamentResolu = null;
        bool estHorsCatalogue = false;
        string? nomMedicamentLibre = null;
        string? dosageLibre = null;

        // PRIORITÉ 1: Si l'ID est fourni (depuis autocomplete), vérifier qu'il existe
        if (med.IdMedicament.HasValue && med.IdMedicament.Value > 0)
        {
            var medicamentExiste = await _context.Medicaments
                .AnyAsync(m => m.IdMedicament == med.IdMedicament.Value);
            
            if (medicamentExiste)
            {
                idMedicamentResolu = med.IdMedicament.Value;
                _logger.LogDebug("Médicament catalogue résolu par ID: {Id}", idMedicamentResolu);
            }
            else
            {
                _logger.LogWarning("ID médicament {Id} fourni mais non trouvé - traité comme saisie libre", med.IdMedicament.Value);
                estHorsCatalogue = true;
                nomMedicamentLibre = med.NomMedicament;
                dosageLibre = med.Dosage;
            }
        }
        // PRIORITÉ 2: Rechercher par nom EXACT (case-insensitive)
        else if (!string.IsNullOrWhiteSpace(med.NomMedicament))
        {
            var medicament = await _context.Medicaments
                .FirstOrDefaultAsync(m => m.Nom.ToLower() == med.NomMedicament.ToLower());

            if (medicament != null)
            {
                idMedicamentResolu = medicament.IdMedicament;
                _logger.LogDebug("Médicament catalogue résolu par nom exact: {Id} - {Nom}", 
                    medicament.IdMedicament, medicament.Nom);
            }
            else
            {
                // Médicament non trouvé dans le catalogue = saisie libre
                estHorsCatalogue = true;
                nomMedicamentLibre = med.NomMedicament;
                dosageLibre = med.Dosage;
                _logger.LogInformation(
                    "Médicament hors catalogue prescrit: {Nom} (Dosage: {Dosage})",
                    med.NomMedicament, med.Dosage ?? "non spécifié");
            }
        }
        else
        {
            throw new ArgumentException("Le nom du médicament est obligatoire");
        }

        return new PrescriptionMedicament
        {
            IdOrdonnance = idOrdonnance,
            IdMedicament = idMedicamentResolu,
            NomMedicamentLibre = nomMedicamentLibre,
            DosageLibre = dosageLibre,
            EstHorsCatalogue = estHorsCatalogue,
            Quantite = med.Quantite > 0 ? med.Quantite : 1,
            Posologie = med.Posologie,
            Frequence = med.Frequence,
            DureeTraitement = med.DureeTraitement,
            VoieAdministration = med.VoieAdministration,
            FormePharmaceutique = med.FormePharmaceutique,
            Instructions = med.Instructions
        };
    }

    /// <summary>
    /// Recherche un médicament dans le catalogue par ID ou nom exact.
    /// Retourne null si non trouvé (ne crée PAS de médicament).
    /// </summary>
    public async Task<int?> RechercherMedicamentCatalogueAsync(int? idMedicament, string nomMedicament)
    {
        // Par ID
        if (idMedicament.HasValue && idMedicament.Value > 0)
        {
            var existe = await _context.Medicaments.AnyAsync(m => m.IdMedicament == idMedicament.Value);
            if (existe) return idMedicament.Value;
        }

        // Par nom exact
        if (!string.IsNullOrWhiteSpace(nomMedicament))
        {
            var medicament = await _context.Medicaments
                .FirstOrDefaultAsync(m => m.Nom.ToLower() == nomMedicament.ToLower());
            if (medicament != null) return medicament.IdMedicament;
        }

        return null;
    }

    // ==================== Méthodes privées ====================

    /// <summary>
    /// Mappe une entité Ordonnance vers un DTO
    /// Utilise les nouveaux champs directs (IdPatient, IdMedecin, TypeContexte, Statut)
    /// avec fallback sur les données de la consultation pour compatibilité
    /// </summary>
    private OrdonnanceDto MapToDto(Ordonnance ordonnance)
    {
        // Utiliser les champs directs si disponibles, sinon fallback sur consultation
        var patient = ordonnance.Patient ?? ordonnance.Consultation?.Patient;
        var medecin = ordonnance.Medecin ?? ordonnance.Consultation?.Medecin;

        // Utiliser le type de contexte stocké, sinon déduire du motif de consultation
        var typeContexte = ordonnance.TypeContexte;
        if (string.IsNullOrEmpty(typeContexte))
        {
            var consultation = ordonnance.Consultation;
            if (consultation?.Motif?.Contains("hospitalisation") == true)
            {
                typeContexte = TypeContexte.Hospitalisation;
            }
            else if (consultation?.Motif?.Contains("Prescription directe") == true)
            {
                typeContexte = TypeContexte.Directe;
            }
            else
            {
                typeContexte = TypeContexte.Consultation;
            }
        }

        // Utiliser le statut stocké, sinon déduire
        var statut = ordonnance.Statut;
        if (string.IsNullOrEmpty(statut) || statut == "active")
        {
            if (ordonnance.Commentaire?.StartsWith("[ANNULÉE") == true)
            {
                statut = StatutOrdonnance.Annulee;
            }
            else
            {
                statut = StatutOrdonnance.Active;
            }
        }

        return new OrdonnanceDto
        {
            IdOrdonnance = ordonnance.IdOrdonnance,
            Date = ordonnance.Date,
            IdPatient = ordonnance.IdPatient ?? patient?.IdUser ?? 0,
            NomPatient = patient?.Utilisateur != null 
                ? $"{patient.Utilisateur.Prenom} {patient.Utilisateur.Nom}" 
                : "Patient inconnu",
            IdMedecin = ordonnance.IdMedecin ?? medecin?.IdUser ?? 0,
            NomMedecin = medecin?.Utilisateur != null 
                ? $"Dr. {medecin.Utilisateur.Prenom} {medecin.Utilisateur.Nom}" 
                : "Médecin inconnu",
            IdConsultation = ordonnance.IdConsultation,
            IdHospitalisation = ordonnance.IdHospitalisation,
            TypeContexte = typeContexte,
            Statut = statut,
            Notes = ordonnance.Commentaire,
            CreatedAt = ordonnance.CreatedAt,
            // Fonctionnalités avancées
            DateExpiration = ordonnance.DateExpiration,
            Renouvelable = ordonnance.Renouvelable,
            NombreRenouvellements = ordonnance.NombreRenouvellements,
            RenouvellementRestants = ordonnance.RenouvellementRestants,
            IdOrdonnanceOriginale = ordonnance.IdOrdonnanceOriginale,
            Lignes = ordonnance.Medicaments?.Select(m => new DTOs.Prescription.LignePrescriptionDto
            {
                IdPrescriptionMed = m.IdPrescriptionMed,
                IdMedicament = m.IdMedicament,
                // Utiliser le nom effectif (catalogue ou saisie libre)
                NomMedicament = m.EstHorsCatalogue 
                    ? m.NomMedicamentLibre ?? "Médicament non spécifié"
                    : m.Medicament?.Nom ?? m.NomMedicamentLibre ?? "Médicament inconnu",
                Dosage = m.EstHorsCatalogue 
                    ? m.DosageLibre 
                    : m.Medicament?.Dosage ?? m.DosageLibre,
                EstHorsCatalogue = m.EstHorsCatalogue,
                Quantite = m.Quantite,
                Posologie = m.Posologie,
                Frequence = m.Frequence,
                DureeTraitement = m.DureeTraitement,
                VoieAdministration = m.VoieAdministration,
                FormePharmaceutique = m.FormePharmaceutique,
                Instructions = m.Instructions,
                QuantiteDispensee = 0, // À calculer depuis les dispensations
                EstDispense = false
            }).ToList() ?? new List<DTOs.Prescription.LignePrescriptionDto>()
        };
    }
}
