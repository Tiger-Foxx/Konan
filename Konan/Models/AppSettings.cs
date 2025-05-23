using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace Konan.Models;

/// <summary>
/// Paramètres de configuration de Konan
/// 🦊 Les préférences de notre renard zen !
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Raccourci clavier global pour ouvrir Konan
    /// </summary>
    public string GlobalHotkey { get; set; } = "Ctrl+Shift+V";

    /// <summary>
    /// Démarrage automatique avec Windows
    /// </summary>
    public bool StartWithWindows { get; set; } = true;

    /// <summary>
    /// Capture automatique du presse-papiers
    /// </summary>
    public bool AutoCapture { get; set; } = true;

    /// <summary>
    /// Nombre maximum d'éléments dans l'historique (0 = illimité)
    /// </summary>
    public int MaxHistoryItems { get; set; } = 1000;

    /// <summary>
    /// Taille maximum des fichiers à capturer (en Mo)
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 5;

    /// <summary>
    /// Durée de rétention automatique (en jours, 0 = pas de nettoyage auto)
    /// </summary>
    public int AutoCleanupDays { get; set; } = 30;

    /// <summary>
    /// Thème de l'interface
    /// </summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>
    /// Langue de l'interface
    /// </summary>
    public string Language { get; set; } = "fr-FR";

    /// <summary>
    /// Position de la fenêtre principale
    /// </summary>
    public WindowPosition WindowPosition { get; set; } = new();

    /// <summary>
    /// Types de fichiers à exclure de la capture
    /// </summary>
    public List<string> ExcludedFileExtensions { get; set; } = new() 
    { 
        ".exe", ".msi", ".dll", ".sys" 
    };

    /// <summary>
    /// Activation des animations
    /// </summary>
    public bool EnableAnimations { get; set; } = true;

    /// <summary>
    /// Activation des sons
    /// </summary>
    public bool EnableSounds { get; set; } = false;

    /// <summary>
    /// Prévisualisation automatique des images
    /// </summary>
    public bool EnableImagePreview { get; set; } = true;

    /// <summary>
    /// Sauvegarde automatique des paramètres
    /// </summary>
    public bool AutoSave { get; set; } = true;

    /// <summary>
    /// Version de la configuration (pour les migrations)
    /// </summary>
    public string ConfigVersion { get; set; } = "1.0.0";
}

/// <summary>
/// Position et taille de la fenêtre
/// </summary>
public class WindowPosition
{
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public double Width { get; set; } = 400;
    public double Height { get; set; } = 600;
    public bool IsMaximized { get; set; } = false;
}