using System.Collections.ObjectModel;
using UrbanDiagnosticCentre.Helpers;
using UrbanDiagnosticCentre.Models;
using UrbanDiagnosticCentre.Services;

namespace UrbanDiagnosticCentre.ViewModels;

// ── Detail panel display types ────────────────────────────────────────────────

public class DetailEntryRow
{
    public string     TestName       { get; init; } = string.Empty;
    public string     Category       { get; init; } = string.Empty;
    public string     ResultValue    { get; init; } = string.Empty;
    public string     Unit           { get; init; } = string.Empty;
    public string     ReferenceRange { get; init; } = string.Empty;
    public ResultFlag Flag           { get; init; }

    public string FlagDisplay => Flag switch
    {
        ResultFlag.Normal   => "Normal",
        ResultFlag.Low      => "↓ Low",
        ResultFlag.High     => "↑ High",
        ResultFlag.Critical => "Critical",
        _                   => "—"
    };

    public string FlagColor => Flag switch
    {
        ResultFlag.Normal   => "#2E7D32",
        ResultFlag.Low      => "#1565C0",
        ResultFlag.High     => "#C62828",
        ResultFlag.Critical => "#B71C1C",
        _                   => "#9E9E9E"
    };
}

public class ReportDetailSnapshot
{
    public string  PatientName       { get; init; } = string.Empty;
    public int     Age               { get; init; }
    public string  Gender            { get; init; } = string.Empty;
    public string  PhoneNumber       { get; init; } = string.Empty;
    public string  ReferringDoctor   { get; init; } = string.Empty;
    public string  Address           { get; init; } = string.Empty;
    public string  ReportCode        { get; init; } = string.Empty;
    public string  TestDateDisplay   { get; init; } = string.Empty;
    public string  CreatedAtDisplay  { get; init; } = string.Empty;
    public ReportStatus Status       { get; init; }
    public bool    HasPdf            { get; init; }
    public string  PdfStatusDisplay  { get; init; } = string.Empty;
    public string? PriceTierName     { get; init; }
    public decimal? TotalCharge      { get; init; }
    public bool    HasPricing        => TotalCharge.HasValue || !string.IsNullOrEmpty(PriceTierName);
    public string  TotalChargeDisplay => TotalCharge.HasValue ? TotalCharge.Value.ToString("F2") : "—";
    public IReadOnlyList<DetailEntryRow> Entries { get; init; } = Array.Empty<DetailEntryRow>();
}

// ── ViewModel ─────────────────────────────────────────────────────────────────

public class ReportHistoryViewModel : BaseViewModel
{
    private readonly ReportService      _reportService;
    private readonly PrintService       _printService;
    private readonly INavigationService _navigationService;

    // ── ComboBox option lists ─────────────────────────────────────────────────
    public string[] StatusOptions { get; } = { "All", "Draft", "Completed", "Printed" };
    public string[] GenderOptions { get; } = { "All", "Male", "Female" };

    // ── Search & filter state ─────────────────────────────────────────────────
    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    private DateTime? _filterFrom;
    public DateTime? FilterFrom
    {
        get => _filterFrom;
        set { SetProperty(ref _filterFrom, value); RunSearch(); }
    }

    private DateTime? _filterTo;
    public DateTime? FilterTo
    {
        get => _filterTo;
        set { SetProperty(ref _filterTo, value); RunSearch(); }
    }

    private string _filterStatus = "All";
    public string FilterStatus
    {
        get => _filterStatus;
        set { SetProperty(ref _filterStatus, value); RunSearch(); }
    }

    private string _filterGender = "All";
    public string FilterGender
    {
        get => _filterGender;
        set { SetProperty(ref _filterGender, value); RunSearch(); }
    }

    // ── Results ───────────────────────────────────────────────────────────────
    public ObservableCollection<ReportHistoryRow> Results { get; } = new();

    private int _totalCount;
    public int TotalCount { get => _totalCount; private set => SetProperty(ref _totalCount, value); }

    public bool HasNoResults => Results.Count == 0;

    private ReportHistoryRow? _selectedRow;
    public ReportHistoryRow? SelectedRow
    {
        get => _selectedRow;
        set { SetProperty(ref _selectedRow, value); }
    }

    // ── Detail panel ──────────────────────────────────────────────────────────
    private ReportDetailSnapshot? _detail;
    public ReportDetailSnapshot? Detail
    {
        get => _detail;
        private set { SetProperty(ref _detail, value); OnPropertyChanged(nameof(HasDetail)); }
    }
    public bool HasDetail => _detail is not null;

    // ── Commands ──────────────────────────────────────────────────────────────
    public RelayCommand SearchCommand          { get; }
    public RelayCommand ClearFiltersCommand   { get; }
    public RelayCommand OpenPdfCommand        { get; }
    public RelayCommand PrintPdfCommand       { get; }
    public RelayCommand ViewDetailsCommand    { get; }
    public RelayCommand CloseDetailCommand    { get; }
    public RelayCommand BackCommand           { get; }
    public RelayCommand NavigateToNewReportCommand { get; }

