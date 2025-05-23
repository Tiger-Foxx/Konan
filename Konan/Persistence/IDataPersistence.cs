namespace Konan.Persistence;

/// <summary>
/// Interface pour la persistence des données
/// 🦊 Comment notre renard se souvient de tout !
/// </summary>
public interface IDataPersistence
{
    /// <summary>
    /// Sauvegarde un objet de manière asynchrone
    /// </summary>
    Task SaveAsync<T>(T data, string filePath);

    /// <summary>
    /// Charge un objet depuis un fichier
    /// </summary>
    T? Load<T>(string filePath) where T : class;

    /// <summary>
    /// Charge un objet de manière asynchrone
    /// </summary>
    Task<T?> LoadAsync<T>(string filePath) where T : class;

    /// <summary>
    /// Vérifie si un fichier existe
    /// </summary>
    bool Exists(string filePath);

    /// <summary>
    /// Supprime un fichier
    /// </summary>
    Task DeleteAsync(string filePath);

    /// <summary>
    /// Sauvegarde des données binaires
    /// </summary>
    Task SaveBinaryAsync(byte[] data, string filePath);

    /// <summary>
    /// Charge des données binaires
    /// </summary>
    Task<byte[]?> LoadBinaryAsync(string filePath);
}