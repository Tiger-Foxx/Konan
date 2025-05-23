using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Konan.Models;

/// <summary>
/// Représente un élément copié dans le presse-papiers
/// 🦊 Chaque souvenir de notre renard méditatif !
/// </summary>
public class ClipboardItem
{
    /// <summary>
    /// Identifiant unique de l'élément
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Type de contenu
    /// </summary>
    public ClipboardItemType Type { get; set; }

    /// <summary>
    /// Contenu principal (texte, chemin d'image, etc.)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Contenu formaté (pour le RTF, HTML, etc.)
    /// </summary>
    public string? FormattedContent { get; set; }

    /// <summary>
    /// Métadonnées supplémentaires (taille fichier, dimensions image, etc.)
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Date et heure de création
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Nombre de fois que l'élément a été utilisé
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Dernière utilisation
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Taille en octets (pour les fichiers et images)
    /// </summary>
    public long SizeInBytes { get; set; } = 0;

    /// <summary>
    /// Chemin vers le fichier de prévisualisation (pour les images)
    /// </summary>
    public string? PreviewPath { get; set; }

    /// <summary>
    /// Indique si l'élément est favori
    /// </summary>
    public bool IsFavorite { get; set; } = false;

    /// <summary>
    /// Tags personnalisés pour l'organisation
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Aperçu textuel pour la recherche (résumé du contenu)
    /// </summary>
    public string SearchablePreview { get; set; } = string.Empty;

    /// <summary>
    /// Marque l'élément comme utilisé
    /// 🦊 Notre renard se souvient de ses préférences !
    /// </summary>
    public void MarkAsUsed()
    {
        UsageCount++;
        LastUsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Obtient un aperçu court du contenu pour l'affichage
    /// </summary>
    [JsonIgnore]
    public string DisplayPreview
    {
        get
        {
            return Type switch
            {
                ClipboardItemType.Text or ClipboardItemType.RichText => 
                    Content.Length > 100 ? Content[..97] + "..." : Content,
                ClipboardItemType.Image => $"Image ({Metadata.GetValueOrDefault("Width", "?")}x{Metadata.GetValueOrDefault("Height", "?")})",
                ClipboardItemType.Files => $"{Metadata.GetValueOrDefault("FileCount", 1)} fichier(s)",
                _ => "Contenu inconnu"
            };
        }
    }

    /// <summary>
    /// Obtient l'icône appropriée pour le type de contenu
    /// </summary>
    [JsonIgnore]
    public string IconPath
    {
        get
        {
            return Type switch
            {
                ClipboardItemType.Text => "📝",
                ClipboardItemType.RichText => "📄",
                ClipboardItemType.Image => "🖼️",
                ClipboardItemType.Files => "📁",
                _ => "❓"
            };
        }
    }
}