using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using UrbanDiagnosticCentre.Helpers;
using UrbanDiagnosticCentre.Models;
using UrbanDiagnosticCentre.Services;

namespace UrbanDiagnosticCentre.ViewModels;

// ── Display row for the package list ─────────────────────────────────────────
public class TestPackageRow
{
    public int    Id          { get; }
    public string Name        { get; }
    public string Description { get; }
    public decimal Price      { get; }
    public bool   IsActive    { get; }
    public int    TestCount   { get; }
    public string PriceDisplay  => Price.ToString("F2");
    public string StatusDisplay => IsActive ? "Active" : "Inactive";
    public TestPackage Source { get; }

    public TestPackageRow(TestPackage p)
    {
        Id          = p.Id;
        Name        = p.Name;
        Description = p.Description;
        Price       = p.Price;
        IsActive    = p.IsActive;
        TestCount   = p.Items.Count;
        Source      = p;
    }
}

// ── Test entry used in the form's test picker ─────────────────────────────────
public class PackageTestItem
{
    public int    TestDefinitionId { get; }
    public string TestName         { get; }
    public string Category         { get; }
    public TestDefinition Source   { get; }

    public PackageTestItem(TestDefinition td)
    {
        TestDefinitionId = td.Id;
        TestName         = td.TestName;
        Category         = td.Category;
        Source           = td;
    }
}

// ── ViewModel ─────────────────────────────────────────────────────────────────
public class PackageManagerViewModel : BaseViewModel
{
    private readonly TestPackageService   _packageService;
    private readonly TestDefinitionService _testService;
    private readonly INavigationService   _navigationService;

    private List<TestPackage>    _allPackages = new();
    private List<TestDefinition> _allTests    = new();
    private int?                 _editingId;      // null = creating new

    // ── Package list ──────────────────────────────────────────────────────────
    public ObservableCollection<TestPackageRow> Packages { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { SetProperty(ref _searchText, value); ApplyFilter(); }
    }

    private TestPackageRow? _selectedPackage;
    public TestPackageRow? SelectedPackage
    {
        get => _selectedPackage;
        set => SetProperty(ref _selectedPackage, value);
    }

    // ── Form visibility ────────────────────────────────────────────────────────
    private bool _isFormVisible;
    public bool IsFormVisible
    {
        get => _isFormVisible;
        set => SetProperty(ref _isFormVisible, value);
    }

    public string FormTitle => _editingId.HasValue ? "Edit Package" : "New Package";

    // ── Form fields ────────────────────────────────────────────────────────────
    private string _formName = string.Empty;
    public string FormName
    {
        get => _formName;
        set => SetProperty(ref _formName, value);
    }

    private string _formDescription = string.Empty;
    public string FormDescription
    {
        get => _formDescription;
        set => SetProperty(ref _formDescription, value);
    }

    private string _formPrice = "0.00";
    public string FormPrice
    {
        get => _formPrice;
        set => SetProperty(ref _formPrice, value);
    }

    private bool _formIsActive = true;
    public bool FormIsActive
    {
        get => _formIsActive;
        set => SetProperty(ref _formIsActive, value);
    }

    private string _formError = string.Empty;
    public string FormError
    {
        get => _formError;
        set => SetProperty(ref _formError, value);
    }

    // ── Test picker ───────────────────────────────────────────────────────────
    public ObservableCollection<PackageTestItem> AvailableTests  { get; } = new();
    public ObservableCollection<PackageTestItem> PackageContents { get; } = new();

    private string _testSearch = string.Empty;
    public string TestSearch
    {
        get => _testSearch;
        set { SetProperty(ref _testSearch, value); RefreshAvailableTests(); }
    }

    private PackageTestItem? _selectedAvailable;
    public PackageTestItem? SelectedAvailable
    {
        get => _selectedAvailable;
        set => SetProperty(ref _selectedAvailable, value);
    }

    private PackageTestItem? _selectedInPackage;
    public PackageTestItem? SelectedInPackage
    {
        get => _selectedInPackage;
        set => SetProperty(ref _selectedInPackage, value);
    }

    public bool HasNoPackageTests => PackageContents.Count == 0;

    // ── Commands ──────────────────────────────────────────────────────────────
    public RelayCommand AddNewCommand              { get; }
    public RelayCommand EditCommand                { get; }
    public RelayCommand SaveCommand                { get; }
    public RelayCommand CancelCommand              { get; }
    public RelayCommand DeleteCommand              { get; }
    public RelayCommand DuplicateCommand           { get; }
    public RelayCommand AddTestToPackageCommand    { get; }
    public RelayCommand RemoveTestFromPackageCommand { get; }
    public RelayCommand NavigateToDashboardCommand { get; }

