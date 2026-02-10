namespace Mediconnet_Backend.Core.Helpers;

/// <summary>
/// Classe de résultat paginé
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

/// <summary>
/// Paramètres de pagination
/// </summary>
public class PaginationParams
{
    private int _page = 1;
    private int _pageSize = 20;
    private const int MaxPageSize = 100;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 1 : value);
    }

    public int Skip => (Page - 1) * PageSize;
}

/// <summary>
/// Extensions pour la pagination de requêtes IQueryable
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Applique la pagination à une requête IQueryable
    /// </summary>
    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, PaginationParams pagination)
    {
        return query.Skip(pagination.Skip).Take(pagination.PageSize);
    }

    /// <summary>
    /// Applique la pagination avec des paramètres directs
    /// </summary>
    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, int page, int pageSize)
    {
        var pagination = new PaginationParams { Page = page, PageSize = pageSize };
        return query.ApplyPagination(pagination);
    }

    /// <summary>
    /// Crée un résultat paginé à partir d'une liste et du total
    /// </summary>
    public static PagedResult<T> ToPagedResult<T>(this List<T> items, int totalCount, PaginationParams pagination)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    /// <summary>
    /// Crée un résultat paginé à partir d'une liste et du total avec paramètres directs
    /// </summary>
    public static PagedResult<T> ToPagedResult<T>(this List<T> items, int totalCount, int page, int pageSize)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
