namespace Monolithic.Api.Modules.Users.Domain;

public sealed class AppUser
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Roles { get; init; } = [];

    public IReadOnlyCollection<string> Permissions { get; init; } = [];

    public DateTimeOffset CreatedAtUtc { get; init; }
}