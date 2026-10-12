using PedidosCrud.Model;

namespace PedidosCrud.Service;

public interface IOrderService
{
    PagedResult<Order> Search(OrderQuery query);
    Order GetById(int id);
    Order Create(OrderInput input);
    Order Update(int id, OrderInput input);
    void Delete(int id, long version);
}
