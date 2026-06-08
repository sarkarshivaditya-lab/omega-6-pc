using System.Windows.Controls;
using System.Windows.Input;
using UrbanDiagnosticCentre.ViewModels;

namespace UrbanDiagnosticCentre.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void LoginButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.LoginCommand.Execute(PasswordBox.Password);
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            LoginButton_Click(sender, new System.Windows.RoutedEventArgs());
    }
}
