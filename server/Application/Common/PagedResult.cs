namespace server.Application.Common;

public class PaginationMeta
{
    public int Page { get; init; }
    public int Limit { get; init; }
    public int Total { get; init; }
}

public class PagedResult<T>
{
    public IEnumerable<T> Data { get; init; } = [];
    public PaginationMeta Pagination { get; init; } = new();

    public static PagedResult<T> Create(IEnumerable<T> data, int page, int limit, int total) =>
        new() { Data = data, Pagination = new() { Page = page, Limit = limit, Total = total } };
}
