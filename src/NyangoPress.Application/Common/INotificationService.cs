namespace NyangoPress.Application.Common;

public enum NotificationChannel
{
    InApp = 0,
    Email = 1,
    Sms = 2,
    WhatsApp = 3
}

public sealed record NotificationAttachment(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed record NotificationRequest(
    string TemplateKey,
    Guid? CustomerId,
    Guid? UserId,
    string? Email,
    string? Phone,
    IReadOnlyDictionary<string, string> Variables,
    IReadOnlyList<NotificationAttachment>? Attachments = null,
    NotificationChannel[]? ForcedChannels = null,
    DateTimeOffset? SendAt = null);

public interface INotificationService
{
    Task EnqueueAsync(NotificationRequest request, CancellationToken ct = default);
}
