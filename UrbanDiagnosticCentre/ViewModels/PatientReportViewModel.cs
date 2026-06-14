using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using UrbanDiagnosticCentre.Helpers;
using UrbanDiagnosticCentre.Models;
using UrbanDiagnosticCentre.Services;

namespace UrbanDiagnosticCentre.ViewModels;

// ── Per-test row in the selected entries table ────────────────────────────────
public class ReportEntryRow : BaseViewModel
{
    public TestDefinition TestDef { get; }
    private decimal _refMin;
    private decimal _refMax;

    private string _resultText = string.Empty;
    public string ResultText
    {
        get => _resultText;
        set
        {
            SetProperty(ref _resultText, value);
            RecalculateFlag();
        }
    }

    public ResultFlag Flag { get; private set; } = ResultFlag.Unknown;
    public string RangeDisplay { get; private set; } = string.Empty;

    public string FlagDisplay => Flag switch
    {
        ResultFlag.Normal => "Normal",
        ResultFlag.Low    => "↓ Low",
        ResultFlag.High   => "↑ High",
        _                 => "—"
    };

    public string FlagColor => Flag switch
    {
        ResultFlag.Normal => "#2E7D32",
        ResultFlag.Low    => "#1565C0",
        ResultFlag.High   => "#C62828",
        _                 => "#9E9E9E"
    };

    public string FlagBackground => Flag switch
    {
        ResultFlag.Normal => "#E8F5E9",
        ResultFlag.Low    => "#E3F2FD",
        ResultFlag.High   => "#FFEBEE",
        _                 => "#F5F5F5"
    };

    // ── Price ─────────────────────────────────────────────────────────────────

    private decimal? _chargedPrice;
    private bool     _isPriceOverridden;
    private string   _priceEditText = string.Empty;

    // Set by the VM when the tier changes; also updated when the user types a custom price.
    public decimal? ChargedPrice
    {
        get => _chargedPrice;
        set
        {
            if (_chargedPrice == value) return;
            _chargedPrice = value;
            OnPropertyChanged(nameof(ChargedPrice));
            OnPropertyChanged(nameof(PriceDisplay));
            // Keep the edit box in sync when the value originates from the tier (not a manual override).
            if (!_isPriceOverridden)
            {
                _priceEditText = value.HasValue
                    ? value.Value.ToString("F2", CultureInfo.InvariantCulture)
                    : string.Empty;
                OnPropertyChanged(nameof(PriceEditText));
            }
        }
    }

    public string PriceDisplay => _chargedPrice.HasValue ? _chargedPrice.Value.ToString("F2") : "—";

    // Two-way binding target for the inline price TextBox.
    // Typing a valid non-negative decimal sets a manual override.
    // Clearing the field removes the override and lets the VM re-apply tier pricing.
    public string PriceEditText
    {
        get => _priceEditText;
        set
        {
            if (_priceEditText == value) return;
            _priceEditText = value;
            OnPropertyChanged(nameof(PriceEditText));
            ApplyPriceEdit(value);
        }
    }

    public bool IsPriceOverridden => _isPriceOverridden;

    // True when this entry was inserted by a package Apply; false for manually-added tests.
    // Package tests are covered by PackageTotalPrice and must never receive tier/manual pricing.
    public bool IsFromPackage { get; private set; } = false;

    public void MarkAsPackage()
    {
        IsFromPackage      = true;
        _isPriceOverridden = false;
        _chargedPrice      = null;
        _priceEditText     = string.Empty;
        OnPropertyChanged(nameof(ChargedPrice));
        OnPropertyChanged(nameof(PriceDisplay));
        OnPropertyChanged(nameof(PriceEditText));
        OnPropertyChanged(nameof(IsPriceOverridden));
    }

    // Demotes a package entry back to a manual entry (used by ClearPackageBilling).
    // Clears override flag so RefreshEntryPrices can apply tier pricing.
    public void ClearPackageMembership()
    {
        IsFromPackage      = false;
        _isPriceOverridden = false;
        OnPropertyChanged(nameof(IsPriceOverridden));
    }

    // Called by the VM's ResetPriceCommand; also triggered when the user clears the field.
    // The VM's OnEntryPropertyChanged will re-apply the current tier price automatically.
    public void ClearOverride()
    {
        _isPriceOverridden = false;
        OnPropertyChanged(nameof(IsPriceOverridden));
    }

