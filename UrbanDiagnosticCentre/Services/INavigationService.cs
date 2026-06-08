using UrbanDiagnosticCentre.ViewModels;

namespace UrbanDiagnosticCentre.Services;

public interface INavigationService
{
    BaseViewModel CurrentViewModel { get; }
    void NavigateTo<TViewModel>() where TViewModel : BaseViewModel;
}
