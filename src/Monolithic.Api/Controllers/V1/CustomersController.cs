using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Customers.Application;
using Monolithic.Api.Modules.Customers.Contracts;
using Monolithic.Api.Modules.Identity.Authorization;

namespace Monolithic.Api.Controllers.V1;

[ApiController]
[Route("api/v1/customers")]
public sealed class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("customers:read")]
    [ProducesResponseType<IReadOnlyCollection<CustomerDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? businessId,
        CancellationToken cancellationToken)
    {
        var customers = await customerService.GetAllAsync(businessId, cancellationToken);
        return Ok(customers);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("customers:read")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var customer = await customerService.GetByIdAsync(id, cancellationToken);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    [RequirePermission("customers:create")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var created = await customerService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("customers:update")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await customerService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("customers:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await customerService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