    private void ApplyPriceEdit(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _chargedPrice      = null;
            _isPriceOverridden = false;
            OnPropertyChanged(nameof(ChargedPrice));
            OnPropertyChanged(nameof(PriceDisplay));
            OnPropertyChanged(nameof(IsPriceOverridden));
            return;
        }

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var amt) && amt >= 0)
        {
            _chargedPrice      = amt;
            _isPriceOverridden = true;
            OnPropertyChanged(nameof(ChargedPrice));
            OnPropertyChanged(nameof(PriceDisplay));
            OnPropertyChanged(nameof(IsPriceOverridden));
        }
        // Invalid text: ChargedPrice unchanged until the user corrects the input.
    }

    // ── Construction and range helpers ────────────────────────────────────────

    public ReportEntryRow(TestDefinition td, decimal refMin, decimal refMax)
    {
        TestDef = td;
        _refMin = refMin;
        _refMax = refMax;
        UpdateRangeDisplay();
    }

    public void UpdateRange(decimal min, decimal max)
    {
        _refMin = min;
        _refMax = max;
        UpdateRangeDisplay();
        RecalculateFlag();
        OnPropertyChanged(nameof(RangeDisplay));
    }

    private void UpdateRangeDisplay()
    {
        var fmt = $"F{TestDef.DecimalPrecision}";
        RangeDisplay = $"{_refMin.ToString(fmt)} – {_refMax.ToString(fmt)} {TestDef.Unit}";
    }

    private void RecalculateFlag()
    {
        if (!decimal.TryParse(_resultText, out var result))
            Flag = ResultFlag.Unknown;
        else
            Flag = result < _refMin ? ResultFlag.Low
                 : result > _refMax ? ResultFlag.High
                 : ResultFlag.Normal;

        OnPropertyChanged(nameof(Flag));
        OnPropertyChanged(nameof(FlagDisplay));
        OnPropertyChanged(nameof(FlagColor));
        OnPropertyChanged(nameof(FlagBackground));
    }
}

// ── Main ViewModel ────────────────────────────────────────────────────────────
public class PatientReportViewModel : BaseViewModel
{
    private readonly TestDefinitionService _testService;
    private readonly TestPackageService    _packageService;
    private readonly ReportService _reportService;
    private readonly IReportCodeService _codeService;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly SettingsService _settingsService;

    private List<TestDefinition> _allTests = new();
    private readonly string _reportCode;
    private DateTime _testDate  = DateTime.Now;
    private DateTime _reportDate = DateTime.Now;

    private readonly int? _editReportId;   // null = new report; non-null = editing existing draft

    // ── Patient fields ────────────────────────────────────────────────────────
    private string _formName = string.Empty;
    public string FormName { get => _formName; set => SetProperty(ref _formName, value); }

    private string _formAge = string.Empty;
    public string FormAge
    {
        get => _formAge;
        set { SetProperty(ref _formAge, value); RefreshEntryRanges(); }
    }

    private string _formPhone = string.Empty;
    public string FormPhone { get => _formPhone; set => SetProperty(ref _formPhone, value); }

    private string _formReferringDoctor = string.Empty;
    public string FormReferringDoctor
    {
        get => _formReferringDoctor;
        set => SetProperty(ref _formReferringDoctor, value);
    }

    private string _formAddress = string.Empty;
    public string FormAddress { get => _formAddress; set => SetProperty(ref _formAddress, value); }

    // ── Gender ────────────────────────────────────────────────────────────────
    private Gender _gender = Gender.Male;

    public bool GenderIsMale
    {
        get => _gender == Gender.Male;
        set
        {
            if (!value) return;
            _gender = Gender.Male;
            RefreshEntryRanges();
            OnPropertyChanged(nameof(GenderIsMale));
            OnPropertyChanged(nameof(GenderIsFemale));
        }
    }

    public bool GenderIsFemale
    {
        get => _gender == Gender.Female;
        set
        {
            if (!value) return;
            _gender = Gender.Female;
            RefreshEntryRanges();
            OnPropertyChanged(nameof(GenderIsMale));
            OnPropertyChanged(nameof(GenderIsFemale));
        }
    }

