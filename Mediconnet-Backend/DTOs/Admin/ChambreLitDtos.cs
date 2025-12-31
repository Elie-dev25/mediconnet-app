namespace Mediconnet_Backend.DTOs.Admin;

// ==================== CHAMBRES ====================

public class ChambreAdminDto
{
    public int IdChambre { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int Capacite { get; set; }
    public string Etat { get; set; } = "bon";
    public string Statut { get; set; } = "actif";
    public int NombreLits { get; set; }
    public int LitsLibres { get; set; }
    public int LitsOccupes { get; set; }
    public int LitsHorsService { get; set; }
    public List<LitAdminDto> Lits { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
}

public class CreateChambreRequest
{
    public string Numero { get; set; } = string.Empty;
    public int Capacite { get; set; } = 1;
    public string? Etat { get; set; } = "bon";
    public string? Statut { get; set; } = "actif";
    public List<CreateLitRequest>? Lits { get; set; }
}

public class UpdateChambreRequest
{
    public string? Numero { get; set; }
    public int? Capacite { get; set; }
    public string? Etat { get; set; }
    public string? Statut { get; set; }
}

// ==================== LITS ====================

public class LitAdminDto
{
    public int IdLit { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Statut { get; set; } = "libre";
    public int IdChambre { get; set; }
    public string? NumeroChambre { get; set; }
    public bool EstOccupe { get; set; }
    public string? PatientActuel { get; set; }
}

public class CreateLitRequest
{
    public string Numero { get; set; } = string.Empty;
    public string Statut { get; set; } = "libre";
    public int? IdChambre { get; set; }
}

public class UpdateLitRequest
{
    public string? Numero { get; set; }
    public string? Statut { get; set; }
}

// ==================== RESPONSES ====================

public class ChambreResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ChambreAdminDto? Chambre { get; set; }
}

public class ChambresListResponse
{
    public bool Success { get; set; } = true;
    public List<ChambreAdminDto> Chambres { get; set; } = new();
    public int Total { get; set; }
    public ChambresStats Stats { get; set; } = new();
}

public class ChambresStats
{
    public int TotalChambres { get; set; }
    public int ChambresActives { get; set; }
    public int TotalLits { get; set; }
    public int LitsLibres { get; set; }
    public int LitsOccupes { get; set; }
    public int LitsHorsService { get; set; }
}

public class LitResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public LitAdminDto? Lit { get; set; }
}

// ==================== LABORATOIRES (Placeholder) ====================

public class LaboratoireDto
{
    public int IdLabo { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Adresse { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public bool Actif { get; set; } = true;
}

public class LaboratoiresListResponse
{
    public bool Success { get; set; } = true;
    public List<LaboratoireDto> Laboratoires { get; set; } = new();
    public int Total { get; set; }
}
