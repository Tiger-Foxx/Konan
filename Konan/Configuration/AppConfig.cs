using System;
using System.IO;
using Konan.Models;
using Konan.Persistence;
using Microsoft.Win32;

namespace Konan.Configuration;

/// <summary>
/// Gestionnaire de configuration de Konan
/// 🦊 Le cerveau de notre renard zen !
/// </summary>
public class AppConfig
{
    private readonly IDataPersistence _persistence;
    private readonly string _configPath;
    private AppSettings? _settings;

    public AppConfig(IDataPersistence persistence)
    {
        _persistence = persistence;
        _configPath = GetConfigPath();
        EnsureDataDirectoryExists();
    }

    /// <summary>
    /// Paramètres actuels de l'application
    /// </summary>
    public AppSettings Settings
    {
        get
        {
            _settings ??= LoadSettings();
            return _settings;
        }
    }

    /// <summary>
    /// Chemin du dossier de données
    /// </summary>
    public string DataPath => GetDataPath();

    /// <summary>
    /// Charge les paramètres depuis le fichier
    /// </summary>
    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var settings = _persistence.Load<AppSettings>(_configPath);
                if (settings != null)
                {
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            // Log l'erreur mais continue avec les paramètres par défaut
            Console.WriteLine($"🦊 Erreur lors du chargement de la config: {ex.Message}");
        }

        return new AppSettings();
    }

    /// <summary>
    /// Sauvegarde les paramètres
    /// </summary>
    public async Task SaveSettingsAsync()
    {
        try
        {
            if (_settings != null)
            {
                await _persistence.SaveAsync(_settings, _configPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur lors de la sauvegarde: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Met à jour un paramètre spécifique
    /// </summary>
    public async Task UpdateSettingAsync<T>(Action<AppSettings> updateAction)
    {
        updateAction(Settings);
        
        if (Settings.AutoSave)
        {
            await SaveSettingsAsync();
        }
    }

    /// <summary>
    /// Configure le démarrage automatique avec Windows
    /// </summary>
    public void ConfigureStartup(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(Constants.REGISTRY_KEY, true);
            if (key != null)
            {
                if (enabled)
                {
                    var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    key.SetValue(Constants.REGISTRY_VALUE_NAME, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(Constants.REGISTRY_VALUE_NAME, false);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur config démarrage: {ex.Message}");
        }
    }

    /// <summary>
    /// Vérifie si le démarrage automatique est activé
    /// </summary>
    public bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(Constants.REGISTRY_KEY);
            return key?.GetValue(Constants.REGISTRY_VALUE_NAME) != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Obtient le chemin du fichier de configuration
    /// </summary>
    private static string GetConfigPath()
    {
        return Path.Combine(GetDataPath(), Constants.SETTINGS_FILE);
    }

    /// <summary>
    /// Obtient le chemin du dossier de données
    /// </summary>
    private static string GetDataPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, Constants.DATA_FOLDER);
    }

    /// <summary>
    /// S'assure que le dossier de données existe
    /// </summary>
    private void EnsureDataDirectoryExists()
    {
        var dataPath = GetDataPath();
        if (!Directory.Exists(dataPath))
        {
            Directory.CreateDirectory(dataPath);
        }

        var previewPath = Path.Combine(dataPath, Constants.PREVIEW_FOLDER);
        if (!Directory.Exists(previewPath))
        {
            Directory.CreateDirectory(previewPath);
        }
    }

    /// <summary>
    /// Réinitialise les paramètres aux valeurs par défaut
    /// </summary>
    public async Task ResetToDefaultsAsync()
    {
        _settings = new AppSettings();
        await SaveSettingsAsync();
    }
}