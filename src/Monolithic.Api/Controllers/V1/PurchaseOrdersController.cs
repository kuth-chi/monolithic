using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.PurchaseOrders.Application;
using Monolithic.Api.Modules.PurchaseOrders.Contracts;

namespace Monolithic.Api.Controllers.V1;

[ApiController]
[Route("api/v1/purchase-orders")]
public sealed class PurchaseOrdersController(IPurchaseOrderService purchaseOrderService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("purchase:read")]
    [ProducesResponseType<IReadOnlyCollection<PurchaseOrderDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? businessId, CancellationToken cancellationToken)
    {
        var purchaseOrders = await purchaseOrderService.GetAllAsync(businessId, cancellationToken);
        return Ok(purchaseOrders);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("purchase:read")]
    [ProducesResponseType<PurchaseOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var purchaseOrder = await purchaseOrderService.GetByIdAsync(id, cancellationToken);
        return purchaseOrder is null ? NotFound() : Ok(purchaseOrder);
    }

    [HttpPost]
    [RequirePermission("purchase:create")]
    [ProducesResponseType<PurchaseOrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = TryGetCurrentUserId();
        var created = await purchaseOrderService.CreateAsync(request, currentUserId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    private Guid? TryGetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(raw, out var parsed) ? parsed : null;
    }
}
