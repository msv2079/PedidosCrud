using PedidosCrud.Model;

namespace PedidosCrud.Repository;

public sealed class OrderSeedGenerator(TimeProvider timeProvider)
{
    public const int InitialCount = 10_000;

    public IEnumerable<Order> Generate()
    {
        string[] firstNames = ["Ana", "Bruno", "Carla", "Diego", "Elisa", "Felipe", "Gabriela", "Henrique", "Isabela", "João"];
        string[] lastNames = ["Silva", "Santos", "Oliveira", "Souza", "Lima", "Costa", "Pereira", "Almeida"];
        string[] descriptions = ["Materiais de escritório", "Equipamentos de informática", "Serviço de manutenção", "Acessórios e periféricos", "Materiais para embalagem", "Consultoria técnica", "Peças de reposição", "Suprimentos de impressão"];
        var random = new Random(42);
        var now = timeProvider.GetUtcNow();

        for (var id = 1; id <= InitialCount; id++)
        {
            yield return new Order
            {
                Id = id,
                CustomerName = $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}",
                Description = $"{descriptions[random.Next(descriptions.Length)]} — lote {id:D5}",
                TotalAmount = random.Next(100, 500_000) / 100m,
                Status = (OrderStatus)random.Next(1, 4),
                CreatedAt = now.AddMinutes(-random.Next(0, 90 * 24 * 60)),
                Version = 1
            };
        }
    }
}
