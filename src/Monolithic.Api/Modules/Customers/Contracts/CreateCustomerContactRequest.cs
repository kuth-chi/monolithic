using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Customers.Contracts;

public sealed class CreateCustomerContactRequest : CustomerContactPayloadBase
{
    [Required]
    public Guid CustomerId { get; init; }
}
