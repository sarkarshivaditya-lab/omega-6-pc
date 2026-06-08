using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using UrbanDiagnosticCentre.Helpers;
using UrbanDiagnosticCentre.Models;
using UrbanDiagnosticCentre.Services;

namespace UrbanDiagnosticCentre.ViewModels;

public class BackupViewModel : BaseViewModel
{
    private readonly BackupService     _backupService;
    private readonly INavigationService _navigationService;

    // ── Destination folder ────────────────────────────────────────────────────
    private string _backupDestinationFolder;
    public string BackupDestinationFolder
    {
        get => _backupDestinationFolder;
        set { SetProperty(ref _backupDestinationFolder, value); UpdateDestinationDiskWarning(); }
    }

    // ── Destination disk space warning ────────────────────────────────────────
    private string? _destinationDiskWarning;
    public string? DestinationDiskWarning
    {
        get => _destinationDiskWarning;
        private set { SetProperty(ref _destinationDiskWarning, value); OnPropertyChanged(nameof(HasDestinationDiskWarning)); }
    }
    public bool HasDestinationDiskWarning => !string.IsNullOrEmpty(_destinationDiskWarning);

    // ── Restore source path ───────────────────────────────────────────────────
    private string _restoreSourcePath = string.Empty;
    public string RestoreSourcePath
    {
        get => _restoreSourcePath;
        set => SetProperty(ref _restoreSourcePath, value);
    }

    // ── Status ────────────────────────────────────────────────────────────────
    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        private set { SetProperty(ref _statusMessage, value); OnPropertyChanged(nameof(HasStatusMessage)); }
    }

    private bool _statusIsError;
    public bool StatusIsError
    {
        get => _statusIsError;
        private set => SetProperty(ref _statusIsError, value);
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(_statusMessage);

    private bool _isWorking;
    public bool IsWorking
    {
        get => _isWorking;
        private set => SetProperty(ref _isWorking, value);
    }

    // ── Recent backups ────────────────────────────────────────────────────────
    public ObservableCollection<BackupRecord> RecentBackups { get; } = new();
    public bool HasNoRecentBackups => RecentBackups.Count == 0;

    // ── Commands ──────────────────────────────────────────────────────────────
    public RelayCommand CreateBackupCommand    { get; }
    public RelayCommand RestoreBackupCommand   { get; }
    public RelayCommand OpenBackupFolderCommand { get; }
    public RelayCommand BackCommand            { get; }

    public BackupViewModel(BackupService backupService, INavigationService navigationService)
    {
        _backupService      = backupService;
        _navigationService  = navigationService;
        _backupDestinationFolder = _backupService.GetDefaultBackupFolder();

        CreateBackupCommand     = new RelayCommand(async _ => await ExecuteCreateBackupAsync());
        RestoreBackupCommand    = new RelayCommand(async _ => await ExecuteRestoreAsync());
        OpenBackupFolderCommand = new RelayCommand(ExecuteOpenFolder);
        BackCommand             = new RelayCommand(_ => _navigationService.NavigateTo<DashboardViewModel>());

        RefreshRecentBackups();
    }

    // ── Command handlers ──────────────────────────────────────────────────────

    private async Task ExecuteCreateBackupAsync()
    {
        if (string.IsNullOrWhiteSpace(_backupDestinationFolder))
        {
            SetStatus("Please select a destination folder first.", isError: true);
            return;
        }

        IsWorking = true;
        SetStatus("Creating backup…", isError: false);

        try
        {
            var path = await Task.Run(() =>
                _backupService.CreateBackup(_backupDestinationFolder));

            RefreshRecentBackups();
            SetStatus($"Backup created successfully: {path}", isError: false);
        }
        catch (FileNotFoundException)
        {
            SetStatus("Destination folder not found. Connect the drive and try again.", isError: true);
        }
        catch (UnauthorizedAccessException)
        {
            SetStatus("Access denied. Check that you have write permission on the selected folder.", isError: true);
        }
        catch (IOException ex) when ((ex.HResult & 0xFFFF) == 112)
        {
            SetStatus("Not enough disk space on the selected drive. Free up space and try again.", isError: true);
        }
        catch (IOException ex)
        {
            SetStatus($"Could not write backup: {ex.Message}", isError: true);
        }
        catch (Exception ex)
        {
            SetStatus($"Backup failed: {ex.Message}", isError: true);
        }
        finally
        {
            IsWorking = false;
        }
    }

    private async Task ExecuteRestoreAsync()
    {
        if (string.IsNullOrWhiteSpace(_restoreSourcePath))
        {
            SetStatus("Please select a backup ZIP file first.", isError: true);
            return;
        }

        IsWorking = true;
        SetStatus("Creating safety snapshot…", isError: false);

        try
        {
            var safetyFolder = _backupDestinationFolder;
            string safetyPath = string.Empty;

            await Task.Run(() =>
            {
                safetyPath = _backupService.RestoreBackup(_restoreSourcePath, safetyFolder);
            });

            SetStatus($"Restore complete. Safety backup saved to: {safetyPath}\nRestarting application…",
                      isError: false);
            await Task.Delay(1800);

            // Launch a fresh instance and exit this one
            var exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exe))
                Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });

            Application.Current.Shutdown();
        }
        catch (FileNotFoundException)
        {
            SetStatus("Backup file not found. Connect the drive and try again.", isError: true);
        }
        catch (InvalidDataException ex)
        {
            SetStatus(ex.Message, isError: true);
        }
        catch (UnauthorizedAccessException)
        {
            SetStatus("Access denied. Check permissions on the backup file and destination.", isError: true);
        }
        catch (IOException ex) when ((ex.HResult & 0xFFFF) == 112)
        {
            SetStatus("Not enough disk space to complete the restore.", isError: true);
        }
        catch (IOException ex)
        {
            SetStatus($"File error during restore: {ex.Message}", isError: true);
        }
        catch (Exception ex)
        {
            SetStatus($"Restore failed: {ex.Message}", isError: true);
        }
        finally
        {
            IsWorking = false;
        }
    }

    private void ExecuteOpenFolder(object? _)
    {
        var folder = string.IsNullOrWhiteSpace(_backupDestinationFolder)
            ? _backupService.GetDefaultBackupFolder()
            : _backupDestinationFolder;
        try
        {
            _backupService.OpenFolder(folder);
        }
        catch
        {
            SetStatus("Could not open the folder.", isError: true);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void UpdateDestinationDiskWarning()
    {
        DestinationDiskWarning = null;
        if (string.IsNullOrWhiteSpace(_backupDestinationFolder)) return;
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(_backupDestinationFolder));
            if (root is null) return;
            var drive = new DriveInfo(root);
            if (drive.IsReady && drive.AvailableFreeSpace < 500L * 1_048_576L)
                DestinationDiskWarning =
                    $"Destination drive is low on space: {drive.AvailableFreeSpace / 1_048_576} MB remaining.";
        }
        catch { /* non-fatal */ }
    }

    private void RefreshRecentBackups()
    {
        RecentBackups.Clear();
        foreach (var r in _backupService.GetRecentBackups())
            RecentBackups.Add(r);
        OnPropertyChanged(nameof(HasNoRecentBackups));
    }

    private void SetStatus(string message, bool isError)
    {
        StatusIsError  = isError;
        StatusMessage  = message;
    }
}
