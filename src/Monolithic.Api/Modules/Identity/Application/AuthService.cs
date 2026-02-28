using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Monolithic.Api.Common.Configuration;
using Monolithic.Api.Modules.Identity.Contracts;
using Monolithic.Api.Modules.Identity.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Identity.Application;

/// <inheritdoc cref="IAuthService"/>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly JwtOptions _jwt;
    private readonly IAuthAuditLogger _audit;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IOptions<JwtOptions> jwtOptions,
        IAuthAuditLogger audit)
    {
        _userManager = userManager;
        _db = db;
        _jwt = jwtOptions.Value;
        _audit = audit;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        // Always check password even when user not found to prevent timing attacks
        var dummyUser = new ApplicationUser();
        var passwordValid = user is not null
            ? await _userManager.CheckPasswordAsync(user, request.Password)
            : await _userManager.CheckPasswordAsync(dummyUser, request.Password);

        if (user is null || !user.IsActive)
        {
            await _audit.LogLoginFailedAsync(request.Email,
                user is null ? "User not found" : "Account inactive", cancellationToken);
            return null;
        }

        if (!passwordValid)
        {
            await _audit.LogLoginFailedAsync(request.Email, "Invalid password", cancellationToken);
            return null;
        }

        // Stamp last login
        user.LastLoginUtc = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);

        var (roles, permissions) = await LoadRolesAndPermissionsAsync(user.Id, cancellationToken);
        var memberships = await LoadMembershipsAsync(user.Id, cancellationToken);
        var activeBusiness = memberships.FirstOrDefault(m => m.IsDefault && m.IsActive)
                             ?? memberships.FirstOrDefault(m => m.IsActive);

        await _audit.LogLoginSuccessAsync(user.Id, user.Email!, activeBusiness?.BusinessId, cancellationToken);

        var token = BuildAccessToken(user, roles, permissions, activeBusiness);
        var profile = BuildMeResponse(user, memberships, activeBusiness, roles, permissions);

        return new LoginResponse(
            AccessToken: new JwtSecurityTokenHandler().WriteToken(token),
            TokenType: "Bearer",
            ExpiresIn: _jwt.ExpiryMinutes * 60,
            User: profile);
    }

    public async Task<SignUpResponse?> SignUpAsync(
        SignUpRequest request,
        CancellationToken cancellationToken = default)
    {
        // Return null (→ 409) if the email is already registered
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            await _audit.LogSignUpFailedAsync(request.Email, "Email already registered", cancellationToken);
            return null;
        }

        // ── First-user-as-Owner rule ────────────────────────────────────────
        // On a fresh Docker install there are no application users yet.
        // The very first account to sign up is automatically granted the
        // Owner role so they can complete the business creation wizard
        // immediately after signup without any manual role assignment.
        // Subsequent signups receive the baseline "User" role.
        var isFirstUser = !await _userManager.Users.AnyAsync(cancellationToken);

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email    = request.Email,
            FullName = request.FullName,
            IsActive = true,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            await _audit.LogSignUpFailedAsync(request.Email, errors, cancellationToken);
            throw new InvalidOperationException($"User creation failed: {errors}");
        }

        // Assign Owner to the first user; all subsequent accounts start as User.
        var defaultRole = isFirstUser ? SystemRoleNames.Owner : "User";
        await _userManager.AddToRoleAsync(user, defaultRole);

        // New user has no business memberships yet
        var (roles, permissions) = await LoadRolesAndPermissionsAsync(user.Id, cancellationToken);
        var token   = BuildAccessToken(user, roles, permissions, activeBusiness: null);
        var profile = BuildMeResponse(user, memberships: [], activeBusiness: null, roles, permissions);

        await _audit.LogSignUpSuccessAsync(user.Id, user.Email!, cancellationToken);

        return new SignUpResponse(
            AccessToken: new JwtSecurityTokenHandler().WriteToken(token),
            TokenType: "Bearer",
            ExpiresIn: _jwt.ExpiryMinutes * 60,
            User: profile);
    }

    public async Task<SwitchBusinessResponse?> SwitchDefaultBusinessAsync(
        Guid userId,
        Guid targetBusinessId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
            return null;

        // Ensure the user has an active membership in the requested business
        var target = await _db.UserBusinesses
            .Include(ub => ub.Business)
            .FirstOrDefaultAsync(
                ub => ub.UserId == userId && ub.BusinessId == targetBusinessId && ub.IsActive,
                cancellationToken);

        if (target is null)
            return null; // user doesn't belong to that business

        // Capture previous default before clearing it
        var previousDefault = await _db.UserBusinesses
            .Where(ub => ub.UserId == userId && ub.IsDefault && ub.BusinessId != targetBusinessId)
            .Select(ub => ub.BusinessId)
            .FirstOrDefaultAsync(cancellationToken);

        // ── Enforce single-default rule in one round-trip ─────────────────────
        await _db.UserBusinesses
            .Where(ub => ub.UserId == userId && ub.IsDefault && ub.BusinessId != targetBusinessId)
            .ExecuteUpdateAsync(s => s.SetProperty(ub => ub.IsDefault, false), cancellationToken);

        target.IsDefault = true;
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogBusinessSwitchedAsync(
            userId, user.Email!,
            previousDefault == Guid.Empty ? targetBusinessId : previousDefault,
            targetBusinessId, cancellationToken);

        var (roles, permissions) = await LoadRolesAndPermissionsAsync(userId, cancellationToken);
        var jwtToken = BuildAccessToken(user, roles, permissions, target);
        var summary = ToSummary(target);

        return new SwitchBusinessResponse(
            AccessToken: new JwtSecurityTokenHandler().WriteToken(jwtToken),
            TokenType: "Bearer",
            ExpiresIn: _jwt.ExpiryMinutes * 60,
            NewDefaultBusiness: summary);
    }

    public async Task<MeResponse?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return null;

        var (roles, permissions) = await LoadRolesAndPermissionsAsync(userId, cancellationToken);
        var memberships = await LoadMembershipsAsync(userId, cancellationToken);
        var activeBusiness = memberships.FirstOrDefault(m => m.IsDefault && m.IsActive)
                             ?? memberships.FirstOrDefault(m => m.IsActive);

        return BuildMeResponse(user, memberships, activeBusiness, roles, permissions);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Loads all active UserBusiness memberships (with Business navigation) for a user.
    /// </summary>
    private async Task<List<UserBusiness>> LoadMembershipsAsync(
        Guid userId, CancellationToken ct)
    {
        return await _db.UserBusinesses
            .Include(ub => ub.Business)
            .Where(ub => ub.UserId == userId && ub.IsActive)
            .OrderByDescending(ub => ub.IsDefault)
            .ThenBy(ub => ub.Business.Name)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns all roles and the effective permission set for the user.
    /// Permissions are derived from role assignments + per-user grants.
    /// </summary>
    private async Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)>
        LoadRolesAndPermissionsAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return ([], []);

        var roles = (await _userManager.GetRolesAsync(user)).AsReadOnly();

        // Role-based permissions
        var rolePermissions = await (
            from rp in _db.RolePermissions
            join r in _db.Roles on rp.RoleId equals r.Id
            join p in _db.Permissions on rp.PermissionId equals p.Id
            where roles.Contains(r.Name!)
            select p.Name
        ).Distinct().ToListAsync(ct);

        // Direct user-level grants
        var userPermissions = await (
            from up in _db.UserPermissions
            join p in _db.Permissions on up.PermissionId equals p.Id
            where up.UserId == userId && up.Granted
            select p.Name
        ).Distinct().ToListAsync(ct);

        var allPermissions = rolePermissions.Union(userPermissions)
            .OrderBy(x => x)
            .ToList()
            .AsReadOnly();

        return (roles, allPermissions);
    }

    /// <summary>
    /// Builds a signed JWT with identity claims, active-business context, roles and permissions.
    /// </summary>
    private JwtSecurityToken BuildAccessToken(
        ApplicationUser user,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions,
        UserBusiness? activeBusiness)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        // Business-context claims (scoped data filter downstream)
        if (activeBusiness is not null)
        {
            claims.Add(new Claim(AppClaimTypes.BusinessId, activeBusiness.BusinessId.ToString()));
            claims.Add(new Claim(AppClaimTypes.BusinessName, activeBusiness.Business.Name));
        }

        // Role claims
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        // Permission claims — drives the PermissionAuthorizationHandler
        claims.AddRange(permissions.Select(p => new Claim(AppClaimTypes.Permission, p)));

        return new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_jwt.ExpiryMinutes),
            signingCredentials: creds);
    }

    /// <summary>Assembles a <see cref="MeResponse"/> from already-loaded data.</summary>
    private static MeResponse BuildMeResponse(
        ApplicationUser user,
        IReadOnlyList<UserBusiness> memberships,
        UserBusiness? activeBusiness,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions)
    {
        return new MeResponse(
            UserId: user.Id,
            Email: user.Email ?? string.Empty,
            FullName: user.FullName,
            ActiveBusiness: activeBusiness is not null ? ToSummary(activeBusiness) : null,
            AllBusinesses: memberships.Select(ToSummary).ToList(),
            Roles: roles,
            Permissions: permissions);
    }

    private static UserBusinessSummary ToSummary(UserBusiness ub) =>
        new(ub.BusinessId, ub.Business.Name, ub.Business.Code, ub.IsDefault, ub.IsActive);
}
