using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using UrbanDiagnosticCentre.Helpers;
using UrbanDiagnosticCentre.Models;
using UrbanDiagnosticCentre.Services;

namespace UrbanDiagnosticCentre.ViewModels;

public class TestPriceRow
{
    public int     Id        { get; }
    public string  TierName  { get; }
    public decimal Price     { get; }
    public int     SortOrder { get; }
    public TestPrice Source  { get; }

    public string PriceDisplay => Price.ToString("F2");

    public TestPriceRow(TestPrice tp)
    {
        Id        = tp.Id;
        TierName  = tp.TierName;
        Price     = tp.Price;
        SortOrder = tp.SortOrder;
        Source    = tp;
    }
}

public class TestRow
{
    public int Id { get; }
    public string TestName { get; }
    public string Category { get; }
    public string SampleType { get; }
    public string Unit { get; }
    public string MaleRange { get; }
    public string FemaleRange { get; }
    public string ChildRange { get; }
    public TestDefinition Source { get; }

    public TestRow(TestDefinition td)
    {
        var fmt = $"F{td.DecimalPrecision}";
        Id = td.Id;
        TestName = td.TestName;
        Category = td.Category;
        SampleType = td.SampleType;
        Unit = td.Unit;
        MaleRange   = $"{td.MaleMinValue.ToString(fmt)}–{td.MaleMaxValue.ToString(fmt)}";
        FemaleRange = $"{td.FemaleMinValue.ToString(fmt)}–{td.FemaleMaxValue.ToString(fmt)}";
        ChildRange  = $"{td.ChildMinValue.ToString(fmt)}–{td.ChildMaxValue.ToString(fmt)}";
        Source = td;
    }
}

public class TestManagerViewModel : BaseViewModel
{
    private readonly TestDefinitionService _service;
    private readonly INavigationService _navigationService;
    private List<TestDefinition> _allTests = new();
    private TestDefinition? _editingTest;

    // ── Display collection ───────────────────────────────────────────────────
    public ObservableCollection<TestRow> Tests { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { SetProperty(ref _searchText, value); ApplyFilter(); }
    }

    private TestRow? _selectedTest;
    public TestRow? SelectedTest
    {
        get => _selectedTest;
        set { SetProperty(ref _selectedTest, value); }
    }

