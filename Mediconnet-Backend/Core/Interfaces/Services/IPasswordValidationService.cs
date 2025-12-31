namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour le service de validation des mots de passe
/// </summary>
public interface IPasswordValidationService
{
    /// <summary>
    /// Valide un mot de passe selon la politique de sécurité
    /// </summary>
    /// <param name="password">Mot de passe à valider</param>
    /// <returns>Résultat de validation avec détails</returns>
    PasswordValidationResult ValidatePassword(string password);

    /// <summary>
    /// Calcule le score de robustesse d'un mot de passe (0-100)
    /// </summary>
    int CalculateStrengthScore(string password);

    /// <summary>
    /// Retourne le niveau de robustesse (faible, moyen, fort)
    /// </summary>
    PasswordStrength GetStrengthLevel(string password);
}

/// <summary>
/// Résultat de la validation d'un mot de passe
/// </summary>
public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public int StrengthScore { get; set; }
    public PasswordStrength StrengthLevel { get; set; }
    
    /// <summary>Détails des critères respectés</summary>
    public PasswordCriteriaStatus Criteria { get; set; } = new();
}

/// <summary>
/// Statut de chaque critère de mot de passe
/// </summary>
public class PasswordCriteriaStatus
{
    public bool HasMinLength { get; set; }
    public bool HasUppercase { get; set; }
    public bool HasLowercase { get; set; }
    public bool HasDigit { get; set; }
    public bool HasSpecialChar { get; set; }
}

/// <summary>
/// Niveau de robustesse du mot de passe
/// </summary>
public enum PasswordStrength
{
    Weak = 0,
    Medium = 1,
    Strong = 2
}
