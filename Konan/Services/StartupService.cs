using System;
using Microsoft.Win32;
using Konan.Configuration;

namespace Konan.Services;

/// <summary>
/// Service de gestion du démarrage automatique
/// 🦊 Pour que notre renard soit toujours là !
/// </summary>
public class StartupService
{
    private readonly AppConfig _appConfig;

    public StartupService(AppConfig appConfig)
    {
        _appConfig = appConfig;
    }

    /// <summary>
    /// Active le démarrage automatique
    /// </summary>
    public bool EnableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(Constants.REGISTRY_KEY, true);
            if (key != null)
            {
                var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var exeDir = System.IO.Path.GetDirectoryName(exePath);
                var actualExe = System.IO.Path.Combine(exeDir!, "Konan.exe");
                
                if (!System.IO.File.Exists(actualExe))
                {
                    actualExe = exePath;
                }

                key.SetValue(Constants.REGISTRY_VALUE_NAME, $"\"{actualExe}\" --startup");
                Console.WriteLine("🦊 Démarrage automatique activé !");
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur activation démarrage: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Désactive le démarrage automatique
    /// </summary>
    public bool DisableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(Constants.REGISTRY_KEY, true);
            if (key != null)
            {
                key.DeleteValue(Constants.REGISTRY_VALUE_NAME, false);
                Console.WriteLine("🦊 Démarrage automatique désactivé !");
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur désactivation démarrage: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Vérifie si le démarrage automatique est activé
    /// </summary>
    public bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(Constants.REGISTRY_KEY);
            var value = key?.GetValue(Constants.REGISTRY_VALUE_NAME);
            return value != null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🦊 Erreur vérification démarrage: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Met à jour le statut selon la configuration
    /// </summary>
    public void UpdateStartupStatus()
    {
        var shouldStart = _appConfig.Settings.StartWithWindows;
        var isEnabled = IsStartupEnabled();

        if (shouldStart && !isEnabled)
        {
            EnableStartup();
        }
        else if (!shouldStart && isEnabled)
        {
            DisableStartup();
        }
    }
}