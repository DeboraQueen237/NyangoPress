using NyangoPress.Application.Common;

namespace NyangoPress.Infrastructure.Common;

public sealed class LocalFileStorage : IFileStorage
{
    private const long MaxFileBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".pdf"
    };

    private readonly string _rootPath;

    public LocalFileStorage(string? rootPath = null)
    {
        _rootPath = rootPath ?? Path.Combine(AppContext.BaseDirectory, "AppData", "uploads");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<StoredFileResult> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"Extension de fichier non autorisée : {ext}");

        var safeFolder = SanitizeSegment(folder);
        var targetDir = Path.Combine(_rootPath, safeFolder);
        Directory.CreateDirectory(targetDir);

        var generatedName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(targetDir, generatedName);

        await using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        if (ms.Length > MaxFileBytes)
            throw new InvalidOperationException("La taille maximale autorisée est de 5 Mo.");

        await File.WriteAllBytesAsync(fullPath, ms.ToArray(), ct);
        var relPath = $"{safeFolder}/{generatedName}";
        return new StoredFileResult(generatedName, relPath, $"/uploads/{relPath}", contentType, ms.Length);
    }

    public async Task<StoredFileResult> SaveBase64ImageAsync(
        string dataUrl,
        string folder,
        string prefix,
        CancellationToken ct = default)
    {
        var cleanBase64 = dataUrl.Trim();
        var contentType = "image/png";
        var ext = ".png";

        if (cleanBase64.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var commaIdx = cleanBase64.IndexOf(',');
            if (commaIdx <= 0)
                throw new InvalidOperationException("Format data URL invalide.");

            var header = cleanBase64[5..commaIdx];
            if (header.Contains("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
                header.Contains("image/jpg", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "image/jpeg";
                ext = ".jpg";
            }
            else if (header.Contains("image/webp", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "image/webp";
                ext = ".webp";
            }

            cleanBase64 = cleanBase64[(commaIdx + 1)..];
        }

        var bytes = Convert.FromBase64String(cleanBase64);
        if (bytes.Length > MaxFileBytes)
            throw new InvalidOperationException("L'image dépasse la taille maximale de 5 Mo.");

        var safeFolder = SanitizeSegment(folder);
        var safePrefix = SanitizeSegment(prefix);
        var targetDir = Path.Combine(_rootPath, safeFolder);
        Directory.CreateDirectory(targetDir);

        var generatedName = $"{safePrefix}-{Guid.NewGuid().ToString("N")[..8]}{ext}";
        var fullPath = Path.Combine(targetDir, generatedName);
        await File.WriteAllBytesAsync(fullPath, bytes, ct);

        var relPath = $"{safeFolder}/{generatedName}";
        return new StoredFileResult(generatedName, relPath, $"/uploads/{relPath}", contentType, bytes.Length);
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_rootPath, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    private static string SanitizeSegment(string value)
    {
        var clean = new string(value.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "general" : clean.ToLowerInvariant();
    }
}
