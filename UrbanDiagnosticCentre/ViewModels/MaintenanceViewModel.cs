using System.Collections.ObjectModel;
using UrbanDiagnosticCentre.Helpers;
using UrbanDiagnosticCentre.Services;

namespace UrbanDiagnosticCentre.ViewModels;

public class MaintenanceViewModel : BaseViewModel
{
    private readonly MaintenanceService    _maintenanceService;
    private readonly IAuthService          _authService;
    private readonly INavigationService    _navigationService;

    // ── Access guard ──────────────────────────────────────────────────────────
    public bool IsAdmin    => _authService.CurrentUser?.Role == "Admin";
    public bool IsNotAdmin => !IsAdmin;

    // ── Status ────────────────────────────────────────────────────────────────
    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        private set { SetProperty(ref _statusMessage, value); OnPropertyChanged(nameof(HasStatus)); }
    }

    private bool _statusIsError;
    public bool StatusIsError
    {
        get => _statusIsError;
        private set => SetProperty(ref _statusIsError, value);
    }

    public bool HasStatus => !string.IsNullOrEmpty(_statusMessage);

    private bool _isWorking;
    public bool IsWorking
    {
        get => _isWorking;
        private set => SetProperty(ref _isWorking, value);
    }

    // ── Result details (file lists for orphan/missing PDF scans) ──────────────
    public ObservableCollection<string> ResultDetails { get; } = new();
    public bool HasResultDetails => ResultDetails.Count > 0;

    // ── Commands ──────────────────────────────────────────────────────────────
    public RelayCommand IntegrityCheckCommand  { get; }
    public RelayCommand FindMissingPdfsCommand { get; }
    public RelayCommand FindOrphanPdfsCommand  { get; }
    public RelayCommand VacuumCommand          { get; }
    public RelayCommand BackCommand            { get; }

    public MaintenanceViewModel(
        MaintenanceService    maintenanceService,
        IAuthService          authService,
        INavigationService    navigationService)
    {
        _maintenanceService = maintenanceService;
        _authService        = authService;
        _navigationService  = navigationService;

        IntegrityCheckCommand  = new RelayCommand(async _ => await RunAsync(_maintenanceService.RunIntegrityCheck,  "Checking database integrity…"));
        FindMissingPdfsCommand = new RelayCommand(async _ => await RunAsync(_maintenanceService.FindMissingPdfs,    "Scanning for missing PDF files…"));
        FindOrphanPdfsCommand  = new RelayCommand(async _ => await RunAsync(_maintenanceService.FindOrphanPdfs,     "Scanning for orphan PDF files…"));
        VacuumCommand          = new RelayCommand(async _ => await RunAsync(_maintenanceService.RunVacuum,          "Vacuuming database…"));
        BackCommand            = new RelayCommand(_ => _navigationService.NavigateTo<DashboardViewModel>());
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private async Task RunAsync(Func<MaintenanceResult> operation, string progressMessage)
    {
        if (_isWorking) return;

        IsWorking     = true;
        StatusIsError = false;
        StatusMessage = progressMessage;
        ResultDetails.Clear();
        OnPropertyChanged(nameof(HasResultDetails));

        try
        {
            var result = await Task.Run(operation);

            StatusIsError = !result.Success;
            StatusMessage = result.Message;

            foreach (var detail in result.Details)
                ResultDetails.Add(detail);
            OnPropertyChanged(nameof(HasResultDetails));
        }
        catch (Exception ex)
        {
            StatusIsError = true;
            StatusMessage = $"Operation failed: {ex.Message}";
            ErrorLoggingService.Log(ex, "Maintenance");
        }
        finally
        {
            IsWorking = false;
        }
    }
}
