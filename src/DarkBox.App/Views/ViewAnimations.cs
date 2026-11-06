using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DarkBox.App.Views;

internal static class ViewAnimations
{
    public static void FadeInSlide(FrameworkElement element)
    {
        element.Opacity = 0;
        var translate = new TranslateTransform(0, 10);
        element.RenderTransform = translate;

        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease };
        var slide = new DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease };

        element.BeginAnimation(UIElement.OpacityProperty, fade);
        translate.BeginAnimation(TranslateTransform.YProperty, slide);
    }
}
