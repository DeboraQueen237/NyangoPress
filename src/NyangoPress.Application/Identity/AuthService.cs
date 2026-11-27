using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NyangoPress.Application.Common;
using NyangoPress.Domain.Identity;

namespace NyangoPress.Application.Identity;

/// <summary>
/// Connexion, jetons et mots de passe (AUTH-01 à AUTH-05). Règles de sécurité :
/// messages d'erreur génériques (on ne dit jamais si c'est l'identifiant ou le mot de passe qui est faux),
/// verrouillage après 5 échecs, jeton de rafraîchissement à rotation avec détection de réutilisation.
/// </summary>
public sealed class AuthService(
    UserManager<AppUser> userManager,
    IAppDbContext db,
    ITokenService tokens,
    ICurrentUser currentUser,
    IAuditService audit,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider clock)
{
    private static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "Identifiant ou mot de passe incorrect.");

    private static readonly Error InvalidTwoFactorCode =
        Error.Unauthorized("Auth.InvalidTwoFactorCode", "Code de vérification incorrect.");

    private static readonly Error InvalidRefreshToken =
        Error.Unauthorized("Auth.InvalidRefreshToken", "Session expirée. Reconnectez-vous.");

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var identifier = request.UserNameOrEmail.Trim();

        var user = await userManager.FindByNameAsync(identifier)
                   ?? await userManager.FindByEmailAsync(identifier);

        if (user is null || !user.IsActive)
        {
            await RecordAttemptAsync(identifier, false, user is null ? "UnknownUser" : "InactiveUser", ct);
            return InvalidCredentials;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            await RecordAttemptAsync(identifier, false, "LockedOut", ct);
            return InvalidCredentials;
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            // Incrémente le compteur ; le compte est verrouillé 15 minutes au 5e échec.
            await userManager.AccessFailedAsync(user);
            await RecordAttemptAsync(identifier, false, "BadPassword", ct);
            return InvalidCredentials;
        }

        if (user.TwoFactorEnabled)
        {
            var hasTotp = !string.IsNullOrWhiteSpace(request.TotpCode);
            var hasRecovery = !string.IsNullOrWhiteSpace(request.RecoveryCode);

            if (!hasTotp && !hasRecovery)
            {
                return new AuthResponse(true, null, null, null, false, null);
            }

            bool secondFactorOk;
            if (hasRecovery)
            {
                var redeem = await userManager.RedeemTwoFactorRecoveryCodeAsync(user, request.RecoveryCode!.Trim());
                secondFactorOk = redeem.Succeeded;
            }
            else
            {
                var code = request.TotpCode!.Replace(" ", string.Empty).Replace("-", string.Empty);
                secondFactorOk = await userManager.VerifyTwoFactorTokenAsync(
                    user,
                    userManager.Options.Tokens.AuthenticatorTokenProvider,
                    code);
            }

            if (!secondFactorOk)
            {
                await userManager.AccessFailedAsync(user);
                await RecordAttemptAsync(identifier, false, "BadTwoFactorCode", ct);
                return InvalidTwoFactorCode;
            }
        }

        await userManager.ResetAccessFailedCountAsync(user);
        user.LastLoginAt = clock.GetUtcNow();
        await userManager.UpdateAsync(user);

        await RecordAttemptAsync(identifier, true, null, ct);
        await audit.LogAsync("Auth.Login", nameof(AppUser), user.Id.ToString(), userId: user.Id, userName: user.UserName, ct: ct);

        return await IssueTokensAsync(user, null, ct);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(refreshToken);
        var stored = await db.Set<RefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        var now = clock.GetUtcNow();

        if (stored is null)
        {
            return InvalidRefreshToken;
        }

        if (stored.RevokedAt is not null)
        {
            // Un jeton déjà utilisé est présenté une 2e fois : vol possible. On coupe toutes les sessions de l'utilisateur.
            await RevokeAllSessionsAsync(stored.UserId, "ReuseDetected", ct);
            await audit.LogAsync("Auth.RefreshTokenReuse", nameof(AppUser), stored.UserId.ToString(), userId: stored.UserId, ct: ct);
            return InvalidRefreshToken;
        }

        if (stored.ExpiresAt <= now)
        {
            return InvalidRefreshToken;
        }

        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || !user.IsActive || await userManager.IsLockedOutAsync(user))
        {
            stored.RevokedAt = now;
            stored.RevokedReason = "UserNotAllowed";
            await db.SaveChangesAsync(ct);
            return InvalidRefreshToken;
        }

        stored.RevokedAt = now;
        stored.RevokedReason = "Rotated";

        return await IssueTokensAsync(user, stored, ct);
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(refreshToken);
        var stored = await db.Set<RefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is not null && stored.RevokedAt is null)
        {
            stored.RevokedAt = clock.GetUtcNow();
            stored.RevokedReason = "Logout";
            await db.SaveChangesAsync(ct);
            await audit.LogAsync("Auth.Logout", nameof(AppUser), stored.UserId.ToString(), userId: stored.UserId, ct: ct);
        }

        // Toujours un succès : on ne révèle pas si le jeton existait.
        return Result.Success();
    }

    /// <summary>Révoque tous les jetons de rafraîchissement d'un utilisateur (désactivation, changement de mot de passe...).</summary>
    public async Task RevokeAllSessionsAsync(Guid userId, string reason, CancellationToken ct)
    {
        var now = clock.GetUtcNow();

        var active = await db.Set<RefreshToken>()
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in active)
        {
            token.RevokedAt = now;
            token.RevokedReason = reason;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<Result<AuthResponse>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return InvalidCredentials;
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var first = result.Errors.First();
            return Error.Validation(
                "Password." + first.Code,
                string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        user.MustChangePassword = false;
        await userManager.UpdateAsync(user);

        // Les anciennes sessions ne sont plus valables ; une nouvelle paire de jetons est renvoyée.
        await RevokeAllSessionsAsync(user.Id, "PasswordChanged", ct);
        await audit.LogAsync("Auth.PasswordChanged", nameof(AppUser), user.Id.ToString(), userId: user.Id, userName: user.UserName, ct: ct);

        return await IssueTokensAsync(user, null, ct);
    }

    public async Task<Result<UserSummary>> GetCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return Error.Unauthorized("Auth.UserNotFound", "Compte introuvable ou désactivé.");
        }

        return await BuildSummaryAsync(user, ct);
    }

    private async Task<AuthResponse> IssueTokensAsync(AppUser user, RefreshToken? replaced, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var summary = await BuildSummaryAsync(user, ct);

        var access = tokens.CreateAccessToken(new TokenSubject(
            user.Id,
            user.UserName ?? user.Email ?? string.Empty,
            user.FullName,
            summary.Role,
            user.BranchId,
            user.MustChangePassword));

        var refreshValue = tokens.GenerateRefreshToken();
        var refreshHash = tokens.HashRefreshToken(refreshValue);

        db.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            CreatedAt = now,
            ExpiresAt = now.AddHours(jwtOptions.Value.RefreshTokenHours),
            CreatedByIp = currentUser.IpAddress,
        });

        if (replaced is not null)
        {
            replaced.ReplacedByTokenHash = refreshHash;
        }

        await db.SaveChangesAsync(ct);

        return new AuthResponse(false, access.Token, access.ExpiresAt, refreshValue, user.MustChangePassword, summary);
    }

    private async Task<UserSummary> BuildSummaryAsync(AppUser user, CancellationToken ct)
    {
        var role = (await userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;
        var profile = await db.Set<EmployeeProfile>().AsNoTracking().FirstOrDefaultAsync(p => p.UserId == user.Id, ct);

        return new UserSummary(
            user.Id,
            user.UserName ?? string.Empty,
            user.FullName,
            user.Email,
            role,
            profile?.Language ?? "fr",
            profile?.Theme ?? "system",
            user.MustChangePassword,
            user.TwoFactorEnabled);
    }

    private async Task RecordAttemptAsync(string identifier, bool succeeded, string? reason, CancellationToken ct)
    {
        db.Set<LoginAttempt>().Add(new LoginAttempt
        {
            UserNameOrEmail = identifier.Length > 256 ? identifier[..256] : identifier,
            Succeeded = succeeded,
            FailureReason = reason,
            IpAddress = currentUser.IpAddress,
            At = clock.GetUtcNow(),
        });

        await db.SaveChangesAsync(ct);
    }
}
