using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedidosCrud.Model;
using PedidosCrud.Service;
using PedidosCrud.Web.Models;

namespace PedidosCrud.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/pedidos")]
public sealed class OrdersController(IOrderService orderService, ILogger<OrdersController> logger) : ControllerBase
{
    [HttpGet]
    public ActionResult<PagedResult<Order>> Search([FromQuery] OrderQuery query)
    {
        return Ok(orderService.Search(query));
    }

    [HttpGet("datatable")]
    public IActionResult SearchDataTable([FromQuery] DataTableRequest request)
    {
        var result = orderService.Search(request.ToQuery());
        return Ok(new { draw = request.Draw, recordsTotal = result.TotalCount, recordsFiltered = result.FilteredCount, data = result.Items });
    }

    [HttpGet("{id:int}")]
    public ActionResult<Order> GetById(int id)
    {
        return Ok(orderService.GetById(id));
    }

    [HttpPost]
    public ActionResult<Order> Create(OrderInput input)
    {
        var order = orderService.Create(input);
        logger.LogInformation("Pedido {OrderId} criado. Requisição {TraceId}.", order.Id, HttpContext.TraceIdentifier);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPut("{id:int}")]
    public ActionResult<Order> Update(int id, OrderInput input)
    {
        var order = orderService.Update(id, input);
        logger.LogInformation("Pedido {OrderId} atualizado para versão {Version}. Requisição {TraceId}.", id, order.Version, HttpContext.TraceIdentifier);
        return Ok(order);
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id, [FromQuery] long version)
    {
        orderService.Delete(id, version);
        logger.LogInformation("Pedido {OrderId} excluído. Requisição {TraceId}.", id, HttpContext.TraceIdentifier);
        return NoContent();
    }
}
