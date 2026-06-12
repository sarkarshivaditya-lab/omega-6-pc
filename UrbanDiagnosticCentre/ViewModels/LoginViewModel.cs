using UrbanDiagnosticCentre.Helpers;
using UrbanDiagnosticCentre.Services;

namespace UrbanDiagnosticCentre.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    private string _username = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoggingIn;

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoggingIn
    {
        get => _isLoggingIn;
        set => SetProperty(ref _isLoggingIn, value);
    }

    public RelayCommand LoginCommand { get; }

    public LoginViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
        LoginCommand = new RelayCommand(ExecuteLogin, _ => !IsLoggingIn);
    }

    private async void ExecuteLogin(object? parameter)
    {
        var password = parameter as string ?? string.Empty;
        ErrorMessage = string.Empty;
        IsLoggingIn = true;

        try
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Please enter your username.";
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "Please enter your password.";
                return;
            }

            if (await _authService.LoginAsync(Username, password))
            {
                if (_authService.IsUsingDefaultPassword)
                    _navigationService.NavigateTo<SettingsViewModel>();
                else
                    _navigationService.NavigateTo<DashboardViewModel>();
            }
            else
            {
                ErrorMessage = "Invalid username or password. Please try again.";
            }
        }
        catch (Exception ex)
        {
            ErrorLoggingService.Log(ex, "Login");
            ErrorMessage = "Unable to sign in. Please try again or contact your administrator.";
        }
        finally
        {
            IsLoggingIn = false;
        }
    }
}
