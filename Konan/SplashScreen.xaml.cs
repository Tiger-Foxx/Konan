using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Konan;

/// <summary>
/// Splash Screen de Konan
/// 🦊 L'écran d'accueil du renard !
/// </summary>
public partial class SplashScreen : Window
{
    public SplashScreen()
    {
        InitializeComponent();
        Loaded += SplashScreen_Loaded;
    }

    private async void SplashScreen_Loaded(object sender, RoutedEventArgs e)
    {
        // Démarrer l'animation
        var animation = (Storyboard)Resources["SplashAnimation"];
        animation.Begin();

        // Attendre la fin de l'animation puis fermer
        await Task.Delay(4000);
        
        // Fermer le splash et ouvrir l'app principale
        DialogResult = true;
        Close();
    }
}