using UrbanDiagnosticCentre.ViewModels;

namespace UrbanDiagnosticCentre.Services;

public class NavigationService : INavigationService
{
    private readonly Func<Type, BaseViewModel> _viewModelFactory;
    private BaseViewModel _currentViewModel = null!;

    // Set before factory invocation so App.xaml.cs can read it during ViewModel construction.
    internal int? PendingEditId { get; private set; }

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
        PendingEditId = null;
        CurrentViewModel = _viewModelFactory(typeof(TViewModel));
    }

    public void NavigateTo<TViewModel>(int editId) where TViewModel : BaseViewModel
    {
        PendingEditId = editId;
        CurrentViewModel = _viewModelFactory(typeof(TViewModel));
        PendingEditId = null;
    }
}
