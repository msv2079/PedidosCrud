namespace PedidosCrud.Model;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int FilteredCount, int Page, int PageSize);
