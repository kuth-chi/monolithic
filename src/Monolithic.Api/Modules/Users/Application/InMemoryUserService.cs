using Microsoft.AspNetCore.Identity;
using Monolithic.Api.Common.Caching;
using Monolithic.Api.Modules.Identity.Domain;
using Monolithic.Api.Modules.Users.Contracts;

namespace Monolithic.Api.Modules.Users.Application;

public sealed class IdentityBackedUserService(
    UserManager<ApplicationUser> userManager,
    ITwoLevelCache cache) : IUserService
{
    private const string DefaultPassword = "TempPassword123!";

    public async Task<IReadOnlyCollection<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = userManager.Users
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

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return null;

        var roles = await userManager.GetRolesAsync(user);
        return MapToDto(user, roles);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email.Trim().ToLowerInvariant(),
            Email = request.Email.Trim().ToLowerInvariant(),
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

        // Assign requested roles
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

        // Invalidate dashboard cache
        await cache.RemoveAsync(CacheKeys.DashboardRealtimeSnapshotV1, cancellationToken);

        return MapToDto(user, validRoles);
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(userManager.Users.Count());
    }

    private static UserDto MapToDto(ApplicationUser user, IEnumerable<string> roles)
    {
        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToArray(),
            Permissions = [], // Permissions come from Identity module, not Users module
            CreatedAtUtc = user.CreatedAtUtc
        };
    }
}