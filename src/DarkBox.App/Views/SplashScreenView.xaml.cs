using System.Windows;
using System.Windows.Controls;

namespace DarkBox.App.Views;

public partial class SplashScreenView : UserControl
{
    public SplashScreenView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewAnimations.FadeInSlide(this);
    }
}
