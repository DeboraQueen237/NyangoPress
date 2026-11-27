namespace NyangoPress.Application.Common;

/// <summary>
/// Journal d'audit pour les actions sensibles (remise, annulation, remboursement, changement de rôle, connexion...).
/// Les créations/modifications d'entités sont déjà journalisées automatiquement par l'intercepteur EF ;
/// utilisez ce service pour les actions métier qui ne se résument pas à une modification de ligne (RG-13).
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Ajoute une ligne d'audit et enregistre immédiatement. Attention : les autres modifications en attente
    /// sur le contexte sont enregistrées en même temps ; appelez cette méthode après votre propre SaveChangesAsync.
    /// </summary>
    Task LogAsync(
        string action,
        string? entityName = null,
        string? entityId = null,
        object? oldValues = null,
        object? newValues = null,
        Guid? userId = null,
        string? userName = null,
        CancellationToken ct = default);
}
