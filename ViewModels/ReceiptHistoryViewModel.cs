using System.Collections.ObjectModel;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class ReceiptHistoryViewModel : ViewModelBase
{
    private readonly ReceiptHistoryService _historyService;
    private readonly WarehouseService _warehouseService;
    private readonly SupplierService _supplierService;

    private bool _isInitializing = true;

    public ObservableCollection<Warehouse> WarehouseFilters { get; } = new();
    public ObservableCollection<Supplier> SupplierFilters { get; } = new();

    public ObservableCollection<ReceiptHistoryDocumentRow> Receipts { get; } = new();
    public ObservableCollection<WarehouseReceiptsRow> WarehouseSummary { get; } = new();

    private Warehouse? _selectedWarehouseFilter;
    public Warehouse? SelectedWarehouseFilter
    {
        get => _selectedWarehouseFilter;
        set
        {
            if (SetField(ref _selectedWarehouseFilter, value) && !_isInitializing)
                _ = LoadAsync();
        }
    }

    private Supplier? _selectedSupplierFilter;
    public Supplier? SelectedSupplierFilter
    {
        get => _selectedSupplierFilter;
        set
        {
            if (SetField(ref _selectedSupplierFilter, value) && !_isInitializing)
                _ = LoadAsync();
        }
    }

    private DateTime? _dateFrom;
    public DateTime? DateFrom
    {
        get => _dateFrom;
        set
        {
            if (SetField(ref _dateFrom, value) && !_isInitializing)
                _ = LoadAsync();
        }
    }

    private DateTime? _dateTo;
    public DateTime? DateTo
    {
        get => _dateTo;
        set
        {
            if (SetField(ref _dateTo, value) && !_isInitializing)
                _ = LoadAsync();
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value) && !_isInitializing)
                _ = LoadAsync();
        }
    }

    private ReceiptHistoryDocumentRow? _selectedReceipt;
    public ReceiptHistoryDocumentRow? SelectedReceipt
    {
        get => _selectedReceipt;
        set => SetField(ref _selectedReceipt, value);
    }

    private string _summaryText = string.Empty;
    public string SummaryText
    {
        get => _summaryText;
        set => SetField(ref _summaryText, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public ICommand ResetCommand { get; }

    public ReceiptHistoryViewModel(
        ReceiptHistoryService historyService,
        WarehouseService warehouseService,
        SupplierService supplierService)
    {
        _historyService = historyService;
        _warehouseService = warehouseService;
        _supplierService = supplierService;

        ResetCommand = new RelayCommand(async _ => await ResetAsync());

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var warehouses = await _warehouseService.GetAllAsync();

        WarehouseFilters.Clear();
        WarehouseFilters.Add(new Warehouse { Id = 0, Name = "Все склады" });
        foreach (var warehouse in warehouses)
            WarehouseFilters.Add(warehouse);

        var suppliers = await _supplierService.GetAllAsync();

        SupplierFilters.Clear();
        SupplierFilters.Add(new Supplier { Id = 0, Name = "Все поставщики" });
        foreach (var supplier in suppliers)
            SupplierFilters.Add(supplier);

        SelectedWarehouseFilter = WarehouseFilters[0];
        SelectedSupplierFilter = SupplierFilters[0];

        _isInitializing = false;
        await LoadAsync();
    }

    private async Task ResetAsync()
    {
        _isInitializing = true;

        SelectedWarehouseFilter = WarehouseFilters[0];
        SelectedSupplierFilter = SupplierFilters[0];
        DateFrom = null;
        DateTo = null;
        SearchText = string.Empty;

        _isInitializing = false;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        DateOnly? dateFrom = DateFrom != null
            ? DateOnly.FromDateTime(DateFrom.Value)
            : null;

        DateOnly? dateTo = DateTo != null
            ? DateOnly.FromDateTime(DateTo.Value)
            : null;

        if (dateFrom != null && dateTo != null && dateFrom.Value > dateTo.Value)
        {
            StatusMessage = "Начало периода не может быть позже его окончания.";
            Receipts.Clear();
            WarehouseSummary.Clear();
            SelectedReceipt = null;
            SummaryText = string.Empty;
            return;
        }

        StatusMessage = string.Empty;

        var documents = await _historyService.GetAsync(
            SelectedWarehouseFilter?.Id ?? 0,
            dateFrom,
            dateTo,
            SelectedSupplierFilter?.Id ?? 0,
            SearchText);

        Receipts.Clear();
        foreach (var document in documents)
            Receipts.Add(document);

        SelectedReceipt = null;

        WarehouseSummary.Clear();

        var summary = documents
            .GroupBy(document => document.WarehouseId)
            .Select(group => new WarehouseReceiptsRow
            {
                WarehouseName = group.First().WarehouseName,
                DocumentsCount = group.Count(),
                TotalQuantity = group.Sum(document => document.TotalQuantity),
                LastArrivalDate = group.Max(document => document.ArrivalDate)
            })
            .OrderBy(row => row.WarehouseName);

        foreach (var row in summary)
            WarehouseSummary.Add(row);

        SummaryText =
            $"Поступлений: {documents.Count} · " +
            $"единиц товара: {documents.Sum(document => document.TotalQuantity)}";
    }
}