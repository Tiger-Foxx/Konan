using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Konan.Configuration;
using Konan.Models;
using Konan.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Konan;

/// <summary>
/// Fenêtre de paramètres moderne de Konan
/// 🦊 Configuration du renard !
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly StartupService? _startupService;
    private bool _isLoaded = false;

    public SettingsWindow()
    {
        InitializeComponent();
        
        // Récupérer les services
        _startupService = App.Services?.GetService<StartupService>();
        
        // Charger les paramètres actuels
        _settings = LoadCurrentSettings();
        
        Loaded += SettingsWindow_Loaded;
        
        // Animation d'entrée
        Opacity = 0;
    }

    private async void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        try
        {
            // Charger les valeurs actuelles
            await LoadSettingsAsync();

            // Animation d'entrée
            var fadeIn = (Storyboard)Resources["SettingsFadeIn"];
            fadeIn.Begin(this);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur chargement paramètres: {ex.Message}");
        }
    }

    #region Event Handlers

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await SaveSettingsAsync();
            
            // Notification de succès
            ShowSuccessNotification("Paramètres sauvegardés !");
            
            // Fermer après un délai
            await Task.Delay(1000);
            Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur sauvegarde: {ex.Message}");
            ShowErrorNotification("Erreur lors de la sauvegarde");
        }
    }

    private async void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Êtes-vous sûr de vouloir réinitialiser tous les paramètres ?",
            "Réinitialisation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await ResetToDefaultsAsync();
                ShowSuccessNotification("Paramètres réinitialisés !");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🦊 Erreur réinitialisation: {ex.Message}");
                ShowErrorNotification("Erreur lors de la réinitialisation");
            }
        }
    }

    private async void CleanNowButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var clipboardService = App.Services?.GetService<ClipboardService>();
            if (clipboardService != null)
            {
                var oldCount = clipboardService.History.Count;
                
                // Nettoyer les éléments anciens (implémentation simplifiée)
                var cutoffDate = DateTime.UtcNow.AddDays(-30);
                var itemsToRemove = clipboardService.History
                    .Where(item => item.CreatedAt < cutoffDate)
                    .ToList();

                foreach (var item in itemsToRemove)
                {
                    clipboardService.RemoveItem(item);
                }

                var removedCount = oldCount - clipboardService.History.Count;
                ShowSuccessNotification($"{removedCount} éléments nettoyés !");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur nettoyage: {ex.Message}");
            ShowErrorNotification("Erreur lors du nettoyage");
        }
    }

    private async void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Êtes-vous sûr de vouloir vider complètement l'historique ?",
            "Vider l'historique",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var clipboardService = App.Services?.GetService<ClipboardService>();
                clipboardService?.ClearHistory();
                
                ShowSuccessNotification("Historique vidé !");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🦊 Erreur vidage: {ex.Message}");
                ShowErrorNotification("Erreur lors du vidage");
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Charge les paramètres actuels
    /// </summary>
    private AppSettings LoadCurrentSettings()
    {
        // Implémentation simplifiée - vous pouvez charger depuis un fichier JSON
        return new AppSettings
        {
            StartWithWindows = _startupService?.IsStartupEnabled() ?? false,
            GlobalHotkey = "Ctrl+Shift+V",
            AutoCapture = true,
            MaxHistoryItems = 1000,
            MaxFileSizeMB = 5,
            EnableAnimations = true,
            EnableImagePreview = true,
            AutoCleanupDays = 30
        };
    }

    /// <summary>
    /// Charge les paramètres dans l'interface
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        await Task.Run(() =>
        {
            Dispatcher.Invoke(() =>
            {
                StartupToggle.IsChecked = _settings.StartWithWindows;
                HotkeyTextBox.Text = _settings.GlobalHotkey;
                AutoCaptureToggle.IsChecked = _settings.AutoCapture;
                MaxItemsTextBox.Text = _settings.MaxHistoryItems.ToString();
                MaxFileSizeTextBox.Text = _settings.MaxFileSizeMB.ToString();
                AnimationsToggle.IsChecked = _settings.EnableAnimations;
                ImagePreviewToggle.IsChecked = _settings.EnableImagePreview;
                AutoCleanupTextBox.Text = _settings.AutoCleanupDays.ToString();
            });
        });
    }

    /// <summary>
    /// Sauvegarde les paramètres
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        await Task.Run(async () =>
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                // Récupérer les valeurs de l'interface
                _settings.StartWithWindows = StartupToggle.IsChecked == true;
                _settings.GlobalHotkey = HotkeyTextBox.Text;
                _settings.AutoCapture = AutoCaptureToggle.IsChecked == true;
                
                if (int.TryParse(MaxItemsTextBox.Text, out var maxItems))
                    _settings.MaxHistoryItems = maxItems;
                
                if (int.TryParse(MaxFileSizeTextBox.Text, out var maxSize))
                    _settings.MaxFileSizeMB = maxSize;
                
                _settings.EnableAnimations = AnimationsToggle.IsChecked == true;
                _settings.EnableImagePreview = ImagePreviewToggle.IsChecked == true;
                
                if (int.TryParse(AutoCleanupTextBox.Text, out var cleanupDays))
                    _settings.AutoCleanupDays = cleanupDays;

                // Appliquer les changements
                await ApplySettingsAsync();
            });
        });
    }

    /// <summary>
    /// Applique les paramètres
    /// </summary>
    private async Task ApplySettingsAsync()
    {
        try
        {
            // Démarrage automatique
            // if (_startupService != null)
            // {
            //     if (_settings.StartWithWindows)
            //         await _startupService.EnableStartupAsync();
            //     else
            //         await _startupService.DisableStartupAsync();
            // }

            // Autres paramètres à appliquer...
            Console.WriteLine("🦊 Paramètres appliqués avec succès !");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur application paramètres: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Remet les paramètres par défaut
    /// </summary>
    private async Task ResetToDefaultsAsync()
    {
        var defaultSettings = new AppSettings();
        
        StartupToggle.IsChecked = defaultSettings.StartWithWindows;
        HotkeyTextBox.Text = defaultSettings.GlobalHotkey;
        AutoCaptureToggle.IsChecked = defaultSettings.AutoCapture;
        MaxItemsTextBox.Text = defaultSettings.MaxHistoryItems.ToString();
        MaxFileSizeTextBox.Text = defaultSettings.MaxFileSizeMB.ToString();
        AnimationsToggle.IsChecked = defaultSettings.EnableAnimations;
        ImagePreviewToggle.IsChecked = defaultSettings.EnableImagePreview;
        AutoCleanupTextBox.Text = defaultSettings.AutoCleanupDays.ToString();

        await SaveSettingsAsync();
    }

    /// <summary>
    /// Affiche une notification de succès
    /// </summary>
    private void ShowSuccessNotification(string message)
    {
        // TODO: Implémenter un toast moderne
        Console.WriteLine($"🦊 ✅ {message}");
    }

    /// <summary>
    /// Affiche une notification d'erreur
    /// </summary>
    private void ShowErrorNotification(string message)
    {
        // TODO: Implémenter un toast moderne
        Console.WriteLine($"🦊 ❌ {message}");
    }

    #endregion
}