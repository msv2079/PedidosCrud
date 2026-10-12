namespace PedidosCrud.Model;

public sealed class OrderQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string SortBy { get; set; } = "id";
    public bool Descending { get; set; } = true;
}
