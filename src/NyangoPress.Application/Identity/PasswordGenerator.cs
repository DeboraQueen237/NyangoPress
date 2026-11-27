using System.Security.Cryptography;

namespace NyangoPress.Application.Identity;

/// <summary>Génère des mots de passe temporaires aléatoires qui respectent la politique de mot de passe.</summary>
public static class PasswordGenerator
{
    // Sans caractères ambigus (0/O, 1/l/I) : le mot de passe est lu sur un message WhatsApp.
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%&*?";

    public static string Generate(int length = 14)
    {
        if (length < 12)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "12 caractères minimum.");
        }

        var all = Upper + Lower + Digits + Symbols;

        while (true)
        {
            var chars = new List<char> { Pick(Upper), Pick(Lower), Pick(Digits), Pick(Symbols) };
            while (chars.Count < length)
            {
                chars.Add(Pick(all));
            }

            Shuffle(chars);

            // Au moins 8 caractères différents : évite "AAAAaaaa1111!!"
            if (chars.Distinct().Count() >= 8)
            {
                return new string(chars.ToArray());
            }
        }
    }

    private static char Pick(string alphabet) => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];

    private static void Shuffle(List<char> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
