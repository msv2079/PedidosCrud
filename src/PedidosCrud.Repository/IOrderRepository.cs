using PedidosCrud.Model;

namespace PedidosCrud.Repository;

public interface IOrderRepository
{
    IReadOnlyList<Order> GetSnapshot();
    Order? GetById(int id);
    Order Create(Order order);
    Order Update(int id, Order order, long expectedVersion);
    void Delete(int id, long expectedVersion);
}
