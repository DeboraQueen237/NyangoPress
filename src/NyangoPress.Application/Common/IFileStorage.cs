namespace NyangoPress.Application.Common;

/// <summary>
/// Stockage des fichiers (photos d'état, signatures, logos). Implémentation actuelle : disque local,
/// hors du dossier web. Types acceptés : JPEG, PNG, WebP, PDF ; taille maximale configurable.
/// </summary>
public interface IFileStorage
{
    /// <summary>Enregistre le fichier et retourne son chemin relatif (à stocker en base, jamais une URL).</summary>
    /// <param name="content">Contenu du fichier.</param>
    /// <param name="contentType">Type MIME déclaré (vérifié aussi sur le contenu réel).</param>
    /// <param name="folder">Sous-dossier logique, ex. "photos/profils" (lettres, chiffres, - et _ seulement).</param>
    Task<Result<string>> SaveAsync(Stream content, string contentType, string folder, CancellationToken ct = default);

    /// <summary>Ouvre un fichier précédemment enregistré ; null s'il n'existe pas.</summary>
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct = default);

    Task DeleteAsync(string relativePath, CancellationToken ct = default);
}
