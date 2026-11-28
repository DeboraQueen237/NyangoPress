namespace NyangoPress.Shared.Common;

/// <summary>
/// Standard error payload returned by NyangoPress API endpoints.
/// </summary>
public sealed record ApiError(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);
