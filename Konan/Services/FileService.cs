using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Konan.Configuration;
using Konan.Models;

namespace Konan.Services;

/// <summary>
/// Service de gestion des fichiers pour Konan
/// 🦊 Notre renard organisateur de fichiers !
/// </summary>
public class FileService
{
    private readonly string _dataPath;
    private readonly string _previewPath;

    public FileService(string dataPath)
    {
        _dataPath = dataPath;
        _previewPath = Path.Combine(dataPath, Constants.PREVIEW_FOLDER);
        EnsureDirectoriesExist();
    }

    /// <summary>
    /// Traite une liste de fichiers copiés
    /// </summary>
    public async Task<ClipboardItem?> ProcessFilesAsync(string[] filePaths, long maxSizeBytes)
    {
        if (filePaths == null || filePaths.Length == 0)
            return null;

        try
        {
            var totalSize = 0L;
            var validFiles = new List<string>();
            var fileInfos = new List<FileInfo>();

            // Vérifier chaque fichier
            foreach (var filePath in filePaths)
            {
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    
                    // Vérifier l'extension
                    if (IsAllowedExtension(fileInfo.Extension))
                    {
                        totalSize += fileInfo.Length;
                        if (totalSize <= maxSizeBytes)
                        {
                            validFiles.Add(filePath);
                            fileInfos.Add(fileInfo);
                        }
                    }
                }
                else if (Directory.Exists(filePath))
                {
                    // Pour les dossiers, on prend juste le chemin
                    validFiles.Add(filePath);
                }
            }

            if (!validFiles.Any())
                return null;

            // Créer l'élément clipboard
            var clipboardItem = new ClipboardItem
            {
                Type = ClipboardItemType.Files,
                Content = string.Join(Environment.NewLine, validFiles),
                SizeInBytes = totalSize,
                Metadata = new Dictionary<string, object>
                {
                    ["FileCount"] = validFiles.Count,
                    ["TotalSize"] = totalSize,
                    ["FilePaths"] = validFiles
                }
            };

            // Créer un aperçu
            if (validFiles.Count == 1)
            {
                var singleFile = validFiles[0];
                var fileName = Path.GetFileName(singleFile);
                clipboardItem.SearchablePreview = fileName;
                
                // Si c'est une image, créer une miniature
                if (IsImageFile(singleFile))
                {
                    clipboardItem.PreviewPath = await CreateImageThumbnailAsync(singleFile, clipboardItem.Id);
                }
            }
            else
            {
                var fileNames = validFiles.Select(Path.GetFileName);
                clipboardItem.SearchablePreview = string.Join(", ", fileNames.Take(3));
                if (validFiles.Count > 3)
                {
                    clipboardItem.SearchablePreview += $" et {validFiles.Count - 3} autres...";
                }
            }

            return clipboardItem;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur traitement fichiers: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Traite une image copiée
    /// </summary>
    public async Task<ClipboardItem?> ProcessImageAsync(System.Windows.Media.Imaging.BitmapSource? bitmapSource)
    {
        if (bitmapSource == null)
            return null;

        try
        {
            var clipboardItem = new ClipboardItem
            {
                Type = ClipboardItemType.Image,
                Content = "Image du presse-papiers",
                Metadata = new Dictionary<string, object>
                {
                    ["Width"] = bitmapSource.PixelWidth,
                    ["Height"] = bitmapSource.PixelHeight,
                    ["DpiX"] = bitmapSource.DpiX,
                    ["DpiY"] = bitmapSource.DpiY
                }
            };

            // Sauvegarder l'image et créer une miniature
            var imagePath = await SaveImageAsync(bitmapSource, clipboardItem.Id);
            if (!string.IsNullOrEmpty(imagePath))
            {
                clipboardItem.Content = imagePath;
                clipboardItem.PreviewPath = await CreateImageThumbnailAsync(imagePath, clipboardItem.Id);
                
                var fileInfo = new FileInfo(imagePath);
                clipboardItem.SizeInBytes = fileInfo.Length;
            }

            clipboardItem.SearchablePreview = $"Image {bitmapSource.PixelWidth}x{bitmapSource.PixelHeight}";

            return clipboardItem;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur traitement image: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sauvegarde une image
    /// </summary>
    private async Task<string?> SaveImageAsync(System.Windows.Media.Imaging.BitmapSource bitmapSource, string itemId)
    {
        try
        {
            var fileName = $"img_{itemId}.png";
            var filePath = Path.Combine(_dataPath, fileName);

            await Task.Run(() =>
            {
                using var fileStream = new FileStream(filePath, FileMode.Create);
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapSource));
                encoder.Save(fileStream);
            });

            return filePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur sauvegarde image: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Crée une miniature d'image
    /// </summary>
    private async Task<string?> CreateImageThumbnailAsync(string imagePath, string itemId)
    {
        try
        {
            var thumbnailFileName = $"thumb_{itemId}.png";
            var thumbnailPath = Path.Combine(_previewPath, thumbnailFileName);

            await Task.Run(() =>
            {
                using var originalImage = Image.FromFile(imagePath);
                using var thumbnail = CreateThumbnail(originalImage, Constants.THUMBNAIL_SIZE);
                thumbnail.Save(thumbnailPath, ImageFormat.Png);
            });

            return thumbnailPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur création miniature: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Crée une miniature avec aspect ratio préservé
    /// </summary>
    private static Bitmap CreateThumbnail(Image original, int maxSize)
    {
        var ratioX = (double)maxSize / original.Width;
        var ratioY = (double)maxSize / original.Height;
        var ratio = Math.Min(ratioX, ratioY);

        var newWidth = (int)(original.Width * ratio);
        var newHeight = (int)(original.Height * ratio);

        var thumbnail = new Bitmap(newWidth, newHeight);
        using var graphics = Graphics.FromImage(thumbnail);
        graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        graphics.DrawImage(original, 0, 0, newWidth, newHeight);

        return thumbnail;
    }

    /// <summary>
    /// Vérifie si l'extension de fichier est autorisée
    /// </summary>
    private static bool IsAllowedExtension(string extension)
    {
        return !Constants.EXCLUDED_FILE_EXTENSIONS.Contains(extension.ToLowerInvariant());
    }

    /// <summary>
    /// Vérifie si c'est un fichier image
    /// </summary>
    private static bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return Constants.SUPPORTED_IMAGE_FORMATS.Contains(extension);
    }

    /// <summary>
    /// Nettoie les fichiers orphelins
    /// </summary>
    public async Task CleanupOrphanedFilesAsync(IEnumerable<string> validItemIds)
    {
        try
        {
            var validIds = validItemIds.ToHashSet();

            // Nettoyer les images principales
            await CleanupDirectoryAsync(_dataPath, "img_", validIds);
            
            // Nettoyer les miniatures
            await CleanupDirectoryAsync(_previewPath, "thumb_", validIds);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur nettoyage: {ex.Message}");
        }
    }

    /// <summary>
    /// Nettoie un répertoire spécifique
    /// </summary>
    private static async Task CleanupDirectoryAsync(string directoryPath, string prefix, HashSet<string> validIds)
    {
        await Task.Run(() =>
        {
            if (!Directory.Exists(directoryPath))
                return;

            var files = Directory.GetFiles(directoryPath, $"{prefix}*");
            foreach (var file in files)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var itemId = fileName.Substring(prefix.Length);
                    
                    if (!validIds.Contains(itemId))
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🦊 Erreur suppression {file}: {ex.Message}");
                }
            }
        });
    }

    /// <summary>
    /// S'assure que les répertoires existent
    /// </summary>
    private void EnsureDirectoriesExist()
    {
        if (!Directory.Exists(_dataPath))
            Directory.CreateDirectory(_dataPath);

        if (!Directory.Exists(_previewPath))
            Directory.CreateDirectory(_previewPath);
    }
}