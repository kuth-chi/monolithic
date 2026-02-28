using Microsoft.AspNetCore.Identity;
using Monolithic.Api.Common.Caching;
using Monolithic.Api.Common.Results;
using Monolithic.Api.Modules.Identity.Domain;
using Monolithic.Api.Modules.Users.Contracts;

namespace Monolithic.Api.Modules.Users.Application;

public sealed class IdentityBackedUserService(
    UserManager<ApplicationUser> userManager,
    ITwoLevelCache cache) : IUserService
{
    private const string DefaultPassword = "TempPassword123!";

    // ── Admin-level list / create ──────────────────────────────────────────

    public async Task<IReadOnlyCollection<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = userManager.Users
            .Where(u => !u.IsDeleted)
            .OrderByDescending(u => u.CreatedAtUtc)
            .ToList();

        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            userDtos.Add(MapToDto(user, roles));
        }

        return userDtos.AsReadOnly();
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(userManager.Users.Count(u => !u.IsDeleted));

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email.Trim().ToLowerInvariant(),
            Email    = request.Email.Trim().ToLowerInvariant(),
            FullName = request.FullName.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(user, DefaultPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        var validRoles = request.Roles
            .Select(r => r.Trim())
            .Where(r => r.Length > 0)
            .Distinct()
            .ToList();

        if (validRoles.Count > 0)
        {
            var roleResult = await userManager.AddToRolesAsync(user, validRoles);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign roles: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
        }

        await cache.RemoveAsync(CacheKeys.DashboardRealtimeSnapshotV1, cancellationToken);
        return MapToDto(user, validRoles);
    }

    // ── Profile read / write (self-data + admin-elevated) ──────────────────

    public async Task<Result<UserProfileDto>> GetProfileAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());

        if (user is null || user.IsDeleted)
            return Error.NotFound("User.NotFound", $"User '{id}' was not found.");

        var roles = await userManager.GetRolesAsync(user);
        return MapToProfileDto(user, roles);
    }

    public async Task<Result<UserProfileDto>> UpdateProfileAsync(
        Guid id, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        // Inline validation
        var validator = new UpdateProfileRequestValidator();
        var validation = validator.Validate(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(f => f.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());

            return Error.Validation("UpdateProfile.Invalid",
                "One or more validation errors occurred.", errors);
        }

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null || user.IsDeleted)
            return Error.NotFound("User.NotFound", $"User '{id}' was not found.");

        user.FullName    = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errs = string.Join("; ", result.Errors.Select(e => e.Description));
            return Error.Validation("UpdateProfile.Failed", errs);
        }

        var roles = await userManager.GetRolesAsync(user);
        return MapToProfileDto(user, roles);
    }

    // ── Mappers ───────────────────────────────────────────────────────────

    private static UserDto MapToDto(ApplicationUser user, IEnumerable<string> roles)
        => new()
        {
            Id          = user.Id,
            FullName    = user.FullName,
            Email       = user.Email ?? string.Empty,
            Roles       = roles.ToArray(),
            Permissions = [],  // served by Identity module JWT
            CreatedAtUtc = user.CreatedAtUtc,
        };

    private static UserProfileDto MapToProfileDto(ApplicationUser user, IEnumerable<string> roles)
        => new()
        {
            Id           = user.Id,
            FullName     = user.FullName,
            Email        = user.Email ?? string.Empty,
            PhoneNumber  = user.PhoneNumber,
            IsActive     = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            LastLoginUtc = user.LastLoginUtc,
            Roles        = roles.ToArray(),
        };
}