using Mediconnet_Backend.Core.Enums;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité User - Représente un utilisateur du système
/// Peut avoir plusieurs rôles (ex: Doctor + Administrator)
/// </summary>
public class User
{
    public int Id { get; set; }
    
    public string Username { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    /// <summary>Password hashé (jamais stocker en clair)</summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>Rôle primaire de l'utilisateur</summary>
    public UserRole PrimaryRole { get; set; }
    
    /// <summary>Rôles secondaires (optionnel - ex: Doctor avec Admin)</summary>
    public string? SecondaryRoles { get; set; } // JSON array
    
    /// <summary>Permissions supplémentaires (override des rôles)</summary>
    public string? CustomPermissions { get; set; } // JSON array
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    public string? Department { get; set; }
    
    /// <summary>Métadonnées spécifiques au rôle (JSON)</summary>
    public string? RoleMetadata { get; set; }
    
    // Relations
    public virtual ICollection<UserAuditLog> AuditLogs { get; set; } = new List<UserAuditLog>();
    
    /// <summary>Profil patient (si l'utilisateur est un patient)</summary>
    public virtual PatientProfile? PatientProfile { get; set; }
}
