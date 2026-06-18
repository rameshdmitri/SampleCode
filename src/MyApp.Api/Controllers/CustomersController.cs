namespace MyApp.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Common.Constants;
using MyApp.Application.Customers.DTOs;
using MyApp.Application.Customers.Interfaces;

[Authorize]
public sealed class CustomersController(ICustomerService customerService) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = AppRoles.AdminOrManager)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => HandleResult(await customerService.GetAllAsync(ct));

    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        => HandleResult(await customerService.GetMyProfileAsync(ct));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => HandleResult(await customerService.GetByIdAsync(id, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Customer)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
        => HandleResult(await customerService.UpdateProfileAsync(id, request, ct));
}