    public PackageManagerViewModel(
        TestPackageService    packageService,
        TestDefinitionService testService,
        INavigationService    navigationService)
    {
        _packageService    = packageService;
        _testService       = testService;
        _navigationService = navigationService;

        AddNewCommand              = new RelayCommand(ExecuteAddNew);
        EditCommand                = new RelayCommand(ExecuteEdit);
        SaveCommand                = new RelayCommand(ExecuteSave);
        CancelCommand              = new RelayCommand(_ => ExecuteCancel());
        DeleteCommand              = new RelayCommand(ExecuteDelete);
        DuplicateCommand           = new RelayCommand(ExecuteDuplicate);
        AddTestToPackageCommand    = new RelayCommand(ExecuteAddTest);
        RemoveTestFromPackageCommand = new RelayCommand(ExecuteRemoveTest);
        NavigateToDashboardCommand = new RelayCommand(_ => _navigationService.NavigateTo<DashboardViewModel>());

        PackageContents.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoPackageTests));

        LoadData();
    }

    // ── Data loading ──────────────────────────────────────────────────────────
    private void LoadData()
    {
        _allPackages = _packageService.GetAll();
        _allTests    = _testService.GetAll();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Packages.Clear();
        var term = _searchText.Trim();
        foreach (var p in _allPackages)
        {
            if (string.IsNullOrEmpty(term) ||
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                Packages.Add(new TestPackageRow(p));
            }
        }
    }

    private void RefreshAvailableTests()
    {
        var inPackage = PackageContents.Select(t => t.TestDefinitionId).ToHashSet();
        var term      = _testSearch.Trim();
        AvailableTests.Clear();
        foreach (var td in _allTests)
        {
            if (inPackage.Contains(td.Id)) continue;
            if (!string.IsNullOrEmpty(term) &&
                !td.TestName.Contains(term, StringComparison.OrdinalIgnoreCase) &&
                !td.Category.Contains(term, StringComparison.OrdinalIgnoreCase))
                continue;
            AvailableTests.Add(new PackageTestItem(td));
        }
    }

    // ── Command handlers ──────────────────────────────────────────────────────
    private void ExecuteAddNew(object? _)
    {
        _editingId = null;
        ClearForm();
        IsFormVisible = true;
        OnPropertyChanged(nameof(FormTitle));
    }

    private void ExecuteEdit(object? parameter)
    {
        var row = parameter as TestPackageRow ?? SelectedPackage;
        if (row is null) return;

        _editingId = row.Id;
        PopulateForm(row.Source);
        IsFormVisible = true;
        OnPropertyChanged(nameof(FormTitle));
    }

    private void ExecuteSave(object? _)
    {
        FormError = string.Empty;

        if (string.IsNullOrWhiteSpace(FormName))
            { FormError = "Package name is required."; return; }

        if (!decimal.TryParse(FormPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price < 0)
            { FormError = "Enter a valid package price (e.g. 999.00)."; return; }

        var testIds = PackageContents.Select(t => t.TestDefinitionId).ToList();

        if (_editingId.HasValue)
        {
            _packageService.UpdateMetadata(
                _editingId.Value,
                FormName.Trim(), FormDescription.Trim(), price, FormIsActive);
            _packageService.SetItems(_editingId.Value, testIds);
        }
        else
        {
            var pkg = new TestPackage
            {
                Name        = FormName.Trim(),
                Description = FormDescription.Trim(),
                Price       = price,
                IsActive    = FormIsActive,
                CreatedAt   = DateTime.Now
            };
            _packageService.Add(pkg);
            _packageService.SetItems(pkg.Id, testIds);
        }

        IsFormVisible = false;
        _editingId    = null;
        LoadData();
    }

    private void ExecuteCancel()
    {
        IsFormVisible = false;
        _editingId    = null;
        ClearForm();
    }

    private void ExecuteDelete(object? parameter)
    {
        var row = parameter as TestPackageRow ?? SelectedPackage;
        if (row is null) return;

        var result = MessageBox.Show(
            $"Delete package \"{row.Name}\"?\n\nThis cannot be undone.",
            "Delete Package",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        if (IsFormVisible && _editingId == row.Id)
            ExecuteCancel();

        _packageService.Delete(row.Id);
        LoadData();
    }

    private void ExecuteDuplicate(object? parameter)
    {
        var row = parameter as TestPackageRow ?? SelectedPackage;
        if (row is null) return;
        _packageService.Duplicate(row.Source);
        LoadData();
    }

    private void ExecuteAddTest(object? parameter)
    {
        var item = parameter as PackageTestItem ?? SelectedAvailable;
        if (item is null) return;
        PackageContents.Add(item);
        RefreshAvailableTests();
    }

    private void ExecuteRemoveTest(object? parameter)
    {
        var item = parameter as PackageTestItem ?? SelectedInPackage;
        if (item is null) return;
        PackageContents.Remove(item);
        RefreshAvailableTests();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void ClearForm()
    {
        FormName        = string.Empty;
        FormDescription = string.Empty;
        FormPrice       = "0.00";
        FormIsActive    = true;
        FormError       = string.Empty;
        TestSearch      = string.Empty;
        PackageContents.Clear();
        RefreshAvailableTests();
    }

    private void PopulateForm(TestPackage pkg)
    {
        FormName        = pkg.Name;
        FormDescription = pkg.Description;
        FormPrice       = pkg.Price.ToString("F2", CultureInfo.InvariantCulture);
        FormIsActive    = pkg.IsActive;
        FormError       = string.Empty;
        TestSearch      = string.Empty;

        PackageContents.Clear();
        foreach (var item in pkg.Items.OrderBy(i => i.SortOrder))
        {
            if (item.TestDefinition is not null)
                PackageContents.Add(new PackageTestItem(item.TestDefinition));
        }
        RefreshAvailableTests();
    }
}
