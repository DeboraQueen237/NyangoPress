using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NyangoPress.Domain.Notifications;

namespace NyangoPress.Infrastructure.Notifications.Configurations;

// Découvertes automatiquement par AppDbContext (ApplyConfigurationsFromAssembly) : ne pas toucher AppDbContext.cs.

public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> b)
    {
        b.ToTable("notification_templates");
        b.Property(x => x.Key).HasMaxLength(100).IsRequired();
        b.Property(x => x.Language).HasMaxLength(5).IsRequired();
        b.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Subject).HasMaxLength(200);
        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        b.Property(x => x.ExternalTemplateName).HasMaxLength(100);
        b.HasIndex(x => new { x.Key, x.Channel, x.Language }).IsUnique();
    }
}

public sealed class NotificationMessageConfiguration : IEntityTypeConfiguration<NotificationMessage>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> b)
    {
        b.ToTable("notification_messages");
        b.Property(x => x.TemplateKey).HasMaxLength(100).IsRequired();
        b.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Priority).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.Language).HasMaxLength(5);
        b.Property(x => x.RecipientName).HasMaxLength(200).IsRequired();
        b.Property(x => x.RecipientAddress).HasMaxLength(200).IsRequired();
        b.Property(x => x.Subject).HasMaxLength(200);
        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        b.Property(x => x.VariablesJson).HasColumnType("jsonb");
        b.Property(x => x.AttachmentKeys).HasMaxLength(1000);
        b.Property(x => x.CorrelationId).HasMaxLength(150);
        b.Property(x => x.ProviderMessageId).HasMaxLength(150);
        b.Property(x => x.LastError).HasMaxLength(2000);

        // Index utilisé par l'OutboxProcessor pour prendre les messages à envoyer.
        b.HasIndex(x => new { x.Status, x.NextAttemptAt });
        // Anti-doublon : un même événement métier ne produit qu'un message par canal.
        b.HasIndex(x => new { x.CorrelationId, x.TemplateKey, x.Channel })
            .IsUnique()
            .HasFilter("\"CorrelationId\" IS NOT NULL");
        // Webhooks de statut WhatsApp/SMS.
        b.HasIndex(x => x.ProviderMessageId);

        b.HasMany(x => x.Attempts)
            .WithOne(a => a.NotificationMessage)
            .HasForeignKey(a => a.NotificationMessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class NotificationAttemptConfiguration : IEntityTypeConfiguration<NotificationAttempt>
{
    public void Configure(EntityTypeBuilder<NotificationAttempt> b)
    {
        b.ToTable("notification_attempts");
        b.Property(x => x.Error).HasMaxLength(2000);
        b.Property(x => x.ProviderResponse).HasMaxLength(2000);
    }
}

public sealed class InAppNotificationConfiguration : IEntityTypeConfiguration<InAppNotification>
{
    public void Configure(EntityTypeBuilder<InAppNotification> b)
    {
        b.ToTable("in_app_notifications");
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Body).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Link).HasMaxLength(300);
        b.HasIndex(x => new { x.UserId, x.IsRead });
    }
}
