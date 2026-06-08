using System.IO;
using System.Windows;
using UrbanDiagnosticCentre.Data;
using UrbanDiagnosticCentre.Helpers;
using UrbanDiagnosticCentre.Services;
using UrbanDiagnosticCentre.ViewModels;
using UrbanDiagnosticCentre.Views;

namespace UrbanDiagnosticCentre;

public partial class App : Application
{
    private static readonly string AppDataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "UrbanDiagnosticCentre");

    private InstallerPreparationService _installerService  = null!;
    private AppDbContext          _db                    = null!;
    private AuthService           _authService           = null!;
    private NavigationService     _navigationService     = null!;
    private ReportCodeService     _reportCodeService     = null!;
    private TestDefinitionService _testDefinitionService = null!;
    private SettingsService       _settingsService       = null!;
    private ReportService         _reportService         = null!;
    private PrintService          _printService          = null!;
    private BackupService         _backupService         = null!;
    private MaintenanceService    _maintenanceService    = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Register global exception handlers before anything else runs
        DispatcherUnhandledException += (_, args) =>
        {
            ErrorLoggingService.Log(args.Exception, "UI");
            MessageBox.Show(
                $"An unexpected error occurred.\n\nDetails have been saved to the log file at:\n{ErrorLoggingService.LogFolder}",
                "Unexpected Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                ErrorLoggingService.Log(ex, "AppDomain");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            ErrorLoggingService.Log(args.Exception, "Task");
            args.SetObserved();
        };

        ErrorLoggingService.LogInfo($"{AppInfo.AppName} v{AppInfo.Version} starting");

        // Ensure required folders exist before any I/O
        _installerService = new InstallerPreparationService();
        _installerService.EnsureFirstRunDirectories();

        // Purge log files older than 90 days
        ErrorLoggingService.PurgeOldLogs(retentionDays: 90);

        _db = new AppDbContext();
        _db.Database.Migrate();
        DatabaseSeeder.Seed(_db);

        _authService           = new AuthService(_db);
        _reportCodeService     = new ReportCodeService(_db);
        _testDefinitionService = new TestDefinitionService(_db);
        _settingsService       = new SettingsService(_db);
        _reportService         = new ReportService(_db, _settingsService);
        _printService          = new PrintService(_reportService);
        _backupService         = new BackupService(_db, _settingsService);
        _maintenanceService    = new MaintenanceService(_db, _settingsService);
        _navigationService     = new NavigationService(CreateViewModel);

        RunStartupDiagnostics();

        _navigationService.NavigateTo<LoginViewModel>();

        var mainVm     = new MainViewModel(_navigationService);
        var mainWindow = new MainWindow { DataContext = mainVm };
        mainWindow.Show();

        // Nudge admin to configure centre details on first run
        var settings = _settingsService.Load();
        if (string.IsNullOrWhiteSpace(settings.CentreName))
        {
            MessageBox.Show(
                "Welcome to Urban Diagnostic Centre!\n\n" +
                "This appears to be the first time the application has been run.\n\n" +
                "After signing in, please go to Settings to enter your centre name, " +
                "address, and other details. This information will appear on all generated PDF reports.",
                "First Run — Setup Required",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        ErrorLoggingService.LogInfo("Startup complete");
    }

    private BaseViewModel CreateViewModel(Type type)
    {
        if (type == typeof(LoginViewModel))
            return new LoginViewModel(_authService, _navigationService);

        if (type == typeof(DashboardViewModel))
            return new DashboardViewModel(_authService, _navigationService, _reportService, _printService, _settingsService);

        if (type == typeof(TestManagerViewModel))
            return new TestManagerViewModel(_testDefinitionService, _navigationService);

        if (type == typeof(PatientReportViewModel))
            return new PatientReportViewModel(
                _testDefinitionService, _reportService,
                _reportCodeService, _authService, _navigationService, _settingsService);

        if (type == typeof(ReportHistoryViewModel))
            return new ReportHistoryViewModel(_reportService, _printService, _navigationService);

        if (type == typeof(BackupViewModel))
            return new BackupViewModel(_backupService, _navigationService);

        if (type == typeof(SettingsViewModel))
            return new SettingsViewModel(_settingsService, _authService, _navigationService);

        if (type == typeof(MaintenanceViewModel))
            return new MaintenanceViewModel(_maintenanceService, _authService, _navigationService);

        throw new InvalidOperationException($"No factory registered for ViewModel: {type.Name}");
    }

    // ── Startup diagnostics ───────────────────────────────────────────────────

    private void RunStartupDiagnostics()
    {
        var issues = new List<string>();

        // 1. Database connectivity
        try
        {
            _db.Database.OpenConnection();
            _db.Database.CloseConnection();
        }
        catch (Exception ex)
        {
            ErrorLoggingService.Log(ex, "Startup.DB");
            issues.Add("Database connection failed. Data may not load correctly.");
        }

        // 2. Reports folder read/write
        var settings     = _settingsService.Load();
        var reportsRoot  = string.IsNullOrEmpty(settings.ReportsRootPath)
            ? Path.Combine(AppDataFolder, "Reports")
            : settings.ReportsRootPath;

        try
        {
            Directory.CreateDirectory(reportsRoot);
            var probe = Path.Combine(reportsRoot, $".startup_{Guid.NewGuid():N}");
            File.WriteAllText(probe, "x");
            File.Delete(probe);
        }
        catch (Exception ex)
        {
            ErrorLoggingService.Log(ex, "Startup.Reports");
            issues.Add($"Reports folder is not accessible: {reportsRoot}");
        }

        // 3. Backup folder accessibility (only if configured)
        if (!string.IsNullOrEmpty(settings.BackupsRootPath))
        {
            try   { Directory.CreateDirectory(settings.BackupsRootPath); }
            catch (Exception ex)
            {
                ErrorLoggingService.Log(ex, "Startup.Backups");
                issues.Add($"Backup folder is not accessible: {settings.BackupsRootPath}");
            }
        }

        if (issues.Count > 0)
        {
            var msg = $"Startup diagnostics found {issues.Count} issue(s):\n\n" +
                      string.Join("\n", issues.Select(i => $"  • {i}")) +
                      "\n\nSome features may not work correctly.\nContact your system administrator if the problem persists.";
            MessageBox.Show(msg, $"{AppInfo.AppName} — Startup Warning",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        ErrorLoggingService.LogInfo($"Startup diagnostics complete — {issues.Count} issue(s) found");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _db.Dispose();
        base.OnExit(e);
    }
}
