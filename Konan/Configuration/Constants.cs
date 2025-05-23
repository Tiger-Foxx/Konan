namespace Konan.Configuration;

/// <summary>
/// Constantes globales de Konan
/// 🦊 Les règles de notre renard !
/// </summary>
public static class Constants
{
    // Fichiers et chemins
    public const string APP_NAME = "Konan";
    public const string DATA_FOLDER = "KonanData";
    public const string SETTINGS_FILE = "settings.json";
    public const string CLIPBOARD_HISTORY_FILE = "clipboard_history.json";
    public const string PREVIEW_FOLDER = "Previews";
    
    // Registry
    public const string REGISTRY_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    public const string REGISTRY_VALUE_NAME = "Konan";
    
    // Hotkeys
    public const int HOTKEY_ID = 9000;
    
    // Limites par défaut
    public const int DEFAULT_MAX_ITEMS = 1000;
    public const int DEFAULT_MAX_FILE_SIZE_MB = 5;
    public const int DEFAULT_CLEANUP_DAYS = 30;
    public const int MAX_PREVIEW_TEXT_LENGTH = 500;
    public const int THUMBNAIL_SIZE = 150;
    
    // Types MIME supportés
    public static readonly string[] SUPPORTED_IMAGE_FORMATS = 
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff", ".ico"
    };
    
    public static readonly string[] EXCLUDED_FILE_EXTENSIONS = 
    {
        ".exe", ".msi", ".dll", ".sys", ".tmp", ".log"
    };
    
    // Messages
    public const string TRAY_TOOLTIP = "Konan - Gestionnaire de presse-papiers illimité - Par Fox";
    public const string APP_TITLE = "Konan - Le Renard du Presse-Papiers";
}