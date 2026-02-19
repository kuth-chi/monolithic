using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Customers.Application;
using Monolithic.Api.Modules.Customers.Contracts;
using Monolithic.Api.Modules.Identity.Authorization;

namespace Monolithic.Api.Controllers.V1;

[ApiController]
[Route("api/v1/customer-addresses")]
public sealed class CustomerAddressesController(ICustomerAddressService addressService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("customers:read")]
    [ProducesResponseType<IReadOnlyCollection<CustomerAddressDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid customerId,
        CancellationToken cancellationToken)
    {
        var addresses = await addressService.GetAllAsync(customerId, cancellationToken);
        return Ok(addresses);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("customers:read")]
    [ProducesResponseType<CustomerAddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var address = await addressService.GetByIdAsync(id, cancellationToken);
        return address is null ? NotFound() : Ok(address);
    }

    [HttpPost]
    [RequirePermission("customers:create")]
    [ProducesResponseType<CustomerAddressDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerAddressRequest request,
        CancellationToken cancellationToken)
    {
        var created = await addressService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("customers:update")]
    [ProducesResponseType<CustomerAddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCustomerAddressRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await addressService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("customers:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await addressService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
