namespace NyangoPress.Application.Common;

/// <summary>Page de résultats (pagination côté serveur obligatoire sur toutes les listes).</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Ramène page et taille dans des bornes raisonnables (taille max 100).</summary>
    public static (int Page, int PageSize) Normalize(int page, int pageSize, int maxPageSize = 100)
    {
        var safePage = page < 1 ? 1 : page;
        var safeSize = pageSize < 1 ? 20 : Math.Min(pageSize, maxPageSize);
        return (safePage, safeSize);
    }
}
