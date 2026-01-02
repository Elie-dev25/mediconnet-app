using FluentValidation;
using Mediconnet_Backend.DTOs.Auth;

namespace Mediconnet_Backend.Validators;

/// <summary>
/// Validateur pour les requêtes de login
/// Protection contre les injections et validation des formats
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage("L'identifiant est requis")
            .MaximumLength(120).WithMessage("L'identifiant ne peut pas dépasser 120 caractères")
            .Must(BeValidIdentifier).WithMessage("Format d'identifiant invalide");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Le mot de passe est requis")
            .MinimumLength(6).WithMessage("Le mot de passe doit contenir au moins 6 caractères")
            .MaximumLength(100).WithMessage("Le mot de passe ne peut pas dépasser 100 caractères");
    }

    private bool BeValidIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier)) return false;
        
        // Vérifier qu'il n'y a pas de caractères suspects (injection)
        var suspiciousPatterns = new[] { "<script", "javascript:", "onclick", "--", "/*", "*/" };
        return !suspiciousPatterns.Any(p => identifier.ToLower().Contains(p));
    }
}

/// <summary>
/// Validateur pour les requêtes d'inscription
/// </summary>
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Le prénom est requis")
            .MaximumLength(100).WithMessage("Le prénom ne peut pas dépasser 100 caractères")
            .Matches(@"^[\p{L}\s\-']+$").WithMessage("Le prénom contient des caractères invalides");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Le nom est requis")
            .MaximumLength(100).WithMessage("Le nom ne peut pas dépasser 100 caractères")
            .Matches(@"^[\p{L}\s\-']+$").WithMessage("Le nom contient des caractères invalides");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email est requis")
            .EmailAddress().WithMessage("Format d'email invalide")
            .MaximumLength(120).WithMessage("L'email ne peut pas dépasser 120 caractères");

        RuleFor(x => x.Telephone)
            .NotEmpty().WithMessage("Le téléphone est requis")
            .Matches(@"^[\d\s\+\-\(\)]+$").WithMessage("Format de téléphone invalide")
            .MaximumLength(20).WithMessage("Le téléphone ne peut pas dépasser 20 caractères");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Le mot de passe est requis")
            .MinimumLength(8).WithMessage("Le mot de passe doit contenir au moins 8 caractères")
            .MaximumLength(100).WithMessage("Le mot de passe ne peut pas dépasser 100 caractères")
            .Matches(@"[A-Z]").WithMessage("Le mot de passe doit contenir au moins une majuscule")
            .Matches(@"[a-z]").WithMessage("Le mot de passe doit contenir au moins une minuscule")
            .Matches(@"[0-9]").WithMessage("Le mot de passe doit contenir au moins un chiffre");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Les mots de passe ne correspondent pas");
    }
}

/// <summary>
/// Validateur pour les requêtes de changement de mot de passe
/// </summary>
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Le mot de passe actuel est requis");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Le nouveau mot de passe est requis")
            .MinimumLength(8).WithMessage("Le mot de passe doit contenir au moins 8 caractères")
            .MaximumLength(100).WithMessage("Le mot de passe ne peut pas dépasser 100 caractères")
            .Matches(@"[A-Z]").WithMessage("Le mot de passe doit contenir au moins une majuscule")
            .Matches(@"[a-z]").WithMessage("Le mot de passe doit contenir au moins une minuscule")
            .Matches(@"[0-9]").WithMessage("Le mot de passe doit contenir au moins un chiffre")
            .NotEqual(x => x.CurrentPassword).WithMessage("Le nouveau mot de passe doit être différent de l'ancien");

        RuleFor(x => x.ConfirmNewPassword)
            .Equal(x => x.NewPassword).WithMessage("Les mots de passe ne correspondent pas");
    }
}
