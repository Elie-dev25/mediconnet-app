using Mediconnet_Backend.Core.Interfaces.Services;
using System.Text.RegularExpressions;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service de validation des mots de passe selon la politique de sécurité
/// </summary>
public class PasswordValidationService : IPasswordValidationService
{
    // Configuration de la politique de mot de passe
    private const int MinLength = 8;
    private const int MaxLength = 128;
    
    // Regex patterns
    private static readonly Regex UppercaseRegex = new(@"[A-Z]", RegexOptions.Compiled);
    private static readonly Regex LowercaseRegex = new(@"[a-z]", RegexOptions.Compiled);
    private static readonly Regex DigitRegex = new(@"\d", RegexOptions.Compiled);
    private static readonly Regex SpecialCharRegex = new(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?~`]", RegexOptions.Compiled);

    public PasswordValidationResult ValidatePassword(string password)
    {
        var result = new PasswordValidationResult
        {
            IsValid = true,
            Errors = new List<string>()
        };

        if (string.IsNullOrEmpty(password))
        {
            result.IsValid = false;
            result.Errors.Add("Le mot de passe est requis");
            result.StrengthLevel = PasswordStrength.Weak;
            return result;
        }

        // Vérifier les critères
        result.Criteria = GetCriteriaStatus(password);

        // Longueur minimale
        if (!result.Criteria.HasMinLength)
        {
            result.IsValid = false;
            result.Errors.Add($"Le mot de passe doit contenir au moins {MinLength} caractères");
        }

        // Longueur maximale
        if (password.Length > MaxLength)
        {
            result.IsValid = false;
            result.Errors.Add($"Le mot de passe ne peut pas dépasser {MaxLength} caractères");
        }

        // Au moins une majuscule
        if (!result.Criteria.HasUppercase)
        {
            result.IsValid = false;
            result.Errors.Add("Le mot de passe doit contenir au moins une majuscule");
        }

        // Au moins une minuscule
        if (!result.Criteria.HasLowercase)
        {
            result.IsValid = false;
            result.Errors.Add("Le mot de passe doit contenir au moins une minuscule");
        }

        // Au moins un chiffre
        if (!result.Criteria.HasDigit)
        {
            result.IsValid = false;
            result.Errors.Add("Le mot de passe doit contenir au moins un chiffre");
        }

        // Calculer le score et le niveau
        result.StrengthScore = CalculateStrengthScore(password);
        result.StrengthLevel = GetStrengthLevel(password);

        return result;
    }

    public int CalculateStrengthScore(string password)
    {
        if (string.IsNullOrEmpty(password))
            return 0;

        int score = 0;

        // Points pour la longueur (max 40 points)
        score += Math.Min(password.Length * 4, 40);

        // Points pour les majuscules (10 points)
        if (UppercaseRegex.IsMatch(password))
            score += 10;

        // Points pour les minuscules (10 points)
        if (LowercaseRegex.IsMatch(password))
            score += 10;

        // Points pour les chiffres (10 points)
        if (DigitRegex.IsMatch(password))
            score += 10;

        // Points pour les caractères spéciaux (15 points)
        if (SpecialCharRegex.IsMatch(password))
            score += 15;

        // Bonus pour la diversité des caractères (15 points max)
        var uniqueChars = password.Distinct().Count();
        score += Math.Min(uniqueChars, 15);

        // Pénalité pour les caractères répétés
        var repeatedChars = password.Length - uniqueChars;
        score -= repeatedChars;

        // Limiter entre 0 et 100
        return Math.Max(0, Math.Min(100, score));
    }

    public PasswordStrength GetStrengthLevel(string password)
    {
        var score = CalculateStrengthScore(password);
        var criteria = GetCriteriaStatus(password);

        // Pour être "fort", il faut:
        // - Score >= 70
        // - Tous les critères obligatoires
        // - Au moins un caractère spécial
        if (score >= 70 && criteria.HasMinLength && criteria.HasUppercase && 
            criteria.HasLowercase && criteria.HasDigit && criteria.HasSpecialChar)
        {
            return PasswordStrength.Strong;
        }

        // Pour être "moyen", il faut:
        // - Score >= 50
        // - Critères obligatoires respectés
        if (score >= 50 && criteria.HasMinLength && criteria.HasUppercase && 
            criteria.HasLowercase && criteria.HasDigit)
        {
            return PasswordStrength.Medium;
        }

        return PasswordStrength.Weak;
    }

    private PasswordCriteriaStatus GetCriteriaStatus(string password)
    {
        return new PasswordCriteriaStatus
        {
            HasMinLength = password.Length >= MinLength,
            HasUppercase = UppercaseRegex.IsMatch(password),
            HasLowercase = LowercaseRegex.IsMatch(password),
            HasDigit = DigitRegex.IsMatch(password),
            HasSpecialChar = SpecialCharRegex.IsMatch(password)
        };
    }
}
