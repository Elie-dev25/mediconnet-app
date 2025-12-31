namespace Mediconnet_Backend.DTOs.Auth;

/// <summary>
/// DTO pour la demande de confirmation d'email
/// </summary>
public class ConfirmEmailRequest
{
    /// <summary>Token de confirmation reçu par email</summary>
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour la réponse de confirmation d'email
/// </summary>
public class ConfirmEmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? RedirectUrl { get; set; }
}

/// <summary>
/// DTO pour la demande de renvoi d'email de confirmation
/// </summary>
public class ResendConfirmationRequest
{
    /// <summary>Adresse email de l'utilisateur</summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour la réponse de renvoi d'email
/// </summary>
public class ResendConfirmationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
