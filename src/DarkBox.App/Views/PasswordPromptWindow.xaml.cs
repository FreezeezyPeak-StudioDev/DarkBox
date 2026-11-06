using System.Windows;

namespace DarkBox.App.Views;

public partial class PasswordPromptWindow : Window
{
    private readonly bool _requireConfirmation;

    public PasswordPromptWindow(string title, string message, bool requireConfirmation)
    {
        InitializeComponent();
        _requireConfirmation = requireConfirmation;
        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmBox.Visibility = requireConfirmation ? Visibility.Visible : Visibility.Collapsed;
        Loaded += (_, _) => PasswordBox.Focus();
    }

    public string Password { get; private set; } = string.Empty;

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        if (PasswordBox.Password.Length == 0)
        {
            ShowError("Escribe la contraseña.");
            return;
        }

        if (_requireConfirmation && PasswordBox.Password != ConfirmBox.Password)
        {
            ShowError("Las contraseñas no coinciden.");
            return;
        }

        Password = PasswordBox.Password;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
