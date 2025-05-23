using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Konan.Configuration;
using Konan.Models;
using System.IO;

namespace Konan.Services;

/// <summary>
/// Service de gestion du presse-papiers Windows
/// 🦊 Le renard qui capture tout !
/// </summary>
public class ClipboardService : IDisposable
{
    private readonly FileService _fileService;
    private readonly List<ClipboardItem> _history;
    private readonly object _lock = new();
    private HwndSource? _hwndSource;
    private IntPtr _nextClipboardViewer = IntPtr.Zero;
    private bool _isCapturing = true;
    private bool _disposed = false;

    // Win32 API pour surveiller le presse-papiers
    private const int WM_DRAWCLIPBOARD = 0x0308;
    private const int WM_CHANGECBCHAIN = 0x030D;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

    public ClipboardService(FileService fileService)
    {
        _fileService = fileService;
        _history = new List<ClipboardItem>();
    }

    /// <summary>
    /// Événement déclenché quand un nouvel élément est capturé
    /// </summary>
    public event EventHandler<ClipboardItem>? ItemCaptured;

    /// <summary>
    /// Événement déclenché quand l'historique change
    /// </summary>
    public event EventHandler? HistoryChanged;

    /// <summary>
    /// Historique des éléments du presse-papiers
    /// </summary>
    public IReadOnlyList<ClipboardItem> History
    {
        get
        {
            lock (_lock)
            {
                return _history.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Indique si la capture est active
    /// </summary>
    public bool IsCapturing
    {
        get => _isCapturing;
        set => _isCapturing = value;
    }

    /// <summary>
    /// Démarre la surveillance du presse-papiers
    /// </summary>
    public void StartMonitoring(Window window)
    {
        try
        {
            var windowHelper = new WindowInteropHelper(window);
            var hwnd = windowHelper.Handle;

            if (hwnd == IntPtr.Zero)
            {
                window.SourceInitialized += (s, e) => StartMonitoring(window);
                return;
            }

            _hwndSource = HwndSource.FromHwnd(hwnd);
            if (_hwndSource != null)
            {
                _hwndSource.AddHook(WndProc);
                _nextClipboardViewer = SetClipboardViewer(hwnd);
            }

            Console.WriteLine("🦊 Surveillance du presse-papiers démarrée !");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur démarrage surveillance: {ex.Message}");
        }
    }

    /// <summary>
    /// Arrête la surveillance du presse-papiers
    /// </summary>
    public void StopMonitoring()
    {
        try
        {
            if (_hwndSource != null)
            {
                var hwnd = _hwndSource.Handle;
                if (hwnd != IntPtr.Zero && _nextClipboardViewer != IntPtr.Zero)
                {
                    ChangeClipboardChain(hwnd, _nextClipboardViewer);
                }
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }

            Console.WriteLine("🦊 Surveillance du presse-papiers arrêtée !");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur arrêt surveillance: {ex.Message}");
        }
    }

    /// <summary>
    /// Gestionnaire de messages Windows
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WM_DRAWCLIPBOARD:
                if (_isCapturing)
                {
                    _ = Task.Run(CaptureClipboardContentAsync);
                }
                if (_nextClipboardViewer != IntPtr.Zero)
                {
                    SendMessage(_nextClipboardViewer, msg, wParam, lParam);
                }
                break;

            case WM_CHANGECBCHAIN:
                if (wParam == _nextClipboardViewer)
                {
                    _nextClipboardViewer = lParam;
                }
                else if (_nextClipboardViewer != IntPtr.Zero)
                {
                    SendMessage(_nextClipboardViewer, msg, wParam, lParam);
                }
                break;
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Capture le contenu actuel du presse-papiers
    /// </summary>
    private async Task CaptureClipboardContentAsync()
    {
        try
        {
            ClipboardItem? newItem = null;

            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    if (!Clipboard.ContainsData(DataFormats.Text) && 
                        !Clipboard.ContainsData(DataFormats.Bitmap) && 
                        !Clipboard.ContainsData(DataFormats.FileDrop))
                    {
                        return;
                    }

                    if (Clipboard.ContainsText())
                    {
                        var text = Clipboard.GetText();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            newItem = CreateTextItem(text);
                        }
                    }
                    else if (Clipboard.ContainsFileDropList())
                    {
                        var files = Clipboard.GetFileDropList();
                        if (files.Count > 0)
                        {
                            var fileArray = files.Cast<string>().ToArray();
                            newItem = await _fileService.ProcessFilesAsync(fileArray, Constants.DEFAULT_MAX_FILE_SIZE_MB * 1024 * 1024);
                        }
                    }
                    else if (Clipboard.ContainsImage())
                    {
                        var image = Clipboard.GetImage();
                        if (image != null)
                        {
                            newItem = await _fileService.ProcessImageAsync(image);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🦊 Erreur capture presse-papiers: {ex.Message}");
                }
            });

            if (newItem != null)
            {
                AddToHistory(newItem);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur traitement capture: {ex.Message}");
        }
    }

    /// <summary>
    /// Crée un élément de texte
    /// </summary>
    private static ClipboardItem CreateTextItem(string text)
    {
        var item = new ClipboardItem
        {
            Type = ClipboardItemType.Text,
            Content = text,
            SearchablePreview = text.Length > Constants.MAX_PREVIEW_TEXT_LENGTH 
                ? text[..Constants.MAX_PREVIEW_TEXT_LENGTH] + "..." 
                : text,
            SizeInBytes = System.Text.Encoding.UTF8.GetByteCount(text)
        };

        if (text.Contains("<html>", StringComparison.OrdinalIgnoreCase) || 
            text.Contains("{\\rtf", StringComparison.OrdinalIgnoreCase))
        {
            item.Type = ClipboardItemType.RichText;
            item.FormattedContent = text;
        }

        return item;
    }

    /// <summary>
    /// Ajoute un élément à l'historique
    /// </summary>
    private void AddToHistory(ClipboardItem item)
    {
        lock (_lock)
        {
            var recent = _history.FirstOrDefault();
            if (recent != null && IsDuplicate(recent, item))
            {
                return;
            }

            _history.Insert(0, item);

            while (_history.Count > Constants.DEFAULT_MAX_ITEMS)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }

        ItemCaptured?.Invoke(this, item);
        HistoryChanged?.Invoke(this, EventArgs.Empty);

        Console.WriteLine($"🦊 Nouvel élément capturé: {item.Type} - {item.DisplayPreview}");
    }

    /// <summary>
    /// Vérifie si deux éléments sont des doublons
    /// </summary>
    private static bool IsDuplicate(ClipboardItem existing, ClipboardItem newItem)
    {
        if (existing.Type != newItem.Type)
            return false;

        return existing.Type switch
        {
            ClipboardItemType.Text or ClipboardItemType.RichText => existing.Content == newItem.Content,
            ClipboardItemType.Image => existing.SizeInBytes == newItem.SizeInBytes,
            ClipboardItemType.Files => existing.Content == newItem.Content,
            _ => false
        };
    }

    /// <summary>
    /// Colle un élément dans le presse-papiers
    /// </summary>
    public async Task PasteItemAsync(ClipboardItem item)
    {
        try
        {
            _isCapturing = false;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    switch (item.Type)
                    {
                        case ClipboardItemType.Text:
                        case ClipboardItemType.RichText:
                            Clipboard.SetText(item.Content);
                            break;

                        case ClipboardItemType.Files:
                            if (item.Metadata.TryGetValue("FilePaths", out var pathsObj) && 
                                pathsObj is List<string> paths)
                            {
                                var fileList = new System.Collections.Specialized.StringCollection();
                                fileList.AddRange(paths.ToArray());
                                Clipboard.SetFileDropList(fileList);
                            }
                            break;

                        case ClipboardItemType.Image:
                            if (!string.IsNullOrEmpty(item.Content) && File.Exists(item.Content))
                            {
                                var bitmap = new BitmapImage(new Uri(item.Content));
                                Clipboard.SetImage(bitmap);
                            }
                            break;
                    }

                    item.MarkAsUsed();
                    Console.WriteLine($"🦊 Élément collé: {item.DisplayPreview}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🦊 Erreur collage: {ex.Message}");
                }
            });

            await Task.Delay(500);
            _isCapturing = true;
        }
        catch (Exception ex)
        {
            _isCapturing = true;
            Console.WriteLine($"🦊 Erreur dans PasteItemAsync: {ex.Message}");
        }
    }

    /// <summary>
    /// Supprime un élément de l'historique
    /// </summary>
    public void RemoveItem(ClipboardItem item)
    {
        lock (_lock)
        {
            _history.Remove(item);
        }

        HistoryChanged?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"🦊 Élément supprimé: {item.DisplayPreview}");
    }

    /// <summary>
    /// Vide tout l'historique
    /// </summary>
    public void ClearHistory()
    {
        lock (_lock)
        {
            _history.Clear();
        }

        HistoryChanged?.Invoke(this, EventArgs.Empty);
        Console.WriteLine("🦊 Historique vidé !");
    }

    /// <summary>
    /// Ajoute manuellement un élément à l'historique (pour l'import)
    /// </summary>
    public void AddItemManually(ClipboardItem item)
    {
        lock (_lock)
        {
            _history.Insert(0, item);
            
            while (_history.Count > Constants.DEFAULT_MAX_ITEMS)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }
        
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Recherche dans l'historique
    /// </summary>
    public IEnumerable<ClipboardItem> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return History;

        lock (_lock)
        {
            return _history.Where(item =>
                item.SearchablePreview.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }
    }

    /// <summary>
    /// Filtre par type
    /// </summary>
    public IEnumerable<ClipboardItem> FilterByType(ClipboardItemType type)
    {
        lock (_lock)
        {
            return _history.Where(item => item.Type == type).ToList();
        }
    }

    /// <summary>
    /// Filtre par date
    /// </summary>
    public IEnumerable<ClipboardItem> FilterByDate(DateTime startDate, DateTime? endDate = null)
    {
        endDate ??= DateTime.UtcNow;

        lock (_lock)
        {
            return _history.Where(item => 
                item.CreatedAt >= startDate && item.CreatedAt <= endDate
            ).ToList();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopMonitoring();
            _disposed = true;
        }
    }
}