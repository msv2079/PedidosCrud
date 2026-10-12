using System.ComponentModel.DataAnnotations;
using PedidosCrud.Model;
using PedidosCrud.Repository;

namespace PedidosCrud.Service;

public sealed class OrderService(IOrderRepository repository, TimeProvider timeProvider) : IOrderService
{
    private static readonly HashSet<string> SortColumns = ["id", "customerName", "description", "totalAmount", "status", "createdAt"];

    public PagedResult<Order> Search(OrderQuery query)
    {
        ValidateQuery(query);
        var snapshot = repository.GetSnapshot();
        var search = query.Search?.Trim();
        var filteredOrders = snapshot.Where(order => MatchesSearch(order, search)).ToArray();
        var orderedOrders = Sort(filteredOrders, query.SortBy, query.Descending);
        var items = orderedOrders.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToArray();
        return new PagedResult<Order>(items, snapshot.Count, filteredOrders.Length, query.Page, query.PageSize);
    }

    public Order GetById(int id)
    {
        return repository.GetById(id) ?? throw new OrderNotFoundException(id);
    }

    public Order Create(OrderInput input)
    {
        ValidateInput(input, requireVersion: false);
        var order = MapToOrder(input) with { CreatedAt = timeProvider.GetUtcNow() };
        return repository.Create(order);
    }

    public Order Update(int id, OrderInput input)
    {
        ValidateInput(input, requireVersion: true);
        return repository.Update(id, MapToOrder(input), input.Version!.Value);
    }

    public void Delete(int id, long version)
    {
        if (version < 1)
        {
            throw new InputValidationException(new() { ["version"] = ["Informe a versão atual do pedido."] });
        }

        repository.Delete(id, version);
    }

    private static Order MapToOrder(OrderInput input)
    {
        return new Order { CustomerName = input.CustomerName.Trim(), Description = input.Description.Trim(), TotalAmount = input.TotalAmount, Status = input.Status };
    }

    private static bool MatchesSearch(Order order, string? search)
    {
        return string.IsNullOrEmpty(search) || order.CustomerName.Contains(search, StringComparison.OrdinalIgnoreCase) || order.Description.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateInput(OrderInput input, bool requireVersion)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), validationResults, validateAllProperties: true);
        var fieldErrors = validationResults.SelectMany(result => result.MemberNames.Select(member => new { Field = char.ToLowerInvariant(member[0]) + member[1..], Message = result.ErrorMessage! }));
        var errors = fieldErrors.GroupBy(result => result.Field).ToDictionary(group => group.Key, group => group.Select(result => result.Message).ToArray());

        if (decimal.Round(input.TotalAmount, 2) != input.TotalAmount)
        {
            errors["totalAmount"] = ["O valor total deve ter no máximo duas casas decimais."];
        }

        if (requireVersion && input.Version is not > 0)
        {
            errors["version"] = ["Informe a versão atual do pedido."];
        }

        if (errors.Count > 0)
        {
            throw new InputValidationException(errors);
        }
    }

    private static void ValidateQuery(OrderQuery query)
    {
        var errors = new Dictionary<string, string[]>();

        if (query.Page < 1 || (long)(query.Page - 1) * query.PageSize > int.MaxValue)
        {
            errors["page"] = ["Informe uma página válida, a partir de 1."];
        }

        if (query.PageSize is < 1 or > 100)
        {
            errors["pageSize"] = ["O tamanho da página deve estar entre 1 e 100."];
        }

        if (query.Search?.Length > 200)
        {
            errors["search"] = ["A pesquisa deve ter até 200 caracteres."];
        }

        if (query.SortBy is null || !SortColumns.Contains(query.SortBy))
        {
            errors["sortBy"] = ["A coluna de ordenação é inválida."];
        }

        if (errors.Count > 0)
        {
            throw new InputValidationException(errors);
        }
    }

    private static IOrderedEnumerable<Order> Sort(IEnumerable<Order> orders, string sortBy, bool descending)
    {
        return sortBy switch
        {
            "customerName" => OrderBy(orders, order => order.CustomerName, descending),
            "description" => OrderBy(orders, order => order.Description, descending),
            "totalAmount" => OrderBy(orders, order => order.TotalAmount, descending),
            "status" => OrderBy(orders, order => order.Status, descending),
            "createdAt" => OrderBy(orders, order => order.CreatedAt, descending),
            _ => OrderBy(orders, order => order.Id, descending)
        };
    }

    private static IOrderedEnumerable<Order> OrderBy<TKey>(IEnumerable<Order> orders, Func<Order, TKey> keySelector, bool descending)
    {
        var orderedOrders = descending ? orders.OrderByDescending(keySelector) : orders.OrderBy(keySelector);
        return orderedOrders.ThenBy(order => order.Id);
    }
}
