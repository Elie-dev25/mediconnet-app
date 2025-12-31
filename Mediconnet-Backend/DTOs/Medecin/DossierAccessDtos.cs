namespace Mediconnet_Backend.DTOs.Medecin;

public class SendCodeRequest
{
    public int IdPatient { get; set; }
}

public class SendCodeResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}

public class VerifyCodeRequest
{
    public int IdPatient { get; set; }
    public string Code { get; set; } = string.Empty;
}

public class VerifyCodeResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
