using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Customers.Contracts;

public sealed class CreateCustomerAddressRequest : CustomerAddressPayloadBase
{
    [Required]
    public Guid CustomerId { get; init; }
}
