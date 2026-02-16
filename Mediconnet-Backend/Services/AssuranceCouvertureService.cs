using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Core.Entities;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Résultat du calcul de couverture assurance pour une facture
/// </summary>
public class CouvertureResult
{
    public bool EstAssure { get; set; }
    public decimal TauxCouverture { get; set; }
    public decimal MontantAssurance { get; set; }
    public decimal MontantPatient { get; set; }
    public int? IdAssurance { get; set; }
    public decimal? Franchise { get; set; }
    public decimal? PlafondParActe { get; set; }
    public decimal? PlafondAnnuel { get; set; }
}

public interface IAssuranceCouvertureService
{
    /// <summary>
    /// Calcule la couverture assurance pour un patient et un type de prestation donné.
    /// Prend en compte le taux spécifique par type, les plafonds et la franchise.
    /// Fallback sur le taux global du patient si aucune config spécifique n'existe.
    /// </summary>
    Task<CouvertureResult> CalculerCouvertureAsync(Patient patient, string typePrestation, decimal montantTotal);
}

public class AssuranceCouvertureService : IAssuranceCouvertureService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AssuranceCouvertureService> _logger;

    public AssuranceCouvertureService(ApplicationDbContext context, ILogger<AssuranceCouvertureService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CouvertureResult> CalculerCouvertureAsync(Patient patient, string typePrestation, decimal montantTotal)
    {
        var now = DateTime.UtcNow;

        // Vérifier si le patient est assuré et que l'assurance est valide
        var estAssure = patient.AssuranceId.HasValue
            && patient.Assurance != null
            && (!patient.DateFinValidite.HasValue || patient.DateFinValidite.Value >= now);

        if (!estAssure || montantTotal <= 0)
        {
            return new CouvertureResult
            {
                EstAssure = false,
                TauxCouverture = 0,
                MontantAssurance = 0,
                MontantPatient = montantTotal,
                IdAssurance = null
            };
        }

        // Logique de priorité pour le taux de couverture:
        // 1. TauxCouvertureOverride du patient (négociations spéciales)
        // 2. AssuranceCouverture par type de prestation
        // 3. Fallback à 0% (pas de couverture configurée)

        decimal tauxCouverture;
        decimal? franchise = null;
        decimal? plafondParActe = null;
        decimal? plafondAnnuel = null;

        // Priorité 1: Override manuel du patient
        if (patient.TauxCouvertureOverride.HasValue && patient.TauxCouvertureOverride.Value > 0)
        {
            tauxCouverture = patient.TauxCouvertureOverride.Value;
            _logger.LogDebug(
                "Utilisation du taux override patient {PatientId}: {Taux}%",
                patient.IdUser, tauxCouverture);
        }
        else
        {
            // Priorité 2: Chercher la couverture spécifique pour ce type de prestation
            var couvertureSpecifique = await _context.AssuranceCouvertures
                .FirstOrDefaultAsync(c => c.IdAssurance == patient.AssuranceId.Value
                    && c.TypePrestation == typePrestation
                    && c.Actif);

            if (couvertureSpecifique != null)
            {
                // Utiliser la couverture spécifique par type de prestation
                tauxCouverture = couvertureSpecifique.TauxCouverture;
                franchise = couvertureSpecifique.Franchise;
                plafondParActe = couvertureSpecifique.PlafondParActe;
                plafondAnnuel = couvertureSpecifique.PlafondAnnuel;
            }
            else
            {
                // Priorité 3: Aucune configuration → 0%
                _logger.LogWarning(
                    "Aucune couverture configurée pour assurance {AssuranceId}, type {Type}. Taux = 0%",
                    patient.AssuranceId, typePrestation);
                tauxCouverture = 0;
            }
        }

        if (tauxCouverture <= 0)
        {
            return new CouvertureResult
            {
                EstAssure = true,
                TauxCouverture = 0,
                MontantAssurance = 0,
                MontantPatient = montantTotal,
                IdAssurance = patient.AssuranceId
            };
        }

        // Calculer le montant couvert
        var montantBase = montantTotal;

        // 1. Appliquer la franchise (montant non couvert)
        if (franchise.HasValue && franchise.Value > 0)
        {
            montantBase = Math.Max(0, montantBase - franchise.Value);
        }

        // 2. Appliquer le taux de couverture
        var montantAssurance = Math.Round(montantBase * tauxCouverture / 100, 2);

        // 3. Appliquer le plafond par acte
        if (plafondParActe.HasValue && montantAssurance > plafondParActe.Value)
        {
            montantAssurance = plafondParActe.Value;
        }

        // 4. Appliquer le plafond annuel (vérifier le cumul de l'année)
        if (plafondAnnuel.HasValue)
        {
            var debutAnnee = new DateTime(now.Year, 1, 1);
            var cumulAnnuel = await _context.Factures
                .Where(f => f.IdPatient == patient.IdUser
                    && f.IdAssurance == patient.AssuranceId
                    && f.TypeFacture == typePrestation
                    && f.DateCreation >= debutAnnee
                    && f.Statut != "annulee")
                .SumAsync(f => f.MontantAssurance ?? 0);

            var resteDisponible = Math.Max(0, plafondAnnuel.Value - cumulAnnuel);
            if (montantAssurance > resteDisponible)
            {
                _logger.LogInformation(
                    "Plafond annuel atteint pour patient {PatientId}, assurance {AssuranceId}, type {Type}. Cumul: {Cumul}, Plafond: {Plafond}",
                    patient.IdUser, patient.AssuranceId, typePrestation, cumulAnnuel, plafondAnnuel.Value);
                montantAssurance = resteDisponible;
            }
        }

        var montantPatient = montantTotal - montantAssurance;

        return new CouvertureResult
        {
            EstAssure = true,
            TauxCouverture = tauxCouverture,
            MontantAssurance = montantAssurance,
            MontantPatient = montantPatient,
            IdAssurance = patient.AssuranceId,
            Franchise = franchise,
            PlafondParActe = plafondParActe,
            PlafondAnnuel = plafondAnnuel
        };
    }
}
