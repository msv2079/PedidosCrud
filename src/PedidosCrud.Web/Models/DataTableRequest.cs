using Microsoft.AspNetCore.Mvc;
using PedidosCrud.Model;

namespace PedidosCrud.Web.Models;

public sealed class DataTableRequest
{
    private static readonly string[] Columns = ["id", "customerName", "description", "totalAmount", "status", "createdAt"];

    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; } = 10;

    [FromQuery(Name = "search[value]")]
    public string? Search { get; set; }

    [FromQuery(Name = "order[0][column]")]
    public int OrderColumn { get; set; }

    [FromQuery(Name = "order[0][dir]")]
    public string OrderDirection { get; set; } = "desc";

    public OrderQuery ToQuery()
    {
        var errors = new Dictionary<string, string[]>();

        if (Draw < 0 || Start < 0 || Length is < 1 or > 100 || (Length > 0 && Start % Length != 0))
        {
            errors["pagination"] = ["Os parâmetros de paginação são inválidos."];
        }

        if (OrderColumn < 0 || OrderColumn >= Columns.Length || OrderDirection is not ("asc" or "desc"))
        {
            errors["order"] = ["A ordenação é inválida."];
        }

        if (errors.Count > 0)
        {
            throw new InputValidationException(errors);
        }

        return new OrderQuery { Page = Start / Length + 1, PageSize = Length, Search = Search, SortBy = Columns[OrderColumn], Descending = OrderDirection == "desc" };
    }
}
