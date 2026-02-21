using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Purchases.Application;
using Monolithic.Api.Modules.Purchases.Contracts;

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

    [HttpPut("{id:guid}")]
    [RequirePermission("purchase:write")]
    [ProducesResponseType<PurchaseOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var updated = await purchaseOrderService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/confirm")]
    [RequirePermission("purchase:write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var userId = TryGetCurrentUserId() ?? Guid.Empty;
        await purchaseOrderService.ConfirmAsync(id, userId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/receive")]
    [RequirePermission("purchase:write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Receive(Guid id, [FromBody] ReceivePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var userId = TryGetCurrentUserId() ?? Guid.Empty;
        await purchaseOrderService.ReceiveAsync(id, request, userId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission("purchase:write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, [FromQuery] string reason = "", CancellationToken cancellationToken = default)
    {
        await purchaseOrderService.CancelAsync(id, reason, cancellationToken);
        return NoContent();
    }

    private Guid? TryGetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(raw, out var parsed) ? parsed : null;
    }
}
