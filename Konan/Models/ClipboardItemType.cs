namespace Konan.Models;

/// <summary>
/// Types de contenu supportés par Konan
/// 🦊 Notre renard peut gérer tout ça !
/// </summary>
public enum ClipboardItemType
{
    Text,           // Texte simple
    RichText,       // Texte formaté (RTF, HTML)
    Image,          // Images (PNG, JPG, BMP, etc.)
    Files,          // Fichiers et dossiers
    Unknown         // Type non reconnu
}