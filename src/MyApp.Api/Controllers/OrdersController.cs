namespace MyApp.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Common.Constants;
using MyApp.Application.Orders.DTOs;
using MyApp.Application.Orders.Interfaces;

[Authorize]
public sealed class OrdersController(IOrderService orderService) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = AppRoles.AdminOrManager)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => HandleResult(await orderService.GetAllAsync(ct));

    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders(CancellationToken ct)
        => HandleResult(await orderService.GetMyOrdersAsync(ct));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => HandleResult(await orderService.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var result = await orderService.CreateAsync(request, ct);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}/cancel")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Customer)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => HandleResult(await orderService.CancelAsync(id, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => HandleResult(await orderService.DeleteAsync(id, ct));
}
