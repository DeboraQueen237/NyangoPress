using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NyangoPress.Domain.Common;
using NyangoPress.Shared.Common;

namespace NyangoPress.Api.Common;

public static class JwtAuthHelper
{
    public const string DefaultSigningKey = "NyangoPress-Dev-Signing-Key-2026-Minimum-64-Characters-Long-Secret!!";

    private sealed record DemoStaffAccount(Guid Id, string Email, string DisplayName, string Role, string DefaultRoute);

    private static readonly IReadOnlyList<DemoStaffAccount> DemoAccounts =
    [
        new(Guid.Parse("11111111-0000-0000-0000-000000000001"), "admin@nyangopress.cm", "Cabrel (Admin)", Roles.Admin, "/"),
        new(Guid.Parse("22222222-0000-0000-0000-000000000002"), "manager@nyangopress.cm", "Manager Pressing", Roles.Manager, "/"),
        new(Guid.Parse("33333333-0000-0000-0000-000000000003"), "caissier@nyangopress.cm", "Debora (Caisse)", Roles.Cashier, "/orders/new"),
        new(Guid.Parse("44444444-0000-0000-0000-000000000004"), "operateur@nyangopress.cm", "Opérateur Atelier", Roles.Operator, "/production"),
        new(Guid.Parse("55555555-5555-5555-5555-555555555555"), "livreur@nyangopress.cm", "Junior Tchinda (Livreur)", Roles.Driver, "/driver")
    ];

    public static void MapAuthEndpoints(this WebApplication app, IConfiguration configuration)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/demo-login", (DemoLoginRequest req) =>
        {
            var role = req.RoleCode.Trim().ToUpperInvariant();
            var account = DemoAccounts.FirstOrDefault(a => a.Role == role) ?? DemoAccounts[2];
            var displayName = string.IsNullOrWhiteSpace(req.DisplayName) ? account.DisplayName : req.DisplayName.Trim();
            var response = IssueToken(account.Id, displayName, account.Email, account.Role, account.DefaultRoute, configuration);
            return Results.Ok(response);
        }).AllowAnonymous();

        group.MapPost("/login", (LoginRequest req) =>
        {
            var identifier = req.Identifier.Trim().ToLowerInvariant();
            var account = DemoAccounts.FirstOrDefault(a =>
                a.Email.Equals(identifier, StringComparison.OrdinalIgnoreCase) ||
                a.Role.Equals(identifier, StringComparison.OrdinalIgnoreCase));

            if (account is null || string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 4)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(IssueToken(account.Id, account.DisplayName, account.Email, account.Role, account.DefaultRoute, configuration));
        }).AllowAnonymous();
    }

    public static AuthResponse IssueToken(
        Guid userId,
        string displayName,
        string email,
        string role,
        string defaultRoute,
        IConfiguration configuration)
    {
        var secret = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            secret = DefaultSigningKey;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(8);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new("branch_id", BaseEntity.DefaultBranchId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "NyangoPress",
            audience: "NyangoPress.Clients",
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthResponse(
            jwt,
            expiresAt,
            new UserInfoDto(userId, displayName, email, role, BaseEntity.DefaultBranchId, defaultRoute));
    }
}
