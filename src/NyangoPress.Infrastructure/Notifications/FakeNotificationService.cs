using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NyangoPress.Application.Notifications;
using NyangoPress.Domain.Notifications;

namespace NyangoPress.Infrastructure.Notifications;

/// <summary>
/// Implémentation factice de J1 : n'envoie rien, écrit dans les logs et garde les demandes en mémoire.
/// - Dev 1 / Dev 3 : utilisable tout de suite pour coder et tester leurs appels.
/// - Tests : résolvez FakeNotificationService et vérifiez <see cref="Sent"/>.
/// Remplacée par NotificationService (outbox) en J2 via Notifications:UseFake = false.
/// </summary>
public sealed class FakeNotificationService(ILogger<FakeNotificationService> logger) : INotificationService
{
    private readonly ConcurrentQueue<NotificationRequest> _sent = new();

    public IReadOnlyCollection<NotificationRequest> Sent => _sent.ToArray();

    public void Clear() => _sent.Clear();

    public Task<NotificationReceipt> SendAsync(NotificationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TemplateKey);

        var channels = request.Channels is { Count: > 0 }
            ? request.Channels
            : DefaultChannels(request.Recipient);

        _sent.Enqueue(request);

        // On ne logge jamais la valeur des variables : elles peuvent contenir un mot de passe temporaire.
        logger.LogInformation(
            "[FAKE] Notification {TemplateKey} pour {RecipientName} sur {Channels} (variables : {VariableNames})",
            request.TemplateKey, request.Recipient.Name, string.Join(", ", channels),
            string.Join(", ", request.Variables.Keys));

        var ids = channels.Select(_ => Guid.NewGuid()).ToList();
        return Task.FromResult(new NotificationReceipt(ids, channels.ToList()));
    }

    public Task<int> CancelPendingAsync(string correlationId, CancellationToken ct = default)
    {
        logger.LogInformation("[FAKE] Annulation des messages en attente pour {CorrelationId}", correlationId);
        return Task.FromResult(0);
    }

    private static IReadOnlyList<NotificationChannel> DefaultChannels(NotificationRecipient r)
    {
        var list = new List<NotificationChannel>();
        if (r.UserId is not null) list.Add(NotificationChannel.InApp);
        if (!string.IsNullOrWhiteSpace(r.Phone)) list.Add(NotificationChannel.WhatsApp);
        if (!string.IsNullOrWhiteSpace(r.Email)) list.Add(NotificationChannel.Email);
        return list;
    }
}
