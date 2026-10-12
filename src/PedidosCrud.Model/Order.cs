namespace PedidosCrud.Model;

public sealed record Order
{
    public int Id { get; init; }
    public required string CustomerName { get; init; }
    public required string Description { get; init; }
    public decimal TotalAmount { get; init; }
    public OrderStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public long Version { get; init; }
}
