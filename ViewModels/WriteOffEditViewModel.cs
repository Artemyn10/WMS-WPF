using System.Collections.ObjectModel;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class WriteOffEditViewModel : ViewModelBase
{
    private readonly WriteOffService _writeOffService;
    private readonly WarehouseService _warehouseService;

    public ObservableCollection<Warehouse> Warehouses { get; } = new();
    public ObservableCollection<StockOption> StockOptions { get; } = new();
    public ObservableCollection<WriteOffItemRowViewModel> Items { get; } = new();
    public List<string> Reasons { get; } = WriteOffReasons.All;

    private Warehouse? _selectedWarehouse;
    public Warehouse? SelectedWarehouse
    {
        get => _selectedWarehouse;
        set
        {
            if (SetField(ref _selectedWarehouse, value))
            {
                _ = OnWarehouseChangedAsync();
            }
        }
    }

    private string? _selectedReason;
    public string? SelectedReason { get => _selectedReason; set => SetField(ref _selectedReason, value); }

    private string _documentNumber = string.Empty;
    public string DocumentNumber { get => _documentNumber; set => SetField(ref _documentNumber, value); }

    private string _comment = string.Empty;
    public string Comment { get => _comment; set => SetField(ref _comment, value); }

    private string _errorMessage = string.Empty;
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public event Action<bool?>? RequestClose;

    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public WriteOffEditViewModel(WriteOffService writeOffService, WarehouseService warehouseService)
    {
        _writeOffService = writeOffService;
        _warehouseService = warehouseService;

        AddItemCommand = new RelayCommand(_ => Items.Add(new WriteOffItemRowViewModel(StockOptions)));
        RemoveItemCommand = new RelayCommand(row => RemoveItem(row as WriteOffItemRowViewModel));
        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    public async Task InitializeAsync()
    {
        var warehouses = await _warehouseService.GetAllAsync();
        Warehouses.Clear();
        foreach (var warehouse in warehouses) Warehouses.Add(warehouse);

        DocumentNumber = await _writeOffService.GenerateNextDocumentNumberAsync();
        SelectedReason = null;
        Comment = string.Empty;
        ErrorMessage = string.Empty;

        if (Warehouses.Count == 1)
        {
            // Если склад один (например, у заведующего), выбираем его сразу.
            // Это подгрузит остатки и добавит первую пустую строку.
            SelectedWarehouse = Warehouses[0];
        }
        else
        {
            Items.Clear();
            Items.Add(new WriteOffItemRowViewModel(StockOptions));
        }
    }

    // При смене склада список остатков и строки начинаются заново
    private async Task OnWarehouseChangedAsync()
    {
        StockOptions.Clear();
        Items.Clear();
        ErrorMessage = string.Empty;

        var warehouse = SelectedWarehouse;
        if (warehouse != null)
        {
            var options = await _writeOffService.GetStockOptionsAsync(warehouse.Id);

            if (SelectedWarehouse?.Id != warehouse.Id) return;

            foreach (var option in options) StockOptions.Add(option);
        }

        Items.Add(new WriteOffItemRowViewModel(StockOptions));
    }

    private void RemoveItem(WriteOffItemRowViewModel? row)
    {
        if (row != null && Items.Contains(row))
        {
            Items.Remove(row);
        }
    }

    private async Task SaveAsync()
    {
        var items = Items
            .Where(i => i.SelectedStock != null)
            .Select(i => new WriteOffItemInput
            {
                ProductId = i.SelectedStock!.ProductId,
                LocationId = i.SelectedStock.LocationId,
                Quantity = i.Quantity
            })
            .ToList();

        var result = await _writeOffService.AddAsync(
            SelectedWarehouse?.Id ?? 0,
            DocumentNumber,
            SelectedReason ?? string.Empty,
            Comment,
            items);

        if (result.Success)
        {
            ErrorMessage = string.Empty;
            RequestClose?.Invoke(true);
        }
        else
        {
            ErrorMessage = result.Error ?? "Ошибка сохранения.";
        }
    }
}