using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedidosCrud.Model;

namespace PedidosCrud.Web.Controllers;

[Authorize]
public sealed class OrdersController : Controller
{
    public IActionResult Index()
    {
        return View(new OrderInput());
    }
}
