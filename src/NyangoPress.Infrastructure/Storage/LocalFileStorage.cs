using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using NyangoPress.Application.Common;

namespace NyangoPress.Infrastructure.Storage;

public sealed class StorageOptions
{
    /// <summary>Dossier de stockage, HORS du dossier web. En production : un volume dédié et sauvegardé.</summary>
    public string RootPath { get; set; } = "storage";

    public long MaxFileBytes { get; set; } = 5 * 1024 * 1024;
}

public sealed partial class LocalFileStorage(IOptions<StorageOptions> options) : IFileStorage
{
    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["application/pdf"] = ".pdf",
    };

    private readonly string _root = Path.GetFullPath(options.Value.RootPath);

    public async Task<Result<string>> SaveAsync(Stream content, string contentType, string folder, CancellationToken ct = default)
    {
        if (!AllowedTypes.TryGetValue(contentType ?? string.Empty, out var extension))
        {
            return Error.Validation("File.TypeNotAllowed", "Type de fichier non autorisé (JPEG, PNG, WebP ou PDF).");
        }

        if (!FolderPattern().IsMatch(folder))
        {
            return Error.Validation("File.InvalidFolder", "Dossier de stockage invalide.");
        }

        var limit = options.Value.MaxFileBytes;
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk.AsMemory(), ct)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > limit)
            {
                return Error.Validation("File.TooLarge", $"Fichier trop volumineux (maximum {limit / 1024 / 1024} Mo).");
            }
        }

        var bytes = buffer.ToArray();
        if (bytes.Length == 0)
        {
            return Error.Validation("File.Empty", "Le fichier est vide.");
        }

        // On ne fait pas confiance au type MIME envoyé par le client : on vérifie les premiers octets.
        if (!SignatureMatches(bytes, extension))
        {
            return Error.Validation("File.ContentMismatch", "Le contenu du fichier ne correspond pas à son type.");
        }

        // Nom généré : le nom d'origine (potentiellement piégé) n'est jamais utilisé.
        var relativePath = $"{folder}/{Guid.CreateVersion7():N}{extension}";
        var fullPath = Resolve(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, bytes, ct);

        return relativePath;
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct = default)
    {
        try
        {
            var fullPath = Resolve(relativePath);
            if (!File.Exists(fullPath))
            {
                return Task.FromResult<Stream?>(null);
            }

            Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            return Task.FromResult<Stream?>(stream);
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult<Stream?>(null);
        }
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        try
        {
            var fullPath = Resolve(relativePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (InvalidOperationException)
        {
            // Chemin invalide : rien à supprimer.
        }

        return Task.CompletedTask;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+(/[A-Za-z0-9_-]+)*$")]
    private static partial Regex FolderPattern();

    private static bool SignatureMatches(byte[] b, string extension) => extension switch
    {
        ".jpg" => b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,
        ".png" => b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47,
        ".webp" => b.Length > 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F'
                   && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P',
        ".pdf" => b.Length > 4 && b[0] == '%' && b[1] == 'P' && b[2] == 'D' && b[3] == 'F',
        _ => false,
    };

    /// <summary>Résout le chemin et refuse toute sortie du dossier de stockage (attaque "../").</summary>
    private string Resolve(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(_root, relativePath));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Chemin de fichier invalide.");
        }

        return full;
    }
}
