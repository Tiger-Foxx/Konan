using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Konan.Configuration;
using Konan.Models;
using Konan.Persistence;
using Konan.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Konan;

/// <summary>
/// Application principale de Konan
/// 🦊 Le renard maître de son royaume !
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private SystemTrayService? _systemTrayService;
    private ClipboardService? _clipboardService;
    private HotkeyService? _hotkeyService;
    private AppConfig? _appConfig;
    private MainWindow? _mainWindow;
    private Mutex? _applicationMutex;
    private bool _isShuttingDown = false;

    /// <summary>
    /// Services de l'application
    /// </summary>
    public static IServiceProvider? Services { get; private set; }

    /// <summary>
    /// Configuration de l'application
    /// </summary>
    public static AppConfig? Configuration { get; private set; }

    /// <summary>
    /// Démarrage de l'application
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        // 🔥 IMPORTANT : Empêcher la fermeture automatique de l'app
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // 🎬 SPLASH SCREEN OBLIGATOIRE
        var splash = new SplashScreen();
        var splashResult = splash.ShowDialog();

        if (splashResult != true)
        {
            Shutdown();
            return;
        }

        try
        {
            Console.WriteLine("🦊 Démarrage de Konan...");

            // Vérifier si une instance est déjà en cours
            if (!EnsureSingleInstance())
            {
                Console.WriteLine("🦊 Une instance de Konan est déjà en cours !");
                Shutdown();
                return;
            }

            // Gérer les exceptions non gérées
            SetupExceptionHandling();

            // Initialiser les services
            await InitializeServicesAsync();

            // Initialiser l'interface utilisateur
            await InitializeUIAsync();

            // Configurer les événements de l'application
            SetupApplicationEvents();

            // Charger les données persistantes
            await LoadPersistedDataAsync();

            // Démarrer les services en arrière-plan
            await StartBackgroundServicesAsync();

            Console.WriteLine("🦊 Konan est maintenant actif et surveille votre presse-papiers !");
            Console.WriteLine("🦊 Utilisez Ctrl+Shift+V pour ouvrir l'interface !");

            // 🔥 NE PAS appeler base.OnStartup(e) ici car ça peut causer des problèmes
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 ERREUR FATALE au démarrage: {ex}");
            MessageBox.Show(
                $"Erreur fatale lors du démarrage de Konan:\n\n{ex.Message}",
                "🦊 Konan - Erreur fatale",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    /// <summary>
    /// S'assure qu'une seule instance de l'application s'exécute
    /// </summary>
    private bool EnsureSingleInstance()
    {
        try
        {
            _applicationMutex = new Mutex(true, "KonanClipboardManager_SingleInstance", out bool createdNew);
            return createdNew;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur vérification instance unique: {ex.Message}");
            return true; // En cas d'erreur, on continue
        }
    }

    /// <summary>
    /// Configure la gestion des exceptions
    /// </summary>
    private void SetupExceptionHandling()
    {
        // Exceptions UI non gérées
        DispatcherUnhandledException += (s, e) =>
        {
            Console.WriteLine($"🦊 Exception UI non gérée: {e.Exception}");
            LogException(e.Exception);
            e.Handled = true; // Éviter le crash
        };

        // Exceptions de domaine d'application
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Console.WriteLine($"🦊 Exception domaine non gérée: {e.ExceptionObject}");
            if (e.ExceptionObject is Exception ex)
            {
                LogException(ex);
            }
        };

        // Exceptions des tâches
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Console.WriteLine($"🦊 Exception tâche non observée: {e.Exception}");
            LogException(e.Exception);
            e.SetObserved(); // Marquer comme observée
        };
    }

    /// <summary>
    /// Initialise les services avec injection de dépendances
    /// </summary>
    private async Task InitializeServicesAsync()
    {
        Console.WriteLine("🦊 Initialisation des services...");

        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices);

        _host = hostBuilder.Build();
        Services = _host.Services;

        // Démarrer l'host
        await _host.StartAsync();

        // Récupérer les services principaux
        _appConfig = Services.GetRequiredService<AppConfig>();
        _systemTrayService = Services.GetRequiredService<SystemTrayService>();
        _clipboardService = Services.GetRequiredService<ClipboardService>();
        _hotkeyService = Services.GetRequiredService<HotkeyService>();

        Configuration = _appConfig;

        Console.WriteLine("🦊 Services initialisés avec succès !");
    }

    /// <summary>
    /// Configure les services d'injection de dépendances
    /// </summary>
    private void ConfigureServices(IServiceCollection services)
    {
        // Services de base
        services.AddSingleton<IDataPersistence, JsonPersistenceService>();
        services.AddSingleton<AppConfig>();

        // Services métier
        services.AddSingleton<FileService>(provider =>
        {
            var config = provider.GetRequiredService<AppConfig>();
            return new FileService(config.DataPath);
        });

        services.AddSingleton<ClipboardService>();
        services.AddSingleton<HotkeyService>();
        services.AddSingleton<SystemTrayService>();

        // Services d'historique et de persistance
        services.AddSingleton<ClipboardHistoryService>();
        services.AddSingleton<SearchService>();
        services.AddSingleton<StartupService>();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }

    /// <summary>
    /// Initialise l'interface utilisateur
    /// </summary>
    private async Task InitializeUIAsync()
    {
        Console.WriteLine("🦊 Initialisation de l'interface utilisateur...");

        // Créer la fenêtre principale (cachée initialement)
        _mainWindow = new MainWindow();
        
        // 🔥 IMPORTANT : Ne pas définir MainWindow = _mainWindow 
        // car ça peut causer des problèmes avec ShutdownMode

        // Initialiser le system tray
        _systemTrayService?.Initialize();

        // Connecter les événements du system tray
        if (_systemTrayService != null)
        {
            _systemTrayService.ShowMainWindow += OnShowMainWindow;
            _systemTrayService.ToggleCapture += OnToggleCapture;
            _systemTrayService.ClearHistory += OnClearHistory;
            _systemTrayService.ExitApplication += OnExitApplication;
            _systemTrayService.OpenSettings += OnOpenSettings;
        }

        // Initialiser les hotkeys
        if (_hotkeyService != null && _mainWindow != null)
        {
            _hotkeyService.Initialize(_mainWindow);

            // Enregistrer le hotkey principal
            var globalHotkey = _appConfig?.Settings.GlobalHotkey ?? "Ctrl+Shift+V";
            _hotkeyService.RegisterHotkey("ShowKonan", globalHotkey, () =>
            {
                Dispatcher.BeginInvoke(() => ShowMainWindow());
            });

            _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        }

        // Configurer la fenêtre principale
        if (_mainWindow != null)
        {
            // 🔥 IMPORTANT : Garder la fenêtre cachée mais créée
            _mainWindow.WindowState = WindowState.Minimized;
            _mainWindow.ShowInTaskbar = false;
            _mainWindow.Visibility = Visibility.Hidden;

            // Événements de fenêtre
            _mainWindow.StateChanged += OnMainWindowStateChanged;
            _mainWindow.Closing += OnMainWindowClosing;
        }

        await Task.CompletedTask;
        Console.WriteLine("🦊 Interface utilisateur initialisée !");
    }

    /// <summary>
    /// Configure les événements de l'application
    /// </summary>
    private void SetupApplicationEvents()
    {
        // Événements de session Windows
        SessionEnding += OnSessionEnding;
        Exit += OnApplicationExit;

        // Événements du presse-papiers
        if (_clipboardService != null)
        {
            _clipboardService.ItemCaptured += OnClipboardItemCaptured;
            _clipboardService.HistoryChanged += OnClipboardHistoryChanged;
        }
    }

    /// <summary>
    /// Charge les données persistantes
    /// </summary>
    private async Task LoadPersistedDataAsync()
    {
        try
        {
            Console.WriteLine("🦊 Chargement des données persistantes...");

            // Charger l'historique du presse-papiers
            var historyService = Services?.GetService<ClipboardHistoryService>();
            if (historyService != null)
            {
                await historyService.LoadHistoryAsync();
            }

            // Configurer le démarrage automatique
            var startupService = Services?.GetService<StartupService>();
            if (_appConfig?.Settings.StartWithWindows == true)
            {
                startupService?.EnableStartup();
            }

            Console.WriteLine("🦊 Données chargées avec succès !");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur chargement données: {ex.Message}");
        }
    }

    /// <summary>
    /// Démarre les services en arrière-plan
    /// </summary>
    private async Task StartBackgroundServicesAsync()
    {
        try
        {
            Console.WriteLine("🦊 Démarrage des services en arrière-plan...");

            // Démarrer la surveillance du presse-papiers
            if (_clipboardService != null && _mainWindow != null)
            {
                _clipboardService.StartMonitoring(_mainWindow);
                _clipboardService.IsCapturing = _appConfig?.Settings.AutoCapture ?? true;
            }

            // Démarrer le nettoyage automatique
            _ = Task.Run(StartPeriodicCleanupAsync);

            // Afficher notification de démarrage
            _systemTrayService?.ShowNotification(
                "🦊 Konan",
                "Konan est maintenant actif et surveille votre presse-papiers !",
                Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

            await Task.CompletedTask;
            Console.WriteLine("🦊 Services en arrière-plan démarrés !");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur démarrage services: {ex.Message}");
        }
    }

    /// <summary>
    /// Nettoyage périodique automatique
    /// </summary>
    private async Task StartPeriodicCleanupAsync()
    {
        try
        {
            while (!_isShuttingDown)
            {
                await Task.Delay(TimeSpan.FromHours(6)); // Nettoyage toutes les 6h

                if (_isShuttingDown) break;

                var historyService = Services?.GetService<ClipboardHistoryService>();
                if (historyService != null)
                {
                    await historyService.PerformAutoCleanupAsync();
                }

                Console.WriteLine("🦊 Nettoyage automatique effectué");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur nettoyage périodique: {ex.Message}");
        }
    }

    #region Event Handlers

    private void OnShowMainWindow(object? sender, EventArgs e)
    {
        ShowMainWindow();
    }

    private void OnToggleCapture(object? sender, EventArgs e)
    {
        if (_clipboardService != null)
        {
            _clipboardService.IsCapturing = !_clipboardService.IsCapturing;
            _systemTrayService?.UpdateCaptureState(_clipboardService.IsCapturing);

            var status = _clipboardService.IsCapturing ? "activée" : "désactivée";
            _systemTrayService?.ShowNotification("🦊 Konan", $"Capture {status}", 
                Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
        }
    }

    private async void OnClearHistory(object? sender, EventArgs e)
    {
        try
        {
            _clipboardService?.ClearHistory();
            var historyService = Services?.GetService<ClipboardHistoryService>();
            if (historyService != null)
            {
                await historyService.ClearHistoryAsync();
            }

            _systemTrayService?.ShowNotification("🦊 Konan", "Historique vidé !",
                Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur vidage historique: {ex.Message}");
        }
    }

    private void OnExitApplication(object? sender, EventArgs e)
    {
        ExitApplication();
    }

    private void OnOpenSettings(object? sender, EventArgs e)
    {
        // Ouvrir la fenêtre de paramètres
        ShowMainWindow();
    }

    private void OnHotkeyPressed(object? sender, HotkeyService.HotkeyPressedEventArgs e)
    {
        Console.WriteLine($"🦊 Hotkey pressé: {e.Name}");

        if (e.Name == "ShowKonan")
        {
            ShowMainWindow();
        }
    }

    private void OnClipboardItemCaptured(object? sender, ClipboardItem e)
    {
        // Notification optionnelle pour les nouveaux éléments
        if (_appConfig?.Settings.EnableSounds == true)
        {
            _systemTrayService?.ShowItemCapturedNotification(e.DisplayPreview);
        }

        // Sauvegarder automatiquement
        _ = Task.Run(async () =>
        {
            try
            {
                var historyService = Services?.GetService<ClipboardHistoryService>();
                if (historyService != null)
                {
                    await historyService.SaveHistoryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🦊 Erreur sauvegarde: {ex.Message}");
            }
        });
    }

    private async void OnClipboardHistoryChanged(object? sender, EventArgs e)
    {
        // Mettre à jour l'interface si elle est ouverte
        if (_mainWindow != null && _mainWindow.IsVisible)
        {
            await _mainWindow.RefreshClipboardHistoryAsync();
        }
    }

    private void OnMainWindowStateChanged(object? sender, EventArgs e)
    {
        if (_mainWindow?.WindowState == WindowState.Minimized)
        {
            _mainWindow.Hide();
            _mainWindow.ShowInTaskbar = false;
        }
    }

    private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Empêcher la fermeture, juste minimiser
        e.Cancel = true;
        _mainWindow?.Hide();
        if (_mainWindow != null)
        {
            _mainWindow.ShowInTaskbar = false;
        }
    }

    private async void OnSessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        Console.WriteLine("🦊 Session Windows se termine...");
        await SaveAllDataAsync();
    }

    private async void OnApplicationExit(object sender, ExitEventArgs e)
    {
        Console.WriteLine("🦊 Application se ferme...");
        await SaveAllDataAsync();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Affiche la fenêtre principale
    /// </summary>
    public void ShowMainWindow()
    {
        try
        {
            if (_mainWindow != null)
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.ShowInTaskbar = true;
                _mainWindow.Activate();
                _mainWindow.Focus();
                
                Console.WriteLine("🦊 Fenêtre principale affichée !");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur affichage fenêtre: {ex.Message}");
        }
    }

    /// <summary>
    /// Ferme complètement l'application
    /// </summary>
    public void ExitApplication()
    {
        try
        {
            Console.WriteLine("🦊 Fermeture de Konan...");
            _isShuttingDown = true;

            Task.Run(async () =>
            {
                await SaveAllDataAsync();
                Dispatcher.BeginInvoke(() => 
                {
                    // 🔥 Utiliser Shutdown() explicitement
                    Shutdown();
                });
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur fermeture: {ex.Message}");
            Shutdown(); // Forcer la fermeture
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Sauvegarde toutes les données
    /// </summary>
    private async Task SaveAllDataAsync()
    {
        try
        {
            Console.WriteLine("🦊 Sauvegarde des données...");

            // Sauvegarder la configuration
            if (_appConfig != null)
            {
                await _appConfig.SaveSettingsAsync();
            }

            // Sauvegarder l'historique
            var historyService = Services?.GetService<ClipboardHistoryService>();
            if (historyService != null)
            {
                await historyService.SaveHistoryAsync();
            }

            Console.WriteLine("🦊 Données sauvegardées !");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur sauvegarde: {ex.Message}");
        }
    }

    /// <summary>
    /// Log une exception
    /// </summary>
    private static void LogException(Exception ex)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Constants.DATA_FOLDER,
                "error.log");

            var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {ex}\n\n";
            File.AppendAllText(logPath, logEntry);
        }
        catch
        {
            // Si on ne peut pas logger, on ignore silencieusement
        }
    }

    #endregion

    /// <summary>
    /// Nettoyage lors de la fermeture
    /// </summary>
    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            Console.WriteLine("🦊 Nettoyage final...");

            // Arrêter les services
            _clipboardService?.StopMonitoring();
            _hotkeyService?.Dispose();
            _systemTrayService?.Dispose();
            _clipboardService?.Dispose();

            // Arrêter l'host
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }

            // Libérer le mutex
            _applicationMutex?.ReleaseMutex();
            _applicationMutex?.Dispose();

            Console.WriteLine("🦊 Au revoir !");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur nettoyage final: {ex.Message}");
        }
        finally
        {
            base.OnExit(e);
        }
    }
}