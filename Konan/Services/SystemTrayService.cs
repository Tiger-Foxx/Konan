using System;
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
    public void Initialize()
    {
        try
        {
            _taskbarIcon = new TaskbarIcon
            {
                Icon = GetApplicationIcon(),
                ToolTipText = "🦊 Konan - Gestionnaire de presse-papiers intelligent",
                Visibility = Visibility.Visible
            };

            // Menu contextuel
            _taskbarIcon.ContextMenu = CreateContextMenu();

            // Double-clic pour ouvrir
            _taskbarIcon.TrayMouseDoubleClick += (s, e) => ShowMainWindow?.Invoke(this, EventArgs.Empty);

            Console.WriteLine("🦊 System tray initialisé !");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur initialisation system tray: {ex.Message}");
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
    private static System.Drawing.Icon? GetApplicationIcon()
    {
        try
        {
            // Essayer de charger l'icône depuis les ressources
            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", "fox.ico");
            if (System.IO.File.Exists(iconPath))
            {
                return new System.Drawing.Icon(iconPath);
            }

            // Fallback vers l'icône par défaut de l'application
            return System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur chargement icône: {ex.Message}");
            return null;
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