using System.ComponentModel.DataAnnotations;

namespace Mediconnet_Backend.DTOs.Admin;

/// <summary>
/// DTO pour afficher un utilisateur
/// </summary>
public class UserDto
{
    public int IdUser { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public string? Specialite { get; set; }
    public string? Service { get; set; }
}

/// <summary>
/// DTO pour creer un utilisateur
/// </summary>
public class CreateUserRequest
{
    [Required(ErrorMessage = "Le nom est requis")]
    [MinLength(2, ErrorMessage = "Le nom doit contenir au moins 2 caracteres")]
    public string Nom { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le prenom est requis")]
    [MinLength(2, ErrorMessage = "Le prenom doit contenir au moins 2 caracteres")]
    public string Prenom { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Email invalide")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le telephone est requis")]
    public string Telephone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est requis")]
    [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caracteres")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le role est requis")]
    public string Role { get; set; } = string.Empty;

    // Champs specifiques medecin
    public int? IdSpecialite { get; set; }
    public int? IdService { get; set; }
    public string? NumeroOrdre { get; set; }

    // Champs specifiques infirmier (IdService aussi utilisé)
    public string? Matricule { get; set; }
    /// <summary>
    /// Spécialité de l'infirmier (IDE, IADE, IBODE, etc.)
    /// </summary>
    public int? IdSpecialiteInfirmier { get; set; }

    // Champs specifiques laborantin
    public int? IdLabo { get; set; }
    public string? Specialisation { get; set; }
}

/// <summary>
/// DTO pour afficher un service
/// </summary>
public class ServiceDto
{
    public int IdService { get; set; }
    public string NomService { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ResponsableId { get; set; }
    public string? ResponsableNom { get; set; }
    public int NombreMedecins { get; set; }
    /// <summary>
    /// Coût de la consultation pour ce service (en FCFA)
    /// </summary>
    public decimal CoutConsultation { get; set; }
}

/// <summary>
/// DTO pour creer un service
/// </summary>
public class CreateServiceRequest
{
    [Required(ErrorMessage = "Le nom du service est requis")]
    [MinLength(2, ErrorMessage = "Le nom doit contenir au moins 2 caracteres")]
    [MaxLength(150, ErrorMessage = "Le nom ne peut pas depasser 150 caracteres")]
    public string NomService { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "La description ne peut pas depasser 500 caracteres")]
    public string? Description { get; set; }

    public int? ResponsableId { get; set; }

    /// <summary>
    /// Coût de la consultation pour ce service (en FCFA). Défaut: 5000
    /// </summary>
    [Range(0, 1000000, ErrorMessage = "Le coût doit être entre 0 et 1 000 000 FCFA")]
    public decimal CoutConsultation { get; set; } = 5000;
}

/// <summary>
/// DTO pour modifier un service
/// </summary>
public class UpdateServiceRequest
{
    [Required(ErrorMessage = "Le nom du service est requis")]
    [MinLength(2, ErrorMessage = "Le nom doit contenir au moins 2 caracteres")]
    [MaxLength(150, ErrorMessage = "Le nom ne peut pas depasser 150 caracteres")]
    public string NomService { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "La description ne peut pas depasser 500 caracteres")]
    public string? Description { get; set; }

    public int? ResponsableId { get; set; }

    /// <summary>
    /// Coût de la consultation pour ce service (en FCFA)
    /// </summary>
    [Range(0, 1000000, ErrorMessage = "Le coût doit être entre 0 et 1 000 000 FCFA")]
    public decimal CoutConsultation { get; set; }
}

/// <summary>
/// DTO pour afficher un responsable (medecin)
/// </summary>
public class ResponsableDto
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour afficher une specialite (médecin)
/// </summary>
public class SpecialiteDto
{
    public int IdSpecialite { get; set; }
    public string NomSpecialite { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour afficher une spécialité infirmier
/// </summary>
public class SpecialiteInfirmierDto
{
    public int IdSpecialite { get; set; }
    public string? Code { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Actif { get; set; }
    public DateTime CreatedAt { get; set; }
    public int NombreInfirmiers { get; set; }
}

/// <summary>
/// Request pour créer une spécialité infirmier
/// </summary>
public class CreateSpecialiteInfirmierRequest
{
    [MaxLength(20, ErrorMessage = "Le code ne peut pas dépasser 20 caractères")]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Le nom est requis")]
    [MinLength(2, ErrorMessage = "Le nom doit contenir au moins 2 caractères")]
    [MaxLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
    public string Nom { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
    public string? Description { get; set; }
}

/// <summary>
/// Request pour mettre à jour une spécialité infirmier
/// </summary>
public class UpdateSpecialiteInfirmierRequest
{
    [MaxLength(20, ErrorMessage = "Le code ne peut pas dépasser 20 caractères")]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Le nom est requis")]
    [MinLength(2, ErrorMessage = "Le nom doit contenir au moins 2 caractères")]
    [MaxLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
    public string Nom { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
    public string? Description { get; set; }

    public bool Actif { get; set; } = true;
}
