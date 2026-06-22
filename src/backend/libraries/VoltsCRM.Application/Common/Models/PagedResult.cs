namespace VoltsCRM.Application.Common.Models;

/// <summary>Standard list envelope returned by every paged query and serialized to the frontend.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