    // ── Auto-generated display ────────────────────────────────────────────────
    public string ReportCodeDisplay  => _reportCode;
    public string TestDateDisplay    => _testDate.ToString("dd MMM yyyy, hh:mm tt");
    public string ReportDateDisplay  => _reportDate.ToString("dd MMM yyyy, hh:mm tt");
    public bool   IsEditMode         => _editReportId.HasValue;
    public string PageTitle          => IsEditMode ? "Edit Draft" : "New Report";

    // ── Test selection ────────────────────────────────────────────────────────
    public ObservableCollection<string>           Categories        { get; } = new();
    public ObservableCollection<TestDefinition>   AvailableTests    { get; } = new();
    public ObservableCollection<ReportEntryRow>   SelectedEntries   { get; } = new();

    private string? _selectedCategory;
    public string? SelectedCategory
    {
        get => _selectedCategory;
        set { SetProperty(ref _selectedCategory, value); RefreshAvailableTests(); }
    }

    private TestDefinition? _selectedAvailableTest;
    public TestDefinition? SelectedAvailableTest
    {
        get => _selectedAvailableTest;
        set => SetProperty(ref _selectedAvailableTest, value);
    }

    private ReportEntryRow? _selectedEntry;
    public ReportEntryRow? SelectedEntry
    {
        get => _selectedEntry;
        set => SetProperty(ref _selectedEntry, value);
    }

    public bool HasNoSelectedEntries => SelectedEntries.Count == 0;

    // ── Pricing tier selection ────────────────────────────────────────────────
    public ObservableCollection<string> AvailableTiers { get; } = new();

    private string? _selectedTier;
    public string? SelectedTier
    {
        get => _selectedTier;
        set
        {
            if (SetProperty(ref _selectedTier, value))
                RefreshEntryPrices();
        }
    }

    // ── Package quick-add ─────────────────────────────────────────────────────
    public ObservableCollection<TestPackage> AvailablePackages { get; } = new();

    private TestPackage? _selectedPackage;
    public TestPackage? SelectedPackage
    {
        get => _selectedPackage;
        set => SetProperty(ref _selectedPackage, value);
    }

    // ── Package billing mode ──────────────────────────────────────────────────
    private BillingMode _billingMode        = BillingMode.Normal;
    private string?     _packageNameSnapshot;
    private decimal?    _packageTotalPrice;

    public BillingMode BillingMode
    {
        get => _billingMode;
        private set
        {
            if (SetProperty(ref _billingMode, value))
            {
                OnPropertyChanged(nameof(IsPackageBilled));
                OnPropertyChanged(nameof(PackageBillingDisplay));
                OnPropertyChanged(nameof(TotalCharge));
                OnPropertyChanged(nameof(TotalChargeDisplay));
            }
        }
    }

    public bool IsPackageBilled => _billingMode == BillingMode.Package;

    public string PackageBillingDisplay =>
        _packageNameSnapshot is not null && _packageTotalPrice.HasValue
            ? $"{_packageNameSnapshot}  —  ₹{_packageTotalPrice.Value:F2}"
            : "Package applied";

    // Grand total: in package mode = PackageTotalPrice + Sum(manual test prices);
    //              in normal mode  = Sum(all entry prices).
    public decimal? TotalCharge
    {
        get
        {
            if (_billingMode == BillingMode.Package)
            {
                if (!_packageTotalPrice.HasValue) return null;
                var manualTotal = SelectedEntries
                    .Where(e => !e.IsFromPackage && e.ChargedPrice.HasValue)
                    .Sum(e => e.ChargedPrice!.Value);
                return _packageTotalPrice.Value + manualTotal;
            }
            if (SelectedEntries.All(e => e.ChargedPrice is null)) return null;
            return SelectedEntries.Sum(e => e.ChargedPrice ?? 0m);
        }
    }

    public string TotalChargeDisplay => TotalCharge.HasValue
        ? TotalCharge.Value.ToString("F2")
        : "—";

    // ── Validation & saving state ─────────────────────────────────────────────
    private string _formError = string.Empty;
    public string FormError { get => _formError; set => SetProperty(ref _formError, value); }

