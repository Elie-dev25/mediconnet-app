namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité UserAuditLog - Enregistre les actions des utilisateurs
/// Utile pour la sécurité et l'audit
/// </summary>
public class UserAuditLog
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    /// <summary>Action effectuée (ex: "Login", "Create_Patient", "Update_Profile")</summary>
    public string Action { get; set; } = string.Empty;
    
    /// <summary>Ressource affectée (ex: "Patient", "Prescription")</summary>
    public string ResourceType { get; set; } = string.Empty;
    
    public int? ResourceId { get; set; }
    
    /// <summary>Détails de l'action (JSON)</summary>
    public string? Details { get; set; }
    
    /// <summary>Adresse IP de l'utilisateur</summary>
    public string? IpAddress { get; set; }
    
    public bool Success { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Relations
    public virtual User? User { get; set; }
}