    public ReportHistoryViewModel(ReportService reportService, PrintService printService, INavigationService navigationService)
    {
        _reportService     = reportService;
        _printService      = printService;
        _navigationService = navigationService;

        SearchCommand              = new RelayCommand(_ => RunSearch());
        ClearFiltersCommand        = new RelayCommand(ExecuteClearFilters);
        OpenPdfCommand             = new RelayCommand(ExecuteOpenPdf);
        PrintPdfCommand            = new RelayCommand(ExecutePrintPdf);
        ViewDetailsCommand         = new RelayCommand(ExecuteViewDetails);
        CloseDetailCommand         = new RelayCommand(_ => { Detail = null; SelectedRow = null; });
        BackCommand                = new RelayCommand(_ => _navigationService.NavigateTo<DashboardViewModel>());
        NavigateToNewReportCommand = new RelayCommand(_ => _navigationService.NavigateTo<PatientReportViewModel>());

        RunSearch(); // load all results on open
    }

    // ── Search ────────────────────────────────────────────────────────────────

    // Called by SearchCommand (button / Enter) and auto-triggers on filter change.
    public void RunSearch()
    {
        ReportStatus? status = _filterStatus switch
        {
            "Draft"     => ReportStatus.Draft,
            "Completed" => ReportStatus.Completed,
            "Printed"   => ReportStatus.Printed,
            _           => null
        };

        Gender? gender = _filterGender switch
        {
            "Male"   => Gender.Male,
            "Female" => Gender.Female,
            _        => null
        };

        var rows = _reportService.Search(_searchQuery, _filterFrom, _filterTo, status, gender);

        Results.Clear();
        foreach (var row in rows)
            Results.Add(row);

        TotalCount = Results.Count;
        OnPropertyChanged(nameof(HasNoResults));

        // Clear detail if the selected row is no longer in the result set
        if (_selectedRow is not null && !Results.Any(r => r.ReportId == _selectedRow.ReportId))
        {
            _selectedRow = null;
            OnPropertyChanged(nameof(SelectedRow));
            Detail = null;
        }
    }

    // ── Command handlers ──────────────────────────────────────────────────────

    private void ExecuteClearFilters(object? _)
    {
        // Update backing fields directly to avoid triggering RunSearch on each set
        _searchQuery  = string.Empty;
        _filterFrom   = null;
        _filterTo     = null;
        _filterStatus = "All";
        _filterGender = "All";

        OnPropertyChanged(nameof(SearchQuery));
        OnPropertyChanged(nameof(FilterFrom));
        OnPropertyChanged(nameof(FilterTo));
        OnPropertyChanged(nameof(FilterStatus));
        OnPropertyChanged(nameof(FilterGender));

        RunSearch();
    }

    private void ExecuteOpenPdf(object? parameter)
    {
        var row = parameter as ReportHistoryRow ?? _selectedRow;
        if (row is null) return;
        _printService.OpenPdf(row.ReportId, row.ReportCode);
    }

    private void ExecutePrintPdf(object? parameter)
    {
        var row = parameter as ReportHistoryRow ?? _selectedRow;
        if (row is null) return;
        _printService.PrintPdf(row.ReportId, row.ReportCode);
    }

    private void ExecuteViewDetails(object? parameter)
    {
        var row = parameter as ReportHistoryRow ?? _selectedRow;
        if (row is null) return;
        LoadDetail(row);
    }

    // ── Detail loading ────────────────────────────────────────────────────────

    private void LoadDetail(ReportHistoryRow row)
    {
        var report = _reportService.GetById(row.ReportId);
        if (report is null) { Detail = null; return; }

        var hasPdf = !string.IsNullOrEmpty(report.PdfPath);

        Detail = new ReportDetailSnapshot
        {
            PatientName      = report.Patient.FullName,
            Age              = report.Patient.Age,
            Gender           = report.Patient.Gender.ToString(),
            PhoneNumber      = report.Patient.PhoneNumber,
            ReferringDoctor  = string.IsNullOrWhiteSpace(report.Patient.ReferringDoctor) ? "—" : report.Patient.ReferringDoctor,
            Address          = string.IsNullOrWhiteSpace(report.Patient.Address) ? "—" : report.Patient.Address,
            ReportCode       = report.ReportCode,
            TestDateDisplay  = report.TestDate.ToString("dd MMM yyyy, hh:mm tt"),
            CreatedAtDisplay = report.CreatedAt.ToString("dd MMM yyyy, hh:mm tt"),
            Status           = report.Status,
            HasPdf           = hasPdf,
            PdfStatusDisplay = hasPdf
                ? $"Generated {report.ReportGeneratedAt?.ToString("dd MMM yyyy") ?? "—"}"
                : "Not generated",
            PriceTierName    = report.PriceTierName,
            TotalCharge      = report.Entries.Any(e => e.ChargedPrice.HasValue)
                ? report.Entries.Sum(e => e.ChargedPrice ?? 0m)
                : (decimal?)null,
            Entries = report.Entries
                .OrderBy(e => e.TestDefinition.Category)
                .ThenBy(e => e.TestDefinition.TestName)
                .Select(e => new DetailEntryRow
                {
                    TestName       = e.TestDefinition.TestName,
                    Category       = e.TestDefinition.Category,
                    ResultValue    = e.ResultValue,
                    Unit           = e.TestDefinition.Unit,
                    ReferenceRange = e.ReferenceRangeUsed,
                    Flag           = e.ResultFlag
                })
                .ToArray()
        };

        // Sync the DataGrid selection to match
        _selectedRow = Results.FirstOrDefault(r => r.ReportId == row.ReportId);
        OnPropertyChanged(nameof(SelectedRow));
    }
}