    // ── Form state ───────────────────────────────────────────────────────────
    private bool _isFormVisible;
    public bool IsFormVisible
    {
        get => _isFormVisible;
        set { SetProperty(ref _isFormVisible, value); }
    }

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set { SetProperty(ref _isEditing, value); }
    }

    public string FormTitle => _isEditing ? "Edit Test" : "Add New Test";

    // ── Form fields ──────────────────────────────────────────────────────────
    private string _formTestName = string.Empty;
    public string FormTestName
    {
        get => _formTestName;
        set { SetProperty(ref _formTestName, value); }
    }

    private string _formCategory = string.Empty;
    public string FormCategory
    {
        get => _formCategory;
        set { SetProperty(ref _formCategory, value); }
    }

    private string _formSampleType = string.Empty;
    public string FormSampleType
    {
        get => _formSampleType;
        set { SetProperty(ref _formSampleType, value); }
    }

    private string _formUnit = string.Empty;
    public string FormUnit
    {
        get => _formUnit;
        set { SetProperty(ref _formUnit, value); }
    }

    private string _formMaleMin = "0";
    public string FormMaleMin
    {
        get => _formMaleMin;
        set { SetProperty(ref _formMaleMin, value); }
    }

    private string _formMaleMax = "0";
    public string FormMaleMax
    {
        get => _formMaleMax;
        set { SetProperty(ref _formMaleMax, value); }
    }

    private string _formFemaleMin = "0";
    public string FormFemaleMin
    {
        get => _formFemaleMin;
        set { SetProperty(ref _formFemaleMin, value); }
    }

    private string _formFemaleMax = "0";
    public string FormFemaleMax
    {
        get => _formFemaleMax;
        set { SetProperty(ref _formFemaleMax, value); }
    }

    private string _formChildMin = "0";
    public string FormChildMin
    {
        get => _formChildMin;
        set { SetProperty(ref _formChildMin, value); }
    }

    private string _formChildMax = "0";
    public string FormChildMax
    {
        get => _formChildMax;
        set { SetProperty(ref _formChildMax, value); }
    }

    private int _formDecimalPrecision = 2;
    public int FormDecimalPrecision
    {
        get => _formDecimalPrecision;
        set { SetProperty(ref _formDecimalPrecision, value); }
    }

    private string _formNotes = string.Empty;
    public string FormNotes
    {
        get => _formNotes;
        set { SetProperty(ref _formNotes, value); }
    }

    private string _formError = string.Empty;
    public string FormError
    {
        get => _formError;
        set { SetProperty(ref _formError, value); }
    }

    // ── Pricing sub-form ──────────────────────────────────────────────────────
    public ObservableCollection<TestPriceRow> Prices { get; } = new();

    private string _formPriceTierName = string.Empty;
    public string FormPriceTierName
    {
        get => _formPriceTierName;
        set => SetProperty(ref _formPriceTierName, value);
    }

    private string _formPriceAmount = string.Empty;
    public string FormPriceAmount
    {
        get => _formPriceAmount;
        set => SetProperty(ref _formPriceAmount, value);
    }

    private string _priceFormError = string.Empty;
    public string PriceFormError
    {
        get => _priceFormError;
        set { SetProperty(ref _priceFormError, value); OnPropertyChanged(nameof(HasPriceFormError)); }
    }

    public bool HasPriceFormError => !string.IsNullOrEmpty(_priceFormError);

    // ── Commands ─────────────────────────────────────────────────────────────
    public RelayCommand AddNewCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand ArchiveCommand { get; }
    public RelayCommand NavigateToDashboardCommand { get; }
    public RelayCommand AddPriceCommand    { get; }
    public RelayCommand DeletePriceCommand { get; }

    public TestManagerViewModel(TestDefinitionService service, INavigationService navigationService)
    {
        _service = service;
        _navigationService = navigationService;

        AddNewCommand             = new RelayCommand(ExecuteAddNew);
        EditCommand               = new RelayCommand(ExecuteEdit);
        SaveCommand               = new RelayCommand(ExecuteSave);
        CancelCommand             = new RelayCommand(ExecuteCancel);
        ArchiveCommand            = new RelayCommand(ExecuteArchive);
        NavigateToDashboardCommand = new RelayCommand(_ => _navigationService.NavigateTo<DashboardViewModel>());
        AddPriceCommand           = new RelayCommand(ExecuteAddPrice);
        DeletePriceCommand        = new RelayCommand(ExecuteDeletePrice);

        LoadTests();
    }

    // ── Data loading ─────────────────────────────────────────────────────────
    private void LoadTests()
    {
        _allTests = _service.GetAll();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Tests.Clear();
        var term = _searchText.Trim();
        foreach (var td in _allTests)
        {
            if (string.IsNullOrEmpty(term) ||
                td.TestName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                td.Category.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                Tests.Add(new TestRow(td));
            }
        }
    }

    // ── Command handlers ──────────────────────────────────────────────────────
    private void ExecuteAddNew(object? _)
    {
        _editingTest = null;
        IsEditing = false;
        ClearForm();
        IsFormVisible = true;
        OnPropertyChanged(nameof(FormTitle));
    }

    private void ExecuteEdit(object? parameter)
    {
        var row = parameter as TestRow ?? SelectedTest;
        if (row is null) return;

        _editingTest = row.Source;
        IsEditing = true;
        PopulateForm(_editingTest);
        LoadPrices();
        IsFormVisible = true;
        OnPropertyChanged(nameof(FormTitle));
    }

    private void ExecuteArchive(object? parameter)
    {
        var row = parameter as TestRow ?? SelectedTest;
        if (row is null) return;

        var result = MessageBox.Show(
            $"Archive \"{row.TestName}\"?\n\nArchived tests will no longer be available for new reports.",
            "Archive Test",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        _service.Archive(row.Source);

        if (IsFormVisible && _editingTest?.Id == row.Id)
            ExecuteCancel(null);

        LoadTests();
    }

    private void ExecuteSave(object? _)
    {
        FormError = string.Empty;

        if (string.IsNullOrWhiteSpace(FormTestName))  { FormError = "Test name is required.";   return; }
        if (string.IsNullOrWhiteSpace(FormCategory))   { FormError = "Category is required.";    return; }
        if (string.IsNullOrWhiteSpace(FormSampleType)) { FormError = "Sample type is required."; return; }
        if (string.IsNullOrWhiteSpace(FormUnit))       { FormError = "Unit is required.";        return; }

        if (!TryParseDecimal(FormMaleMin,   "Male Min",   out var maleMin))   return;
        if (!TryParseDecimal(FormMaleMax,   "Male Max",   out var maleMax))   return;
        if (!TryParseDecimal(FormFemaleMin, "Female Min", out var femaleMin)) return;
        if (!TryParseDecimal(FormFemaleMax, "Female Max", out var femaleMax)) return;
        if (!TryParseDecimal(FormChildMin,  "Child Min",  out var childMin))  return;
        if (!TryParseDecimal(FormChildMax,  "Child Max",  out var childMax))  return;

        if (_isEditing && _editingTest is not null)
        {
            _editingTest.TestName        = FormTestName.Trim();
            _editingTest.Category        = FormCategory.Trim();
            _editingTest.SampleType      = FormSampleType.Trim();
            _editingTest.Unit            = FormUnit.Trim();
            _editingTest.MaleMinValue    = maleMin;
            _editingTest.MaleMaxValue    = maleMax;
            _editingTest.FemaleMinValue  = femaleMin;
            _editingTest.FemaleMaxValue  = femaleMax;
            _editingTest.ChildMinValue   = childMin;
            _editingTest.ChildMaxValue   = childMax;
            _editingTest.DecimalPrecision = FormDecimalPrecision;
            _editingTest.Notes           = FormNotes.Trim();
            _service.Update(_editingTest);
        }
        else
        {
            var td = new TestDefinition
            {
                TestName        = FormTestName.Trim(),
                Category        = FormCategory.Trim(),
                SampleType      = FormSampleType.Trim(),
                Unit            = FormUnit.Trim(),
                MaleMinValue    = maleMin,
                MaleMaxValue    = maleMax,
                FemaleMinValue  = femaleMin,
                FemaleMaxValue  = femaleMax,
                ChildMinValue   = childMin,
                ChildMaxValue   = childMax,
                DecimalPrecision = FormDecimalPrecision,
                Notes           = FormNotes.Trim(),
                CreatedAt       = DateTime.Now
            };
            _service.Add(td);
        }

        IsFormVisible = false;
        _editingTest  = null;
        LoadTests();
    }

    private void ExecuteCancel(object? _)
    {
        IsFormVisible = false;
        _editingTest  = null;
        ClearForm();
    }

    private void ExecuteAddPrice(object? _)
    {
        PriceFormError = string.Empty;

        if (string.IsNullOrWhiteSpace(FormPriceTierName))
            { PriceFormError = "Tier name is required."; return; }

        if (!decimal.TryParse(FormPriceAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount < 0)
            { PriceFormError = "Enter a valid price (e.g. 250.00)."; return; }

        if (_editingTest is null)
            { PriceFormError = "Save the test first before adding prices."; return; }

        var nextOrder = Prices.Count > 0 ? Prices.Max(p => p.SortOrder) + 1 : 0;

        _service.UpsertPrice(new TestPrice
        {
            TestDefinitionId = _editingTest.Id,
            TierName         = FormPriceTierName.Trim(),
            Price            = amount,
            SortOrder        = nextOrder,
            IsActive         = true
        });

        FormPriceTierName = string.Empty;
        FormPriceAmount   = string.Empty;
        LoadPrices();
    }

    private void ExecuteDeletePrice(object? parameter)
    {
        if (parameter is not TestPriceRow row) return;
        _service.DeletePrice(row.Id);
        LoadPrices();
    }

    private void LoadPrices()
    {
        Prices.Clear();
        if (_editingTest is null) return;
        foreach (var tp in _service.GetPrices(_editingTest.Id))
            Prices.Add(new TestPriceRow(tp));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private bool TryParseDecimal(string input, string fieldName, out decimal result)
    {
        if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out result)) return true;
        FormError = $"{fieldName} must be a valid number (e.g. 150.5).";
        return false;
    }

    private void ClearForm()
    {
        FormTestName        = string.Empty;
        FormCategory        = string.Empty;
        FormSampleType      = string.Empty;
        FormUnit            = string.Empty;
        FormMaleMin         = "0";
        FormMaleMax         = "0";
        FormFemaleMin       = "0";
        FormFemaleMax       = "0";
        FormChildMin        = "0";
        FormChildMax        = "0";
        FormDecimalPrecision = 2;
        FormNotes           = string.Empty;
        FormError           = string.Empty;
        FormPriceTierName   = string.Empty;
        FormPriceAmount     = string.Empty;
        PriceFormError      = string.Empty;
        Prices.Clear();
    }

    private void PopulateForm(TestDefinition td)
    {
        FormTestName         = td.TestName;
        FormCategory         = td.Category;
        FormSampleType       = td.SampleType;
        FormUnit             = td.Unit;
        FormMaleMin          = td.MaleMinValue.ToString();
        FormMaleMax          = td.MaleMaxValue.ToString();
        FormFemaleMin        = td.FemaleMinValue.ToString();
        FormFemaleMax        = td.FemaleMaxValue.ToString();
        FormChildMin         = td.ChildMinValue.ToString();
        FormChildMax         = td.ChildMaxValue.ToString();
        FormDecimalPrecision = td.DecimalPrecision;
        FormNotes            = td.Notes;
        FormError            = string.Empty;
    }
}
