using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NyangoPress.Domain.Identity;
using NyangoPress.Infrastructure.Persistence;

namespace NyangoPress.Infrastructure.Identity;

/// <summary>Complète la table des utilisateurs créée par Identity avec nos colonnes.</summary>
public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.HasIndex(u => u.BranchId);
    }
}

public sealed class EmployeeProfileConfiguration : BaseEntityConfiguration<EmployeeProfile>
{
    protected override void ConfigureEntity(EntityTypeBuilder<EmployeeProfile> builder)
    {
        builder.ToTable("EmployeeProfiles");
        builder.Property(p => p.PhotoPath).HasMaxLength(300);
        builder.Property(p => p.WhatsAppNumber).HasMaxLength(30);
        builder.Property(p => p.Address).HasMaxLength(500);
        builder.Property(p => p.Language).HasMaxLength(5).IsRequired();
        builder.Property(p => p.Theme).HasMaxLength(10).IsRequired();
        builder.Property(p => p.Notes).HasMaxLength(2000);

        builder.HasIndex(p => p.UserId).IsUnique();
        builder.HasOne(p => p.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<EmployeeProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(t => t.CreatedByIp).HasMaxLength(64);
        builder.Property(t => t.UserAgent).HasMaxLength(300);
        builder.Property(t => t.RevokedReason).HasMaxLength(50);
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(128);

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.UserId);
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("LoginAttempts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.UserNameOrEmail).HasMaxLength(256).IsRequired();
        builder.Property(a => a.FailureReason).HasMaxLength(50);
        builder.Property(a => a.IpAddress).HasMaxLength(64);
        builder.HasIndex(a => a.At);
    }
}
