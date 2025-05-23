using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Konan.Persistence;

/// <summary>
/// Service de persistence JSON pour Konan
/// 🦊 La mémoire JSON de notre renard !
/// </summary>
public class JsonPersistenceService : IDataPersistence
{
    private readonly JsonSerializerSettings _jsonSettings;

    public JsonPersistenceService()
    {
        _jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            TypeNameHandling = TypeNameHandling.Auto
        };
    }

    /// <summary>
    /// Sauvegarde un objet en JSON de manière asynchrone
    /// </summary>
    public async Task SaveAsync<T>(T data, string filePath)
    {
        try
        {
            // S'assurer que le dossier parent existe
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonConvert.SerializeObject(data, _jsonSettings);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"🦊 Erreur sauvegarde {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Charge un objet depuis un fichier JSON
    /// </summary>
    public T? Load<T>(string filePath) where T : class
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath, Encoding.UTF8);
            return JsonConvert.DeserializeObject<T>(json, _jsonSettings);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur chargement {filePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Charge un objet de manière asynchrone
    /// </summary>
    public async Task<T?> LoadAsync<T>(string filePath) where T : class
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            return JsonConvert.DeserializeObject<T>(json, _jsonSettings);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur chargement async {filePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Vérifie si un fichier existe
    /// </summary>
    public bool Exists(string filePath)
    {
        return File.Exists(filePath);
    }

    /// <summary>
    /// Supprime un fichier de manière asynchrone
    /// </summary>
    public async Task DeleteAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"🦊 Erreur suppression {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Sauvegarde des données binaires
    /// </summary>
    public async Task SaveBinaryAsync(byte[] data, string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(filePath, data);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"🦊 Erreur sauvegarde binaire {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Charge des données binaires
    /// </summary>
    public async Task<byte[]?> LoadBinaryAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            return await File.ReadAllBytesAsync(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur chargement binaire {filePath}: {ex.Message}");
            return null;
        }
    }
}