using FluentValidation;
using Mediconnet_Backend.DTOs.Patient;

namespace Mediconnet_Backend.Validators;

/// <summary>
/// Validateur pour la mise à jour du profil patient
/// </summary>
public class UpdatePatientProfileRequestValidator : AbstractValidator<UpdatePatientProfileRequest>
{
    public UpdatePatientProfileRequestValidator()
    {
        RuleFor(x => x.Telephone)
            .Matches(@"^[\d\s\+\-\(\)]*$").WithMessage("Format de téléphone invalide")
            .MaximumLength(20).WithMessage("Le téléphone ne peut pas dépasser 20 caractères")
            .When(x => !string.IsNullOrEmpty(x.Telephone));

        RuleFor(x => x.Adresse)
            .MaximumLength(500).WithMessage("L'adresse ne peut pas dépasser 500 caractères")
            .When(x => !string.IsNullOrEmpty(x.Adresse));

        RuleFor(x => x.Sexe)
            .Must(BeValidSexe).WithMessage("Sexe invalide (M, F, ou Autre)")
            .When(x => !string.IsNullOrEmpty(x.Sexe));

        RuleFor(x => x.GroupeSanguin)
            .Must(BeValidGroupeSanguin).WithMessage("Groupe sanguin invalide")
            .When(x => !string.IsNullOrEmpty(x.GroupeSanguin));

        RuleFor(x => x.NumeroContact)
            .Matches(@"^[\d\s\+\-\(\)]*$").WithMessage("Format de numéro de contact invalide")
            .MaximumLength(20).WithMessage("Le numéro de contact ne peut pas dépasser 20 caractères")
            .When(x => !string.IsNullOrEmpty(x.NumeroContact));

        RuleFor(x => x.PersonneContact)
            .MaximumLength(200).WithMessage("Le nom de la personne de contact ne peut pas dépasser 200 caractères")
            .When(x => !string.IsNullOrEmpty(x.PersonneContact));

        RuleFor(x => x.Profession)
            .MaximumLength(100).WithMessage("La profession ne peut pas dépasser 100 caractères")
            .When(x => !string.IsNullOrEmpty(x.Profession));

        RuleFor(x => x.Naissance)
            .LessThan(DateTime.Now).WithMessage("La date de naissance doit être dans le passé")
            .GreaterThan(DateTime.Now.AddYears(-150)).WithMessage("Date de naissance invalide")
            .When(x => x.Naissance.HasValue);

        RuleFor(x => x.NbEnfants)
            .GreaterThanOrEqualTo(0).WithMessage("Le nombre d'enfants ne peut pas être négatif")
            .LessThanOrEqualTo(50).WithMessage("Nombre d'enfants invalide")
            .When(x => x.NbEnfants.HasValue);
    }

    private bool BeValidSexe(string? sexe)
    {
        if (string.IsNullOrEmpty(sexe)) return true;
        var validValues = new[] { "M", "F", "Masculin", "Féminin", "Autre" };
        return validValues.Contains(sexe, StringComparer.OrdinalIgnoreCase);
    }

    private bool BeValidGroupeSanguin(string? groupe)
    {
        if (string.IsNullOrEmpty(groupe)) return true;
        var validValues = new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
        return validValues.Contains(groupe, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Validateur pour la recherche de patients
/// </summary>
public class PatientSearchRequestValidator : AbstractValidator<PatientSearchRequest>
{
    public PatientSearchRequestValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotEmpty().WithMessage("Le terme de recherche est requis")
            .MinimumLength(2).WithMessage("Le terme de recherche doit contenir au moins 2 caractères")
            .MaximumLength(100).WithMessage("Le terme de recherche ne peut pas dépasser 100 caractères")
            .Must(NotContainSqlInjection).WithMessage("Caractères non autorisés dans la recherche");

        RuleFor(x => x.Limit)
            .GreaterThan(0).WithMessage("La limite doit être positive")
            .LessThanOrEqualTo(100).WithMessage("La limite ne peut pas dépasser 100")
            .When(x => x.Limit > 0);
    }

    private bool NotContainSqlInjection(string? term)
    {
        if (string.IsNullOrEmpty(term)) return true;
        var suspiciousPatterns = new[] { "--", ";--", "/*", "*/", "@@", "@", "char(", "nchar(", "varchar(", "exec(", "execute(" };
        return !suspiciousPatterns.Any(p => term.ToLower().Contains(p));
    }
}
