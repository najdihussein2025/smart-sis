namespace SmartSis.Api.Shared.Models;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record PageQuery(int Page = 1, int PageSize = 20)
{
    public int Skip => (Math.Max(Page, 1) - 1) * Take;
    public int Take => Math.Clamp(PageSize, 1, 100);
}
