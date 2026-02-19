namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// A contact person (e.g. account manager, billing contact) belonging to a customer.
/// Stores direct contact details without coupling to the Identity/User system.
/// </summary>
public sealed class CustomerContact
{
    public Guid Id { get; set; }

    /// <summary>The customer this contact belongs to.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Full name of the contact person.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Job title / role at the customer company (e.g. "Procurement Manager").</summary>
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>Department name.</summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>Direct email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Direct phone number.</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>Indicates this is the primary / main contact for the customer.</summary>
    public bool IsPrimary { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────

    public Customer Customer { get; set; } = null!;
}
