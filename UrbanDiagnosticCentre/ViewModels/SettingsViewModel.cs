using UrbanDiagnosticCentre.Helpers;
using UrbanDiagnosticCentre.Models;
using UrbanDiagnosticCentre.Services;

namespace UrbanDiagnosticCentre.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly SettingsService    _settingsService;
    private readonly IAuthService       _authService;
    private readonly INavigationService _navigationService;

    private AppSettings _loaded = new();

    // ── Access guard ──────────────────────────────────────────────────────────
    public bool IsAdmin    => _authService.CurrentUser?.Role == "Admin";
    public bool IsNotAdmin => !IsAdmin;

    // ── Organisation ──────────────────────────────────────────────────────────
    private string _centreName = string.Empty;
    public string CentreName
    {
        get => _centreName;
        set => SetProperty(ref _centreName, value);
    }

    private string _centreAddress = string.Empty;
    public string CentreAddress
    {
        get => _centreAddress;
        set => SetProperty(ref _centreAddress, value);
    }

    private string _centrePhone = string.Empty;
    public string CentrePhone
    {
        get => _centrePhone;
        set => SetProperty(ref _centrePhone, value);
    }

    private string _centreEmail = string.Empty;
    public string CentreEmail
    {
        get => _centreEmail;
        set => SetProperty(ref _centreEmail, value);
    }

    private string _logoPath = string.Empty;
    public string LogoPath
    {
        get => _logoPath;
        set => SetProperty(ref _logoPath, value);
    }

    // ── Pricing ───────────────────────────────────────────────────────────────
    private string _defaultPriceTier = string.Empty;
    public string DefaultPriceTier
    {
        get => _defaultPriceTier;
        set => SetProperty(ref _defaultPriceTier, value);
    }

    // ── Storage locations ─────────────────────────────────────────────────────
    private string _reportsRootPath = string.Empty;
    public string ReportsRootPath
    {
        get => _reportsRootPath;
        set => SetProperty(ref _reportsRootPath, value);
    }

    private string _backupsRootPath = string.Empty;
    public string BackupsRootPath
    {
        get => _backupsRootPath;
        set => SetProperty(ref _backupsRootPath, value);
    }

    // ── PDF branding (future-ready) ───────────────────────────────────────────
    private string _gstTaxId = string.Empty;
    public string GstTaxId
    {
        get => _gstTaxId;
        set => SetProperty(ref _gstTaxId, value);
    }

    private string _signatureFooterText = string.Empty;
    public string SignatureFooterText
    {
        get => _signatureFooterText;
        set => SetProperty(ref _signatureFooterText, value);
    }

    private string _watermarkText = string.Empty;
    public string WatermarkText
    {
        get => _watermarkText;
        set => SetProperty(ref _watermarkText, value);
    }

    // ── Metadata ──────────────────────────────────────────────────────────────
    public string LastSavedDisplay => _loaded.UpdatedAt.HasValue
        ? $"Last saved {_loaded.UpdatedAt.Value:dd MMM yyyy, hh:mm tt}"
        : "Not saved yet";

    // ── Password change ───────────────────────────────────────────────────────

    public string PasswordChangeForDisplay =>
        $"Signed in as: {_authService.CurrentUser?.Username ?? "unknown"}";

    public bool IsUsingDefaultPassword => _authService.IsUsingDefaultPassword;

    private string _pwChangeStatus = string.Empty;
    public string PwChangeStatus
    {
        get => _pwChangeStatus;
        set { SetProperty(ref _pwChangeStatus, value); OnPropertyChanged(nameof(HasPwChangeStatus)); }
    }

    private bool _pwChangeIsError;
    public bool PwChangeIsError
    {
        get => _pwChangeIsError;
        set => SetProperty(ref _pwChangeIsError, value);
    }

    public bool HasPwChangeStatus => !string.IsNullOrEmpty(_pwChangeStatus);

    // Called from code-behind (PasswordBox values cannot be bound in XAML).
    // Returns (success, message) so the code-behind can clear fields on success.
    public (bool Success, string Message) TryChangePassword(
        string current, string newPass, string confirm)
    {
        if (string.IsNullOrWhiteSpace(current))
            return (false, "Please enter your current password.");
        if (string.IsNullOrWhiteSpace(newPass))
            return (false, "Please enter a new password.");
        if (newPass.Length < 6)
            return (false, "New password must be at least 6 characters.");
        if (newPass != confirm)
            return (false, "The new passwords do not match. Please re-enter.");
        if (newPass == current)
            return (false, "New password must be different from the current password.");

        var ok = _authService.ChangePassword(current, newPass);
        if (ok) OnPropertyChanged(nameof(IsUsingDefaultPassword));
        return ok
            ? (true,  "Password changed successfully.")
            : (false, "Current password is incorrect. Please try again.");
    }

    // ── Backup reminder ───────────────────────────────────────────────────────
    private string? _backupReminderMessage;
    public string? BackupReminderMessage
    {
        get => _backupReminderMessage;
        private set { SetProperty(ref _backupReminderMessage, value); OnPropertyChanged(nameof(HasBackupReminder)); }
    }
    public bool HasBackupReminder => !string.IsNullOrEmpty(_backupReminderMessage);

    // ── Save status ───────────────────────────────────────────────────────────
    private string _saveStatusMessage = string.Empty;
    public string SaveStatusMessage
    {
        get => _saveStatusMessage;
        private set { SetProperty(ref _saveStatusMessage, value); OnPropertyChanged(nameof(HasSaveStatus)); }
    }

    private bool _saveStatusIsError;
    public bool SaveStatusIsError
    {
        get => _saveStatusIsError;
        private set => SetProperty(ref _saveStatusIsError, value);
    }

    public bool HasSaveStatus => !string.IsNullOrEmpty(_saveStatusMessage);

    // ── Commands ──────────────────────────────────────────────────────────────
    public RelayCommand SaveCommand              { get; }
    public RelayCommand BackCommand              { get; }

    // Browse commands are handled in codebehind; these are no-ops exposed so
    // XAML buttons can bind Click handlers without needing code-behind commands.
    // Actual dialogs are opened via Click events in SettingsView.xaml.cs.
    public RelayCommand BrowseLogoCommand          { get; }
    public RelayCommand BrowseReportsFolderCommand { get; }
    public RelayCommand BrowseBackupsFolderCommand { get; }

    public SettingsViewModel(
        SettingsService    settingsService,
        IAuthService       authService,
        INavigationService navigationService)
    {
        _settingsService   = settingsService;
        _authService       = authService;
        _navigationService = navigationService;

        SaveCommand              = new RelayCommand(ExecuteSave, _ => IsAdmin);
        BackCommand              = new RelayCommand(_ => _navigationService.NavigateTo<DashboardViewModel>());
        BrowseLogoCommand          = new RelayCommand(_ => { });
        BrowseReportsFolderCommand = new RelayCommand(_ => { });
        BrowseBackupsFolderCommand = new RelayCommand(_ => { });

        LoadSettings();
        BackupReminderMessage = _settingsService.GetBackupReminder();
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private void LoadSettings()
    {
        _loaded = _settingsService.Load();

        _centreName          = _loaded.CentreName;
        _centreAddress       = _loaded.CentreAddress;
        _centrePhone         = _loaded.CentrePhone;
        _centreEmail         = _loaded.CentreEmail;
        _logoPath            = _loaded.LogoPath          ?? string.Empty;
        _reportsRootPath     = _loaded.ReportsRootPath   ?? string.Empty;
        _backupsRootPath     = _loaded.BackupsRootPath   ?? string.Empty;
        _defaultPriceTier    = _loaded.DefaultPriceTier;
        _gstTaxId            = _loaded.GstTaxId;
        _signatureFooterText = _loaded.SignatureFooterText;
        _watermarkText       = _loaded.WatermarkText;

        OnPropertyChanged(nameof(CentreName));
        OnPropertyChanged(nameof(CentreAddress));
        OnPropertyChanged(nameof(CentrePhone));
        OnPropertyChanged(nameof(CentreEmail));
        OnPropertyChanged(nameof(LogoPath));
        OnPropertyChanged(nameof(ReportsRootPath));
        OnPropertyChanged(nameof(BackupsRootPath));
        OnPropertyChanged(nameof(DefaultPriceTier));
        OnPropertyChanged(nameof(GstTaxId));
        OnPropertyChanged(nameof(SignatureFooterText));
        OnPropertyChanged(nameof(WatermarkText));
        OnPropertyChanged(nameof(LastSavedDisplay));
    }

    private void ExecuteSave(object? _)
    {
        try
        {
            var s = new AppSettings
            {
                Id                  = 1,
                CentreName          = _centreName.Trim(),
                CentreAddress       = _centreAddress.Trim(),
                CentrePhone         = _centrePhone.Trim(),
                CentreEmail         = _centreEmail.Trim(),
                LogoPath            = string.IsNullOrWhiteSpace(_logoPath)          ? null : _logoPath.Trim(),
                ReportsRootPath     = string.IsNullOrWhiteSpace(_reportsRootPath)   ? null : _reportsRootPath.Trim(),
                BackupsRootPath     = string.IsNullOrWhiteSpace(_backupsRootPath)   ? null : _backupsRootPath.Trim(),
                DefaultPriceTier    = _defaultPriceTier.Trim(),
                GstTaxId            = _gstTaxId.Trim(),
                SignatureFooterText = _signatureFooterText.Trim(),
                WatermarkText       = _watermarkText.Trim()
            };

            _settingsService.Save(s);
            _loaded = s;

            SaveStatusIsError  = false;
            SaveStatusMessage  = "Settings saved successfully.";
            OnPropertyChanged(nameof(LastSavedDisplay));
        }
        catch (Exception ex)
        {
            SaveStatusIsError = true;
            SaveStatusMessage = $"Failed to save: {ex.Message}";
        }
    }
}
