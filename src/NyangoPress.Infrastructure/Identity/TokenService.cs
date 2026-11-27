using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NyangoPress.Application.Identity;

namespace NyangoPress.Infrastructure.Identity;

public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    public AccessToken CreateAccessToken(TokenSubject subject)
    {
        var jwt = options.Value;
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(jwt.AccessTokenMinutes);

        // Noms de claims courts et fixes (l'API désactive la conversion automatique des noms).
        var claims = new List<Claim>
        {
            new("sub", subject.UserId.ToString()),
            new("name", subject.UserName),
            new("full_name", subject.FullName),
            new("role", subject.Role),
            new("branch", subject.BranchId.ToString()),
            new("jti", Guid.NewGuid().ToString("N")),
        };

        if (subject.MustChangePassword)
        {
            // Tant que ce claim est présent, toutes les politiques (sauf AuthenticatedAny) refusent l'accès.
            claims.Add(new Claim("must_change_pwd", "true"));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AccessToken(token, expires);
    }

    public string GenerateRefreshToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));

    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
