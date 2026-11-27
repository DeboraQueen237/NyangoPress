using Microsoft.AspNetCore.Identity;
using NyangoPress.Domain.Identity;

namespace NyangoPress.Infrastructure.Identity;

/// <summary>Refuse les mots de passe très courants et ceux qui contiennent l'identifiant de l'utilisateur.</summary>
public sealed class CommonPasswordValidator : IPasswordValidator<AppUser>
{
    // Liste courte pour le projet. En production, remplacez-la par une vraie liste (ex. 10 000 mots de passe courants).
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password1234", "password12345", "password123456", "motdepasse123", "motdepasse1234",
        "azerty123456", "azertyuiop12", "azertyuiop123", "qwerty123456", "qwertyuiop12",
        "123456789012", "1234567890ab", "abcdef123456", "admin1234567", "administrator1",
        "welcome12345", "bienvenue123", "bonjour12345", "iloveyou1234", "letmein12345",
        "nyangopress1", "nyangopress12", "nyangopress123", "pressing1234", "pressing12345",
        "changeme1234", "changemoi1234", "p@ssw0rd1234", "passw0rd1234", "motdepasse12",
    };

    public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user, string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            // Les règles de longueur et de composition sont vérifiées par le validateur standard.
            return Task.FromResult(IdentityResult.Success);
        }

        var errors = new List<IdentityError>();

        if (CommonPasswords.Contains(password))
        {
            errors.Add(new IdentityError
            {
                Code = "PasswordTooCommon",
                Description = "Ce mot de passe est trop courant. Choisissez-en un autre.",
            });
        }

        if (!string.IsNullOrEmpty(user.UserName)
            && user.UserName.Length >= 4
            && password.Contains(user.UserName, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new IdentityError
            {
                Code = "PasswordContainsUserName",
                Description = "Le mot de passe ne doit pas contenir votre identifiant.",
            });
        }

        return Task.FromResult(errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray()));
    }
}
