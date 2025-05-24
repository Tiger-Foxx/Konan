using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace Konan.Services;

/// <summary>
/// Service de gestion de l'icône dans la barre des tâches
/// 🦊 Notre renard discret dans le system tray !
/// </summary>
public class SystemTrayService : IDisposable
{
    private TaskbarIcon? _taskbarIcon;
    private bool _disposed = false;

    /// <summary>
    /// Événements du system tray
    /// </summary>
    public event EventHandler? ShowMainWindow;
    public event EventHandler? ToggleCapture;
    public event EventHandler? ClearHistory;
    public event EventHandler? ExitApplication;
    public event EventHandler? OpenSettings;

    /// <summary>
    /// Initialise l'icône dans la barre des tâches
    /// </summary>
    /// <summary>
    /// Initialise l'icône dans la barre des tâches
    /// </summary>
    public void Initialize()
    {
        try
        {
            Console.WriteLine("🦊 Initialisation du system tray...");
        
            _taskbarIcon = new TaskbarIcon
            {
                Icon = GetApplicationIcon(),
                ToolTipText = "🦊 Konan - Gestionnaire de presse-papiers intelligent",
                Visibility = Visibility.Visible
            };

            // Vérifier que l'icône a été chargée
            if (_taskbarIcon.Icon == null)
            {
                Console.WriteLine("⚠️ Aucune icône chargée !");
            }
            else
            {
                Console.WriteLine("✅ Icône chargée avec succès");
            }

            // Menu contextuel
            _taskbarIcon.ContextMenu = CreateContextMenu();

            // Double-clic pour ouvrir
            _taskbarIcon.TrayMouseDoubleClick += (s, e) => ShowMainWindow?.Invoke(this, EventArgs.Empty);

            Console.WriteLine("✅ System tray initialisé !");
        
            // // Test des notifications après initialisation
            // TestNotifications();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erreur initialisation system tray: {ex.Message}");
        }
    }

    /// <summary>
    /// Crée le menu contextuel
    /// </summary>
    private ContextMenu CreateContextMenu()
    {
        var contextMenu = new ContextMenu();

        // Ouvrir Konan
        var openItem = new MenuItem
        {
            Header = "🦊 Ouvrir Konan",
            FontWeight = FontWeights.Bold
        };
        openItem.Click += (s, e) => ShowMainWindow?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(openItem);

        contextMenu.Items.Add(new Separator());

        // Activer/Désactiver capture
        var captureItem = new MenuItem
        {
            Header = "⏸️ Pause capture",
            Name = "CaptureToggle"
        };
        captureItem.Click += (s, e) => ToggleCapture?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(captureItem);

        // Paramètres
        var settingsItem = new MenuItem
        {
            Header = "⚙️ Paramètres"
        };
        settingsItem.Click += (s, e) => OpenSettings?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(settingsItem);

        contextMenu.Items.Add(new Separator());

        // Vider l'historique
        var clearItem = new MenuItem
        {
            Header = "🗑️ Vider l'historique"
        };
        clearItem.Click += (s, e) =>
        {
            var result = MessageBox.Show(
                "Êtes-vous sûr de vouloir vider tout l'historique ?",
                "🦊 Konan - Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ClearHistory?.Invoke(this, EventArgs.Empty);
            }
        };
        contextMenu.Items.Add(clearItem);

        contextMenu.Items.Add(new Separator());

        // À propos
        var aboutItem = new MenuItem
        {
            Header = "ℹ️ À propos"
        };
        aboutItem.Click += (s, e) => ShowAboutDialog();
        contextMenu.Items.Add(aboutItem);

        // Quitter
        var exitItem = new MenuItem
        {
            Header = "❌ Quitter"
        };
        exitItem.Click += (s, e) => ExitApplication?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(exitItem);

        return contextMenu;
    }

