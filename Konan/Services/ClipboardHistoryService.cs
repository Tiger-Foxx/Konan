using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Konan.Configuration;
using Konan.Models;
using Konan.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Konan.Services;

/// <summary>
/// Service de gestion de l'historique du presse-papiers
/// 🦊 La mémoire éternelle de notre renard !
/// </summary>
public class ClipboardHistoryService
{
    private readonly IDataPersistence _persistence;
    private readonly FileService _fileService;
    private readonly string _historyPath;
    private readonly AppConfig _appConfig;

    public ClipboardHistoryService(IDataPersistence persistence, FileService fileService, AppConfig appConfig)
    {
        _persistence = persistence;
        _fileService = fileService;
        _appConfig = appConfig;
        _historyPath = Path.Combine(appConfig.DataPath, Constants.CLIPBOARD_HISTORY_FILE);
    }

    /// <summary>
    /// Charge l'historique depuis le fichier
    /// </summary>
    public async Task<List<ClipboardItem>> LoadHistoryAsync()
    {
        try
        {
            Console.WriteLine("🦊 Chargement de l'historique...");

            var history = await _persistence.LoadAsync<List<ClipboardItem>>(_historyPath);
            if (history == null)
            {
                Console.WriteLine("🦊 Aucun historique trouvé, création d'un nouvel historique");
                return new List<ClipboardItem>();
            }

            // Valider et nettoyer l'historique
            var validItems = new List<ClipboardItem>();
            foreach (var item in history)
            {
                if (await ValidateItemAsync(item))
                {
                    validItems.Add(item);
                }
            }

            // Charger dans le ClipboardService
            var clipboardService = App.Services?.GetService<ClipboardService>();
            if (clipboardService != null)
            {
                foreach (var item in validItems)
                {
                    clipboardService.AddItemManually(item);
                }
            }

            Console.WriteLine($"🦊 {validItems.Count} éléments chargés dans l'historique");
            return validItems;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur chargement historique: {ex.Message}");
            return new List<ClipboardItem>();
        }
    }

    /// <summary>
    /// Sauvegarde l'historique dans le fichier
    /// </summary>
    public async Task SaveHistoryAsync()
    {
        try
        {
            // Récupérer l'historique du service clipboard
            var clipboardService = App.Services?.GetService<ClipboardService>();
            if (clipboardService == null) return;

            var history = clipboardService.History.ToList();
            
            // Appliquer les limites de configuration
            var maxItems = _appConfig.Settings.MaxHistoryItems;
            if (maxItems > 0 && history.Count > maxItems)
            {
                history = history.Take(maxItems).ToList();
            }

            await _persistence.SaveAsync(history, _historyPath);
            Console.WriteLine($"🦊 {history.Count} éléments sauvegardés");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur sauvegarde historique: {ex.Message}");
        }
    }

    /// <summary>
    /// Effectue un nettoyage automatique basé sur les paramètres
    /// </summary>
    public async Task PerformAutoCleanupAsync()
    {
        try
        {
            var clipboardService = App.Services?.GetService<ClipboardService>();
            if (clipboardService == null) return;

            var history = clipboardService.History.ToList();
            var settings = _appConfig.Settings;
            var itemsToRemove = new List<ClipboardItem>();

            // Nettoyage par âge
            if (settings.AutoCleanupDays > 0)
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-settings.AutoCleanupDays);
                itemsToRemove.AddRange(history.Where(item => item.CreatedAt < cutoffDate));
            }

            // Nettoyage par nombre d'éléments
            if (settings.MaxHistoryItems > 0 && history.Count > settings.MaxHistoryItems)
            {
                var excessItems = history.Skip(settings.MaxHistoryItems);
                itemsToRemove.AddRange(excessItems);
            }

            // Supprimer les éléments invalides
            foreach (var item in history)
            {
                if (!await ValidateItemAsync(item))
                {
                    itemsToRemove.Add(item);
                }
            }

            // Effectuer le nettoyage
            foreach (var item in itemsToRemove.Distinct())
            {
                clipboardService.RemoveItem(item);
            }

            // Nettoyer les fichiers orphelins
            var remainingIds = clipboardService.History.Select(item => item.Id);
            await _fileService.CleanupOrphanedFilesAsync(remainingIds);

            if (itemsToRemove.Any())
            {
                await SaveHistoryAsync();
                Console.WriteLine($"🦊 Nettoyage automatique: {itemsToRemove.Count} éléments supprimés");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur nettoyage automatique: {ex.Message}");
        }
    }

    /// <summary>
    /// Vide complètement l'historique
    /// </summary>
    public async Task ClearHistoryAsync()
    {
        try
        {
            // Supprimer le fichier d'historique
            if (File.Exists(_historyPath))
            {
                await _persistence.DeleteAsync(_historyPath);
            }

            // Nettoyer tous les fichiers
            await _fileService.CleanupOrphanedFilesAsync(Enumerable.Empty<string>());

            Console.WriteLine("🦊 Historique complètement vidé !");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur vidage historique: {ex.Message}");
        }
    }

    /// <summary>
    /// Valide qu'un élément est toujours valide
    /// </summary>
    private async Task<bool> ValidateItemAsync(ClipboardItem item)
    {
        try
        {
            switch (item.Type)
            {
                case ClipboardItemType.Files:
                    if (item.Metadata.TryGetValue("FilePaths", out var pathsObj) && 
                        pathsObj is List<string> paths)
                    {
                        return paths.Any(File.Exists) || paths.Any(Directory.Exists);
                    }
                    break;

                case ClipboardItemType.Image:
                    if (!string.IsNullOrEmpty(item.Content))
                    {
                        return File.Exists(item.Content);
                    }
                    break;

                case ClipboardItemType.Text:
                case ClipboardItemType.RichText:
                    return true;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}