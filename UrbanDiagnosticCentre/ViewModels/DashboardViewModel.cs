using System.Collections.ObjectModel;
using UrbanDiagnosticCentre.Helpers;
using UrbanDiagnosticCentre.Models;
using UrbanDiagnosticCentre.Services;

namespace UrbanDiagnosticCentre.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly ReportService _reportService;
    private readonly PrintService _printService;
    private readonly SettingsService _settingsService;

    public string WelcomeMessage => $"Welcome, {_authService.CurrentUser?.FullName ?? "User"}";
    public string UserRole => _authService.CurrentUser?.Role ?? string.Empty;
    public string UserInitials => GetInitials(_authService.CurrentUser?.FullName);
    public bool IsAdmin => _authService.CurrentUser?.Role == "Admin";
    public string CurrentDate => DateTime.Now.ToString("dddd, dd MMMM yyyy");

    // ── Stat cards ────────────────────────────────────────────────────────────
    private int _todayPatients;
    public int TodayPatients { get => _todayPatients; private set => SetProperty(ref _todayPatients, value); }

    private int _todayTests;
    public int TodayTests { get => _todayTests; private set => SetProperty(ref _todayTests, value); }

    private int _readyReports;
    public int ReadyReports { get => _readyReports; private set => SetProperty(ref _readyReports, value); }

    private int _pendingReports;
    public int PendingReports { get => _pendingReports; private set => SetProperty(ref _pendingReports, value); }

    // ── Disk space ────────────────────────────────────────────────────────────
    private string? _diskSpaceWarning;
    public string? DiskSpaceWarning
    {
        get => _diskSpaceWarning;
        private set { SetProperty(ref _diskSpaceWarning, value); OnPropertyChanged(nameof(HasDiskSpaceWarning)); }
    }
    public bool HasDiskSpaceWarning => !string.IsNullOrEmpty(_diskSpaceWarning);

    // ── Setup warnings (first-run / misconfiguration) ─────────────────────────
    private string? _setupWarning;
    public string? SetupWarning
    {
        get => _setupWarning;
        private set { SetProperty(ref _setupWarning, value); OnPropertyChanged(nameof(HasSetupWarning)); }
    }
    public bool HasSetupWarning => !string.IsNullOrEmpty(_setupWarning);

    // ── Recent reports ────────────────────────────────────────────────────────
    public ObservableCollection<ReportSummary> RecentReports { get; } = new();
    public bool HasNoRecentReports => RecentReports.Count == 0;

    // ── Commands ──────────────────────────────────────────────────────────────
    public RelayCommand LogoutCommand { get; }
    public RelayCommand NavigateToPatientsCommand     { get; }
    public RelayCommand NavigateToTestManagerCommand  { get; }
    public RelayCommand NavigateToNewReportCommand    { get; }
    public RelayCommand NavigateToHistoryCommand      { get; }
    public RelayCommand NavigateToBackupCommand       { get; }
    public RelayCommand NavigateToSettingsCommand     { get; }
    public RelayCommand NavigateToMaintenanceCommand  { get; }
    public RelayCommand NavigateToAccountsCommand     { get; }
    public RelayCommand NavigateToPackagesCommand     { get; }
    public RelayCommand PrintReportCommand            { get; }
    public RelayCommand EditDraftCommand              { get; }

    public DashboardViewModel(IAuthService authService, INavigationService navigationService, ReportService reportService, PrintService printService, SettingsService settingsService)
    {
        _authService = authService;
        _navigationService = navigationService;
        _reportService = reportService;
        _printService = printService;
        _settingsService = settingsService;

        LogoutCommand                = new RelayCommand(ExecuteLogout);
        NavigateToPatientsCommand    = new RelayCommand(_ => _navigationService.NavigateTo<ReportHistoryViewModel>());
        NavigateToTestManagerCommand = new RelayCommand(_ => _navigationService.NavigateTo<TestManagerViewModel>());
        NavigateToNewReportCommand   = new RelayCommand(_ => _navigationService.NavigateTo<PatientReportViewModel>());
        NavigateToHistoryCommand     = new RelayCommand(_ => _navigationService.NavigateTo<ReportHistoryViewModel>());
        NavigateToBackupCommand      = new RelayCommand(_ => _navigationService.NavigateTo<BackupViewModel>());
        NavigateToSettingsCommand    = new RelayCommand(_ => _navigationService.NavigateTo<SettingsViewModel>());
        NavigateToMaintenanceCommand = new RelayCommand(_ => _navigationService.NavigateTo<MaintenanceViewModel>());
        NavigateToAccountsCommand    = new RelayCommand(_ => _navigationService.NavigateTo<AccountsViewModel>());
        NavigateToPackagesCommand    = new RelayCommand(_ => _navigationService.NavigateTo<PackageManagerViewModel>());
        PrintReportCommand           = new RelayCommand(ExecutePrintReport);
        EditDraftCommand             = new RelayCommand(ExecuteEditDraft);

        Refresh();
    }

    public void Refresh()
    {
        var stats = _reportService.GetTodayStats();
        TodayPatients  = stats.TodayPatients;
        TodayTests     = stats.TodayTests;
        ReadyReports   = stats.ReadyReports;
        PendingReports = stats.PendingReports;

        RecentReports.Clear();
        foreach (var r in _reportService.GetRecent(10))
            RecentReports.Add(r);
        OnPropertyChanged(nameof(HasNoRecentReports));

        DiskSpaceWarning = _reportService.GetDiskSpaceWarning();

        var settings = _settingsService.Load();
        var warnings = InstallerPreparationService.GetSetupWarnings(settings, _reportService.GetReportsRoot());
        SetupWarning = warnings.FirstOrDefault();
    }

    private void ExecuteLogout(object? _)
    {
        _authService.Logout();
        _navigationService.NavigateTo<LoginViewModel>();
    }

    private void ExecutePrintReport(object? parameter)
    {
        if (parameter is not ReportSummary summary) return;
        _printService.OpenPdf(summary.Id, summary.ReportCode);
    }

    private void ExecuteEditDraft(object? parameter)
    {
        if (parameter is not ReportSummary summary) return;
        _navigationService.NavigateTo<PatientReportViewModel>(summary.Id);
    }

    private static string GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "?";
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}"
            : $"{parts[0][0]}";
    }
}
