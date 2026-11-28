using Microsoft.Extensions.Logging.Abstractions;
using NyangoPress.Application.Notifications;
using NyangoPress.Domain.Notifications;
using NyangoPress.Infrastructure.Notifications;
using Xunit;

namespace NyangoPress.UnitTests.Notifications;

public class FakeNotificationServiceTests
{
    private readonly FakeNotificationService _sut = new(NullLogger<FakeNotificationService>.Instance);

    [Fact]
    public async Task Garde_la_demande_et_respecte_les_canaux_demandes()
    {
        var receipt = await _sut.SendAsync(new NotificationRequest
        {
            TemplateKey = NotificationTemplateKeys.AccountCreated,
            Recipient = NotificationRecipient.ForUser(Guid.NewGuid(), "Awa", "awa@test.cm", null),
            Channels = [NotificationChannel.Email]
        });

        Assert.True(receipt.Queued);
        Assert.Equal(new[] { NotificationChannel.Email }, receipt.Channels);
        Assert.Single(_sut.Sent);
    }

    [Fact]
    public async Task Sans_canal_precise_choisit_selon_les_coordonnees()
    {
        var receipt = await _sut.SendAsync(new NotificationRequest
        {
            TemplateKey = NotificationTemplateKeys.OrderReady,
            Recipient = NotificationRecipient.ForCustomer(Guid.NewGuid(), "Jean", null, "+237600000000")
        });

        Assert.Equal(new[] { NotificationChannel.WhatsApp }, receipt.Channels);
    }
}
