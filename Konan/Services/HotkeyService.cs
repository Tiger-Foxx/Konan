using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Konan.Services;

/// <summary>
/// Service de gestion des raccourcis clavier globaux
/// 🦊 Notre renard réactif aux touches !
/// </summary>
public class HotkeyService : IDisposable
{
    private readonly Dictionary<int, HotkeyInfo> _registeredHotkeys = new();
    private HwndSource? _hwndSource;
    private int _currentId = 1000;
    private bool _disposed = false;

    // Win32 API pour les hotkeys
    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    /// <summary>
    /// Événement déclenché quand un hotkey est pressé
    /// </summary>
    public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    /// <summary>
    /// Information sur un hotkey enregistré
    /// </summary>
    private class HotkeyInfo
    {
        public string Name { get; set; } = string.Empty;
        public ModifierKeys Modifiers { get; set; }
        public Key Key { get; set; }
        public Action? Action { get; set; }
    }

    /// <summary>
    /// Arguments de l'événement hotkey pressé
    /// </summary>
    public class HotkeyPressedEventArgs : EventArgs
    {
        public string Name { get; set; } = string.Empty;
        public ModifierKeys Modifiers { get; set; }
        public Key Key { get; set; }
    }

    /// <summary>
    /// Modifiers flags pour Win32 API
    /// </summary>
    [Flags]
    private enum ModifierFlags : uint
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }

    /// <summary>
    /// Initialise le service avec une fenêtre
    /// </summary>
    public void Initialize(Window window)
    {
        try
        {
            var windowHelper = new WindowInteropHelper(window);
            var hwnd = windowHelper.Handle;

            if (hwnd == IntPtr.Zero)
            {
                // Si la fenêtre n'est pas encore créée, attendre
                window.SourceInitialized += (s, e) => Initialize(window);
                return;
            }

            _hwndSource = HwndSource.FromHwnd(hwnd);
            if (_hwndSource != null)
            {
                _hwndSource.AddHook(WndProc);
            }

            Console.WriteLine("🦊 Service de hotkeys initialisé !");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur initialisation hotkeys: {ex.Message}");
        }
    }

    /// <summary>
    /// Enregistre un hotkey global
    /// </summary>
    public bool RegisterHotkey(string name, ModifierKeys modifiers, Key key, Action? action = null)
    {
        try
        {
            if (_hwndSource?.Handle == null)
            {
                Console.WriteLine("🦊 Service non initialisé !");
                return false;
            }

            var id = _currentId++;
            var vkCode = KeyInterop.VirtualKeyFromKey(key);
            var modifierFlags = GetModifierFlags(modifiers);

            if (RegisterHotKey(_hwndSource.Handle, id, (uint)modifierFlags, (uint)vkCode))
            {
                _registeredHotkeys[id] = new HotkeyInfo
                {
                    Name = name,
                    Modifiers = modifiers,
                    Key = key,
                    Action = action
                };

                Console.WriteLine($"🦊 Hotkey enregistré: {name} ({modifiers}+{key})");
                return true;
            }
            else
            {
                Console.WriteLine($"🦊 Échec enregistrement hotkey: {name}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur enregistrement hotkey {name}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Enregistre un hotkey à partir d'une chaîne de caractères
    /// </summary>
    public bool RegisterHotkey(string name, string hotkeyString, Action? action = null)
    {
        if (TryParseHotkeyString(hotkeyString, out var modifiers, out var key))
        {
            return RegisterHotkey(name, modifiers, key, action);
        }

        Console.WriteLine($"🦊 Format hotkey invalide: {hotkeyString}");
        return false;
    }

    /// <summary>
    /// Désactive un hotkey
    /// </summary>
    public bool UnregisterHotkey(string name)
    {
        try
        {
            var hotkeyToRemove = _registeredHotkeys.FirstOrDefault(kvp => kvp.Value.Name == name);
            if (hotkeyToRemove.Key != 0 && _hwndSource?.Handle != null)
            {
                if (UnregisterHotKey(_hwndSource.Handle, hotkeyToRemove.Key))
                {
                    _registeredHotkeys.Remove(hotkeyToRemove.Key);
                    Console.WriteLine($"🦊 Hotkey désactivé: {name}");
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur désactivation hotkey {name}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gestionnaire de messages Windows
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_registeredHotkeys.TryGetValue(id, out var hotkeyInfo))
            {
                try
                {
                    // Exécuter l'action associée
                    hotkeyInfo.Action?.Invoke();

                    // Déclencher l'événement
                    HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs
                    {
                        Name = hotkeyInfo.Name,
                        Modifiers = hotkeyInfo.Modifiers,
                        Key = hotkeyInfo.Key
                    });

                    Console.WriteLine($"🦊 Hotkey pressé: {hotkeyInfo.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🦊 Erreur exécution hotkey {hotkeyInfo.Name}: {ex.Message}");
                }

                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Convertit ModifierKeys en flags Win32
    /// </summary>
    private static ModifierFlags GetModifierFlags(ModifierKeys modifiers)
    {
        var flags = ModifierFlags.None;

        if (modifiers.HasFlag(ModifierKeys.Alt))
            flags |= ModifierFlags.Alt;

        if (modifiers.HasFlag(ModifierKeys.Control))
            flags |= ModifierFlags.Control;

        if (modifiers.HasFlag(ModifierKeys.Shift))
            flags |= ModifierFlags.Shift;

        if (modifiers.HasFlag(ModifierKeys.Windows))
            flags |= ModifierFlags.Windows;

        return flags;
    }

    /// <summary>
    /// Parse une chaîne de hotkey (ex: "Ctrl+Shift+V")
    /// </summary>
    private static bool TryParseHotkeyString(string hotkeyString, out ModifierKeys modifiers, out Key key)
    {
        modifiers = ModifierKeys.None;
        key = Key.None;

        try
        {
            var parts = hotkeyString.Split('+').Select(p => p.Trim()).ToArray();
            if (parts.Length == 0)
                return false;

            // La dernière partie est la touche principale
            var keyString = parts[^1];
            if (!Enum.TryParse<Key>(keyString, true, out key))
                return false;

            // Les autres parties sont les modificateurs
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var modifier = parts[i].ToLowerInvariant();
                switch (modifier)
                {
                    case "ctrl":
                    case "control":
                        modifiers |= ModifierKeys.Control;
                        break;
                    case "alt":
                        modifiers |= ModifierKeys.Alt;
                        break;
                    case "shift":
                        modifiers |= ModifierKeys.Shift;
                        break;
                    case "win":
                    case "windows":
                        modifiers |= ModifierKeys.Windows;
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Obtient la liste des hotkeys enregistrés
    /// </summary>
    public IEnumerable<string> GetRegisteredHotkeys()
    {
        return _registeredHotkeys.Values.Select(h => h.Name).ToList();
    }

    /// <summary>
    /// Vérifie si un hotkey est enregistré
    /// </summary>
    public bool IsHotkeyRegistered(string name)
    {
        return _registeredHotkeys.Values.Any(h => h.Name == name);
    }

    /// <summary>
    /// Désactive tous les hotkeys
    /// </summary>
    public void UnregisterAllHotkeys()
    {
        if (_hwndSource?.Handle != null)
        {
            foreach (var id in _registeredHotkeys.Keys.ToList())
            {
                UnregisterHotKey(_hwndSource.Handle, id);
            }
        }

        _registeredHotkeys.Clear();
        Console.WriteLine("🦊 Tous les hotkeys désactivés !");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            UnregisterAllHotkeys();
            
            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }

            _disposed = true;
            Console.WriteLine("🦊 Service hotkeys libéré !");
        }
    }
}