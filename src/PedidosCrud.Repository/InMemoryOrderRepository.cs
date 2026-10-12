using PedidosCrud.Model;

namespace PedidosCrud.Repository;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<int, Order> _orders;
    private int _lastId;

    public InMemoryOrderRepository(OrderSeedGenerator seedGenerator)
    {
        _orders = seedGenerator.Generate().ToDictionary(order => order.Id);
        _lastId = _orders.Keys.Max();
    }

    public IReadOnlyList<Order> GetSnapshot()
    {
        lock (_syncRoot)
        {
            return _orders.Values.ToArray();
        }
    }

    public Order? GetById(int id)
    {
        lock (_syncRoot)
        {
            return _orders.GetValueOrDefault(id);
        }
    }

    public Order Create(Order order)
    {
        lock (_syncRoot)
        {
            var savedOrder = order with { Id = ++_lastId, Version = 1 };
            _orders.Add(savedOrder.Id, savedOrder);
            return savedOrder;
        }
    }

    public Order Update(int id, Order order, long expectedVersion)
    {
        lock (_syncRoot)
        {
            var currentOrder = GetCurrentOrder(id, expectedVersion);
            var savedOrder = order with { Id = id, CreatedAt = currentOrder.CreatedAt, Version = currentOrder.Version + 1 };
            _orders[id] = savedOrder;
            return savedOrder;
        }
    }

    public void Delete(int id, long expectedVersion)
    {
        lock (_syncRoot)
        {
            GetCurrentOrder(id, expectedVersion);
            _orders.Remove(id);
        }
    }

    private Order GetCurrentOrder(int id, long expectedVersion)
    {
        if (!_orders.TryGetValue(id, out var order))
        {
            throw new OrderNotFoundException(id);
        }

        if (order.Version != expectedVersion)
        {
            throw new OrderConflictException();
        }

        return order;
    }
}
