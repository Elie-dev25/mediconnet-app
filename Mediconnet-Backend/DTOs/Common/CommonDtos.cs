namespace Mediconnet_Backend.DTOs.Common;

/// <summary>
/// Réponse API générique avec message
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";

    public static ApiResponse Ok(string message = "Opération réussie") => 
        new() { Success = true, Message = message };

    public static ApiResponse Error(string message) => 
        new() { Success = false, Message = message };
}

/// <summary>
/// Réponse API générique avec données
/// </summary>
/// <typeparam name="T">Type des données</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Opération réussie") => 
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Error(string message) => 
        new() { Success = false, Message = message, Data = default };
}

/// <summary>
/// Réponse paginée
/// </summary>
/// <typeparam name="T">Type des éléments</typeparam>
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// Paramètres de pagination
/// </summary>
public class PaginationParams
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    public int Page { get; set; } = 1;
    
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
}

/// <summary>
/// DTO pour les select/dropdown
/// </summary>
public class SelectItemDto
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
    public string? Description { get; set; }
    public bool Disabled { get; set; }
}

/// <summary>
/// DTO de base pour les utilisateurs
/// </summary>
public class UserBaseDto
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string NomComplet => $"{Prenom} {Nom}".Trim();
    public string? Email { get; set; }
    public string? Telephone { get; set; }
}
