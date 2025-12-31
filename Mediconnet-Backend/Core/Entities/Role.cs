using Mediconnet_Backend.Core.Enums;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité Role - Représente un rôle et ses permissions associées
/// </summary>
public class Role
{
    public int Id { get; set; }
    
    /// <summary>Nom du rôle (ex: "Administrator", "Doctor")</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Description du rôle</summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>Énumération du rôle pour le mapping</summary>
    public UserRole RoleType { get; set; }
    
    /// <summary>Permissions associées au rôle (JSON)</summary>
    public string Permissions { get; set; } = "[]";
    
    /// <summary>Le rôle est-il actif?</summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>Le rôle est-il modifiable?</summary>
    public bool IsSystemRole { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public string? CreatedBy { get; set; }
    
    public string? UpdatedBy { get; set; }
}
