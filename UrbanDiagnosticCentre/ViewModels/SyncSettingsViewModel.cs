using System.Net.Http;
using UrbanDiagnosticCentre.Data;
using UrbanDiagnosticCentre.Helpers;
using UrbanDiagnosticCentre.Models;
using UrbanDiagnosticCentre.Services;
using UrbanDiagnosticCentre.Sync;

namespace UrbanDiagnosticCentre.ViewModels;

public class SyncSettingsViewModel : BaseViewModel
{
    private readonly AppDbContext      _db;
    private readonly IAuthService      _authService;
    private readonly INavigationService _navigationService;
    private SyncIdentity? _identity;

    // ── Machine identity (read-only display) ──────────────────────────────────
    public string MachineRole => AppDbContext.CurrentMachineCode switch
    {
        "ADM" => "Administrator (ADM)",
        "RCP" => "Reception Terminal (RCP)",
        _     => "Not configured"
    };

    public string MachineIdDisplay => _identity?.MachineId.ToString() ?? "Not set";
    public bool IsAdminMachine     => AppDbContext.CurrentMachineCode == "ADM";
    public bool IsReception        => AppDbContext.CurrentMachineCode == "RCP";
    public bool IsAdmin            => _authService.CurrentUser?.Role == "Admin";

    // ── Reception connection settings (editable) ──────────────────────────────
    private string _adminApiBaseUrl = string.Empty;
    public string AdminApiBaseUrl
    {
        get => _adminApiBaseUrl;
        set => SetProperty(ref _adminApiBaseUrl, value);
    }

    private string _apiKey = string.Empty;
    public string ApiKey
    {
        get => _apiKey;
        set => SetProperty(ref _apiKey, value);
    }

    // ── Live sync status ──────────────────────────────────────────────────────
    public string SyncStateLabel   => SyncStatusInfo.Instance.StateLabel;
    public string SyncMessage      => SyncStatusInfo.Instance.Message;
    public string LastSyncDisplay  => SyncStatusInfo.Instance.LastSyncAtDisplay;
    public int    PendingCount     => SyncStatusInfo.Instance.PendingCount;
    public int    ConsFailures     => SyncStatusInfo.Instance.ConsecutiveFailures;
    public string RetryDelay       => SyncStatusInfo.Instance.RetryDelayDisplay;

    // ── Test connection ───────────────────────────────────────────────────────
    private string _testStatus = string.Empty;
    public string TestStatus
    {
        get => _testStatus;
        set { SetProperty(ref _testStatus, value); OnPropertyChanged(nameof(HasTestStatus)); }
    }
    public bool HasTestStatus => !string.IsNullOrEmpty(_testStatus);

    private bool _testIsError;
    public bool TestIsError
    {
        get => _testIsError;
        set => SetProperty(ref _testIsError, value);
    }

    // ── Save status ───────────────────────────────────────────────────────────
    private string _saveStatus = string.Empty;
    public string SaveStatus
    {
        get => _saveStatus;
        set { SetProperty(ref _saveStatus, value); OnPropertyChanged(nameof(HasSaveStatus)); }
    }
    public bool HasSaveStatus => !string.IsNullOrEmpty(_saveStatus);

    private bool _saveStatusIsError;
    public bool SaveStatusIsError
    {
        get => _saveStatusIsError;
        set => SetProperty(ref _saveStatusIsError, value);
    }

    // ── Commands ──────────────────────────────────────────────────────────────
    public RelayCommand SaveCommand          { get; }
    public RelayCommand TestCommand          { get; }
    public RelayCommand BackCommand          { get; }
    public RelayCommand CopyMachineIdCommand { get; }

    public SyncSettingsViewModel(AppDbContext db, IAuthService authService, INavigationService navigationService)
    {
        _db                = db;
        _authService       = authService;
        _navigationService = navigationService;

        SaveCommand          = new RelayCommand(ExecuteSave, _ => IsAdmin && IsReception);
        TestCommand          = new RelayCommand(ExecuteTest, _ => IsReception);
        BackCommand          = new RelayCommand(_ => _navigationService.NavigateTo<DashboardViewModel>());
        CopyMachineIdCommand = new RelayCommand(_ => System.Windows.Clipboard.SetText(MachineIdDisplay));

        SyncStatusInfo.Instance.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(SyncStateLabel));
            OnPropertyChanged(nameof(SyncMessage));
            OnPropertyChanged(nameof(LastSyncDisplay));
            OnPropertyChanged(nameof(PendingCount));
            OnPropertyChanged(nameof(ConsFailures));
            OnPropertyChanged(nameof(RetryDelay));
        };

        LoadIdentity();
    }

    private void LoadIdentity()
    {
        _identity = _db.SyncIdentities.FirstOrDefault();
        _adminApiBaseUrl = _identity?.AdminApiBaseUrl ?? string.Empty;
        _apiKey          = _identity?.ApiKey          ?? string.Empty;
        OnPropertyChanged(nameof(AdminApiBaseUrl));
        OnPropertyChanged(nameof(ApiKey));
        OnPropertyChanged(nameof(MachineIdDisplay));
    }

    private void ExecuteSave(object? _)
    {
        try
        {
            if (_identity is null)
            {
                _identity = new SyncIdentity { Id = 1, MachineCode = "RCP" };
                _db.SyncIdentities.Add(_identity);
            }
            _identity.AdminApiBaseUrl = string.IsNullOrWhiteSpace(_adminApiBaseUrl) ? null : _adminApiBaseUrl.Trim();
            _identity.ApiKey          = string.IsNullOrWhiteSpace(_apiKey)          ? null : _apiKey.Trim();
            _db.SaveChanges();

            SaveStatusIsError = false;
            SaveStatus        = "Sync settings saved.";
        }
        catch (Exception ex)
        {
            SaveStatusIsError = true;
            SaveStatus        = $"Failed to save: {ex.Message}";
        }
    }

    private void ExecuteTest(object? _)
    {
        var url = _adminApiBaseUrl.Trim();
        var key = _apiKey.Trim();
        if (string.IsNullOrEmpty(url))
        {
            TestIsError  = true;
            TestStatus   = "Admin API URL is empty.";
            return;
        }

        TestIsError = false;
        TestStatus  = "Testing connection…";

        Task.Run(() =>
        {
            try
            {
                using var client = new HttpClient { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(10) };
                if (!string.IsNullOrEmpty(key))
                    client.DefaultRequestHeaders.Add("X-Api-Key", key);

                var response = client.GetAsync("/sync/health").GetAwaiter().GetResult();
                var msg      = response.IsSuccessStatusCode
                    ? $"Connected! Admin responded HTTP {(int)response.StatusCode}"
                    : $"HTTP {(int)response.StatusCode} — check API key";
                SyncStatusInfo.Instance.SetOn(() =>
                {
                    TestIsError = !response.IsSuccessStatusCode;
                    TestStatus  = msg;
                });
            }
            catch (Exception ex)
            {
                SyncStatusInfo.Instance.SetOn(() =>
                {
                    TestIsError = true;
                    TestStatus  = $"Connection failed: {ex.Message}";
                });
            }
        });
    }
}
