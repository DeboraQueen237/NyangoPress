namespace NyangoPress.Application.Common;

public sealed record StoredFileResult(
    string FileName,
    string StoragePath,
    string PublicUrl,
    string ContentType,
    long SizeBytes);

public interface IFileStorage
{
    Task<StoredFileResult> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default);

    Task<StoredFileResult> SaveBase64ImageAsync(
        string dataUrl,
        string folder,
        string prefix,
        CancellationToken ct = default);

    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}
