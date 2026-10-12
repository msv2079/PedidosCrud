using System.ComponentModel.DataAnnotations;

namespace PedidosCrud.Model;

public sealed class OrderInput
{
    [Required(ErrorMessage = "Informe o nome do cliente.")]
    [StringLength(100, ErrorMessage = "O nome do cliente deve ter até 100 caracteres.")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a descrição do pedido.")]
    [StringLength(500, ErrorMessage = "A descrição deve ter até 500 caracteres.")]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "1000000.00", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "Informe um valor entre R$ 0,01 e R$ 1.000.000,00.")]
    public decimal TotalAmount { get; set; }

    [EnumDataType(typeof(OrderStatus), ErrorMessage = "Selecione um status válido.")]
    public OrderStatus Status { get; set; } = OrderStatus.Open;

    public long? Version { get; set; }
}