    /// <summary>
    /// Met à jour l'état de capture dans le menu
    /// </summary>
    public void UpdateCaptureState(bool isCapturing)
    {
        try
        {
            if (_taskbarIcon?.ContextMenu != null)
            {
                foreach (MenuItem item in _taskbarIcon.ContextMenu.Items)
                {
                    if (item.Name == "CaptureToggle")
                    {
                        item.Header = isCapturing ? "⏸️ Pause capture" : "▶️ Reprendre capture";
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur mise à jour état capture: {ex.Message}");
        }
    }

    /// <summary>
    /// Affiche une notification
    /// </summary>
    public void ShowNotification(string title, string message, BalloonIcon icon = BalloonIcon.Info)
    {
        try
        {
            _taskbarIcon?.ShowBalloonTip(title, message, icon);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Affiche une notification de nouvel élément capturé
    /// </summary>
    public void ShowItemCapturedNotification(string itemPreview)
    {
        try
        {
            var preview = itemPreview.Length > 50 ? itemPreview[..47] + "..." : itemPreview;
            ShowNotification("🦊 Konan", $"Nouvel élément capturé:\n{preview}", BalloonIcon.Info);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur notification capture: {ex.Message}");
        }
    }

    /// <summary>
    /// Cache l'icône (pour les tests)
    /// </summary>
    public void Hide()
    {
        if (_taskbarIcon != null)
        {
            _taskbarIcon.Visibility = Visibility.Hidden;
        }
    }

    /// <summary>
    /// Affiche l'icône
    /// </summary>
    public void Show()
    {
        if (_taskbarIcon != null)
        {
            _taskbarIcon.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// Obtient l'icône de l'application
    /// </summary>
    /// <summary>
/// Obtient l'icône de l'application
/// </summary>
private static System.Drawing.Icon? GetApplicationIcon()
{
    try
    {
        Console.WriteLine("🦊 Tentative de chargement de l'icône...");
        
        // Méthode 1 : Depuis les ressources embarquées
        try
        {
            var iconUri = new Uri("pack://application:,,,/Assets/Icons/fox.ico");
            var resourceStream = Application.GetResourceStream(iconUri);
            if (resourceStream != null)
            {
                Console.WriteLine("✅ Icône chargée depuis les ressources embarquées");
                return new System.Drawing.Icon(resourceStream.Stream);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Échec ressources embarquées: {ex.Message}");
        }

        // Méthode 2 : Depuis le dossier de l'application
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var iconPaths = new[]
        {
            Path.Combine(baseDir, "Assets", "Icons", "fox.ico"),
            Path.Combine(baseDir, "fox.ico"),
            Path.Combine(baseDir, "Assets", "fox.ico")
        };

        foreach (var iconPath in iconPaths)
        {
            Console.WriteLine($"🔍 Recherche icône: {iconPath}");
            if (File.Exists(iconPath))
            {
                Console.WriteLine($"✅ Icône trouvée: {iconPath}");
                return new System.Drawing.Icon(iconPath);
            }
        }

        // Méthode 3 : Icône de l'exécutable
        var executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(executablePath) && File.Exists(executablePath))
        {
            var extractedIcon = System.Drawing.Icon.ExtractAssociatedIcon(executablePath);
            if (extractedIcon != null)
            {
                Console.WriteLine("✅ Icône extraite de l'exécutable");
                return extractedIcon;
            }
        }

        // Fallback : Icône système
        Console.WriteLine("⚠️ Utilisation icône système par défaut");
        return SystemIcons.Application;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erreur chargement icône: {ex.Message}");
        return SystemIcons.Application;
    }
}

    /// <summary>
    /// Affiche la boîte de dialogue À propos
    /// </summary>
    private static void ShowAboutDialog()
    {
        MessageBox.Show(
            "🦊 Konan v1.0.0\n" +
            "Le Gestionnaire de Presse-Papiers Infini Intelligent\n\n" +
            "Par Fox\n" +
            "Propulsé par WPF et .NET 8\n\n" +
            "© 2025 - the-fox.tech",
            "🦊 À propos de Konan",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _taskbarIcon?.Dispose();
            _taskbarIcon = null;
            _disposed = true;
            Console.WriteLine("🦊 System tray libéré !");
        }
    }
}