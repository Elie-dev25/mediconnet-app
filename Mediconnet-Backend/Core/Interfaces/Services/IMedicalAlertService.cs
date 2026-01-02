namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Service d'alertes médicales - Interactions, allergies, contre-indications
/// </summary>
public interface IMedicalAlertService
{
    // Vérification des interactions médicamenteuses
    Task<InteractionCheckResult> CheckInteractionsMedicamenteusesAsync(int idPatient, List<int> medicamentIds);
    Task<InteractionCheckResult> CheckInteractionAvecTraitementEnCoursAsync(int idPatient, int medicamentId);
    
    // Vérification des allergies
    Task<AllergieCheckResult> CheckAllergiesAsync(int idPatient, int medicamentId);
    Task<List<AllergiePatientDto>> GetAllergiesPatientAsync(int idPatient);
    Task<AllergiePatientDto> AjouterAllergieAsync(int idPatient, CreateAllergieRequest request);
    Task<bool> SupprimerAllergieAsync(int idAllergie);
    
    // Contre-indications
    Task<ContreIndicationCheckResult> CheckContreIndicationsAsync(int idPatient, int medicamentId);
    
    // Alertes globales pour une prescription
    Task<PrescriptionAlertResult> ValidatePrescriptionAsync(int idPatient, List<PrescriptionItemRequest> items);
    
    // Historique des alertes
    Task<List<AlerteMedicaleDto>> GetHistoriqueAlertesAsync(int idPatient);
    Task LogAlerteAsync(int idPatient, string type, string message, string? details, int? idMedicament);
}

// DTOs pour les alertes médicales
public class InteractionCheckResult
{
    public bool HasInteractions { get; set; }
    public List<InteractionMedicamenteuseDto> Interactions { get; set; } = new();
    public string SeveriteMax { get; set; } = "none"; // none, faible, moderee, severe, critique
}

public class InteractionMedicamenteuseDto
{
    public int IdMedicament1 { get; set; }
    public string NomMedicament1 { get; set; } = string.Empty;
    public int IdMedicament2 { get; set; }
    public string NomMedicament2 { get; set; } = string.Empty;
    public string TypeInteraction { get; set; } = string.Empty;
    public string Severite { get; set; } = string.Empty; // faible, moderee, severe, critique
    public string Description { get; set; } = string.Empty;
    public string Recommandation { get; set; } = string.Empty;
}

public class AllergieCheckResult
{
    public bool HasAllergie { get; set; }
    public List<AllergieAlertDto> Alertes { get; set; } = new();
}

public class AllergieAlertDto
{
    public string Allergene { get; set; } = string.Empty;
    public string Severite { get; set; } = string.Empty; // legere, moderee, severe, anaphylaxie
    public string TypeReaction { get; set; } = string.Empty;
    public string Recommandation { get; set; } = string.Empty;
}

public class AllergiePatientDto
{
    public int IdAllergie { get; set; }
    public int IdPatient { get; set; }
    public string Allergene { get; set; } = string.Empty;
    public string TypeAllergene { get; set; } = string.Empty; // medicament, aliment, environnement
    public string Severite { get; set; } = string.Empty;
    public string? TypeReaction { get; set; }
    public DateTime DateDecouverte { get; set; }
    public string? Notes { get; set; }
}

public class CreateAllergieRequest
{
    public string Allergene { get; set; } = string.Empty;
    public string TypeAllergene { get; set; } = "medicament";
    public string Severite { get; set; } = "moderee";
    public string? TypeReaction { get; set; }
    public string? Notes { get; set; }
}

public class ContreIndicationCheckResult
{
    public bool HasContreIndication { get; set; }
    public List<ContreIndicationDto> ContreIndications { get; set; } = new();
}

public class ContreIndicationDto
{
    public string Condition { get; set; } = string.Empty;
    public string TypeContreIndication { get; set; } = string.Empty; // absolue, relative
    public string Description { get; set; } = string.Empty;
    public string Recommandation { get; set; } = string.Empty;
}

public class PrescriptionAlertResult
{
    public bool IsValid { get; set; }
    public bool HasCriticalAlerts { get; set; }
    public List<PrescriptionAlertDto> Alerts { get; set; } = new();
    public string RecommandationGlobale { get; set; } = string.Empty;
}

public class PrescriptionAlertDto
{
    public string Type { get; set; } = string.Empty; // interaction, allergie, contre_indication, dosage
    public string Severite { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? IdMedicament { get; set; }
    public string? NomMedicament { get; set; }
    public bool Bloquant { get; set; }
}

public class PrescriptionItemRequest
{
    public int IdMedicament { get; set; }
    public string Dosage { get; set; } = string.Empty;
    public int Quantite { get; set; }
    public string Posologie { get; set; } = string.Empty;
}

public class AlerteMedicaleDto
{
    public int IdAlerte { get; set; }
    public int IdPatient { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int? IdMedicament { get; set; }
    public string? NomMedicament { get; set; }
    public DateTime DateAlerte { get; set; }
    public bool Resolue { get; set; }
}
