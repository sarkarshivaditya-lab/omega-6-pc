using UrbanDiagnosticCentre.Services;

namespace UrbanDiagnosticCentre.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel _currentViewModel = null!;

    public BaseViewModel CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
    }

    public MainViewModel(NavigationService navigationService)
    {
        navigationService.CurrentViewModelChanged += () =>
            CurrentViewModel = navigationService.CurrentViewModel;

        CurrentViewModel = navigationService.CurrentViewModel;
    }
}
