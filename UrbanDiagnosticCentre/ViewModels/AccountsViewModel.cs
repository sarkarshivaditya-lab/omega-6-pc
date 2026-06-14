using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using UrbanDiagnosticCentre.Helpers;
using UrbanDiagnosticCentre.Models;
using UrbanDiagnosticCentre.Services;

namespace UrbanDiagnosticCentre.ViewModels;

public class AccountsViewModel : BaseViewModel
{
    private readonly AccountingService  _accountingService;
    private readonly SettingsService    _settingsService;
    private readonly IAuthService       _authService;
    private readonly INavigationService _navigationService;

    // ── User / sidebar ────────────────────────────────────────────────────────
    public string WelcomeMessage => $"Welcome, {_authService.CurrentUser?.FullName ?? "User"}";
    public string UserRole       => _authService.CurrentUser?.Role ?? string.Empty;
    public string UserInitials   => GetInitials(_authService.CurrentUser?.FullName);
    public bool   IsAdmin        => _authService.CurrentUser?.Role == "Admin";

    // ── Active tab ────────────────────────────────────────────────────────────
    private string _activeTab = "Summary";
    public  string  ActiveTab
    {
        get => _activeTab;
        private set
        {
            SetProperty(ref _activeTab, value);
            OnPropertyChanged(nameof(IsSummaryTab));
            OnPropertyChanged(nameof(IsIncomeTab));
            OnPropertyChanged(nameof(IsExpensesTab));
            OnPropertyChanged(nameof(IsTransactionsTab));
            OnPropertyChanged(nameof(IsExportTab));
        }
    }
    public bool IsSummaryTab      => ActiveTab == "Summary";
    public bool IsIncomeTab       => ActiveTab == "Income";
    public bool IsExpensesTab     => ActiveTab == "Expenses";
    public bool IsTransactionsTab => ActiveTab == "Transactions";
    public bool IsExportTab       => ActiveTab == "Export";

    // ── Summary KPIs ──────────────────────────────────────────────────────────
    private decimal _todayIncome;
    public  decimal  TodayIncome   { get => _todayIncome;   private set => SetProperty(ref _todayIncome,   value); }
    private decimal _todayExpenses;
    public  decimal  TodayExpenses { get => _todayExpenses; private set => SetProperty(ref _todayExpenses, value); }
    private decimal _monthIncome;
    public  decimal  MonthIncome   { get => _monthIncome;   private set => SetProperty(ref _monthIncome,   value); }
    private decimal _monthExpenses;
    public  decimal  MonthExpenses { get => _monthExpenses; private set => SetProperty(ref _monthExpenses, value); }
    private decimal _netProfitLoss;
    public  decimal  NetProfitLoss { get => _netProfitLoss; private set { SetProperty(ref _netProfitLoss, value); OnPropertyChanged(nameof(IsNetPositive)); } }
    private int _totalTransactions;
    public  int  TotalTransactions { get => _totalTransactions; private set => SetProperty(ref _totalTransactions, value); }
    public bool IsNetPositive => NetProfitLoss >= 0;

    // ── Income tab ────────────────────────────────────────────────────────────
    public ObservableCollection<IncomeEntry> IncomeList { get; } = new();
    private DateTime _incomeFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _incomeTo   = DateTime.Today;
    public DateTime IncomeFrom { get => _incomeFrom; set { SetProperty(ref _incomeFrom, value); LoadIncome(); } }
    public DateTime IncomeTo   { get => _incomeTo;   set { SetProperty(ref _incomeTo,   value); LoadIncome(); } }

    // ── Expenses tab ─────────────────────────────────────────────────────────
    public ObservableCollection<FinancialTransaction> ExpenseList { get; } = new();

    private DateTime _expenseDate = DateTime.Today;
    public  DateTime  ExpenseDate { get => _expenseDate; set => SetProperty(ref _expenseDate, value); }

    private string _expenseCategory = "Miscellaneous";
    public  string  ExpenseCategory { get => _expenseCategory; set => SetProperty(ref _expenseCategory, value); }

    private string _expenseDescription = string.Empty;
    public  string  ExpenseDescription { get => _expenseDescription; set { SetProperty(ref _expenseDescription, value); AddExpenseCommand.RaiseCanExecuteChanged(); } }

    private string _expenseAmountText = string.Empty;
    public  string  ExpenseAmountText  { get => _expenseAmountText;  set { SetProperty(ref _expenseAmountText, value); AddExpenseCommand.RaiseCanExecuteChanged(); } }

    private string _expenseNotes = string.Empty;
    public  string  ExpenseNotes  { get => _expenseNotes;  set => SetProperty(ref _expenseNotes, value); }

    private string? _expenseError;
    public  string?  ExpenseError { get => _expenseError; private set { SetProperty(ref _expenseError, value); OnPropertyChanged(nameof(HasExpenseError)); } }
    public bool HasExpenseError => !string.IsNullOrEmpty(_expenseError);

