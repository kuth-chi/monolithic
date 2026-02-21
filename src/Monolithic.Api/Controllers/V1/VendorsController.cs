using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Purchases.Vendors.Application;
using Monolithic.Api.Modules.Purchases.Vendors.Contracts;

namespace Monolithic.Api.Controllers.V1;

[ApiController]
[Route("api/v1/vendors")]
public sealed class VendorsController(IVendorService vendorService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("vendors:read")]
    [ProducesResponseType<IReadOnlyCollection<VendorDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? businessId, CancellationToken cancellationToken)
    {
        var vendors = await vendorService.GetAllAsync(businessId, cancellationToken);
        return Ok(vendors);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("vendors:read")]
    [ProducesResponseType<VendorDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var vendor = await vendorService.GetByIdAsync(id, cancellationToken);
        return vendor is null ? NotFound() : Ok(vendor);
    }

    [HttpPost]
    [RequirePermission("vendors:create")]
    [ProducesResponseType<VendorDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateVendorRequest request, CancellationToken cancellationToken)
    {
        var created = await vendorService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("vendors:update")]
    [ProducesResponseType<VendorDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorRequest request, CancellationToken cancellationToken)
    {
        var updated = await vendorService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("vendors:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await vendorService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
