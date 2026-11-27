namespace NyangoPress.Application.Common;

/// <summary>Utilisateur de la requête en cours (lu dans le jeton JWT). Contrat figé le jour 1.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? UserName { get; }

    /// <summary>Rôle de l'utilisateur (voir <c>Roles</c>).</summary>
    string? Role { get; }

    /// <summary>Succursale de l'utilisateur ; succursale par défaut si personne n'est connecté.</summary>
    Guid BranchId { get; }

    string? IpAddress { get; }

    bool IsInRole(string role);
}
