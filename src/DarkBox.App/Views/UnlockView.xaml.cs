using System.Windows;
using System.Windows.Controls;

namespace DarkBox.App.Views;

public partial class UnlockView : UserControl
{
    public UnlockView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewAnimations.FadeInSlide(this);
    }
}
