namespace MyApp.Application.Common.Models;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items      { get; }
    public int              Page       { get; }
    public int              PageSize   { get; }
    public int              TotalCount { get; }
    public int              TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool             HasNext    => Page < TotalPages;
    public bool             HasPrev    => Page > 1;

    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items      = items;
        Page       = page;
        PageSize   = pageSize;
        TotalCount = totalCount;
    }
}