    private bool _isSaving;
    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            SetProperty(ref _isSaving, value);
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }

    public string SavingMessage => "Generating PDF report, please wait...";

    // ── Commands ──────────────────────────────────────────────────────────────
    public RelayCommand AddTestCommand             { get; }
    public RelayCommand RemoveTestCommand          { get; }
    public RelayCommand SaveDraftCommand           { get; }
    public RelayCommand CompleteReportCommand      { get; }
    public RelayCommand NavigateToDashboardCommand { get; }
    public RelayCommand ResetPriceCommand          { get; }
    public RelayCommand ApplyPackageCommand           { get; }
    public RelayCommand ClearPackageBillingCommand    { get; }

    public PatientReportViewModel(
        TestDefinitionService testService,
        TestPackageService    packageService,
        ReportService reportService,
        IReportCodeService codeService,
        IAuthService authService,
        INavigationService navigationService,
        SettingsService settingsService,
        int? editReportId = null)
    {
        _testService       = testService;
        _packageService    = packageService;
        _reportService     = reportService;
        _codeService       = codeService;
        _authService       = authService;
        _navigationService = navigationService;
        _settingsService   = settingsService;
        _editReportId      = editReportId;

        if (editReportId.HasValue)
        {
            var existing = _reportService.GetById(editReportId.Value)
                ?? throw new InvalidOperationException($"Draft report {editReportId} not found.");
            _reportCode  = existing.ReportCode;
            _testDate    = existing.TestDate;
            _reportDate  = existing.ReportDate;
        }
        else
        {
            _reportCode = _codeService.Generate();
        }

        AddTestCommand             = new RelayCommand(ExecuteAddTest,                                        _ => !IsSaving);
        RemoveTestCommand          = new RelayCommand(ExecuteRemoveTest,                                      _ => !IsSaving);
        SaveDraftCommand           = new RelayCommand(async _ => await TrySaveAsync(ReportStatus.Draft),      _ => !IsSaving);
        CompleteReportCommand      = new RelayCommand(async _ => await TrySaveAsync(ReportStatus.Completed),  _ => !IsSaving);
        NavigateToDashboardCommand = new RelayCommand(_ => _navigationService.NavigateTo<DashboardViewModel>(), _ => !IsSaving);
        ResetPriceCommand          = new RelayCommand(ExecuteResetPrice,                                      _ => !IsSaving);
        ApplyPackageCommand           = new RelayCommand(ExecuteApplyPackage,        _ => !IsSaving);
        ClearPackageBillingCommand    = new RelayCommand(ExecuteClearPackageBilling, _ => !IsSaving);

        SelectedEntries.CollectionChanged += OnEntriesChanged;

        LoadTests();
        LoadTiers();
        LoadPackages();

        if (editReportId.HasValue)
            LoadExistingReport(editReportId.Value);
    }

    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (ReportEntryRow row in e.NewItems)
                row.PropertyChanged += OnEntryPropertyChanged;

        if (e.OldItems is not null)
            foreach (ReportEntryRow row in e.OldItems)
                row.PropertyChanged -= OnEntryPropertyChanged;

        OnPropertyChanged(nameof(HasNoSelectedEntries));
        OnPropertyChanged(nameof(TotalCharge));
        OnPropertyChanged(nameof(TotalChargeDisplay));
    }

    private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ReportEntryRow.ChargedPrice))
        {
            OnPropertyChanged(nameof(TotalCharge));
            OnPropertyChanged(nameof(TotalChargeDisplay));
        }
        else if (e.PropertyName == nameof(ReportEntryRow.IsPriceOverridden) &&
                 sender is ReportEntryRow row && !row.IsPriceOverridden &&
                 !row.IsFromPackage)
        {
            // Override cleared on a manual entry — re-apply current tier price.
            // Package-member entries never receive tier pricing.
            row.ChargedPrice = _selectedTier is not null
                ? _testService.GetPrice(row.TestDef.Id, _selectedTier)
                : null;
        }
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    private void LoadTiers()
    {
        var tiers = _testService.GetDistinctActiveTiers();
        AvailableTiers.Clear();
        foreach (var t in tiers)
            AvailableTiers.Add(t);

        // Stored value is already canonical (normalized by SettingsService.Save); direct equality is correct.
        var defaultTier = _settingsService.Load().DefaultPriceTier;
        _selectedTier = tiers.FirstOrDefault(t => t == defaultTier)
                      ?? (tiers.Count > 0 ? tiers[0] : null);
        OnPropertyChanged(nameof(SelectedTier));
        RefreshEntryPrices();
    }

    private void RefreshEntryPrices()
    {
        foreach (var row in SelectedEntries)
        {
            if (row.IsFromPackage) continue;    // package tests are never tier-priced
            if (row.IsPriceOverridden) continue;
            row.ChargedPrice = _selectedTier is not null
                ? _testService.GetPrice(row.TestDef.Id, _selectedTier)
                : null;
        }
        OnPropertyChanged(nameof(TotalCharge));
        OnPropertyChanged(nameof(TotalChargeDisplay));
    }

    private void LoadTests()
    {
        _allTests = _testService.GetAll();

        Categories.Clear();
        foreach (var cat in _allTests.Select(t => t.Category).Distinct().OrderBy(c => c))
            Categories.Add(cat);

        if (Categories.Count > 0)
            SelectedCategory = Categories[0];
    }

    private void LoadExistingReport(int reportId)
    {
        var report = _reportService.GetById(reportId);
        if (report is null) return;

        // Patient fields
        _formName            = report.Patient.FullName;
        _formAge             = report.Patient.Age.ToString();
        _formPhone           = report.Patient.PhoneNumber;
        _formReferringDoctor = report.Patient.ReferringDoctor ?? string.Empty;
        _formAddress         = report.Patient.Address ?? string.Empty;
        _gender              = report.Patient.Gender;

        OnPropertyChanged(nameof(FormName));
        OnPropertyChanged(nameof(FormAge));
        OnPropertyChanged(nameof(FormPhone));
        OnPropertyChanged(nameof(FormReferringDoctor));
        OnPropertyChanged(nameof(FormAddress));
        OnPropertyChanged(nameof(GenderIsMale));
        OnPropertyChanged(nameof(GenderIsFemale));

        // Override tier from stored report
        if (!string.IsNullOrEmpty(report.PriceTierName))
        {
            _selectedTier = AvailableTiers.FirstOrDefault(t => t == report.PriceTierName) ?? _selectedTier;
            OnPropertyChanged(nameof(SelectedTier));
        }

        // Populate selected entries from stored results
        SelectedEntries.Clear();
        foreach (var entry in report.Entries)
        {
            var td = _allTests.FirstOrDefault(t => t.Id == entry.TestDefinitionId);
            if (td is null) continue;

            var (min, max) = GetRange(td);
            var row = new ReportEntryRow(td, min, max);

            // Set result first (this recalculates the flag)
            row.ResultText = entry.ResultValue ?? string.Empty;

            // Set stored price as an override so it is preserved when tier changes
            if (entry.ChargedPrice.HasValue)
                row.PriceEditText = entry.ChargedPrice.Value.ToString("F2", CultureInfo.InvariantCulture);

            // Restore package membership. MarkAsPackage enforces ChargedPrice == null,
            // so stale persisted prices on package entries are cleared unconditionally.
            if (entry.IsFromPackage)
                row.MarkAsPackage();

            SelectedEntries.Add(row);
        }

        // Restore billing mode state
        _billingMode         = report.BillingMode;
        _packageNameSnapshot = report.PackageNameSnapshot;
        _packageTotalPrice   = report.PackageTotalPrice;
        OnPropertyChanged(nameof(BillingMode));
        OnPropertyChanged(nameof(IsPackageBilled));
        OnPropertyChanged(nameof(PackageBillingDisplay));

        RefreshAvailableTests();
        OnPropertyChanged(nameof(TotalCharge));
        OnPropertyChanged(nameof(TotalChargeDisplay));
    }

    private void RefreshAvailableTests()
    {
        AvailableTests.Clear();
        if (_selectedCategory is null) return;

        var selected = SelectedEntries.Select(e => e.TestDef.Id).ToHashSet();
        foreach (var td in _allTests.Where(t => t.Category == _selectedCategory && !selected.Contains(t.Id)))
            AvailableTests.Add(td);
    }

    // ── Range helpers ─────────────────────────────────────────────────────────
    private (decimal min, decimal max) GetRange(TestDefinition td)
    {
        if (int.TryParse(_formAge, out var age) && age < 18)
            return (td.ChildMinValue, td.ChildMaxValue);
        return _gender == Gender.Female
            ? (td.FemaleMinValue, td.FemaleMaxValue)
            : (td.MaleMinValue, td.MaleMaxValue);
    }

    private void RefreshEntryRanges()
    {
        foreach (var row in SelectedEntries)
        {
            var (min, max) = GetRange(row.TestDef);
            row.UpdateRange(min, max);
        }
    }

    // ── Command handlers ──────────────────────────────────────────────────────
    private void ExecuteAddTest(object? parameter)
    {
        var td = parameter as TestDefinition ?? _selectedAvailableTest;
        if (td is null) return;

        var (min, max) = GetRange(td);
        // Manually-added tests always get tier pricing (IsFromPackage stays false by default).
        var row = new ReportEntryRow(td, min, max)
        {
            ChargedPrice = _selectedTier is not null
                ? _testService.GetPrice(td.Id, _selectedTier)
                : null
        };
        SelectedEntries.Add(row);
        RefreshAvailableTests();
        OnPropertyChanged(nameof(TotalCharge));
        OnPropertyChanged(nameof(TotalChargeDisplay));
    }

    private void ExecuteRemoveTest(object? parameter)
    {
        var row = parameter as ReportEntryRow ?? _selectedEntry;
        if (row is null) return;

        SelectedEntries.Remove(row);
        RefreshAvailableTests();
        OnPropertyChanged(nameof(TotalCharge));
        OnPropertyChanged(nameof(TotalChargeDisplay));
    }

    private void ExecuteResetPrice(object? parameter)
    {
        if (parameter is not ReportEntryRow row) return;
        row.ClearOverride();
        // OnEntryPropertyChanged detects IsPriceOverridden → false and re-applies tier price.
    }

    private void LoadPackages()
    {
        AvailablePackages.Clear();
        foreach (var pkg in _packageService.GetActive())
            AvailablePackages.Add(pkg);
    }

    private void ExecuteApplyPackage(object? _)
    {
        if (_selectedPackage is null) return;

        var packageName  = _selectedPackage.Name;
        var packagePrice = _selectedPackage.Price;

        var existingIds = SelectedEntries.Select(e => e.TestDef.Id).ToHashSet();

        // Add only tests not already present and mark them as package members.
        // NEVER touch pre-existing manually-added entries — their prices are preserved.
        foreach (var item in _selectedPackage.Items.OrderBy(i => i.SortOrder))
        {
            if (item.TestDefinition is null) continue;
            if (existingIds.Contains(item.TestDefinitionId)) continue;
            var (min, max) = GetRange(item.TestDefinition);
            var row = new ReportEntryRow(item.TestDefinition, min, max);
            row.MarkAsPackage();  // no ChargedPrice — covered by PackageTotalPrice
            SelectedEntries.Add(row);
        }

        // Record package billing state. Grand total = PackageTotalPrice + manual extras.
        _billingMode         = BillingMode.Package;
        _packageNameSnapshot = packageName;
        _packageTotalPrice   = packagePrice;
        OnPropertyChanged(nameof(BillingMode));
        OnPropertyChanged(nameof(IsPackageBilled));
        OnPropertyChanged(nameof(PackageBillingDisplay));

        SelectedPackage = null;
        RefreshAvailableTests();
        OnPropertyChanged(nameof(TotalCharge));
        OnPropertyChanged(nameof(TotalChargeDisplay));
    }

    private void ExecuteClearPackageBilling(object? _)
    {
        var confirm = MessageBox.Show(
            "Removing the package will convert package tests to individually billed tests " +
            "using the current pricing tier. Continue?",
            "Remove Package",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        // Demote package entries back to manual so RefreshEntryPrices can tier-price them.
        foreach (var row in SelectedEntries.Where(r => r.IsFromPackage))
            row.ClearPackageMembership();

        _billingMode         = BillingMode.Normal;
        _packageNameSnapshot = null;
        _packageTotalPrice   = null;
        OnPropertyChanged(nameof(BillingMode));
        OnPropertyChanged(nameof(IsPackageBilled));
        OnPropertyChanged(nameof(PackageBillingDisplay));

        // Re-apply tier prices to all entries (now including formerly-package entries).
        RefreshEntryPrices();
    }

    private async Task TrySaveAsync(ReportStatus status)
    {
        if (_isSaving) return;

        FormError = string.Empty;

        if (string.IsNullOrWhiteSpace(_formName))
            { FormError = "Patient name is required."; return; }
        if (!int.TryParse(_formAge, out var age) || age <= 0 || age > 150)
            { FormError = "Enter a valid age (1–150)."; return; }
        if (string.IsNullOrWhiteSpace(_formPhone))
            { FormError = "Phone number is required."; return; }
        if (_formPhone.Count(char.IsDigit) < 10)
            { FormError = "Enter a valid phone number (at least 10 digits)."; return; }
        if (SelectedEntries.Count == 0)
            { FormError = "Select at least one test before saving."; return; }

        if (status == ReportStatus.Completed)
        {
            var missing = SelectedEntries.FirstOrDefault(e => string.IsNullOrWhiteSpace(e.ResultText));
            if (missing is not null)
                { FormError = $"Enter result for: {missing.TestDef.TestName}"; return; }

            var invalid = SelectedEntries.FirstOrDefault(e =>
                !string.IsNullOrWhiteSpace(e.ResultText) &&
                !decimal.TryParse(e.ResultText, out _));
            if (invalid is not null)
                { FormError = $"Result for {invalid.TestDef.TestName} must be a number."; return; }
        }

        var patient = new Patient
        {
            FullName        = _formName.Trim(),
            Age             = age,
            Gender          = _gender,
            PhoneNumber     = _formPhone.Trim(),
            ReferringDoctor = _formReferringDoctor.Trim(),
            Address         = _formAddress.Trim(),
            CreatedAt       = DateTime.Now,
            CreatedByUserId = _authService.CurrentUser?.Id
        };

        var report = new Report
        {
            ReportCode          = _reportCode,
            TestDate            = _testDate,
            ReportDate          = _reportDate,
            Status              = status,
            CreatedAt           = DateTime.Now,
            CreatedByUserId     = _authService.CurrentUser?.Id,
            ModifiedByUserId    = _editReportId.HasValue ? _authService.CurrentUser?.Id : null,
            PriceTierName       = _billingMode == BillingMode.Normal && !string.IsNullOrEmpty(_selectedTier) ? _selectedTier : null,
            BillingMode         = _billingMode,
            PackageNameSnapshot = _packageNameSnapshot,
            PackageTotalPrice   = _packageTotalPrice
        };

        var entries = SelectedEntries.Select(row => new ReportEntry
        {
            TestDefinitionId   = row.TestDef.Id,
            ResultValue        = row.ResultText.Trim(),
            ResultFlag         = row.Flag,
            ReferenceRangeUsed = row.RangeDisplay,
            PriceTierName      = row.ChargedPrice.HasValue ? _selectedTier : null,
            ChargedPrice       = row.ChargedPrice,
            IsFromPackage      = row.IsFromPackage
        }).ToList();

        // Defensive invariant: IsFromPackage == true → ChargedPrice must be null.
        // Normalises any stale data that survived to this point; prevents double billing
        // regardless of how the entry was constructed or modified during the session.
        foreach (var entry in entries.Where(e => e.IsFromPackage))
        {
            entry.ChargedPrice  = null;
            entry.PriceTierName = null;
        }

        IsSaving = true;
        try
        {
            if (_editReportId.HasValue)
            {
                if (status == ReportStatus.Completed)
                    await _reportService.FinalizeDraftReportAsync(_editReportId.Value, patient, report, entries);
                else
                    _reportService.UpdateDraft(_editReportId.Value, patient, report, entries);
            }
            else if (status == ReportStatus.Completed)
            {
                report.Status = ReportStatus.Draft;  // FinalizeReportAsync owns the transition
                await _reportService.FinalizeReportAsync(patient, report, entries);
            }
            else
            {
                _reportService.Save(patient, report, entries);
            }
            _navigationService.NavigateTo<DashboardViewModel>();
        }
        catch (Exception ex)
        {
            FormError = status == ReportStatus.Completed
                ? $"Could not generate report: {ex.Message}"
                : $"Could not save draft: {ex.Message}";
            ErrorLoggingService.Log(ex, "TrySave");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
