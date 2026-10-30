using System.Windows;

namespace DarkBox.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        FitToWorkArea();
    }

    private void FitToWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        var availableWidth = Math.Max(720, workArea.Width - 24);
        var availableHeight = Math.Max(480, workArea.Height - 24);

        MinWidth = Math.Min(MinWidth, availableWidth);
        MinHeight = Math.Min(MinHeight, availableHeight);
        Width = Math.Min(Width, availableWidth);
        Height = Math.Min(Height, availableHeight);
    }
}
