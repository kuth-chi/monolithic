using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Finance.Application;
using Monolithic.Api.Modules.Finance.Contracts;
using Monolithic.Api.Modules.Identity.Authorization;

namespace Monolithic.Api.Controllers.V1;

[ApiController]
[Route("api/v1/bank-accounts")]
public sealed class BankAccountsController(IBankAccountService bankAccountService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("bankaccounts:read")]
    [ProducesResponseType<IReadOnlyCollection<BankAccountDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? businessId,
        [FromQuery] Guid? vendorId,
        [FromQuery] Guid? customerId,
        CancellationToken cancellationToken)
    {
        var accounts = await bankAccountService.GetAllAsync(businessId, vendorId, customerId, cancellationToken);
        return Ok(accounts);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("bankaccounts:read")]
    [ProducesResponseType<BankAccountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var account = await bankAccountService.GetByIdAsync(id, cancellationToken);
        return account is null ? NotFound() : Ok(account);
    }

    [HttpPost("business")]
    [RequirePermission("bankaccounts:create")]
    [ProducesResponseType<BankAccountDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessBankAccountRequest request, CancellationToken cancellationToken)
    {
        var created = await bankAccountService.CreateForBusinessAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("vendor")]
    [RequirePermission("bankaccounts:create")]
    [ProducesResponseType<BankAccountDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateVendor([FromBody] CreateVendorBankAccountRequest request, CancellationToken cancellationToken)
    {
        var created = await bankAccountService.CreateForVendorAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("customer")]
    [RequirePermission("bankaccounts:create")]
    [ProducesResponseType<BankAccountDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerBankAccountRequest request, CancellationToken cancellationToken)
    {
        var created = await bankAccountService.CreateForCustomerAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("bankaccounts:update")]
    [ProducesResponseType<BankAccountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBankAccountRequest request, CancellationToken cancellationToken)
    {
        var updated = await bankAccountService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("bankaccounts:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await bankAccountService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
