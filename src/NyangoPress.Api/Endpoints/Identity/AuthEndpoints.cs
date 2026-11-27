using NyangoPress.Api.Common;
using NyangoPress.Application.Common;
using NyangoPress.Application.Identity;

namespace NyangoPress.Api.Endpoints.Identity;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentification");

        // Publics (pas encore de jeton) mais limités à 10 appels par minute et par adresse IP.
        group.MapPost("/login", async (LoginRequest request, AuthService auth, CancellationToken ct) =>
                (await auth.LoginAsync(request, ct)).ToHttpResult())
            .AllowAnonymous()
            .RequireRateLimiting("login")
            .WithValidation<LoginRequest>();

        group.MapPost("/refresh", async (RefreshRequest request, AuthService auth, CancellationToken ct) =>
                (await auth.RefreshAsync(request.RefreshToken, ct)).ToHttpResult())
            .AllowAnonymous()
            .RequireRateLimiting("login")
            .WithValidation<RefreshRequest>();

        // Réservés aux utilisateurs connectés, y compris ceux qui doivent encore changer leur mot de passe.
        group.MapPost("/logout", async (LogoutRequest request, AuthService auth, CancellationToken ct) =>
                (await auth.LogoutAsync(request.RefreshToken, ct)).ToHttpResult())
            .RequireAuthorization(Policies.AuthenticatedAny)
            .WithValidation<LogoutRequest>();

        group.MapPost("/change-password", async (ChangePasswordRequest request, ICurrentUser user, AuthService auth, CancellationToken ct) =>
                user.UserId is { } id
                    ? (await auth.ChangePasswordAsync(id, request, ct)).ToHttpResult()
                    : Results.Unauthorized())
            .RequireAuthorization(Policies.AuthenticatedAny)
            .WithValidation<ChangePasswordRequest>();

        group.MapGet("/me", async (ICurrentUser user, AuthService auth, CancellationToken ct) =>
                user.UserId is { } id
                    ? (await auth.GetCurrentUserAsync(id, ct)).ToHttpResult()
                    : Results.Unauthorized())
            .RequireAuthorization(Policies.AuthenticatedAny);

        return app;
    }
}
