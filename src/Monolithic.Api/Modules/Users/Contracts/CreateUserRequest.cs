using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Users.Contracts;

public sealed class CreateUserRequest
{
    [Required]
    [MaxLength(120)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public IReadOnlyCollection<string> Roles { get; init; } = [];

    /// <remarks>
    /// Note: Permissions are managed through the Identity module.
    /// This field is deprecated and ignored. Use role-based or
    /// permission-based authorization through Identity system.
    /// </remarks>
    public IReadOnlyCollection<string> Permissions { get; init; } = [];
}