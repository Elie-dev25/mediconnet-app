using Mediconnet_Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service helper pour les opérations communes des médecins
/// Évite la duplication de code dans les controllers
/// </summary>
public interface IMedecinHelperService
{
    Task<int?> GetMedecinSpecialiteIdAsync(int medecinId);
    Task<bool> IsPremiereConsultationAsync(int patientId, int medecinId);
    bool IsStatutTermine(string? statut);
}

public class MedecinHelperService : IMedecinHelperService
{
    private readonly ApplicationDbContext _context;
    private static readonly HashSet<string> StatutsTermines = new() { "termine", "terminee" };

    public MedecinHelperService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Récupère l'ID de spécialité d'un médecin
    /// </summary>
    public async Task<int?> GetMedecinSpecialiteIdAsync(int medecinId)
    {
        return await _context.Medecins
            .Where(m => m.IdUser == medecinId)
            .Select(m => m.IdSpecialite)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Vérifie si c'est la première consultation d'un patient
    /// Prend en compte:
    /// - Si le dossier est clôturé (DossierCloture = true), c'est une première consultation
    /// - Si le patient n'a jamais eu de consultation terminée dans le système
    /// </summary>
    public async Task<bool> IsPremiereConsultationAsync(int patientId, int medecinId)
    {
        // Vérifier si le dossier du patient est clôturé
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.IdUser == patientId);
        
        if (patient != null && patient.DossierCloture)
        {
            return true; // Dossier clôturé = prochaine consultation est une première consultation
        }
        
        // Vérifier si le patient a déjà eu une consultation terminée (globalement, pas juste avec ce médecin)
        var hasCompletedConsultation = await _context.Consultations
            .AnyAsync(c => c.IdPatient == patientId && 
                          (c.Statut == "termine" || c.Statut == "terminee"));
        
        return !hasCompletedConsultation;
    }

    /// <summary>
    /// Vérifie si un statut correspond à une consultation terminée
    /// Gère les deux formats: "termine" et "terminee"
    /// </summary>
    public bool IsStatutTermine(string? statut)
    {
        return statut != null && StatutsTermines.Contains(statut.ToLower());
    }
}
