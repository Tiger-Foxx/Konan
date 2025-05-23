using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Collections.ObjectModel;
using Konan.Models;
using Konan.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using MahApps.Metro.IconPacks;
using System.Windows.Media;
using System.Runtime.InteropServices;

namespace Konan;

/// <summary>
/// Fenêtre principale NOIR/ROUGE de Konan
/// 🦊 L'interface de notre renard moderne !
/// </summary>
public partial class MainWindow : Window
{
    private ClipboardService? _clipboardService;
    private SearchService? _searchService;
    private readonly ObservableCollection<ClipboardItem> _displayedItems = new();
    private bool _isLoaded = false;
    private SystemTrayService? _systemTrayService;


    // Notifications Windows
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        
        // Gestion des raccourcis
        KeyDown += MainWindow_KeyDown;
        
        // Animation d'entrée
        Opacity = 0;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        try
        {
            // Récupérer les services
            _clipboardService = App.Services?.GetService<ClipboardService>();
            _searchService = App.Services?.GetService<SearchService>();

            // S'abonner aux événements
            if (_clipboardService != null)
            {
                _clipboardService.ItemCaptured += OnClipboardItemCaptured;
                _clipboardService.HistoryChanged += OnHistoryChanged;
            }

            // Charger l'historique existant
            await RefreshClipboardHistoryAsync();

            // Animation d'entrée smooth
            var fadeIn = (Storyboard)Resources["FadeInAnimation"];
            fadeIn.Begin(this);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur chargement MainWindow: {ex.Message}");
        }
    }

    #region Event Handlers

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            HideWindow();
        }
        else if (e.Key == Key.Enter && !string.IsNullOrEmpty(SearchTextBox.Text))
        {
            // Sélectionner le premier élément filtré
            var firstItem = ClipboardHistoryPanel.Children.OfType<Border>()
                .FirstOrDefault(b => b.Tag is ClipboardItem);
            if (firstItem?.Tag is ClipboardItem item)
            {
                PasteItem(item);
            }
        }
        else if (e.Key >= Key.D1 && e.Key <= Key.D9)
        {
            // Raccourcis numériques 1-9 pour sélection rapide
            var index = (int)(e.Key - Key.D1);
            var items = ClipboardHistoryPanel.Children.OfType<Border>()
                .Where(b => b.Tag is ClipboardItem).ToList();
            if (index < items.Count && items[index].Tag is ClipboardItem item)
            {
                PasteItem(item);
            }
        }
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        HideWindow();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur ouverture paramètres: {ex.Message}");
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchTextBox.Text;
        ClearSearchButton.Visibility = string.IsNullOrEmpty(query) ? 
            Visibility.Collapsed : Visibility.Visible;
        
        FilterHistoryAsync(query);
    }

    private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Clear();
        SearchTextBox.Focus();
    }

    private async void OnClipboardItemCaptured(object? sender, ClipboardItem item)
    {
        await Dispatcher.InvokeAsync(async () =>
        {
            await RefreshClipboardHistoryAsync();
            
            // 🚨 NOTIFICATION WINDOWS
            ShowWindowsNotification("Élément capturé", $"📋 {item.DisplayPreview}");
        });
    }

    private async void OnHistoryChanged(object? sender, EventArgs e)
    {
        await Dispatcher.InvokeAsync(async () =>
        {
            await RefreshClipboardHistoryAsync();
        });
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Affiche la fenêtre avec animation
    /// </summary>
    public new void Show()
    {
        base.Show();
        WindowState = WindowState.Normal;
        Activate();
        Focus();
        
        // Animation d'entrée
        var fadeIn = (Storyboard)Resources["FadeInAnimation"];
        fadeIn.Begin(this);
        
        // Focus sur la recherche
        SearchTextBox.Focus();
    }

    /// <summary>
    /// Cache la fenêtre avec animation
    /// </summary>
    public void HideWindow()
    {
        var fadeOut = (Storyboard)Resources["FadeOutAnimation"];
        fadeOut.Completed += (s, e) => Hide();
        fadeOut.Begin(this);
    }

    /// <summary>
    /// Rafraîchit l'historique du presse-papiers
    /// </summary>
    public async Task RefreshClipboardHistoryAsync()
    {
        try
        {
            if (_clipboardService == null) return;

            var history = _clipboardService.History.ToList();
            
            await Dispatcher.InvokeAsync(() =>
            {
                ClipboardHistoryPanel.Children.Clear();
                
                if (!history.Any())
                {
                    ClipboardHistoryPanel.Children.Add(EmptyStatePanel);
                }
                else
                {
                    for (int i = 0; i < history.Count; i++)
                    {
                        var item = history[i];
                        var itemControl = CreateClipboardItemControl(item, i + 1);
                        ClipboardHistoryPanel.Children.Add(itemControl);
                    }
                }
                
                UpdateItemCount(history.Count);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur rafraîchissement historique: {ex.Message}");
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Filtre l'historique selon la recherche
    /// </summary>
    private async void FilterHistoryAsync(string query)
    {
        try
        {
            if (_clipboardService == null) return;

            var allItems = _clipboardService.History.ToList();
            var filteredItems = string.IsNullOrWhiteSpace(query) ? 
                allItems : _clipboardService.Search(query).ToList();

            await Dispatcher.InvokeAsync(() =>
            {
                ClipboardHistoryPanel.Children.Clear();
                
                if (!filteredItems.Any())
                {
                    var noResultsPanel = CreateNoResultsPanel(query);
                    ClipboardHistoryPanel.Children.Add(noResultsPanel);
                }
                else
                {
                    for (int i = 0; i < filteredItems.Count; i++)
                    {
                        var item = filteredItems[i];
                        var itemControl = CreateClipboardItemControl(item, i + 1);
                        ClipboardHistoryPanel.Children.Add(itemControl);
                    }
                }
                
                UpdateItemCount(filteredItems.Count, allItems.Count);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur filtrage: {ex.Message}");
        }
    }

    /// <summary>
    /// Crée un contrôle pour un élément du presse-papiers BIEN SÉPARÉ
    /// </summary>
    private Border CreateClipboardItemControl(ClipboardItem item, int number)
    {
        var border = new Border
        {
            Style = (Style)Resources["ClipboardCard"],
            Tag = item
        };

        border.MouseLeftButtonDown += (s, e) => PasteItem(item);
        border.MouseRightButtonDown += (s, e) => ShowItemContextMenu(item, border);

        var mainGrid = new Grid();
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Numéro
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Icône
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Contenu
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Actions

        // Numéro ROUGE
        var numberBorder = new Border
        {
            Width = 24,
            Height = 24,
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Color.FromArgb(120, 220, 38, 38)), // Rouge transparent
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 12, 0)
        };

        var numberText = new TextBlock
        {
            Text = number.ToString(),
            FontSize = 11,
            FontWeight = FontWeights.Medium,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        numberBorder.Child = numberText;

        // Icône selon le type
        var iconPack = new PackIconMaterial
        {
            Width = 20,
            Height = 20,
            Foreground = GetTypeColor(item.Type),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 12, 0),
            Kind = GetTypeIcon(item.Type)
        };

        // Contenu principal
        var contentStack = new StackPanel();
        
        // Preview du texte
        var preview = new TextBlock
        {
            Text = item.DisplayPreview,
            Foreground = Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 60,
            Margin = new Thickness(0, 0, 0, 8)
        };

        // Métadonnées
        var metaStack = new StackPanel { Orientation = Orientation.Horizontal };
        
        var dateText = new TextBlock
        {
            Text = item.CreatedAt.ToString("dd/MM HH:mm"),
            Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
            FontSize = 10,
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        if (item.UsageCount > 0)
        {
            var usageIcon = new PackIconMaterial
            {
                Kind = PackIconMaterialKind.TrendingUp,
                Width = 10,
                Height = 10,
                Foreground = new SolidColorBrush(Color.FromArgb(128, 220, 38, 38)),
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var usageText = new TextBlock
            {
                Text = item.UsageCount.ToString(),
                Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
                FontSize = 10,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            metaStack.Children.Add(usageIcon);
            metaStack.Children.Add(usageText);
        }

        if (item.SizeInBytes > 0)
        {
            var sizeText = new TextBlock
            {
                Text = FormatFileSize(item.SizeInBytes),
                Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            };
            metaStack.Children.Add(sizeText);
        }

        metaStack.Children.Add(dateText);
        contentStack.Children.Add(preview);
        contentStack.Children.Add(metaStack);

        // Bouton favoris
        var favButton = new Button
        {
            Style = (Style)FindResource("ModernButton"),
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Top,
            Content = new PackIconMaterial
            {
                Kind = item.IsFavorite ? PackIconMaterialKind.Star : PackIconMaterialKind.StarOutline,
                Width = 16,
                Height = 16,
                Foreground = item.IsFavorite ? new SolidColorBrush(Color.FromArgb(255, 220, 38, 38)) : 
                            new SolidColorBrush(Color.FromArgb(128, 255, 255, 255))
            }
        };

        favButton.Click += (s, e) =>
        {
            e.Handled = true;
            ToggleFavorite(item);
        };

        Grid.SetColumn(numberBorder, 0);
        Grid.SetColumn(iconPack, 1);
        Grid.SetColumn(contentStack, 2);
        Grid.SetColumn(favButton, 3);

        mainGrid.Children.Add(numberBorder);
        mainGrid.Children.Add(iconPack);
        mainGrid.Children.Add(contentStack);
        mainGrid.Children.Add(favButton);
        border.Child = mainGrid;

        return border;
    }

    /// <summary>
    /// Crée un panneau "Aucun résultat"
    /// </summary>
    private Border CreateNoResultsPanel(string query)
    {
        var border = new Border
        {
            Style = (Style)Resources["ClipboardCard"],
            Margin = new Thickness(8, 20, 8, 20),
            Padding = new Thickness(40)
        };

        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        
        var iconBorder = new Border
        {
            Width = 60,
            Height = 60,
            CornerRadius = new CornerRadius(30),
            Background = new SolidColorBrush(Color.FromArgb(32, 42, 42, 42)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var icon = new PackIconMaterial
        {
            Kind = PackIconMaterialKind.MagnifyClose,
            Width = 30,
            Height = 30,
            Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        iconBorder.Child = icon;

        var title = new TextBlock
        {
            Text = "Aucun résultat",
            FontSize = 16,
            FontWeight = FontWeights.Light,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var subtitle = new TextBlock
        {
            Text = $"Aucun élément trouvé pour \"{query}\"",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center
        };

        stack.Children.Add(iconBorder);
        stack.Children.Add(title);
        stack.Children.Add(subtitle);
        border.Child = stack;

        return border;
    }

    /// <summary>
    /// COLLE un élément + NOTIFICATION WINDOWS
    /// </summary>
    private async void PasteItem(ClipboardItem item)
    {
        try
        {
            if (_clipboardService != null)
            {
                // Mettre l'élément dans le presse-papiers
                await _clipboardService.PasteItemAsync(item);
                
                // 🚨 NOTIFICATION WINDOWS
                ShowWindowsNotification("Élément copié/collé", $"✅ {item.DisplayPreview}");
                
                // Cacher la fenêtre
                HideWindow();
                
                // Simuler Ctrl+V automatiquement après un délai
                await Task.Delay(2000);
                await SimulateCtrlV();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur collage: {ex.Message}");
            ShowWindowsNotification("Erreur", "❌ Erreur lors du collage");
        }
    }

    /// <summary>
    /// Simule Ctrl+V pour coller automatiquement
    /// </summary>
    private async Task SimulateCtrlV()
    {
        try
        {
            await Task.Run(() =>
            {
                [DllImport("user32.dll")]
                static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

                const byte VK_CONTROL = 0x11;
                const byte VK_V = 0x56;
                const uint KEYEVENTF_KEYUP = 0x0002;

                keybd_event(VK_CONTROL, 0, 0, 0);
                keybd_event(VK_V, 0, 0, 0);
                keybd_event(VK_V, 0, KEYEVENTF_KEYUP, 0);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur simulation Ctrl+V: {ex.Message}");
        }
    }

    /// <summary>
    /// Bascule le statut favori
    /// </summary>
    private async void ToggleFavorite(ClipboardItem item)
    {
        try
        {
            item.IsFavorite = !item.IsFavorite;
            await RefreshClipboardHistoryAsync();
            
            ShowWindowsNotification("Favori", 
                item.IsFavorite ? "⭐ Ajouté aux favoris" : "☆ Retiré des favoris");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur favori: {ex.Message}");
        }
    }

    /// <summary>
    /// Affiche le menu contextuel d'un élément
    /// </summary>
    private void ShowItemContextMenu(ClipboardItem item, FrameworkElement element)
    {
        var contextMenu = new ContextMenu
        {
            Background = new SolidColorBrush(Color.FromArgb(240, 10, 10, 10)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(80, 220, 38, 38)),
            BorderThickness = new Thickness(1)
        };
        
        var pasteItem = new MenuItem 
        { 
            Header = "Coller",
            Icon = new PackIconMaterial { Kind = PackIconMaterialKind.ContentPaste, Width = 16, Height = 16 }
        };
        pasteItem.Click += (s, e) => PasteItem(item);
        
        var favoriteItem = new MenuItem 
        { 
            Header = item.IsFavorite ? "Retirer des favoris" : "Ajouter aux favoris",
            Icon = new PackIconMaterial 
            { 
                Kind = item.IsFavorite ? PackIconMaterialKind.Star : PackIconMaterialKind.StarOutline, 
                Width = 16, 
                Height = 16 
            }
        };
        favoriteItem.Click += (s, e) => ToggleFavorite(item);
        
        var deleteItem = new MenuItem 
        { 
            Header = "Supprimer",
            Icon = new PackIconMaterial { Kind = PackIconMaterialKind.Delete, Width = 16, Height = 16 }
        };
        deleteItem.Click += (s, e) => DeleteItem(item);

        contextMenu.Items.Add(pasteItem);
        contextMenu.Items.Add(favoriteItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(deleteItem);

        element.ContextMenu = contextMenu;
        contextMenu.IsOpen = true;
    }

    /// <summary>
    /// Supprime un élément
    /// </summary>
    private async void DeleteItem(ClipboardItem item)
    {
        try
        {
            _clipboardService?.RemoveItem(item);
            await RefreshClipboardHistoryAsync();
            ShowWindowsNotification("Suppression", "🗑️ Élément supprimé");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur suppression: {ex.Message}");
        }
    }

    /// <summary>
    /// Met à jour le compteur d'éléments
    /// </summary>
    private void UpdateItemCount(int displayedCount, int totalCount = -1)
    {
        if (totalCount == -1)
        {
            ItemCountText.Text = $"{displayedCount} élément{(displayedCount > 1 ? "s" : "")}";
        }
        else
        {
            ItemCountText.Text = $"{displayedCount}/{totalCount} élément{(totalCount > 1 ? "s" : "")}";
        }
    }

    /// <summary>
    /// Obtient l'icône Material pour un type de contenu
    /// </summary>
    private static PackIconMaterialKind GetTypeIcon(ClipboardItemType type)
    {
        return type switch
        {
            ClipboardItemType.Text => PackIconMaterialKind.FormatText,
            ClipboardItemType.RichText => PackIconMaterialKind.FormatColorText,
            ClipboardItemType.Image => PackIconMaterialKind.Image,
            ClipboardItemType.Files => PackIconMaterialKind.Folder,
            _ => PackIconMaterialKind.HelpCircle
        };
    }

    /// <summary>
    /// Obtient la couleur ROUGE pour un type de contenu
    /// </summary>
    private static SolidColorBrush GetTypeColor(ClipboardItemType type)
    {
        return type switch
        {
            ClipboardItemType.Text => new SolidColorBrush(Color.FromArgb(255, 220, 38, 38)),      // Rouge
            ClipboardItemType.RichText => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)), // Rouge clair
            ClipboardItemType.Image => new SolidColorBrush(Color.FromArgb(255, 185, 28, 28)),    // Rouge foncé
            ClipboardItemType.Files => new SolidColorBrush(Color.FromArgb(255, 153, 27, 27)),    // Rouge très foncé
            _ => new SolidColorBrush(Color.FromArgb(128, 255, 255, 255))                          // Gris
        };
    }

    /// <summary>
    /// Formate une taille de fichier
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }

    /// <summary>
    /// 🚨 NOTIFICATION WINDOWS NATIVE
    /// </summary>
    private void ShowWindowsNotification(string title, string message)
    {
        try
        {
            var notifyIcon = App.Services?.GetService<SystemTrayService>();
            if (notifyIcon != null)
            {
                // Utiliser le service de notification si disponible
                Console.WriteLine($"🔔 {title}: {message}");
                
                _systemTrayService?.ShowNotification(
                    $"🦊 {title}",
                    $"{message}",
                    Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

            }
            else
            {
                // Fallback vers console
                Console.WriteLine($"🔔 {title}: {message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur notification: {ex.Message}");
        }
    }

    #endregion
}