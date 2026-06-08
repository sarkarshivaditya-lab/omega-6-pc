using UrbanDiagnosticCentre.ViewModels;

namespace UrbanDiagnosticCentre.Services;

public class NavigationService : INavigationService
{
    private readonly Func<Type, BaseViewModel> _viewModelFactory;
    private BaseViewModel _currentViewModel = null!;

    public BaseViewModel CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            _currentViewModel = value;
            CurrentViewModelChanged?.Invoke();
        }
    }

    public event Action? CurrentViewModelChanged;

    public NavigationService(Func<Type, BaseViewModel> viewModelFactory)
    {
        _viewModelFactory = viewModelFactory;
    }

    public void NavigateTo<TViewModel>() where TViewModel : BaseViewModel
    {
        CurrentViewModel = _viewModelFactory(typeof(TViewModel));
    }
}
