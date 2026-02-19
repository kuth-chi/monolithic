using Microsoft.AspNetCore.Identity;

namespace Monolithic.Api.Modules.Identity.Domain;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public string Description { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
