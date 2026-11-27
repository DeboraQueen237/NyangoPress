namespace NyangoPress.Domain.Identity;

/// <summary>Tentative de connexion (réussie ou non), pour l'analyse de sécurité.</summary>
public class LoginAttempt
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string UserNameOrEmail { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string? FailureReason { get; set; }

    public string? IpAddress { get; set; }

    public DateTimeOffset At { get; set; }
}
