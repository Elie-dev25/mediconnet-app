namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Représente une permission dans le système RBAC
/// </summary>
public class Permission
{
    public int IdPermission { get; set; }
    
    /// <summary>
    /// Code unique de la permission (ex: "patients.view", "consultations.create")
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Nom lisible de la permission
    /// </summary>
    public string Nom { get; set; } = string.Empty;
    
    /// <summary>
    /// Description de ce que permet cette permission
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Module/catégorie de la permission (ex: "patients", "consultations", "facturation")
    /// </summary>
    public string Module { get; set; } = string.Empty;
    
    /// <summary>
    /// Indique si la permission est active
    /// </summary>
    public bool Actif { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public ICollection<RolePermission>? RolePermissions { get; set; }
}

/// <summary>
/// Table de liaison entre les rôles et les permissions
/// </summary>
public class RolePermission
{
    public int IdRolePermission { get; set; }
    
    /// <summary>
    /// Nom du rôle (ex: "medecin", "infirmier")
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// ID de la permission associée
    /// </summary>
    public int IdPermission { get; set; }
    
    /// <summary>
    /// Indique si cette association est active
    /// </summary>
    public bool Actif { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Permission? Permission { get; set; }
}

/// <summary>
/// Permission spécifique à un utilisateur (override des permissions du rôle)
/// </summary>
public class UserPermission
{
    public int IdUserPermission { get; set; }
    
    /// <summary>
    /// ID de l'utilisateur
    /// </summary>
    public int IdUser { get; set; }
    
    /// <summary>
    /// ID de la permission
    /// </summary>
    public int IdPermission { get; set; }
    
    /// <summary>
    /// true = permission accordée, false = permission révoquée
    /// </summary>
    public bool Granted { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Utilisateur? Utilisateur { get; set; }
    public Permission? Permission { get; set; }
}