    public IReadOnlyList<string> Categories => AccountingService.BuiltInCategories;

    private DateTime _expensesFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _expensesTo   = DateTime.Today;
    public DateTime ExpensesFrom { get => _expensesFrom; set { SetProperty(ref _expensesFrom, value); LoadExpenses(); } }
    public DateTime ExpensesTo   { get => _expensesTo;   set { SetProperty(ref _expensesTo,   value); LoadExpenses(); } }

    // ── Transactions tab ─────────────────────────────────────────────────────
    public ObservableCollection<LedgerRow> LedgerList { get; } = new();
    private DateTime _ledgerFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _ledgerTo   = DateTime.Today;
    public DateTime LedgerFrom { get => _ledgerFrom; set { SetProperty(ref _ledgerFrom, value); LoadLedger(); } }
    public DateTime LedgerTo   { get => _ledgerTo;   set { SetProperty(ref _ledgerTo,   value); LoadLedger(); } }

    private string _ledgerTypeFilter = "All";
    public  string  LedgerTypeFilter { get => _ledgerTypeFilter; set { SetProperty(ref _ledgerTypeFilter, value); LoadLedger(); } }
    public IReadOnlyList<string> TypeFilters => ["All", "Income", "Expense"];

    // ── Export tab ────────────────────────────────────────────────────────────
    public IReadOnlyList<string> ExportRanges =>
        ["Today", "This Week", "This Month", "Last Month", "This Year", "Custom"];

    private string _exportRange = "This Month";
    public  string  ExportRange
    {
        get => _exportRange;
        set
        {
            SetProperty(ref _exportRange, value);
            ApplyExportRange();
            OnPropertyChanged(nameof(IsCustomExportRange));
        }
    }
    public bool IsCustomExportRange => _exportRange == "Custom";

    private DateTime _exportFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _exportTo   = DateTime.Today;
    public DateTime ExportFrom { get => _exportFrom; set => SetProperty(ref _exportFrom, value); }
    public DateTime ExportTo   { get => _exportTo;   set => SetProperty(ref _exportTo,   value); }

    private bool _isExporting;
    public  bool  IsExporting { get => _isExporting; private set => SetProperty(ref _isExporting, value); }

    // ── Commands ──────────────────────────────────────────────────────────────
    public RelayCommand LogoutCommand                { get; }
    public RelayCommand NavigateToDashboardCommand   { get; }
    public RelayCommand NavigateToPatientsCommand    { get; }
    public RelayCommand NavigateToTestManagerCommand { get; }
    public RelayCommand NavigateToBackupCommand      { get; }
    public RelayCommand NavigateToSettingsCommand    { get; }
    public RelayCommand NavigateToMaintenanceCommand { get; }

    public RelayCommand ShowSummaryCommand      { get; }
    public RelayCommand ShowIncomeCommand       { get; }
    public RelayCommand ShowExpensesCommand     { get; }
    public RelayCommand ShowTransactionsCommand { get; }
    public RelayCommand ShowExportCommand       { get; }

    public RelayCommand AddExpenseCommand    { get; }
    public RelayCommand DeleteExpenseCommand { get; }
    public RelayCommand ExportPdfCommand     { get; }

    // ── Constructor ───────────────────────────────────────────────────────────
    public AccountsViewModel(
        AccountingService  accountingService,
        SettingsService    settingsService,
        IAuthService       authService,
        INavigationService navigationService)
    {
        _accountingService = accountingService;
        _settingsService   = settingsService;
        _authService       = authService;
        _navigationService = navigationService;

        LogoutCommand                = new RelayCommand(_ => { _authService.Logout(); _navigationService.NavigateTo<LoginViewModel>(); });
        NavigateToDashboardCommand   = new RelayCommand(_ => _navigationService.NavigateTo<DashboardViewModel>());
        NavigateToPatientsCommand    = new RelayCommand(_ => _navigationService.NavigateTo<ReportHistoryViewModel>());
        NavigateToTestManagerCommand = new RelayCommand(_ => _navigationService.NavigateTo<TestManagerViewModel>());
        NavigateToBackupCommand      = new RelayCommand(_ => _navigationService.NavigateTo<BackupViewModel>());
        NavigateToSettingsCommand    = new RelayCommand(_ => _navigationService.NavigateTo<SettingsViewModel>());
        NavigateToMaintenanceCommand = new RelayCommand(_ => _navigationService.NavigateTo<MaintenanceViewModel>());

        ShowSummaryCommand      = new RelayCommand(_ => { ActiveTab = "Summary";      LoadSummary(); });
        ShowIncomeCommand       = new RelayCommand(_ => { ActiveTab = "Income";       LoadIncome(); });
        ShowExpensesCommand     = new RelayCommand(_ => { ActiveTab = "Expenses";     LoadExpenses(); });
        ShowTransactionsCommand = new RelayCommand(_ => { ActiveTab = "Transactions"; LoadLedger(); });
        ShowExportCommand       = new RelayCommand(_ => { ActiveTab = "Export";       ApplyExportRange(); });

        AddExpenseCommand    = new RelayCommand(ExecuteAddExpense,    _ => CanAddExpense());
        DeleteExpenseCommand = new RelayCommand(ExecuteDeleteExpense);
        ExportPdfCommand     = new RelayCommand(ExecuteExportPdf,     _ => !IsExporting);

        LoadSummary();
    }

