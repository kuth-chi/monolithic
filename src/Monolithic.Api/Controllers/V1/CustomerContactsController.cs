using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Sales.Customers.Application;
using Monolithic.Api.Modules.Sales.Customers.Contracts;
using Monolithic.Api.Modules.Identity.Authorization;

namespace Monolithic.Api.Controllers.V1;

[ApiController]
[Route("api/v1/customer-contacts")]
public sealed class CustomerContactsController(ICustomerContactService contactService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("customers:read")]
    [ProducesResponseType<IReadOnlyCollection<CustomerContactDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid customerId,
        CancellationToken cancellationToken)
    {
        var contacts = await contactService.GetAllAsync(customerId, cancellationToken);
        return Ok(contacts);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("customers:read")]
    [ProducesResponseType<CustomerContactDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var contact = await contactService.GetByIdAsync(id, cancellationToken);
        return contact is null ? NotFound() : Ok(contact);
    }

    [HttpPost]
    [RequirePermission("customers:create")]
    [ProducesResponseType<CustomerContactDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerContactRequest request,
        CancellationToken cancellationToken)
    {
        var created = await contactService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("customers:update")]
    [ProducesResponseType<CustomerContactDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCustomerContactRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await contactService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("customers:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await contactService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
