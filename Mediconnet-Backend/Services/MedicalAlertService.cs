using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Core.Entities.Medical;
using Mediconnet_Backend.Data;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service d'alertes médicales - Interactions, allergies, contre-indications
/// </summary>
public class MedicalAlertService : IMedicalAlertService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedicalAlertService> _logger;

    public MedicalAlertService(ApplicationDbContext context, ILogger<MedicalAlertService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InteractionCheckResult> CheckInteractionsMedicamenteusesAsync(int idPatient, List<int> medicamentIds)
    {
        var result = new InteractionCheckResult();
        
        if (medicamentIds.Count < 2) return result;

        var interactions = await _context.InteractionsMedicamenteuses
            .Include(i => i.Medicament1)
            .Include(i => i.Medicament2)
            .Where(i => i.Actif &&
                medicamentIds.Contains(i.IdMedicament1) && 
                medicamentIds.Contains(i.IdMedicament2))
            .ToListAsync();

        foreach (var interaction in interactions)
        {
            result.Interactions.Add(new InteractionMedicamenteuseDto
            {
                IdMedicament1 = interaction.IdMedicament1,
                NomMedicament1 = interaction.Medicament1?.Nom ?? "",
                IdMedicament2 = interaction.IdMedicament2,
                NomMedicament2 = interaction.Medicament2?.Nom ?? "",
                TypeInteraction = interaction.TypeInteraction,
                Severite = interaction.Severite,
                Description = interaction.Description,
                Recommandation = interaction.Recommandation ?? ""
            });
        }

        result.HasInteractions = result.Interactions.Any();
        result.SeveriteMax = result.Interactions
            .OrderByDescending(i => GetSeverityScore(i.Severite))
            .FirstOrDefault()?.Severite ?? "none";

        return result;
    }

    public async Task<InteractionCheckResult> CheckInteractionAvecTraitementEnCoursAsync(int idPatient, int medicamentId)
    {
        // Récupérer les médicaments du traitement en cours du patient
        var traitementEnCours = await _context.PrescriptionMedicaments
            .Include(pm => pm.Ordonnance)
                .ThenInclude(o => o!.Consultation)
            .Where(pm => pm.Ordonnance != null && 
                pm.Ordonnance.Consultation != null &&
                pm.Ordonnance.Consultation.IdPatient == idPatient &&
                pm.Ordonnance.Date > DateTime.UtcNow.AddMonths(-3))
            .Select(pm => pm.IdMedicament)
            .Distinct()
            .ToListAsync();

        traitementEnCours.Add(medicamentId);
        return await CheckInteractionsMedicamenteusesAsync(idPatient, traitementEnCours);
    }

    public async Task<AllergieCheckResult> CheckAllergiesAsync(int idPatient, int medicamentId)
    {
        var result = new AllergieCheckResult();

        var medicament = await _context.Medicaments.FindAsync(medicamentId);
        if (medicament == null) return result;

        var allergies = await _context.AllergiesPatients
            .Where(a => a.IdPatient == idPatient && a.Actif && a.TypeAllergene == "medicament")
            .ToListAsync();

        foreach (var allergie in allergies)
        {
            // Vérifier si le médicament contient l'allergène
            if (medicament.Nom.Contains(allergie.Allergene, StringComparison.OrdinalIgnoreCase) ||
                (medicament.Dosage != null && medicament.Dosage.Contains(allergie.Allergene, StringComparison.OrdinalIgnoreCase)))
            {
                result.Alertes.Add(new AllergieAlertDto
                {
                    Allergene = allergie.Allergene,
                    Severite = allergie.Severite,
                    TypeReaction = allergie.TypeReaction ?? "Réaction allergique",
                    Recommandation = $"ATTENTION: Le patient est allergique à {allergie.Allergene}. Ne pas prescrire ce médicament."
                });
            }
        }

        result.HasAllergie = result.Alertes.Any();
        return result;
    }

    public async Task<List<AllergiePatientDto>> GetAllergiesPatientAsync(int idPatient)
    {
        return await _context.AllergiesPatients
            .Where(a => a.IdPatient == idPatient && a.Actif)
            .Select(a => new AllergiePatientDto
            {
                IdAllergie = a.IdAllergie,
                IdPatient = a.IdPatient,
                Allergene = a.Allergene,
                TypeAllergene = a.TypeAllergene,
                Severite = a.Severite,
                TypeReaction = a.TypeReaction,
                DateDecouverte = a.DateDecouverte,
                Notes = a.Notes
            })
            .ToListAsync();
    }

    public async Task<AllergiePatientDto> AjouterAllergieAsync(int idPatient, CreateAllergieRequest request)
    {
        var allergie = new AllergiePatient
        {
            IdPatient = idPatient,
            Allergene = request.Allergene,
            TypeAllergene = request.TypeAllergene,
            Severite = request.Severite,
            TypeReaction = request.TypeReaction,
            Notes = request.Notes,
            DateDecouverte = DateTime.UtcNow
        };

        _context.AllergiesPatients.Add(allergie);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Allergie ajoutée pour patient {IdPatient}: {Allergene}", idPatient, request.Allergene);

        return new AllergiePatientDto
        {
            IdAllergie = allergie.IdAllergie,
            IdPatient = allergie.IdPatient,
            Allergene = allergie.Allergene,
            TypeAllergene = allergie.TypeAllergene,
            Severite = allergie.Severite,
            TypeReaction = allergie.TypeReaction,
            DateDecouverte = allergie.DateDecouverte,
            Notes = allergie.Notes
        };
    }

    public async Task<bool> SupprimerAllergieAsync(int idAllergie)
    {
        var allergie = await _context.AllergiesPatients.FindAsync(idAllergie);
        if (allergie == null) return false;

        allergie.Actif = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ContreIndicationCheckResult> CheckContreIndicationsAsync(int idPatient, int medicamentId)
    {
        var result = new ContreIndicationCheckResult();

        var patient = await _context.Patients.FindAsync(idPatient);
        if (patient == null) return result;

        var contreIndications = await _context.ContreIndications
            .Include(ci => ci.Medicament)
            .Where(ci => ci.IdMedicament == medicamentId && ci.Actif)
            .ToListAsync();

        // Vérifier les maladies chroniques du patient
        var maladiesPatient = patient.MaladiesChroniques?.ToLower() ?? "";

        foreach (var ci in contreIndications)
        {
            if (maladiesPatient.Contains(ci.Condition.ToLower()))
            {
                result.ContreIndications.Add(new ContreIndicationDto
                {
                    Condition = ci.Condition,
                    TypeContreIndication = ci.TypeContreIndication,
                    Description = ci.Description,
                    Recommandation = ci.Recommandation ?? ""
                });
            }
        }

        result.HasContreIndication = result.ContreIndications.Any();
        return result;
    }

    public async Task<PrescriptionAlertResult> ValidatePrescriptionAsync(int idPatient, List<PrescriptionItemRequest> items)
    {
        var result = new PrescriptionAlertResult { IsValid = true };
        var medicamentIds = items.Select(i => i.IdMedicament).ToList();

        // Vérifier les interactions
        var interactions = await CheckInteractionsMedicamenteusesAsync(idPatient, medicamentIds);
        foreach (var interaction in interactions.Interactions)
        {
            var isCritique = interaction.Severite == "critique" || interaction.Severite == "severe";
            result.Alerts.Add(new PrescriptionAlertDto
            {
                Type = "interaction",
                Severite = interaction.Severite,
                Message = $"Interaction entre {interaction.NomMedicament1} et {interaction.NomMedicament2}: {interaction.Description}",
                IdMedicament = interaction.IdMedicament1,
                NomMedicament = interaction.NomMedicament1,
                Bloquant = isCritique
            });
            if (isCritique) result.HasCriticalAlerts = true;
        }

        // Vérifier les allergies pour chaque médicament
        foreach (var item in items)
        {
            var allergies = await CheckAllergiesAsync(idPatient, item.IdMedicament);
            foreach (var allergie in allergies.Alertes)
            {
                var isCritique = allergie.Severite == "severe" || allergie.Severite == "anaphylaxie";
                result.Alerts.Add(new PrescriptionAlertDto
                {
                    Type = "allergie",
                    Severite = allergie.Severite,
                    Message = $"Allergie à {allergie.Allergene}: {allergie.TypeReaction}",
                    IdMedicament = item.IdMedicament,
                    Bloquant = isCritique
                });
                if (isCritique) result.HasCriticalAlerts = true;
            }

            // Vérifier les contre-indications
            var contreIndications = await CheckContreIndicationsAsync(idPatient, item.IdMedicament);
            foreach (var ci in contreIndications.ContreIndications)
            {
                var isCritique = ci.TypeContreIndication == "absolue";
                result.Alerts.Add(new PrescriptionAlertDto
                {
                    Type = "contre_indication",
                    Severite = isCritique ? "critique" : "moderee",
                    Message = $"Contre-indication ({ci.Condition}): {ci.Description}",
                    IdMedicament = item.IdMedicament,
                    Bloquant = isCritique
                });
                if (isCritique) result.HasCriticalAlerts = true;
            }
        }

        result.IsValid = !result.HasCriticalAlerts;
        result.RecommandationGlobale = result.HasCriticalAlerts
            ? "ATTENTION: Des alertes critiques ont été détectées. Veuillez réviser la prescription."
            : result.Alerts.Any()
                ? "Des alertes mineures ont été détectées. Veuillez les prendre en compte."
                : "Aucune alerte détectée.";

        return result;
    }

    public async Task<List<AlerteMedicaleDto>> GetHistoriqueAlertesAsync(int idPatient)
    {
        return await _context.AlertesMedicales
            .Include(a => a.Medicament)
            .Where(a => a.IdPatient == idPatient)
            .OrderByDescending(a => a.DateAlerte)
            .Take(50)
            .Select(a => new AlerteMedicaleDto
            {
                IdAlerte = a.IdAlerte,
                IdPatient = a.IdPatient,
                Type = a.Type,
                Message = a.Message,
                Details = a.Details,
                IdMedicament = a.IdMedicament,
                NomMedicament = a.Medicament != null ? a.Medicament.Nom : null,
                DateAlerte = a.DateAlerte,
                Resolue = a.Resolue
            })
            .ToListAsync();
    }

    public async Task LogAlerteAsync(int idPatient, string type, string message, string? details, int? idMedicament)
    {
        var alerte = new AlerteMedicale
        {
            IdPatient = idPatient,
            Type = type,
            Message = message,
            Details = details,
            IdMedicament = idMedicament,
            DateAlerte = DateTime.UtcNow
        };

        _context.AlertesMedicales.Add(alerte);
        await _context.SaveChangesAsync();
    }

    private static int GetSeverityScore(string severite) => severite.ToLower() switch
    {
        "critique" => 4,
        "severe" => 3,
        "moderee" => 2,
        "faible" => 1,
        _ => 0
    };
}