    // ── Data loading ─────────────────────────────────────────────────────────

    private void LoadSummary()
    {
        var s = _accountingService.GetSummary();
        TodayIncome       = s.TodayIncome;
        TodayExpenses     = s.TodayExpenses;
        MonthIncome       = s.MonthIncome;
        MonthExpenses     = s.MonthExpenses;
        NetProfitLoss     = s.NetProfitLoss;
        TotalTransactions = s.TotalTransactions;
    }

    private void LoadIncome()
    {
        IncomeList.Clear();
        foreach (var e in _accountingService.GetIncomeEntries(_incomeFrom, _incomeTo))
            IncomeList.Add(e);
    }

    private void LoadExpenses()
    {
        ExpenseList.Clear();
        foreach (var e in _accountingService.GetExpenses(_expensesFrom, _expensesTo))
            ExpenseList.Add(e);
    }

    private void LoadLedger()
    {
        LedgerList.Clear();
        var filter = _ledgerTypeFilter == "All" ? null : _ledgerTypeFilter;
        foreach (var r in _accountingService.GetLedger(_ledgerFrom, _ledgerTo, filter))
            LedgerList.Add(r);
    }

    // ── Expense operations ────────────────────────────────────────────────────

    private bool CanAddExpense()
        => !string.IsNullOrWhiteSpace(_expenseDescription) &&
           decimal.TryParse(_expenseAmountText, out var v) && v > 0;

    private void ExecuteAddExpense(object? _)
    {
        ExpenseError = null;
        if (!decimal.TryParse(_expenseAmountText, out var amount) || amount <= 0)
        {
            ExpenseError = "Please enter a valid amount greater than zero.";
            return;
        }
        if (string.IsNullOrWhiteSpace(_expenseDescription))
        {
            ExpenseError = "Description is required.";
            return;
        }

        _accountingService.AddExpense(new FinancialTransaction
        {
            TransactionDate = _expenseDate,
            Category        = _expenseCategory,
            Description     = _expenseDescription.Trim(),
            Amount          = amount,
            Notes           = string.IsNullOrWhiteSpace(_expenseNotes) ? null : _expenseNotes.Trim(),
            CreatedByUserId = _authService.CurrentUser?.Id
        });

        // Reset form
        ExpenseDescription = string.Empty;
        ExpenseAmountText  = string.Empty;
        ExpenseNotes       = string.Empty;
        ExpenseDate        = DateTime.Today;
        ExpenseCategory    = "Miscellaneous";

        LoadExpenses();
        LoadSummary();
    }

    private void ExecuteDeleteExpense(object? parameter)
    {
        if (parameter is not FinancialTransaction tx) return;
        var confirm = MessageBox.Show(
            $"Delete expense \"{tx.Description}\" (₹ {tx.Amount:N2})?",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;
        _accountingService.DeleteExpense(tx.Id);
        LoadExpenses();
        LoadSummary();
    }

    // ── PDF export ────────────────────────────────────────────────────────────

    private void ApplyExportRange()
    {
        var today = DateTime.Today;
        (ExportFrom, ExportTo) = _exportRange switch
        {
            "Today"      => (today, today),
            "This Week"  => (today.AddDays(-(int)today.DayOfWeek), today),
            "This Month" => (new DateTime(today.Year, today.Month, 1), today),
            "Last Month" => (new DateTime(today.Year, today.Month, 1).AddMonths(-1),
                             new DateTime(today.Year, today.Month, 1).AddDays(-1)),
            "This Year"  => (new DateTime(today.Year, 1, 1), today),
            _            => (ExportFrom, ExportTo)
        };
    }

    private void ExecuteExportPdf(object? _)
    {
        IsExporting = true;
        ExportPdfCommand.RaiseCanExecuteChanged();
        try
        {
            var settings = _settingsService.Load();
            var (income, expenses) = _accountingService.GetForExport(_exportFrom, _exportTo);
            var path = FinancialPdfService.Generate(_exportFrom, _exportTo, income, expenses, settings);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsExporting = false;
            ExportPdfCommand.RaiseCanExecuteChanged();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "?";
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? $"{parts[0][0]}{parts[1][0]}" : $"{parts[0][0]}";
    }
}
