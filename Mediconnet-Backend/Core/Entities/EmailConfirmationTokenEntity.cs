using System.ComponentModel.DataAnnotations;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Token de confirmation d'email
/// Stocke les tokens générés pour la vérification d'adresse email
/// </summary>
public class EmailConfirmationToken
{
    [Key]
    public int Id { get; set; }

    /// <summary>ID de l'utilisateur concerné</summary>
    [Required]
    public int IdUser { get; set; }

    /// <summary>Token unique (GUID)</summary>
    [Required]
    [MaxLength(100)]
    public string Token { get; set; } = string.Empty;

    /// <summary>Date de création du token</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Date d'expiration du token</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Token utilisé (confirmé)</summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>Date d'utilisation du token</summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>Adresse IP lors de la confirmation</summary>
    [MaxLength(50)]
    public string? ConfirmedFromIp { get; set; }

    // Navigation
    public virtual Utilisateur? Utilisateur { get; set; }

    /// <summary>
    /// Vérifie si le token est encore valide
    /// </summary>
    public bool IsValid => !IsUsed && DateTime.UtcNow < ExpiresAt;
}
